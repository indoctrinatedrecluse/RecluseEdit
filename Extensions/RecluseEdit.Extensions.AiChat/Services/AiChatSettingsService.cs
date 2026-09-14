using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RecluseEdit.Extensions.AiChat.Models;

namespace RecluseEdit.Extensions.AiChat.Services;

/// <summary>
/// Persists and manages user configuration for the AI Chat extension,
/// ensuring all API keys and secrets are stored in encrypted formats on disk.
/// Automatically migrates legacy DeepSeek settings if present.
/// </summary>
public class AiChatSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly byte[] Entropy = "RecluseEdit::AiChat::SecretStorage::v1"u8.ToArray();
    private static readonly byte[] LegacyEntropy = "RecluseEdit::DeepSeek::SecretStorage::v1"u8.ToArray();

    private readonly string _settingsFilePath;
    private readonly string? _legacyFilePath;
    private AiChatSettings _currentSettings;

    public event Action<AiChatSettings>? SettingsChanged;

    public AiChatSettings CurrentSettings => _currentSettings;

    public AiChatSettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsFilePath = customPath;
            _legacyFilePath = null;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var recluseDir = Path.Combine(appData, "RecluseEdit");
            _settingsFilePath = Path.Combine(recluseDir, "aichat_settings.json");
            _legacyFilePath = Path.Combine(recluseDir, "deepseek_settings.json");
        }

        _currentSettings = LoadSettings();
    }

    public AiChatSettings LoadSettings()
    {
        try
        {
            // 1. Try loading primary aichat_settings.json
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AiChatSettings>(json);
                if (loaded != null)
                {
                    var hadPlaintext = !string.IsNullOrEmpty(loaded.LegacyPlaintextApiKey);
                    DecryptSettingsKey(loaded, isLegacy: false);
                    _currentSettings = loaded;
                    if (hadPlaintext)
                    {
                        SaveSettings(loaded);
                    }
                    return loaded;
                }
            }

            // 2. Try migrating from legacy deepseek_settings.json if present
            if (!string.IsNullOrEmpty(_legacyFilePath) && File.Exists(_legacyFilePath))
            {
                var legacyJson = File.ReadAllText(_legacyFilePath);
                var legacyLoaded = JsonSerializer.Deserialize<AiChatSettings>(legacyJson);
                if (legacyLoaded != null)
                {
                    DecryptSettingsKey(legacyLoaded, isLegacy: true);
                    _currentSettings = legacyLoaded;
                    SaveSettings(legacyLoaded);
                    return legacyLoaded;
                }
            }
        }
        catch
        {
            // Fall back to default settings on read error
        }

        _currentSettings = new AiChatSettings();
        return _currentSettings;
    }

    private void DecryptSettingsKey(AiChatSettings settings, bool isLegacy)
    {
        if (!string.IsNullOrEmpty(settings.EncryptedApiKey))
        {
            settings.ApiKey = DecryptSecret(settings.EncryptedApiKey, isLegacy);
        }
        else if (!string.IsNullOrEmpty(settings.LegacyPlaintextApiKey))
        {
            settings.ApiKey = settings.LegacyPlaintextApiKey;
            settings.EncryptedApiKey = EncryptSecret(settings.ApiKey);
            settings.LegacyPlaintextApiKey = null;
        }
    }

    public void SaveSettings(AiChatSettings settings)
    {
        _currentSettings = settings;
        try
        {
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                settings.EncryptedApiKey = EncryptSecret(settings.ApiKey);
            }
            else
            {
                settings.EncryptedApiKey = null;
            }

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
            // Ignore write errors
        }
    }

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

    public static string DecryptSecret(string cipherText, bool checkLegacy = false)
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
                try
                {
                    var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
                catch when (checkLegacy)
                {
                    var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, LegacyEntropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }

            if (cipherText.StartsWith("aes:", StringComparison.OrdinalIgnoreCase))
            {
                var payload = cipherText["aes:".Length..];
                return AesDecrypt(payload);
            }

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
        var raw = $"RecluseEdit::{Environment.MachineName}::{Environment.UserName}::AiChatSecretVault";
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

/// <summary>
/// Backward-compatibility alias for DeepSeekSettingsService.
/// </summary>
public class DeepSeekSettingsService : AiChatSettingsService
{
    public DeepSeekSettingsService(string? customPath = null) : base(customPath) { }
}
