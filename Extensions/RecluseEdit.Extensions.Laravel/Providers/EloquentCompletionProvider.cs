using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Laravel.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for Laravel Eloquent models,
/// relationships (HasMany, BelongsTo, BelongsToMany), query scopes, migrations, and database factories.
/// </summary>
public class EloquentCompletionProvider : IInlineCompletionProvider
{
    public string Id => "laravel.eloquent.completion";
    public string Name => "Laravel Eloquent & Database Completions";

    public IReadOnlyList<string> SupportedLanguages => ["php", "blade"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Model Properties & Casts
        {
            "protected $fillable = ",
            "[\n        'name',\n        'email',\n    ];"
        },
        {
            "protected function casts(): array",
            "\n    {\n        return [\n            'email_verified_at' => 'datetime',\n            'is_active' => 'boolean',\n        ];\n    }"
        },
        {
            "protected $hidden = ",
            "[\n        'password',\n        'remember_token',\n    ];"
        },

        // Eloquent Relationships
        {
            "public function user(): BelongsTo",
            "\n    {\n        return $this->belongsTo(User::class);\n    }"
        },
        {
            "public function items(): HasMany",
            "\n    {\n        return $this->hasMany(Item::class);\n    }"
        },
        {
            "public function profile(): HasOne",
            "\n    {\n        return $this->hasOne(Profile::class);\n    }"
        },
        {
            "public function roles(): BelongsToMany",
            "\n    {\n        return $this->belongsToMany(Role::class);\n    }"
        },
        {
            "public function comments(): MorphMany",
            "\n    {\n        return $this->morphMany(Comment::class, 'commentable');\n    }"
        },

        // Query Scopes
        {
            "public function scopeActive(",
            "Builder $query): void\n    {\n        $query->where('is_active', true);\n    }"
        },

        // Migration Schema Builder
        {
            "Schema::create(",
            "'table_name', function (Blueprint $table) {\n            $table->id();\n            $table->string('name');\n            $table->timestamps();\n        });"
        },
        {
            "Schema::table(",
            "'table_name', function (Blueprint $table) {\n            $table->string('column_name')->nullable();\n        });"
        },
        {
            "$table->foreignId(",
            "'user_id')->constrained()->cascadeOnDelete();"
        },
        {
            "$table->string(",
            "'name', 255);"
        },
        {
            "$table->enum(",
            "'status', ['pending', 'active', 'archived'])->default('pending');"
        },
        {
            "$table->softDeletes(",
            ");"
        },

        // Factories & Seeders
        {
            "return [\n            'name' => ",
            "fake()->name(),\n            'email' => fake()->unique()->safeEmail(),\n            'password' => static::$password ??= Hash::make('password'),\n        ];"
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
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
