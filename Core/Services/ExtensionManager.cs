using System.IO;
using System.Reflection;
using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Core.Models;
using RecluseEdit.Extensions;
using RecluseEdit.Extensions.BuiltIn;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages discovery, loading, and lifecycle of editor extensions.
/// </summary>
public class ExtensionManager : IExtensionContext
{
    private readonly SyntaxManager _syntaxManager;
    private readonly AutocompleteManager _autocompleteManager;
    private readonly List<IExtension> _loadedExtensions = [];
    private readonly List<string> _logs = [];

    public IReadOnlyList<IExtension> LoadedExtensions => _loadedExtensions.AsReadOnly();
    public IReadOnlyList<string> Logs => _logs.AsReadOnly();

    public event Action? ExtensionsChanged;

    public ExtensionManager(SyntaxManager syntaxManager, AutocompleteManager autocompleteManager)
    {
        _syntaxManager = syntaxManager;
        _autocompleteManager = autocompleteManager;
    }

    public void Initialize()
    {
        // 1. Load Built-In Extensions
        LoadExtension(new WebDevExtension());

        // 2. Discover and load external plugins from extensions directory
        LoadExternalExtensions();

        ExtensionsChanged?.Invoke();
    }

    public void LoadExtension(IExtension extension)
    {
        try
        {
            if (_loadedExtensions.Any(e => e.Id == extension.Id))
            {
                Log($"Extension '{extension.Id}' is already loaded.");
                return;
            }

            extension.Initialize(this);
            _loadedExtensions.Add(extension);
            Log($"Loaded extension: {extension.Name} v{extension.Version} by {extension.Author}");
        }
        catch (Exception ex)
        {
            Log($"Failed to initialize extension '{extension.Name}': {ex.Message}");
        }
    }

    private void LoadExternalExtensions()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var extDir = Path.Combine(appDir, "Extensions");

        if (!Directory.Exists(extDir))
        {
            try
            {
                Directory.CreateDirectory(extDir);
            }
            catch
            {
                // Ignore if unable to create directory
                return;
            }
        }

        var dlls = Directory.GetFiles(extDir, "*.dll", SearchOption.AllDirectories);
        foreach (var dll in dlls)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var extensionTypes = assembly.GetTypes()
                    .Where(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in extensionTypes)
                {
                    if (Activator.CreateInstance(type) is IExtension ext)
                    {
                        LoadExtension(ext);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading extension assembly '{dll}': {ex.Message}");
            }
        }
    }

    #region IExtensionContext Implementation

    public void RegisterLanguage(LanguageDefinition language)
    {
        _syntaxManager.RegisterLanguage(language);
        Log($"Registered language '{language.DisplayName}' ({language.Id})");
    }

    public void RegisterInlineCompletion(IInlineCompletionProvider provider)
    {
        _autocompleteManager.RegisterProvider(provider);
        Log($"Registered inline completion provider '{provider.Name}'");
    }

    public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition)
    {
        _syntaxManager.RegisterSyntaxDefinition(languageId, definition);
        Log($"Registered syntax highlighting for language '{languageId}'");
    }

    public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages()
    {
        return _syntaxManager.SupportedLanguages;
    }

    public void Log(string message)
    {
        _logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    #endregion
}

