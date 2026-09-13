using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class LivePreviewBridgeTests
{
    [TestMethod]
    public void TestPrepareHtmlInjectsBaseAndConsoleShim()
    {
        string rawHtml = "<!DOCTYPE html><html><head><title>Test</title></head><body><h1>Hello</h1></body></html>";
        string filePath = @"C:\Workspace\index.html";

        string prepared = LivePreviewBridge.PrepareHtmlContent(rawHtml, filePath);

        Assert.Contains("<base href=\"file:///C:/Workspace/\">", prepared);
        Assert.Contains("window.chrome.webview.postMessage", prepared);
        Assert.Contains("<h1>Hello</h1>", prepared);
    }

    [TestMethod]
    public void TestPrepareHtmlHandlesFragmentWithoutHead()
    {
        string rawFragment = "<div><span>Simple Fragment</span></div>";
        string filePath = @"C:\Workspace\fragment.html";

        string prepared = LivePreviewBridge.PrepareHtmlContent(rawFragment, filePath);

        Assert.Contains("<base href=\"file:///C:/Workspace/\">", prepared);
        Assert.Contains("window.chrome.webview.postMessage", prepared);
        Assert.Contains("Simple Fragment", prepared);
    }

    [TestMethod]
    public void TestRenderMarkdownProducesGithubDarkHtml()
    {
        string md = """
        # RecluseEdit Title
        Here is a paragraph with **bold text** and `inline code`.

        - Feature 1
        - Feature 2

        ```javascript
        console.log("Hello Live Preview");
        ```
        """;

        string rendered = LivePreviewBridge.MarkdownToHtml(md, @"C:\Workspace\README.md");

        Assert.Contains("<h1>RecluseEdit Title</h1>", rendered);
        Assert.Contains("<strong>bold text</strong>", rendered);
        Assert.Contains("<code>inline code</code>", rendered);
        Assert.Contains("<li>Feature 1</li>", rendered);
        Assert.Contains("<pre><code class=\"language-javascript\">", rendered);
        Assert.Contains("Hello Live Preview", rendered);
        Assert.Contains("background-color: var(--bg);", rendered);
        Assert.Contains("--text: #c9d1d9;", rendered);
        Assert.Contains("window.chrome.webview.postMessage", rendered);
    }
}
