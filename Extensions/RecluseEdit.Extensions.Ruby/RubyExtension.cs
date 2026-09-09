using RecluseEdit.Extensions.Ruby.Providers;
using RecluseEdit.Extensions.Ruby.Syntaxes;
using RecluseEdit.Extensions.Ruby.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Ruby;

/// <summary>
/// RecluseEdit extension providing language support, syntax highlighting, snippets, and compiler checks
/// for modern Ruby and the Ruby on Rails framework.
/// </summary>
public class RubyExtension : IExtension
{
    public string Id => "recluse.ruby";
    public string Name => "Ruby & Ruby on Rails Language Pack";
    public string Version => "1.0.0";
    public string Description => "Modern Ruby and Ruby on Rails development support with custom XSHD syntax highlighting, ActiveRecord/ActionController completions, ERB templates, and Ruby/Bundler/Rails toolchain checks.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "ruby",
            DisplayName = "Ruby",
            Extensions = [".rb", ".rake", ".gemspec", ".ru", "Gemfile", "Rakefile"],
            HighlightingName = "Ruby"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "erb",
            DisplayName = "Ruby ERB Template",
            Extensions = [".erb", ".html.erb"],
            HighlightingName = "HTML"
        });

        // 2. Register Custom Syntax Highlighting for Ruby
        try
        {
            var rubyDef = RubySyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("ruby", rubyDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Ruby syntax definition: {ex.Message}");
        }

        // 3. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new RubyCompletionProvider());
        host.RegisterInlineCompletion(new RailsCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new RubyToolchainCheck());
        host.RegisterToolchainCheck(new BundlerToolchainCheck());
        host.RegisterToolchainCheck(new RailsToolchainCheck());

        host.Log("Ruby & Ruby on Rails Language Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
