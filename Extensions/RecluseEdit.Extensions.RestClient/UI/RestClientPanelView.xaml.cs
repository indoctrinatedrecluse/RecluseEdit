using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RecluseEdit.Extensions.RestClient.Services;

namespace RecluseEdit.Extensions.RestClient.UI;

public partial class RestClientPanelView : UserControl
{
    private readonly HttpRequestEngine _engine = new();

    public RestClientPanelView()
    {
        InitializeComponent();
    }

    private async void OnSendClick(object sender, RoutedEventArgs e)
    {
        await ExecuteRequestAsync();
    }

    private async void OnUrlKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ExecuteRequestAsync();
        }
    }

    public async Task ExecuteRequestAsync()
    {
        BtnSend.IsEnabled = false;
        TxtStatusCode.Text = "Sending...";
        BadgeStatus.Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x63, 0x9C));
        TxtStatusCode.Foreground = Brushes.White;

        try
        {
            var req = BuildRequest();
            var response = await _engine.SendAsync(req);

            // 1. Status Code Badge
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                BadgeStatus.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                TxtStatusCode.Foreground = Brushes.White;
            }
            else if (response.StatusCode >= 300 && response.StatusCode < 400)
            {
                BadgeStatus.Background = new SolidColorBrush(Color.FromRgb(0x85, 0x5A, 0x00));
                TxtStatusCode.Foreground = Brushes.White;
            }
            else if (response.StatusCode >= 400 && response.StatusCode < 500)
            {
                BadgeStatus.Background = new SolidColorBrush(Color.FromRgb(0xA1, 0x5C, 0x00));
                TxtStatusCode.Foreground = Brushes.White;
            }
            else
            {
                BadgeStatus.Background = new SolidColorBrush(Color.FromRgb(0xA1, 0x26, 0x0D));
                TxtStatusCode.Foreground = Brushes.White;
            }

            TxtStatusCode.Text = response.StatusCode > 0
                ? $"{response.StatusCode} {response.StatusReason}"
                : response.StatusReason;

            // 2. Metrics
            TxtElapsed.Text = $"{response.ElapsedMilliseconds} ms";
            TxtSize.Text = FormatBytes(response.ContentLength);

            // 3. Response Body
            TxtResponseBody.Text = !string.IsNullOrEmpty(response.FormattedBody)
                ? response.FormattedBody
                : response.Body;

            // 4. Response Headers
            var sb = new System.Text.StringBuilder();
            foreach (var (k, v) in response.Headers)
            {
                sb.AppendLine($"{k}: {v}");
            }
            TxtResponseHeaders.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            BadgeStatus.Background = new SolidColorBrush(Color.FromRgb(0xA1, 0x26, 0x0D));
            TxtStatusCode.Text = "Error";
            TxtResponseBody.Text = ex.Message;
        }
        finally
        {
            BtnSend.IsEnabled = true;
        }
    }

    private HttpRequestModel BuildRequest()
    {
        var method = (ComboMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "GET";
        var url = TxtUrl.Text.Trim();

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var headerLines = TxtRequestHeaders.Text.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in headerLines)
        {
            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                headers[line[..idx].Trim()] = line[(idx + 1)..].Trim();
            }
        }

        var body = TxtRequestBody.Text;

        return new HttpRequestModel
        {
            Method = method,
            Url = url,
            Headers = headers,
            Body = string.IsNullOrWhiteSpace(body) ? null : body
        };
    }

    private void OnCopyResponseClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtResponseBody.Text))
        {
            Clipboard.SetText(TxtResponseBody.Text);
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
