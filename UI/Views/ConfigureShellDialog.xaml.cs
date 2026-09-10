using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Views;

public partial class ConfigureShellDialog : Window
{
    private readonly ShellInfo _shell;

    public string VerifiedPath { get; private set; } = string.Empty;

    public ConfigureShellDialog(ShellInfo shell, Window? owner = null)
    {
        InitializeComponent();
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));

        if (owner != null)
        {
            Owner = owner;
        }

        TxtShellIcon.Text = shell.Icon;
        TxtShellTitle.Text = $"Configure {shell.DisplayName}";
        TxtShellDescription.Text = string.IsNullOrWhiteSpace(shell.Description)
            ? $"Specify the path to the {shell.DisplayName} executable on your system. RecluseEdit will verify it before activating."
            : shell.Description;

        TxtExpectedBinaries.Text = shell.ExpectedBinaryNames is { Length: > 0 }
            ? string.Join(", ", shell.ExpectedBinaryNames)
            : "(Any executable binary)";

        TxtStartupArgs.Text = string.IsNullOrWhiteSpace(shell.Arguments)
            ? "(None)"
            : shell.Arguments;

        TxtExecutablePath.Text = shell.ExecutablePath ?? string.Empty;
        TxtExecutablePath.CaretIndex = TxtExecutablePath.Text.Length;
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var initialDir = string.Empty;
        if (!string.IsNullOrWhiteSpace(TxtExecutablePath.Text))
        {
            try
            {
                var dir = Path.GetDirectoryName(TxtExecutablePath.Text);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    initialDir = dir;
                }
            }
            catch { }
        }

        var dlg = new OpenFileDialog
        {
            Title = $"Select {_shell.DisplayName} Executable",
            Filter = "Executable Files (*.exe;*.cmd;*.bat)|*.exe;*.cmd;*.bat|All Files (*.*)|*.*",
            InitialDirectory = initialDir
        };

        if (dlg.ShowDialog(this) == true)
        {
            TxtExecutablePath.Text = dlg.FileName;
            TxtExecutablePath.CaretIndex = TxtExecutablePath.Text.Length;
        }
    }

    private async void OnVerifyAndSaveClick(object sender, RoutedEventArgs e)
    {
        var rawPath = TxtExecutablePath.Text?.Trim() ?? string.Empty;

        BtnVerifyAndSave.IsEnabled = false;
        ShowStatus("⏳", "Verifying executable...", Color.FromRgb(0x1E, 0x3A, 0x5F), Color.FromRgb(0x2B, 0x5B, 0x94));

        var result = await ShellVerifier.VerifyAsync(_shell, rawPath);

        if (result.IsValid)
        {
            ShowStatus("✅", result.Message, Color.FromRgb(0x1B, 0x5E, 0x20), Color.FromRgb(0x2E, 0x7D, 0x32));
            VerifiedPath = Environment.ExpandEnvironmentVariables(rawPath.Trim('\"', ' '));

            await Task.Delay(400);
            DialogResult = true;
            Close();
        }
        else
        {
            ShowStatus("❌", result.Message, Color.FromRgb(0x5A, 0x1D, 0x1D), Color.FromRgb(0x8B, 0x2E, 0x2E));
            BtnVerifyAndSave.IsEnabled = true;
        }
    }

    private void ShowStatus(string icon, string message, Color bg, Color border)
    {
        StatusBorder.Background = new SolidColorBrush(bg);
        StatusBorder.BorderBrush = new SolidColorBrush(border);
        StatusBorder.BorderThickness = new Thickness(1);
        TxtStatusIcon.Text = icon;
        TxtStatusMessage.Text = message;
        StatusBorder.Visibility = Visibility.Visible;
    }

    private void OnExecutablePathChanged(object sender, TextChangedEventArgs e)
    {
        StatusBorder.Visibility = Visibility.Collapsed;
        BtnVerifyAndSave.IsEnabled = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

