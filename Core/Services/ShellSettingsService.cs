using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Data model for persistent terminal and shell settings.
/// </summary>
public class TerminalSettings
{
    public Dictionary<string, string> CustomShellPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? DefaultShellId { get; set; }
}

/// <summary>
/// Persists and manages user-configured shell executable paths and terminal preferences.
/// </summary>
public class ShellSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsFilePath;
    private TerminalSettings _currentSettings;

    public TerminalSettings CurrentSettings => _currentSettings;

    public ShellSettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsFilePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var recluseDir = Path.Combine(appData, "RecluseEdit");
            _settingsFilePath = Path.Combine(recluseDir, "terminal_settings.json");
        }

        _currentSettings = LoadSettings();
    }

    public TerminalSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<TerminalSettings>(json);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                    return _currentSettings;
                }
            }
        }
        catch
        {
            // Fall back to empty settings on read error
        }

        _currentSettings = new TerminalSettings();
        return _currentSettings;
    }

    public void SaveSettings(TerminalSettings settings)
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
        }
        catch
        {
            // Ignore write errors in restricted environments
        }
    }

    public string? GetCustomPath(string shellId)
    {
        if (string.IsNullOrWhiteSpace(shellId)) return null;
        return _currentSettings.CustomShellPaths.TryGetValue(shellId, out var path) ? path : null;
    }

    public void SetCustomPath(string shellId, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(shellId)) return;

        _currentSettings.CustomShellPaths[shellId] = executablePath;
        SaveSettings(_currentSettings);
    }

    public void RemoveCustomPath(string shellId)
    {
        if (string.IsNullOrWhiteSpace(shellId)) return;

        if (_currentSettings.CustomShellPaths.Remove(shellId))
        {
            SaveSettings(_currentSettings);
        }
    }
}

