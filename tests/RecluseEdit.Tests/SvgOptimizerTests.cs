using RecluseEdit.Extensions.Frontend.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class SvgOptimizerTests
{
    private const string RawSampleSvg = """
    <?xml version="1.0" encoding="UTF-8"?>
    <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
    <!-- Created with Inkscape (http://www.inkscape.org/) -->
    <svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" width="100" height="100" viewBox="0 0 100 100">
      <metadata id="metadata5">
        <rdf:RDF><cc:Work></cc:Work></rdf:RDF>
      </metadata>
      <g inkscape:groupmode="layer" id="layer1">
        <path d="M 10.12345 20.67890 L 50.55555 80.99999 Z" fill="#ff0000" stroke-width="2" class="icon" />
      </g>
    </svg>
    """;

    [TestMethod]
    public void OptimizeSvg_StripsXmlDeclarationAndComments()
    {
        var result = SvgOptimizerService.OptimizeSvg(RawSampleSvg);

        Assert.IsGreaterThan(0.0, result.PercentSaved);
        Assert.IsLessThan(result.OriginalSizeBytes, result.OptimizedSizeBytes);
        Assert.DoesNotContain("<?xml", result.OptimizedSvg);
        Assert.DoesNotContain("<!DOCTYPE", result.OptimizedSvg);
        Assert.DoesNotContain("<!-- Created with Inkscape", result.OptimizedSvg);
        Assert.DoesNotContain("<metadata", result.OptimizedSvg);
        Assert.DoesNotContain("inkscape:groupmode", result.OptimizedSvg);
    }

    [TestMethod]
    public void OptimizeSvg_RoundsCoordinatesProperly()
    {
        var options = new SvgOptimizeOptions { DecimalPrecision = 2, RoundCoordinates = true };
        var result = SvgOptimizerService.OptimizeSvg(RawSampleSvg, options);

        // 10.12345 -> 10.12, 20.67890 -> 20.68
        StringAssert.Contains(result.OptimizedSvg, "10.12");
        StringAssert.Contains(result.OptimizedSvg, "20.68");
        Assert.DoesNotContain("10.12345", result.OptimizedSvg);
    }

    [TestMethod]
    public void OptimizeSvg_HandlesNullAndEmpty()
    {
        var resNull = SvgOptimizerService.OptimizeSvg("");
        Assert.AreEqual(0, resNull.OriginalSizeBytes);
        Assert.AreEqual(0, resNull.OptimizedSizeBytes);
        Assert.AreEqual(string.Empty, resNull.OptimizedSvg);
    }

    [TestMethod]
    public void ConvertToJsx_GeneratesReactComponent()
    {
        var jsx = SvgOptimizerService.ConvertToJsx(RawSampleSvg, "PlayIcon");

        StringAssert.Contains(jsx, "export function PlayIcon(props: React.SVGProps<SVGSVGElement>)");
        StringAssert.Contains(jsx, "export default PlayIcon;");
        StringAssert.Contains(jsx, "<svg {...props}");
        StringAssert.Contains(jsx, "className=");
        StringAssert.Contains(jsx, "strokeWidth=");
        Assert.DoesNotContain("class=", jsx);
        Assert.DoesNotContain("stroke-width=", jsx);
    }

    [TestMethod]
    public void ConvertToDataUri_ProducesValidCssDataUri()
    {
        var dataUri = SvgOptimizerService.ConvertToDataUri("<svg width='10' height='10'><rect fill='red'/></svg>");

        Assert.StartsWith("data:image/svg+xml,", dataUri);
        Assert.DoesNotContain(" ", dataUri);
        StringAssert.Contains(dataUri, "%3Crect");
    }
}

