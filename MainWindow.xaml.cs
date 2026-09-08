using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static readonly RoutedUICommand NewFileCommand = new("New File", "NewFile", typeof(MainWindow));
    public static readonly RoutedUICommand OpenFileCommand = new("Open File", "OpenFile", typeof(MainWindow));
    public static readonly RoutedUICommand SaveFileCommand = new("Save File", "SaveFile", typeof(MainWindow));
    public static readonly RoutedUICommand SaveAsFileCommand = new("Save As File", "SaveAsFile", typeof(MainWindow));
    public static readonly RoutedUICommand CloseFileCommand = new("Close File", "CloseFile", typeof(MainWindow));

    private readonly SyntaxManager _syntaxManager;
    private readonly AutocompleteManager _autocompleteManager;
    private readonly ExtensionManager _extensionManager;
    private readonly DocumentManager _documentManager;

    public MainWindow()
    {
        InitializeComponent();

        _syntaxManager = new SyntaxManager();
        _autocompleteManager = new AutocompleteManager();
        _extensionManager = new ExtensionManager(_syntaxManager, _autocompleteManager);
        _documentManager = new DocumentManager(_syntaxManager);

        EditorHost.SyntaxManager = _syntaxManager;
        EditorHost.AutocompleteManager = _autocompleteManager;

        // Command bindings
        CommandBindings.Add(new CommandBinding(NewFileCommand, (_, _) => CreateNewFile()));
        CommandBindings.Add(new CommandBinding(OpenFileCommand, (_, _) => OpenFileDialog()));
        CommandBindings.Add(new CommandBinding(SaveFileCommand, (_, _) => SaveActiveFile()));
        CommandBindings.Add(new CommandBinding(SaveAsFileCommand, (_, _) => SaveActiveFileAs()));
        CommandBindings.Add(new CommandBinding(CloseFileCommand, (_, _) => CloseActiveFile()));

        // Wire document changes
        _documentManager.ActiveDocumentChanged += OnActiveDocumentChanged;
        _documentManager.DocumentClosed += OnDocumentClosed;

        TabItemsControl.ItemsSource = _documentManager.Documents;
        CmbLanguage.ItemsSource = _syntaxManager.SupportedLanguages;

        // Initialize extensions (built-in and dynamic)
        _extensionManager.Initialize();
        _extensionManager.ExtensionsChanged += UpdateExtensionsStatus;
        UpdateExtensionsStatus();

        // Create default initial document
        CreateNewFile();
    }

    private void UpdateExtensionsStatus()
    {
        StatusExtensions.Text = $"Extensions: {_extensionManager.LoadedExtensions.Count}";
    }

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
            Filter = "Web Files (*.html;*.htm;*.css;*.js;*.ts;*.json)|*.html;*.htm;*.css;*.js;*.ts;*.json|" +
                     "Text & Code (*.txt;*.xml;*.cs;*.md)|*.txt;*.xml;*.cs;*.md|" +
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
            Filter = "HTML Files (*.html)|*.html|" +
                     "JavaScript Files (*.js)|*.js|" +
                     "CSS Files (*.css)|*.css|" +
                     "TypeScript Files (*.ts)|*.ts|" +
                     "JSON Files (*.json)|*.json|" +
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

            // Sync language combo
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

    private void OnViewExtensionsClick(object sender, RoutedEventArgs e)
    {
        var exts = _extensionManager.LoadedExtensions;
        var info = exts.Count == 0
            ? "No extensions currently loaded."
            : string.Join("\n\n", exts.Select(x => $"• {x.Name} (v{x.Version})\n  ID: {x.Id}\n  Author: {x.Author}\n  {x.Description}"));

        MessageBox.Show(this,
            $"RecluseEdit Loaded Extensions ({exts.Count}):\n\n{info}\n\nTo add external language extensions, drop compiled extension DLLs into the Extensions folder.",
            "RecluseEdit Extensions",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
            "RecluseEdit v1.0.0\n\n" +
            "A fast, modern code editor optimized for web applications.\n" +
            "Features:\n" +
            "- High performance AvalonEdit engine with line numbers\n" +
            "- Syntax highlighting for HTML, CSS, JS, TS, JSON, XML, C#\n" +
            "- Inline ghost-text autocomplete (Tab to complete)\n" +
            "- Multi-file tabbed workspace\n" +
            "- Pluggable Extension Architecture\n\n" +
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

        base.OnClosing(e);
    }

    #endregion
}