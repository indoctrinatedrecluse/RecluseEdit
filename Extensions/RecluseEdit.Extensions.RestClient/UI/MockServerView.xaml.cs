using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecluseEdit.Extensions.RestClient.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.RestClient.UI;

public partial class MockServerView : UserControl
{
    private readonly MockServerService _server = new();
    private readonly ObservableCollection<MockRequestLog> _logs = [];
    private bool _isBindingRoute = false;

    public MockServerView(IWorkspaceContext? workspaceContext = null)
    {
        InitializeComponent();

        ListRoutes.ItemsSource = _server.Routes;
        GridTrafficLogs.ItemsSource = _logs;

        _server.RequestLogged += log =>
        {
            Dispatcher.Invoke(() =>
            {
                _logs.Insert(0, log);
                if (_logs.Count > 100)
                {
                    _logs.RemoveAt(_logs.Count - 1);
                }
            });
        };

        _server.StateChanged += running =>
        {
            Dispatcher.Invoke(() => UpdateServerStatusUi(running));
        };

        if (_server.Routes.Count > 0)
        {
            ListRoutes.SelectedIndex = 0;
        }

        UpdateServerStatusUi(false);
    }

    private void UpdateServerStatusUi(bool running)
    {
        if (running)
        {
            BtnToggleServer.Content = "⏹ Stop Server";
            BtnToggleServer.Background = new SolidColorBrush(Color.FromRgb(180, 40, 40));
            TxtServerStatus.Text = "🟢 Server Running";
            TxtServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(78, 201, 176));
            TxtPort.IsEnabled = false;
        }
        else
        {
            BtnToggleServer.Content = "▶ Start Server";
            BtnToggleServer.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            TxtServerStatus.Text = "⚪ Server Stopped";
            TxtServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            TxtPort.IsEnabled = true;
        }

        TxtBaseUrl.Text = $"http://localhost:{_server.Port}";
    }

    private async void OnToggleServerClick(object sender, RoutedEventArgs e)
    {
        if (_server.IsRunning)
        {
            _server.Stop();
        }
        else
        {
            if (int.TryParse(TxtPort.Text, out var port) && port > 1000 && port < 65535)
            {
                try
                {
                    await _server.StartAsync(port);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start mock server on port {port}:\n{ex.Message}", "Mock Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid port between 1024 and 65535.", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void OnRouteSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListRoutes.SelectedItem is MockRoute route)
        {
            _isBindingRoute = true;
            try
            {
                for (int i = 0; i < ComboMethod.Items.Count; i++)
                {
                    if (ComboMethod.Items[i] is ComboBoxItem item &&
                        item.Content.ToString()?.Equals(route.Method, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        ComboMethod.SelectedIndex = i;
                        break;
                    }
                }

                TxtRoutePath.Text = route.Path;
                TxtStatusCode.Text = route.StatusCode.ToString();
                SliderLatency.Value = route.LatencyMs;
                TxtLatencyVal.Text = $"{route.LatencyMs}ms";
                TxtResponseBody.Text = route.ResponseBody;
            }
            finally
            {
                _isBindingRoute = false;
            }
        }
    }

    private void OnRouteFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_isBindingRoute) return;

        if (ListRoutes.SelectedItem is MockRoute route)
        {
            if (ComboMethod.SelectedItem is ComboBoxItem item)
            {
                route.Method = item.Content?.ToString() ?? "GET";
            }
            route.Path = TxtRoutePath.Text.Trim();
            if (int.TryParse(TxtStatusCode.Text, out var code))
            {
                route.StatusCode = code;
            }
            route.LatencyMs = (int)SliderLatency.Value;
            TxtLatencyVal.Text = $"{route.LatencyMs}ms";
            route.ResponseBody = TxtResponseBody.Text;

            // Refresh list display
            ListRoutes.Items.Refresh();
        }
    }

    private void OnAddRouteClick(object sender, RoutedEventArgs e)
    {
        var newRoute = new MockRoute
        {
            Method = "GET",
            Path = $"/api/resource_{_server.Routes.Count + 1}",
            StatusCode = 200,
            ResponseBody = "{\"status\": \"ok\"}",
            LatencyMs = 50
        };

        _server.Routes.Add(newRoute);
        ListRoutes.SelectedItem = newRoute;
    }

    private void OnDeleteRouteClick(object sender, RoutedEventArgs e)
    {
        if (ListRoutes.SelectedItem is MockRoute route)
        {
            _server.Routes.Remove(route);
            if (_server.Routes.Count > 0)
            {
                ListRoutes.SelectedIndex = 0;
            }
        }
    }

    private void OnClearLogsClick(object sender, RoutedEventArgs e)
    {
        _logs.Clear();
    }

    private void OnCopyBaseUrlClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Clipboard.SetText(TxtBaseUrl.Text);
        MessageBox.Show($"Copied {TxtBaseUrl.Text} to clipboard!", "Mock Server", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

