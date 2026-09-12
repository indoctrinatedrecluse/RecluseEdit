using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RecluseEdit.Extensions.RestClient.Services;

public record HttpRequestModel
{
    public string Method { get; init; } = "GET";
    public string Url { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Body { get; init; }
}

public record HttpResponseModel
{
    public int StatusCode { get; init; }
    public string StatusReason { get; init; } = "";
    public bool IsSuccessStatusCode { get; init; }
    public long ElapsedMilliseconds { get; init; }
    public long ContentLength { get; init; }
    public string? ContentType { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; init; } = "";
    public string FormattedBody { get; init; } = "";
    public string? Error { get; init; }
}

public class HttpRequestEngine
{
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Allow self-signed in dev
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static HttpRequestModel ParseRawRequest(string rawText)
    {
        var lines = rawText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var request = new HttpRequestModel();

        int i = 0;
        // Skip leading comments or empty lines
        while (i < lines.Length && (string.IsNullOrWhiteSpace(lines[i]) || lines[i].TrimStart().StartsWith('#') || lines[i].TrimStart().StartsWith("//")))
        {
            i++;
        }

        if (i >= lines.Length)
        {
            return request;
        }

        // Parse request line: METHOD URL [HTTP/VERSION]
        var reqLine = lines[i++].Trim();
        var reqParts = reqLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (reqParts.Length == 1)
        {
            request = request with { Method = "GET", Url = reqParts[0] };
        }
        else if (reqParts.Length >= 2)
        {
            request = request with { Method = reqParts[0].ToUpperInvariant(), Url = reqParts[1] };
        }

        // Parse headers until empty line
        while (i < lines.Length)
        {
            var headerLine = lines[i++];
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                break;
            }

            var colonIdx = headerLine.IndexOf(':');
            if (colonIdx > 0)
            {
                var name = headerLine[..colonIdx].Trim();
                var value = headerLine[(colonIdx + 1)..].Trim();
                request.Headers[name] = value;
            }
        }

        // Parse body
        if (i < lines.Length)
        {
            var bodyBuilder = new StringBuilder();
            while (i < lines.Length)
            {
                // Stop if next request boundary
                if (lines[i].TrimStart().StartsWith("###"))
                {
                    break;
                }
                bodyBuilder.AppendLine(lines[i++]);
            }
            var body = bodyBuilder.ToString().TrimEnd();
            if (!string.IsNullOrWhiteSpace(body))
            {
                request = request with { Body = body };
            }
        }

        return request;
    }

    public async Task<HttpResponseModel> SendAsync(HttpRequestModel req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
        {
            return new HttpResponseModel
            {
                StatusCode = 0,
                StatusReason = "Invalid URL",
                Error = "Request URL is empty."
            };
        }

        var url = req.Url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var msg = new HttpRequestMessage(new HttpMethod(req.Method.ToUpperInvariant()), url);

            if (!string.IsNullOrEmpty(req.Body) &&
                req.Method.ToUpperInvariant() is not "GET" and not "HEAD" and not "DELETE")
            {
                var contentType = req.Headers.TryGetValue("Content-Type", out var ctHeader)
                    ? ctHeader
                    : "application/json";

                msg.Content = new StringContent(req.Body, Encoding.UTF8, contentType);
            }

            foreach (var (key, value) in req.Headers)
            {
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Handled by StringContent
                }

                if (!msg.Headers.TryAddWithoutValidation(key, value) && msg.Content != null)
                {
                    msg.Content.Headers.TryAddWithoutValidation(key, value);
                }
            }

            using var response = await HttpClient.SendAsync(msg, ct);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var h in response.Headers)
            {
                responseHeaders[h.Key] = string.Join(", ", h.Value);
            }
            foreach (var h in response.Content.Headers)
            {
                responseHeaders[h.Key] = string.Join(", ", h.Value);
            }

            var formatted = FormatBody(responseBody, response.Content.Headers.ContentType?.MediaType);

            return new HttpResponseModel
            {
                StatusCode = (int)response.StatusCode,
                StatusReason = response.ReasonPhrase ?? response.StatusCode.ToString(),
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                ContentLength = response.Content.Headers.ContentLength ?? Encoding.UTF8.GetByteCount(responseBody),
                ContentType = response.Content.Headers.ContentType?.MediaType,
                Headers = responseHeaders,
                Body = responseBody,
                FormattedBody = formatted
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HttpResponseModel
            {
                StatusCode = 0,
                StatusReason = "Request Failed",
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                Error = ex.Message,
                Body = ex.ToString(),
                FormattedBody = ex.ToString()
            };
        }
    }

    public static string FormatBody(string body, string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        // Try JSON formatting
        if (mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true ||
            body.TrimStart().StartsWith('{') || body.TrimStart().StartsWith('['))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                // Fallback to raw if JSON parsing fails
            }
        }

        return body;
    }
}

