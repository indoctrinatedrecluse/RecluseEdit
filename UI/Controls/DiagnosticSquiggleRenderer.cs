using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using RecluseEdit.Core.Models;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Represents a text segment with an active diagnostic squiggle in AvalonEdit.
/// </summary>
public record DiagnosticMarker(
    DiagnosticItem Diagnostic,
    int StartOffset,
    int Length,
    DiagnosticSeverity Severity,
    string Message
);

/// <summary>
/// Renders live wavy squiggles under code tokens identified by DiagnosticService,
/// color-coded by severity (Red for Error, Amber for Warning, Cyan for Info/Hint).
/// </summary>
public class DiagnosticSquiggleRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private readonly List<DiagnosticMarker> _markers = [];
    private readonly object _lock = new();

    private static readonly Pen ErrorPen;
    private static readonly Pen WarningPen;
    private static readonly Pen InfoPen;

    static DiagnosticSquiggleRenderer()
    {
        // Red squiggle (#F44336)
        var errorBrush = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
        errorBrush.Freeze();
        ErrorPen = new Pen(errorBrush, 1.25);
        ErrorPen.Freeze();

        // Amber/Orange squiggle (#FFB74D)
        var warningBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB7, 0x4D));
        warningBrush.Freeze();
        WarningPen = new Pen(warningBrush, 1.25);
        WarningPen.Freeze();

        // Cyan/Blue squiggle (#4F, #C3, #F7)
        var infoBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
        infoBrush.Freeze();
        InfoPen = new Pen(infoBrush, 1.25);
        InfoPen.Freeze();
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public DiagnosticSquiggleRenderer(TextView textView)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
    }

    public void SetDiagnostics(TextDocument? document, IReadOnlyList<DiagnosticItem>? diagnostics)
    {
        lock (_lock)
        {
            _markers.Clear();

            if (document == null || diagnostics == null || diagnostics.Count == 0)
            {
                _textView.InvalidateVisual();
                return;
            }

            int docLen = document.TextLength;

            foreach (var diag in diagnostics)
            {
                if (diag.LineNumber < 1 || diag.LineNumber > document.LineCount)
                    continue;

                try
                {
                    var line = document.GetLineByNumber(diag.LineNumber);
                    int col = Math.Max(1, diag.ColumnNumber);
                    int offset = line.Offset + Math.Min(col - 1, line.Length);

                    if (offset < 0 || offset > docLen)
                        continue;

                    // Determine token span length
                    int length = 1;
                    if (offset < docLen)
                    {
                        char ch = document.GetCharAt(offset);
                        if (char.IsLetterOrDigit(ch) || ch == '_')
                        {
                            int end = offset + 1;
                            while (end < line.EndOffset && (char.IsLetterOrDigit(document.GetCharAt(end)) || document.GetCharAt(end) == '_'))
                            {
                                end++;
                            }
                            length = Math.Max(1, end - offset);
                        }
                        else if (offset + 2 < docLen && ch == '`' && document.GetCharAt(offset + 1) == '`' && document.GetCharAt(offset + 2) == '`')
                        {
                            length = 3;
                        }
                        else
                        {
                            length = 1;
                        }
                    }

                    _markers.Add(new DiagnosticMarker(
                        diag,
                        offset,
                        length,
                        diag.Severity,
                        diag.Message
                    ));
                }
                catch
                {
                    // Ignore document sync race during typing
                }
            }
        }

        _textView.InvalidateVisual();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _markers.Clear();
        }
        _textView.InvalidateVisual();
    }

    public DiagnosticMarker? GetMarkerAtOffset(int offset)
    {
        lock (_lock)
        {
            return _markers.FirstOrDefault(m => offset >= m.StartOffset && offset <= m.StartOffset + m.Length);
        }
    }

    public IReadOnlyList<DiagnosticMarker> GetMarkers()
    {
        lock (_lock)
        {
            return _markers.ToList();
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document == null) return;

        List<DiagnosticMarker> markersCopy;
        lock (_lock)
        {
            if (_markers.Count == 0) return;
            markersCopy = [.. _markers];
        }

        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0) return;

        int firstVisualOffset = visualLines.First().FirstDocumentLine.Offset;
        int lastVisualOffset = visualLines.Last().LastDocumentLine.EndOffset;

        double viewportWidth = textView.ActualWidth;
        double viewportHeight = textView.ActualHeight;

        foreach (var marker in markersCopy)
        {
            int markerStart = marker.StartOffset;
            int markerEnd = marker.StartOffset + marker.Length;

            // Only process markers visible in the current viewport range
            if (markerEnd < firstVisualOffset || markerStart > lastVisualOffset)
                continue;

            try
            {
                var locStart = textView.Document.GetLocation(markerStart);
                var locEnd = textView.Document.GetLocation(Math.Min(markerEnd, textView.Document.TextLength));

                var posStart = new TextViewPosition(locStart.Line, locStart.Column);
                var posEnd = new TextViewPosition(locEnd.Line, Math.Max(locStart.Column + 1, locEnd.Column));

                var ptStart = textView.GetVisualPosition(posStart, VisualYPosition.LineBottom);
                var ptEnd = textView.GetVisualPosition(posEnd, VisualYPosition.LineBottom);

                double startX = ptStart.X - textView.ScrollOffset.X;
                double endX = ptEnd.X - textView.ScrollOffset.X;
                double baseY = ptStart.Y - textView.ScrollOffset.Y - 1.0;

                // Ensure minimum visible width for single punctuation characters
                if (endX - startX < 8.0)
                {
                    endX = startX + 8.0;
                }

                // Check horizontal and vertical bounds
                if (endX < 0 || startX > viewportWidth || baseY < 0 || baseY > viewportHeight)
                    continue;

                Pen pen = marker.Severity switch
                {
                    DiagnosticSeverity.Error => ErrorPen,
                    DiagnosticSeverity.Warning => WarningPen,
                    _ => InfoPen
                };

                DrawSquiggle(drawingContext, pen, startX, endX, baseY);
            }
            catch
            {
                // Ignore transient visual calculation errors during document mutation
            }
        }
    }

    private static void DrawSquiggle(DrawingContext dc, Pen pen, double startX, double endX, double baseY)
    {
        if (endX <= startX) return;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(startX, baseY), false, false);
            double step = 3.0;
            double amplitude = 2.0;
            bool up = true;

            for (double x = startX; x < endX; x += step)
            {
                double nextX = Math.Min(x + step, endX);
                double y = up ? (baseY - amplitude) : (baseY + amplitude);
                ctx.LineTo(new Point(nextX, y), true, false);
                up = !up;
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}

