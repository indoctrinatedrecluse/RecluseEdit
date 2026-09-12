using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class CommandRegistryTests
{
    [TestMethod]
    public void TestRegisterAndSearchCommands()
    {
        var registry = new CommandRegistry();
        bool executed = false;

        registry.Register(new CommandItem
        {
            Id = "test.action",
            Title = "Toggle Terminal",
            Category = "View",
            InputGestureText = "Ctrl+`",
            Action = () => executed = true
        });

        // Search with '>' prefix
        var results = registry.Search("> Terminal");
        Assert.HasCount(1, results);
        Assert.AreEqual("Toggle Terminal", results[0].Title);

        // Execute action
        results[0].Action();
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void TestQuickOpenFileSearch()
    {
        var registry = new CommandRegistry();
        registry.FileProvider = () =>
        [
            new CommandItem
            {
                Id = "file.app",
                Title = "App.xaml.cs",
                Category = "Workspace",
                Action = () => { }
            },
            new CommandItem
            {
                Id = "file.main",
                Title = "MainWindow.xaml.cs",
                Category = "Workspace",
                Action = () => { }
            }
        ];

        var results = registry.Search("Main");
        Assert.IsTrue(results.Any(r => r.Title == "MainWindow.xaml.cs"));
    }

    [TestMethod]
    public void TestGoToLineMode()
    {
        var registry = new CommandRegistry();
        int targetLine = 0;
        int targetCol = 0;

        registry.LineJumpHandler = (l, c) =>
        {
            targetLine = l;
            targetCol = c;
        };

        var results = registry.Search(":42:15");
        Assert.HasCount(1, results);
        Assert.Contains("Line 42", results[0].Title);

        results[0].Action();
        Assert.AreEqual(42, targetLine);
        Assert.AreEqual(15, targetCol);
    }

    [TestMethod]
    public void TestHelpMode()
    {
        var registry = new CommandRegistry();
        var results = registry.Search("?");

        Assert.IsGreaterThanOrEqualTo(results.Count, 3);
        Assert.IsTrue(results.Any(r => r.Title.Contains("'>'")));
        Assert.IsTrue(results.Any(r => r.Title.Contains("':'")));
    }
}
