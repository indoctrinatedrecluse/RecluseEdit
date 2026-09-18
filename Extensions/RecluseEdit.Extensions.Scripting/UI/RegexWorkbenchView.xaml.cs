using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecluseEdit.Extensions.Scripting.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Scripting.UI;

public partial class RegexWorkbenchView : UserControl
{
    private readonly IWorkspaceContext? _workspaceContext;
    private RegexEvalResult? _lastResult;

    public RegexWorkbenchView(IWorkspaceContext? workspaceContext = null)
    {
        InitializeComponent();
        _workspaceContext = workspaceContext;

        var presets = RegexWorkbenchService.GetPresets();
        ComboPresets.ItemsSource = presets;
        if (presets.Count > 0)
        {
            ComboPresets.SelectedIndex = 0;
        }

        EvaluateRegex();
    }

    private void OnPresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboPresets.SelectedItem is RegexPreset preset)
        {
            TxtPattern.Text = preset.Pattern;
            TxtInput.Text = preset.SampleText;
            EvaluateRegex();
        }
    }

    private void OnPatternOrInputChanged(object sender, TextChangedEventArgs e)
    {
        EvaluateRegex();
    }

    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        EvaluateRegex();
    }

    private void EvaluateRegex()
    {
        if (TxtPattern == null || TxtInput == null) return;

        var pattern = TxtPattern.Text;
        var input = TxtInput.Text;
        var replacement = TxtReplacementPattern?.Text;

        var options = RegexOptions.None;
        if (ChkIgnoreCase.IsChecked == true) options |= RegexOptions.IgnoreCase;
        if (ChkMultiline.IsChecked == true) options |= RegexOptions.Multiline;
        if (ChkSingleline.IsChecked == true) options |= RegexOptions.Singleline;
        if (ChkIgnoreWhitespace.IsChecked == true) options |= RegexOptions.IgnorePatternWhitespace;

        _lastResult = RegexWorkbenchService.Evaluate(pattern, input, options, replacement);

        if (_lastResult.IsSuccess)
        {
            GridMatches.ItemsSource = _lastResult.AllGroups;
            TxtReplacementOutput.Text = _lastResult.ReplacementOutput;
            TxtMetrics.Text = $"Matches: {_lastResult.Matches.Count} ({_lastResult.ElapsedMs} ms)";
            TxtMetrics.Foreground = new SolidColorBrush(Color.FromRgb(78, 201, 176));
        }
        else
        {
            GridMatches.ItemsSource = null;
            TxtReplacementOutput.Text = string.Empty;
            TxtMetrics.Text = _lastResult.ErrorMessage ?? "Regex evaluation error.";
            TxtMetrics.Foreground = new SolidColorBrush(Color.FromRgb(244, 135, 113));
        }
    }

    private void OnCopyMatchesClick(object sender, RoutedEventArgs e)
    {
        if (_lastResult?.Matches.Count > 0)
        {
            var matchesText = string.Join(Environment.NewLine, _lastResult.Matches.Select(m => m.Value));
            Clipboard.SetText(matchesText);
            TxtMetrics.Text = $"Copied {_lastResult.Matches.Count} match(es) to clipboard!";
        }
    }

    private void OnCopyReplacedClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtReplacementOutput.Text))
        {
            Clipboard.SetText(TxtReplacementOutput.Text);
            TxtMetrics.Text = "Copied replaced output to clipboard!";
        }
    }
}

