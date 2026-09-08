using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Renders inline ghost text (autocomplete suggestion) right after the caret position in faint italic text.
/// </summary>
public class GhostTextRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private string? _ghostText;
    private int _targetOffset = -1;

    public KnownLayer Layer => KnownLayer.Caret;

    public FontFamily FontFamily { get; set; } = new("Consolas, Cascadia Code, Courier New");
    public double FontSize { get; set; } = 14.0;

    public string? GhostText
    {
        get => _ghostText;
        set
        {
            if (_ghostText != value)
            {
                _ghostText = value;
                _textView.InvalidateVisual();
            }
        }
    }

    public int TargetOffset
    {
        get => _targetOffset;
        set
        {
            if (_targetOffset != value)
            {
                _targetOffset = value;
                _textView.InvalidateVisual();
            }
        }
    }

    public bool HasSuggestion => !string.IsNullOrEmpty(_ghostText) && _targetOffset >= 0;

    public GhostTextRenderer(TextView textView)
    {
        _textView = textView;
    }

    public void Clear()
    {
        if (_ghostText != null || _targetOffset != -1)
        {
            _ghostText = null;
            _targetOffset = -1;
            _textView.InvalidateVisual();
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (string.IsNullOrEmpty(_ghostText) || _targetOffset < 0 || _targetOffset > textView.Document.TextLength)
        {
            return;
        }

        try
        {
            var loc = textView.Document.GetLocation(_targetOffset);
            var position = new ICSharpCode.AvalonEdit.TextViewPosition(loc.Line, loc.Column);

            var visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineTop);
            var screenX = visualPosition.X - textView.ScrollOffset.X;
            var screenY = visualPosition.Y - textView.ScrollOffset.Y;

            // Only draw if within visible viewport
            if (screenX >= 0 && screenX < textView.ActualWidth && screenY >= -20 && screenY < textView.ActualHeight)
            {
                var brush = new SolidColorBrush(Color.FromArgb(160, 150, 150, 150));
                brush.Freeze();

                var dpi = VisualTreeHelper.GetDpi(textView).PixelsPerDip;
                var formattedText = new FormattedText(
                    _ghostText,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal),
                    FontSize,
                    brush,
                    dpi
                );

                drawingContext.DrawText(formattedText, new Point(screenX, screenY));
            }
        }
        catch
        {
            // If offset visual calculation is momentarily out of range during fast typing, safely ignore
        }
    }
}

