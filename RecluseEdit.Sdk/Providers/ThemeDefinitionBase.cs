using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Convenient abstract base class for implementing custom themes in RecluseEdit extensions.
/// </summary>
public abstract class ThemeDefinitionBase : IThemeDefinition
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public virtual string? Description => null;
    public virtual string? Author => "Community";
    public abstract ThemeType Type { get; }
    public abstract ThemeColors Colors { get; }
}

