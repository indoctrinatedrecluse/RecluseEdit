using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.NodeBackend.Providers;

/// <summary>
/// Provides inline completions for NestJS enterprise TypeScript backend applications:
/// controllers, services, dependency injection, modules, route decorators, guards, and DTOs.
/// </summary>
public class NestJsCompletionProvider : IInlineCompletionProvider
{
    public string Id => "nodebackend.nestjs.inline";
    public string Name => "NestJS Enterprise Backend & Decorators";

    public IReadOnlyList<string> SupportedLanguages => ["typescript", "javascript", "ts", "js"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "@Controller('",
            "items')\nexport class ItemsController {\n  constructor(private readonly itemsService: ItemsService) {}\n\n  @Get()\n  findAll() {\n    return this.itemsService.findAll();\n  }\n}"
        },
        {
            "@Injectable(",
            ")\nexport class ItemsService {\n  private readonly items: any[] = [];\n\n  findAll() {\n    return this.items;\n  }\n}"
        },
        {
            "@Module({",
            "\n  imports: [],\n  controllers: [AppController],\n  providers: [AppService],\n  exports: [AppService],\n})\nexport class AppModule {}"
        },
        {
            "@Get('",
            ":id')\n  findOne(@Param('id') id: string) {\n    return this.itemsService.findOne(id);\n  }"
        },
        {
            "@Post(",
            ")\n  create(@Body() createDto: CreateDto) {\n    return this.itemsService.create(createDto);\n  }"
        },
        {
            "@Put('",
            ":id')\n  update(@Param('id') id: string, @Body() updateDto: UpdateDto) {\n    return this.itemsService.update(id, updateDto);\n  }"
        },
        {
            "@Delete('",
            ":id')\n  remove(@Param('id') id: string) {\n    return this.itemsService.remove(id);\n  }"
        },
        {
            "class CreateItemDto {",
            "\n  @IsString()\n  @IsNotEmpty()\n  name: string;\n\n  @IsNumber()\n  price: number;\n}"
        },
        {
            "export class AuthGuard implements CanActivate {",
            "\n  canActivate(context: ExecutionContext): boolean {\n    const request = context.switchToHttp().getRequest();\n    return !!request.headers.authorization;\n  }\n}"
        }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
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

