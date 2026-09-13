using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Themes;

/// <summary>
/// Sample third-party theme demonstrating Theme API inheritance via ThemeDefinitionBase.
/// </summary>
public class VueEmeraldTheme : ThemeDefinitionBase
{
    public override string Id => "frontend.vue-emerald";
    public override string DisplayName => "Vue Emerald";
    public override string? Description => "Deep emerald dark theme inspired by Vue.js mint and forest tones.";
    public override string? Author => "Vue Community";
    public override ThemeType Type => ThemeType.Dark;

    public override ThemeColors Colors => new()
    {
        BgPrimary = "#0B1B14",
        BgSecondary = "#06130D",
        BgTertiary = "#12281E",
        BgHover = "#1A382B",
        BgActive = "#42B883",
        FgPrimary = "#E8FAF0",
        FgSecondary = "#8CD3B0",
        FgMuted = "#4D7864",
        FgAccent = "#35495E",
        BorderDark = "#122A1E",
        BorderLight = "#1F4532",
        Accent = "#42B883",
        AccentHover = "#33A06F",
        AccentMuted = "#0F2E20",
        GutterBg = "#0B1B14",
        LineNumberFg = "#4D7864",
        Caret = "#42B883",
        Selection = "#1A382B",
        CurrentLine = "#10261C",
        BracketMatch = "#42B883",
        StatusBarBg = "#06130D",
        StatusBarFg = "#42B883",
        ActivityBarBg = "#06130D",
        ActivityBarFg = "#8CD3B0",
        ActivityBarSelected = "#42B883",
        TerminalBg = "#06130D",
        TerminalFg = "#E8FAF0",
        MarkdownBg = "#0B1B14",
        MarkdownFg = "#E8FAF0",
        MarkdownCodeBg = "#06130D"
    };
}

