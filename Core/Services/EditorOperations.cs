using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Headless service providing developer quality-of-life (QoL) editing operations,
/// line manipulation, multiline/column cursors, and text transformations.
/// All document modifications wrap operations in RunUpdate() for atomic Undo/Redo.
/// </summary>
public static class EditorOperations
{
    /// <summary>
    /// Returns the single-line comment prefix for a given language ID or file extension.
    /// </summary>
    public static string GetCommentPrefix(string? languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return "//";

        var lang = languageId.ToLowerInvariant().Trim().TrimStart('.');
        return lang switch
        {
            "python" or "py" or "ruby" or "rb" or "powershell" or "ps1" or "bash" or "sh" or "zsh"
                or "yaml" or "yml" or "r" or "perl" or "pl" or "toml" or "dockerfile" => "#",
            "lua" or "sql" => "--",
            "html" or "xml" or "xaml" or "svg" or "markdown" or "md" => "<!--",
            "css" => "/*",
            _ => "//"
        };
    }

    /// <summary>
    /// Toggles single-line comment on the current line or across all lines overlapping the selection.
    /// </summary>
    public static void ToggleLineComment(TextEditor editor, string? languageId)
    {
        if (editor.Document == null || editor.Document.LineCount == 0) return;

        var prefix = GetCommentPrefix(languageId);
        var isHtmlBlock = prefix == "<!--";
        var isCssBlock = prefix == "/*";

        int startLine;
        int endLine;

        if (editor.TextArea.Selection.IsEmpty)
        {
            startLine = editor.TextArea.Caret.Line;
            endLine = startLine;
        }
        else
        {
            var startPos = editor.TextArea.Selection.StartPosition;
            var endPos = editor.TextArea.Selection.EndPosition;
            startLine = Math.Min(startPos.Line, endPos.Line);
            endLine = Math.Max(startPos.Line, endPos.Line);

            // If selection ends at column 1 of a line, don't include that line
            if (endPos.Column == 1 && endLine > startLine)
            {
                endLine--;
            }
        }

        using (editor.Document.RunUpdate())
        {
            if (isHtmlBlock)
            {
                ToggleBlockCommentLines(editor.Document, startLine, endLine, "<!-- ", " -->");
            }
            else if (isCssBlock)
            {
                ToggleBlockCommentLines(editor.Document, startLine, endLine, "/* ", " */");
            }
            else
            {
                TogglePrefixCommentLines(editor.Document, startLine, endLine, prefix);
            }
        }
    }

    private static void TogglePrefixCommentLines(TextDocument doc, int startLine, int endLine, string prefix)
    {
        var cleanPrefix = prefix.Trim();
        bool allCommented = true;
        int nonEmptyCount = 0;

        for (int i = startLine; i <= endLine; i++)
        {
            var line = doc.GetLineByNumber(i);
            var text = doc.GetText(line.Offset, line.Length);
            var trimmed = text.TrimStart();
            if (trimmed.Length > 0)
            {
                nonEmptyCount++;
                if (!trimmed.StartsWith(cleanPrefix, StringComparison.Ordinal))
                {
                    allCommented = false;
                    break;
                }
            }
        }

        if (nonEmptyCount == 0) return;

        for (int i = startLine; i <= endLine; i++)
        {
            var line = doc.GetLineByNumber(i);
            var text = doc.GetText(line.Offset, line.Length);

            if (allCommented)
            {
                // Uncomment
                var trimmed = text.TrimStart();
                if (trimmed.StartsWith(cleanPrefix, StringComparison.Ordinal))
                {
                    int leadingSpaces = text.Length - trimmed.Length;
                    int removeLen = cleanPrefix.Length;
                    // Also remove a single following space if present
                    if (trimmed.Length > cleanPrefix.Length && trimmed[cleanPrefix.Length] == ' ')
                    {
                        removeLen++;
                    }
                    doc.Remove(line.Offset + leadingSpaces, removeLen);
                }
            }
            else
            {
                // Comment
                if (text.Trim().Length > 0 || startLine == endLine)
                {
                    int leadingSpaces = text.Length - text.TrimStart().Length;
                    doc.Insert(line.Offset + leadingSpaces, cleanPrefix + " ");
                }
            }
        }
    }

    private static void ToggleBlockCommentLines(TextDocument doc, int startLine, int endLine, string openTag, string closeTag)
    {
        var firstLine = doc.GetLineByNumber(startLine);
        var lastLine = doc.GetLineByNumber(endLine);
        var fullText = doc.GetText(firstLine.Offset, (lastLine.Offset + lastLine.Length) - firstLine.Offset);
        var trimmed = fullText.Trim();

        if (trimmed.StartsWith(openTag.Trim(), StringComparison.Ordinal) && trimmed.EndsWith(closeTag.Trim(), StringComparison.Ordinal))
        {
            // Remove block comment
            var openIdx = fullText.IndexOf(openTag.Trim(), StringComparison.Ordinal);
            var closeIdx = fullText.LastIndexOf(closeTag.Trim(), StringComparison.Ordinal);

            if (openIdx >= 0 && closeIdx >= 0)
            {
                int openLen = openTag.Trim().Length;
                if (fullText.Length > openIdx + openLen && fullText[openIdx + openLen] == ' ') openLen++;

                int closeLen = closeTag.Trim().Length;
                int closeStart = closeIdx;
                if (closeIdx > 0 && fullText[closeIdx - 1] == ' ')
                {
                    closeStart--;
                    closeLen++;
                }

                doc.Remove(firstLine.Offset + closeStart, closeLen);
                doc.Remove(firstLine.Offset + openIdx, openLen);
            }
        }
        else
        {
            // Wrap in block comment
            doc.Insert(lastLine.Offset + lastLine.Length, " " + closeTag.Trim());
            doc.Insert(firstLine.Offset, openTag.Trim() + " ");
        }
    }

    /// <summary>
    /// Moves the selected line(s) up by one line, maintaining caret and selection.
    /// </summary>
    public static void MoveLinesUp(TextEditor editor)
    {
        if (editor.Document == null) return;

        int startLine;
        int endLine;
        GetLineRange(editor, out startLine, out endLine);

        if (startLine <= 1) return;

        using (editor.Document.RunUpdate())
        {
            var prevLine = editor.Document.GetLineByNumber(startLine - 1);
            var firstLine = editor.Document.GetLineByNumber(startLine);
            var lastLine = editor.Document.GetLineByNumber(endLine);

            var prevText = editor.Document.GetText(prevLine.Offset, prevLine.Length);
            var nl = editor.Document.GetText(prevLine.Offset + prevLine.Length, prevLine.TotalLength - prevLine.Length);
            if (string.IsNullOrEmpty(nl)) nl = Environment.NewLine;

            var blockText = editor.Document.GetText(firstLine.Offset, (lastLine.Offset + lastLine.Length) - firstLine.Offset);

            int startOffset = prevLine.Offset;
            int totalLength = (lastLine.Offset + lastLine.Length) - startOffset;

            editor.Document.Replace(startOffset, totalLength, blockText + nl + prevText);
        }

        editor.TextArea.Caret.Line = Math.Max(1, startLine - 1);
        editor.ScrollTo(editor.TextArea.Caret.Line, editor.TextArea.Caret.Column);
    }

    /// <summary>
    /// Moves the selected line(s) down by one line, maintaining caret and selection.
    /// </summary>
    public static void MoveLinesDown(TextEditor editor)
    {
        if (editor.Document == null) return;

        int startLine;
        int endLine;
        GetLineRange(editor, out startLine, out endLine);

        if (endLine >= editor.Document.LineCount) return;

        using (editor.Document.RunUpdate())
        {
            var firstLine = editor.Document.GetLineByNumber(startLine);
            var lastLine = editor.Document.GetLineByNumber(endLine);
            var nextLine = editor.Document.GetLineByNumber(endLine + 1);

            var blockText = editor.Document.GetText(firstLine.Offset, (lastLine.Offset + lastLine.Length) - firstLine.Offset);
            var nl = editor.Document.GetText(lastLine.Offset + lastLine.Length, lastLine.TotalLength - lastLine.Length);
            if (string.IsNullOrEmpty(nl)) nl = Environment.NewLine;

            var nextText = editor.Document.GetText(nextLine.Offset, nextLine.Length);

            int startOffset = firstLine.Offset;
            int totalLength = (nextLine.Offset + nextLine.Length) - startOffset;

            editor.Document.Replace(startOffset, totalLength, nextText + nl + blockText);
        }

        editor.TextArea.Caret.Line = Math.Min(editor.Document.LineCount, endLine + 1);
        editor.ScrollTo(editor.TextArea.Caret.Line, editor.TextArea.Caret.Column);
    }

    /// <summary>
    /// Duplicates the current line or selection directly below.
    /// </summary>
    public static void DuplicateLinesDown(TextEditor editor)
    {
        if (editor.Document == null) return;

        using (editor.Document.RunUpdate())
        {
            if (editor.TextArea.Selection.IsEmpty)
            {
                var line = editor.Document.GetLineByNumber(editor.TextArea.Caret.Line);
                var text = editor.Document.GetText(line.Offset, line.Length);
                var nl = Environment.NewLine;

                editor.Document.Insert(line.Offset + line.Length, nl + text);
                editor.TextArea.Caret.Line++;
            }
            else
            {
                int startLine;
                int endLine;
                GetLineRange(editor, out startLine, out endLine);

                var firstLine = editor.Document.GetLineByNumber(startLine);
                var lastLine = editor.Document.GetLineByNumber(endLine);

                var text = editor.Document.GetText(firstLine.Offset, (lastLine.Offset + lastLine.Length) - firstLine.Offset);
                var nl = Environment.NewLine;

                editor.Document.Insert(lastLine.Offset + lastLine.Length, nl + text);
                editor.TextArea.Caret.Line = endLine + 1;
            }
        }
    }

    /// <summary>
    /// Duplicates the current line or selection directly above.
    /// </summary>
    public static void DuplicateLinesUp(TextEditor editor)
    {
        if (editor.Document == null) return;

        using (editor.Document.RunUpdate())
        {
            if (editor.TextArea.Selection.IsEmpty)
            {
                var line = editor.Document.GetLineByNumber(editor.TextArea.Caret.Line);
                var text = editor.Document.GetText(line.Offset, line.Length);
                var nl = Environment.NewLine;

                editor.Document.Insert(line.Offset, text + nl);
            }
            else
            {
                int startLine;
                int endLine;
                GetLineRange(editor, out startLine, out endLine);

                var firstLine = editor.Document.GetLineByNumber(startLine);
                var lastLine = editor.Document.GetLineByNumber(endLine);

                var text = editor.Document.GetText(firstLine.Offset, (lastLine.Offset + lastLine.Length) - firstLine.Offset);
                var nl = Environment.NewLine;

                editor.Document.Insert(firstLine.Offset, text + nl);
            }
        }
    }

    /// <summary>
    /// Deletes the current line or all lines overlapping the selection.
    /// </summary>
    public static void DeleteLines(TextEditor editor)
    {
        if (editor.Document == null) return;

        int startLine;
        int endLine;
        GetLineRange(editor, out startLine, out endLine);

        using (editor.Document.RunUpdate())
        {
            var firstLine = editor.Document.GetLineByNumber(startLine);
            var lastLine = editor.Document.GetLineByNumber(endLine);

            int startOffset = firstLine.Offset;
            int length = (lastLine.Offset + lastLine.TotalLength) - startOffset;

            // If we're deleting the very last line of the document and it has no newline after it,
            // also remove the preceding newline so an empty line isn't left over
            if (endLine == editor.Document.LineCount && startLine > 1)
            {
                var prevLine = editor.Document.GetLineByNumber(startLine - 1);
                int prevNlLen = prevLine.TotalLength - prevLine.Length;
                startOffset -= prevNlLen;
                length += prevNlLen;
            }

            editor.Document.Remove(startOffset, length);
        }
    }

    /// <summary>
    /// Joins the current line and the next line, replacing line break and indentation with a single space.
    /// </summary>
    public static void JoinLines(TextEditor editor)
    {
        if (editor.Document == null) return;

        int lineNum = editor.TextArea.Caret.Line;
        if (lineNum >= editor.Document.LineCount) return;

        using (editor.Document.RunUpdate())
        {
            var currentLine = editor.Document.GetLineByNumber(lineNum);
            var nextLine = editor.Document.GetLineByNumber(lineNum + 1);

            int replaceStart = currentLine.Offset + currentLine.Length;
            var nextText = editor.Document.GetText(nextLine.Offset, nextLine.Length);
            int nextLeadingSpaces = nextText.Length - nextText.TrimStart().Length;
            int replaceLen = (nextLine.Offset + nextLeadingSpaces) - replaceStart;

            editor.Document.Replace(replaceStart, replaceLen, " ");
        }
    }

    /// <summary>
    /// Transforms the selected text (or word under cursor) to UPPERCASE.
    /// </summary>
    public static void TransformToUppercase(TextEditor editor)
    {
        if (editor.Document == null) return;

        if (!editor.TextArea.Selection.IsEmpty)
        {
            var text = editor.SelectedText;
            editor.TextArea.Selection.ReplaceSelectionWithText(text.ToUpperInvariant());
        }
        else
        {
            TransformWordUnderCaret(editor, w => w.ToUpperInvariant());
        }
    }

    /// <summary>
    /// Transforms the selected text (or word under cursor) to lowercase.
    /// </summary>
    public static void TransformToLowercase(TextEditor editor)
    {
        if (editor.Document == null) return;

        if (!editor.TextArea.Selection.IsEmpty)
        {
            var text = editor.SelectedText;
            editor.TextArea.Selection.ReplaceSelectionWithText(text.ToLowerInvariant());
        }
        else
        {
            TransformWordUnderCaret(editor, w => w.ToLowerInvariant());
        }
    }

    private static void TransformWordUnderCaret(TextEditor editor, Func<string, string> transform)
    {
        var offset = editor.CaretOffset;
        var doc = editor.Document;
        if (doc == null || doc.TextLength == 0) return;

        int start = offset;
        while (start > 0 && char.IsLetterOrDigit(doc.GetCharAt(start - 1)))
        {
            start--;
        }

        int end = offset;
        while (end < doc.TextLength && char.IsLetterOrDigit(doc.GetCharAt(end)))
        {
            end++;
        }

        if (end > start)
        {
            var word = doc.GetText(start, end - start);
            doc.Replace(start, end - start, transform(word));
        }
    }

    /// <summary>
    /// Sorts selected lines alphabetically ascending.
    /// </summary>
    public static void SortLines(TextEditor editor)
    {
        if (editor.Document == null) return;

        int startLine;
        int endLine;
        GetLineRange(editor, out startLine, out endLine);

        if (endLine <= startLine)
        {
            // If nothing selected, sort entire document
            startLine = 1;
            endLine = editor.Document.LineCount;
        }

        if (endLine <= startLine) return;

        using (editor.Document.RunUpdate())
        {
            var lines = new List<string>();
            for (int i = startLine; i <= endLine; i++)
            {
                var l = editor.Document.GetLineByNumber(i);
                lines.Add(editor.Document.GetText(l.Offset, l.Length));
            }

            lines.Sort(StringComparer.OrdinalIgnoreCase);

            var firstLine = editor.Document.GetLineByNumber(startLine);
            var lastLine = editor.Document.GetLineByNumber(endLine);

            var nl = Environment.NewLine;
            var sortedBlock = string.Join(nl, lines);

            int startOffset = firstLine.Offset;
            int length = (lastLine.Offset + lastLine.Length) - startOffset;

            editor.Document.Replace(startOffset, length, sortedBlock);
        }
    }

    /// <summary>
    /// Trims trailing whitespace from every line in the document.
    /// </summary>
    public static void TrimTrailingWhitespace(TextEditor editor)
    {
        if (editor.Document == null) return;

        using (editor.Document.RunUpdate())
        {
            for (int i = 1; i <= editor.Document.LineCount; i++)
            {
                var line = editor.Document.GetLineByNumber(i);
                var text = editor.Document.GetText(line.Offset, line.Length);
                int trimmedLen = text.TrimEnd().Length;

                if (trimmedLen < text.Length)
                {
                    int removeCount = text.Length - trimmedLen;
                    editor.Document.Remove(line.Offset + trimmedLen, removeCount);
                }
            }
        }
    }

    /// <summary>
    /// Navigates to a specific line and optional column, clamping to document bounds.
    /// </summary>
    public static void GoToLine(TextEditor editor, int lineNumber, int columnNumber = 1)
    {
        if (editor.Document == null || editor.Document.LineCount == 0) return;

        int targetLine = Math.Clamp(lineNumber, 1, editor.Document.LineCount);
        var line = editor.Document.GetLineByNumber(targetLine);
        int targetCol = Math.Clamp(columnNumber, 1, line.Length + 1);

        editor.CaretOffset = line.Offset + (targetCol - 1);
        editor.TextArea.Caret.Line = targetLine;
        editor.TextArea.Caret.Column = targetCol;
        editor.ScrollTo(targetLine, targetCol);
        editor.Focus();
    }

    /// <summary>
    /// Extends or creates a rectangular / column selection down by one line.
    /// </summary>
    public static void AddColumnCursorDown(TextEditor editor)
    {
        if (editor.Document == null) return;

        var textArea = editor.TextArea;
        if (textArea.Selection is RectangleSelection rectSel)
        {
            var start = rectSel.StartPosition;
            var end = rectSel.EndPosition;
            int top = Math.Min(start.Line, end.Line);
            int bottom = Math.Max(start.Line, end.Line);
            int nextLine = bottom + 1;

            if (nextLine <= editor.Document.LineCount)
            {
                var newStart = new TextViewPosition(top, start.Column, start.VisualColumn);
                var newEnd = new TextViewPosition(nextLine, end.Column, end.VisualColumn);
                textArea.Selection = new RectangleSelection(textArea, newStart, newEnd);
                textArea.Caret.Position = newEnd;
            }
        }
        else
        {
            var pos = textArea.Caret.Position;
            if (pos.Line < editor.Document.LineCount)
            {
                var newEnd = new TextViewPosition(pos.Line + 1, pos.Column, pos.VisualColumn);
                textArea.Selection = new RectangleSelection(textArea, pos, newEnd);
                textArea.Caret.Position = newEnd;
            }
        }
    }

    /// <summary>
    /// Extends or creates a rectangular / column selection up by one line.
    /// </summary>
    public static void AddColumnCursorUp(TextEditor editor)
    {
        if (editor.Document == null) return;

        var textArea = editor.TextArea;
        if (textArea.Selection is RectangleSelection rectSel)
        {
            var start = rectSel.StartPosition;
            var end = rectSel.EndPosition;
            int top = Math.Min(start.Line, end.Line);
            int bottom = Math.Max(start.Line, end.Line);
            int prevLine = top - 1;

            if (prevLine >= 1)
            {
                var newStart = new TextViewPosition(prevLine, start.Column, start.VisualColumn);
                var newEnd = new TextViewPosition(bottom, end.Column, end.VisualColumn);
                textArea.Selection = new RectangleSelection(textArea, newStart, newEnd);
                textArea.Caret.Position = newStart;
            }
        }
        else
        {
            var pos = textArea.Caret.Position;
            if (pos.Line > 1)
            {
                var newStart = new TextViewPosition(pos.Line - 1, pos.Column, pos.VisualColumn);
                textArea.Selection = new RectangleSelection(textArea, newStart, pos);
                textArea.Caret.Position = newStart;
            }
        }
    }

    private static void GetLineRange(TextEditor editor, out int startLine, out int endLine)
    {
        if (editor.TextArea.Selection.IsEmpty)
        {
            startLine = editor.TextArea.Caret.Line;
            endLine = startLine;
        }
        else
        {
            var startPos = editor.TextArea.Selection.StartPosition;
            var endPos = editor.TextArea.Selection.EndPosition;
            startLine = Math.Min(startPos.Line, endPos.Line);
            endLine = Math.Max(startPos.Line, endPos.Line);

            if (endPos.Column == 1 && endLine > startLine)
            {
                endLine--;
            }
        }
    }
}
