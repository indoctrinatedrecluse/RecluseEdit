using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Interaction logic for FindReplaceControl.xaml
/// </summary>
public partial class FindReplaceControl : UserControl
{
    private TextEditor? _editor;

    public TextEditor? Editor
    {
        get => _editor;
        set => _editor = value;
    }

    public event Action? CloseRequested;

    public FindReplaceControl()
    {
        InitializeComponent();
    }

    public void ShowFind(string? initialText = null)
    {
        Visibility = Visibility.Visible;
        ReplaceRow.Visibility = Visibility.Collapsed;
        BtnToggleReplace.Content = "▶";

        if (!string.IsNullOrEmpty(initialText))
        {
            TxtFind.Text = initialText;
            TxtFind.SelectAll();
        }

        TxtFind.Focus();
        UpdateMatchesCount();
    }

    public void ShowReplace(string? initialText = null)
    {
        Visibility = Visibility.Visible;
        ReplaceRow.Visibility = Visibility.Visible;
        BtnToggleReplace.Content = "▼";

        if (!string.IsNullOrEmpty(initialText))
        {
            TxtFind.Text = initialText;
            TxtFind.SelectAll();
        }

        TxtFind.Focus();
        UpdateMatchesCount();
    }

    private void OnToggleReplaceClick(object sender, RoutedEventArgs e)
    {
        if (ReplaceRow.Visibility == Visibility.Visible)
        {
            ReplaceRow.Visibility = Visibility.Collapsed;
            BtnToggleReplace.Content = "▶";
        }
        else
        {
            ReplaceRow.Visibility = Visibility.Visible;
            BtnToggleReplace.Content = "▼";
            TxtReplace.Focus();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        CloseRequested?.Invoke();
        _editor?.Focus();
    }

    private void OnFindTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMatchesCount();
    }

    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        UpdateMatchesCount();
    }

    private void OnFindKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                FindPrevious();
            }
            else
            {
                FindNext();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            OnCloseClick(sender, e);
            e.Handled = true;
        }
    }

    private void OnReplaceKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ReplaceNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            OnCloseClick(sender, e);
            e.Handled = true;
        }
    }

    private void OnNextMatchClick(object sender, RoutedEventArgs e) => FindNext();
    private void OnPrevMatchClick(object sender, RoutedEventArgs e) => FindPrevious();
    private void OnReplaceClick(object sender, RoutedEventArgs e) => ReplaceNext();
    private void OnReplaceAllClick(object sender, RoutedEventArgs e) => ReplaceAll();

    private void UpdateMatchesCount()
    {
        if (_editor == null || string.IsNullOrEmpty(TxtFind.Text))
        {
            TxtMatchCount.Text = "No results";
            return;
        }

        var count = GetMatches().Count;
        TxtMatchCount.Text = count > 0 ? $"{count} found" : "No results";
    }

    private List<Match> GetMatches()
    {
        if (_editor == null || string.IsNullOrEmpty(TxtFind.Text)) return [];

        var text = _editor.Document.Text;
        var pattern = Regex.Escape(TxtFind.Text);
        var options = RegexOptions.None;

        if (ToggleMatchCase.IsChecked != true)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return Regex.Matches(text, pattern, options).ToList();
    }

    public bool FindNext()
    {
        if (_editor == null || string.IsNullOrEmpty(TxtFind.Text)) return false;

        var matches = GetMatches();
        if (matches.Count == 0) return false;

        var caret = _editor.CaretOffset;
        var nextMatch = matches.FirstOrDefault(m => m.Index > caret) ?? matches[0];

        _editor.Select(nextMatch.Index, nextMatch.Length);
        _editor.ScrollToLine(_editor.Document.GetLocation(nextMatch.Index).Line);
        return true;
    }

    public bool FindPrevious()
    {
        if (_editor == null || string.IsNullOrEmpty(TxtFind.Text)) return false;

        var matches = GetMatches();
        if (matches.Count == 0) return false;

        var caret = _editor.SelectionStart;
        var prevMatch = matches.LastOrDefault(m => m.Index < caret) ?? matches[^1];

        _editor.Select(prevMatch.Index, prevMatch.Length);
        _editor.ScrollToLine(_editor.Document.GetLocation(prevMatch.Index).Line);
        return true;
    }

    public void ReplaceNext()
    {
        if (_editor == null || string.IsNullOrEmpty(TxtFind.Text)) return;

        // If currently selected text matches find text, replace it
        if (_editor.SelectionLength > 0 && string.Equals(_editor.SelectedText, TxtFind.Text,
            ToggleMatchCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            var start = _editor.SelectionStart;
            var replaceText = TxtReplace.Text ?? "";
            _editor.Document.Replace(start, _editor.SelectionLength, replaceText);
            _editor.CaretOffset = start + replaceText.Length;
        }

        FindNext();
        UpdateMatchesCount();
    }

    public void ReplaceAll()
    {
        if (_editor == null || string.IsNullOrEmpty(TxtFind.Text)) return;

        var replaceText = TxtReplace.Text ?? "";
        var matches = GetMatches();
        if (matches.Count == 0) return;

        _editor.Document.BeginUpdate();
        try
        {
            // Replace backwards so offsets remain valid
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                _editor.Document.Replace(match.Index, match.Length, replaceText);
            }
        }
        finally
        {
            _editor.Document.EndUpdate();
        }

        UpdateMatchesCount();
    }
}

