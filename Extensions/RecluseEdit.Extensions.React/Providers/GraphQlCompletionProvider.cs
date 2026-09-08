using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.React.Providers;

/// <summary>
/// Provides inline autocomplete and schema snippets for GraphQL and Apollo Client.
/// </summary>
public class GraphQlCompletionProvider : IInlineCompletionProvider
{
    public string Id => "graphql.inline.completion";
    public string Name => "GraphQL & Apollo Client Completions";

    public IReadOnlyList<string> SupportedLanguages => ["graphql", "jsx", "tsx", "javascript", "typescript"];

    private static readonly Dictionary<string, string> GraphQlCompletions = new()
    {
        // Queries & Mutations
        { "query ", "GetItem($id: ID!) {\n  item(id: $id) {\n    id\n    name\n    description\n  }\n}" },
        { "mutation ", "CreateItem($input: CreateItemInput!) {\n  createItem(input: $input) {\n    id\n    success\n  }\n}" },
        { "subscription ", "OnItemUpdated($id: ID!) {\n  itemUpdated(id: $id) {\n    id\n    status\n  }\n}" },
        { "fragment ", "ItemFields on Item {\n  id\n  title\n  createdAt\n}" },

        // Schema Definitions
        { "type ", "User {\n  id: ID!\n  name: String!\n  email: String!\n  roles: [String!]!\n}" },
        { "input ", "UserInput {\n  name: String!\n  email: String!\n}" },
        { "enum ", "Status {\n  ACTIVE\n  PENDING\n  ARCHIVED\n}" },
        { "interface ", "Node {\n  id: ID!\n}" },

        // Apollo Client React Hooks
        { "useQue", "ry(GET_DATA, {\n  variables: { id }\n});" },
        { "useMut", "ation(UPDATE_DATA);" },
        { "useSub", "scription(SUBSCRIBE_DATA);" },
        { "const GET_", "DATA = gql`\n  query GetData {\n    \n  }\n`;" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        foreach (var (key, completion) in GraphQlCompletions)
        {
            if (prefix.EndsWith(key, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(completion);
            }

            for (var len = key.Length - 1; len >= 4; len--)
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

