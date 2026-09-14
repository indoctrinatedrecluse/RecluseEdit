using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Interaction logic for EditorControl.xaml
/// </summary>
public partial class EditorControl : UserControl
{
    private readonly GhostTextRenderer _ghostRenderer;
    private readonly BracketHighlightRenderer _bracketRenderer;
    private readonly DiagnosticSquiggleRenderer _diagnosticRenderer;
    private readonly GitDiffMargin _gitDiffMargin;
    private readonly GitService _gitService = new();
    private CancellationTokenSource? _suggestionCts;
    private DocumentModel? _documentModel;
    private CompletionWindow? _completionWindow;
    private FoldingManager? _foldingManager;
    private XmlFoldingStrategy? _xmlFoldingStrategy;
    private readonly UniversalFoldingStrategy _foldingStrategy = new();
    private readonly MultiSelectionManager _multiSelectionManager = new();
    private readonly MultiCaretRenderer _multiCaretRenderer;
    private readonly SemanticSelectionService _semanticSelectionService = new();
    private ToolTip? _diagnosticToolTip;

    public string? WorkspacePath { get; set; }
    public TextEditor UnderlyingEditor => Editor;
    public FindReplaceControl FindReplaceBar => FindReplace;
    public InlineAiPromptBar InlineAiPromptBar => InlineAiPrompt;

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

                if (!_documentModel.IsLargeFile)
                {
                    try
                    {
                        _foldingManager = FoldingManager.Install(Editor.TextArea);
                    }
                    catch
                    {
                        _foldingManager = null;
                    }

                    UpdateSyntaxHighlighting();
                    UpdateFolding();
                }
                else
                {
                    _foldingManager = null;
                    Editor.SyntaxHighlighting = null;
                }

                IndentationDetector.Apply(Editor, _documentModel.Indentation);
                _documentModel.PropertyChanged += OnDocumentModelPropertyChanged;
                _documentModel.Document.TextChanged += OnDocumentTextChanged;
                _ = RefreshGitDiffAsync();
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

            _gitDiffMargin.Hunks = [];
            _ghostRenderer.Clear();
            _bracketRenderer.Clear();
            _diagnosticRenderer.Clear();
            FindReplace.Visibility = Visibility.Collapsed;
            InlineAiPrompt.Visibility = Visibility.Collapsed;
        }
    }

    public EditorControl()
    {
        InitializeComponent();

        _gitDiffMargin = new GitDiffMargin();
        Editor.TextArea.LeftMargins.Add(_gitDiffMargin);

        FindReplace.Editor = Editor;
        InlineAiPrompt.Editor = Editor;
        OverviewRuler.Editor = Editor;

        FindReplace.MatchesUpdated += lines => OverviewRuler.SetFindMatches(lines);
        FindReplace.CloseRequested += () => OverviewRuler.SetFindMatches([]);

        _ghostRenderer = new GhostTextRenderer(Editor.TextArea.TextView);
        _bracketRenderer = new BracketHighlightRenderer(Editor.TextArea.TextView);
        _diagnosticRenderer = new DiagnosticSquiggleRenderer(Editor.TextArea.TextView);
        _multiCaretRenderer = new MultiCaretRenderer(Editor.TextArea.TextView, _multiSelectionManager);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_ghostRenderer);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_bracketRenderer);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_diagnosticRenderer);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_multiCaretRenderer);

        Editor.TextArea.PreviewMouseDown += (_, _) => _multiSelectionManager.Clear();

        Editor.TextArea.TextView.MouseHover += OnTextViewMouseHover;
        Editor.TextArea.TextView.MouseHoverStopped += OnTextViewMouseHoverStopped;

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

    public void OpenInlineAi()
    {
        FindReplace.Visibility = Visibility.Collapsed;
        InlineAiPrompt.Show();
    }

    public void SetDiagnostics(IReadOnlyList<DiagnosticItem> diagnostics)
    {
        _diagnosticRenderer.SetDiagnostics(Editor.Document, diagnostics);
        OverviewRuler.SetDiagnostics(diagnostics);
    }

    public void ClearDiagnostics()
    {
        _diagnosticRenderer.Clear();
        OverviewRuler.SetDiagnostics([]);
    }

    private void OnTextViewMouseHover(object sender, MouseEventArgs e)
    {
        if (Editor.Document == null) return;

        var pos = Editor.GetPositionFromPoint(e.GetPosition(Editor));
        if (pos.HasValue)
        {
            try
            {
                int offset = Editor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
                var marker = _diagnosticRenderer.GetMarkerAtOffset(offset);
                if (marker != null)
                {
                    _diagnosticToolTip = new ToolTip
                    {
                        Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint,
                        PlacementTarget = Editor,
                        HorizontalOffset = e.GetPosition(Editor).X + 10,
                        VerticalOffset = e.GetPosition(Editor).Y + 16,
                        Content = new TextBlock
                        {
                            Text = $"[{marker.Severity}] {marker.Message}",
                            Foreground = marker.Severity switch
                            {
                                DiagnosticSeverity.Error => Brushes.Red,
                                DiagnosticSeverity.Warning => Brushes.Orange,
                                _ => Brushes.DeepSkyBlue
                            },
                            FontSize = 12
                        },
                        IsOpen = true
                    };
                    e.Handled = true;
                }
            }
            catch
            {
            }
        }
    }

    private void OnTextViewMouseHoverStopped(object sender, MouseEventArgs e)
    {
        if (_diagnosticToolTip != null)
        {
            _diagnosticToolTip.IsOpen = false;
            _diagnosticToolTip = null;
        }
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        UpdateFolding();
    }

    private void UpdateFolding()
    {
        if (_foldingManager == null || _documentModel == null || _xmlFoldingStrategy == null) return;
        if (_foldingManager == null || _documentModel == null || Editor.Document == null || _documentModel.IsLargeFile) return;

        var langId = _documentModel.Language.Id.ToLowerInvariant();
        if (langId is "html" or "xml")
        try
        {
            try
            {
                _xmlFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
            }
            catch
            {
                // Unclosed XML tags during typing
            }
            _foldingStrategy.UpdateFoldings(_foldingManager, Editor.Document, _documentModel.Language?.Id);
        }
        catch
        {
            // Ignore folding errors during typing
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
        else if (e.PropertyName is nameof(DocumentModel.FilePath) or nameof(DocumentModel.IsDirty))
        {
            _ = RefreshGitDiffAsync();
        }
    }

    public async Task RefreshGitDiffAsync()
    {
        if (string.IsNullOrEmpty(WorkspacePath) || _documentModel == null || string.IsNullOrEmpty(_documentModel.FilePath))
        {
            Dispatcher.Invoke(() => _gitDiffMargin.Hunks = []);
            return;
        }

        try
        {
            string relativePath = System.IO.Path.GetRelativePath(WorkspacePath, _documentModel.FilePath);
            var hunks = await _gitService.GetFileDiffHunksAsync(WorkspacePath, relativePath);
            Dispatcher.Invoke(() =>
            {
                _gitDiffMargin.Hunks = hunks;
                OverviewRuler.SetGitHunks(hunks);
            });
        }
        catch
        {
            Dispatcher.Invoke(() =>
            {
                _gitDiffMargin.Hunks = [];
                OverviewRuler.SetGitHunks([]);
            });
        }
    }

    public void UpdateSyntaxHighlighting()
    {
        if (_documentModel == null || SyntaxManager == null || _documentModel.IsLargeFile)
        {
            Editor.SyntaxHighlighting = null;
            return;
        }

        try
        {
            var def = SyntaxManager.GetHighlighting(_documentModel.Language);
            if (def != null && Editor.Document != null && Editor.Document.LineCount > 0)
            {
                // Safely pre-test highlighting on the first few lines to ensure definition rules don't throw
                var testHighlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(Editor.Document, def);
                int checkCount = Math.Min(10, Editor.Document.LineCount);
                for (int i = 1; i <= checkCount; i++)
                {
                    testHighlighter.HighlightLine(i);
                }
            }

            Editor.SyntaxHighlighting = def;
        }
        catch (Exception ex)
        {
            Editor.SyntaxHighlighting = null;
            System.Diagnostics.Debug.WriteLine($"[EditorControl] Failed to apply syntax highlighting: {ex.Message}");
        }
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

        // Live bracket matching highlight
        if (Editor.Document != null)
        {
            var match = BracketHighlightRenderer.FindMatchingBrackets(Editor.Document, Editor.CaretOffset);
            if (match.HasValue)
            {
                _bracketRenderer.SetBracketMatch(match.Value.OpenOffset, match.Value.CloseOffset);
            }
            else
            {
                _bracketRenderer.Clear();
            }
        }

        // Live scope calculation for breadcrumb / status bar
        if (!_documentModel.IsLargeFile && Editor.Document != null)
        {
            var symbols = DocumentSymbolService.ExtractSymbols(Editor.Document, _documentModel.FilePath ?? string.Empty);
            _documentModel.CurrentScope = DocumentSymbolService.GetEnclosingScope(symbols, caret.Line);
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

        // 3b. Ctrl+I triggers Inline AI Prompt & Generation
        if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenInlineAi();
            e.Handled = true;
            return;
        }

        // 4. Ctrl+/ toggles line comment
        if ((e.Key is Key.OemQuestion or Key.Divide) && Keyboard.Modifiers == ModifierKeys.Control)
        {
            EditorOperations.ToggleLineComment(Editor, _documentModel?.Language?.Id);
            e.Handled = true;
            return;
        }

        // 5. Alt+Up / Alt+Down moves lines
        if (e.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            EditorOperations.MoveLinesUp(Editor);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            EditorOperations.MoveLinesDown(Editor);
            e.Handled = true;
            return;
        }

        // 6. Shift+Alt+Down / Shift+Alt+Up or Ctrl+D duplicates lines
        if (e.Key == Key.Down && Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Alt))
        {
            EditorOperations.DuplicateLinesDown(Editor);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Up && Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Alt))
        {
            EditorOperations.DuplicateLinesUp(Editor);
            e.Handled = true;
            return;
        }
        // 6. Ctrl+D triggers Multi-Caret: Add Next Occurrence
        if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
        {
            EditorOperations.DuplicateLinesDown(Editor);
            _multiSelectionManager.AddNextOccurrence(Editor);
            e.Handled = true;
            return;
        }

        // 6b. Ctrl+Shift+L triggers Multi-Caret: Select All Occurrences
        if (e.Key == Key.L && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _multiSelectionManager.SelectAllOccurrences(Editor);
            e.Handled = true;
            return;
        }

        // 6c. Shift+Alt+Right triggers Semantic Selection: Expand
        if (e.Key == Key.Right && Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Alt))
        {
            _semanticSelectionService.ExpandSelection(Editor);
            e.Handled = true;
            return;
        }

        // 6d. Shift+Alt+Left triggers Semantic Selection: Shrink
        if (e.Key == Key.Left && Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Alt))
        {
            _semanticSelectionService.ShrinkSelection(Editor);
            e.Handled = true;
            return;
        }

        // 7. Ctrl+Shift+K deletes line(s)
        if (e.Key == Key.K && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            EditorOperations.DeleteLines(Editor);
            e.Handled = true;
            return;
        }

        // 8. Ctrl+J joins lines
        if (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
        {
            EditorOperations.JoinLines(Editor);
            e.Handled = true;
            return;
        }

        // 9. Ctrl+Shift+U / Ctrl+U case transformations
        if (e.Key == Key.U && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            EditorOperations.TransformToUppercase(Editor);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
        {
            EditorOperations.TransformToLowercase(Editor);
            e.Handled = true;
            return;
        }

        // 10. Ctrl+Alt+Down / Ctrl+Alt+Up column cursors
        if (e.Key == Key.Down && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            EditorOperations.AddColumnCursorDown(Editor);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Up && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            EditorOperations.AddColumnCursorUp(Editor);
            e.Handled = true;
            return;
        }

        // 11. Multiline / Column Backspace and Delete
        if (e.Key == Key.Back && Editor.TextArea.Selection is ICSharpCode.AvalonEdit.Editing.RectangleSelection rectSelBack)
        {
            e.Handled = true;
            HandleRectangleBackspace(rectSelBack);
            return;
        }
        if (e.Key == Key.Delete && Editor.TextArea.Selection is ICSharpCode.AvalonEdit.Editing.RectangleSelection rectSelDel)
        {
            e.Handled = true;
            HandleRectangleDelete(rectSelDel);
            return;
        }

        // 12. Tab accepts ghost-text inline completion
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

        // 13. Escape dismisses ghost text or clears multi-carets
        if (e.Key == Key.Escape)
        {
            if (_multiSelectionManager.HasSecondarySelections)
            {
                _multiSelectionManager.Clear();
                e.Handled = true;
                return;
            }
            if (_ghostRenderer.HasSuggestion)
            {
                e.Handled = true;
                _ghostRenderer.Clear();
                return;
            }
        }

        // 13b. Multi-Caret Backspace and Delete
        if (e.Key == Key.Back && _multiSelectionManager.HasSecondarySelections)
        {
            if (_multiSelectionManager.HandleBackspace(Editor))
            {
                e.Handled = true;
                return;
            }
        }
        if (e.Key == Key.Delete && _multiSelectionManager.HasSecondarySelections)
        {
            if (_multiSelectionManager.HandleDelete(Editor))
            {
                e.Handled = true;
                return;
            }
        }

        // 14. Clear ghost text on navigation
        if (e.Key is Key.Back or Key.Delete or Key.Enter or Key.Return or Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _ghostRenderer.Clear();
        }

        // 15. Code folding shortcuts: Ctrl+Shift+[ (fold current), Ctrl+Shift+] (unfold current), Ctrl+M (toggle fold)
        if ((e.Key == Key.OemOpenBrackets || e.Key == Key.Oem4) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            FoldCurrent();
            e.Handled = true;
            return;
        }
        if ((e.Key == Key.OemCloseBrackets || e.Key == Key.Oem6) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            UnfoldCurrent();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ToggleFoldAtCaret();
            e.Handled = true;
            return;
        }

        // 16. Smart Backspace for delimiter pairs
        if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (EditorOperations.HandleSmartBackspace(Editor))
            {
                e.Handled = true;
                return;
            }
        }

        // 17. Smart Enter (auto-indent, brace-splitting)
        if (_completionWindow == null && (e.Key is Key.Enter or Key.Return) && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (Editor.Document != null)
            {
                EditorOperations.HandleSmartEnter(Editor, _documentModel?.Language?.Id);
                e.Handled = true;
                return;
            }
        }
    }

    private void HandleRectangleBackspace(ICSharpCode.AvalonEdit.Editing.RectangleSelection rectSel)
    {
        if (Editor.Document == null) return;

        using (Editor.Document.RunUpdate())
        {
            if (rectSel.StartPosition.VisualColumn != rectSel.EndPosition.VisualColumn)
            {
                rectSel.ReplaceSelectionWithText(string.Empty);
            }
            else
            {
                var segments = rectSel.Segments.OrderByDescending(s => s.StartOffset).ToList();
                int deletedCount = 0;
                foreach (var seg in segments)
                {
                    var line = Editor.Document.GetLineByOffset(seg.StartOffset);
                    int colInLine = seg.StartOffset - line.Offset;
                    if (colInLine > 0)
                    {
                        Editor.Document.Remove(seg.StartOffset - 1, 1);
                        deletedCount++;
                    }
                }

                int top = Math.Min(rectSel.StartPosition.Line, rectSel.EndPosition.Line);
                int bottom = Math.Max(rectSel.StartPosition.Line, rectSel.EndPosition.Line);
                int col = rectSel.StartPosition.Column;
                int visCol = rectSel.StartPosition.VisualColumn;

                if (deletedCount > 0 && col > 1)
                {
                    int newCol = col - 1;
                    int newVisCol = Math.Max(1, visCol - 1);
                    var newStart = new ICSharpCode.AvalonEdit.TextViewPosition(top, newCol, newVisCol);
                    var newEnd = new ICSharpCode.AvalonEdit.TextViewPosition(bottom, newCol, newVisCol);
                    Editor.TextArea.Selection = new ICSharpCode.AvalonEdit.Editing.RectangleSelection(Editor.TextArea, newStart, newEnd);
                    Editor.TextArea.Caret.Position = newEnd;
                }
            }
        }
    }

    private void HandleRectangleDelete(ICSharpCode.AvalonEdit.Editing.RectangleSelection rectSel)
    {
        if (Editor.Document == null) return;

        using (Editor.Document.RunUpdate())
        {
            if (rectSel.StartPosition.VisualColumn != rectSel.EndPosition.VisualColumn)
            {
                rectSel.ReplaceSelectionWithText(string.Empty);
            }
            else
            {
                var segments = rectSel.Segments.OrderByDescending(s => s.StartOffset).ToList();
                foreach (var seg in segments)
                {
                    var line = Editor.Document.GetLineByOffset(seg.StartOffset);
                    if (seg.StartOffset < line.Offset + line.Length)
                    {
                        Editor.Document.Remove(seg.StartOffset, 1);
                    }
                }
            }
        }
    }

    public void ToggleLineComment() => EditorOperations.ToggleLineComment(Editor, _documentModel?.Language?.Id);
    public void MoveLinesUp() => EditorOperations.MoveLinesUp(Editor);
    public void MoveLinesDown() => EditorOperations.MoveLinesDown(Editor);
    public void DuplicateLinesDown() => EditorOperations.DuplicateLinesDown(Editor);
    public void DuplicateLinesUp() => EditorOperations.DuplicateLinesUp(Editor);
    public void DeleteLines() => EditorOperations.DeleteLines(Editor);
    public void JoinLines() => EditorOperations.JoinLines(Editor);
    public void TransformToUppercase() => EditorOperations.TransformToUppercase(Editor);
    public void TransformToLowercase() => EditorOperations.TransformToLowercase(Editor);
    public void SortLines() => EditorOperations.SortLines(Editor);
    public void TrimTrailingWhitespace() => EditorOperations.TrimTrailingWhitespace(Editor);
    public void GoToLine(int line, int col = 1) => EditorOperations.GoToLine(Editor, line, col);
    public void FoldAll() => EditorOperations.FoldAll(_foldingManager);
    public void UnfoldAll() => EditorOperations.UnfoldAll(_foldingManager);
    public void ToggleFoldAtCaret() => EditorOperations.ToggleFoldAtOffset(_foldingManager, Editor.CaretOffset);
    public void FoldCurrent() => EditorOperations.SetFoldAtOffset(_foldingManager, Editor.CaretOffset, true);
    public void UnfoldCurrent() => EditorOperations.SetFoldAtOffset(_foldingManager, Editor.CaretOffset, false);
    public void AddNextOccurrence() => _multiSelectionManager.AddNextOccurrence(Editor);
    public void SelectAllOccurrences() => _multiSelectionManager.SelectAllOccurrences(Editor);
    public void ExpandSelection() => _semanticSelectionService.ExpandSelection(Editor);
    public void ShrinkSelection() => _semanticSelectionService.ShrinkSelection(Editor);
    public void ClearMultiCarets() => _multiSelectionManager.Clear();

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

        // Multi-Caret Text Insertion
        if (_multiSelectionManager.HasSecondarySelections && !string.IsNullOrEmpty(e.Text))
        {
            if (_multiSelectionManager.HandleTextInput(Editor, e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        // Smart Overtype: If typing closing delimiter and the next character is that delimiter, skip over it instead of inserting duplicate
        if (e.Text.Length == 1 && Editor.Document != null && Editor.TextArea.Selection.IsEmpty)
        {
            char inputChar = e.Text[0];
            if (inputChar is ')' or '}' or ']' or '"' or '\'' or '`')
            {
                int caret = Editor.CaretOffset;
                if (caret < Editor.Document.TextLength && Editor.Document.GetCharAt(caret) == inputChar)
                {
                    Editor.CaretOffset++;
                    e.Handled = true;
                    return;
                }
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
        if (AutocompleteManager == null || _documentModel == null || _documentModel.IsLargeFile) return;

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

    /// <summary>
    /// Dynamically applies theme-specific caret, selection, and line highlight colors to the AvalonEdit text area.
    /// </summary>
    public void ApplyTheme(ThemeColors colors)
    {
        try
        {
            if (!string.IsNullOrEmpty(colors.Caret))
            {
                var caretColor = (Color)ColorConverter.ConvertFromString(colors.Caret);
                var caretBrush = new SolidColorBrush(caretColor);
                caretBrush.Freeze();
                Editor.TextArea.Caret.CaretBrush = caretBrush;
            }

            if (!string.IsNullOrEmpty(colors.Selection))
            {
                var selColor = (Color)ColorConverter.ConvertFromString(colors.Selection);
                var selBrush = new SolidColorBrush(selColor);
                selBrush.Freeze();
                Editor.TextArea.SelectionBrush = selBrush;
            }

            if (!string.IsNullOrEmpty(colors.CurrentLine))
            {
                var lineBgColor = (Color)ColorConverter.ConvertFromString(colors.CurrentLine);
                var lineBgBrush = new SolidColorBrush(lineBgColor);
                lineBgBrush.Freeze();
                Editor.TextArea.TextView.CurrentLineBackground = lineBgBrush;
            }

            if (!string.IsNullOrEmpty(colors.BracketMatch))
            {
                var bracketColor = (Color)ColorConverter.ConvertFromString(colors.BracketMatch);
                _bracketRenderer.SetHighlightColor(bracketColor);
            }

            _multiCaretRenderer?.UpdateColors(Editor.TextArea.SelectionBrush, Editor.TextArea.Caret.CaretBrush);
        }
        catch
        {
            // Fallback gracefully if hex format is invalid
        }
    }
}
