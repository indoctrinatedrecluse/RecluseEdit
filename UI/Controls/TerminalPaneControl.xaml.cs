using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.UI.Views;

namespace RecluseEdit.UI.Controls;

public partial class TerminalPaneControl : UserControl
{
    public class TerminalTabItem : INotifyPropertyChanged
    {
        public TerminalSession Session { get; }
        public StringBuilder Buffer { get; } = new();
        public List<string> History { get; } = new();
        public int HistoryIndex { get; set; } = -1;
        public string Title => Session.Title;
        public bool IsActive { get; set; }
        public bool IsClosing { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public TerminalTabItem(TerminalSession session)
        {
            Session = session;
        }

        public void NotifyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }

    private readonly ShellSettingsService _settingsService = new();
    private readonly ShellDetector _detector;
    private List<ShellInfo> _detectedShells = [];
    private ShellInfo? _defaultShell;
    private int _terminalCounter = 1;
    private string? _workingDirectory;
    private bool _isUpdatingSelection;

    public ObservableCollection<TerminalTabItem> Tabs { get; } = [];
    public TerminalTabItem? ActiveTab { get; private set; }

    public int TerminalCount => Tabs.Count;
    public event Action? ClosePaneRequested;
    public event Action<int>? TerminalCountChanged;

    public TerminalPaneControl()
    {
        InitializeComponent();
        _detector = new ShellDetector(_settingsService);
        Loaded += OnControlLoaded;
    }

    public void SetWorkingDirectory(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            _workingDirectory = path;
        }
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        if (_detectedShells.Count == 0)
        {
            RefreshShellsList();
        }

        UpdateVisualState();
    }

    private void RefreshShellsList()
    {
        _detectedShells = _detector.DetectShells();
        _defaultShell = _detectedShells.FirstOrDefault(s => s.IsDefault && s.IsAvailable)
                        ?? _detectedShells.FirstOrDefault(s => s.IsAvailable)
                        ?? _detectedShells.FirstOrDefault();

        _isUpdatingSelection = true;
        try
        {
            CmbShells.ItemsSource = null;
            CmbShells.ItemsSource = _detectedShells;
            if (_defaultShell != null)
            {
                CmbShells.SelectedItem = _defaultShell;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }

        CmbShells.Visibility = _detectedShells.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void FocusInput()
    {
        TxtCommandInput.Focus();
    }

    /// <summary>
    /// Spawns a new terminal session with the specified or default shell.
    /// </summary>
    public void CreateTerminal(ShellInfo? shell = null)
    {
        if (_detectedShells.Count == 0)
        {
            RefreshShellsList();
        }

        var chosenShell = shell ?? (CmbShells.SelectedItem as ShellInfo) ?? _defaultShell;
        if (chosenShell == null) return;

        // If shell is marked unavailable, launch configuration flow instead of broken session
        if (!chosenShell.IsAvailable)
        {
            PromptConfigureShell(chosenShell);
            return;
        }

        var title = $"{chosenShell.Icon} {chosenShell.DisplayName} ({_terminalCounter++})";
        var session = new TerminalSession(chosenShell, title, _workingDirectory);
        var tabItem = new TerminalTabItem(session);

        session.OutputReceived += text =>
        {
            Dispatcher.BeginInvoke(() => AppendOutput(tabItem, text));
        };

        session.ProcessExited += code =>
        {
            Dispatcher.BeginInvoke(() => HandleProcessExited(tabItem, code));
        };

        try
        {
            session.Start();
        }
        catch (Exception ex)
        {
            tabItem.Buffer.AppendLine($"[Error launching {chosenShell.DisplayName}: {ex.Message}]");
        }

        Tabs.Add(tabItem);
        RebuildTabHeaders();
        SelectTab(tabItem);

        TerminalCountChanged?.Invoke(Tabs.Count);
    }

    public void PromptConfigureShell(ShellInfo shell)
    {
        var owner = Window.GetWindow(this);
        var dialog = new ConfigureShellDialog(shell, owner);

        if (dialog.ShowDialog() == true)
        {
            shell.ExecutablePath = dialog.VerifiedPath;
            shell.IsAvailable = true;
            shell.IsCustomConfigured = true;
            _settingsService.SetCustomPath(shell.Id, dialog.VerifiedPath);

            RefreshShellsList();

            _isUpdatingSelection = true;
            try
            {
                CmbShells.SelectedItem = shell;
            }
            finally
            {
                _isUpdatingSelection = false;
            }

            CreateTerminal(shell);
        }
        else
        {
            // Reset selection back to default or active shell
            _isUpdatingSelection = true;
            try
            {
                CmbShells.SelectedItem = ActiveTab?.Session.Shell ?? _defaultShell;
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }
    }

    private void AppendOutput(TerminalTabItem tab, string text)
    {
        tab.Buffer.Append(text);

        if (tab == ActiveTab)
        {
            TxtConsoleOutput.AppendText(text);
            TxtConsoleOutput.ScrollToEnd();
        }
    }

    private void HandleProcessExited(TerminalTabItem tab, int exitCode)
    {
        // Clean exit: remove tab automatically
        CloseTab(tab);
    }

    public void CloseTab(TerminalTabItem tab)
    {
        if (tab.IsClosing) return;
        tab.IsClosing = true;

        tab.Session.Close();
        var wasActive = tab == ActiveTab;
        Tabs.Remove(tab);

        if (wasActive)
        {
            var nextTab = Tabs.LastOrDefault();
            SelectTab(nextTab);
        }

        RebuildTabHeaders();
        TerminalCountChanged?.Invoke(Tabs.Count);
        UpdateVisualState();
    }

    public void SelectTab(TerminalTabItem? tab)
    {
        ActiveTab = tab;
        foreach (var t in Tabs)
        {
            t.IsActive = (t == tab);
        }

        RebuildTabHeaders();

        if (tab != null)
        {
            TxtConsoleOutput.Text = tab.Buffer.ToString();
            TxtConsoleOutput.ScrollToEnd();

            // Update prompt prefix
            var id = tab.Session.Shell.Id.ToLowerInvariant();
            TxtPromptPrefix.Text = id.Contains("power") || id.Contains("pwsh")
                ? "PS > "
                : id.Contains("bash") || id.Contains("zsh") || id.Contains("sh")
                    ? "$ "
                    : id == "cmd"
                        ? "> "
                        : $"{id} > ";

            TxtCommandInput.Focus();
        }
        else
        {
            TxtConsoleOutput.Text = "";
        }

        UpdateVisualState();
    }

    private void RebuildTabHeaders()
    {
        TabsItemsControl.Items.Clear();

        foreach (var tab in Tabs)
        {
            var border = new Border
            {
                Background = tab.IsActive ? (Brush)FindResource("BgActive") : (Brush)FindResource("BgTertiary"),
                BorderBrush = (Brush)FindResource("BorderDark"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 2, 4, 2),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var textBlock = new TextBlock
            {
                Text = tab.Title,
                Foreground = tab.IsActive ? Brushes.White : (Brush)FindResource("FgPrimary"),
                FontSize = 11,
                FontWeight = tab.IsActive ? FontWeights.SemiBold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var closeButton = new Button
            {
                Style = (Style)FindResource("TabCloseButtonStyle"),
                Content = "✕",
                ToolTip = "Close Terminal",
                Margin = new Thickness(6, 0, 0, 0)
            };

            var capturedTab = tab;
            border.MouseLeftButtonDown += (s, e) => SelectTab(capturedTab);
            closeButton.Click += (s, e) =>
            {
                e.Handled = true;
                CloseTab(capturedTab);
            };

            stack.Children.Add(textBlock);
            stack.Children.Add(closeButton);
            border.Child = stack;

            TabsItemsControl.Items.Add(border);
        }
    }

    private void UpdateVisualState()
    {
        var hasTabs = Tabs.Count > 0;
        EmptyStateBorder.Visibility = hasTabs ? Visibility.Collapsed : Visibility.Visible;
        TxtConsoleOutput.Visibility = hasTabs ? Visibility.Visible : Visibility.Collapsed;
        InputBarBorder.Visibility = hasTabs ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnNewTerminalClick(object sender, RoutedEventArgs e)
    {
        CreateTerminal();
    }

    private void OnConfigureShellClick(object sender, RoutedEventArgs e)
    {
        var shell = (CmbShells.SelectedItem as ShellInfo) ?? _defaultShell ?? _detectedShells.FirstOrDefault();
        if (shell != null)
        {
            PromptConfigureShell(shell);
        }
    }

    private void OnShellSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection || !IsLoaded) return;

        if (CmbShells.SelectedItem is ShellInfo selected)
        {
            if (!selected.IsAvailable)
            {
                PromptConfigureShell(selected);
            }
            else
            {
                CreateTerminal(selected);
            }
        }
    }

    private void OnClearOutputClick(object sender, RoutedEventArgs e)
    {
        if (ActiveTab != null)
        {
            ActiveTab.Buffer.Clear();
            TxtConsoleOutput.Text = "";
        }
    }

    private void OnClosePaneClick(object sender, RoutedEventArgs e)
    {
        ClosePaneRequested?.Invoke();
    }

    private void OnCommandInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ActiveTab == null) return;

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            var input = TxtCommandInput.Text;
            TxtCommandInput.Text = "";

            if (!string.IsNullOrWhiteSpace(input))
            {
                ActiveTab.History.Add(input);
            }
            ActiveTab.HistoryIndex = ActiveTab.History.Count;

            var trimmed = input.Trim();
            if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                // Send exit to shell process
                ActiveTab.Session.SendInput(input);

                // Fallback safety timeout (1200ms) to ensure tab closes even if the process takes longer
                var tabToClose = ActiveTab;
                Task.Delay(1200).ContinueWith(_ =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (Tabs.Contains(tabToClose) && !tabToClose.IsClosing)
                        {
                            CloseTab(tabToClose);
                        }
                    });
                });
                return;
            }

            ActiveTab.Session.SendInput(input);
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = true;
            if (ActiveTab.History.Count > 0 && ActiveTab.HistoryIndex > 0)
            {
                ActiveTab.HistoryIndex--;
                TxtCommandInput.Text = ActiveTab.History[ActiveTab.HistoryIndex];
                TxtCommandInput.CaretIndex = TxtCommandInput.Text.Length;
            }
        }
        else if (e.Key == Key.Down)
        {
            e.Handled = true;
            if (ActiveTab.History.Count > 0 && ActiveTab.HistoryIndex < ActiveTab.History.Count - 1)
            {
                ActiveTab.HistoryIndex++;
                TxtCommandInput.Text = ActiveTab.History[ActiveTab.HistoryIndex];
                TxtCommandInput.CaretIndex = TxtCommandInput.Text.Length;
            }
            else
            {
                ActiveTab.HistoryIndex = ActiveTab.History.Count;
                TxtCommandInput.Text = "";
            }
        }
        else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (string.IsNullOrEmpty(TxtCommandInput.SelectedText))
            {
                e.Handled = true;
                ActiveTab.Session.SendCtrlC();
                AppendOutput(ActiveTab, "^C\n");
            }
        }
    }

    private void OnConsoleOutputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (string.IsNullOrEmpty(TxtConsoleOutput.SelectedText) && ActiveTab != null)
            {
                e.Handled = true;
                ActiveTab.Session.SendCtrlC();
                AppendOutput(ActiveTab, "^C\n");
            }
        }
    }

    public void CloseAllTerminals()
    {
        foreach (var tab in Tabs.ToList())
        {
            tab.Session.Close();
        }
        Tabs.Clear();
        ActiveTab = null;
        RebuildTabHeaders();
        UpdateVisualState();
        TerminalCountChanged?.Invoke(0);
    }
}
