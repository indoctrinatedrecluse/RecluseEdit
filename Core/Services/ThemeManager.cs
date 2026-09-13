using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Core service managing theme discovery, dynamic WPF resource mutation,
/// editor styling, live preview harmonization, and user preferences.
/// </summary>
public class ThemeManager
{
    private readonly List<IThemeDefinition> _themes = [];
    private IThemeDefinition _activeTheme;
    private IThemeDefinition? _previewOriginalTheme;
    private readonly string _settingsFilePath;

    public IReadOnlyList<IThemeDefinition> RegisteredThemes => _themes.AsReadOnly();
    public IThemeDefinition ActiveTheme => _activeTheme;

    public event Action<IThemeDefinition>? ThemeChanged;

    public ThemeManager(string? customSettingsPath = null)
    {
        if (!string.IsNullOrEmpty(customSettingsPath))
        {
            _settingsFilePath = customSettingsPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "RecluseEdit");
            try { Directory.CreateDirectory(folder); } catch { }
            _settingsFilePath = Path.Combine(folder, "theme_settings.json");
        }

        // Register default 9 built-in creative themes
        RegisterBuiltInThemes();

        // Default to Recluse Dark+
        _activeTheme = _themes[0];
    }

    private void RegisterBuiltInThemes()
    {
        // 1. Recluse Dark+ (Default)
        RegisterTheme(new BuiltInTheme(
            id: "recluse.dark",
            displayName: "Recluse Dark+ (Default)",
            type: ThemeType.Dark,
            description: "Modern VS Code-inspired dark theme with deep neutral slate grays and VS blue accents.",
            author: "indoctrinatedrecluse",
            colors: new ThemeColors
            {
                BgPrimary = "#1E1E1E",
                BgSecondary = "#252526",
                BgTertiary = "#2D2D30",
                BgHover = "#3E3E42",
                BgActive = "#007ACC",
                FgPrimary = "#D4D4D4",
                FgSecondary = "#969696",
                FgMuted = "#6E6E6E",
                FgAccent = "#FFFFFF",
                BorderDark = "#333337",
                BorderLight = "#3F3F46",
                Accent = "#007ACC",
                AccentHover = "#1C97EA",
                AccentMuted = "#1B3A5A",
                GutterBg = "#1E1E1E",
                LineNumberFg = "#858585",
                Caret = "#AEAFAD",
                Selection = "#264F78",
                CurrentLine = "#282828",
                BracketMatch = "#007ACC",
                StatusBarBg = "#007ACC",
                StatusBarFg = "#FFFFFF",
                ActivityBarBg = "#252526",
                ActivityBarFg = "#858585",
                ActivityBarSelected = "#FFFFFF",
                TerminalBg = "#181818",
                TerminalFg = "#CCCCCC",
                MarkdownBg = "#0D1117",
                MarkdownFg = "#C9D1D9",
                MarkdownCodeBg = "#161B22"
            }
        ));

        // 2. Recluse Light+ (Clean Daylight)
        RegisterTheme(new BuiltInTheme(
            id: "recluse.light",
            displayName: "Recluse Light+ (Clean Daylight)",
            type: ThemeType.Light,
            description: "Crisp, daylight mode with soft gray panels, high-clarity charcoal text, and blue headers.",
            author: "indoctrinatedrecluse",
            colors: new ThemeColors
            {
                BgPrimary = "#FFFFFF",
                BgSecondary = "#F3F3F3",
                BgTertiary = "#E5E5E5",
                BgHover = "#E8E8E8",
                BgActive = "#0078D4",
                FgPrimary = "#1E1E1E",
                FgSecondary = "#6E6E6E",
                FgMuted = "#9E9E9E",
                FgAccent = "#000000",
                BorderDark = "#E0E0E0",
                BorderLight = "#CCCCCC",
                Accent = "#0066B8",
                AccentHover = "#107C41",
                AccentMuted = "#D0E7F9",
                GutterBg = "#F8F8F8",
                LineNumberFg = "#A0A0A0",
                Caret = "#000000",
                Selection = "#ADD6FF",
                CurrentLine = "#F0F0F0",
                BracketMatch = "#0066B8",
                StatusBarBg = "#0066B8",
                StatusBarFg = "#FFFFFF",
                ActivityBarBg = "#2C2C2C",
                ActivityBarFg = "#D0D0D0",
                ActivityBarSelected = "#FFFFFF",
                TerminalBg = "#F8F8F8",
                TerminalFg = "#1E1E1E",
                MarkdownBg = "#FFFFFF",
                MarkdownFg = "#24292F",
                MarkdownCodeBg = "#F6F8FA"
            }
        ));

        // 3. Cyberpunk 2077 / Neon Dusk
        RegisterTheme(new BuiltInTheme(
            id: "recluse.cyberpunk",
            displayName: "Cyberpunk 2077 (Neon Dusk)",
            type: ThemeType.Creative,
            description: "High-octane synthwave theme featuring electric violet, laser cyan, and hot neon magenta.",
            author: "indoctrinatedrecluse",
            colors: new ThemeColors
            {
                BgPrimary = "#120E24",
                BgSecondary = "#1A1435",
                BgTertiary = "#261C4A",
                BgHover = "#3B2668",
                BgActive = "#FF2A6D",
                FgPrimary = "#E0F7FA",
                FgSecondary = "#05D9E8",
                FgMuted = "#795290",
                FgAccent = "#FFE600",
                BorderDark = "#3E266D",
                BorderLight = "#542D8F",
                Accent = "#FF2A6D",
                AccentHover = "#05D9E8",
                AccentMuted = "#3D1338",
                GutterBg = "#120E24",
                LineNumberFg = "#795290",
                Caret = "#05D9E8",
                Selection = "#501B5E",
                CurrentLine = "#1F153F",
                BracketMatch = "#FFE600",
                StatusBarBg = "#7A04EB",
                StatusBarFg = "#FFE600",
                ActivityBarBg = "#150C2E",
                ActivityBarFg = "#05D9E8",
                ActivityBarSelected = "#FF2A6D",
                TerminalBg = "#0E0A1E",
                TerminalFg = "#05D9E8",
                MarkdownBg = "#120E24",
                MarkdownFg = "#E0F7FA",
                MarkdownCodeBg = "#1A1435"
            }
        ));

        // 4. Monokai Pro
        RegisterTheme(new BuiltInTheme(
            id: "recluse.monokai",
            displayName: "Monokai Pro",
            type: ThemeType.Dark,
            description: "Warm charcoal olive palette with vibrant green, sunshine yellow, and magenta accents.",
            author: "Monokai",
            colors: new ThemeColors
            {
                BgPrimary = "#272822",
                BgSecondary = "#1E1F1C",
                BgTertiary = "#34352F",
                BgHover = "#49483E",
                BgActive = "#A6E22E",
                FgPrimary = "#F8F8F2",
                FgSecondary = "#CFCFC2",
                FgMuted = "#75715E",
                FgAccent = "#FD971F",
                BorderDark = "#383930",
                BorderLight = "#49483E",
                Accent = "#A6E22E",
                AccentHover = "#E6DB74",
                AccentMuted = "#2E3B1C",
                GutterBg = "#272822",
                LineNumberFg = "#90908A",
                Caret = "#F8F8F0",
                Selection = "#49483E",
                CurrentLine = "#3E3D32",
                BracketMatch = "#F92672",
                StatusBarBg = "#1E1F1C",
                StatusBarFg = "#A6E22E",
                ActivityBarBg = "#1E1F1C",
                ActivityBarFg = "#90908A",
                ActivityBarSelected = "#A6E22E",
                TerminalBg = "#1B1C19",
                TerminalFg = "#F8F8F2",
                MarkdownBg = "#272822",
                MarkdownFg = "#F8F8F2",
                MarkdownCodeBg = "#1E1F1C"
            }
        ));

        // 5. Solarized Dark
        RegisterTheme(new BuiltInTheme(
            id: "recluse.solarized",
            displayName: "Solarized Dark",
            type: ThemeType.Dark,
            description: "Precision low-contrast palette designed by Ethan Schoonover for eye comfort during long sessions.",
            author: "Ethan Schoonover",
            colors: new ThemeColors
            {
                BgPrimary = "#002B36",
                BgSecondary = "#073642",
                BgTertiary = "#094959",
                BgHover = "#0E586C",
                BgActive = "#268BD2",
                FgPrimary = "#839496",
                FgSecondary = "#657B83",
                FgMuted = "#586E75",
                FgAccent = "#93A1A1",
                BorderDark = "#073642",
                BorderLight = "#094959",
                Accent = "#268BD2",
                AccentHover = "#2AA198",
                AccentMuted = "#0A4656",
                GutterBg = "#002B36",
                LineNumberFg = "#586E75",
                Caret = "#93A1A1",
                Selection = "#073642",
                CurrentLine = "#073642",
                BracketMatch = "#B58900",
                StatusBarBg = "#073642",
                StatusBarFg = "#2AA198",
                ActivityBarBg = "#00212B",
                ActivityBarFg = "#657B83",
                ActivityBarSelected = "#268BD2",
                TerminalBg = "#00212B",
                TerminalFg = "#839496",
                MarkdownBg = "#002B36",
                MarkdownFg = "#839496",
                MarkdownCodeBg = "#073642"
            }
        ));

        // 6. Dracula (Vampire Night)
        RegisterTheme(new BuiltInTheme(
            id: "recluse.dracula",
            displayName: "Dracula (Vampire Night)",
            type: ThemeType.Dark,
            description: "Famous dark gothic theme with rich purples, neon pink, cyan, and spring green highlights.",
            author: "Zeno Rocha",
            colors: new ThemeColors
            {
                BgPrimary = "#282A36",
                BgSecondary = "#21222C",
                BgTertiary = "#343746",
                BgHover = "#44475A",
                BgActive = "#BD93F9",
                FgPrimary = "#F8F8F2",
                FgSecondary = "#BD93F9",
                FgMuted = "#6272A4",
                FgAccent = "#50FA7B",
                BorderDark = "#383A49",
                BorderLight = "#44475A",
                Accent = "#BD93F9",
                AccentHover = "#FF79C6",
                AccentMuted = "#382D54",
                GutterBg = "#282A36",
                LineNumberFg = "#6272A4",
                Caret = "#F8F8F0",
                Selection = "#44475A",
                CurrentLine = "#303241",
                BracketMatch = "#FF79C6",
                StatusBarBg = "#191A21",
                StatusBarFg = "#BD93F9",
                ActivityBarBg = "#191A21",
                ActivityBarFg = "#6272A4",
                ActivityBarSelected = "#BD93F9",
                TerminalBg = "#1E1F29",
                TerminalFg = "#F8F8F2",
                MarkdownBg = "#282A36",
                MarkdownFg = "#F8F8F2",
                MarkdownCodeBg = "#21222C"
            }
        ));

        // 7. Nord (Arctic Frost)
        RegisterTheme(new BuiltInTheme(
            id: "recluse.nord",
            displayName: "Nord (Arctic Frost)",
            type: ThemeType.Dark,
            description: "Arctic Scandinavian aesthetic with cool polar slate, snow storm typography, and frost cyan.",
            author: "Arctic Ice Studio",
            colors: new ThemeColors
            {
                BgPrimary = "#2E3440",
                BgSecondary = "#3B4252",
                BgTertiary = "#434C5E",
                BgHover = "#4C566A",
                BgActive = "#88C0D0",
                FgPrimary = "#ECEFF4",
                FgSecondary = "#D8DEE9",
                FgMuted = "#616E88",
                FgAccent = "#88C0D0",
                BorderDark = "#3B4252",
                BorderLight = "#4C566A",
                Accent = "#88C0D0",
                AccentHover = "#81A1C1",
                AccentMuted = "#2B404E",
                GutterBg = "#2E3440",
                LineNumberFg = "#616E88",
                Caret = "#D8DEE9",
                Selection = "#434C5E",
                CurrentLine = "#353C4A",
                BracketMatch = "#88C0D0",
                StatusBarBg = "#3B4252",
                StatusBarFg = "#88C0D0",
                ActivityBarBg = "#2E3440",
                ActivityBarFg = "#D8DEE9",
                ActivityBarSelected = "#88C0D0",
                TerminalBg = "#272C36",
                TerminalFg = "#ECEFF4",
                MarkdownBg = "#2E3440",
                MarkdownFg = "#ECEFF4",
                MarkdownCodeBg = "#3B4252"
            }
        ));

        // 8. Retro Matrix / Cyber Green
        RegisterTheme(new BuiltInTheme(
            id: "recluse.matrix",
            displayName: "Retro Matrix (Phosphor Green)",
            type: ThemeType.Creative,
            description: "Pure hacker nostalgia: deep obsidian CRT black illuminated by luminous phosphor green.",
            author: "indoctrinatedrecluse",
            colors: new ThemeColors
            {
                BgPrimary = "#0A0E0A",
                BgSecondary = "#0F160F",
                BgTertiary = "#162016",
                BgHover = "#1C2E1C",
                BgActive = "#00FF66",
                FgPrimary = "#00FF66",
                FgSecondary = "#00CC52",
                FgMuted = "#006629",
                FgAccent = "#66FF99",
                BorderDark = "#162B16",
                BorderLight = "#234523",
                Accent = "#00FF66",
                AccentHover = "#33FF85",
                AccentMuted = "#0B2613",
                GutterBg = "#0A0E0A",
                LineNumberFg = "#008033",
                Caret = "#00FF66",
                Selection = "#143D1D",
                CurrentLine = "#101B10",
                BracketMatch = "#66FF99",
                StatusBarBg = "#0D1A0D",
                StatusBarFg = "#00FF66",
                ActivityBarBg = "#080C08",
                ActivityBarFg = "#008033",
                ActivityBarSelected = "#00FF66",
                TerminalBg = "#060906",
                TerminalFg = "#00FF66",
                MarkdownBg = "#0A0E0A",
                MarkdownFg = "#00FF66",
                MarkdownCodeBg = "#0F160F"
            }
        ));

        // 9. High Contrast Black (Accessibility)
        RegisterTheme(new BuiltInTheme(
            id: "recluse.highcontrast",
            displayName: "High Contrast Black",
            type: ThemeType.HighContrast,
            description: "W3C AAA high-contrast theme with pitch black background, stark white text, and vivid borders.",
            author: "indoctrinatedrecluse",
            colors: new ThemeColors
            {
                BgPrimary = "#000000",
                BgSecondary = "#0C0C0C",
                BgTertiary = "#181818",
                BgHover = "#282828",
                BgActive = "#6FC3DF",
                FgPrimary = "#FFFFFF",
                FgSecondary = "#E0E0E0",
                FgMuted = "#A0A0A0",
                FgAccent = "#FFFF00",
                BorderDark = "#6FC3DF",
                BorderLight = "#FFFFFF",
                Accent = "#F38518",
                AccentHover = "#FFFF00",
                AccentMuted = "#302010",
                GutterBg = "#000000",
                LineNumberFg = "#FFFFFF",
                Caret = "#FFFFFF",
                Selection = "#0E3A5A",
                CurrentLine = "#181818",
                BracketMatch = "#FFFF00",
                StatusBarBg = "#000000",
                StatusBarFg = "#FFFFFF",
                ActivityBarBg = "#000000",
                ActivityBarFg = "#FFFFFF",
                ActivityBarSelected = "#FFFF00",
                TerminalBg = "#000000",
                TerminalFg = "#FFFFFF",
                MarkdownBg = "#000000",
                MarkdownFg = "#FFFFFF",
                MarkdownCodeBg = "#121212"
            }
        ));
    }

    public void RegisterTheme(IThemeDefinition theme)
    {
        var existing = _themes.FirstOrDefault(t => t.Id == theme.Id);
        if (existing != null)
        {
            _themes.Remove(existing);
        }
        _themes.Add(theme);
    }

    public void ApplyTheme(IThemeDefinition theme, bool persist = true)
    {
        _activeTheme = theme;
        _previewOriginalTheme = null;

        UpdateApplicationResources(theme.Colors);

        if (persist)
        {
            SaveThemePreference(theme.Id);
        }

        ThemeChanged?.Invoke(theme);
    }

    public void PreviewTheme(IThemeDefinition theme)
    {
        _previewOriginalTheme ??= _activeTheme;
        UpdateApplicationResources(theme.Colors);
        ThemeChanged?.Invoke(theme);
    }

    public void RollbackPreview()
    {
        if (_previewOriginalTheme != null)
        {
            var target = _previewOriginalTheme;
            _previewOriginalTheme = null;
            ApplyTheme(target, persist: false);
        }
    }

    public void LoadPersistedTheme()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("themeId", out var idProp))
                {
                    var id = idProp.GetString();
                    var found = _themes.FirstOrDefault(t => t.Id == id);
                    if (found != null)
                    {
                        ApplyTheme(found, persist: false);
                        return;
                    }
                }
            }
        }
        catch
        {
            // Fallback to default
        }

        // Apply default if no saved theme
        ApplyTheme(_themes[0], persist: false);
    }

    private void SaveThemePreference(string themeId)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(new { themeId, updatedAt = DateTime.UtcNow });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignore settings write errors in restricted environments
        }
    }

    /// <summary>
    /// Applies color tokens into Application.Current.Resources dynamically.
    /// Controls referencing {DynamicResource BgPrimary}, etc. update in real time.
    /// </summary>
    public static void UpdateApplicationResources(ThemeColors c)
    {
        if (Application.Current == null) return;

        var res = Application.Current.Resources;

        void SetBrush(string key, string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                res[key] = brush;
            }
            catch
            {
                // Ignore invalid hex in custom tokens
            }
        }

        // Primary Surface Brushes
        SetBrush("BgPrimary", c.BgPrimary);
        SetBrush("BgSecondary", c.BgSecondary);
        SetBrush("BgTertiary", c.BgTertiary);
        SetBrush("BgHover", c.BgHover);
        SetBrush("BgActive", c.BgActive);

        // Foregrounds
        SetBrush("FgPrimary", c.FgPrimary);
        SetBrush("FgSecondary", c.FgSecondary);
        SetBrush("FgMuted", c.FgMuted);
        SetBrush("FgAccent", c.FgAccent);

        // Borders
        SetBrush("BorderDark", c.BorderDark);
        SetBrush("BorderLight", c.BorderLight);

        // Accents
        SetBrush("AccentBlue", c.Accent);
        SetBrush("AccentBlueHover", c.AccentHover);
        SetBrush("AccentMuted", c.AccentMuted);

        // Editor Specific
        SetBrush("GutterBg", c.GutterBg);
        SetBrush("LineNumberFg", c.LineNumberFg);
        SetBrush("CaretBrush", c.Caret);
        SetBrush("SelectionBrush", c.Selection);
        SetBrush("CurrentLineBrush", c.CurrentLine);
        SetBrush("BracketMatchBrush", c.BracketMatch);

        // Docks & Status Bars
        SetBrush("StatusBarBg", c.StatusBarBg);
        SetBrush("StatusBarFg", c.StatusBarFg);
        SetBrush("ActivityBarBg", c.ActivityBarBg);
        SetBrush("ActivityBarFg", c.ActivityBarFg);
        SetBrush("ActivityBarSelected", c.ActivityBarSelected);

        // Terminal & Console
        SetBrush("TerminalBg", c.TerminalBg);
        SetBrush("TerminalFg", c.TerminalFg);

        // Custom extension tokens
        if (c.CustomTokens != null)
        {
            foreach (var (key, hex) in c.CustomTokens)
            {
                SetBrush(key, hex);
            }
        }
    }

    private sealed class BuiltInTheme : IThemeDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string? Description { get; }
        public string? Author { get; }
        public ThemeType Type { get; }
        public ThemeColors Colors { get; }

        public BuiltInTheme(string id, string displayName, ThemeType type, string description, string author, ThemeColors colors)
        {
            Id = id;
            DisplayName = displayName;
            Type = type;
            Description = description;
            Author = author;
            Colors = colors;
        }
    }
}

