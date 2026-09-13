using System.Collections.Generic;

namespace RecluseEdit.Core.Models;

public class ProjectTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Web";
    public string IconGlyph { get; set; } = "🌐";
    public IReadOnlyList<string> Tags { get; set; } = new List<string>();
    public string EntrypointRelativePath { get; set; } = "index.html";
}

