using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using RecluseEdit.Core.Models;

namespace RecluseEdit.UI.Controls;

public class GitDiffMargin : AbstractMargin
{
    private static readonly SolidColorBrush AddedBrush = CreateFrozenBrush(Color.FromRgb(0x2E, 0xA0, 0x43));      // #2EA043
    private static readonly SolidColorBrush ModifiedBrush = CreateFrozenBrush(Color.FromRgb(0x00, 0x7A, 0xCC));   // #007ACC
    private static readonly SolidColorBrush DeletedBrush = CreateFrozenBrush(Color.FromRgb(0xF8, 0x51, 0x49));    // #F85149

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private List<DiffHunk> _hunks = [];

    public List<DiffHunk> Hunks
    {
        get => _hunks;
        set
        {
            _hunks = value ?? [];
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(4, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var textView = TextView;
        if (textView == null || !textView.VisualLinesValid || _hunks.Count == 0)
        {
            return;
        }

        foreach (var visualLine in textView.VisualLines)
        {
            int lineNumber = visualLine.FirstDocumentLine.LineNumber;
            double y = visualLine.VisualTop - textView.ScrollOffset.Y;
            double height = visualLine.Height;

            var hunk = FindHunkForLine(lineNumber);
            if (hunk != null)
            {
                var brush = hunk.Type switch
                {
                    DiffHunkType.Added => AddedBrush,
                    DiffHunkType.Modified => ModifiedBrush,
                    DiffHunkType.Deleted => DeletedBrush,
                    _ => ModifiedBrush
                };

                if (hunk.Type == DiffHunkType.Deleted)
                {
                    // For deleted hunks, draw a compact red wedge at the top boundary of the following line
                    var triangle = new StreamGeometry();
                    using (var ctx = triangle.Open())
                    {
                        ctx.BeginFigure(new Point(0, y), true, true);
                        ctx.LineTo(new Point(4, y), true, false);
                        ctx.LineTo(new Point(0, y + 4), true, false);
                    }
                    triangle.Freeze();
                    drawingContext.DrawGeometry(brush, null, triangle);
                }
                else
                {
                    drawingContext.DrawRectangle(brush, null, new Rect(0.5, y, 3, height));
                }
            }
        }
    }

    private DiffHunk? FindHunkForLine(int line)
    {
        for (int i = 0; i < _hunks.Count; i++)
        {
            var h = _hunks[i];
            if (h.Type == DiffHunkType.Deleted)
            {
                // In git diff, a deletion before/at newStartLine
                if (line == h.NewStartLine || (h.NewStartLine == 0 && line == 1))
                {
                    return h;
                }
            }
            else
            {
                if (line >= h.NewStartLine && line < h.NewStartLine + h.NewLineCount)
                {
                    return h;
                }
            }
        }
        return null;
    }
}

