using System.Text.Json.Serialization;

namespace RecluseEdit.Core.Models;

// ─── Enums ────────────────────────────────────────────────────────────────────

/// <summary>License tier controls which features and extensions are available.</summary>
public enum LicenseTier
{
    /// <summary>Trial mode — 60-day free period; language/syntax extensions only.</summary>
    Trial,
    /// <summary>Standard end-user license; all extensions unlocked.</summary>
    User,
    /// <summary>Developer/QA/partner license; all extensions unlocked.</summary>
    Developer,
    /// <summary>Admin license; all extensions, no expiry.</summary>
    Admin,
    /// <summary>No license at all (should not persist; fallback to Trial).</summary>
    Unlicensed
}

/// <summary>Current lifecycle status of the active license.</summary>
public enum LicenseStatus
{
    /// <summary>Trial period active.</summary>
    Trial,
    /// <summary>License is valid and active.</summary>
    Active,
    /// <summary>Validated offline within grace period.</summary>
    OfflineGrace,
    /// <summary>Trial period has expired.</summary>
    TrialExpired,
    /// <summary>Paid license has expired.</summary>
    Expired,
    /// <summary>License was suspended by admin.</summary>
    Suspended,
    /// <summary>License was revoked.</summary>
    Revoked,
    /// <summary>No license is present.</summary>
    None
}

// ─── LicenseInfo ─────────────────────────────────────────────────────────────

/// <summary>
/// Represents the current license state of the application.
/// This is the central object used throughout the app to make gating decisions.
/// </summary>
public sealed record LicenseInfo
{
    public static readonly LicenseInfo Unlicensed = new()
    {
        Tier = LicenseTier.Unlicensed,
        Status = LicenseStatus.None,
        LicenseKey = string.Empty
    };

    public LicenseTier Tier { get; init; } = LicenseTier.Unlicensed;
    public LicenseStatus Status { get; init; } = LicenseStatus.None;
    public string LicenseKey { get; init; } = string.Empty;
    public string? Username { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastValidated { get; init; }
    public int GraceDaysLeft { get; init; }

    /// <summary>Trial-specific: when the trial started (from registry).</summary>
    public DateTime? TrialStartUtc { get; init; }

    /// <summary>How many days remain in the trial period. Negative = expired.</summary>
    public int TrialDaysRemaining =>
        TrialStartUtc.HasValue
            ? Math.Max(0, 60 - (int)(DateTime.UtcNow - TrialStartUtc.Value).TotalDays)
            : 0;

    /// <summary>True when the app can function normally (trial or active paid license).</summary>
    public bool IsValid => Status is LicenseStatus.Trial
                                   or LicenseStatus.Active
                                   or LicenseStatus.OfflineGrace;

    /// <summary>True when all extensions (not just language ones) should be unlocked.</summary>
    public bool AllExtensionsUnlocked => Tier is LicenseTier.Admin
                                              or LicenseTier.User
                                              or LicenseTier.Developer
                                        && Status is LicenseStatus.Active or LicenseStatus.OfflineGrace;

    /// <summary>Human-readable display name for the current tier.</summary>
    public string DisplayTierName => Tier switch
    {
        LicenseTier.Trial => "Trial",
        LicenseTier.User => "User",
        LicenseTier.Developer => "Developer",
        LicenseTier.Admin => "Admin",
        _ => "Unlicensed"
    };

    /// <summary>Emoji icon shown in the title-bar badge.</summary>
    public string BadgeIcon => Tier switch
    {
        LicenseTier.Admin => "🛡️",
        LicenseTier.Developer => "🔧",
        LicenseTier.User => "✅",
        LicenseTier.Trial => "⏳",
        _ => "🔒"
    };

    /// <summary>Short label shown on the title-bar badge.</summary>
    public string BadgeLabel => Tier switch
    {
        LicenseTier.Admin => "ADM",
        LicenseTier.Developer => "DEV",
        LicenseTier.User => "USER",
        LicenseTier.Trial => "TRIAL",
        _ => "UNLICENSED"
    };

    /// <summary>Accent colour for the badge (WPF-compatible hex string).</summary>
    public string BadgeColor => Tier switch
    {
        LicenseTier.Admin => "#E74C3C",
        LicenseTier.Developer => "#9B59B6",
        LicenseTier.User => "#27AE60",
        LicenseTier.Trial => "#E67E22",
        _ => "#7F8C8D"
    };
}

// ─── Local cache DTO (stored in %APPDATA%\RecluseEdit\license.json) ────────────

public sealed class LicenseCacheDto
{
    [JsonPropertyName("license_key")]   public string LicenseKey  { get; set; } = string.Empty;
    [JsonPropertyName("username")]      public string? Username    { get; set; }
    [JsonPropertyName("hwid")]          public string? Hwid        { get; set; }
    [JsonPropertyName("tier")]          public string Tier         { get; set; } = "Unlicensed";
    [JsonPropertyName("status")]        public string Status       { get; set; } = "None";
    [JsonPropertyName("expires_at")]    public DateTime? ExpiresAt { get; set; }
    [JsonPropertyName("last_validated")]public DateTime LastValidated { get; set; }
    [JsonPropertyName("grace_days_left")]public int GraceDaysLeft  { get; set; }
}

// ─── Licensor API DTOs ────────────────────────────────────────────────────────

public sealed class LicensorActivateRequest
{
    [JsonPropertyName("license_key")]  public string LicenseKey  { get; set; } = string.Empty;
    [JsonPropertyName("product_id")]   public string ProductId   { get; set; } = "recluse-edit";
    [JsonPropertyName("username")]     public string Username    { get; set; } = string.Empty;
    [JsonPropertyName("password")]     public string? Password   { get; set; }
    [JsonPropertyName("hwid")]         public string Hwid        { get; set; } = string.Empty;
    [JsonPropertyName("machine_name")] public string MachineName { get; set; } = string.Empty;
    [JsonPropertyName("platform")]     public string Platform    { get; set; } = "windows-amd64";
    [JsonPropertyName("app_version")]  public string AppVersion  { get; set; } = "6.1.0";
}

public sealed class LicensorActivateResponse
{
    [JsonPropertyName("success")]    public bool Success    { get; set; }
    [JsonPropertyName("status")]     public string Status   { get; set; } = string.Empty;
    [JsonPropertyName("message")]    public string Message  { get; set; } = string.Empty;
    [JsonPropertyName("expires_at")] public DateTime? ExpiresAt { get; set; }
    [JsonPropertyName("error")]      public string? Error   { get; set; }
}

public sealed class LicensorValidateRequest
{
    [JsonPropertyName("license_key")] public string LicenseKey { get; set; } = string.Empty;
    [JsonPropertyName("product_id")]  public string ProductId  { get; set; } = "recluse-edit";
    [JsonPropertyName("hwid")]        public string Hwid       { get; set; } = string.Empty;
    [JsonPropertyName("username")]    public string? Username  { get; set; }
}

public sealed class LicensorValidateResponse
{
    [JsonPropertyName("valid")]            public bool Valid         { get; set; }
    [JsonPropertyName("status")]           public string Status      { get; set; } = string.Empty;
    [JsonPropertyName("product_id")]       public string? ProductId  { get; set; }
    [JsonPropertyName("license_key")]      public string? LicenseKey { get; set; }
    [JsonPropertyName("username")]         public string? Username   { get; set; }
    [JsonPropertyName("prefix")]           public string? Prefix     { get; set; }
    [JsonPropertyName("expires_at")]       public DateTime? ExpiresAt { get; set; }
    [JsonPropertyName("message")]          public string Message     { get; set; } = string.Empty;
    [JsonPropertyName("grace_days_left")]  public int GraceDaysLeft  { get; set; }
    [JsonPropertyName("error")]            public string? Error      { get; set; }
}

public sealed class LicensorDeactivateRequest
{
    [JsonPropertyName("license_key")] public string LicenseKey { get; set; } = string.Empty;
    [JsonPropertyName("product_id")]  public string ProductId  { get; set; } = "recluse-edit";
    [JsonPropertyName("hwid")]        public string Hwid       { get; set; } = string.Empty;
    [JsonPropertyName("username")]    public string? Username  { get; set; }
    [JsonPropertyName("password")]    public string? Password  { get; set; }
}

public sealed class LicensorDeactivateResponse
{
    [JsonPropertyName("success")] public bool Success  { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("error")]   public string? Error  { get; set; }
}
