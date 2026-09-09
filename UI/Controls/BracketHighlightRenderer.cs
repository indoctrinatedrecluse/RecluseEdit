using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Renders visual highlight borders around matching brackets, braces, and parentheses
/// when the caret is positioned adjacent to them.
/// </summary>
public class BracketHighlightRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private int _firstOffset = -1;
    private int _secondOffset = -1;

    private static readonly Brush FillBrush;
    private static readonly Pen BorderPen;

    static BracketHighlightRenderer()
    {
        // VS Code style subtle blue fill with vibrant accent border
        var fill = new SolidColorBrush(Color.FromArgb(70, 14, 99, 156));
        fill.Freeze();
        FillBrush = fill;

        var penBrush = new SolidColorBrush(Color.FromArgb(200, 86, 156, 214)); // #569CD6
        penBrush.Freeze();
        var pen = new Pen(penBrush, 1);
        pen.Freeze();
        BorderPen = pen;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public BracketHighlightRenderer(TextView textView)
    {
        _textView = textView;
    }

    public void SetBracketMatch(int firstOffset, int secondOffset)
    {
        if (_firstOffset != firstOffset || _secondOffset != secondOffset)
        {
            _firstOffset = firstOffset;
            _secondOffset = secondOffset;
            _textView.InvalidateVisual();
        }
    }

    public void Clear()
    {
        SetBracketMatch(-1, -1);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_firstOffset < 0 || _secondOffset < 0) return;
        if (textView.Document == null) return;

        DrawBracketBox(textView, drawingContext, _firstOffset);
        DrawBracketBox(textView, drawingContext, _secondOffset);
    }

    private static void DrawBracketBox(TextView textView, DrawingContext drawingContext, int offset)
    {
        if (offset < 0 || offset >= textView.Document.TextLength) return;

        try
        {
            var loc = textView.Document.GetLocation(offset);
            var pos = new TextViewPosition(loc.Line, loc.Column);
            var nextPos = new TextViewPosition(loc.Line, loc.Column + 1);

            var top = textView.GetVisualPosition(pos, VisualYPosition.LineTop);
            var bottom = textView.GetVisualPosition(pos, VisualYPosition.LineBottom);
            var nextTop = textView.GetVisualPosition(nextPos, VisualYPosition.LineTop);

            var screenX = top.X - textView.ScrollOffset.X;
            var screenY = top.Y - textView.ScrollOffset.Y;
            var width = Math.Max(8, nextTop.X - top.X);
            var height = bottom.Y - top.Y;

            // Only draw if within visible viewport
            if (screenX + width >= 0 && screenX < textView.ActualWidth &&
                screenY + height >= 0 && screenY < textView.ActualHeight)
            {
                var rect = new Rect(screenX, screenY, width, height);
                drawingContext.DrawRectangle(FillBrush, BorderPen, rect);
            }
        }
        catch
        {
            // Safely ignore out-of-bounds visual calculations during editing
        }
    }

    /// <summary>
    /// Searches for a matching bracket pair at or immediately preceding the specified caret offset.
    /// </summary>
    public static (int OpenOffset, int CloseOffset)? FindMatchingBrackets(TextDocument document, int caretOffset)
    {
        if (document.TextLength == 0) return null;

        // 1. Check right of caret (character at caretOffset)
        if (caretOffset < document.TextLength)
        {
            var ch = document.GetCharAt(caretOffset);
            var match = SearchBracket(document, caretOffset, ch);
            if (match.HasValue) return match;
        }

        // 2. Check left of caret (character at caretOffset - 1)
        if (caretOffset > 0)
        {
            var ch = document.GetCharAt(caretOffset - 1);
            var match = SearchBracket(document, caretOffset - 1, ch);
            if (match.HasValue) return match;
        }

        return null;
    }

    private static (int OpenOffset, int CloseOffset)? SearchBracket(TextDocument doc, int offset, char ch)
    {
        const int maxSearchDistance = 25000;

        char open;
        char close;
        bool forward;

        switch (ch)
        {
            case '(': open = '('; close = ')'; forward = true; break;
            case '[': open = '['; close = ']'; forward = true; break;
            case '{': open = '{'; close = '}'; forward = true; break;

            case ')': open = '('; close = ')'; forward = false; break;
            case ']': open = '['; close = ']'; forward = false; break;
            case '}': open = '{'; close = '}'; forward = false; break;

            default:
                return null;
        }

        if (forward)
        {
            var depth = 1;
            var limit = Math.Min(doc.TextLength, offset + maxSearchDistance);
            for (var i = offset + 1; i < limit; i++)
            {
                var cur = doc.GetCharAt(i);
                if (cur == open) depth++;
                else if (cur == close)
                {
                    depth--;
                    if (depth == 0) return (offset, i);
                }
            }
        }
        else
        {
            var depth = 1;
            var limit = Math.Max(0, offset - maxSearchDistance);
            for (var i = offset - 1; i >= limit; i--)
            {
                var cur = doc.GetCharAt(i);
                if (cur == close) depth++;
                else if (cur == open)
                {
                    depth--;
                    if (depth == 0) return (i, offset);
                }
            }
        }

        return null;
    }
}

