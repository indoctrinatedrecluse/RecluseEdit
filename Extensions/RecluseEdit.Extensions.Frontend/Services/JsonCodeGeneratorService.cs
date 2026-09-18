using System.Text;
using System.Text.Json;

namespace RecluseEdit.Extensions.Frontend.Services;

/// <summary>
/// Service for transforming JSON payloads into TypeScript interfaces, Zod schemas, C# records, and formatted JSON.
/// </summary>
public static class JsonCodeGeneratorService
{
    /// <summary>
    /// Generates TypeScript interfaces from JSON.
    /// </summary>
    public static string GenerateTypeScript(string json, string rootTypeName = "Root")
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;

        using var doc = JsonDocument.Parse(json);
        var interfaces = new Dictionary<string, List<(string Name, string Type)>>();
        
        var rootType = InferType(doc.RootElement, rootTypeName, interfaces);

        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated TypeScript definitions by RecluseEdit");
        sb.AppendLine();

        // Emit all collected interfaces (children first, root last)
        foreach (var (typeName, fields) in interfaces)
        {
            sb.AppendLine($"export interface {typeName} {{");
            foreach (var (fName, fType) in fields)
            {
                var safeProp = NeedsQuotes(fName) ? $"\"{fName}\"" : fName;
                sb.AppendLine($"  {safeProp}: {fType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (!interfaces.ContainsKey(rootTypeName))
        {
            sb.AppendLine($"export type {rootTypeName} = {rootType};");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates a Zod validation schema from JSON.
    /// </summary>
    public static string GenerateZodSchema(string json, string rootSchemaName = "rootSchema")
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;

        using var doc = JsonDocument.Parse(json);
        var sb = new StringBuilder();
        sb.AppendLine("import { z } from \"zod\";");
        sb.AppendLine();
        sb.AppendLine($"export const {rootSchemaName} = {BuildZodType(doc.RootElement, 0)};");
        sb.AppendLine();
        
        var typeName = char.ToUpperInvariant(rootSchemaName[0]) + rootSchemaName[1..];
        if (typeName.EndsWith("Schema", StringComparison.OrdinalIgnoreCase))
        {
            typeName = typeName[..^6];
        }
        if (string.IsNullOrEmpty(typeName)) typeName = "Root";

        sb.AppendLine($"export type {typeName} = z.infer<typeof {rootSchemaName}>;");
        return sb.ToString();
    }

    /// <summary>
    /// Generates C# 10/12 records with System.Text.Json attributes from JSON.
    /// </summary>
    public static string GenerateCSharpRecord(string json, string rootClassName = "RootModel")
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;

        using var doc = JsonDocument.Parse(json);
        var records = new Dictionary<string, List<(string PropName, string JsonName, string Type)>>();

        InferCSharpType(doc.RootElement, rootClassName, records);

        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        foreach (var (recName, fields) in records)
        {
            sb.AppendLine($"public record {recName}(");
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                var comma = i < fields.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"    [property: JsonPropertyName(\"{f.JsonName}\")] {f.Type} {f.PropName}{comma}");
            }
            sb.AppendLine(");");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Prettifies or minifies a JSON string.
    /// </summary>
    public static string FormatJson(string json, bool minified)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;

        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented = !minified
        });
    }

    #region Helpers

    private static string InferType(JsonElement el, string suggestedName, Dictionary<string, List<(string Name, string Type)>> interfaces)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return "string";
            case JsonValueKind.Number:
                return "number";
            case JsonValueKind.True:
            case JsonValueKind.False:
                return "boolean";
            case JsonValueKind.Null:
                return "null";
            case JsonValueKind.Array:
                var items = el.EnumerateArray().ToList();
                if (items.Count == 0) return "any[]";
                var itemSuggested = suggestedName.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                    ? suggestedName[..^1]
                    : suggestedName + "Item";
                var elemType = InferType(items[0], itemSuggested, interfaces);
                return $"{elemType}[]";
            case JsonValueKind.Object:
                var typeName = ToPascalCase(suggestedName);
                var fields = new List<(string Name, string Type)>();

                foreach (var prop in el.EnumerateObject())
                {
                    var childSuggested = ToPascalCase(prop.Name);
                    var childType = InferType(prop.Value, childSuggested, interfaces);
                    fields.Add((prop.Name, childType));
                }

                interfaces[typeName] = fields;
                return typeName;
            default:
                return "any";
        }
    }

    private static string BuildZodType(JsonElement el, int indent)
    {
        var spaces = new string(' ', indent * 2);
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return "z.string()";
            case JsonValueKind.Number:
                return "z.number()";
            case JsonValueKind.True:
            case JsonValueKind.False:
                return "z.boolean()";
            case JsonValueKind.Null:
                return "z.null()";
            case JsonValueKind.Array:
                var items = el.EnumerateArray().ToList();
                if (items.Count == 0) return "z.array(z.any())";
                return $"z.array({BuildZodType(items[0], indent)})";
            case JsonValueKind.Object:
                var sb = new StringBuilder();
                sb.AppendLine("z.object({");
                var props = el.EnumerateObject().ToList();
                foreach (var prop in props)
                {
                    var child = BuildZodType(prop.Value, indent + 1);
                    sb.AppendLine($"{spaces}  {prop.Name}: {child},");
                }
                sb.Append($"{spaces}}})");
                return sb.ToString();
            default:
                return "z.any()";
        }
    }

    private static string InferCSharpType(JsonElement el, string suggestedName, Dictionary<string, List<(string PropName, string JsonName, string Type)>> records)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return "string";
            case JsonValueKind.Number:
                return el.TryGetInt64(out _) ? "long" : "double";
            case JsonValueKind.True:
            case JsonValueKind.False:
                return "bool";
            case JsonValueKind.Null:
                return "object?";
            case JsonValueKind.Array:
                var items = el.EnumerateArray().ToList();
                if (items.Count == 0) return "List<object>";
                var itemSuggested = suggestedName.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                    ? suggestedName[..^1]
                    : suggestedName + "Item";
                var elemType = InferCSharpType(items[0], itemSuggested, records);
                return $"List<{elemType}>";
            case JsonValueKind.Object:
                var recName = ToPascalCase(suggestedName);
                var fields = new List<(string PropName, string JsonName, string Type)>();

                foreach (var prop in el.EnumerateObject())
                {
                    var childName = ToPascalCase(prop.Name);
                    var childType = InferCSharpType(prop.Value, childName, records);
                    fields.Add((childName, prop.Name, childType));
                }

                records[recName] = fields;
                return recName;
            default:
                return "object";
        }
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Item";
        var clean = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        var parts = clean.Split(['_', ' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Item";

        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            sb.Append(char.ToUpperInvariant(p[0]) + p[1..]);
        }
        return sb.ToString();
    }

    private static bool NeedsQuotes(string propName)
    {
        return !propName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    #endregion
}

