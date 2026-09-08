using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.Extensions;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Interaction logic for EditorControl.xaml
/// </summary>
public partial class EditorControl : UserControl
{
    private readonly GhostTextRenderer _ghostRenderer;
    private CancellationTokenSource? _suggestionCts;
    private DocumentModel? _documentModel;

    public TextEditor UnderlyingEditor => Editor;

    public SyntaxManager? SyntaxManager { get; set; }
    public AutocompleteManager? AutocompleteManager { get; set; }

    public DocumentModel? DocumentModel
    {
        get => _documentModel;
        set
        {
            if (_documentModel != null)
            {
                _documentModel.PropertyChanged -= OnDocumentModelPropertyChanged;
            }

            _documentModel = value;

            if (_documentModel != null)
            {
                Editor.Document = _documentModel.Document;
                _documentModel.PropertyChanged += OnDocumentModelPropertyChanged;
                UpdateSyntaxHighlighting();
            }
            else
            {
                Editor.Document = null;
            }

            _ghostRenderer.Clear();
        }
    }

    public EditorControl()
    {
        InitializeComponent();

        _ghostRenderer = new GhostTextRenderer(Editor.TextArea.TextView);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_ghostRenderer);

        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        Editor.TextArea.TextEntered += OnTextEntered;
        Editor.PreviewKeyDown += OnPreviewKeyDown;

        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.IndentationSize = 2;
        Editor.Options.EnableRectangularSelection = true;
        Editor.Options.EnableTextDragDrop = true;
        Editor.Options.ShowBoxForControlCharacters = true;
    }

    private void OnDocumentModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentModel.Language))
        {
            Dispatcher.Invoke(UpdateSyntaxHighlighting);
        }
    }

    public void UpdateSyntaxHighlighting()
    {
        if (_documentModel == null || SyntaxManager == null)
        {
            Editor.SyntaxHighlighting = null;
            return;
        }

        Editor.SyntaxHighlighting = SyntaxManager.GetHighlighting(_documentModel.Language);
    }

    public void ToggleWordWrap(bool wrap)
    {
        Editor.WordWrap = wrap;
    }

    public void ToggleLineNumbers(bool show)
    {
        Editor.ShowLineNumbers = show;
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_documentModel == null) return;

        var caret = Editor.TextArea.Caret;
        _documentModel.CaretLine = caret.Line;
        _documentModel.CaretColumn = caret.Column;
        _documentModel.CaretOffset = Editor.CaretOffset;

        // If caret moved away from ghost text target, clear suggestion
        if (_ghostRenderer.HasSuggestion && Editor.CaretOffset != _ghostRenderer.TargetOffset)
        {
            _ghostRenderer.Clear();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 1. Accept Inline Completion with Tab
        if (e.Key == Key.Tab && _ghostRenderer.HasSuggestion)
        {
            e.Handled = true;
            var textToInsert = _ghostRenderer.GhostText;
            var offset = _ghostRenderer.TargetOffset;
            _ghostRenderer.Clear();

            if (!string.IsNullOrEmpty(textToInsert) && offset <= Editor.Document.TextLength)
            {
                Editor.Document.Insert(offset, textToInsert);
                Editor.CaretOffset = offset + textToInsert.Length;
            }
            return;
        }

        // 2. Dismiss Inline Completion with Escape
        if (e.Key == Key.Escape && _ghostRenderer.HasSuggestion)
        {
            e.Handled = true;
            _ghostRenderer.Clear();
            return;
        }

        // 3. Clear ghost text on Backspace, Delete, Enter, Arrows
        if (e.Key is Key.Back or Key.Delete or Key.Enter or Key.Return or Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _ghostRenderer.Clear();
        }
    }

    private async void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (AutocompleteManager == null || _documentModel == null)
        {
            return;
        }

        // Cancel pending suggestion query
        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = new CancellationTokenSource();
        var ct = _suggestionCts.Token;

        var caretOffset = Editor.CaretOffset;
        var textLength = Editor.Document.TextLength;
        if (caretOffset > textLength) return;

        try
        {
            // Debounce for 60ms to let user finish fast keystrokes
            await Task.Delay(60, ct);

            var line = Editor.Document.GetLineByOffset(caretOffset);
            var lineText = Editor.Document.GetText(line.Offset, line.Length);
            var colInLine = caretOffset - line.Offset;
            var textBeforeCaret = lineText[..Math.Min(colInLine, lineText.Length)];

            var context = new InlineCompletionContext
            {
                TextBeforeCaret = textBeforeCaret,
                CurrentLineText = lineText,
                CaretOffset = caretOffset,
                LineNumber = line.LineNumber,
                ColumnNumber = colInLine + 1,
                LanguageId = _documentModel.Language.Id,
                FilePath = _documentModel.FilePath,
                FullText = Editor.Document.Text
            };

            var suggestion = await AutocompleteManager.GetSuggestionAsync(context, ct);

            if (!ct.IsCancellationRequested && Editor.CaretOffset == caretOffset)
            {
                if (!string.IsNullOrEmpty(suggestion))
                {
                    _ghostRenderer.TargetOffset = caretOffset;
                    _ghostRenderer.GhostText = suggestion;
                }
                else
                {
                    _ghostRenderer.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when user types faster than debounce
        }
        catch
        {
            _ghostRenderer.Clear();
        }
    }
}

