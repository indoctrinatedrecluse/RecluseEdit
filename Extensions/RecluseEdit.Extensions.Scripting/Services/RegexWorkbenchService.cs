using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RecluseEdit.Extensions.Scripting.Services;

public class RegexGroupItem
{
    public int MatchIndex { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Index { get; set; }
    public int Length { get; set; }
}

public class RegexMatchItem
{
    public int MatchIndex { get; set; }
    public string Value { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public List<RegexGroupItem> Groups { get; set; } = [];
}

public class RegexEvalResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RegexMatchItem> Matches { get; set; } = [];
    public List<RegexGroupItem> AllGroups { get; set; } = [];
    public long ElapsedMs { get; set; }
    public string ReplacementOutput { get; set; } = string.Empty;
}

public class RegexPreset
{
    public string Title { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SampleText { get; set; } = string.Empty;
}

public static class RegexWorkbenchService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    public static RegexEvalResult Evaluate(
        string pattern,
        string input,
        RegexOptions options = RegexOptions.None,
        string? replacementPattern = null)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return new RegexEvalResult
            {
                IsSuccess = true,
                Matches = [],
                AllGroups = [],
                ReplacementOutput = input
            };
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var regex = new Regex(pattern, options, DefaultTimeout);
            var matches = regex.Matches(input);

            var matchItems = new List<RegexMatchItem>();
            var allGroups = new List<RegexGroupItem>();

            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                var mItem = new RegexMatchItem
                {
                    MatchIndex = i + 1,
                    Value = m.Value,
                    StartIndex = m.Index,
                    Length = m.Length
                };

                foreach (Group g in m.Groups)
                {
                    var gItem = new RegexGroupItem
                    {
                        MatchIndex = i + 1,
                        GroupName = g.Name,
                        Value = g.Value,
                        Index = g.Index,
                        Length = g.Length
                    };
                    mItem.Groups.Add(gItem);
                    allGroups.Add(gItem);
                }

                matchItems.Add(mItem);
            }

            string replaced = string.Empty;
            if (replacementPattern != null)
            {
                replaced = regex.Replace(input, replacementPattern);
            }

            sw.Stop();

            return new RegexEvalResult
            {
                IsSuccess = true,
                Matches = matchItems,
                AllGroups = allGroups,
                ElapsedMs = sw.ElapsedMilliseconds,
                ReplacementOutput = replaced
            };
        }
        catch (RegexParseException ex)
        {
            sw.Stop();
            return new RegexEvalResult
            {
                IsSuccess = false,
                ErrorMessage = $"Syntax Error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (RegexMatchTimeoutException)
        {
            sw.Stop();
            return new RegexEvalResult
            {
                IsSuccess = false,
                ErrorMessage = "Evaluation timed out (> 1000ms). Pattern may cause catastrophic backtracking.",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RegexEvalResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }

    public static IReadOnlyList<RegexPreset> GetPresets() =>
    [
        new()
        {
            Title = "Email Address",
            Pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            Description = "RFC 5322 compatible email address matching",
            SampleText = "Contact us at support@recluseedit.com or sales@example.org for questions."
        },
        new()
        {
            Title = "HTTP / HTTPS URL",
            Pattern = @"https?:\/\/(?:www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b(?:[-a-zA-Z0-9()@:%_\+.~#?&//=]*)",
            Description = "Matches standard web URLs",
            SampleText = "Visit https://github.com/indoctrinatedrecluse/RecluseEdit or http://localhost:5050/api"
        },
        new()
        {
            Title = "Semantic Versioning (SemVer)",
            Pattern = @"v?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z.-]+))?",
            Description = "Extracts major, minor, patch, and prerelease components",
            SampleText = "Releases: v5.5.0, 6.0.0-rc.1, 1.2.3-beta.2, and 2.0.0"
        },
        new()
        {
            Title = "IPv4 Address",
            Pattern = @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
            Description = "Matches valid IPv4 addresses",
            SampleText = "Localhost is 127.0.0.1, router is 192.168.1.1, DNS is 8.8.8.8."
        },
        new()
        {
            Title = "UUID / GUID",
            Pattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}",
            Description = "Standard RFC 4122 UUID matching",
            SampleText = "Generated ID: c56a4180-65aa-42ec-a945-5fd21dec0538 (status: verified)"
        },
        new()
        {
            Title = "Hex Color Code",
            Pattern = @"#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b",
            Description = "Matches #RGB, #RRGGBB, and #RRGGBBAA hex color codes",
            SampleText = "Palette: #007ACC, #1E1E1E, #fff, and translucent #FF5500AA."
        },
        new()
        {
            Title = "ISO 8601 Date / Timestamp",
            Pattern = @"\d{4}-\d{2}-\d{2}(?:T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?)?",
            Description = "Matches ISO 8601 dates and UTC timestamps",
            SampleText = "Logged at 2026-09-19T00:00:00Z and modified on 2026-09-18."
        }
    ];
}

