# 💻 RecluseEdit

> *Made with love by indoctrinatedrecluse* ❤️✨

---

## 🌟 Overview & Project Brief

**RecluseEdit** is a lightning-fast, modern desktop code editor crafted specifically for building web applications (HTML, CSS, JavaScript, TypeScript, JSON, XML, and more). Designed with developer agility and productivity in mind, it couples a high-performance text buffer with intelligent inline autocomplete, robust tab management, and an extensible architecture for plug-and-play language engines.

---

## 🎯 Key Objectives & Features

- 🗂️ **Web Workspace Explorer**: Open entire project directories (`Ctrl+Shift+O`), browse files via a collapsible tree sidebar (`Ctrl+B`), and double-click to open.
- 📑 **Multi-File Tabbed Workspace**: Seamlessly open, edit, and switch between multiple tabs. Middle-click tab to close, and right-click for tab context actions (*Close Others*, *Close to Right*, *Copy Path*, *Reveal in Explorer*).
- 🎨 **Exhaustive High-Fidelity Syntax Highlighting**: Custom, dedicated XSHD syntax definitions across all supported languages (Markdown, modern ECMAScript/TypeScript with template literals and control flow, CSS3/SCSS/LESS with CSS variables and pseudo-classes, PHP 8+ with attributes and match expressions, React JSX/TSX, GraphQL, Angular HTML templates, Angular TypeScript, Dart 3, Ruby on Rails, JSON, and XML/XAML), beautifully tuned with a rich VS Code Dark+ color palette.
- ⚡ **Dual Autocomplete System**:
  - **Inline Ghost Text**: Intelligent suggestions inline at the caret in faded italic text (<kbd>Tab</kbd> to accept, <kbd>Esc</kbd> to dismiss).
  - **IntelliSense Popup**: Rich completion list with tags, CSS properties, and JS APIs (<kbd>Ctrl+Space</kbd>).
- 🔄 **Auto-Closing Pairs & HTML Tags**: Automatic closure for `()`, `{}`, `[]`, `""`, `''` and auto-tag closing for HTML (e.g. `<div>` &rarr; `</div>`).
- 🔍 **Built-In Find & Replace Overlay**: Floating top-right search panel with Next (<kbd>Enter</kbd>), Previous (<kbd>Shift+Enter</kbd>), Match Case, and Replace All (<kbd>Ctrl+F</kbd>, <kbd>Ctrl+H</kbd>).
- 📐 **Code Folding & Live Bracket Matching**: Expand/collapse blocks and sections for HTML/XML and live accent border highlighting for matching pairs of `()`, `[]`, and `{}` as the caret moves.
- 🔢 **Visual Line Numbers & Formatting**: Customizable line-number gutter, word wrapping toggle, and font scaling with <kbd>Ctrl</kbd> + <kbd>MouseWheel</kbd>.
- 🔌 **Pluggable Extension Architecture**: Dynamic plugin discovery from the `Extensions/` directory with separate project build targets and a dedicated UI manager (`Extensions -> Manage Extensions...`).
- 🌙 **Modern Dark UI**: VS Code-inspired sleek dark theme (`#1E1E1E`), complete with menu bar, quick-action toolbar, and informative status bar.

---

## ⌨️ Keyboard Shortcuts Reference

| Shortcut | Action |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | New File |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open File |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>O</kbd> | Open Workspace Folder |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save File |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Save As |
| <kbd>Ctrl</kbd> + <kbd>W</kbd> | Close Active Tab |
| <kbd>Ctrl</kbd> + <kbd>B</kbd> | Toggle Workspace Explorer Sidebar |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Find in Document |
| <kbd>Ctrl</kbd> + <kbd>H</kbd> | Find & Replace in Document |
| <kbd>Ctrl</kbd> + <kbd>Space</kbd> | Trigger IntelliSense Completion Popup |
| <kbd>Tab</kbd> | Accept Inline Ghost Text Suggestion |
| <kbd>Esc</kbd> | Dismiss Ghost Text / Close Find Overlay |
| <kbd>Ctrl</kbd> + <kbd>MouseWheel</kbd> | Zoom In / Out Editor Font Size |

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Framework** | .NET 10.0 (`net10.0-windows`) |
| **UI Platform** | Windows Presentation Foundation (WPF) |
| **Language** | C# 13 / 14 |
| **Editor Core Engine** | [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) (v6.3.1) |
| **IDE / Toolchain** | Visual Studio 2026 Enterprise Edition |
| **Source Control** | Git & GitHub CLI (`gh`) |

---

## 🚀 Getting Started: Build & Test Instructions

### 📋 Prerequisites

1. **Visual Studio 2026 Enterprise Edition** (or newer)
   - Workload: **.NET Desktop Development**
   - Individual Components: **.NET 10.0 SDK** & **Windows Desktop App runtime**
2. **Git** and optionally **GitHub CLI (`gh`)**

---

### 🖥️ Building with Visual Studio 2026 Enterprise Edition

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/indoctrinatedrecluse/RecluseEdit.git
   cd RecluseEdit
   ```
2. **Open the Solution**:
   - Double-click `RecluseEdit.slnx` to launch the multi-project solution in **Visual Studio 2026 Enterprise**.
3. **Restore NuGet Packages**:
   - Visual Studio restores NuGet packages automatically upon solution load.
   - Alternatively, right-click the solution in **Solution Explorer** &rarr; select **Restore NuGet Packages**.
4. **Build the Solution**:
   - Select **Build &rarr; Build Solution** from the top menu, or press <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>B</kbd>.
   - All projects (Host, SDK, React, Angular, Flutter, PHP, Ruby, and Tests) build as independent targets.
   - Verify the Output window reports `Build succeeded: 0 Warning(s), 0 Error(s)`.
5. **Run & Test**:
   - Set active configuration to `Debug` or `Release` with target `x64` or `Any CPU`.
   - Run tests via **Test &rarr; Run All Tests** (<kbd>Ctrl</kbd> + <kbd>R</kbd>, <kbd>A</kbd>).
   - Press <kbd>F5</kbd> (Start Debugging) or <kbd>Ctrl</kbd> + <kbd>F5</kbd> (Start Without Debugging) to launch the editor.

---

### ⌨️ Building from the Command Line (`dotnet CLI`)

You can also compile and test directly using the .NET 10 SDK:

```powershell
# Restore all dependencies across solution
dotnet restore RecluseEdit.slnx

# Build the entire solution (host + all extensions)
dotnet build RecluseEdit.slnx

# Run all unit and integration tests (49 tests)
dotnet test RecluseEdit.slnx

# Launch the editor
dotnet run --project RecluseEdit.csproj
```

---

## 🧩 Building Language Extensions

RecluseEdit features a modular, decoupled extension architecture. Each extension is built as an independent project target that compiles into its own assembly:

1. Create a C# class library targeting `net10.0-windows`.
2. Reference the lightweight `RecluseEdit.Sdk` library:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\RecluseEdit.Sdk\RecluseEdit.Sdk.csproj" />
   </ItemGroup>
   ```
3. Implement `IExtension`, `IInlineCompletionProvider`, and optional `IToolchainCheck`:
   ```csharp
   using RecluseEdit.Sdk;
   using RecluseEdit.Sdk.Models;
   using RecluseEdit.Sdk.Providers;

   public class MyLanguageExtension : IExtension
   {
       public string Id => "recluse.mylang";
       public string Name => "My Language Pack";
       public string Version => "1.0.0";
       public string Description => "Adds support for My Language";
       public string Author => "indoctrinatedrecluse";

       public Task InitializeAsync(IExtensionHost host, CancellationToken ct = default)
       {
           // 1. Register Language Definition
           host.RegisterLanguage(new LanguageDefinition
           {
               Id = "mylang",
               DisplayName = "My Language",
               Extensions = [".ml", ".mylang"]
           });

           // 2. Register Autocomplete & Snippets
           host.RegisterInlineCompletion(new MyCompletionProvider());

           // 3. Register Toolchain / Compiler Prerequisites
           host.RegisterToolchainCheck(new MyCompilerCheck());

           return Task.CompletedTask;
       }

       public Task DeinitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
   }
   ```
4. Configure an MSBuild post-build target to output the `.dll` directly to `bin/$(Configuration)/net10.0-windows/Extensions/<Name>/`.

---

## 📦 Official Extensions (Separate Build Targets)

RecluseEdit comes with eight modular language extensions built as dedicated targets in `RecluseEdit.slnx`:

### 🐹 Go Backend & Language Pack (`RecluseEdit.Extensions.Go`)
- Supports **Go** (`.go`), **Go Modules & Workspaces** (`go.mod`, `go.work`, `go.sum`), and **Go Templates** (`.gotmpl`, `.gohtml`).
- **Custom Go 1.23+ XSHD Syntax Highlighting**: Custom grammar for keywords, control flow (`select`, `defer`, `go`), built-in types (`any`, `comparable`, `rune`, `byte`), built-in functions (`make`, `new`, `len`, `append`, `clear`), raw backtick strings (`` `...` ``), runes (`'...'`), operators (`:=`, `<-`), and struct tag highlighting (`json:"..."`, `db:"..."`, `binding:"..."`).
- **Custom Go Module Grammar**: Dedicated XSHD highlighting for `go.mod` and `go.work` directives (`module`, `go`, `toolchain`, `require`, `replace`, `exclude`, `use`), versions (`v1.2.3`), and package paths.
- **Go Core Idioms & Concurrency**: Inline completions for entrypoints (`package main`, `func main()`), error handling (`if err != nil`), goroutines (`go func() { ... }()`), channels (`ch := make(chan string)`), `select` blocks, `sync.WaitGroup`, `sync.RWMutex`, context timeouts (`context.WithTimeout`), and structs/interfaces.
- **Go Backend Web Frameworks & ORM**: Snippets for **Gin** (`r := gin.Default()`, `r.GET`, `r.POST`, `c.ShouldBindJSON`, `c.JSON`), **Fiber** (`app := fiber.New()`, `c.BodyParser`), **Chi Router** (`r := chi.NewRouter()`, `r.Use`), **Echo** (`e := echo.New()`), **standard net/http** (`http.HandleFunc`, `http.ListenAndServe`, `json.NewDecoder`, `json.NewEncoder`), **GORM** (`gorm.Open`, `db.AutoMigrate`, `db.Where`, `db.Create`), and `database/sql` (`db.QueryContext`, `db.BeginTx`).
- **Toolchain Diagnostics**: Active diagnostics for the **Go compiler** (`go version`) detecting version and target platform, and **golangci-lint** (`golangci-lint --version`) with installation guidance.

### 🔴 Laravel Framework & Blade Pack (`RecluseEdit.Extensions.Laravel`)
- Supports **Laravel Blade Templates** (`.blade.php`) and Artisan scripts (`artisan`).
- **Custom XSHD Blade Syntax Highlighting**: Highlighting for Blade directives (`@extends`, `@section`, `@yield`, `@if`, `@foreach`, `@forelse`, `@auth`, `@csrf`, `@method`, `@error`, `@livewire`, `@vite`), escaped and unescaped expressions (`{{ $var }}`, `{!! $html !!}`), Blade comments (`{{-- ... --}}`), and embedded HTML.
- **Eloquent ORM & Migrations**: Snippets for model properties (`$fillable`, `$casts`), relationships (`hasMany`, `belongsTo`, `hasOne`, `belongsToMany`, `morphMany`), schema builders (`Schema::create`, `foreignId`), and query scopes.
- **Routing, Controllers & Middleware**: RESTful routing (`Route::get`, `Route::post`, `Route::resource`, `Route::apiResource`), route groups, controller action signatures, Request validation, and response helpers (`view()`, `response()->json()`).
- **Toolchain Diagnostics**: Active diagnostics for the **Laravel Installer CLI** (`laravel -V`) and **Artisan CLI** (`php artisan --version`).

### 🐍 Python & Full-Stack Web Pack (`RecluseEdit.Extensions.Python`)
- Supports **Python** (`.py`, `.pyw`, `.pyi`, `.pyd`) and **Jinja2 / Django Templates** (`.jinja`, `.jinja2`, `.j2`, `.html.jinja`, `.djhtml`).
- **Modern Python 3.12+ Syntax Highlighting**: Custom XSHD definition for decorators (`@app.route`, `@property`), f-strings with `{expr}` interpolation, type hints, dunder methods (`__init__`, `__repr__`), and pattern matching (`match`/`case`).
- **Jinja2 & Django Templates**: Highlighting and snippets for statements (`{% for %}`, `{% if %}`, `{% block %}`, `{% csrf_token %}`), expressions (`{{ ... }}`), filters (`|upper`, `|safe`), and embedded HTML.
- **Python Frontends & UI**: Rich snippets and idioms for **Streamlit** (`st.title`, `st.button`, `st.sidebar`, `st.dataframe`), **Gradio** (`gr.Interface`, `gr.Blocks`, `gr.Row`), and **Reflex** (`rx.State`, `rx.vstack`, `rx.button`).
- **Python Web Backends**: Completions for **Flask** (`@app.route`, `render_template`, `jsonify`), **Django** (`models.Model`, `urlpatterns`, `views.View`, `JsonResponse`), and **FastAPI** (`app = FastAPI()`, `BaseModel`).
- **Express.js Support**: Route and middleware snippets (`app.get`, `app.post`, `app.use`, `express.json()`, `express.Router()`).
- **Toolchain Diagnostics**: Active diagnostics and path checks for **Python interpreter** (`python -V`), **Pip** (`pip -V`), and **Django CLI** (`django-admin --version`).

### ⚛️ React, Redux & GraphQL Pack (`RecluseEdit.Extensions.React`)
- Supports **React JSX** (`.jsx`), **React TSX** (`.tsx`), and **GraphQL** (`.graphql`, `.gql`).
- **React Hooks**: `useState`, `useEffect`, `useCallback`, `useMemo`, `useRef`, `useContext`, `useReducer`, `useId`.
- **Redux Toolkit**: `createSlice`, `createAsyncThunk`, `configureStore`, `useSelector`, `useDispatch`.
- **GraphQL**: `query`, `mutation`, `subscription`, `fragment`, `schema`, `type`, and Apollo Client hooks.
- **Toolchain Diagnostics**: Actively verifies Node.js (`node`), npm (`npm`), and TypeScript compiler (`tsc`) on system `PATH`.

### 🅰️ Angular Language & Framework Pack (`RecluseEdit.Extensions.Angular`)
- Supports **Angular Templates** (`.component.html`) and **Angular TypeScript** (`.component.ts`, `.service.ts`, `.directive.ts`, etc.).
- **Angular Signals**: `signal()`, `computed()`, `effect()`, `input()`, `output()`, `model()`.
- **Modern Control Flow**: `@if (...)`, `@for (... track ...)`, `@switch (...)`, and `@defer (...)`.
- **Standalone Decorators**: `@Component`, `@Injectable`, `@Directive`, `@Pipe`, and modern `inject()` DI.
- **Toolchain Diagnostics**: Verifies Angular CLI (`ng`) with actionable global install instructions.

### 🐦 Flutter & Dart Language Pack (`RecluseEdit.Extensions.Flutter`)
- Supports **Dart** (`.dart`) with dedicated Dart 3 XSHD syntax highlighting and Dark+ colorization.
- **Flutter Widget Snippets**: `stless` (StatelessWidget), `stful` (StatefulWidget), `setState()`, and lifecycle methods.
- **Common Layouts**: `Scaffold`, `Column`, `Row`, `Container`, `ListView.builder`, `ElevatedButton`, `Navigator`.
- **Toolchain Diagnostics**: Actively detects both the **Flutter SDK** (`flutter`) and **Dart SDK** (`dart`).

### 🐘 PHP Language Pack (`RecluseEdit.Extensions.Php`)
- Supports **PHP** (`.php`, `.phtml`, etc.) with modern PHP 8+ features.
- **Modern PHP Constructs**: `match (...)`, `enum`, constructor property promotion, typed properties, arrow functions (`fn()`).
- **Class & Error Handling**: `try ... catch (Throwable)`, `declare(strict_types=1);`, and JSON helpers.
- **Toolchain Diagnostics**: Verifies **PHP CLI** (`php`) and **Composer** (`composer`) package manager.

### 💎 Ruby & Ruby on Rails Language Pack (`RecluseEdit.Extensions.Ruby`)
- Supports **Ruby** (`.rb`, `.rake`, `.gemspec`, `.ru`, `Gemfile`, `Rakefile`) and **ERB Templates** (`.erb`, `.html.erb`).
- **Custom Syntax Highlighting**: Dedicated XSHD grammar for Ruby keywords, symbols (`:symbol`), instance/class variables (`@var`, `@@var`), and string interpolation (`#{...}`).
- **Ruby Idioms & Blocks**: `def`, `class`, `module`, `attr_accessor`, `each do |item|`, `map do |item|`, `begin ... rescue StandardError`.
- **Rails & ActiveRecord Snippets**: `class User < ApplicationRecord`, `has_many`, `belongs_to`, `validates`, `before_action`, `resources`, `render json:`, and ERB tags (`<%= ... %>`, `<% ... %>`).
- **Toolchain Diagnostics**: Actively verifies **Ruby runtime** (`ruby`), **Bundler** (`bundle`), and **Rails CLI** (`rails`).

---

## 🤖 Automated CI/CD & GitHub Releases

RecluseEdit includes a fully automated GitHub Actions workflow (`.github/workflows/release.yml`) configured for continuous delivery:

- 🏷️ **Triggered on Tag Push**: Pushing a new version tag (e.g. `git tag v1.0.0 && git push origin v1.0.0`) triggers an automated build pipeline on `windows-latest`.
- 🧪 **Full Verification**: Executes the complete test suite (`dotnet test RecluseEdit.slnx -c Release`) across all projects before packaging.
- 📦 **Bundle & Package**:
  - Compiles the host editor and all extensions in `Release` configuration.
  - Bundles the main application executable, dependencies, and all eight language extensions (`React`, `Angular`, `Flutter`, `Php`, `Ruby`, `Python`, `Laravel`, `Go`) under `Extensions/`.
  - Packages the entire distribution into a portable archive: `RecluseEdit-windows-<tag>.zip`.
- 🚀 **GitHub Release**: Automatically creates a new GitHub Release with the bundled `.zip` asset attached and generates release notes.
- 🕹️ **Manual Trigger**: Can also be executed manually via the **Actions** tab with custom version tags (`workflow_dispatch`).

---

## 🛠️ Troubleshooting & Known Issues

### 🔧 Windows / VS Code Git Error: `index file smaller than expected`

#### Problem
When developing on Windows alongside IDEs or background file watchers (such as VS Code, Visual Studio, or language server analyzers), running Git commands or pushes may occasionally fail with:
```text
fatal: .git/index: index file smaller than expected
```

#### Cause
On Windows, NTFS enforces strict file-sharing locks. When background file watchers or IDE status polls query `.git/index` at the exact millisecond Git attempts to atomically replace it via `.git/index.lock`, the atomic rename can be interrupted mid-transaction, leaving the `.git/index` staging cache truncated to `0 bytes`. The underlying Git repository database, commit history, and branches remain completely safe and uncorrupted.

#### Quick 1-Second Resolution
To safely regenerate the staging index directly from `HEAD` without losing any uncommitted working tree changes, run this command in **PowerShell**:

```powershell
Remove-Item .git\index -Force; git reset
```

*(Or in Bash / POSIX shells: `rm -f .git/index && git reset`)*

---

## 📜 License

Copyright © 2026 **indoctrinatedrecluse**. All rights reserved.

This project is licensed under the **RecluseEdit Software License**.
- ✅ **Free to Use**: You may freely download, install, and run this editor, along with all included official extensions, for personal, educational, and commercial projects.
- ✉️ **Modification & Redistribution Restriction**: Modifying, creating derivative works, or redistributing the software or any of its official extensions/SDK modules (in source or binary form) requires prior explicit written permission. Please contact [indoctrinatedrecluse](https://github.com/indoctrinatedrecluse/RecluseEdit) for permission requests.

See the [LICENSE](LICENSE) file for complete legal terms. All official extensions and SDK components are covered under these same clauses.

