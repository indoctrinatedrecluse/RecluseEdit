using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Implements smart semantic expand and shrink selection (Shift+Alt+Right / Shift+Alt+Left),
/// expanding selections outward through words, quotes, bracket blocks, lines, and scopes.
/// </summary>
public class SemanticSelectionService
{
    private readonly Stack<EditorSelectionRange> _history = new();

    public void ClearHistory() => _history.Clear();

    /// <summary>
    /// Expands the current selection outwards to the next semantic scope boundary.
    /// </summary>
    public void ExpandSelection(TextEditor editor)
    {
        var doc = editor.Document;
        if (doc == null || doc.TextLength == 0) return;

        int currentStart = editor.SelectionStart;
        int currentLength = editor.SelectionLength;

        var nextRange = CalculateExpandedRange(doc, currentStart, currentLength);
        if (nextRange.StartOffset != currentStart || nextRange.Length != currentLength)
        {
            _history.Push(new EditorSelectionRange(currentStart, currentLength));
            editor.Select(nextRange.StartOffset, nextRange.Length);
        }
    }

    /// <summary>
    /// Shrinks the current selection back to the previous range in the history stack.
    /// </summary>
    public void ShrinkSelection(TextEditor editor)
    {
        if (_history.Count > 0)
        {
            var prev = _history.Pop();
            if (editor.Document != null && prev.StartOffset + prev.Length <= editor.Document.TextLength)
            {
                editor.Select(prev.StartOffset, prev.Length);
            }
        }
    }

    public static EditorSelectionRange CalculateExpandedRange(TextDocument doc, int currentStart, int currentLength)
    {
        int currentEnd = currentStart + currentLength;

        // Level 1: If cursor is empty, select word under cursor
        if (currentLength == 0)
        {
            var word = MultiSelectionManager.GetWordAtOffset(doc, currentStart);
            if (word.Length > 0)
            {
                return word;
            }
        }

        // Level 2: Inside enclosing quotes ("", '', ``)
        var quoteRange = FindEnclosingQuotes(doc, currentStart, currentEnd);
        if (quoteRange.HasValue)
        {
            var (innerStart, innerLength, outerStart, outerLength) = quoteRange.Value;
            if (currentStart > innerStart || currentEnd < innerStart + innerLength)
            {
                return new EditorSelectionRange(innerStart, innerLength);
            }
            if (currentStart == innerStart && currentLength == innerLength)
            {
                return new EditorSelectionRange(outerStart, outerLength);
            }
        }

        // Level 3: Inside enclosing brackets ((), [], {}, <>)
        var bracketRange = FindEnclosingBrackets(doc, currentStart, currentEnd);
        if (bracketRange.HasValue)
        {
            var (innerStart, innerLength, outerStart, outerLength) = bracketRange.Value;
            if (currentStart > innerStart || currentEnd < innerStart + innerLength)
            {
                return new EditorSelectionRange(innerStart, innerLength);
            }
            if (currentStart == innerStart && currentLength == innerLength)
            {
                return new EditorSelectionRange(outerStart, outerLength);
            }
        }

        // Level 4: Full Line
        var startLine = doc.GetLineByOffset(currentStart);
        var endLine = doc.GetLineByOffset(Math.Min(doc.TextLength, Math.Max(currentStart, currentEnd - 1)));
        int lineStart = startLine.Offset;
        int lineEnd = endLine.Offset + endLine.Length;

        if (currentStart > lineStart || currentEnd < lineEnd)
        {
            return new EditorSelectionRange(lineStart, lineEnd - lineStart);
        }

        // Level 5: Whole Document
        if (currentStart > 0 || currentLength < doc.TextLength)
        {
            return new EditorSelectionRange(0, doc.TextLength);
        }

        return new EditorSelectionRange(currentStart, currentLength);
    }

    private static (int InnerStart, int InnerLength, int OuterStart, int OuterLength)? FindEnclosingQuotes(TextDocument doc, int start, int end)
    {
        var line = doc.GetLineByOffset(start);
        var lineText = doc.GetText(line.Offset, line.Length);
        int localStart = start - line.Offset;
        int localEnd = end - line.Offset;

        char[] quoteChars = ['"', '\'', '`'];
        foreach (char q in quoteChars)
        {
            // Find quote before localStart
            int qStart = -1;
            for (int i = localStart - 1; i >= 0; i--)
            {
                if (lineText[i] == q && (i == 0 || lineText[i - 1] != '\\'))
                {
                    qStart = i;
                    break;
                }
            }

            if (qStart == -1) continue;

            // Find matching closing quote after localEnd
            int qEnd = -1;
            for (int i = Math.Max(qStart + 1, localEnd); i < lineText.Length; i++)
            {
                if (lineText[i] == q && lineText[i - 1] != '\\')
                {
                    qEnd = i;
                    break;
                }
            }

            if (qEnd != -1)
            {
                int outerStart = line.Offset + qStart;
                int outerLength = qEnd - qStart + 1;
                int innerStart = outerStart + 1;
                int innerLength = Math.Max(0, outerLength - 2);

                return (innerStart, innerLength, outerStart, outerLength);
            }
        }

        return null;
    }

    private static (int InnerStart, int InnerLength, int OuterStart, int OuterLength)? FindEnclosingBrackets(TextDocument doc, int start, int end)
    {
        var text = doc.Text;
        int depthParen = 0, depthSquare = 0, depthCurly = 0;

        // Scan backwards for open bracket
        for (int i = start - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c == ')') depthParen++;
            else if (c == ']') depthSquare++;
            else if (c == '}') depthCurly++;
            else if (c == '(')
            {
                if (depthParen > 0) depthParen--;
                else return MatchForward(text, i, '(', ')', end);
            }
            else if (c == '[')
            {
                if (depthSquare > 0) depthSquare--;
                else return MatchForward(text, i, '[', ']', end);
            }
            else if (c == '{')
            {
                if (depthCurly > 0) depthCurly--;
                else return MatchForward(text, i, '{', '}', end);
            }
        }

        return null;
    }

    private static (int InnerStart, int InnerLength, int OuterStart, int OuterLength)? MatchForward(string text, int openOffset, char open, char close, int minEnd)
    {
        int depth = 1;
        for (int i = openOffset + 1; i < text.Length; i++)
        {
            if (text[i] == open) depth++;
            else if (text[i] == close)
            {
                depth--;
                if (depth == 0)
                {
                    if (i >= minEnd)
                    {
                        int outerStart = openOffset;
                        int outerLength = i - openOffset + 1;
                        int innerStart = openOffset + 1;
                        int innerLength = Math.Max(0, outerLength - 2);
                        return (innerStart, innerLength, outerStart, outerLength);
                    }
                    break;
                }
            }
        }
        return null;
    }
}

