using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Ruby.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for the Ruby on Rails framework,
/// including ActiveRecord models, associations, validations, ActionController endpoints, routing, and ERB templates.
/// </summary>
public class RailsCompletionProvider : IInlineCompletionProvider
{
    public string Id => "rails.inline.completion";
    public string Name => "Ruby on Rails & ERB Completions";

    public IReadOnlyList<string> SupportedLanguages => ["ruby", "erb", "html"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // ActiveRecord Models
        {
            "class User < ApplicationRecord",
            "\n  # Associations\n  has_many :posts, dependent: :destroy\n  belongs_to :account\n\n  # Validations\n  validates :email, presence: true, uniqueness: true\nend"
        },
        { "has_many ", ":items, dependent: :destroy" },
        { "belongs_to ", ":user" },
        { "has_one ", ":profile, dependent: :destroy" },
        { "has_and_belongs_to_many ", ":tags" },
        { "validates :", "name, presence: true" },
        { "validate :", "custom_validation_method" },
        { "scope :", "active, -> { where(active: true) }" },
        { "before_save :", "normalize_data" },
        { "before_create :", "generate_token" },

        // ActionController
        {
            "class UsersController < ApplicationController",
            "\n  before_action :set_user, only: %i[show edit update destroy]\n\n  def index\n    @users = User.all\n  end\n\n  def show\n  end\nend"
        },
        { "before_action :", "authenticate_user!" },
        {
            "respond_to do |format|",
            "\n  format.html\n  format.json { render json: @item }\nend"
        },
        { "render json: ", "{ status: :ok, data: @item }" },
        { "params.require(", ":user).permit(:name, :email)" },

        // Routing
        { "resources :", "users" },
        { "root to: \"", "home#index\"" },
        {
            "namespace :",
            "api do\n  namespace :v1 do\n    \n  end\nend"
        },

        // ERB Templates
        { "<% ", "code %>" },
        { "<%= ", "expression %>" },
        { "<%# ", "comment %>" },
        { "<% if ", "condition %>\n  \n<% end %>" },
        { "<% @items.each do |item| %>", "\n  \n<% end %>" }
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
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
