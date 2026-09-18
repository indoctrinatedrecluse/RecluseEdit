using System.Text.RegularExpressions;
using RecluseEdit.Extensions.Scripting.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class RegexWorkbenchTests
{
    [TestMethod]
    public void Evaluate_ExtractsMatchesAndCaptureGroups()
    {
        var pattern = @"(?<first>\w+)\s+(?<second>\w+)";
        var input = "Hello World, Foo Bar";

        var result = RegexWorkbenchService.Evaluate(pattern, input);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Matches);

        var m1 = result.Matches[0];
        Assert.AreEqual("Hello World", m1.Value);
        Assert.IsTrue(m1.Groups.Any(g => g.GroupName == "first" && g.Value == "Hello"));
        Assert.IsTrue(m1.Groups.Any(g => g.GroupName == "second" && g.Value == "World"));

        var m2 = result.Matches[1];
        Assert.AreEqual("Foo Bar", m2.Value);
        Assert.IsTrue(m2.Groups.Any(g => g.GroupName == "first" && g.Value == "Foo"));
        Assert.IsTrue(m2.Groups.Any(g => g.GroupName == "second" && g.Value == "Bar"));
    }

    [TestMethod]
    public void Evaluate_PerformsReplacementCorrectly()
    {
        var pattern = @"\b([a-z]+)\b";
        var input = "apple banana cherry";
        var replacement = "[$1]";

        var result = RegexWorkbenchService.Evaluate(pattern, input, RegexOptions.None, replacement);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("[apple] [banana] [cherry]", result.ReplacementOutput);
    }

    [TestMethod]
    public void Evaluate_HandlesInvalidPatternGracefully()
    {
        var pattern = @"[unclosed-bracket";
        var input = "test string";

        var result = RegexWorkbenchService.Evaluate(pattern, input);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        StringAssert.Contains(result.ErrorMessage, "Syntax Error");
    }

    [TestMethod]
    public void Evaluate_EmptyPattern_ReturnsInputAsOutput()
    {
        var result = RegexWorkbenchService.Evaluate("", "hello world");
        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Matches);
        Assert.AreEqual("hello world", result.ReplacementOutput);
    }

    [TestMethod]
    public void GetPresets_ReturnsComprehensiveCatalog()
    {
        var presets = RegexWorkbenchService.GetPresets();

        Assert.IsGreaterThanOrEqualTo(7, presets.Count);
        Assert.IsTrue(presets.Any(p => p.Title.Contains("Email")));
        Assert.IsTrue(presets.Any(p => p.Title.Contains("URL")));
        Assert.IsTrue(presets.Any(p => p.Title.Contains("SemVer")));
        Assert.IsTrue(presets.Any(p => p.Title.Contains("IPv4")));
        Assert.IsTrue(presets.Any(p => p.Title.Contains("UUID")));
        Assert.IsTrue(presets.Any(p => p.Title.Contains("Hex Color")));

        foreach (var p in presets)
        {
            var testRes = RegexWorkbenchService.Evaluate(p.Pattern, p.SampleText);
            Assert.IsTrue(testRes.IsSuccess, $"Preset '{p.Title}' failed to evaluate on its sample text.");
            Assert.IsNotEmpty(testRes.Matches, $"Preset '{p.Title}' found 0 matches on its sample text.");
        }
    }
}

