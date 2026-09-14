using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.RestClient.Providers;

/// <summary>
/// Provides inline completions for GraphQL queries, mutations, subscriptions,
/// fragments, and SDL schema definitions.
/// </summary>
public class GraphQlCompletionProvider : IInlineCompletionProvider
{
    public string Id => "restclient.graphql.inline";
    public string Name => "GraphQL Operations & SDL Templates";

    public IReadOnlyList<string> SupportedLanguages => ["graphql", "gql"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Operations
        { "query ", "GetItems($limit: Int = 10, $offset: Int = 0) {\n  items(limit: $limit, offset: $offset) {\n    id\n    name\n    createdAt\n  }\n}" },
        { "mutation ", "CreateItem($input: CreateItemInput!) {\n  createItem(input: $input) {\n    id\n    success\n    errors {\n      field\n      message\n    }\n  }\n}" },
        { "subscription ", "OnItemCreated {\n  itemCreated {\n    id\n    name\n    timestamp\n  }\n}" },
        { "fragment ", "ItemFields on Item {\n  id\n  title\n  status\n  updatedAt\n}" },

        // SDL Schema Definitions
        { "type ", "User implements Node {\n  id: ID!\n  username: String!\n  email: String!\n  isActive: Boolean!\n  posts(limit: Int): [Post!]!\n}" },
        { "input ", "CreateUserInput {\n  username: String!\n  email: String!\n  bio: String\n}" },
        { "enum ", "UserRole {\n  ADMIN\n  EDITOR\n  VIEWER\n}" },
        { "interface ", "Node {\n  id: ID!\n}" },
        { "union ", "SearchResult = User | Post | Comment" },
        { "schema ", "{\n  query: Query\n  mutation: Mutation\n  subscription: Subscription\n}" },

        // Directives
        { "@include(", "if: $withDetails)" },
        { "@skip(", "if: $omitDetails)" },
        { "@deprecated(", "reason: \"Use alternative field instead.\")" }
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
