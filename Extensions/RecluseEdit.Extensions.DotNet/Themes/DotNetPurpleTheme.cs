using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.DotNet.Themes;

/// <summary>
/// Dark theme featuring Microsoft .NET purple and indigo brand accents (#512BD4).
/// </summary>
public class DotNetPurpleTheme : ThemeDefinitionBase
{
    public override string Id => "dotnet.purple-dark";
    public override string DisplayName => ".NET Purple Dark";
    public override string? Description => "Deep modern dark theme with Microsoft .NET purple and indigo accents.";
    public override string? Author => "indoctrinatedrecluse";
    public override ThemeType Type => ThemeType.Dark;

    public override ThemeColors Colors => new()
    {
        BgPrimary = "#12101F",
        BgSecondary = "#0B0914",
        BgTertiary = "#1A172D",
        BgHover = "#282346",
        BgActive = "#512BD4",
        FgPrimary = "#F3F0FF",
        FgSecondary = "#B8A7EA",
        FgMuted = "#6B628A",
        FgAccent = "#8A63D2",
        BorderDark = "#1F1B38",
        BorderLight = "#312B58",
        Accent = "#7E57C2",
        AccentHover = "#673AB7",
        AccentMuted = "#2B1E5E",
        GutterBg = "#12101F",
        LineNumberFg = "#6B628A",
        Caret = "#B388FF",
        Selection = "#312368",
        CurrentLine = "#1B1731",
        BracketMatch = "#B388FF",
        StatusBarBg = "#0B0914",
        StatusBarFg = "#B8A7EA",
        ActivityBarBg = "#0B0914",
        ActivityBarFg = "#B8A7EA",
        ActivityBarSelected = "#7E57C2",
        TerminalBg = "#0B0914",
        TerminalFg = "#F3F0FF",
        MarkdownBg = "#12101F",
        MarkdownFg = "#F3F0FF",
        MarkdownCodeBg = "#0B0914"
    };
}
