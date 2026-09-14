using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Renders visual selection highlights and secondary cursor lines for all
/// active secondary carets managed by MultiSelectionManager.
/// </summary>
public class MultiCaretRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private readonly MultiSelectionManager _manager;

    private Brush _selectionBrush;
    private Pen _caretPen;

    public KnownLayer Layer => KnownLayer.Selection;

    public MultiCaretRenderer(TextView textView, MultiSelectionManager manager)
    {
        _textView = textView;
        _manager = manager;

        // Default VS Code style selection and caret colors
        var selBrush = new SolidColorBrush(Color.FromArgb(100, 38, 79, 120));
        selBrush.Freeze();
        _selectionBrush = selBrush;

        var caretBrush = new SolidColorBrush(Color.FromArgb(240, 212, 212, 212));
        caretBrush.Freeze();
        var pen = new Pen(caretBrush, 1.5);
        pen.Freeze();
        _caretPen = pen;

        _manager.SelectionsChanged += (_, _) => _textView.InvalidateVisual();
    }

    public void UpdateColors(Brush? selectionBrush, Brush? caretBrush)
    {
        if (selectionBrush != null)
        {
            _selectionBrush = selectionBrush;
        }

        if (caretBrush != null)
        {
            var pen = new Pen(caretBrush, 1.5);
            pen.Freeze();
            _caretPen = pen;
        }

        _textView.InvalidateVisual();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!_manager.HasSecondarySelections || textView.Document == null) return;

        foreach (var range in _manager.SecondarySelections)
        {
            // 1. Draw selection highlight if range has length
            if (range.Length > 0 && range.StartOffset + range.Length <= textView.Document.TextLength)
            {
                try
                {
                    var segment = new TextSegment { StartOffset = range.StartOffset, Length = range.Length };
                    var geoBuilder = new BackgroundGeometryBuilder();
                    geoBuilder.AddSegment(textView, segment);
                    var geo = geoBuilder.CreateGeometry();
                    if (geo != null)
                    {
                        drawingContext.DrawGeometry(_selectionBrush, null, geo);
                    }
                }
                catch
                {
                    // Ignore layout calculation during rapid typing
                }
            }

            // 2. Draw caret line at the end/cursor position of the range
            int caretOffset = range.EndOffset;
            if (caretOffset >= 0 && caretOffset <= textView.Document.TextLength)
            {
                try
                {
                    var loc = textView.Document.GetLocation(caretOffset);
                    var pos = new ICSharpCode.AvalonEdit.TextViewPosition(loc.Line, loc.Column);
                    var top = textView.GetVisualPosition(pos, VisualYPosition.LineTop);
                    var bottom = textView.GetVisualPosition(pos, VisualYPosition.LineBottom);

                    var screenX = top.X - textView.ScrollOffset.X;
                    var screenY = top.Y - textView.ScrollOffset.Y;
                    var height = Math.Max(12, bottom.Y - top.Y);

                    if (screenX >= -5 && screenX <= textView.ActualWidth + 5 &&
                        screenY + height >= -5 && screenY <= textView.ActualHeight + 5)
                    {
                        drawingContext.DrawLine(_caretPen, new Point(screenX, screenY), new Point(screenX, screenY + height));
                    }
                }
                catch
                {
                    // Ignore layout calculation during rapid typing
                }
            }
        }
    }
}

