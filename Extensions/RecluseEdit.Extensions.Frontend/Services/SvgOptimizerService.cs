using System.Text;
using System.Text.RegularExpressions;

namespace RecluseEdit.Extensions.Frontend.Services;

/// <summary>
/// Configuration options for SVG optimization.
/// </summary>
public class SvgOptimizeOptions
{
    public bool StripMetadata { get; set; } = true;
    public bool StripComments { get; set; } = true;
    public bool StripXmlDeclaration { get; set; } = true;
    public bool RoundCoordinates { get; set; } = true;
    public int DecimalPrecision { get; set; } = 2;
    public bool MinifyWhitespace { get; set; } = true;
}

/// <summary>
/// Result metrics from an SVG optimization pass.
/// </summary>
public class SvgOptimizationResult
{
    public string OriginalSvg { get; set; } = string.Empty;
    public string OptimizedSvg { get; set; } = string.Empty;
    public int OriginalSizeBytes { get; set; }
    public int OptimizedSizeBytes { get; set; }
    public double PercentSaved => OriginalSizeBytes > 0
        ? Math.Max(0, (OriginalSizeBytes - OptimizedSizeBytes) / (double)OriginalSizeBytes * 100.0)
        : 0;
}

/// <summary>
/// Engine for cleaning, minifying, and converting SVG vector assets for web apps.
/// </summary>
public static class SvgOptimizerService
{
    private static readonly Regex XmlDeclRegex = new(@"<\?xml[^>]*\?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DoctypeRegex = new(@"<!DOCTYPE[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CommentsRegex = new(@"<!--[\s\S]*?-->", RegexOptions.Compiled);
    private static readonly Regex MetadataRegex = new(@"<metadata[\s\S]*?<\/metadata>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EditorNsRegex = new(@"\s*(xmlns:(?:inkscape|sodipodi|sketch|illustrator)=""[^""]*"")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EditorAttrsRegex = new(@"\s*(?:inkscape|sodipodi):[a-z0-9_-]+=""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceBetweenTagsRegex = new(@">\s+<", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesRegex = new(@"\s{2,}", RegexOptions.Compiled);
    private static readonly Regex DecimalCoordRegex = new(@"(?<=\s|,|-|^|(?<=[a-df-z]))(?:\d+\.\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Optimizes and minifies the provided SVG string based on configured options.
    /// </summary>
    public static SvgOptimizationResult OptimizeSvg(string svg, SvgOptimizeOptions? options = null)
    {
        options ??= new SvgOptimizeOptions();

        if (string.IsNullOrWhiteSpace(svg))
        {
            return new SvgOptimizationResult
            {
                OriginalSvg = string.Empty,
                OptimizedSvg = string.Empty,
                OriginalSizeBytes = 0,
                OptimizedSizeBytes = 0
            };
        }

        var originalBytes = Encoding.UTF8.GetByteCount(svg);
        var result = svg;

        // 1. Strip XML declaration
        if (options.StripXmlDeclaration)
        {
            result = XmlDeclRegex.Replace(result, string.Empty);
            result = DoctypeRegex.Replace(result, string.Empty);
        }

        // 2. Strip comments
        if (options.StripComments)
        {
            result = CommentsRegex.Replace(result, string.Empty);
        }

        // 3. Strip editor metadata & namespaces
        if (options.StripMetadata)
        {
            result = MetadataRegex.Replace(result, string.Empty);
            result = EditorNsRegex.Replace(result, string.Empty);
            result = EditorAttrsRegex.Replace(result, string.Empty);
        }

        // 4. Round coordinates in paths
        if (options.RoundCoordinates && options.DecimalPrecision >= 0)
        {
            result = DecimalCoordRegex.Replace(result, m =>
            {
                if (double.TryParse(m.Value, System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    return Math.Round(val, options.DecimalPrecision).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                return m.Value;
            });
        }

        // 5. Minify whitespace
        if (options.MinifyWhitespace)
        {
            result = WhitespaceBetweenTagsRegex.Replace(result, "><");
            result = MultipleSpacesRegex.Replace(result, " ");
            result = result.Trim();
        }

        var optBytes = Encoding.UTF8.GetByteCount(result);

        return new SvgOptimizationResult
        {
            OriginalSvg = svg,
            OptimizedSvg = result,
            OriginalSizeBytes = originalBytes,
            OptimizedSizeBytes = optBytes
        };
    }

    /// <summary>
    /// Converts raw SVG markup into a clean React JSX functional component.
    /// </summary>
    public static string ConvertToJsx(string svg, string componentName = "Icon")
    {
        if (string.IsNullOrWhiteSpace(svg)) return string.Empty;

        var opt = OptimizeSvg(svg, new SvgOptimizeOptions
        {
            StripComments = true,
            StripMetadata = true,
            StripXmlDeclaration = true,
            MinifyWhitespace = false
        }).OptimizedSvg;

        // Attribute replacements: kebab-case to React camelCase
        var attributeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "class=", "className=" },
            { "stroke-width=", "strokeWidth=" },
            { "stroke-linecap=", "strokeLinecap=" },
            { "stroke-linejoin=", "strokeLinejoin=" },
            { "stroke-miterlimit=", "strokeMiterlimit=" },
            { "stroke-dasharray=", "strokeDasharray=" },
            { "stroke-dashoffset=", "strokeDashoffset=" },
            { "stroke-opacity=", "strokeOpacity=" },
            { "fill-rule=", "fillRule=" },
            { "fill-opacity=", "fillOpacity=" },
            { "clip-rule=", "clipRule=" },
            { "clip-path=", "clipPath=" },
            { "xmlns:xlink=", "xmlnsXlink=" },
            { "xlink:href=", "xlinkHref=" },
            { "color-interpolation-filters=", "colorInterpolationFilters=" }
        };

        foreach (var (kebab, camel) in attributeMap)
        {
            opt = Regex.Replace(opt, Regex.Escape(kebab), camel, RegexOptions.IgnoreCase);
        }

        // Inject props spreading into root <svg> tag: <svg {...props} ...>
        var svgIdx = opt.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
        if (svgIdx >= 0)
        {
            opt = opt[..(svgIdx + 4)] + " {...props}" + opt[(svgIdx + 4)..];
        }

        var sb = new StringBuilder();
        sb.AppendLine("import * as React from \"react\";");
        sb.AppendLine();
        sb.AppendLine($"export function {componentName}(props: React.SVGProps<SVGSVGElement>) {{");
        sb.AppendLine("  return (");
        sb.AppendLine($"    {opt}");
        sb.AppendLine("  );");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"export default {componentName};");

        return sb.ToString();
    }

    /// <summary>
    /// Converts SVG into a CSS Data URI (background-image: url("data:image/svg+xml,...")).
    /// </summary>
    public static string ConvertToDataUri(string svg)
    {
        if (string.IsNullOrWhiteSpace(svg)) return string.Empty;

        var optimized = OptimizeSvg(svg, new SvgOptimizeOptions
        {
            StripXmlDeclaration = true,
            StripComments = true,
            StripMetadata = true,
            MinifyWhitespace = true
        }).OptimizedSvg;

        // URL encode reserved characters
        var encoded = Uri.EscapeDataString(optimized)
            .Replace("%2F", "/")
            .Replace("%3A", ":")
            .Replace("%3D", "=")
            .Replace("%22", "'");

        return $"data:image/svg+xml,{encoded}";
    }
}

