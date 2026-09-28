namespace RecluseEdit.Sdk;

/// <summary>
/// Root contract implemented by RecluseEdit extensions.
/// </summary>
public interface IExtension
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }

    /// <summary>
    /// When <c>true</c> (default), this extension provides language or syntax support
    /// and is always loaded — even during the trial period.
    /// When <c>false</c>, the extension is a premium feature that requires a paid license
    /// to load.  Override this to <c>false</c> in any non-language-support extension.
    /// </summary>
    bool IsLanguageSupport => true;

    Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default);
    Task DeinitializeAsync(CancellationToken cancellationToken = default);
}

