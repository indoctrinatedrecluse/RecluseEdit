using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
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
    private CompletionWindow? _completionWindow;
    private FoldingManager? _foldingManager;
    private XmlFoldingStrategy? _xmlFoldingStrategy;

    public TextEditor UnderlyingEditor => Editor;
    public FindReplaceControl FindReplaceBar => FindReplace;

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
                _documentModel.Document.TextChanged -= OnDocumentTextChanged;
            }

            _documentModel = value;

            if (_documentModel != null)
            {
                if (_foldingManager != null)
                {
                    FoldingManager.Uninstall(_foldingManager);
                    _foldingManager = null;
                }

                Editor.Document = _documentModel.Document;

                try
                {
                    _foldingManager = FoldingManager.Install(Editor.TextArea);
                }
                catch
                {
                    _foldingManager = null;
                }

                _documentModel.PropertyChanged += OnDocumentModelPropertyChanged;
                _documentModel.Document.TextChanged += OnDocumentTextChanged;
                UpdateSyntaxHighlighting();
                UpdateFolding();
            }
            else
            {
                if (_foldingManager != null)
                {
                    FoldingManager.Uninstall(_foldingManager);
                    _foldingManager = null;
                }
                Editor.Document = null;
            }

            _ghostRenderer.Clear();
            FindReplace.Visibility = Visibility.Collapsed;
        }
    }

    public EditorControl()
    {
        InitializeComponent();

        FindReplace.Editor = Editor;

        _ghostRenderer = new GhostTextRenderer(Editor.TextArea.TextView);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_ghostRenderer);

        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        Editor.TextArea.TextEntering += OnTextEntering;
        Editor.TextArea.TextEntered += OnTextEntered;
        Editor.PreviewKeyDown += OnPreviewKeyDown;
        Editor.PreviewMouseWheel += OnPreviewMouseWheel;

        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.IndentationSize = 2;
        Editor.Options.EnableRectangularSelection = true;
        Editor.Options.EnableTextDragDrop = true;
        Editor.Options.ShowBoxForControlCharacters = true;
        Editor.Options.HighlightCurrentLine = true;

        _xmlFoldingStrategy = new XmlFoldingStrategy();
    }

    public void OpenFind()
    {
        var selected = Editor.SelectedText;
        FindReplace.ShowFind(string.IsNullOrEmpty(selected) ? null : selected);
    }

    public void OpenReplace()
    {
        var selected = Editor.SelectedText;
        FindReplace.ShowReplace(string.IsNullOrEmpty(selected) ? null : selected);
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        UpdateFolding();
    }

    private void UpdateFolding()
    {
        if (_foldingManager == null || _documentModel == null || _xmlFoldingStrategy == null) return;

        var langId = _documentModel.Language.Id.ToLowerInvariant();
        if (langId is "html" or "xml")
        {
            try
            {
                _xmlFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
            }
            catch
            {
                // Unclosed XML tags during typing
            }
        }
    }

    private void OnDocumentModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentModel.Language))
        {
            Dispatcher.Invoke(() =>
            {
                UpdateSyntaxHighlighting();
                UpdateFolding();
            });
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

    public void ToggleWordWrap(bool wrap) => Editor.WordWrap = wrap;
    public void ToggleLineNumbers(bool show) => Editor.ShowLineNumbers = show;

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_documentModel == null) return;

        var caret = Editor.TextArea.Caret;
        _documentModel.CaretLine = caret.Line;
        _documentModel.CaretColumn = caret.Column;
        _documentModel.CaretOffset = Editor.CaretOffset;

        if (_ghostRenderer.HasSuggestion && Editor.CaretOffset != _ghostRenderer.TargetOffset)
        {
            _ghostRenderer.Clear();
        }
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ctrl + MouseWheel zoom
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (e.Delta > 0)
            {
                Editor.FontSize = Math.Min(40, Editor.FontSize + 1);
            }
            else
            {
                Editor.FontSize = Math.Max(9, Editor.FontSize - 1);
            }

            _ghostRenderer.FontSize = Editor.FontSize;
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 1. Ctrl+Space triggers IntelliSense popup window
        if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ShowIntelliSense(force: true);
            e.Handled = true;
            return;
        }

        // 2. Ctrl+F triggers Find
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenFind();
            e.Handled = true;
            return;
        }

        // 3. Ctrl+H triggers Replace
        if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenReplace();
            e.Handled = true;
            return;
        }

        // 4. Tab accepts ghost-text inline completion
        if (e.Key == Key.Tab && _ghostRenderer.HasSuggestion && _completionWindow == null)
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

        // 5. Escape dismisses ghost text
        if (e.Key == Key.Escape && _ghostRenderer.HasSuggestion)
        {
            e.Handled = true;
            _ghostRenderer.Clear();
            return;
        }

        // 6. Clear ghost text on navigation
        if (e.Key is Key.Back or Key.Delete or Key.Enter or Key.Return or Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _ghostRenderer.Clear();
        }
    }

    private void OnTextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_' && e.Text[0] != '-')
            {
                // Non-identifier key closes completion window
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
    }

    private async void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (_documentModel == null || e.Text.Length == 0) return;

        var ch = e.Text[0];

        // 1. Auto-closing pairs
        HandleAutoClosingPairs(ch);

        // 2. Query inline autocomplete (Ghost text)
        await QueryGhostTextAsync();
    }

    private void HandleAutoClosingPairs(char ch)
    {
        var offset = Editor.CaretOffset;
        var textLength = Editor.Document.TextLength;

        switch (ch)
        {
            case '(':
                Editor.Document.Insert(offset, ")");
                Editor.CaretOffset = offset;
                break;
            case '{':
                Editor.Document.Insert(offset, "}");
                Editor.CaretOffset = offset;
                break;
            case '[':
                Editor.Document.Insert(offset, "]");
                Editor.CaretOffset = offset;
                break;
            case '"':
                Editor.Document.Insert(offset, "\"");
                Editor.CaretOffset = offset;
                break;
            case '\'':
                Editor.Document.Insert(offset, "'");
                Editor.CaretOffset = offset;
                break;
            case '>':
                // Auto-close HTML tags: check if typing '>' after an open tag like <div
                if (_documentModel?.Language.Id is "html" or "xml")
                {
                    TryAutoCloseHtmlTag(offset);
                }
                break;
        }
    }

    private void TryAutoCloseHtmlTag(int offset)
    {
        try
        {
            var line = Editor.Document.GetLineByOffset(offset);
            var col = offset - line.Offset;
            var lineText = Editor.Document.GetText(line.Offset, col);

            // Regex match for unclosed opening tag like <div or <button class="btn"
            var match = Regex.Match(lineText, @"<([a-zA-Z0-9\-]+)(?:\s+[^>]*?)?>?$");
            if (match.Success)
            {
                var tag = match.Groups[1].Value;
                // Don't auto-close void tags
                var voidTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "img", "input", "br", "hr", "meta", "link", "!doctype", "source", "area", "base", "col", "embed"
                };

                if (!voidTags.Contains(tag) && !lineText.EndsWith("/>"))
                {
                    var closeTag = $"</{tag}>";
                    Editor.Document.Insert(offset, closeTag);
                    Editor.CaretOffset = offset;
                }
            }
        }
        catch
        {
            // Ignore if bounds check fails
        }
    }

    private void ShowIntelliSense(bool force = false)
    {
        if (_documentModel == null) return;

        var caretOffset = Editor.CaretOffset;
        var word = GetWordAtOffset(caretOffset);

        var completions = WebIntelliSenseHelper.GetCompletions(_documentModel.Language.Id, word);
        if (completions.Count == 0 && !force) return;

        _completionWindow?.Close();
        _completionWindow = new CompletionWindow(Editor.TextArea);

        foreach (var c in completions)
        {
            _completionWindow.CompletionList.CompletionData.Add(c);
        }

        _completionWindow.Show();
        _completionWindow.Closed += (s, e) => _completionWindow = null;
    }

    private string GetWordAtOffset(int offset)
    {
        if (offset <= 0 || offset > Editor.Document.TextLength) return "";
        var line = Editor.Document.GetLineByOffset(offset);
        var lineText = Editor.Document.GetText(line.Offset, line.Length);
        var col = offset - line.Offset;
        var prefix = lineText[..Math.Min(col, lineText.Length)];
        var match = Regex.Match(prefix, @"([a-zA-Z0-9_\-]+)$");
        return match.Success ? match.Value : "";
    }

    private async Task QueryGhostTextAsync()
    {
        if (AutocompleteManager == null || _documentModel == null) return;

        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = new CancellationTokenSource();
        var ct = _suggestionCts.Token;

        var caretOffset = Editor.CaretOffset;
        if (caretOffset > Editor.Document.TextLength) return;

        try
        {
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
        }
        catch
        {
            _ghostRenderer.Clear();
        }
    }
}
