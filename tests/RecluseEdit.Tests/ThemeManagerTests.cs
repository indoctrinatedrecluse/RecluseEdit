using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;
using RecluseEdit.Extensions.Frontend.Themes;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public class ThemeManagerTests
{
    private string _tempSettingsPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"recluse_test_theme_{Guid.NewGuid():N}.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempSettingsPath))
        {
            try { File.Delete(_tempSettingsPath); } catch { }
        }
    }

    [TestMethod]
    public void ThemeManager_InitializesWithNineCreativeThemes()
    {
        var manager = new ThemeManager(_tempSettingsPath);

        Assert.HasCount(9, manager.RegisteredThemes);

        var expectedIds = new[]
        {
            "recluse.dark",
            "recluse.light",
            "recluse.cyberpunk",
            "recluse.monokai",
            "recluse.solarized",
            "recluse.dracula",
            "recluse.nord",
            "recluse.matrix",
            "recluse.highcontrast"
        };

        foreach (var id in expectedIds)
        {
            var theme = manager.RegisteredThemes.FirstOrDefault(t => t.Id == id);
            Assert.IsNotNull(theme, $"Theme with ID '{id}' must be registered.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(theme.DisplayName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(theme.Description));
            Assert.IsFalse(string.IsNullOrWhiteSpace(theme.Author));
            Assert.IsNotNull(theme.Colors);

            // Verify essential color tokens
            Assert.StartsWith("#", theme.Colors.BgPrimary);
            Assert.StartsWith("#", theme.Colors.FgPrimary);
            Assert.StartsWith("#", theme.Colors.Accent);
            Assert.StartsWith("#", theme.Colors.StatusBarBg);
            Assert.StartsWith("#", theme.Colors.Caret);
            Assert.StartsWith("#", theme.Colors.Selection);
        }

        // Active default should be Recluse Dark+
        Assert.AreEqual("recluse.dark", manager.ActiveTheme.Id);
    }

    [TestMethod]
    public void ThemeManager_CreativeThemes_HaveUniqueAesthetics()
    {
        var manager = new ThemeManager(_tempSettingsPath);

        var cyberpunk = manager.RegisteredThemes.First(t => t.Id == "recluse.cyberpunk");
        Assert.AreEqual(ThemeType.Creative, cyberpunk.Type);
        Assert.AreEqual("#120E24", cyberpunk.Colors.BgPrimary);
        Assert.AreEqual("#FF2A6D", cyberpunk.Colors.Accent);
        Assert.AreEqual("#FFE600", cyberpunk.Colors.BracketMatch);

        var matrix = manager.RegisteredThemes.First(t => t.Id == "recluse.matrix");
        Assert.AreEqual(ThemeType.Creative, matrix.Type);
        Assert.AreEqual("#0A0E0A", matrix.Colors.BgPrimary);
        Assert.AreEqual("#00FF66", matrix.Colors.FgPrimary);
        Assert.AreEqual("#00FF66", matrix.Colors.Accent);

        var highContrast = manager.RegisteredThemes.First(t => t.Id == "recluse.highcontrast");
        Assert.AreEqual(ThemeType.HighContrast, highContrast.Type);
        Assert.AreEqual("#000000", highContrast.Colors.BgPrimary);
        Assert.AreEqual("#FFFFFF", highContrast.Colors.FgPrimary);
    }

    [TestMethod]
    public void ThemeManager_RegisterCustomTheme_ViaInterface()
    {
        var manager = new ThemeManager(_tempSettingsPath);

        var customTheme = new CustomTestTheme(
            id: "custom.sunset",
            name: "Sunset Boulevard",
            colors: new ThemeColors
            {
                BgPrimary = "#1A0B2E",
                FgPrimary = "#FFEAA7",
                Accent = "#FD79A8"
            }
        );

        manager.RegisterTheme(customTheme);

        Assert.HasCount(10, manager.RegisteredThemes);
        var found = manager.RegisteredThemes.FirstOrDefault(t => t.Id == "custom.sunset");
        Assert.IsNotNull(found);
        Assert.AreEqual("Sunset Boulevard", found.DisplayName);
        Assert.AreEqual("#1A0B2E", found.Colors.BgPrimary);
    }

    [TestMethod]
    public void ThemeManager_RegisterExtensionTheme_ViaThemeDefinitionBase()
    {
        var manager = new ThemeManager(_tempSettingsPath);

        // VueEmeraldTheme from RecluseEdit.Extensions.Frontend inheriting ThemeDefinitionBase
        var vueTheme = new VueEmeraldTheme();
        manager.RegisterTheme(vueTheme);

        Assert.HasCount(10, manager.RegisteredThemes);
        var found = manager.RegisteredThemes.FirstOrDefault(t => t.Id == "frontend.vue-emerald");
        Assert.IsNotNull(found);
        Assert.AreEqual("Vue Emerald", found.DisplayName);
        Assert.AreEqual("#0B1B14", found.Colors.BgPrimary);
        Assert.AreEqual("#42B883", found.Colors.Accent);
        Assert.AreEqual("#E8FAF0", found.Colors.FgPrimary);
    }

    [TestMethod]
    public void ThemeManager_ApplyTheme_ChangesActiveThemeAndRaisesEvent()
    {
        var manager = new ThemeManager(_tempSettingsPath);
        var dracula = manager.RegisteredThemes.First(t => t.Id == "recluse.dracula");

        IThemeDefinition? eventTheme = null;
        manager.ThemeChanged += theme => eventTheme = theme;

        manager.ApplyTheme(dracula, persist: false);

        Assert.AreEqual(dracula.Id, manager.ActiveTheme.Id);
        Assert.IsNotNull(eventTheme);
        Assert.AreEqual(dracula.Id, eventTheme.Id);
    }

    [TestMethod]
    public void ThemeManager_PreviewAndRollback_RestoresInitialTheme()
    {
        var manager = new ThemeManager(_tempSettingsPath);
        var original = manager.ActiveTheme;
        var nord = manager.RegisteredThemes.First(t => t.Id == "recluse.nord");

        IThemeDefinition? latestTheme = null;
        manager.ThemeChanged += theme => latestTheme = theme;

        // Preview nord
        manager.PreviewTheme(nord);
        Assert.IsNotNull(latestTheme);
        Assert.AreEqual(nord.Id, latestTheme.Id);

        // Rollback
        manager.RollbackPreview();
        Assert.AreEqual(original.Id, manager.ActiveTheme.Id);
        Assert.AreEqual(original.Id, latestTheme.Id);
    }

    [TestMethod]
    public void ThemeManager_Persistence_RoundtripsUserPreference()
    {
        var manager1 = new ThemeManager(_tempSettingsPath);
        var monokai = manager1.RegisteredThemes.First(t => t.Id == "recluse.monokai");

        // Apply and persist
        manager1.ApplyTheme(monokai, persist: true);
        Assert.IsTrue(File.Exists(_tempSettingsPath));

        // Create new manager with same settings file
        var manager2 = new ThemeManager(_tempSettingsPath);
        manager2.LoadPersistedTheme();

        Assert.AreEqual("recluse.monokai", manager2.ActiveTheme.Id);
    }

    [TestMethod]
    public void LivePreviewBridge_MarkdownToHtml_AppliesThemeColors()
    {
        var md = "# Cyberpunk Preview\n\nGlowing laser text.";
        var colors = new ThemeColors
        {
            MarkdownBg = "#0E0A1E",
            MarkdownFg = "#05D9E8",
            FgAccent = "#FF2A85",
            Accent = "#05D9E8",
            BorderDark = "#2A1F4E",
            MarkdownCodeBg = "#1A1435"
        };

        var html = LivePreviewBridge.MarkdownToHtml(md, "test.md", colors);

        Assert.Contains("--bg: #0E0A1E;", html);
        Assert.Contains("--text: #05D9E8;", html);
        Assert.Contains("--heading: #FF2A85;", html);
        Assert.Contains("--link: #05D9E8;", html);
        Assert.Contains("--code-bg: #1A1435;", html);
        Assert.Contains("Cyberpunk Preview", html);
    }

    private sealed class CustomTestTheme : IThemeDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string? Description => "Custom test theme";
        public string? Author => "Unit Test";
        public ThemeType Type => ThemeType.Dark;
        public ThemeColors Colors { get; }

        public CustomTestTheme(string id, string name, ThemeColors colors)
        {
            Id = id;
            DisplayName = name;
            Colors = colors;
        }
    }
}
