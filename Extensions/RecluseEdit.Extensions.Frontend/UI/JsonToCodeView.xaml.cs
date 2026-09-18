using System.Windows;
using System.Windows.Controls;
using RecluseEdit.Extensions.Frontend.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Frontend.UI;

public partial class JsonToCodeView : UserControl
{
    private readonly IWorkspaceContext? _workspaceContext;

    public JsonToCodeView(IWorkspaceContext? workspaceContext = null)
    {
        InitializeComponent();
        _workspaceContext = workspaceContext;

        TxtInputJson.Text = SampleJson;
        GenerateCode();
    }

    private void OnInputJsonChanged(object sender, TextChangedEventArgs e)
    {
        GenerateCode();
    }

    private void OnTargetChanged(object sender, RoutedEventArgs e)
    {
        GenerateCode();
    }

    private void OnRootNameChanged(object sender, TextChangedEventArgs e)
    {
        GenerateCode();
    }

    private void GenerateCode()
    {
        var json = TxtInputJson.Text;
        if (string.IsNullOrWhiteSpace(json))
        {
            TxtOutputCode.Text = string.Empty;
            TxtStatus.Text = "Paste JSON payload to generate code.";
            return;
        }

        var rootName = string.IsNullOrWhiteSpace(TxtRootName.Text) ? "RootModel" : TxtRootName.Text.Trim();

        try
        {
            if (RadioTs.IsChecked == true)
            {
                TxtOutputCode.Text = JsonCodeGeneratorService.GenerateTypeScript(json, rootName);
                TxtStatus.Text = "Generated TypeScript interfaces.";
            }
            else if (RadioZod.IsChecked == true)
            {
                TxtOutputCode.Text = JsonCodeGeneratorService.GenerateZodSchema(json, rootName);
                TxtStatus.Text = "Generated Zod validation schema.";
            }
            else if (RadioCSharp.IsChecked == true)
            {
                TxtOutputCode.Text = JsonCodeGeneratorService.GenerateCSharpRecord(json, rootName);
                TxtStatus.Text = "Generated C# records.";
            }
        }
        catch (Exception ex)
        {
            TxtOutputCode.Text = $"// JSON Parsing Error:\n// {ex.Message}";
            TxtStatus.Text = "Invalid JSON syntax.";
        }
    }

    private void OnFormatJsonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var formatted = JsonCodeGeneratorService.FormatJson(TxtInputJson.Text, minified: false);
            TxtInputJson.Text = formatted;
            TxtStatus.Text = "JSON formatted.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Format error: {ex.Message}";
        }
    }

    private void OnLoadDocClick(object sender, RoutedEventArgs e)
    {
        var text = _workspaceContext?.ActiveDocumentContent;
        if (!string.IsNullOrWhiteSpace(text) && (text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("[")))
        {
            TxtInputJson.Text = text;
            TxtStatus.Text = "Loaded JSON from active document.";
        }
        else
        {
            TxtStatus.Text = "Active document is not a JSON document.";
        }
    }

    private void OnCopyCodeClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtOutputCode.Text))
        {
            Clipboard.SetText(TxtOutputCode.Text);
            TxtStatus.Text = "Copied generated code to clipboard!";
        }
    }

    private async void OnNewFileClick(object sender, RoutedEventArgs e)
    {
        var code = TxtOutputCode.Text;
        if (string.IsNullOrEmpty(code)) return;

        var ext = RadioCSharp.IsChecked == true ? "cs" : "ts";
        if (_workspaceContext?.WorkspaceRoot != null)
        {
            var filePath = System.IO.Path.Combine(_workspaceContext.WorkspaceRoot, $"GeneratedModel.{ext}");
            await _workspaceContext.WriteFileAsync(filePath, code);
            _workspaceContext.OpenFile(filePath);
            TxtStatus.Text = $"Created and opened: GeneratedModel.{ext}";
        }
        else
        {
            Clipboard.SetText(code);
            TxtStatus.Text = "No workspace open. Copied types to clipboard!";
        }
    }

    private const string SampleJson = """
    {
      "id": 101,
      "username": "indoctrinatedrecluse",
      "email": "recluse@example.com",
      "isAdmin": true,
      "roles": ["developer", "designer"],
      "profile": {
        "bio": "Building RecluseEdit",
        "avatarUrl": "https://example.com/avatar.png",
        "reputation": 4980
      }
    }
    """;
}

