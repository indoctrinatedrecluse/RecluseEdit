using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace RecluseEdit.Extensions.RestClient.Services;

/// <summary>
/// Configured endpoint route for the mock HTTP server.
/// </summary>
public class MockRoute
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/api/resource";
    public int StatusCode { get; set; } = 200;
    public string ContentType { get; set; } = "application/json";
    public string ResponseBody { get; set; } = "{\"message\": \"success\"}";
    public int LatencyMs { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Traffic record of an incoming mock HTTP request.
/// </summary>
public class MockRequestLog
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public string ClientIp { get; set; } = "127.0.0.1";
    public int StatusCode { get; set; } = 200;
    public long DurationMs { get; set; }
    public string FormattedDuration => $"{DurationMs} ms";
    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
}

/// <summary>
/// Lightweight local HTTP mock server for web frontend development with CORS and latency simulation.
/// </summary>
public class MockServerService : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public bool IsRunning => _listener != null && _listener.IsListening;
    public int Port { get; private set; } = 5050;
    public ObservableCollection<MockRoute> Routes { get; } = [];
    public ObservableCollection<MockRequestLog> RequestLogs { get; } = [];

    public event Action<MockRequestLog>? RequestLogged;
    public event Action<bool>? StateChanged;

    public MockServerService()
    {
        InitializeDefaultRoutes();
    }

    private void InitializeDefaultRoutes()
    {
        Routes.Add(new MockRoute
        {
            Method = "GET",
            Path = "/api/users",
            StatusCode = 200,
            ContentType = "application/json",
            ResponseBody = "[\n  {\"id\": 1, \"name\": \"Alice Chen\", \"role\": \"Frontend Lead\"},\n  {\"id\": 2, \"name\": \"Bob Smith\", \"role\": \"Backend Engineer\"}\n]",
            LatencyMs = 150
        });

        Routes.Add(new MockRoute
        {
            Method = "POST",
            Path = "/api/login",
            StatusCode = 200,
            ContentType = "application/json",
            ResponseBody = "{\n  \"token\": \"mock_jwt_token_header.payload.signature\",\n  \"expiresIn\": 3600,\n  \"user\": {\"id\": 1, \"username\": \"admin\"}\n}",
            LatencyMs = 200
        });

        Routes.Add(new MockRoute
        {
            Method = "GET",
            Path = "/api/health",
            StatusCode = 200,
            ContentType = "application/json",
            ResponseBody = "{\"status\": \"healthy\", \"server\": \"RecluseEdit Mock API\", \"version\": \"6.0.0\"}",
            LatencyMs = 0
        });
    }

    public Task StartAsync(int port = 5050)
    {
        if (IsRunning) return Task.CompletedTask;

        Port = port;
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");

        try
        {
            _listener.Start();
            StateChanged?.Invoke(true);
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
        catch (Exception)
        {
            Stop();
            throw;
        }

        return Task.CompletedTask;
    }

    public void Stop()
    {
        if (!IsRunning) return;

        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch
        {
            // Ignore shutdown errors
        }
        finally
        {
            _listener = null;
            _cts?.Dispose();
            _cts = null;
            StateChanged?.Invoke(false);
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context), ct);
            }
            catch (HttpListenerException)
            {
                break; // Server stopped
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception)
            {
                // Continue loop
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var sw = Stopwatch.StartNew();
        var req = context.Request;
        var res = context.Response;

        var clientIp = req.RemoteEndPoint.Address.ToString();
        var rawPath = req.Url?.AbsolutePath ?? "/";
        var method = req.HttpMethod.ToUpperInvariant();

        // 1. Add CORS Headers
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "*");

        // 2. Handle CORS Preflight OPTIONS
        if (method == "OPTIONS")
        {
            res.StatusCode = 200;
            res.Close();
            return;
        }

        // 3. Match Route
        var matchedRoute = FindMatchingRoute(method, rawPath);
        int statusCode;
        string responseBody;
        string contentType;
        int latency = 0;

        if (matchedRoute != null)
        {
            statusCode = matchedRoute.StatusCode;
            responseBody = matchedRoute.ResponseBody;
            contentType = matchedRoute.ContentType;
            latency = matchedRoute.LatencyMs;
        }
        else
        {
            statusCode = 404;
            contentType = "application/json";
            responseBody = $"{{\"error\": \"Not Found\", \"path\": \"{rawPath}\", \"method\": \"{method}\"}}";
        }

        // 4. Simulate Latency
        if (latency > 0)
        {
            await Task.Delay(latency);
        }

        // 5. Send Response
        res.StatusCode = statusCode;
        res.ContentType = contentType;
        var buffer = Encoding.UTF8.GetBytes(responseBody);
        res.ContentLength64 = buffer.Length;

        try
        {
            await res.OutputStream.WriteAsync(buffer);
            res.OutputStream.Close();
        }
        catch
        {
            // Client closed connection early
        }

        sw.Stop();

        var log = new MockRequestLog
        {
            Timestamp = DateTime.Now,
            Method = method,
            Path = rawPath,
            ClientIp = clientIp,
            StatusCode = statusCode,
            DurationMs = sw.ElapsedMilliseconds
        };

        RequestLogged?.Invoke(log);
    }

    public MockRoute? FindMatchingRoute(string method, string path)
    {
        var normPath = path.TrimEnd('/');
        if (string.IsNullOrEmpty(normPath)) normPath = "/";

        return Routes.FirstOrDefault(r =>
            r.IsEnabled &&
            r.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
            NormalizeRoutePath(r.Path).Equals(normPath, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRoutePath(string p)
    {
        var trimmed = p.Trim().TrimEnd('/');
        return string.IsNullOrEmpty(trimmed) ? "/" : trimmed;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}

