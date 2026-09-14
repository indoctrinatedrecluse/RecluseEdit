using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class StatusBarProviderTests
{
    [TestMethod]
    public void TestStatusBarItemInitializationAndProperties()
    {
        var item = new StatusBarItem("test.item", "Status OK", StatusBarAlignment.Left, priority: 50)
        {
            Tooltip = "System healthy",
            Icon = "⚡",
            IsVisible = true
        };

        Assert.AreEqual("test.item", item.Id);
        Assert.AreEqual("Status OK", item.Text);
        Assert.AreEqual(StatusBarAlignment.Left, item.Alignment);
        Assert.AreEqual(50, item.Priority);
        Assert.AreEqual("System healthy", item.Tooltip);
        Assert.AreEqual("⚡", item.Icon);
        Assert.IsTrue(item.IsVisible);
    }

    [TestMethod]
    public void TestStatusBarItemChangeEvents()
    {
        var item = new StatusBarItem("test.events", "Initial");
        int eventCount = 0;
        item.Changed += (_, _) => eventCount++;

        item.Text = "Updated";
        item.Tooltip = "New Tooltip";
        item.IsVisible = false;
        item.Alignment = StatusBarAlignment.Left;
        item.Priority = 100;

        Assert.AreEqual(5, eventCount);
        Assert.AreEqual("Updated", item.Text);
        Assert.AreEqual("New Tooltip", item.Tooltip);
        Assert.IsFalse(item.IsVisible);
        Assert.AreEqual(StatusBarAlignment.Left, item.Alignment);
        Assert.AreEqual(100, item.Priority);
    }

    [TestMethod]
    public void TestStatusBarItemClickCallback()
    {
        bool clicked = false;
        var item = new StatusBarItem("test.click", "Clickable")
        {
            OnClick = () => clicked = true
        };

        Assert.IsNotNull(item.OnClick);
        item.OnClick.Invoke();
        Assert.IsTrue(clicked);
    }

    [TestMethod]
    public void TestExtensionManagerStatusBarRegistration()
    {
        var syntaxManager = new SyntaxManager();
        var autocompleteManager = new AutocompleteManager();
        var toolchainManager = new ToolchainManager();
        var extManager = new ExtensionManager(syntaxManager, autocompleteManager, toolchainManager);

        var registeredItems = new List<IStatusBarItem>();
        extManager.StatusBarItemRegistered += item => registeredItems.Add(item);

        var item1 = new StatusBarItem("ext.item1", "Item 1", StatusBarAlignment.Right, 10);
        var item2 = new StatusBarItem("ext.item2", "Item 2", StatusBarAlignment.Left, 20);

        extManager.RegisterStatusBarItem(item1);
        extManager.RegisterStatusBarItem(item2);

        // Duplicate registration should be ignored
        extManager.RegisterStatusBarItem(item1);

        Assert.HasCount(2, extManager.RegisteredStatusBarItems);
        Assert.HasCount(2, registeredItems);
        Assert.AreEqual("Item 1", extManager.RegisteredStatusBarItems[0].Text);
        Assert.AreEqual("Item 2", extManager.RegisteredStatusBarItems[1].Text);
    }

    private class SampleStatusBarProvider : IStatusBarProvider
    {
        private readonly List<IStatusBarItem> _items = [];
        public string Id => "sample.provider";

        public event EventHandler? ItemsChanged;

        public SampleStatusBarProvider(params IStatusBarItem[] items)
        {
            _items.AddRange(items);
        }

        public IReadOnlyList<IStatusBarItem> GetItems() => _items.AsReadOnly();

        public void AddItem(IStatusBarItem item)
        {
            _items.Add(item);
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [TestMethod]
    public void TestExtensionManagerStatusBarProviderRegistration()
    {
        var syntaxManager = new SyntaxManager();
        var autocompleteManager = new AutocompleteManager();
        var toolchainManager = new ToolchainManager();
        var extManager = new ExtensionManager(syntaxManager, autocompleteManager, toolchainManager);

        var itemA = new StatusBarItem("prov.itemA", "Provider Item A");
        var provider = new SampleStatusBarProvider(itemA);

        extManager.RegisterStatusBarProvider(provider);

        Assert.HasCount(1, extManager.RegisteredStatusBarProviders);
        Assert.HasCount(1, extManager.RegisteredStatusBarItems);
        Assert.AreEqual("Provider Item A", extManager.RegisteredStatusBarItems[0].Text);

        // Add dynamic item through provider
        var itemB = new StatusBarItem("prov.itemB", "Provider Item B");
        provider.AddItem(itemB);

        Assert.HasCount(2, extManager.RegisteredStatusBarItems);
    }
}
