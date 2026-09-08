namespace RecluseEdit.Extensions;

/// <summary>
/// Base contract that all RecluseEdit extensions must implement.
/// </summary>
public interface IExtension
{
    /// <summary>
    /// Unique identifier for the extension (e.g. "recluse.webdev").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Friendly display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Semantic version of the extension (e.g. "1.0.0").
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Brief description of the capabilities provided.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Author or organization name.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Called when the extension is loaded and activated.
    /// </summary>
    void Initialize(IExtensionContext context);

    /// <summary>
    /// Called when the extension is unloaded or editor shuts down.
    /// </summary>
    void Deinitialize();
}

