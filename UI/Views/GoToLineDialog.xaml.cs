using System.Windows;
using System.Windows.Input;

namespace RecluseEdit.UI.Views;

/// <summary>
/// Interaction logic for GoToLineDialog.xaml
/// </summary>
public partial class GoToLineDialog : Window
{
    private readonly int _maxLines;

    public int LineNumber { get; private set; } = 1;
    public int ColumnNumber { get; private set; } = 1;

    public GoToLineDialog(int currentLine, int maxLines)
    {
        InitializeComponent();
        _maxLines = Math.Max(1, maxLines);
        TxtPrompt.Text = $"Type a line number between 1 and {_maxLines}:";
        TxtLineInput.Text = currentLine.ToString();
        TxtLineInput.SelectAll();
        Loaded += (_, _) => TxtLineInput.Focus();
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmAndClose();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        ConfirmAndClose();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ConfirmAndClose()
    {
        var input = TxtLineInput.Text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        int line = 1;
        int col = 1;

        var parts = input.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out line))
        {
            if (parts.Length > 1 && int.TryParse(parts[1], out col))
            {
                ColumnNumber = Math.Max(1, col);
            }
            else
            {
                ColumnNumber = 1;
            }

            LineNumber = Math.Clamp(line, 1, _maxLines);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show(this, "Please enter a valid line number.", "Go to Line", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtLineInput.SelectAll();
            TxtLineInput.Focus();
        }
    }
}
