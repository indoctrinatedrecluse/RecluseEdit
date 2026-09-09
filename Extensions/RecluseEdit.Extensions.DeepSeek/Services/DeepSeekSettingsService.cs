using System.IO;
using System.Text.Json;
using RecluseEdit.Extensions.DeepSeek.Models;

namespace RecluseEdit.Extensions.DeepSeek.Services;

/// <summary>
/// Persists and manages user configuration for the DeepSeek AI extension.
/// </summary>
public class DeepSeekSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

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
                    _currentSettings = loaded;
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
}
