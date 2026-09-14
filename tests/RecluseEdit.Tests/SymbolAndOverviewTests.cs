using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.UI.Controls;

namespace RecluseEdit.Tests;

[TestClass]
public class SymbolAndOverviewTests
{
    [TestMethod]
    public void TestExtractCSharpSymbols()
    {
        string csharpCode = @"
namespace Demo;

public interface IService
{
    void DoWork();
}

public class MyWorker : IService
{
    public MyWorker()
    {
    }

    public void DoWork()
    {
        int x = 10;
    }

    public static async Task<bool> RunAsync()
    {
        return true;
    }
}

public enum Status
{
    Active,
    Inactive
}
";

        var symbols = DocumentSymbolService.ExtractSymbols(csharpCode, "Worker.cs");

        Assert.IsNotNull(symbols);
        Assert.IsTrue(symbols.Any(s => s.Name == "IService" && s.Kind == SymbolKind.Interface));
        Assert.IsTrue(symbols.Any(s => s.Name == "MyWorker" && s.Kind == SymbolKind.Class));
        Assert.IsTrue(symbols.Any(s => s.Name == "DoWork" && s.Kind == SymbolKind.Method));
        Assert.IsTrue(symbols.Any(s => s.Name == "RunAsync" && s.Kind == SymbolKind.Method));
        Assert.IsTrue(symbols.Any(s => s.Name == "Status" && s.Kind == SymbolKind.Enum));
    }

    [TestMethod]
    public void TestExtractPythonSymbols()
    {
        string pyCode = @"
class DataPipeline:
    def __init__(self, name):
        self.name = name

    def process(self, data):
        return [x * 2 for x in data]

def calculate_stats(numbers):
    return sum(numbers)
";

        var symbols = DocumentSymbolService.ExtractSymbols(pyCode, "pipeline.py");

        Assert.IsNotNull(symbols);
        Assert.IsTrue(symbols.Any(s => s.Name == "DataPipeline" && s.Kind == SymbolKind.Class));
        Assert.IsTrue(symbols.Any(s => s.Name == "__init__" && s.Kind == SymbolKind.Constructor));
        Assert.IsTrue(symbols.Any(s => s.Name == "process" && s.Kind == SymbolKind.Method));
        Assert.IsTrue(symbols.Any(s => s.Name == "calculate_stats" && s.Kind == SymbolKind.Function));
    }

    [TestMethod]
    public void TestExtractJavaScriptAndTypeScriptSymbols()
    {
        string jsCode = @"
export class AuthService {
    constructor() {
        this.token = null;
    }

    async login(user, pass) {
        return true;
    }
}

function helperUtil() {
    return 42;
}

const computeTotal = (a, b) => {
    return a + b;
};
";

        var symbols = DocumentSymbolService.ExtractSymbols(jsCode, "auth.ts");

        Assert.IsNotNull(symbols);
        Assert.IsTrue(symbols.Any(s => s.Name == "AuthService" && s.Kind == SymbolKind.Class));
        Assert.IsTrue(symbols.Any(s => s.Name == "constructor" && s.Kind == SymbolKind.Constructor));
        Assert.IsTrue(symbols.Any(s => s.Name == "login" && s.Kind == SymbolKind.Method));
        Assert.IsTrue(symbols.Any(s => s.Name == "helperUtil" && s.Kind == SymbolKind.Function));
        Assert.IsTrue(symbols.Any(s => s.Name == "computeTotal" && s.Kind == SymbolKind.Function));
    }

    [TestMethod]
    public void TestExtractGoSymbols()
    {
        string goCode = @"
package main

type Server struct {
    port int
}

func (s *Server) Start() error {
    return nil
}

func HandleRequest() {
}
";

        var symbols = DocumentSymbolService.ExtractSymbols(goCode, "main.go");

        Assert.IsNotNull(symbols);
        Assert.IsTrue(symbols.Any(s => s.Name == "Server" && s.Kind == SymbolKind.Struct));
        Assert.IsTrue(symbols.Any(s => s.Name == "Start" && s.Kind == SymbolKind.Method && s.ContainerName == "Server"));
        Assert.IsTrue(symbols.Any(s => s.Name == "HandleRequest" && s.Kind == SymbolKind.Function));
    }

    [TestMethod]
    public void TestExtractRustSymbols()
    {
        string rustCode = @"
pub struct Client {
    id: u32,
}

impl Client {
    pub fn new(id: u32) -> Self {
        Client { id }
    }

    pub fn send(&self) {
    }
}

fn standalone_fn() {
}
";

        var symbols = DocumentSymbolService.ExtractSymbols(rustCode, "client.rs");

        Assert.IsNotNull(symbols);
        Assert.IsTrue(symbols.Any(s => s.Name == "Client" && s.Kind == SymbolKind.Struct));
        Assert.IsTrue(symbols.Any(s => s.Name == "new" && s.Kind == SymbolKind.Method));
        Assert.IsTrue(symbols.Any(s => s.Name == "send" && s.Kind == SymbolKind.Method));
        Assert.IsTrue(symbols.Any(s => s.Name == "standalone_fn" && s.Kind == SymbolKind.Function));
    }

    [TestMethod]
    public void TestGetEnclosingScope()
    {
        string code = @"
class TestClass {
    void MethodA() {
        int a = 1;
    }

    void MethodB() {
        int b = 2;
    }
}
";

        var symbols = DocumentSymbolService.ExtractSymbols(code, "test.cs");

        var scopeAtLine1 = DocumentSymbolService.GetEnclosingSymbol(symbols, 1);
        Assert.IsNull(scopeAtLine1);

        var scopeInMethodA = DocumentSymbolService.GetEnclosingSymbol(symbols, 4);
        Assert.IsNotNull(scopeInMethodA);
        Assert.AreEqual("MethodA", scopeInMethodA.Name);

        var scopeInMethodB = DocumentSymbolService.GetEnclosingSymbol(symbols, 8);
        Assert.IsNotNull(scopeInMethodB);
        Assert.AreEqual("MethodB", scopeInMethodB.Name);
    }

    [TestMethod]
    public void TestCommandRegistrySymbolProvider()
    {
        var registry = new CommandRegistry();
        registry.SymbolProvider = filter =>
        [
            new CommandItem { Id = "sym.1", Title = "AlphaMethod", Category = "Method", Action = () => { } },
            new CommandItem { Id = "sym.2", Title = "BetaClass", Category = "Class", Action = () => { } }
        ];

        // Search with '@'
        var allSymbols = registry.Search("@");
        Assert.HasCount(2, allSymbols);

        // Search with filter '@Alpha'
        var filtered = registry.Search("@Alpha");
        Assert.HasCount(1, filtered);
        Assert.AreEqual("AlphaMethod", filtered[0].Title);

        // Help should contain symbol instruction
        var help = registry.Search("?");
        Assert.IsTrue(help.Any(h => h.Title.Contains('@')));
    }

    [TestMethod]
    public void TestOverviewMarkerKindValues()
    {
        var marker = new OverviewMarker(42, OverviewMarkerKind.FindMatch);
        Assert.AreEqual(42, marker.LineNumber);
        Assert.AreEqual(OverviewMarkerKind.FindMatch, marker.Kind);

        var errMarker = new OverviewMarker(100, OverviewMarkerKind.DiagnosticError);
        Assert.AreEqual(OverviewMarkerKind.DiagnosticError, errMarker.Kind);
    }
}
