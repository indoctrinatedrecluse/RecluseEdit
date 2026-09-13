using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Helper bridge for preparing HTML and Markdown documents for WebView2 rendering,
/// relative asset resolution, and console message forwarding.
/// </summary>
public static class LivePreviewBridge
{
    public const string ConsoleBridgeScript = """
    <script id="recluse-console-bridge">
    (function() {
      if (window.__recluse_console_installed) return;
      window.__recluse_console_installed = true;

      function post(level, args) {
        try {
          if (window.chrome && window.chrome.webview) {
            var msg = Array.from(args).map(function(a) {
              if (a === null) return 'null';
              if (a === undefined) return 'undefined';
              if (typeof a === 'object') {
                try { return JSON.stringify(a); } catch(e) { return String(a); }
              }
              return String(a);
            }).join(' ');

            window.chrome.webview.postMessage(JSON.stringify({
              type: 'console',
              level: level,
              message: msg,
              time: new Date().toISOString()
            }));
          }
        } catch(err) {}
      }

      var origLog = console.log, origWarn = console.warn, origError = console.error, origInfo = console.info;
      console.log = function() { post('Info', arguments); origLog && origLog.apply(console, arguments); };
      console.info = function() { post('Info', arguments); origInfo && origInfo.apply(console, arguments); };
      console.warn = function() { post('Warn', arguments); origWarn && origWarn.apply(console, arguments); };
      console.error = function() { post('Error', arguments); origError && origError.apply(console, arguments); };

      window.addEventListener('error', function(e) {
        post('Error', [e.message + ' (' + (e.filename || 'script') + ':' + e.lineno + ':' + e.colno + ')']);
      });

      window.addEventListener('unhandledrejection', function(e) {
        post('Error', ['Unhandled Promise Rejection: ' + (e.reason ? (e.reason.stack || e.reason) : 'Unknown reason')]);
      });
    })();
    </script>
    """;

    public static string PrepareHtmlContent(string sourceHtml, string? documentFilePath)
    {
        if (string.IsNullOrEmpty(sourceHtml))
            return "<html><body></body></html>";

        var baseTag = string.Empty;
        if (!string.IsNullOrEmpty(documentFilePath))
        {
            var dir = Path.GetDirectoryName(documentFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                var uri = new Uri(dir.EndsWith(Path.DirectorySeparatorChar) ? dir : dir + Path.DirectorySeparatorChar).AbsoluteUri;
                baseTag = $"<base href=\"{uri}\">";
            }
        }

        var html = sourceHtml;
        if (html.Contains("<head>", StringComparison.OrdinalIgnoreCase))
        {
            var idx = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase) + 6;
            html = html.Insert(idx, $"\n{baseTag}\n{ConsoleBridgeScript}\n");
        }
        else if (html.Contains("<html>", StringComparison.OrdinalIgnoreCase))
        {
            var idx = html.IndexOf("<html>", StringComparison.OrdinalIgnoreCase) + 6;
            html = html.Insert(idx, $"\n<head>{baseTag}\n{ConsoleBridgeScript}</head>\n");
        }
        else
        {
            html = $"<!DOCTYPE html><html><head>{baseTag}\n{ConsoleBridgeScript}</head><body>{html}</body></html>";
        }

        return html;
    }

    public static string MarkdownToHtml(string markdown, string? documentFilePath)
    {
        var dir = !string.IsNullOrEmpty(documentFilePath) ? Path.GetDirectoryName(documentFilePath) : null;
        var baseUri = dir != null ? new Uri(dir.EndsWith(Path.DirectorySeparatorChar) ? dir : dir + Path.DirectorySeparatorChar).AbsoluteUri : "";
        var baseTag = !string.IsNullOrEmpty(baseUri) ? $"<base href=\"{baseUri}\">" : "";

        var body = RenderMarkdownBody(markdown);

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          {{baseTag}}
          {{ConsoleBridgeScript}}
          <style>
            :root {
              --bg: #0d1117;
              --text: #c9d1d9;
              --heading: #f0f6fc;
              --link: #58a6ff;
              --border: #30363d;
              --code-bg: #161b22;
              --card-bg: #161b22;
              --quote: #8b949e;
            }
            * { box-sizing: border-box; }
            body {
              background-color: var(--bg);
              color: var(--text);
              font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
              font-size: 15px;
              line-height: 1.6;
              padding: 2.5rem;
              max-width: 900px;
              margin: 0 auto;
            }
            h1, h2, h3, h4, h5, h6 {
              color: var(--heading);
              margin-top: 1.5rem;
              margin-bottom: 0.75rem;
              font-weight: 600;
              line-height: 1.25;
            }
            h1 { font-size: 2rem; border-bottom: 1px solid var(--border); padding-bottom: 0.3rem; }
            h2 { font-size: 1.5rem; border-bottom: 1px solid var(--border); padding-bottom: 0.3rem; }
            h3 { font-size: 1.25rem; }
            p, ul, ol, table, blockquote { margin-bottom: 1rem; }
            a { color: var(--link); text-decoration: none; }
            a:hover { text-decoration: underline; }
            code {
              font-family: Consolas, "Liberation Mono", Menlo, Courier, monospace;
              background-color: var(--code-bg);
              padding: 0.2em 0.4em;
              border-radius: 6px;
              font-size: 85%;
              color: #f0f6fc;
            }
            pre {
              background-color: var(--code-bg);
              border: 1px solid var(--border);
              border-radius: 6px;
              padding: 1rem;
              overflow-x: auto;
              margin-bottom: 1rem;
            }
            pre code {
              background-color: transparent;
              padding: 0;
              font-size: 13px;
            }
            blockquote {
              padding: 0.5rem 1rem;
              color: var(--quote);
              border-left: 4px solid var(--border);
              background: rgba(255,255,255,0.02);
              border-radius: 0 6px 6px 0;
            }
            table {
              border-collapse: collapse;
              width: 100%;
              margin-bottom: 1rem;
            }
            th, td {
              border: 1px solid var(--border);
              padding: 8px 12px;
              text-align: left;
            }
            th { background-color: var(--code-bg); font-weight: 600; }
            tr:nth-child(even) { background-color: rgba(255,255,255,0.02); }
            hr { height: 1px; background-color: var(--border); border: 0; margin: 1.5rem 0; }
            ul, ol { padding-left: 2rem; }
            li { margin-bottom: 0.25rem; }
            .alert {
              padding: 0.75rem 1rem;
              border-radius: 6px;
              margin-bottom: 1rem;
              border-left: 4px solid;
            }
            .alert-note { background: rgba(56, 189, 248, 0.1); border-color: #38bdf8; color: #bae6fd; }
            .alert-tip { background: rgba(74, 222, 128, 0.1); border-color: #4ade80; color: #bbf7d0; }
            .alert-warn { background: rgba(251, 191, 36, 0.1); border-color: #fbbf24; color: #fef08a; }
            .alert-important { background: rgba(168, 85, 247, 0.1); border-color: #a855f7; color: #e9d5ff; }
          </style>
        </head>
        <body>
          {{body}}
        </body>
        </html>
        """;
    }

    private static string RenderMarkdownBody(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return "<p></p>";

        var sb = new StringBuilder();
        var lines = markdown.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        bool inCodeBlock = false;
        var codeBlockContent = new StringBuilder();
        string codeBlockLang = "";
        bool inList = false;
        bool inTable = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine;

            // Fenced code blocks
            if (line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("~~~"))
            {
                if (inCodeBlock)
                {
                    inCodeBlock = false;
                    sb.Append("<pre><code");
                    if (!string.IsNullOrEmpty(codeBlockLang))
                        sb.Append($" class=\"language-{WebUtility.HtmlEncode(codeBlockLang)}\"");
                    sb.Append(">");
                    sb.Append(WebUtility.HtmlEncode(codeBlockContent.ToString()));
                    sb.AppendLine("</code></pre>");
                    codeBlockContent.Clear();
                }
                else
                {
                    inCodeBlock = true;
                    codeBlockLang = line.TrimStart().Trim('`', '~', ' ');
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockContent.AppendLine(line);
                continue;
            }

            // Tables
            if (line.TrimStart().StartsWith("|") && line.TrimEnd().EndsWith("|"))
            {
                if (!inTable)
                {
                    inTable = true;
                    sb.AppendLine("<table>");
                }
                if (Regex.IsMatch(line, @"^\|\s*[-:]+[-| :]*\|$"))
                {
                    continue; // table delimiter row
                }
                var cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                sb.Append("<tr>");
                foreach (var cell in cells)
                {
                    sb.Append($"<td>{ProcessInline(cell)}</td>");
                }
                sb.AppendLine("</tr>");
                continue;
            }
            else if (inTable)
            {
                inTable = false;
                sb.AppendLine("</table>");
            }

            // Lists
            var listMatch = Regex.Match(line, @"^(\s*)[-*+]\s+(.*)$");
            if (listMatch.Success)
            {
                if (!inList)
                {
                    inList = true;
                    sb.AppendLine("<ul>");
                }
                sb.AppendLine($"<li>{ProcessInline(listMatch.Groups[2].Value)}</li>");
                continue;
            }
            else if (inList && !string.IsNullOrWhiteSpace(line))
            {
                inList = false;
                sb.AppendLine("</ul>");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (inList)
                {
                    inList = false;
                    sb.AppendLine("</ul>");
                }
                continue;
            }

            // Headings
            var hMatch = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
            if (hMatch.Success)
            {
                int level = hMatch.Groups[1].Value.Length;
                sb.AppendLine($"<h{level}>{ProcessInline(hMatch.Groups[2].Value)}</h{level}>");
                continue;
            }

            // Horizontal rules
            if (Regex.IsMatch(line, @"^(\*{3,}|-{3,}|_{3,})$"))
            {
                sb.AppendLine("<hr />");
                continue;
            }

            // GitHub Alerts & Blockquotes
            if (line.StartsWith(">"))
            {
                var text = line[1..].Trim();
                if (text.StartsWith("[!NOTE]", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"<div class=\"alert alert-note\"><strong>NOTE:</strong> {ProcessInline(text[7..].Trim())}</div>");
                }
                else if (text.StartsWith("[!TIP]", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"<div class=\"alert alert-tip\"><strong>TIP:</strong> {ProcessInline(text[6..].Trim())}</div>");
                }
                else if (text.StartsWith("[!WARNING]", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"<div class=\"alert alert-warn\"><strong>WARNING:</strong> {ProcessInline(text[10..].Trim())}</div>");
                }
                else if (text.StartsWith("[!IMPORTANT]", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"<div class=\"alert alert-important\"><strong>IMPORTANT:</strong> {ProcessInline(text[12..].Trim())}</div>");
                }
                else
                {
                    sb.AppendLine($"<blockquote>{ProcessInline(text)}</blockquote>");
                }
                continue;
            }

            // Paragraph
            sb.AppendLine($"<p>{ProcessInline(line)}</p>");
        }

        if (inList) sb.AppendLine("</ul>");
        if (inTable) sb.AppendLine("</table>");

        return sb.ToString();
    }

    private static string ProcessInline(string text)
    {
        var escaped = WebUtility.HtmlEncode(text);

        // Inline code `code`
        escaped = Regex.Replace(escaped, @"`([^`]+)`", "<code>$1</code>");

        // Bold **text**
        escaped = Regex.Replace(escaped, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");

        // Italic *text*
        escaped = Regex.Replace(escaped, @"\*([^*]+)\*", "<em>$1</em>");

        // Links [text](url)
        escaped = Regex.Replace(escaped, @"\[([^\]]+)\]\(([^)]+)\)", "<a href=\"$2\" target=\"_blank\">$1</a>");

        return escaped;
    }
}
