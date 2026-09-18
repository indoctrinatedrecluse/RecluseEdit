namespace RecluseEdit.Core.Models;

/// <summary>
/// Identifies the package type of a release asset.
/// </summary>
public enum UpdateAssetType
{
    Unknown,
    ZipArchive,
    ExecutableInstaller
}

/// <summary>
/// Details of a downloadable release asset from GitHub.
/// </summary>
public class UpdateAssetInfo
{
    public string Name { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public UpdateAssetType AssetType { get; set; } = UpdateAssetType.Unknown;

    public string FormattedSize
    {
        get
        {
            if (SizeBytes <= 0) return "Unknown size";
            double mb = SizeBytes / (1024.0 * 1024.0);
            return $"{mb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} MB";
        }
    }
}

/// <summary>
/// Progress reporting data during an update download.
/// </summary>
public class UpdateProgressReport
{
    public long BytesReceived { get; set; }
    public long TotalBytes { get; set; }
    public double Percentage { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result of an update check against the release repository.
/// </summary>
public class UpdateCheckResult
{
    public bool IsSuccess { get; set; }
    public bool IsUpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseTitle { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public UpdateAssetInfo? Asset { get; set; }
    public string? ErrorMessage { get; set; }

    public static UpdateCheckResult SuccessUpToDate(string currentVersion, string latestVersion, string htmlUrl) =>
        new()
        {
            IsSuccess = true,
            IsUpdateAvailable = false,
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            HtmlUrl = htmlUrl
        };

    public static UpdateCheckResult SuccessUpdateAvailable(
        string currentVersion,
        string latestVersion,
        string title,
        string notes,
        DateTime? publishedAt,
        string htmlUrl,
        UpdateAssetInfo? asset) =>
        new()
        {
            IsSuccess = true,
            IsUpdateAvailable = true,
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            ReleaseTitle = title,
            ReleaseNotes = notes,
            PublishedAt = publishedAt,
            HtmlUrl = htmlUrl,
            Asset = asset
        };

    public static UpdateCheckResult Failed(string currentVersion, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            IsUpdateAvailable = false,
            CurrentVersion = currentVersion,
            ErrorMessage = errorMessage
        };
}
