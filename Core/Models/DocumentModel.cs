using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Core.Models;

/// <summary>
/// Represents an open file or tab within the editor.
/// </summary>
public class DocumentModel : INotifyPropertyChanged
{
    private string? _filePath;
    private string _title = "Untitled";
    private bool _isDirty;
    private bool _isActive;
    private LanguageDefinition _language;
    private Encoding _encoding = Encoding.UTF8;
    private int _caretLine = 1;
    private int _caretColumn = 1;
    private int _caretOffset;
    private IndentationInfo _indentation;
    private DocumentLineEnding _lineEnding;
    private bool _isLargeFile;
    private string? _currentScope;

    public Guid Id { get; } = Guid.NewGuid();

    public TextDocument Document { get; }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                UpdateTitle();
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    public string FileName => string.IsNullOrEmpty(_filePath) ? _title : Path.GetFileName(_filePath);

    public string Title
    {
        get => _isDirty ? $"{FileName} *" : FileName;
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public LanguageDefinition Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged();
            }
        }
    }

    public Encoding Encoding
    {
        get => _encoding;
        set
        {
            if (_encoding != value)
            {
                _encoding = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EncodingName));
            }
        }
    }

    public string EncodingName => _encoding.WebName.ToUpperInvariant();

    public int CaretLine
    {
        get => _caretLine;
        set
        {
            if (_caretLine != value)
            {
                _caretLine = value;
                OnPropertyChanged();
            }
        }
    }

    public int CaretColumn
    {
        get => _caretColumn;
        set
        {
            if (_caretColumn != value)
            {
                _caretColumn = value;
                OnPropertyChanged();
            }
        }
    }

    public int CaretOffset
    {
        get => _caretOffset;
        set
        {
            if (_caretOffset != value)
            {
                _caretOffset = value;
                OnPropertyChanged();
            }
        }
    }

    public IndentationInfo Indentation
    {
        get => _indentation;
        set
        {
            if (_indentation != value)
            {
                _indentation = value;
                OnPropertyChanged();
            }
        }
    }

    public DocumentLineEnding LineEnding
    {
        get => _lineEnding;
        set
        {
            if (_lineEnding != value)
            {
                _lineEnding = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLargeFile
    {
        get => _isLargeFile;
        set
        {
            if (_isLargeFile != value)
            {
                _isLargeFile = value;
                OnPropertyChanged();
            }
        }
    }

    public string? CurrentScope
    {
        get => _currentScope;
        set
        {
            if (_currentScope != value)
            {
                _currentScope = value;
                OnPropertyChanged();
            }
        }
    }

    public DocumentModel(string title, string initialText, LanguageDefinition language, string? filePath = null)
    {
        _title = title;
        _filePath = filePath;
        _language = language;
        _indentation = IndentationDetector.Detect(initialText);
        _lineEnding = LineEndingDetector.Detect(initialText);
        Document = new TextDocument(initialText);

        Document.TextChanged += (s, e) =>
        {
            IsDirty = true;
        };
    }

    public void MarkSaved(string path)
    {
        _filePath = path;
        IsDirty = false;
        UpdateTitle();
        OnPropertyChanged(nameof(FileName));
    }

    private void UpdateTitle()
    {
        OnPropertyChanged(nameof(Title));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
