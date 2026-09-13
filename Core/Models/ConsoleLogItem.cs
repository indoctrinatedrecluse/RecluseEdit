using System;

namespace RecluseEdit.Core.Models;

public enum ConsoleLogLevel
{
    Info,
    Warn,
    Error,
    Debug
}

public class ConsoleLogItem
{
    public ConsoleLogLevel Level { get; set; } = ConsoleLogLevel.Info;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Source { get; set; } = "LivePreview";

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public string BadgeText => Level switch
    {
        ConsoleLogLevel.Error => "ERR",
        ConsoleLogLevel.Warn => "WARN",
        ConsoleLogLevel.Debug => "DEBUG",
        _ => "INFO"
    };

    public string BadgeBrush => Level switch
    {
        ConsoleLogLevel.Error => "#F48771",
        ConsoleLogLevel.Warn => "#CCA700",
        ConsoleLogLevel.Debug => "#9CDCFE",
        _ => "#4EC9B0"
    };
}

