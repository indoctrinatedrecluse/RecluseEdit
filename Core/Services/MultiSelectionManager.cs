using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Represents a text selection range or caret position.
/// </summary>
public record struct EditorSelectionRange(int StartOffset, int Length)
{
    public int EndOffset => StartOffset + Length;
    public bool IsEmpty => Length == 0;
}

/// <summary>
/// Manages multiple selections and carets for simultaneous multi-point editing,
/// implementing Ctrl+D (Add Next Occurrence) and Ctrl+Shift+L (Select All Occurrences).
/// </summary>
public class MultiSelectionManager
{
    private readonly List<EditorSelectionRange> _secondarySelections = [];
    private string? _lastSearchTerm;

    public IReadOnlyList<EditorSelectionRange> SecondarySelections => _secondarySelections;
    public bool HasSecondarySelections => _secondarySelections.Count > 0;

    public event EventHandler? SelectionsChanged;

    public void Clear()
    {
        if (_secondarySelections.Count > 0)
        {
            _secondarySelections.Clear();
            _lastSearchTerm = null;
            SelectionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Implements Ctrl+D (Add Next Occurrence):
    /// - If no selection, selects the word under caret.
    /// - If selection exists, searches forward and adds the next occurrence as a secondary selection.
    /// </summary>
    public void AddNextOccurrence(TextEditor editor)
    {
        var doc = editor.Document;
        if (doc == null || doc.TextLength == 0) return;

        // 1. If no primary selection and no secondary selections, select word under cursor
        if (editor.SelectionLength == 0 && _secondarySelections.Count == 0)
        {
            var word = GetWordAtOffset(doc, editor.CaretOffset);
            if (word.Length > 0)
            {
                editor.Select(word.StartOffset, word.Length);
                _lastSearchTerm = word.Length > 0 ? doc.GetText(word.StartOffset, word.Length) : null;
                SelectionsChanged?.Invoke(this, EventArgs.Empty);
            }
            return;
        }

        // 2. We have a selection to search for
        var searchTerm = editor.SelectedText;
        if (string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = _lastSearchTerm;
        }
        else
        {
            _lastSearchTerm = searchTerm;
        }

        if (string.IsNullOrEmpty(searchTerm)) return;

        // Combine primary selection and secondary selections to find existing match bounds
        var allRanges = GetAllRanges(editor);
        int maxEnd = allRanges.Max(r => r.EndOffset);

        // Search forward from the furthest selection
        int match = doc.Text.IndexOf(searchTerm, maxEnd, StringComparison.Ordinal);

        // If not found after maxEnd, wrap around to beginning
        if (match == -1)
        {
            match = doc.Text.IndexOf(searchTerm, 0, maxEnd, StringComparison.Ordinal);
        }

        if (match != -1)
        {
            // Verify not already selected
            bool alreadySelected = allRanges.Any(r => r.StartOffset == match && r.Length == searchTerm.Length);
            if (!alreadySelected)
            {
                _secondarySelections.Add(new EditorSelectionRange(match, searchTerm.Length));

                // Scroll match into view
                var loc = doc.GetLocation(match);
                editor.ScrollTo(loc.Line, loc.Column);

                SelectionsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Implements Ctrl+Shift+L (Select All Occurrences):
    /// Selects all occurrences of the currently selected text (or word under cursor) in the document.
    /// </summary>
    public void SelectAllOccurrences(TextEditor editor)
    {
        var doc = editor.Document;
        if (doc == null || doc.TextLength == 0) return;

        if (editor.SelectionLength == 0)
        {
            var word = GetWordAtOffset(doc, editor.CaretOffset);
            if (word.Length > 0)
            {
                editor.Select(word.StartOffset, word.Length);
            }
        }

        var searchTerm = editor.SelectedText;
        if (string.IsNullOrEmpty(searchTerm)) return;

        _lastSearchTerm = searchTerm;
        _secondarySelections.Clear();

        int index = 0;
        bool isFirst = true;

        while (index < doc.TextLength)
        {
            int match = doc.Text.IndexOf(searchTerm, index, StringComparison.Ordinal);
            if (match == -1) break;

            if (isFirst)
            {
                editor.Select(match, searchTerm.Length);
                isFirst = false;
            }
            else
            {
                _secondarySelections.Add(new EditorSelectionRange(match, searchTerm.Length));
            }

            index = match + searchTerm.Length;
        }

        SelectionsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles text input across all selections/carets simultaneously.
    /// </summary>
    public bool HandleTextInput(TextEditor editor, string text)
    {
        if (!HasSecondarySelections || editor.Document == null) return false;

        var allRanges = GetAllRanges(editor);
        var doc = editor.Document;

        using (doc.RunUpdate())
        {
            // Apply replacements in descending offset order
            foreach (var r in allRanges.OrderByDescending(r => r.StartOffset))
            {
                if (r.Length > 0)
                {
                    doc.Replace(r.StartOffset, r.Length, text);
                }
                else
                {
                    doc.Insert(r.StartOffset, text);
                }
            }
        }

        // Recalculate new caret positions ascending
        int cumulativeShift = 0;
        var sortedAsc = allRanges.OrderBy(r => r.StartOffset).ToList();
        var newSecondary = new List<EditorSelectionRange>();

        for (int i = 0; i < sortedAsc.Count; i++)
        {
            var r = sortedAsc[i];
            int newStart = r.StartOffset + cumulativeShift;
            int delta = text.Length - r.Length;
            int newCaret = newStart + text.Length;
            cumulativeShift += delta;

            if (i == 0)
            {
                editor.CaretOffset = newCaret;
                editor.SelectionLength = 0;
            }
            else
            {
                newSecondary.Add(new EditorSelectionRange(newCaret, 0));
            }
        }

        _secondarySelections.Clear();
        _secondarySelections.AddRange(newSecondary);
        SelectionsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Handles backspace across all selections/carets simultaneously.
    /// </summary>
    public bool HandleBackspace(TextEditor editor)
    {
        if (!HasSecondarySelections || editor.Document == null) return false;

        var allRanges = GetAllRanges(editor);
        var doc = editor.Document;

        using (doc.RunUpdate())
        {
            foreach (var r in allRanges.OrderByDescending(r => r.StartOffset))
            {
                if (r.Length > 0)
                {
                    doc.Remove(r.StartOffset, r.Length);
                }
                else if (r.StartOffset > 0)
                {
                    doc.Remove(r.StartOffset - 1, 1);
                }
            }
        }

        int cumulativeShift = 0;
        var sortedAsc = allRanges.OrderBy(r => r.StartOffset).ToList();
        var newSecondary = new List<EditorSelectionRange>();

        for (int i = 0; i < sortedAsc.Count; i++)
        {
            var r = sortedAsc[i];
            int newCaret;
            int delta;

            if (r.Length > 0)
            {
                newCaret = r.StartOffset + cumulativeShift;
                delta = -r.Length;
            }
            else if (r.StartOffset > 0)
            {
                newCaret = r.StartOffset - 1 + cumulativeShift;
                delta = -1;
            }
            else
            {
                newCaret = 0;
                delta = 0;
            }

            cumulativeShift += delta;

            if (i == 0)
            {
                editor.CaretOffset = Math.Max(0, newCaret);
                editor.SelectionLength = 0;
            }
            else
            {
                newSecondary.Add(new EditorSelectionRange(Math.Max(0, newCaret), 0));
            }
        }

        _secondarySelections.Clear();
        _secondarySelections.AddRange(newSecondary);
        SelectionsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Handles delete across all selections/carets simultaneously.
    /// </summary>
    public bool HandleDelete(TextEditor editor)
    {
        if (!HasSecondarySelections || editor.Document == null) return false;

        var allRanges = GetAllRanges(editor);
        var doc = editor.Document;

        using (doc.RunUpdate())
        {
            foreach (var r in allRanges.OrderByDescending(r => r.StartOffset))
            {
                if (r.Length > 0)
                {
                    doc.Remove(r.StartOffset, r.Length);
                }
                else if (r.StartOffset < doc.TextLength)
                {
                    doc.Remove(r.StartOffset, 1);
                }
            }
        }

        int cumulativeShift = 0;
        var sortedAsc = allRanges.OrderBy(r => r.StartOffset).ToList();
        var newSecondary = new List<EditorSelectionRange>();

        for (int i = 0; i < sortedAsc.Count; i++)
        {
            var r = sortedAsc[i];
            int newCaret = r.StartOffset + cumulativeShift;
            int delta = r.Length > 0 ? -r.Length : (r.StartOffset < doc.TextLength ? -1 : 0);
            cumulativeShift += delta;

            if (i == 0)
            {
                editor.CaretOffset = Math.Min(doc.TextLength, newCaret);
                editor.SelectionLength = 0;
            }
            else
            {
                newSecondary.Add(new EditorSelectionRange(Math.Min(doc.TextLength, newCaret), 0));
            }
        }

        _secondarySelections.Clear();
        _secondarySelections.AddRange(newSecondary);
        SelectionsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private List<EditorSelectionRange> GetAllRanges(TextEditor editor)
    {
        var list = new List<EditorSelectionRange>
        {
            new(editor.SelectionStart, editor.SelectionLength)
        };
        list.AddRange(_secondarySelections);
        return list;
    }

    public static EditorSelectionRange GetWordAtOffset(TextDocument doc, int offset)
    {
        if (doc == null || doc.TextLength == 0 || offset < 0 || offset > doc.TextLength)
            return new EditorSelectionRange(offset, 0);

        int start = Math.Min(offset, doc.TextLength - 1);
        if (start < 0) return new EditorSelectionRange(0, 0);

        // If cursor is at whitespace, check character immediately before
        if (!IsWordChar(doc.GetCharAt(start)) && start > 0 && IsWordChar(doc.GetCharAt(start - 1)))
        {
            start--;
        }

        if (!IsWordChar(doc.GetCharAt(start)))
            return new EditorSelectionRange(offset, 0);

        int wordStart = start;
        while (wordStart > 0 && IsWordChar(doc.GetCharAt(wordStart - 1)))
        {
            wordStart--;
        }

        int wordEnd = start;
        while (wordEnd < doc.TextLength && IsWordChar(doc.GetCharAt(wordEnd)))
        {
            wordEnd++;
        }

        return new EditorSelectionRange(wordStart, wordEnd - wordStart);
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}

