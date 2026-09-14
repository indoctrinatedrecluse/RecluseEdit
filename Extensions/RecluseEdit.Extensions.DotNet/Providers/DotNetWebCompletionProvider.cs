using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.DotNet.Providers;

/// <summary>
/// Provides inline completions for ASP.NET Core Minimal APIs, Blazor components (.razor),
/// Razor Pages (.cshtml), and Entity Framework Core.
/// </summary>
public class DotNetWebCompletionProvider : IInlineCompletionProvider
{
    public string Id => "dotnet.web.inline";
    public string Name => "ASP.NET Core, Blazor & EF Core";

    public IReadOnlyList<string> SupportedLanguages => ["csharp", "cs", "razor", "cshtml"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Minimal APIs & Web API Endpoints
        { "app.MapGet(\"", "/\", () => Results.Ok(new { message = \"Hello from ASP.NET Core!\", timestamp = DateTime.UtcNow }));" },
        { "app.MapPost(\"", "\", async (CreateItemDto dto, AppDbContext db) => {\n    var item = new Item { Name = dto.Name };\n    db.Items.Add(item);\n    await db.SaveChangesAsync();\n    return Results.Created($\"/api/items/{item.Id}\", item);\n});" },
        { "app.MapPut(\"", "/{id}\", async (int id, UpdateItemDto dto, AppDbContext db) => {\n    var item = await db.Items.FindAsync(id);\n    if (item is null) return Results.NotFound();\n    item.Name = dto.Name;\n    await db.SaveChangesAsync();\n    return Results.NoContent();\n});" },
        { "app.MapDelete(\"", "/{id}\", async (int id, AppDbContext db) => {\n    var item = await db.Items.FindAsync(id);\n    if (item is null) return Results.NotFound();\n    db.Items.Remove(item);\n    await db.SaveChangesAsync();\n    return Results.NoContent();\n});" },

        // Builder & Services
        { "var builder = WebApplication.CreateBuilder(", "args);\nbuilder.Services.AddEndpointsApiExplorer();\nbuilder.Services.AddSwaggerGen();\n\nvar app = builder.Build();\nif (app.Environment.IsDevelopment()) {\n    app.UseSwagger();\n    app.UseSwaggerUI();\n}\n\napp.Run();" },
        { "builder.Services.AddDbContext<", "AppDbContext>(options =>\n    options.UseSqlServer(builder.Configuration.GetConnectionString(\"DefaultConnection\")));" },

        // Blazor Component Templates
        { "@page \"", "/my-page\"\n@rendermode InteractiveServer\n\n<PageTitle>My Page</PageTitle>\n\n<h3>My Page</h3>\n\n@code {\n\n}" },
        { "@code {", "\n    [Parameter] public string Title { get; set; } = string.Empty;\n    [Parameter] public EventCallback<string> TitleChanged { get; set; }\n\n    protected override async Task OnInitializedAsync()\n    {\n        await Task.Yield();\n    }\n}" },
        { "@inject NavigationManager ", "Navigation" },
        { "@inject HttpClient ", "Http" },
        { "@inject IJSRuntime ", "JS" },
        { "[Parameter] public ", "string Text { get; set; } = string.Empty;" },
        { "protected override async Task OnInitializedAsync(", ") {\n    await base.OnInitializedAsync();\n}" },
        { "protected override async Task OnParametersSetAsync(", ") {\n    await base.OnParametersSetAsync();\n}" },

        // EF Core Models & Context
        { "public class AppDbContext : DbContext", "\n{\n    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }\n\n    public DbSet<Item> Items => Set<Item>();\n}" },
        { "public DbSet<", "Item> Items => Set<Item>();" },
        { "await db.SaveChangesAsync(", ");" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var prefix = !string.IsNullOrEmpty(context.TextBeforeCaret)
            ? context.TextBeforeCaret
            : context.CurrentLineText;

        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        foreach (var (trigger, suggestion) in Completions)
        {
            if (prefix.EndsWith(trigger, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
