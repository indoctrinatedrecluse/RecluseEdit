using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.UI.Views;

namespace RecluseEdit;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static readonly RoutedUICommand NewFileCommand = new("New File", "NewFile", typeof(MainWindow));
    public static readonly RoutedUICommand OpenFileCommand = new("Open File", "OpenFile", typeof(MainWindow));
    public static readonly RoutedUICommand OpenFolderCommand = new("Open Folder", "OpenFolder", typeof(MainWindow));
    public static readonly RoutedUICommand SaveFileCommand = new("Save File", "SaveFile", typeof(MainWindow));
    public static readonly RoutedUICommand SaveAsFileCommand = new("Save As File", "SaveAsFile", typeof(MainWindow));
    public static readonly RoutedUICommand CloseFileCommand = new("Close File", "CloseFile", typeof(MainWindow));
    public static readonly RoutedUICommand ToggleSidebarCommand = new("Toggle Sidebar", "ToggleSidebar", typeof(MainWindow));
    public static readonly RoutedUICommand ToggleAiChatCommand = new("Toggle AI Chat", "ToggleAiChat", typeof(MainWindow));
    public static readonly RoutedUICommand ToggleTerminalCommand = new("Toggle Terminal", "ToggleTerminal", typeof(MainWindow));
    public static readonly RoutedUICommand FindCommand = new("Find", "Find", typeof(MainWindow));
    public static readonly RoutedUICommand ReplaceCommand = new("Replace", "Replace", typeof(MainWindow));

    private readonly SyntaxManager _syntaxManager;
    private readonly AutocompleteManager _autocompleteManager;
    private readonly ToolchainManager _toolchainManager;
    private readonly ExtensionManager _extensionManager;
    private readonly DocumentManager _documentManager;
    private readonly WorkspaceManager _workspaceManager;

    private GridLength _lastSidebarWidth = new(240);
    private GridLength _lastRightPaneWidth = new(380);
    private GridLength _lastBottomPaneHeight = new(220);

    public MainWindow()
    {
        InitializeComponent();

        _syntaxManager = new SyntaxManager();
        _autocompleteManager = new AutocompleteManager();
        _toolchainManager = new ToolchainManager();
        _workspaceManager = new WorkspaceManager();
        _documentManager = new DocumentManager(_syntaxManager);
        var workspaceContext = new WorkspaceContext(_workspaceManager, _documentManager);
        _extensionManager = new ExtensionManager(_syntaxManager, _autocompleteManager, _toolchainManager, workspaceContext);

        EditorHost.SyntaxManager = _syntaxManager;
        EditorHost.AutocompleteManager = _autocompleteManager;

        // Command bindings
        CommandBindings.Add(new CommandBinding(NewFileCommand, (_, _) => CreateNewFile()));
        CommandBindings.Add(new CommandBinding(OpenFileCommand, (_, _) => OpenFileDialog()));
        CommandBindings.Add(new CommandBinding(OpenFolderCommand, (_, _) => OpenFolderDialog()));
        CommandBindings.Add(new CommandBinding(SaveFileCommand, (_, _) => SaveActiveFile()));
        CommandBindings.Add(new CommandBinding(SaveAsFileCommand, (_, _) => SaveActiveFileAs()));
        CommandBindings.Add(new CommandBinding(CloseFileCommand, (_, _) => CloseActiveFile()));
        CommandBindings.Add(new CommandBinding(ToggleSidebarCommand, (_, _) => ToggleSidebar()));
        CommandBindings.Add(new CommandBinding(ToggleAiChatCommand, (_, _) => ToggleRightPane()));
        CommandBindings.Add(new CommandBinding(ToggleTerminalCommand, (_, _) => ToggleTerminal()));
        CommandBindings.Add(new CommandBinding(FindCommand, (_, _) => EditorHost.OpenFind()));
        CommandBindings.Add(new CommandBinding(ReplaceCommand, (_, _) => EditorHost.OpenReplace()));

        TerminalPane.ClosePaneRequested += () => ToggleTerminal(false);

        // Document events
        _documentManager.ActiveDocumentChanged += OnActiveDocumentChanged;
        _documentManager.DocumentClosed += OnDocumentClosed;

        // Workspace events
        _workspaceManager.WorkspaceChanged += OnWorkspaceChanged;

        TabItemsControl.ItemsSource = _documentManager.Documents;
        CmbLanguage.ItemsSource = _syntaxManager.SupportedLanguages;

        // Initialize extensions and side panels
        _extensionManager.SidePanelRegistered += OnSidePanelRegistered;
        foreach (var panel in _extensionManager.RegisteredSidePanels)
        {
            OnSidePanelRegistered(panel);
        }

        _ = _extensionManager.InitializeAsync();
        _extensionManager.ExtensionsChanged += UpdateExtensionsStatus;
        _toolchainManager.ToolchainStatusChanged += UpdateExtensionsStatus;
        UpdateExtensionsStatus();

        // Create default initial document
        CreateNewFile();
    }

    private void UpdateExtensionsStatus()
    {
        Dispatcher.Invoke(() =>
        {
            var extCount = _extensionManager.LoadedExtensions.Count;
            if (_toolchainManager.HasIssues)
            {
                StatusExtensions.Text = $"Extensions: {extCount} (⚠️ Toolchains)";
                StatusExtensions.ToolTip = "One or more compiler/toolchain prerequisites require attention. Click to inspect.";
            }
            else
            {
                StatusExtensions.Text = $"Extensions: {extCount}";
                StatusExtensions.ToolTip = "Click to manage extensions and compilers";
            }
        });
    }

    #region Workspace Explorer Handling

    private void OpenFolderDialog()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select Workspace Folder"
        };

        if (dlg.ShowDialog(this) == true)
        {
            _workspaceManager.OpenWorkspace(dlg.FolderName);
            StatusMessage.Text = $"Opened workspace: {Path.GetFileName(dlg.FolderName)}";
        }
    }

    private void OnWorkspaceChanged()
    {
        if (_workspaceManager.HasWorkspace && _workspaceManager.RootItem != null)
        {
            TxtWorkspaceName.Text = Path.GetFileName(_workspaceManager.RootPath)?.ToUpperInvariant() ?? "EXPLORER";
            WorkspaceTreeView.ItemsSource = _workspaceManager.RootItem.Children;
            TerminalPane.SetWorkingDirectory(_workspaceManager.RootPath);

            // Ensure sidebar is visible
            if (ColSidebar.Width.Value == 0)
            {
                ToggleSidebar();
            }
        }
        else
        {
            TxtWorkspaceName.Text = "EXPLORER";
            WorkspaceTreeView.ItemsSource = null;
        }
    }

    private void OnRefreshWorkspaceClick(object sender, RoutedEventArgs e)
    {
        _workspaceManager.RefreshWorkspace();
        StatusMessage.Text = "Workspace refreshed";
    }

    private void OnWorkspaceTreeDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (WorkspaceTreeView.SelectedItem is FileSystemItem item && !item.IsDirectory)
        {
            try
            {
                var doc = _documentManager.OpenDocument(item.FullPath);
                StatusMessage.Text = $"Opened {doc.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open file:\n{ex.Message}", "RecluseEdit", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnWorkspaceItemSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // Highlight status or file preview if needed
    }

    private void ToggleSidebar()
    {
        if (ColSidebar.Width.Value > 0)
        {
            _lastSidebarWidth = ColSidebar.Width;
            ColSidebar.MinWidth = 0;
            ColSidebar.Width = new GridLength(0);
            Splitter.Visibility = Visibility.Collapsed;
            MenuSidebar.IsChecked = false;
        }
        else
        {
            ColSidebar.MinWidth = 140;
            ColSidebar.Width = _lastSidebarWidth.Value > 50 ? _lastSidebarWidth : new GridLength(240);
            Splitter.Visibility = Visibility.Visible;
            MenuSidebar.IsChecked = true;
        }
    }

    private void OnToggleSidebarClick(object sender, RoutedEventArgs e) => ToggleSidebar();

    public void ToggleRightPane()
    {
        if (ColRightPane.Width.Value > 0)
        {
            _lastRightPaneWidth = ColRightPane.Width;
            ColRightPane.MinWidth = 0;
            ColRightPane.Width = new GridLength(0);
            RightSplitter.Visibility = Visibility.Collapsed;
            RightPaneBorder.Visibility = Visibility.Collapsed;
            MenuRightPane.IsChecked = false;
            BtnToggleAiChat.IsChecked = false;
        }
        else
        {
            ColRightPane.MinWidth = 260;
            ColRightPane.Width = _lastRightPaneWidth.Value > 100 ? _lastRightPaneWidth : new GridLength(380);
            RightSplitter.Visibility = Visibility.Visible;
            RightPaneBorder.Visibility = Visibility.Visible;
            MenuRightPane.IsChecked = true;
            BtnToggleAiChat.IsChecked = true;
        }
    }

    private void OnToggleAiChatClick(object sender, RoutedEventArgs e) => ToggleRightPane();

    private void OnCloseRightPaneClick(object sender, RoutedEventArgs e)
    {
        if (ColRightPane.Width.Value > 0)
        {
            ToggleRightPane();
        }
    }

    public void ToggleTerminal(bool? forceState = null)
    {
        var shouldOpen = forceState ?? (RowTerminalPane.Height.Value == 0 || TerminalPane.Visibility == Visibility.Collapsed);

        if (!shouldOpen)
        {
            _lastBottomPaneHeight = RowTerminalPane.Height.Value > 50 ? RowTerminalPane.Height : _lastBottomPaneHeight;
            RowTerminalPane.MinHeight = 0;
            RowTerminalPane.Height = new GridLength(0);
            RowTerminalSplitter.Height = new GridLength(0);
            TerminalSplitter.Visibility = Visibility.Collapsed;
            TerminalPane.Visibility = Visibility.Collapsed;
            MenuTerminal.IsChecked = false;
            BtnToggleTerminal.IsChecked = false;
        }
        else
        {
            RowTerminalPane.MinHeight = 100;
            RowTerminalPane.Height = _lastBottomPaneHeight.Value > 80 ? _lastBottomPaneHeight : new GridLength(220);
            RowTerminalSplitter.Height = GridLength.Auto;
            TerminalSplitter.Visibility = Visibility.Visible;
            TerminalPane.Visibility = Visibility.Visible;
            MenuTerminal.IsChecked = true;
            BtnToggleTerminal.IsChecked = true;

            if (TerminalPane.TerminalCount == 0)
            {
                TerminalPane.CreateTerminal();
            }
            TerminalPane.FocusInput();
        }
    }

    private void OnToggleTerminalClick(object sender, RoutedEventArgs e) => ToggleTerminal();
    private void OnToolbarTerminalClick(object sender, RoutedEventArgs e) => ToggleTerminal();

    private void OnSidePanelRegistered(ISidePanelProvider panel)
    {
        Dispatcher.Invoke(() =>
        {
            TxtRightPaneTitle.Text = panel.Title.ToUpperInvariant();
            TxtRightPaneIcon.Text = panel.Icon ?? "🤖";
            RightPaneHost.Content = panel.CreateView(_extensionManager.WorkspaceContext);
        });
    }

    #endregion

    #region Document Lifecycle Actions

    private void CreateNewFile()
    {
        var doc = _documentManager.CreateNewDocument();
        StatusMessage.Text = $"Created {doc.FileName}";
    }

    private void OpenFileDialog()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "All Supported Files (*.html;*.css;*.js;*.ts;*.jsx;*.tsx;*.json;*.xml;*.xaml;*.cs;*.md;*.dart;*.php;*.rb;*.erb;*.py;*.jinja;*.blade.php;*.go;*.graphql;*.txt)|*.html;*.htm;*.xhtml;*.css;*.scss;*.less;*.js;*.mjs;*.cjs;*.ts;*.mts;*.cts;*.jsx;*.tsx;*.json;*.jsonc;*.xml;*.xaml;*.svg;*.config;*.csproj;*.props;*.targets;*.axaml;*.cs;*.md;*.markdown;*.dart;*.php;*.phtml;*.rb;*.rake;*.gemspec;*.ru;Gemfile;Rakefile;*.erb;*.html.erb;*.py;*.pyw;*.pyi;*.pyd;*.jinja;*.jinja2;*.j2;*.html.jinja;*.djhtml;*.blade.php;artisan;*.go;*.mod;*.work;go.mod;go.work;go.sum;*.gotmpl;*.gohtml;*.graphql;*.gql;*.txt;*.log|" +
                     "Go Source & Modules (*.go;*.mod;*.work;go.mod)|*.go;*.mod;*.work;go.mod;go.work;go.sum;*.gotmpl;*.gohtml|" +
                     "Laravel Blade Templates (*.blade.php)|*.blade.php|" +
                     "Python & Web Templates (*.py;*.pyw;*.jinja;*.jinja2)|*.py;*.pyw;*.pyi;*.pyd;*.jinja;*.jinja2;*.j2;*.html.jinja;*.djhtml|" +
                     "Web & Script Files (*.html;*.css;*.js;*.ts;*.jsx;*.tsx;*.json)|*.html;*.htm;*.xhtml;*.css;*.scss;*.less;*.js;*.mjs;*.cjs;*.ts;*.mts;*.cts;*.jsx;*.tsx;*.json;*.jsonc|" +
                     "React & GraphQL (*.jsx;*.tsx;*.graphql;*.gql)|*.jsx;*.tsx;*.graphql;*.gql|" +
                     "Angular Templates & Code (*.component.html;*.component.ts)|*.component.html;*.component.ts;*.service.ts;*.directive.ts;*.pipe.ts;*.guard.ts|" +
                     "Flutter & Dart (*.dart)|*.dart|" +
                     "PHP Scripts (*.php;*.phtml)|*.php;*.phtml;*.php3;*.php4;*.php5;*.php8|" +
                     "Ruby & Rails (*.rb;*.rake;*.erb)|*.rb;*.rake;*.gemspec;*.ru;Gemfile;Rakefile;*.erb;*.html.erb|" +
                     "C# & XAML (*.cs;*.xaml;*.xml)|*.cs;*.xaml;*.xml;*.csproj;*.props;*.targets;*.axaml;*.config|" +
                     "Markdown & Documents (*.md;*.txt)|*.md;*.markdown;*.txt;*.log|" +
                     "All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog(this) == true)
        {
            foreach (var file in dlg.FileNames)
            {
                try
                {
                    var doc = _documentManager.OpenDocument(file);
                    StatusMessage.Text = $"Opened {doc.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to open '{file}':\n{ex.Message}", "RecluseEdit", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private bool SaveActiveFile()
    {
        var doc = _documentManager.ActiveDocument;
        if (doc == null) return false;

        if (string.IsNullOrEmpty(doc.FilePath))
        {
            return SaveActiveFileAs();
        }

        try
        {
            _documentManager.SaveDocument(doc);
            StatusMessage.Text = $"Saved {doc.FileName}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to save '{doc.FileName}':\n{ex.Message}", "RecluseEdit", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool SaveActiveFileAs()
    {
        var doc = _documentManager.ActiveDocument;
        if (doc == null) return false;

        var dlg = new SaveFileDialog
        {
            FileName = doc.FileName.Replace(" *", ""),
            Filter = "Go Source Files (*.go)|*.go|" +
                     "Go Module Files (go.mod;go.work)|*.mod;*.work;go.mod;go.work|" +
                     "Laravel Blade Templates (*.blade.php)|*.blade.php|" +
                     "Python Files (*.py)|*.py|" +
                     "Jinja Templates (*.jinja;*.jinja2)|*.jinja;*.jinja2;*.j2;*.html.jinja;*.djhtml|" +
                     "HTML Files (*.html)|*.html|" +
                     "JavaScript Files (*.js)|*.js|" +
                     "TypeScript Files (*.ts)|*.ts|" +
                     "React JSX Files (*.jsx)|*.jsx|" +
                     "React TSX Files (*.tsx)|*.tsx|" +
                     "CSS Files (*.css)|*.css|" +
                     "SCSS Files (*.scss)|*.scss|" +
                     "JSON Files (*.json)|*.json|" +
                     "XML / XAML Files (*.xml;*.xaml)|*.xml;*.xaml|" +
                     "Markdown Files (*.md)|*.md|" +
                     "Dart Files (*.dart)|*.dart|" +
                     "PHP Files (*.php)|*.php|" +
                     "Ruby Files (*.rb)|*.rb|" +
                     "ERB Templates (*.erb)|*.erb|" +
                     "GraphQL Files (*.graphql)|*.graphql|" +
                     "C# Source Files (*.cs)|*.cs|" +
                     "Plain Text Files (*.txt)|*.txt|" +
                     "All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                _documentManager.SaveDocument(doc, dlg.FileName);
                StatusMessage.Text = $"Saved as {doc.FileName}";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to save file:\n{ex.Message}", "RecluseEdit", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        return false;
    }

    private void SaveAllFiles()
    {
        var savedCount = 0;
        foreach (var doc in _documentManager.Documents.Where(d => d.IsDirty).ToList())
        {
            if (string.IsNullOrEmpty(doc.FilePath))
            {
                _documentManager.ActiveDocument = doc;
                if (SaveActiveFileAs()) savedCount++;
            }
            else
            {
                _documentManager.SaveDocument(doc);
                savedCount++;
            }
        }

        StatusMessage.Text = savedCount > 0 ? $"Saved {savedCount} document(s)" : "All documents up to date";
    }

    private void CloseActiveFile()
    {
        if (_documentManager.ActiveDocument != null)
        {
            CloseDocumentWithPrompt(_documentManager.ActiveDocument);
        }
    }

    private bool CloseDocumentWithPrompt(DocumentModel document)
    {
        return _documentManager.CloseDocument(document, doc =>
        {
            var res = MessageBox.Show(this,
                $"Do you want to save changes to '{doc.FileName}'?",
                "RecluseEdit",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            return res switch
            {
                MessageBoxResult.Yes => true,
                MessageBoxResult.No => false,
                _ => null
            };
        });
    }

    #endregion

    #region Tab Context Menu & Middle Click

    private void OnTabMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed && sender is FrameworkElement { DataContext: DocumentModel doc })
        {
            CloseDocumentWithPrompt(doc);
            e.Handled = true;
        }
    }

    private DocumentModel? GetTabDocument(object sender)
    {
        if (sender is MenuItem { DataContext: DocumentModel doc }) return doc;
        if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is FrameworkElement fe && fe.DataContext is DocumentModel targetDoc)
        {
            return targetDoc;
        }
        return _documentManager.ActiveDocument;
    }

    private void OnMenuCloseTabClick(object sender, RoutedEventArgs e)
    {
        var doc = GetTabDocument(sender);
        if (doc != null) CloseDocumentWithPrompt(doc);
    }

    private void OnMenuCloseOtherTabsClick(object sender, RoutedEventArgs e)
    {
        var doc = GetTabDocument(sender);
        if (doc != null)
        {
            _documentManager.CloseOtherDocuments(doc, d =>
            {
                var res = MessageBox.Show(this, $"Do you want to save changes to '{d.FileName}'?", "RecluseEdit", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                return res switch { MessageBoxResult.Yes => true, MessageBoxResult.No => false, _ => null };
            });
        }
    }

    private void OnMenuCloseRightTabsClick(object sender, RoutedEventArgs e)
    {
        var doc = GetTabDocument(sender);
        if (doc != null)
        {
            _documentManager.CloseDocumentsToTheRight(doc, d =>
            {
                var res = MessageBox.Show(this, $"Do you want to save changes to '{d.FileName}'?", "RecluseEdit", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                return res switch { MessageBoxResult.Yes => true, MessageBoxResult.No => false, _ => null };
            });
        }
    }

    private void OnMenuCopyFullPathClick(object sender, RoutedEventArgs e)
    {
        var doc = GetTabDocument(sender);
        if (!string.IsNullOrEmpty(doc?.FilePath))
        {
            Clipboard.SetText(doc.FilePath);
            StatusMessage.Text = "Copied full path to clipboard";
        }
    }

    private void OnMenuRevealInExplorerClick(object sender, RoutedEventArgs e)
    {
        var doc = GetTabDocument(sender);
        if (!string.IsNullOrEmpty(doc?.FilePath) && File.Exists(doc.FilePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{doc.FilePath}\"",
                UseShellExecute = true
            });
        }
    }

    private void OnCloseAllTabsClick(object sender, RoutedEventArgs e)
    {
        _documentManager.CloseAllDocuments(doc =>
        {
            var res = MessageBox.Show(this, $"Do you want to save changes to '{doc.FileName}'?", "RecluseEdit", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return res switch { MessageBoxResult.Yes => true, MessageBoxResult.No => false, _ => null };
        });
    }

    #endregion

    #region Tab & Active Document Handling

    private void OnActiveDocumentChanged(DocumentModel? document)
    {
        if (document != null)
        {
            EmptyStateOverlay.Visibility = Visibility.Collapsed;
            EditorHost.Visibility = Visibility.Visible;
            EditorHost.DocumentModel = document;

            document.PropertyChanged += OnActiveDocumentPropertyChanged;
            UpdateDocumentStatusUI(document);

            CmbLanguage.SelectedItem = document.Language;
            Title = $"{document.FileName} - RecluseEdit";
        }
        else
        {
            EditorHost.DocumentModel = null;
            EditorHost.Visibility = Visibility.Collapsed;
            EmptyStateOverlay.Visibility = Visibility.Visible;

            StatusCaret.Text = "--";
            StatusLength.Text = "No open files";
            StatusLanguage.Text = "--";
            Title = "RecluseEdit";
        }
    }

    private void OnActiveDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is DocumentModel doc && doc == _documentManager.ActiveDocument)
        {
            UpdateDocumentStatusUI(doc);
        }
    }

    private void OnDocumentClosed(DocumentModel document)
    {
        document.PropertyChanged -= OnActiveDocumentPropertyChanged;
    }

    private void UpdateDocumentStatusUI(DocumentModel doc)
    {
        StatusCaret.Text = $"Ln {doc.CaretLine}, Col {doc.CaretColumn}";
        StatusLength.Text = $"Length: {doc.Document.TextLength}";
        StatusLanguage.Text = doc.Language.DisplayName;
        StatusEncoding.Text = doc.EncodingName;
    }

    private void OnTabClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DocumentModel doc })
        {
            _documentManager.ActiveDocument = doc;
        }
    }

    private void OnCloseTabButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DocumentModel doc })
        {
            CloseDocumentWithPrompt(doc);
        }
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_documentManager.ActiveDocument != null && CmbLanguage.SelectedItem is LanguageDefinition lang)
        {
            _documentManager.ActiveDocument.Language = lang;
            EditorHost.UpdateSyntaxHighlighting();
            StatusLanguage.Text = lang.DisplayName;
        }
    }

    #endregion

    #region Menu & Toolbar Handlers

    private void OnNewFileClick(object sender, RoutedEventArgs e) => CreateNewFile();
    private void OnOpenFileClick(object sender, RoutedEventArgs e) => OpenFileDialog();
    private void OnOpenFolderClick(object sender, RoutedEventArgs e) => OpenFolderDialog();
    private void OnSaveFileClick(object sender, RoutedEventArgs e) => SaveActiveFile();
    private void OnSaveAsFileClick(object sender, RoutedEventArgs e) => SaveActiveFileAs();
    private void OnSaveAllClick(object sender, RoutedEventArgs e) => SaveAllFiles();
    private void OnCloseTabClick(object sender, RoutedEventArgs e) => CloseActiveFile();
    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void OnUndoClick(object sender, RoutedEventArgs e) => EditorHost.UnderlyingEditor.Undo();
    private void OnRedoClick(object sender, RoutedEventArgs e) => EditorHost.UnderlyingEditor.Redo();
    private void OnCutClick(object sender, RoutedEventArgs e) => EditorHost.UnderlyingEditor.Cut();
    private void OnCopyClick(object sender, RoutedEventArgs e) => EditorHost.UnderlyingEditor.Copy();
    private void OnPasteClick(object sender, RoutedEventArgs e) => EditorHost.UnderlyingEditor.Paste();
    private void OnSelectAllClick(object sender, RoutedEventArgs e) => EditorHost.UnderlyingEditor.SelectAll();
    private void OnFindClick(object sender, RoutedEventArgs e) => EditorHost.OpenFind();
    private void OnReplaceClick(object sender, RoutedEventArgs e) => EditorHost.OpenReplace();

    private void OnToggleWordWrapClick(object sender, RoutedEventArgs e)
    {
        var wrap = MenuWordWrap.IsChecked;
        BtnWordWrap.IsChecked = wrap;
        EditorHost.ToggleWordWrap(wrap);
    }

    private void OnToolbarWordWrapClick(object sender, RoutedEventArgs e)
    {
        var wrap = BtnWordWrap.IsChecked == true;
        MenuWordWrap.IsChecked = wrap;
        EditorHost.ToggleWordWrap(wrap);
    }

    private void OnToggleLineNumbersClick(object sender, RoutedEventArgs e)
    {
        var show = MenuLineNumbers.IsChecked;
        BtnLineNumbers.IsChecked = show;
        EditorHost.ToggleLineNumbers(show);
    }

    private void OnToolbarLineNumbersClick(object sender, RoutedEventArgs e)
    {
        var show = BtnLineNumbers.IsChecked == true;
        MenuLineNumbers.IsChecked = show;
        EditorHost.ToggleLineNumbers(show);
    }

    private void OnManageExtensionsClick(object sender, RoutedEventArgs e)
    {
        var win = new ExtensionManagerWindow(_extensionManager, _toolchainManager)
        {
            Owner = this
        };
        win.ShowDialog();
        UpdateExtensionsStatus();
    }

    private void OnOpenExtensionsFolderClick(object sender, RoutedEventArgs e)
    {
        var extDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extensions");
        if (!Directory.Exists(extDir))
        {
            Directory.CreateDirectory(extDir);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = extDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to open folder:\n{ex.Message}", "RecluseEdit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "RecluseEdit v1.4.1\n\n" +
            "A fast, modern code editor optimized for web applications.\n\n" +
            "Key Features:\n" +
            "• Integrated Multi-Shell Terminal (Ctrl+`)\n" +
            "• DeepSeek AI Chat Assistant (Ctrl+Alt+A)\n" +
            "• Web Workspace Explorer (Ctrl+B)\n" +
            "• Dual Autocomplete: Ghost-text (Tab) + IntelliSense popup (Ctrl+Space)\n" +
            "• Built-in Find & Replace (Ctrl+F, Ctrl+H)\n" +
            "• Auto-closing pairs & HTML tags\n" +
            "• Tab context menu & middle-click close\n" +
            "• Code folding & bracket matching\n" +
            "• Pluggable Extension System\n\n" +
            "Made with love by indoctrinatedrecluse\n" +
            "Built with .NET 10 WPF & Visual Studio 2026 Enterprise.",
            "About RecluseEdit",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var dirtyDocs = _documentManager.Documents.Where(d => d.IsDirty).ToList();
        if (dirtyDocs.Count > 0)
        {
            var res = MessageBox.Show(this,
                $"You have {dirtyDocs.Count} unsaved document(s). Do you want to review and save them before exiting?",
                "RecluseEdit",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (res == MessageBoxResult.Yes)
            {
                SaveAllFiles();
            }
        }

        TerminalPane.CloseAllTerminals();
        base.OnClosing(e);
    }

    #endregion
}