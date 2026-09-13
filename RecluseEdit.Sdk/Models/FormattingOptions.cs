namespace RecluseEdit.Sdk.Models;

public class FormattingOptions
{
    public int IndentSize { get; set; } = 2;
    public bool InsertSpaces { get; set; } = true;
    public bool TrimTrailingWhitespace { get; set; } = true;
    public bool InsertFinalNewline { get; set; } = true;

    public string GetIndentString(int level)
    {
        if (level <= 0) return string.Empty;
        return InsertSpaces ? new string(' ', level * IndentSize) : new string('\t', level);
    }
}

