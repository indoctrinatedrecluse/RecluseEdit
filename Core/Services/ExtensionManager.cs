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
    private readonly List<IStatusBarProvider> _statusBarProviders = [];
    private readonly List<IStatusBarItem> _statusBarItems = [];
    private readonly List<IAiProvider> _aiProviders = [];
    private readonly List<string> _logs = [];

    // Track which extension owns each registration, for cleanup on unload
    private IExtension? _activeExtension;
    private readonly Dictionary<string, List<object>> _registrationsByExtension = new();

    /// <summary>
    /// Returns <c>true</c> when all extensions (including non-language ones) are unlocked.
    /// Defaults to <c>() => true</c> so tests and design-time hosts are unaffected.
    /// </summary>
    private readonly Func<bool> _allExtensionsUnlocked;

    public IReadOnlyList<IExtension> LoadedExtensions => _loadedExtensions.AsReadOnly();
    public IReadOnlyList<ISidePanelProvider> RegisteredSidePanels => _sidePanels.AsReadOnly();
    public IReadOnlyList<IDocumentFormatter> RegisteredFormatters => _formatters.AsReadOnly();
    public IReadOnlyList<IThemeDefinition> RegisteredThemes => _themeManager?.RegisteredThemes ?? _extensionThemes.AsReadOnly();
    public IReadOnlyList<IStatusBarProvider> RegisteredStatusBarProviders => _statusBarProviders.AsReadOnly();
    public IReadOnlyList<IStatusBarItem> RegisteredStatusBarItems => _statusBarItems.AsReadOnly();
    public IReadOnlyList<IAiProvider> RegisteredAiProviders => _aiProviders.AsReadOnly();
    public IWorkspaceContext WorkspaceContext => _workspaceContext;
    public IReadOnlyList<string> Logs => _logs.AsReadOnly();
    public ToolchainManager ToolchainManager => _toolchainManager;

    private readonly Func<bool> _isPaidLicense;
    public bool IsPaidLicense => _isPaidLicense();

    public event Action? ExtensionsChanged;
    public event Action<ISidePanelProvider>? SidePanelRegistered;
    public event Action<string>? SidePanelRequested;
    public event Action<IStatusBarItem>? StatusBarItemRegistered;
    public event Action<IStatusBarProvider>? StatusBarProviderRegistered;
    public event Action<IAiProvider>? AiProviderRegistered;

    public ExtensionManager(
        SyntaxManager syntaxManager,
        AutocompleteManager autocompleteManager,
        ToolchainManager toolchainManager,
        IWorkspaceContext? workspaceContext = null,
        ThemeManager? themeManager = null,
        Func<bool>? allExtensionsUnlocked = null,
        Func<bool>? isPaidLicense = null)
    {
        _syntaxManager = syntaxManager;
        _autocompleteManager = autocompleteManager;
        _toolchainManager = toolchainManager;
        _workspaceContext = workspaceContext ?? new WorkspaceContext(new WorkspaceManager(), new DocumentManager(syntaxManager));
        _themeManager = themeManager;
        _allExtensionsUnlocked = allExtensionsUnlocked ?? (() => true);
        _isPaidLicense = isPaidLicense ?? (() => true);
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

            // ── License gating ──────────────────────────────────────────────
            // Non-language extensions require a paid license. On trial, they are
            // skipped silently so the app still starts cleanly.
            if (!extension.IsLanguageSupport && !_allExtensionsUnlocked())
            {
                Log($"Extension '{extension.Name}' requires a paid license and will not load during trial.");
                return;
            }

            _activeExtension = extension;
            try
            {
                await extension.InitializeAsync(this);
            }
            finally
            {
                _activeExtension = null;
            }
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
            TrackRegistration(_activeExtension, panelProvider);
            Log($"Registered side panel '{panelProvider.Title}' ({panelProvider.Id})");
            SidePanelRegistered?.Invoke(panelProvider);
        }
    }

    public void ShowSidePanel(string panelId)
    {
        Log($"Extension requested side panel '{panelId}'");
        SidePanelRequested?.Invoke(panelId);
    }

    public void RegisterDocumentFormatter(IDocumentFormatter formatter)
    {
        if (!_formatters.Any(f => f.FormatterId == formatter.FormatterId))
        {
            _formatters.Add(formatter);
            TrackRegistration(_activeExtension, formatter);
            Log($"Registered document formatter '{formatter.DisplayName}' ({formatter.FormatterId})");
        }
    }

    public void RegisterTheme(IThemeDefinition theme)
    {
        if (!_extensionThemes.Any(t => t.Id == theme.Id))
        {
            _extensionThemes.Add(theme);
            TrackRegistration(_activeExtension, theme);
            _themeManager?.RegisterTheme(theme);
            Log($"Registered theme '{theme.DisplayName}' ({theme.Id})");
        }
    }

    public void RegisterStatusBarItem(IStatusBarItem item)
    {
        if (!_statusBarItems.Any(i => i.Id == item.Id))
        {
            _statusBarItems.Add(item);
            TrackRegistration(_activeExtension, item);
            Log($"Registered status bar item '{item.Text}' ({item.Id})");
            StatusBarItemRegistered?.Invoke(item);
        }
    }

    public void RegisterStatusBarProvider(IStatusBarProvider provider)
    {
        if (!_statusBarProviders.Any(p => p.Id == provider.Id))
        {
            _statusBarProviders.Add(provider);
            TrackRegistration(_activeExtension, provider);
            Log($"Registered status bar provider ({provider.Id})");
            StatusBarProviderRegistered?.Invoke(provider);

            foreach (var item in provider.GetItems())
            {
                RegisterStatusBarItem(item);
            }

            provider.ItemsChanged += (_, _) =>
            {
                foreach (var item in provider.GetItems())
                {
                    RegisterStatusBarItem(item);
                }
            };
        }
    }

    public void RegisterAiProvider(IAiProvider provider)
    {
        if (!_aiProviders.Any(p => p.Id == provider.Id))
        {
            _aiProviders.Add(provider);
            TrackRegistration(_activeExtension, provider);
            Log($"Registered AI provider '{provider.DisplayName}' ({provider.Id})");
            AiProviderRegistered?.Invoke(provider);
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

    /// <summary>
    /// Unloads all currently-loaded premium (non-language-support) extensions,
    /// calling DeinitializeAsync and removing their registered contributions.
    /// </summary>
    public async Task UnloadPremiumExtensionsAsync()
    {
        var premium = _loadedExtensions.Where(e => !e.IsLanguageSupport).ToList();
        if (premium.Count == 0) return;

        foreach (var ext in premium)
        {
            try
            {
                await ext.DeinitializeAsync();
                Log($"Unloaded premium extension: {ext.Name}");
            }
            catch (Exception ex)
            {
                Log($"Error unloading extension '{ext.Name}': {ex.Message}");
            }

            _loadedExtensions.Remove(ext);

            // Remove all registrations owned by this extension
            if (_registrationsByExtension.TryGetValue(ext.Id, out var regs))
            {
                foreach (var reg in regs)
                {
                    if (reg is ISidePanelProvider sp) _sidePanels.RemoveAll(p => ReferenceEquals(p, sp));
                    else if (reg is IAiProvider ap) _aiProviders.RemoveAll(p => ReferenceEquals(p, ap));
                    else if (reg is IStatusBarItem si) _statusBarItems.RemoveAll(i => ReferenceEquals(i, si));
                    else if (reg is IStatusBarProvider ssp) _statusBarProviders.RemoveAll(p => ReferenceEquals(p, ssp));
                    else if (reg is IDocumentFormatter df) _formatters.RemoveAll(f => ReferenceEquals(f, df));
                    else if (reg is IThemeDefinition td)
                    {
                        _extensionThemes.RemoveAll(t => ReferenceEquals(t, td));
                        _themeManager?.UnregisterTheme(td.Id);
                    }
                }
                _registrationsByExtension.Remove(ext.Id);
            }
        }

        ExtensionsChanged?.Invoke();
    }

    /// <summary>
    /// Reloads all external extensions. Unloads premium extensions first (if any),
    /// then re-discovers from disk. This allows premium extensions to be loaded
    /// immediately when a license is activated, or unloaded when reverted to trial.
    /// </summary>
    public async Task ReloadExtensionsAsync()
    {
        await UnloadPremiumExtensionsAsync();
        await LoadExternalExtensionsAsync();
        ExtensionsChanged?.Invoke();
    }

    private void TrackRegistration(IExtension? extension, object registration)
    {
        if (extension is null) return;
        if (!_registrationsByExtension.TryGetValue(extension.Id, out var list))
        {
            list = new List<object>();
            _registrationsByExtension[extension.Id] = list;
        }
        list.Add(registration);
    }

    #endregion
}
