using System.IO;
using System.Reflection;
using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.BuiltIn;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages discovery, loading, and lifecycle of editor extensions using RecluseEdit.Sdk.
/// </summary>
public class ExtensionManager : IExtensionHost
{
    private readonly SyntaxManager _syntaxManager;
    private readonly AutocompleteManager _autocompleteManager;
    private readonly ToolchainManager _toolchainManager;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ThemeManager? _themeManager;
    private readonly List<IExtension> _loadedExtensions = [];
    private readonly List<ISidePanelProvider> _sidePanels = [];
    private readonly List<IDocumentFormatter> _formatters = [];
    private readonly List<IThemeDefinition> _extensionThemes = [];
    private readonly List<string> _logs = [];

    public IReadOnlyList<IExtension> LoadedExtensions => _loadedExtensions.AsReadOnly();
    public IReadOnlyList<ISidePanelProvider> RegisteredSidePanels => _sidePanels.AsReadOnly();
    public IReadOnlyList<IDocumentFormatter> RegisteredFormatters => _formatters.AsReadOnly();
    public IReadOnlyList<IThemeDefinition> RegisteredThemes => _themeManager?.RegisteredThemes ?? _extensionThemes.AsReadOnly();
    public IWorkspaceContext WorkspaceContext => _workspaceContext;
    public IReadOnlyList<string> Logs => _logs.AsReadOnly();
    public ToolchainManager ToolchainManager => _toolchainManager;

    public event Action? ExtensionsChanged;
    public event Action<ISidePanelProvider>? SidePanelRegistered;

    public ExtensionManager(
        SyntaxManager syntaxManager,
        AutocompleteManager autocompleteManager,
        ToolchainManager toolchainManager,
        IWorkspaceContext? workspaceContext = null,
        ThemeManager? themeManager = null)
    {
        _syntaxManager = syntaxManager;
        _autocompleteManager = autocompleteManager;
        _toolchainManager = toolchainManager;
        _workspaceContext = workspaceContext ?? new WorkspaceContext(new WorkspaceManager(), new DocumentManager(syntaxManager));
        _themeManager = themeManager;
    }

    public async Task InitializeAsync()
    {
        // 1. Load Built-In Extensions
        await LoadExtensionAsync(new WebDevExtension());

        // 2. Discover and load external plugins from extensions directory
        await LoadExternalExtensionsAsync();

        ExtensionsChanged?.Invoke();
    }

    public async Task LoadExtensionAsync(IExtension extension)
    {
        try
        {
            if (_loadedExtensions.Any(e => e.Id == extension.Id))
            {
                Log($"Extension '{extension.Id}' is already loaded.");
                return;
            }

            await extension.InitializeAsync(this);
            _loadedExtensions.Add(extension);
            Log($"Loaded extension: {extension.Name} v{extension.Version} by {extension.Author}");
            ExtensionsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Log($"Failed to initialize extension '{extension.Name}': {ex.Message}");
        }
    }

    private async Task LoadExternalExtensionsAsync()
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
                return;
            }
        }

        var dlls = Directory.GetFiles(extDir, "*.dll", SearchOption.AllDirectories);
        foreach (var dll in dlls)
        {
            var fileName = Path.GetFileName(dll);
            // Skip known SDK / framework assemblies if copied
            if (fileName.Equals("RecluseEdit.Sdk.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("ICSharpCode.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var extensionTypes = assembly.GetTypes()
                    .Where(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var type in extensionTypes)
                {
                    if (Activator.CreateInstance(type) is IExtension ext)
                    {
                        await LoadExtensionAsync(ext);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading extension assembly '{fileName}': {ex.Message}");
            }
        }
    }

    #region IExtensionHost Implementation

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

    public void RegisterIntelliSense(IIntelliSenseProvider provider)
    {
        _autocompleteManager.RegisterIntelliSenseProvider(provider);
        Log($"Registered IntelliSense provider '{provider.Name}'");
    }

    public void RegisterToolchainCheck(IToolchainCheck toolchainCheck)
    {
        _toolchainManager.RegisterCheck(toolchainCheck);
        Log($"Registered toolchain check '{toolchainCheck.ToolName}' ({toolchainCheck.Command})");
    }

    public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition)
    {
        _syntaxManager.RegisterSyntaxDefinition(languageId, definition);
        Log($"Registered syntax highlighting for language '{languageId}'");
    }

    public void RegisterSidePanel(ISidePanelProvider panelProvider)
    {
        if (!_sidePanels.Any(p => p.Id == panelProvider.Id))
        {
            _sidePanels.Add(panelProvider);
            Log($"Registered side panel '{panelProvider.Title}' ({panelProvider.Id})");
            SidePanelRegistered?.Invoke(panelProvider);
        }
    }

    public void RegisterDocumentFormatter(IDocumentFormatter formatter)
    {
        if (!_formatters.Any(f => f.FormatterId == formatter.FormatterId))
        {
            _formatters.Add(formatter);
            Log($"Registered document formatter '{formatter.DisplayName}' ({formatter.FormatterId})");
        }
    }

    public void RegisterTheme(IThemeDefinition theme)
    {
        if (!_extensionThemes.Any(t => t.Id == theme.Id))
        {
            _extensionThemes.Add(theme);
            _themeManager?.RegisterTheme(theme);
            Log($"Registered theme '{theme.DisplayName}' ({theme.Id})");
        }
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
