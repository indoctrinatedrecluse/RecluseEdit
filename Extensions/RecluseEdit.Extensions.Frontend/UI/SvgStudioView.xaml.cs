using System.Windows;
using System.Windows.Controls;
using RecluseEdit.Extensions.Frontend.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Frontend.UI;

public partial class SvgStudioView : UserControl
{
    private readonly IWorkspaceContext? _workspaceContext;
    private SvgOptimizationResult? _lastResult;

    public SvgStudioView(IWorkspaceContext? workspaceContext = null)
    {
        InitializeComponent();
        _workspaceContext = workspaceContext;

        TxtInputSvg.Text = SampleSvg;
        RenderPreview(SampleSvg);
        PerformOptimization();
    }

    private void OnInputSvgChanged(object sender, TextChangedEventArgs e)
    {
        RenderPreview(TxtInputSvg.Text);
    }

    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        PerformOptimization();
    }

    private void OnOptimizeClick(object sender, RoutedEventArgs e)
    {
        PerformOptimization();
    }

    private void PerformOptimization()
    {
        var input = TxtInputSvg.Text;
        if (string.IsNullOrWhiteSpace(input))
        {
            TxtMetrics.Text = "No SVG markup provided.";
            TxtOutputSvg.Text = string.Empty;
            return;
        }

        var options = new SvgOptimizeOptions
        {
            StripMetadata = ChkMetadata.IsChecked == true,
            StripComments = ChkComments.IsChecked == true,
            RoundCoordinates = ChkRound.IsChecked == true,
            MinifyWhitespace = ChkMinify.IsChecked == true
        };

        _lastResult = SvgOptimizerService.OptimizeSvg(input, options);
        TxtOutputSvg.Text = _lastResult.OptimizedSvg;

        TxtMetrics.Text = $"Original: {_lastResult.OriginalSizeBytes} B | Optimized: {_lastResult.OptimizedSizeBytes} B | Saved: {_lastResult.PercentSaved:0.#}%";
    }

    private void RenderPreview(string svg)
    {
        try
        {
            var html = $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta http-equiv="X-UA-Compatible" content="IE=edge" />
              <style>
                html, body {
                  margin: 0;
                  padding: 0;
                  width: 100%;
                  height: 100%;
                  overflow: hidden;
                  display: flex;
                  align-items: center;
                  justify-content: center;
                  background-color: #141414;
                  background-image: 
                    linear-gradient(45deg, #1c1c1c 25%, transparent 25%),
                    linear-gradient(-45deg, #1c1c1c 25%, transparent 25%),
                    linear-gradient(45deg, transparent 75%, #1c1c1c 75%),
                    linear-gradient(-45deg, transparent 75%, #1c1c1c 75%);
                  background-size: 16px 16px;
                  background-position: 0 0, 0 8px, 8px -8px, -8px 0;
                }
                svg {
                  max-width: 90%;
                  max-height: 90%;
                  filter: drop-shadow(0 2px 4px rgba(0,0,0,0.5));
                }
              </style>
            </head>
            <body>
              {{svg}}
            </body>
            </html>
            """;

            WebPreview.NavigateToString(html);
        }
        catch
        {
            // Ignore WebBrowser rendering exceptions
        }
    }

    private void OnCopyJsxClick(object sender, RoutedEventArgs e)
    {
        var jsx = SvgOptimizerService.ConvertToJsx(TxtInputSvg.Text);
        TxtOutputSvg.Text = jsx;
        Clipboard.SetText(jsx);
        TxtMetrics.Text = "React JSX component copied to clipboard!";
    }

    private void OnCopyDataUriClick(object sender, RoutedEventArgs e)
    {
        var uri = SvgOptimizerService.ConvertToDataUri(TxtInputSvg.Text);
        TxtOutputSvg.Text = uri;
        Clipboard.SetText(uri);
        TxtMetrics.Text = "CSS Data URI copied to clipboard!";
    }

    private void OnCopyOutputClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtOutputSvg.Text))
        {
            Clipboard.SetText(TxtOutputSvg.Text);
            TxtMetrics.Text = "Output copied to clipboard!";
        }
    }

    private void OnLoadDocClick(object sender, RoutedEventArgs e)
    {
        var activeText = _workspaceContext?.ActiveDocumentContent;
        if (!string.IsNullOrWhiteSpace(activeText) && activeText.Contains("<svg", StringComparison.OrdinalIgnoreCase))
        {
            TxtInputSvg.Text = activeText;
            RenderPreview(activeText);
            PerformOptimization();
            TxtMetrics.Text = "Loaded SVG from active document!";
        }
        else
        {
            TxtMetrics.Text = "Active document does not contain an SVG.";
        }
    }

    private const string SampleSvg = """
    <!-- Generator: Adobe Illustrator 25.0, SVG Export Plug-In -->
    <svg version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"
         viewBox="0 0 100 100" width="80" height="80">
      <defs>
        <linearGradient id="grad1" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" style="stop-color:#007ACC;stop-opacity:1" />
          <stop offset="100%" style="stop-color:#4EC9B0;stop-opacity:1" />
        </linearGradient>
      </defs>
      <circle cx="50.0001" cy="50.0001" r="42.5000" fill="url(#grad1)" stroke="#3F3F46" stroke-width="3.0000" />
      <polygon points="50,22 61.2,44.7 86.4,48.4 68.2,66.1 72.5,91.2 50,79.4 27.5,91.2 31.8,66.1 13.6,48.4 38.8,44.7" fill="#FFFFFF" />
    </svg>
    """;
}

