using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.React.Providers;

/// <summary>
/// Provides inline autocomplete and snippets for Redux Toolkit (RTK) and React-Redux.
/// </summary>
public class ReduxCompletionProvider : IInlineCompletionProvider
{
    public string Id => "redux.inline.completion";
    public string Name => "Redux Toolkit & React-Redux Completions";

    public IReadOnlyList<string> SupportedLanguages => ["jsx", "tsx", "javascript", "typescript"];

    private static readonly Dictionary<string, string> ReduxCompletions = new()
    {
        // Redux Toolkit createSlice
        { "createSl", "ice({\n  name: 'feature',\n  initialState: {\n    value: 0,\n    loading: false\n  },\n  reducers: {\n    increment: (state) => {\n      state.value += 1;\n    },\n    reset: (state) => {\n      state.value = 0;\n    }\n  }\n});" },

        // createAsyncThunk
        { "createAsync", "Thunk(\n  'feature/fetchData',\n  async (arg, thunkAPI) => {\n    const response = await fetch('/api/data');\n    return await response.json();\n  }\n);" },

        // configureStore
        { "configureSt", "ore({\n  reducer: {\n    \n  }\n});" },

        // React-Redux Hooks
        { "useSelect", "or((state: RootState) => state.);" },
        { "useDisp", "atch<AppDispatch>();" },

        // Types & Actions
        { "PayloadAction<", "string>" },
        { "extraReducers: (builder) =>", " {\n    builder\n      .addCase(fetchData.pending, (state) => {\n        state.loading = true;\n      })\n      .addCase(fetchData.fulfilled, (state, action) => {\n        state.loading = false;\n      })\n      .addCase(fetchData.rejected, (state, action) => {\n        state.loading = false;\n      });\n  }" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        foreach (var (key, completion) in ReduxCompletions)
        {
            if (prefix.EndsWith(key, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(completion);
            }

            for (var len = key.Length - 1; len >= 5; len--)
            {
                var subKey = key[..len];
                if (prefix.EndsWith(subKey, StringComparison.Ordinal))
                {
                    var remainder = key[len..] + completion;
                    return Task.FromResult<string?>(remainder);
                }
            }
        }

        return Task.FromResult<string?>(null);
    }
}
