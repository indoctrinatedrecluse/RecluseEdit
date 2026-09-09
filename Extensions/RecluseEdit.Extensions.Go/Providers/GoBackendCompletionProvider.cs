using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Go.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for Go backend web frameworks
/// including Gin, Fiber, Chi, Echo, standard net/http, GORM, and database/sql.
/// </summary>
public class GoBackendCompletionProvider : IInlineCompletionProvider
{
    public string Id => "go.backend.completion";
    public string Name => "Go Backend & Web Frameworks (Gin, Fiber, Chi, GORM, net/http)";

    public IReadOnlyList<string> SupportedLanguages => ["go"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Standard Library net/http
        {
            "http.HandleF",
            "unc(\"/api/v1/health\", func(w http.ResponseWriter, r *http.Request) {\n\tw.Header().Set(\"Content-Type\", \"application/json\")\n\tw.WriteHeader(http.StatusOK)\n\tjson.NewEncoder(w).Encode(map[string]string{\"status\": \"healthy\"})\n})"
        },
        {
            "http.Listen",
            "AndServe(\":8080\", mux)"
        },
        {
            "json.NewDec",
            "oder(r.Body).Decode(&payload)"
        },
        {
            "json.NewEnc",
            "oder(w).Encode(response)"
        },

        // Gin Web Framework (github.com/gin-gonic/gin)
        {
            "r := gin.D",
            "efault()\n\nr.GET(\"/ping\", func(c *gin.Context) {\n\tc.JSON(http.StatusOK, gin.H{\"message\": \"pong\"})\n})\n\nr.Run(\":8080\")"
        },
        {
            "r.GET(",
            "\"/api/v1/items\", func(c *gin.Context) {\n\tc.JSON(http.StatusOK, gin.H{\"data\": items})\n})"
        },
        {
            "r.POST(",
            "\"/api/v1/items\", func(c *gin.Context) {\n\tvar req CreateItemRequest\n\tif err := c.ShouldBindJSON(&req); err != nil {\n\t\tc.JSON(http.StatusBadRequest, gin.H{\"error\": err.Error()})\n\t\treturn\n\t}\n\tc.JSON(http.StatusCreated, gin.H{\"status\": \"created\"})\n})"
        },
        {
            "r.Group(",
            "\"/api/v1\")\n{\n\tv1.GET(\"/users\", listUsersHandler)\n\tv1.POST(\"/users\", createUserHandler)\n}"
        },
        {
            "c.ShouldB",
            "indJSON(&req)"
        },
        {
            "c.JSON(h",
            "ttp.StatusOK, gin.H{\"data\": payload})"
        },

        // Fiber Web Framework (github.com/gofiber/fiber/v2)
        {
            "app := fiber.N",
            "ew()\n\napp.Get(\"/\", func(c *fiber.Ctx) error {\n\treturn c.SendString(\"Hello, Fiber!\")\n})\n\napp.Listen(\":3000\")"
        },
        {
            "app.Get(",
            "\"/api/v1/users\", func(c *fiber.Ctx) error {\n\treturn c.JSON(fiber.Map{\"users\": users})\n})"
        },
        {
            "app.Post(",
            "\"/api/v1/users\", func(c *fiber.Ctx) error {\n\tvar req CreateUserDTO\n\tif err := c.BodyParser(&req); err != nil {\n\t\treturn c.Status(fiber.StatusBadRequest).JSON(fiber.Map{\"error\": err.Error()})\n\t}\n\treturn c.Status(fiber.StatusCreated).JSON(req)\n})"
        },
        {
            "c.BodyP",
            "arser(&payload)"
        },

        // Chi Router (github.com/go-chi/chi/v5)
        {
            "r := chi.N",
            "ewRouter()\nr.Use(middleware.Logger)\nr.Use(middleware.Recoverer)\n\nr.Get(\"/health\", func(w http.ResponseWriter, r *http.Request) {\n\tw.Write([]byte(\"OK\"))\n})"
        },
        {
            "r.Route(",
            "\"/api/v1\", func(r chi.Router) {\n\tr.Get(\"/todos\", listTodos)\n\tr.Post(\"/todos\", createTodo)\n})"
        },

        // Echo Web Framework (github.com/labstack/echo/v4)
        {
            "e := echo.N",
            "ew()\ne.GET(\"/\", func(c echo.Context) error {\n\treturn c.String(http.StatusOK, \"Hello from Echo!\")\n})\ne.Logger.Fatal(e.Start(\":1323\"))"
        },

        // GORM (gorm.io/gorm)
        {
            "db, err := gorm.O",
            "pen(postgres.Open(dsn), &gorm.Config{})\nif err != nil {\n\tlog.Fatalf(\"failed to connect database: %v\", err)\n}"
        },
        {
            "db.AutoM",
            "igrate(&User{}, &Product{}, &Order{})"
        },
        {
            "db.Where(",
            "\"email = ?\", email).First(&user)"
        },
        {
            "db.Create(",
            "&newItem)"
        },
        {
            "db.Model(",
            "&user).Updates(map[string]any{\"name\": newName, \"active\": true})"
        },

        // database/sql & sqlx
        {
            "db, err := sql.O",
            "pen(\"postgres\", connString)\nif err != nil {\n\tlog.Fatalf(\"failed to open sql db: %v\", err)\n}\ndefer db.Close()"
        },
        {
            "rows, err := db.Q",
            "ueryContext(ctx, \"SELECT id, name, email FROM users WHERE active = $1\", true)\nif err != nil {\n\treturn nil, err\n}\ndefer rows.Close()"
        },
        {
            "tx, err := db.B",
            "eginTx(ctx, nil)\nif err != nil {\n\treturn err\n}\ndefer tx.Rollback()\n// Perform queries\nreturn tx.Commit()"
        }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText.TrimStart();

        foreach (var (prefix, suggestion) in Completions)
        {
            if (line.EndsWith(prefix, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
