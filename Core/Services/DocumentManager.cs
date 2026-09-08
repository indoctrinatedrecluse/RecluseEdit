using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using RecluseEdit.Core.Models;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages open document tabs, active document selection, and file I/O.
/// </summary>
public class DocumentManager
{
    private readonly SyntaxManager _syntaxManager;
    private int _untitledCounter = 1;
    private DocumentModel? _activeDocument;

    public ObservableCollection<DocumentModel> Documents { get; } = [];

    public DocumentModel? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (_activeDocument != value)
            {
                if (_activeDocument != null)
                {
                    _activeDocument.IsActive = false;
                }

                _activeDocument = value;

                if (_activeDocument != null)
                {
                    _activeDocument.IsActive = true;
                }

                ActiveDocumentChanged?.Invoke(_activeDocument);
            }
        }
    }

    public event Action<DocumentModel?>? ActiveDocumentChanged;
    public event Action<DocumentModel>? DocumentClosed;

    public DocumentManager(SyntaxManager syntaxManager)
    {
        _syntaxManager = syntaxManager;
    }

    /// <summary>
    /// Creates a new untitled document tab.
    /// </summary>
    public DocumentModel CreateNewDocument(string? initialContent = null, LanguageDefinition? language = null)
    {
        var title = $"Untitled-{_untitledCounter++}";
        var lang = language ?? _syntaxManager.GetLanguageById("html") ?? SyntaxManager.PlainText;
        var content = initialContent ?? GetDefaultTemplateForLanguage(lang.Id);

        var doc = new DocumentModel(title, content, lang);
        Documents.Add(doc);
        ActiveDocument = doc;
        return doc;
    }

    /// <summary>
    /// Opens an existing file from disk or activates it if already open.
    /// </summary>
    public DocumentModel OpenDocument(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);

        // Check if already open
        var existing = Documents.FirstOrDefault(d =>
            !string.IsNullOrEmpty(d.FilePath) &&
            string.Equals(Path.GetFullPath(d.FilePath), fullPath, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            ActiveDocument = existing;
            return existing;
        }

        var text = File.ReadAllText(fullPath, Encoding.UTF8);
        var lang = _syntaxManager.GetLanguageForFile(fullPath);
        var fileName = Path.GetFileName(fullPath);

        var doc = new DocumentModel(fileName, text, lang, fullPath)
        {
            IsDirty = false
        };

        Documents.Add(doc);
        ActiveDocument = doc;
        return doc;
    }

    /// <summary>
    /// Saves the document to its designated file path.
    /// </summary>
    public bool SaveDocument(DocumentModel document, string? targetFilePath = null)
    {
        var path = targetFilePath ?? document.FilePath;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        File.WriteAllText(path, document.Document.Text, document.Encoding);
        document.MarkSaved(path);
        document.Language = _syntaxManager.GetLanguageForFile(path);
        return true;
    }

    /// <summary>
    /// Closes a document tab. If dirty and confirmSavePrompt is provided, calls the confirmation callback.
    /// </summary>
    public bool CloseDocument(DocumentModel document, Func<DocumentModel, bool?>? confirmSavePrompt = null)
    {
        if (document.IsDirty && confirmSavePrompt != null)
        {
            var saveResult = confirmSavePrompt(document);
            if (saveResult == null)
            {
                // User cancelled close operation
                return false;
            }

            if (saveResult == true)
            {
                if (!SaveDocument(document))
                {
                    return false;
                }
            }
        }

        var index = Documents.IndexOf(document);
        Documents.Remove(document);
        DocumentClosed?.Invoke(document);

        if (ActiveDocument == document)
        {
            if (Documents.Count > 0)
            {
                var nextIndex = Math.Min(index, Documents.Count - 1);
                ActiveDocument = Documents[nextIndex];
            }
            else
            {
                ActiveDocument = null;
            }
        }

        return true;
    }

    /// <summary>
    /// Closes all tabs except the specified document.
    /// </summary>
    public void CloseOtherDocuments(DocumentModel keepDoc, Func<DocumentModel, bool?>? confirmSavePrompt = null)
    {
        var others = Documents.Where(d => d != keepDoc).ToList();
        foreach (var doc in others)
        {
            CloseDocument(doc, confirmSavePrompt);
        }
    }

    /// <summary>
    /// Closes all tabs located to the right of the specified document.
    /// </summary>
    public void CloseDocumentsToTheRight(DocumentModel currentDoc, Func<DocumentModel, bool?>? confirmSavePrompt = null)
    {
        var index = Documents.IndexOf(currentDoc);
        if (index < 0) return;

        var toClose = Documents.Skip(index + 1).ToList();
        foreach (var doc in toClose)
        {
            CloseDocument(doc, confirmSavePrompt);
        }
    }

    /// <summary>
    /// Closes all open document tabs.
    /// </summary>
    public void CloseAllDocuments(Func<DocumentModel, bool?>? confirmSavePrompt = null)
    {
        var all = Documents.ToList();
        foreach (var doc in all)
        {
            CloseDocument(doc, confirmSavePrompt);
        }
    }

    private static string GetDefaultTemplateForLanguage(string languageId)
    {
        return languageId switch
        {
            "html" => "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n  <meta charset=\"UTF-8\">\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n  <title>Recluse App</title>\n</head>\n<body>\n  <h1>Hello, RecluseEdit!</h1>\n</body>\n</html>\n",
            "javascript" => "// RecluseEdit JavaScript\nconsole.log(\"Ready to build web applications!\");\n",
            "css" => "/* RecluseEdit Stylesheet */\nbody {\n  margin: 0;\n  font-family: system-ui, sans-serif;\n  background-color: #121212;\n  color: #f0f0f0;\n}\n",
            _ => ""
        };
    }
}
