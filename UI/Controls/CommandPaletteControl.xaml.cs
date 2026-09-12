using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Interaction logic for CommandPaletteControl.xaml
/// </summary>
public partial class CommandPaletteControl : UserControl
{
    public event Action? Closed;

    public CommandRegistry? CommandRegistry { get; set; }

    public CommandPaletteControl()
    {
        InitializeComponent();
    }

    public void Show(string initialText = ">")
    {
        Visibility = Visibility.Visible;
        TxtSearch.Text = initialText;
        TxtSearch.CaretIndex = TxtSearch.Text.Length;
        UpdatePlaceholder();
        UpdateResults();
        TxtSearch.Focus();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        Closed?.Invoke();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder();
        UpdateModeHint();
        UpdateResults();
    }

    private void UpdatePlaceholder()
    {
        TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateModeHint()
    {
        var text = TxtSearch.Text.TrimStart();
        if (text.StartsWith('>'))
        {
            TxtModeHint.Text = "Command Mode";
        }
        else if (text.StartsWith(':'))
        {
            TxtModeHint.Text = "Go to Line";
        }
        else if (text.StartsWith('?'))
        {
            TxtModeHint.Text = "Palette Help";
        }
        else
        {
            TxtModeHint.Text = "Quick Open Files";
        }
    }

    private void UpdateResults()
    {
        if (CommandRegistry == null) return;

        var results = CommandRegistry.Search(TxtSearch.Text);
        LstResults.ItemsSource = results;

        if (results.Count > 0)
        {
            LstResults.SelectedIndex = 0;
        }
    }

    private void OnSearchPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            if (LstResults.Items.Count > 0)
            {
                int next = (LstResults.SelectedIndex + 1) % LstResults.Items.Count;
                LstResults.SelectedIndex = next;
                LstResults.ScrollIntoView(LstResults.SelectedItem);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            if (LstResults.Items.Count > 0)
            {
                int prev = (LstResults.SelectedIndex - 1 + LstResults.Items.Count) % LstResults.Items.Count;
                LstResults.SelectedIndex = prev;
                LstResults.ScrollIntoView(LstResults.SelectedItem);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
            return;
        }
    }

    private void OnListPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteSelected();
    }

    private void ExecuteSelected()
    {
        if (LstResults.SelectedItem is CommandItem selected)
        {
            Hide();
            selected.Action?.Invoke();
        }
    }
}

