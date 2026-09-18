using RecluseEdit.Extensions.Frontend.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class JsonCodeGeneratorTests
{
    private const string ComplexJson = """
    {
      "id": 42,
      "title": "Super Task",
      "completed": false,
      "author": {
        "name": "Jane",
        "email": "jane@example.com"
      },
      "tags": ["frontend", "react", "csharp"]
    }
    """;

    [TestMethod]
    public void GenerateTypeScript_CreatesValidInterfaces()
    {
        var ts = JsonCodeGeneratorService.GenerateTypeScript(ComplexJson, "TaskModel");

        StringAssert.Contains(ts, "export interface TaskModel");
        StringAssert.Contains(ts, "id: number;");
        StringAssert.Contains(ts, "title: string;");
        StringAssert.Contains(ts, "completed: boolean;");
        StringAssert.Contains(ts, "author: Author;");
        StringAssert.Contains(ts, "tags: string[];");
        StringAssert.Contains(ts, "export interface Author");
        StringAssert.Contains(ts, "name: string;");
        StringAssert.Contains(ts, "email: string;");
    }

    [TestMethod]
    public void GenerateZodSchema_CreatesValidSchema()
    {
        var zod = JsonCodeGeneratorService.GenerateZodSchema(ComplexJson, "taskSchema");

        StringAssert.Contains(zod, "import { z } from \"zod\";");
        StringAssert.Contains(zod, "export const taskSchema = z.object({");
        StringAssert.Contains(zod, "id: z.number(),");
        StringAssert.Contains(zod, "title: z.string(),");
        StringAssert.Contains(zod, "completed: z.boolean(),");
        StringAssert.Contains(zod, "tags: z.array(z.string())");
        StringAssert.Contains(zod, "export type Task = z.infer<typeof taskSchema>;");
    }

    [TestMethod]
    public void GenerateCSharpRecord_CreatesValidRecords()
    {
        var cs = JsonCodeGeneratorService.GenerateCSharpRecord(ComplexJson, "TaskRecord");

        StringAssert.Contains(cs, "using System.Text.Json.Serialization;");
        StringAssert.Contains(cs, "public record TaskRecord(");
        StringAssert.Contains(cs, "[property: JsonPropertyName(\"id\")] long Id");
        StringAssert.Contains(cs, "[property: JsonPropertyName(\"title\")] string Title");
        StringAssert.Contains(cs, "[property: JsonPropertyName(\"author\")] Author Author");
        StringAssert.Contains(cs, "public record Author(");
        StringAssert.Contains(cs, "[property: JsonPropertyName(\"email\")] string Email");
    }

    [TestMethod]
    public void FormatJson_PrettifiesAndMinifies()
    {
        var min = JsonCodeGeneratorService.FormatJson(ComplexJson, minified: true);
        Assert.DoesNotContain("\n", min);
        Assert.StartsWith("{\"id\":42", min);

        var pretty = JsonCodeGeneratorService.FormatJson(min, minified: false);
        Assert.Contains("\n", pretty);
        Assert.Contains("  \"id\": 42", pretty);
    }

    [TestMethod]
    public void Generate_HandlesEmptyInputGracefully()
    {
        Assert.AreEqual(string.Empty, JsonCodeGeneratorService.GenerateTypeScript(""));
        Assert.AreEqual(string.Empty, JsonCodeGeneratorService.GenerateZodSchema(""));
        Assert.AreEqual(string.Empty, JsonCodeGeneratorService.GenerateCSharpRecord(""));
    }
}

