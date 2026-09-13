# 💻 RecluseEdit

> *Made with love by indoctrinatedrecluse* ❤️✨

---

## 🌟 Overview & Project Brief

**RecluseEdit** is a lightning-fast, modern desktop code editor crafted specifically for building web applications (HTML, CSS, JavaScript, TypeScript, JSON, XML, and more). Designed with developer agility and productivity in mind, it couples a high-performance text buffer with intelligent inline autocomplete, robust tab management, and an extensible architecture for plug-and-play language engines.

---

## 🎯 Key Objectives & Features

- 🎨 **Extensible Theme System & Public Theme API**: First-class open Theme API in `RecluseEdit.Sdk` (`IThemeDefinition`, `ThemeDefinitionBase`, `ThemeColors`, `ThemeType`) allowing anyone to author and register custom themes. Includes 9 creative built-in themes (Recluse Dark+, Recluse Light+, Cyberpunk 2077, Monokai Pro, Solarized Dark, Dracula, Nord, Retro Matrix, High Contrast Black), a dedicated **Themes** menu bar item, an interactive Theme Picker dialog (<kbd>Ctrl+K, Ctrl+T</kbd> / <kbd>Ctrl+Alt+T</kbd>) with real-time live preview and safe rollback, and dynamic AvalonEdit and Live Preview Markdown styling synchronization.
- 🌐 **Chromium-Powered Live Web & Markdown Preview**: Embedded split-pane live preview backed by `Microsoft.Web.WebView2` with real-time debounced updates (<kbd>Ctrl+Shift+V</kbd>), `<base href="...">` relative asset resolution for local CSS/JS/images, multi-device viewport switching (Responsive 100%, Mobile 375px, Tablet 768px, Desktop 1200px), and GitHub Dark-themed Markdown rendering.
- 🎨 **Document Formatting & Syntax Diagnostics**: Extensible `IDocumentFormatter` pipeline with built-in pure C# formatters for JSON, CSS/SCSS, HTML/XML, SQL, JS/TS, and Markdown (<kbd>Shift+Alt+F</kbd>), configurable "Format on Save", and real-time syntax diagnostics (JSON parser errors, bracket/brace mismatch tracking, HTML tag auditing).
- 🚀 **Project Scaffolding Wizard**: Interactive project creation wizard (<kbd>Ctrl+Shift+N</kbd>) featuring 7 production starters (Static Web, Vite+React 19, Vite+Vue 3, Vite+Svelte 5, Vite+SolidJS, Fastify Microservice, Markdown Docs) with one-click Git repo initialization and instant workspace opening.
- 💻 **Integrated Web Developer Console & Problems Dock**: Unified bottom dock with **Terminal** (<kbd>Ctrl+`</kbd>), **Web Console**, and **Problems**. JavaScript console bridge capturing `console.log/info/warn/error` and unhandled exceptions directly from the preview pane into a filterable viewer, plus double-click jump-to-error in code.
- 🌿 **Git Diff Gutter Indicators & Source Control Panel**: Interactive editor gutter margin displaying live line additions (green), modifications (blue), and deletions (red triangle) computed directly against Git `HEAD`. Dedicated Source Control panel (<kbd>Ctrl+Shift+G</kbd>) with Activity Bar navigation, branch & ahead/behind counters, commit message composer (<kbd>Ctrl+Enter</kbd>), one-click staging/unstaging, and discard changes.
- ⚡ **Universal Command Palette**: Modal fuzzy command launcher (<kbd>Ctrl+Shift+P</kbd>, <kbd>F1</kbd>) to search and execute any command, quick-open workspace files and tabs (<kbd>Ctrl+P</kbd>), jump to line numbers (<kbd>Ctrl+G</kbd>), and query help commands (`?`).
- ✂️ **Advanced Line & Multiline Editing**: Move lines up/down (<kbd>Alt+&uarr;/&darr;</kbd>), duplicate lines (<kbd>Shift+Alt+&uarr;/&darr;</kbd>), toggle comments (<kbd>Ctrl+/</kbd>), delete lines (<kbd>Ctrl+Shift+K</kbd>), join lines (<kbd>Ctrl+J</kbd>), transform case (<kbd>Ctrl+Shift+U</kbd>, <kbd>Ctrl+U</kbd>), sort lines, trim trailing whitespace, and rectangular column cursor editing (<kbd>Ctrl+Alt+&uarr;/&darr;</kbd>).
- 🗂️ **Web Workspace Explorer**: Open entire project directories (`Ctrl+Shift+O`), browse files via a collapsible tree sidebar (`Ctrl+B`), and double-click to open.
- 🤖 **DeepSeek AI Chat Assistant**: Collapsible right-pane conversational AI panel (<kbd>Ctrl+Alt+A</kbd>) with real-time streaming, configurable API endpoints & keys, secure file read/write permissions, and interactive confirmation prompts before running terminal commands.
- 💻 **Integrated Multi-Terminal Dock**: Persistent multi-tab terminal (<kbd>Ctrl+`</kbd>) supporting PowerShell, CMD, Git Bash, WSL, and custom verified executables.
- 📑 **Multi-File Tabbed Workspace**: Seamlessly open, edit, and switch between multiple tabs. Middle-click tab to close, and right-click for tab context actions (*Close Others*, *Close to Right*, *Copy Path*, *Reveal in Explorer*).
- 🎨 **Exhaustive High-Fidelity Syntax Highlighting**: Custom, dedicated XSHD syntax definitions across all supported languages (Vue 3 SFC, Svelte 5 runes, Astro, SQL, HTTP/REST, Markdown, modern ECMAScript/TypeScript, CSS3/SCSS/LESS, PHP 8+, React JSX/TSX, GraphQL, Angular HTML templates, Angular TypeScript, Dart 3, Ruby on Rails, Go, Python, Rust, Lua, PowerShell, Bash, JSON, and XML/XAML), beautifully tuned with a rich VS Code Dark+ color palette.
- ⚡ **Dual Autocomplete System**:
  - **Inline Ghost Text**: Intelligent suggestions inline at the caret in faded italic text (<kbd>Tab</kbd> to accept, <kbd>Esc</kbd> to dismiss).
  - **IntelliSense Popup**: Rich completion list with tags, CSS properties, and JS APIs (<kbd>Ctrl+Space</kbd>).
- 🔄 **Auto-Closing Pairs & HTML Tags**: Automatic closure for `()`, `{}`, `[]`, `""`, `''` and auto-tag closing for HTML (e.g. `<div>` &rarr; `</div>`).
- 🔍 **Built-In Find & Replace Overlay**: Floating top-right search panel with Next (<kbd>Enter</kbd>), Previous (<kbd>Shift+Enter</kbd>), Match Case, and Replace All (<kbd>Ctrl+F</kbd>, <kbd>Ctrl+H</kbd>).
- 📐 **Code Folding & Live Bracket Matching**: Expand/collapse blocks and sections for HTML/XML and live accent border highlighting for matching pairs of `()`, `[]`, and `{}` as the caret moves.
- 🔢 **Visual Line Numbers & Formatting**: Customizable line-number gutter, word wrapping toggle, and font scaling with <kbd>Ctrl</kbd> + <kbd>MouseWheel</kbd>.
- 🔌 **Pluggable Extension Architecture**: Dynamic plugin discovery from the `Extensions/` directory with separate project build targets, multi-side-panel UI dock integration, and a dedicated UI manager (`Extensions -> Manage Extensions...`).
- 🌙 **Modern Dark UI**: VS Code-inspired sleek dark theme (`#1E1E1E`), complete with Activity Bar sidebar, menu bar, quick-action toolbar, and informative status bar.

---

## ⌨️ Keyboard Shortcuts Reference

| Shortcut | Action |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>K</kbd>, <kbd>Ctrl</kbd> + <kbd>T</kbd> | Color Theme Picker (Live Preview & Rollback) |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> | Quick Color Theme Selector |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>V</kbd> | Toggle Chromium Live Web & Markdown Preview |
| <kbd>Shift</kbd> + <kbd>Alt</kbd> + <kbd>F</kbd> | Format Document (Prettify Code) |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>N</kbd> | New Project... (Scaffolding Wizard) |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd> / <kbd>F1</kbd> | Universal Command Palette (Commands Mode `>`) |
| <kbd>Ctrl</kbd> + <kbd>P</kbd> | Quick Open (Search Files & Open Tabs) |
| <kbd>Ctrl</kbd> + <kbd>G</kbd> | Go to Line & Column (`:line[:col]`) |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>E</kbd> | Focus Explorer Activity Bar |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>G</kbd> | Focus Source Control Activity Bar |
| <kbd>Ctrl</kbd> + <kbd>K</kbd>, <kbd>Ctrl</kbd> + <kbd>S</kbd> | Keyboard Shortcuts Reference Window |
| <kbd>Ctrl</kbd> + <kbd>/</kbd> | Toggle Line Comment (`//`, `#`, `--`, etc.) |
| <kbd>Alt</kbd> + <kbd>&uarr;</kbd> / <kbd>&darr;</kbd> | Move Line / Selection Up / Down |
| <kbd>Shift</kbd> + <kbd>Alt</kbd> + <kbd>&uarr;</kbd> / <kbd>&darr;</kbd> | Duplicate Line / Selection Up / Down |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>K</kbd> | Delete Current Line(s) |
| <kbd>Ctrl</kbd> + <kbd>J</kbd> | Join Lines |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>U</kbd> | Transform Selection to Uppercase |
| <kbd>Ctrl</kbd> + <kbd>U</kbd> | Transform Selection to Lowercase |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>&uarr;</kbd> / <kbd>&darr;</kbd> | Add Column / Box Selection Cursor |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | New File |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open File |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>O</kbd> | Open Workspace Folder |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save File |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Save As |
| <kbd>Ctrl</kbd> + <kbd>W</kbd> | Close Active Tab |
| <kbd>Ctrl</kbd> + <kbd>`</kbd> | Toggle Terminal / Bottom Dock Panel |
| <kbd>Ctrl</kbd> + <kbd>B</kbd> | Toggle Sidebar (Explorer / Source Control) |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>A</kbd> | Toggle DeepSeek AI Chat Right Pane |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Find in Document |
| <kbd>Ctrl</kbd> + <kbd>H</kbd> | Find & Replace in Document |
| <kbd>Ctrl</kbd> + <kbd>Space</kbd> | Trigger IntelliSense Completion Popup |
| <kbd>Tab</kbd> | Accept Inline Ghost Text Suggestion |
| <kbd>Esc</kbd> | Dismiss Ghost Text / Close Overlay / Command Palette |
| <kbd>Ctrl</kbd> + <kbd>MouseWheel</kbd> | Zoom In / Out Editor Font Size |

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Framework** | .NET 10.0 (`net10.0-windows`) |
| **UI Platform** | Windows Presentation Foundation (WPF) |
| **Language** | C# 13 / 14 |
| **Editor Core Engine** | [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) (v6.3.1) |
| **Web Preview Engine** | [Microsoft.Web.WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/) (Chromium) |
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

# Run all unit and integration tests (119 tests)
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

RecluseEdit comes with fourteen modular extensions built as dedicated targets in `RecluseEdit.slnx`:

### 🤖 DeepSeek AI Chat Assistant Pack (`RecluseEdit.Extensions.DeepSeek`)
- **Right Pane AI Interface**: Integrated collapsible right-side dock (<kbd>Ctrl+Alt+A</kbd>) styled seamlessly in dark theme, with live conversation history, auto-scroll, message bubbles, and status updates.
- **Configurable Endpoint & Encrypted API Secret Storage**: User-configurable API Endpoint URL (defaulting to `https://api.deepseek.com/chat/completions` or custom local OpenAI/Ollama-compatible services) and API secret. Secrets are stored strictly in encrypted formats on disk (`%APPDATA%\RecluseEdit\deepseek_settings.json`) using the Windows Data Protection API (DPAPI, `CurrentUser` scope) with an AES fallback; plaintext credentials are never persisted to disk.
- **Live Token Streaming**: Server-Sent Events (SSE) streaming engine delivering instantaneous token-by-token responses.
- **Autonomous Tool Calling**:
  - `read_file`: Reads full text contents of workspace files to analyze code context.
  - `write_file`: Directly creates or modifies files in the project workspace with automatic tab buffer synchronization.
  - `list_files`: Traverses and enumerates directory trees within the workspace.
  - `execute_command`: Spawns terminal commands with workspace working directories.
- **Interactive Security Confirmation Dialog**: DeepSeek will never execute shell commands silently; users are explicitly prompted with interactive confirmation dialogs to approve or deny terminal execution.
- **Active Document Context**: One-click checkbox to attach the active editor tab's file path and code content to the prompt.
- **Extensible Side Panel SDK**: Leverages `ISidePanelProvider` and `IWorkspaceContext` in `RecluseEdit.Sdk`.

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
 
### 📜 Scripting & Systems Language Pack (`RecluseEdit.Extensions.Scripting`)
- Supports **Rust** (`.rs`), **Lua** (`.lua`), **PowerShell** (`.ps1`, `.psm1`, `.psd1`), and **Bash / POSIX Shell** (`.sh`, `.bash`, `.zsh`, `.ksh`, `.command`).
- **Custom XSHD Syntax Highlighting**:
  - **Rust**: Highlighting for lifetimes (`'a`), macros (`println!`, `vec!`), attributes (`#[derive(...)]`), raw strings (`r#"..."#`), byte strings, standard types, and doc comments (`///`, `//!`).
  - **Lua**: Highlighting for multiline block comments (`--[[ ... ]]`), multiline literal strings (`[[ ... ]]`), standard library tables (`string`, `table`, `math`, `io`, etc.), and operators (`..`, `~=`, `//`, `#`).
  - **PowerShell**: Highlighting for Verb-Noun cmdlets (`Get-Process`, `Invoke-WebRequest`), parameters (`-Path`, `-Force`), type accelerators (`[string]`, `[hashtable]`), and environment variables (`$env:PATH`, `$_`).
  - **Bash**: Highlighting for shebangs (`#!/usr/bin/env bash`), parameter expansion (`${VAR:-default}`, `$@`), shell builtins, Unix utilities (`grep`, `awk`, `sed`, `curl`), and test brackets (`[[ ]]`).
- **Contextual Autocomplete & Snippet Providers**:
  - `RustCompletionProvider`: Functions, pattern matching (`match`, `if let`), tests, structs, enums, derive macros, and standard collections.
  - `LuaCompletionProvider`: Local and member functions, `for pairs`/`ipairs` loops, and `pcall` safe invocation.
  - `PowerShellCompletionProvider`: Advanced cmdlets with `[CmdletBinding()]` and `param()` blocks, `try/catch`, and loop templates.
  - `BashCompletionProvider`: Strict mode templates (`set -euo pipefail`), file testing conditions, and traps.
- **Toolchain Diagnostics**: Actively detects compiler and shell runtimes: `rustc`, `lua`/`luajit`, `pwsh`/`powershell`, and `bash`.

### 🗄️ Database & SQL Explorer Pack (`RecluseEdit.Extensions.Database`)
- Supports **SQL** (`.sql`) across ANSI SQL, SQLite, PostgreSQL, MySQL, and T-SQL dialects.
- **Custom SQL XSHD Syntax Highlighting**: Dark+ colorization for keywords (`SELECT`, `INSERT`, `UPDATE`, `DELETE`, `JOIN`, `GROUP BY`, `TRANSACTION`), functions (`COUNT`, `COALESCE`, `NOW`), data types (`VARCHAR`, `INTEGER`, `TIMESTAMP`, `JSON`), comments (`--`, `/* ... */`), and dialect-specific identifiers (`` `backtick` ``, `[bracket]`, `"quoted"`).
- **Interactive Database Side Panel**: Dedicated tab in the right dock with SQLite database file picker, live schema inspector showing tables and column types, interactive multi-line query editor with <kbd>Ctrl+Enter</kbd> execution, responsive DataGrid for tabular query results, and one-click **CSV Export**.
- **SQL Inline Completion**: Snippets and statements for `SELECT`, `INSERT INTO`, `UPDATE`, `DELETE FROM`, `CREATE TABLE IF NOT EXISTS`, `ALTER TABLE`, `INNER JOIN`, `LEFT JOIN`, `CREATE INDEX`, and transaction blocks.
- **Toolchain Diagnostics**: Verifies `sqlite3`, PostgreSQL (`psql`), and MySQL (`mysql`) CLI utilities on system `PATH`.

### ⚡ REST Client & API Workbench Pack (`RecluseEdit.Extensions.RestClient`)
- Supports **HTTP / REST** request files (`.http`, `.rest`).
- **Custom HTTP Syntax Highlighting**: Dark+ highlighting for HTTP methods (`GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD`, `OPTIONS`), request boundaries (`###`), request headers (`Content-Type`, `Authorization`), variables (`{{...}}`), URLs, and JSON body payloads.
- **Integrated API Workbench Dock**: Dedicated right dock panel with HTTP method dropdown, request URL bar, <kbd>Ctrl+Enter</kbd> execution, real-time HTTP client engine with timing / latency metrics (ms) and content-length counters, status code pill indicators (green 2xx, yellow 3xx, orange 4xx, red 5xx), request headers & body editor tabs, and syntax-formatted JSON response viewer with one-click clipboard copying.
- **HTTP Inline Completion**: Snippets for request headers (`Content-Type: application/json`, `Authorization: Bearer`), methods, and request templates.
- **Toolchain Diagnostics**: Verifies `curl` CLI on system `PATH`.

### ⚡ Frontend Frameworks & Node Tooling Pack (`RecluseEdit.Extensions.Frontend`)
- Supports **Vue 3 SFC** (`.vue`), **Svelte 5** (`.svelte`), **Astro** (`.astro`), **SolidJS**, **Next.js App Router**, **Remix**, and modern JavaScript/TypeScript bundlers (**Vite**, **Webpack**, **Turbopack**, **Rollup**).
- **Custom High-Fidelity XSHD Syntax Highlighting**:
  - **Vue 3**: Highlighting for `<template>`, `<script lang="ts">`, `<style scoped>`, directives (`v-if`, `v-for`, `v-model`, `@click`, `:bind`), and interpolations (`{{ ... }}`).
  - **Svelte 5**: Highlighting for modern Svelte 5 runes (`$state`, `$derived`, `$effect`, `$props`), control flow blocks (`{#if}`, `{#each}`, `{#await}`), and bindings (`bind:`, `on:`).
  - **Astro**: Highlighting for Astro frontmatter code fences (`---`), component hydration directives (`client:load`, `client:idle`, `client:visible`), `<slot />`, and embedded styles.
- **Modern Frontend Autocomplete & Snippets**:
  - **Vue 3**: `<script setup lang="ts">` boilerplate, reactivity APIs (`ref`, `reactive`, `computed`, `watchEffect`), component macros (`defineProps`, `defineEmits`, `defineModel`), and component template skeletons.
  - **Svelte 5**: Modern rune state declarations, reactive effects, props destructuring with `$props()`, and async markup blocks.
  - **Astro**: Frontmatter skeleton with typed `Astro.props`, slotted layout wrappers, and dynamic islands.
  - **SolidJS**: Fine-grained reactivity (`createSignal`, `createEffect`, `createMemo`), and control components (`<For>`, `<Show>`).
  - **Next.js & Remix**: App Router server/client boundary patterns (`layout.tsx`, `page.tsx`, `'use server'`, `'use client'`, route handlers `GET`/`POST`), and Remix `loader`/`action` functions.
  - **Bundler Configs**: Zero-friction templates for `vite.config.ts`, `webpack.config.js`, `next.config.js`, and `astro.config.mjs`.
- **Toolchain Diagnostics**: Actively detects Node build tools and runtimes: `vite`, `next`, `astro`, `turbo`, `pnpm`, and `bun`.

### 🌐 Node Backend & Microservices Pack (`RecluseEdit.Extensions.NodeBackend`)
- Supports **NestJS**, **Fastify**, **Koa**, and **Socket.io** microservices and realtime web applications. *(Note: Flask, FastAPI, Django, and Express are purposefully and strictly isolated within the Python and existing web packs to eliminate any provider collision or rule overlaps).*
- **Contextual Autocomplete & Idiom Providers**:
  - **NestJS**: Decorator-driven enterprise architecture (`@Controller`, `@Get`, `@Post`, `@Injectable`, `@Module`, DTO validation with `class-validator`, and authorization Guards).
  - **Fastify**: High-throughput routing (`fastify.get`, `fastify.post`), JSON Schema request validation (`querystring`, `params`, `body`, `response`), custom plugins (`fastifyPlugin`), and lifecycle hooks (`preHandler`, `onRequest`).
  - **Koa**: Cascading async middleware chains (`async (ctx, next) => { ... }`), context response management (`ctx.body`, `ctx.status`), and `@koa/router` integration.
  - **Socket.io**: Real-time event-driven communication, server initialization (`new Server(httpServer)`), connection lifecycle (`io.on('connection')`), room broadcasting, and client event handlers (`socket.emit`, `socket.on`).
- **Toolchain Diagnostics**: Actively checks backend process managers and CLIs on `PATH`: **NestJS CLI** (`nest`), **PM2 Process Manager** (`pm2`), and **Fastify CLI** (`fastify`).

---

## 🤖 Automated CI/CD & GitHub Releases

RecluseEdit includes a fully automated GitHub Actions workflow (`.github/workflows/release.yml`) configured for continuous delivery:

- 🏷️ **Triggered on Tag Push**: Pushing a new version tag (e.g. `git tag v1.0.0 && git push origin v1.0.0`) triggers an automated build pipeline on `windows-latest`.
- 🧪 **Full Verification**: Executes the complete test suite (`dotnet test RecluseEdit.slnx -c Release`) across all projects before packaging.
- 📦 **Bundle & Package**:
  - Compiles the host editor and all extensions in `Release` configuration.
  - Bundles the main application executable, dependencies, and all fourteen extensions (`React`, `Angular`, `Flutter`, `Php`, `Ruby`, `Python`, `Laravel`, `Go`, `Scripting`, `DeepSeek`, `Database`, `RestClient`, `Frontend`, and `NodeBackend`) under `Extensions/`.
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
On Windows, NTFS enforces strict file-sharing locks. When background file watchers, IDE status polls, or active **VPN tunnel virtual network drivers & security inspection hooks** (such as WireGuard, OpenVPN, Cisco AnyConnect, or Zscaler) hold or intercept file handles and socket streams at the exact millisecond Git attempts to atomically replace `.git/index` via `.git/index.lock`, the atomic rename can be interrupted mid-transaction, leaving the `.git/index` staging cache truncated to `0 bytes`. The underlying Git repository database, commit history, and branches remain completely safe and uncorrupted.

#### Quick 1-Second Resolution
To safely regenerate the staging index directly from `HEAD` without losing any uncommitted working tree changes, run this command in **PowerShell**:

```powershell
Remove-Item .git\index -Force; git reset
```

*(Or in Bash / POSIX shells: `rm -f .git/index && git reset`)*

---

## 🗺️ Planned Roadmap & SDK Evolution (TODO)

The following architectural evolutions and modular APIs are planned for upcoming milestones:

### 1. Extensible Multi-Model AI Hub (`IAiProvider`)
- **Multi-Model Provider Abstraction**: Evolve `RecluseEdit.Extensions.DeepSeek` into a pluggable multi-model provider architecture with `IAiProvider`, enabling runtime switching between DeepSeek, OpenAI (GPT-4o), Anthropic (Claude 3.5 Sonnet), Google Gemini 2.5, and local offline Ollama models.
- **AI Code Review & In-line Generation**: Inline prompt generation and streaming code actions directly inside the AvalonEdit editor surface.

### 2. Custom Status Bar & Extension UI Contributions (`IStatusBarProvider`)
- **Custom Status Bar Contributions (`IStatusBarProvider`)**: Allow extensions to dynamically inject interactive status bar indicators (e.g., active database connection, Git ahead/behind counters, API server latency, language server health).
- **External CLI Formatters & Linter Adapters**: Extend `IDocumentFormatter` with out-of-the-box adapters for CLI tools (`prettier`, `black`, `ruff`, `gofmt`, `rustfmt`, `dart format`).
- **Live In-Editor Diagnostic Squiggles**: Render wavy squiggles directly beneath tokens identified by `DiagnosticService` and language server providers.

---

## 📜 License

Copyright © 2026 **indoctrinatedrecluse**. All rights reserved.

This project is licensed under the **RecluseEdit Software License**.
- ✅ **Free to Use**: You may freely download, install, and run this editor, along with all included official extensions, for personal, educational, and commercial projects.
- ✉️ **Modification & Redistribution Restriction**: Modifying, creating derivative works, or redistributing the software or any of its official extensions/SDK modules (in source or binary form) requires prior explicit written permission. Please contact [indoctrinatedrecluse](https://github.com/indoctrinatedrecluse/RecluseEdit) for permission requests.

See the [LICENSE](LICENSE) file for complete legal terms. All official extensions and SDK components are covered under these same clauses.

