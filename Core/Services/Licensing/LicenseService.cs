using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Win32;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services.Licensing;

/// <summary>
/// Central licensing service for RecluseEdit.
///
/// Responsibilities:
/// - Trial license creation / persistence via HKCU registry (survives uninstalls)
/// - Paid license activation / validation / deactivation via the Licensor server
/// - Offline grace period (14 days) via local JSON cache
/// - "Remove license" to revert to trial
/// - Background heartbeat validation
/// - Raising LicenseChanged when state transitions
/// </summary>
public sealed class LicenseService : IDisposable
{
    // ─── Constants ────────────────────────────────────────────────────────────
    private const string BaseUrl    = "https://licensor-h5zdysrkqa-uc.a.run.app";
    private const string ProductId  = "recluse-edit";
    private const int    GraceDays  = 14;
    private const int    TrialDays  = 60;

    // Registry path that survives uninstalls (HKCU user data, not HKLM)
    private const string RegPath    = @"SOFTWARE\IndoctrinatedRecluse\RecluseEdit\Licensing";

    // Local cache file path (for paid license caching)
    private static readonly string CacheFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RecluseEdit", "license.json");

    // ─── State ────────────────────────────────────────────────────────────────
    private LicenseInfo _current = LicenseInfo.Unlicensed;
    private readonly HttpClient _http;
    private Timer? _heartbeatTimer;
    private bool _disposed;

    public LicenseInfo CurrentLicense => _current;
    public event Action<LicenseInfo>? LicenseChanged;

    // ─── Constructor ──────────────────────────────────────────────────────────
    public LicenseService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    // ─── Initialise (call once at startup) ───────────────────────────────────

    /// <summary>
    /// Loads the current license state. Always results in a valid LicenseInfo
    /// (trial if nothing else). Call this from MainWindow constructor.
    /// </summary>
    public async Task InitializeAsync()
    {
        // 1. Try to restore paid license from local cache
        var cached = TryLoadCachedPaidLicense();
        if (cached is not null)
        {
            _current = cached;
            Notify();

            // 2. Background-validate the cached license (non-blocking)
            _ = Task.Run(() => BackgroundValidateAsync());
            StartHeartbeat();
            return;
        }

        // 3. No paid license — ensure trial exists in registry
        var trial = EnsureTrialLicense();
        _current = trial;
        Notify();
    }

    // ─── Trial License ────────────────────────────────────────────────────────

    /// <summary>
    /// Reads or creates the trial license in HKCU registry.
    /// Returns a LicenseInfo with Tier=Trial.
    /// </summary>
    private static LicenseInfo EnsureTrialLicense()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegPath, writable: true);

        var startStr = key.GetValue("TrialStartUtc") as string;
        DateTime start;

        if (string.IsNullOrEmpty(startStr) ||
            !DateTime.TryParse(startStr, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out start))
        {
            // First run — create trial
            start = DateTime.UtcNow;
            key.SetValue("TrialStartUtc", start.ToString("O"), RegistryValueKind.String);
            key.SetValue("LicenseTier", "Trial", RegistryValueKind.String);
        }

        var daysElapsed = (int)(DateTime.UtcNow - start).TotalDays;
        var remaining   = Math.Max(0, TrialDays - daysElapsed);
        var expired     = daysElapsed >= TrialDays;

        return new LicenseInfo
        {
            Tier          = LicenseTier.Trial,
            Status        = expired ? LicenseStatus.TrialExpired : LicenseStatus.Trial,
            LicenseKey    = "TRIAL",
            TrialStartUtc = start,
            ExpiresAt     = start.AddDays(TrialDays),
        };
    }

    // ─── Activate ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Activates a new license key against the Licensor server.
    /// Returns (success, errorMessage).
    /// </summary>
    public async Task<(bool Ok, string? Error)> ActivateAsync(
        string licenseKey, string username, string? password)
    {
        var tier = DetectTier(licenseKey);
        if (tier == LicenseTier.Unlicensed)
            return (false, "Invalid license key format. Expected ADM-xxxx-xxxx-xxxx, USER-xxxx-xxxx-xxxx, or DEV-xxxx-xxxx-xxxx.");

        var hwid = HwidService.GetHwid();

        // ADM keys don't need username/password
        if (tier == LicenseTier.Admin)
        {
            username = !string.IsNullOrEmpty(username) ? username : "admin";
            password = string.Empty;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username is required for USER and DEV licenses.");
        }

        try
        {
            var req = new LicensorActivateRequest
            {
                LicenseKey  = licenseKey,
                ProductId   = ProductId,
                Username    = username,
                Password    = password,
                Hwid        = hwid,
                MachineName = Environment.MachineName,
                Platform    = "windows-amd64",
                AppVersion  = "6.2.1"
            };

            var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/license/activate", req);
            var body = await response.Content.ReadFromJsonAsync<LicensorActivateResponse>();

            if (body is null)
                return (false, "Empty response from licensing server.");

            if (!response.IsSuccessStatusCode || !(body.Success))
                return (false, body.Error ?? body.Message ?? $"Server returned {(int)response.StatusCode}");

            // Activation succeeded — build LicenseInfo
            var info = new LicenseInfo
            {
                Tier          = tier,
                Status        = LicenseStatus.Active,
                LicenseKey    = licenseKey,
                Username      = username,
                ExpiresAt     = body.ExpiresAt,
                LastValidated = DateTime.UtcNow,
                GraceDaysLeft = GraceDays,
            };

            SaveCachedPaidLicense(info);
            WriteRegistryTier(tier.ToString());
            _current = info;
            Notify();
            StartHeartbeat();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Network error: {ex.Message}");
        }
    }

    // ─── Validate (background heartbeat) ─────────────────────────────────────

    private async Task BackgroundValidateAsync()
    {
        if (_current.Tier == LicenseTier.Trial) return;
        if (_current.Tier == LicenseTier.Admin)
        {
            // ADM licenses: just extend grace and mark active — no server check needed for lifetime
            _current = _current with { Status = LicenseStatus.Active, LastValidated = DateTime.UtcNow };
            SaveCachedPaidLicense(_current);
            Notify();
            return;
        }

        try
        {
            var req = new LicensorValidateRequest
            {
                LicenseKey = _current.LicenseKey,
                ProductId  = ProductId,
                Hwid       = HwidService.GetHwid(),
                Username   = _current.Username
            };

            var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/license/validate", req);
            var body = await response.Content.ReadFromJsonAsync<LicensorValidateResponse>();

            if (body is null) { HandleOffline(); return; }

            if (body.Valid)
            {
                var info = _current with
                {
                    Status        = LicenseStatus.Active,
                    ExpiresAt     = body.ExpiresAt,
                    LastValidated = DateTime.UtcNow,
                    GraceDaysLeft = body.GraceDaysLeft > 0 ? body.GraceDaysLeft : GraceDays
                };
                _current = info;
                SaveCachedPaidLicense(_current);
                Notify();
            }
            else
            {
                var newStatus = body.Status switch
                {
                    "expired"   => LicenseStatus.Expired,
                    "suspended" => LicenseStatus.Suspended,
                    "revoked"   => LicenseStatus.Revoked,
                    _           => LicenseStatus.None
                };
                _current = _current with { Status = newStatus };
                // For expired/revoked, fall back to trial
                if (newStatus is LicenseStatus.Expired or LicenseStatus.Revoked or LicenseStatus.Suspended)
                    RemovePaidLicense();
                else
                    Notify();
            }
        }
        catch
        {
            HandleOffline();
        }
    }

    private void HandleOffline()
    {
        if (_current.LastValidated.HasValue)
        {
            var daysSinceValidation = (int)(DateTime.UtcNow - _current.LastValidated.Value).TotalDays;
            var graceDaysLeft = Math.Max(0, GraceDays - daysSinceValidation);
            if (graceDaysLeft > 0)
            {
                _current = _current with
                {
                    Status        = LicenseStatus.OfflineGrace,
                    GraceDaysLeft = graceDaysLeft
                };
                Notify();
                return;
            }
        }
        // Grace expired → revert to trial
        RemovePaidLicense();
    }

    // ─── Deactivate ───────────────────────────────────────────────────────────

    /// <summary>
    /// Deactivates this machine's seat on the Licensor server.
    /// Returns (success, errorMessage).
    /// </summary>
    public async Task<(bool Ok, string? Error)> DeactivateAsync(string? password = null)
    {
        if (_current.Tier == LicenseTier.Trial || _current.Tier == LicenseTier.Unlicensed)
            return (false, "No paid license to deactivate.");

        try
        {
            var req = new LicensorDeactivateRequest
            {
                LicenseKey = _current.LicenseKey,
                ProductId  = ProductId,
                Hwid       = HwidService.GetHwid(),
                Username   = _current.Username,
                Password   = password
            };

            var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/license/deactivate", req);
            var body = await response.Content.ReadFromJsonAsync<LicensorDeactivateResponse>();

            if (body is null)
                return (false, "Empty response from licensing server.");

            if (!response.IsSuccessStatusCode || !body.Success)
                return (false, body.Error ?? body.Message ?? $"Server returned {(int)response.StatusCode}");

            RemovePaidLicense();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Network error: {ex.Message}");
        }
    }

    // ─── Remove Paid License (revert to trial) ────────────────────────────────

    /// <summary>
    /// Clears the paid license locally and reverts to the trial license.
    /// Does NOT call the deactivation endpoint (use DeactivateAsync for that).
    /// </summary>
    public void RemovePaidLicense()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;

        // Delete local paid license cache
        if (File.Exists(CacheFile))
        {
            try { File.Delete(CacheFile); } catch { /* ignore */ }
        }

        // Clear registry paid keys (keep TrialStartUtc)
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        if (key is not null)
        {
            key.DeleteValue("PaidLicenseKey", throwOnMissingValue: false);
            key.DeleteValue("PaidUsername",   throwOnMissingValue: false);
            key.SetValue("LicenseTier", "Trial", RegistryValueKind.String);
        }

        _current = EnsureTrialLicense();
        Notify();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static LicenseTier DetectTier(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return LicenseTier.Unlicensed;
        var up = key.Trim().ToUpperInvariant();
        if (up.StartsWith("ADM-"))  return LicenseTier.Admin;
        if (up.StartsWith("USER-")) return LicenseTier.User;
        if (up.StartsWith("DEV-"))  return LicenseTier.Developer;
        return LicenseTier.Unlicensed;
    }

    private static bool IsValidKeyFormat(string key)
    {
        // Pattern: PREFIX-XXXX-XXXX-XXXX (3 groups of 4 alphanum after prefix)
        var parts = key?.Trim().Split('-') ?? [];
        return parts.Length == 4
            && parts[0] is "ADM" or "USER" or "DEV"
            && parts[1].Length == 4
            && parts[2].Length == 4
            && parts[3].Length == 4;
    }

    public static bool ValidateKeyFormat(string key) => IsValidKeyFormat(key);
    public static LicenseTier DetectLicenseTier(string key) => DetectTier(key);

    private static void SaveCachedPaidLicense(LicenseInfo info)
    {
        try
        {
            var dir = Path.GetDirectoryName(CacheFile)!;
            Directory.CreateDirectory(dir);
            var dto = new LicenseCacheDto
            {
                LicenseKey    = info.LicenseKey,
                Username      = info.Username,
                Hwid          = HwidService.GetHwid(),
                Tier          = info.Tier.ToString(),
                Status        = info.Status.ToString(),
                ExpiresAt     = info.ExpiresAt,
                LastValidated = info.LastValidated ?? DateTime.UtcNow,
                GraceDaysLeft = info.GraceDaysLeft
            };
            File.WriteAllText(CacheFile, JsonSerializer.Serialize(dto,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-fatal */ }
    }

    private static LicenseInfo? TryLoadCachedPaidLicense()
    {
        if (!File.Exists(CacheFile)) return null;
        try
        {
            var json = File.ReadAllText(CacheFile);
            var dto  = JsonSerializer.Deserialize<LicenseCacheDto>(json);
            if (dto is null || string.IsNullOrEmpty(dto.LicenseKey)) return null;
            if (dto.LicenseKey == "TRIAL") return null;

            var tier = Enum.TryParse<LicenseTier>(dto.Tier, out var t) ? t : LicenseTier.Unlicensed;
            if (tier == LicenseTier.Unlicensed) return null;

            // Check if grace has expired locally before even hitting the network
            var daysSince = (int)(DateTime.UtcNow - dto.LastValidated).TotalDays;
            var graceDaysLeft = Math.Max(0, GraceDays - daysSince);

            // ADM licenses don't expire
            if (tier == LicenseTier.Admin)
            {
                return new LicenseInfo
                {
                    Tier          = LicenseTier.Admin,
                    Status        = LicenseStatus.Active,
                    LicenseKey    = dto.LicenseKey,
                    Username      = dto.Username,
                    LastValidated = dto.LastValidated,
                    GraceDaysLeft = GraceDays
                };
            }

            if (graceDaysLeft <= 0) return null; // will revert to trial below

            return new LicenseInfo
            {
                Tier          = tier,
                Status        = graceDaysLeft < GraceDays ? LicenseStatus.OfflineGrace : LicenseStatus.Active,
                LicenseKey    = dto.LicenseKey,
                Username      = dto.Username,
                ExpiresAt     = dto.ExpiresAt,
                LastValidated = dto.LastValidated,
                GraceDaysLeft = graceDaysLeft
            };
        }
        catch
        {
            return null;
        }
    }

    private static void WriteRegistryTier(string tier)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegPath, writable: true);
            key.SetValue("LicenseTier", tier, RegistryValueKind.String);
        }
        catch { /* non-fatal */ }
    }

    private void StartHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        // Validate every 6 hours
        _heartbeatTimer = new Timer(
            _ => _ = Task.Run(() => BackgroundValidateAsync()),
            null,
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(6));
    }

    private void Notify() =>
        LicenseChanged?.Invoke(_current);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _heartbeatTimer?.Dispose();
        _http.Dispose();
    }
}
