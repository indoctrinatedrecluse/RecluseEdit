using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Defines a visual color theme for RecluseEdit.
/// Third-party extensions can implement this interface to contribute custom themes.
/// </summary>
public interface IThemeDefinition
{
    /// <summary>
    /// Unique identifier for the theme (e.g., "recluse.dark", "ext.cyberpunk").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-friendly display name shown in the Theme Picker.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Optional short description explaining the style or source.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Author or publisher of the theme.
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Theme classification (Dark, Light, HighContrast, Creative).
    /// </summary>
    ThemeType Type { get; }

    /// <summary>
    /// Complete color palette tokens.
    /// </summary>
    ThemeColors Colors { get; }
}

