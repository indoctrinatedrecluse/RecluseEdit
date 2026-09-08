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

    Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default);
    Task DeinitializeAsync(CancellationToken cancellationToken = default);
}

