using System.Collections.Generic;

namespace RecluseEdit.Sdk.Models;

/// <summary>
/// Palette of color tokens representing the visual styling of RecluseEdit.
/// Colors are specified in standard hex string format ("#RRGGBB" or "#AARRGGBB").
/// </summary>
public class ThemeColors
{
    // --- Core Surfaces ---
    public string BgPrimary { get; set; } = "#1E1E1E";
    public string BgSecondary { get; set; } = "#252526";
    public string BgTertiary { get; set; } = "#2D2D30";
    public string BgHover { get; set; } = "#3E3E42";
    public string BgActive { get; set; } = "#007ACC";

    // --- Text & Foregrounds ---
    public string FgPrimary { get; set; } = "#D4D4D4";
    public string FgSecondary { get; set; } = "#969696";
    public string FgMuted { get; set; } = "#6E6E6E";
    public string FgAccent { get; set; } = "#FFFFFF";

    // --- Borders & Separators ---
    public string BorderDark { get; set; } = "#333337";
    public string BorderLight { get; set; } = "#3F3F46";

    // --- Accent Colors ---
    public string Accent { get; set; } = "#007ACC";
    public string AccentHover { get; set; } = "#1C97EA";
    public string AccentMuted { get; set; } = "#1A3A5C";

    // --- Editor Surface & Gutter ---
    public string GutterBg { get; set; } = "#1E1E1E";
    public string LineNumberFg { get; set; } = "#858585";
    public string Caret { get; set; } = "#AEAFAD";
    public string Selection { get; set; } = "#264F78";
    public string CurrentLine { get; set; } = "#282828";
    public string BracketMatch { get; set; } = "#007ACC";

    // --- Docks & Activity Bars ---
    public string StatusBarBg { get; set; } = "#007ACC";
    public string StatusBarFg { get; set; } = "#FFFFFF";
    public string ActivityBarBg { get; set; } = "#252526";
    public string ActivityBarFg { get; set; } = "#858585";
    public string ActivityBarSelected { get; set; } = "#FFFFFF";

    // --- Terminal & Console ---
    public string TerminalBg { get; set; } = "#181818";
    public string TerminalFg { get; set; } = "#CCCCCC";

    // --- Live Markdown Preview Tokens ---
    public string MarkdownBg { get; set; } = "#0D1117";
    public string MarkdownFg { get; set; } = "#C9D1D9";
    public string MarkdownCodeBg { get; set; } = "#161B22";

    // --- Arbitrary Custom Extension Tokens ---
    public Dictionary<string, string> CustomTokens { get; set; } = new();

    /// <summary>
    /// Safely gets a color token by key or returns fallback if not defined.
    /// </summary>
    public string GetToken(string key, string fallback = "#000000")
    {
        if (CustomTokens != null && CustomTokens.TryGetValue(key, out var val))
            return val;

        return key switch
        {
            nameof(BgPrimary) => BgPrimary,
            nameof(BgSecondary) => BgSecondary,
            nameof(BgTertiary) => BgTertiary,
            nameof(BgHover) => BgHover,
            nameof(BgActive) => BgActive,
            nameof(FgPrimary) => FgPrimary,
            nameof(FgSecondary) => FgSecondary,
            nameof(FgMuted) => FgMuted,
            nameof(FgAccent) => FgAccent,
            nameof(BorderDark) => BorderDark,
            nameof(BorderLight) => BorderLight,
            nameof(Accent) => Accent,
            nameof(AccentHover) => AccentHover,
            nameof(AccentMuted) => AccentMuted,
            nameof(GutterBg) => GutterBg,
            nameof(LineNumberFg) => LineNumberFg,
            nameof(Caret) => Caret,
            nameof(Selection) => Selection,
            nameof(CurrentLine) => CurrentLine,
            nameof(BracketMatch) => BracketMatch,
            nameof(StatusBarBg) => StatusBarBg,
            nameof(StatusBarFg) => StatusBarFg,
            nameof(ActivityBarBg) => ActivityBarBg,
            nameof(ActivityBarFg) => ActivityBarFg,
            nameof(ActivityBarSelected) => ActivityBarSelected,
            nameof(TerminalBg) => TerminalBg,
            nameof(TerminalFg) => TerminalFg,
            nameof(MarkdownBg) => MarkdownBg,
            nameof(MarkdownFg) => MarkdownFg,
            nameof(MarkdownCodeBg) => MarkdownCodeBg,
            _ => fallback
        };
    }
}

