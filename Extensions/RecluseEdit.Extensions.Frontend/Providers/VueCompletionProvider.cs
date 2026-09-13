using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Providers;

/// <summary>
/// Provides inline ghost-text completions for Vue 3 Single File Components,
/// script setup, TypeScript definitions, Composition API, and template directives.
/// </summary>
public class VueCompletionProvider : IInlineCompletionProvider
{
    public string Id => "frontend.vue.inline";
    public string Name => "Vue 3 Composition API & SFC Templates";

    public IReadOnlyList<string> SupportedLanguages => ["vue"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "<template",
            ">\n  <div class=\"container\">\n    <h1>{{ title }}</h1>\n  </div>\n</template>\n\n<script setup lang=\"ts\">\nimport { ref } from 'vue';\n\nconst title = ref('Hello Vue 3');\n</script>\n\n<style scoped>\n.container {\n  padding: 1rem;\n}\n</style>"
        },
        {
            "const count = ref(",
            "0);"
        },
        {
            "const state = reactive(",
            "{\n  loading: false,\n  items: []\n});"
        },
        {
            "const double = computed(",
            "() => count.value * 2);"
        },
        {
            "watch(",
            "source, (newVal, oldVal) => {\n  console.log(newVal);\n});"
        },
        {
            "watchEffect(",
            "() => {\n  console.log(state);\n});"
        },
        {
            "onMounted(",
            "() => {\n  \n});"
        },
        {
            "onUnmounted(",
            "() => {\n  \n});"
        },
        {
            "const props = defineProps<",
            "{\n  title: string;\n  count?: number;\n}>();"
        },
        {
            "const emit = defineEmits<",
            "{\n  (e: 'change', value: string): void;\n  (e: 'close'): void;\n}>();"
        },
        {
            "const model = defineModel<",
            "string>();"
        },
        {
            "v-for=\"",
            "item in items\" :key=\"item.id\""
        },
        {
            "v-if=\"",
            "condition\""
        },
        {
            "@click=\"",
            "handleClick\""
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

