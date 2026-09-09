using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RecluseEdit.Extensions.DeepSeek.Models;

namespace RecluseEdit.Extensions.DeepSeek.Services;

/// <summary>
/// Persists and manages user configuration for the DeepSeek AI extension,
/// ensuring all API keys and secrets are stored in encrypted formats on disk.
/// </summary>
public class DeepSeekSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly byte[] Entropy = "RecluseEdit::DeepSeek::SecretStorage::v1"u8.ToArray();

    private readonly string _settingsFilePath;
    private DeepSeekSettings _currentSettings;

    public event Action<DeepSeekSettings>? SettingsChanged;

    public DeepSeekSettings CurrentSettings => _currentSettings;

    public DeepSeekSettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsFilePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var recluseDir = Path.Combine(appData, "RecluseEdit");
            _settingsFilePath = Path.Combine(recluseDir, "deepseek_settings.json");
        }

        _currentSettings = LoadSettings();
    }

    public DeepSeekSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<DeepSeekSettings>(json);
                if (loaded != null)
                {
                    var migrated = false;

                    // Decrypt stored API key
                    if (!string.IsNullOrEmpty(loaded.EncryptedApiKey))
                    {
                        loaded.ApiKey = DecryptSecret(loaded.EncryptedApiKey);
                    }
                    else if (!string.IsNullOrEmpty(loaded.LegacyPlaintextApiKey))
                    {
                        // One-way migration: encrypt existing plaintext key and scrub plaintext from disk
                        loaded.ApiKey = loaded.LegacyPlaintextApiKey;
                        loaded.EncryptedApiKey = EncryptSecret(loaded.ApiKey);
                        loaded.LegacyPlaintextApiKey = null;
                        migrated = true;
                    }

                    _currentSettings = loaded;

                    if (migrated)
                    {
                        SaveSettings(loaded);
                    }

                    return loaded;
                }
            }
        }
        catch
        {
            // Fall back to default settings on read error
        }

        _currentSettings = new DeepSeekSettings();
        return _currentSettings;
    }

    public void SaveSettings(DeepSeekSettings settings)
    {
        _currentSettings = settings;
        try
        {
            // Ensure API key is encrypted for disk storage
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                settings.EncryptedApiKey = EncryptSecret(settings.ApiKey);
            }
            else
            {
                settings.EncryptedApiKey = null;
            }

            // Guarantee legacy plaintext is never written
            settings.LegacyPlaintextApiKey = null;

            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
            SettingsChanged?.Invoke(settings);
        }
        catch
        {
            // Ignore write errors (e.g. read-only envs)
        }
    }

    /// <summary>
    /// Encrypts sensitive secret strings using Windows DPAPI (CurrentUser) or machine-bound AES fallback.
    /// </summary>
    public static string EncryptSecret(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return "dpapi:" + Convert.ToBase64String(encryptedBytes);
            }
        }
        catch
        {
            // Fallback to AES if DPAPI fails
        }

        return "aes:" + AesEncrypt(plainText);
    }

    /// <summary>
    /// Decrypts sensitive secret strings stored on disk.
    /// </summary>
    public static string DecryptSecret(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            if (cipherText.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
            {
                var payload = cipherText["dpapi:".Length..];
                var encryptedBytes = Convert.FromBase64String(payload);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }

            if (cipherText.StartsWith("aes:", StringComparison.OrdinalIgnoreCase))
            {
                var payload = cipherText["aes:".Length..];
                return AesDecrypt(payload);
            }

            // Raw base64 or legacy DPAPI attempt
            try
            {
                var rawBytes = Convert.FromBase64String(cipherText);
                var decrypted = ProtectedData.Unprotect(rawBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return cipherText;
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte[] GetMachineKey()
    {
        var raw = $"RecluseEdit::{Environment.MachineName}::{Environment.UserName}::SecretVault";
        return SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    private static string AesEncrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = GetMachineKey();
        aes.GenerateIV();

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private static string AesDecrypt(string cipherText)
    {
        var allBytes = Convert.FromBase64String(cipherText);
        using var ms = new MemoryStream(allBytes);

        var iv = new byte[16];
        var bytesRead = ms.Read(iv, 0, iv.Length);
        if (bytesRead < 16) return string.Empty;

        using var aes = Aes.Create();
        aes.Key = GetMachineKey();
        aes.IV = iv;

        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}
