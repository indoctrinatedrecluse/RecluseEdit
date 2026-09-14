using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline completions for Spring Boot 3+ REST controllers, JPA entities,
/// services, dependency injection, and configuration properties.
/// </summary>
public class SpringBootCompletionProvider : IInlineCompletionProvider
{
    public string Id => "scripting.springboot.inline";
    public string Name => "Java & Spring Boot 3+ Web Framework";

    public IReadOnlyList<string> SupportedLanguages => ["java", "properties", "yaml", "yml"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Controllers & Endpoints
        { "@RestController", "\n@RequestMapping(\"/api/v1/items\")\npublic class ItemController {\n\n    private final ItemService itemService;\n\n    public ItemController(ItemService itemService) {\n        this.itemService = itemService;\n    }\n\n    @GetMapping\n    public ResponseEntity<List<Item>> getAllItems() {\n        return ResponseEntity.ok(itemService.findAll());\n    }\n}" },
        { "@GetMapping(\"", "/{id}\")\npublic ResponseEntity<Item> getItemById(@PathVariable Long id) {\n    return itemService.findById(id)\n            .map(ResponseEntity::ok)\n            .orElse(ResponseEntity.notFound().build());\n}" },
        { "@PostMapping", "\npublic ResponseEntity<Item> createItem(@Valid @RequestBody CreateItemRequest request) {\n    Item created = itemService.create(request);\n    return ResponseEntity.status(HttpStatus.CREATED).body(created);\n}" },
        { "@PutMapping(\"", "/{id}\")\npublic ResponseEntity<Item> updateItem(@PathVariable Long id, @Valid @RequestBody UpdateItemRequest request) {\n    return ResponseEntity.ok(itemService.update(id, request));\n}" },
        { "@DeleteMapping(\"", "/{id}\")\npublic ResponseEntity<Void> deleteItem(@PathVariable Long id) {\n    itemService.delete(id);\n    return ResponseEntity.noContent().build();\n}" },

        // JPA & Persistence
        { "@Entity", "\n@Table(name = \"items\")\npublic class Item {\n    @Id\n    @GeneratedValue(strategy = GenerationType.IDENTITY)\n    private Long id;\n\n    @Column(nullable = false)\n    private String name;\n}" },
        { "@Repository", "\npublic interface ItemRepository extends JpaRepository<Item, Long> {\n    Optional<Item> findByName(String name);\n}" },

        // Services & Injection
        { "@Service", "\n@Transactional\npublic class ItemService {\n    private final ItemRepository itemRepository;\n\n    public ItemService(ItemRepository itemRepository) {\n        this.itemRepository = itemRepository;\n    }\n}" },

        // Main Entrypoint
        { "@SpringBootApplication", "\npublic class Application {\n    public static void main(String[] args) {\n        SpringApplication.run(Application.class, args);\n    }\n}" },

        // Configuration Properties
        { "spring.datasource.url=", "jdbc:postgresql://localhost:5432/mydb" },
        { "spring.datasource.username=", "postgres" },
        { "server.port=", "8080" },
        { "spring.jpa.hibernate.ddl-auto=", "update" }
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
