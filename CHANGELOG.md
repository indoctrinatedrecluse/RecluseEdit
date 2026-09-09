# 📜 Changelog

All notable changes to **RecluseEdit** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.0] - 2026-09-09

### ✨ Added Features

#### 🐹 Go Backend & Language Pack Extension (`RecluseEdit.Extensions.Go`)
- **First-Class Go & Module Support**: File associations for `.go`, `go.mod`, `go.work`, `go.sum`, `.gotmpl`, and `.gohtml`.
- **Custom XSHD Go Syntax Definition**: Dedicated syntax highlighting for Go 1.23+ language constructs including keywords, control flow (`select`, `defer`, `go`), built-in types (`any`, `comparable`, `rune`, `byte`, integers, floats), built-in functions (`make`, `new`, `len`, `append`, `clear`), raw backtick strings (`` `...` ``) with nested struct tag colorization (`json:"..."`, `db:"..."`, `binding:"..."`), runes (`'...'`), and operators (`:=`, `<-`, `...`).
- **Custom XSHD Go Module Syntax Definition**: Highlighting for `go.mod` and `go.work` files covering directives (`module`, `go`, `toolchain`, `require`, `replace`, `exclude`, `use`), version tags (`v1.2.3`), and package paths.
- **Go Core Idioms & Concurrency Completions**: Inline suggestions for entrypoints (`package main`, `func main()`), error handling (`if err != nil`, sentinel errors), goroutines (`go func() { ... }()`), channels, `select` statements, `sync.WaitGroup`, `sync.RWMutex`, context cancellation (`context.WithTimeout`), and structs/interfaces.
- **Go Backend Web Frameworks & ORM**: Rich snippets for **Gin** (`gin.Default`, `r.GET`, `r.POST`, `c.ShouldBindJSON`, `c.JSON`), **Fiber** (`fiber.New`, `app.Get`, `c.BodyParser`), **Chi Router** (`chi.NewRouter`, `r.Use`), **Echo** (`echo.New`), **standard net/http** (`http.HandleFunc`, `http.ListenAndServe`, `json.NewDecoder`, `json.NewEncoder`), **GORM** (`gorm.Open`, `db.AutoMigrate`, `db.Where`, `db.Create`), and `database/sql` (`db.QueryContext`, `db.BeginTx`).
- **Toolchain Diagnostics**: Active diagnostics for the **Go compiler** (`go version`) detecting runtime version and target platform, and **golangci-lint** (`golangci-lint --version`) with installation guidance.
- **Decoupled Architecture**: Built as an independent project target in `RecluseEdit.slnx` outputting directly to `Extensions/Go/` and auto-discovered at runtime.
- **Automated Tests**: Added dedicated unit tests and expanded suite to **60 passing tests** across 11 projects.

#### 🔴 Laravel Framework & Blade Pack Extension (`RecluseEdit.Extensions.Laravel`)
- **First-Class Blade Template Support**: File associations for `.blade.php` and Artisan scripts (`artisan`).
- **Custom XSHD Blade Syntax Definition**: Dedicated syntax highlighting for Blade directives (`@extends`, `@section`, `@yield`, `@if`, `@foreach`, `@forelse`, `@auth`, `@guest`, `@csrf`, `@method`, `@error`, `@livewire`, `@vite`), escaped and unescaped expressions (`{{ $var }}`, `{!! $html !!}`), Blade comments (`{{-- ... --}}`), and embedded HTML markup.
- **Eloquent ORM & Database Snippets**: Model properties (`$fillable`, `$casts`, `$hidden`), relationships (`hasMany`, `belongsTo`, `hasOne`, `belongsToMany`, `morphMany`), query scopes, and migration schema definitions (`Schema::create`, `foreignId`).
- **Routing & Controller Snippets**: RESTful endpoints (`Route::get`, `Route::post`, `Route::resource`, `Route::apiResource`), route middleware groups, controller actions, Request validation, and response helpers (`view()`, `response()->json()`).
- **Toolchain Diagnostics**: Active diagnostics for the official **Laravel Installer CLI** (`laravel -V`) and **Artisan CLI** (`php artisan --version`).
- **Decoupled Architecture**: Built as an independent project target in `RecluseEdit.slnx` outputting directly to `Extensions/Laravel/` and auto-discovered at runtime.
- **Automated Tests**: Added dedicated unit tests and expanded suite to **54 passing tests** across 10 projects.

---

## [1.1.0] - 2026-09-09

### ✨ Added Features

#### 🐍 Python & Full-Stack Web Pack Extension (`RecluseEdit.Extensions.Python`)
- **First-Class Python & Template Support**: File associations for `.py`, `.pyw`, `.pyi`, `.pyd`, `.jinja`, `.jinja2`, `.j2`, `.html.jinja`, and `.djhtml`.
- **Custom XSHD Python Syntax Definition**: Highlighting for Python 3.12+ features including decorators (`@app.route`, `@property`, `@classmethod`), f-strings (`f"..."` and `f'...'` with `{expression}` variable interpolation), triple-quoted multiline docstrings (`"""..."""` and `'''...'''`), type annotations (`Any`, `Optional`, `Union`, `List`, `Dict`), dunder attributes/methods (`__init__`, `__repr__`, `__name__`), and pattern matching (`match`/`case`).
- **Custom XSHD Jinja2 & Django Template Definition**: Highlighting for template statements (`{% for %}`, `{% if %}`, `{% block %}`, `{% extends %}`, `{% include %}`, `{% csrf_token %}`), template expressions (`{{ ... }}`), filter pipes (`|upper`, `|safe`, `|length`), comments (`{# ... #}`), and embedded HTML element/attribute tags.
- **Python Frontends & UI Completions**: Inline suggestions and snippets for modern Python UI frameworks:
  - **Streamlit**: `st.title`, `st.header`, `st.button`, `st.dataframe`, `st.sidebar`, `st.plotly_chart`, `st.selectbox`, `st.multiselect`, and metrics.
  - **Gradio**: `gr.Interface`, `gr.Blocks`, `gr.Row`, `gr.Column`, `gr.Button`, `gr.Textbox`, `gr.Number`, and `gr.Image`.
  - **Reflex**: `class State(rx.State)`, `def index() -> rx.Component`, `rx.vstack`, and `app = rx.App()`.
- **Python Web Backends & Templates**:
  - **Flask**: `app = Flask(__name__)`, `@app.route()`, `@app.get()`, `@app.post()`, `render_template()`, `jsonify()`, `request.get_json()`, and `request.args.get()`.
  - **Django**: `class Model(models.Model)`, `urlpatterns = [ path(...) ]`, `render()`, `JsonResponse()`, and template tags.
  - **FastAPI**: `app = FastAPI()`, `BaseModel`, and status code helpers.
- **Express.js Snippets**: Server bootstrap (`const app = express()`), JSON body parsing middleware, REST endpoint routing (`app.get`, `app.post`, `app.put`, `app.delete`), router modules (`express.Router()`), and global error handling middleware.
- **Toolchain Diagnostics**: Active diagnostics for the **Python interpreter** (`python -V`), **Pip package manager** (`pip -V`), and **Django CLI** (`django-admin --version`).
- **Decoupled Architecture**: Fully isolated project target in `RecluseEdit.slnx` built to `Extensions/Python/` and automatically discovered at startup by `ExtensionManager`.

#### 💎 Ruby & Ruby on Rails Language Pack (`RecluseEdit.Extensions.Ruby`)
- **First-Class Ruby Support**: File associations for `.rb`, `.rake`, `.gemspec`, `.ru`, `Gemfile`, and `Rakefile`.
- **Custom XSHD Ruby Syntax Highlighting**: Custom AvalonEdit syntax definition covering Ruby keywords (`def`, `class`, `module`, `yield`, `self`), symbols (`:symbol`), instance variables (`@var`), class variables (`@@var`), global variables (`$var`), regex literals (`/.../`), percent string/array notations (`%w`, `%i`), and double-quoted string interpolation (`#{...}`).
- **Ruby Completion Snippets**: Inline snippets and idioms for methods (`def`), classes (`class`), modules (`module`), attribute accessors (`attr_accessor`), block iteration (`each do |item|`, `map do |item|`), exception handling (`begin ... rescue StandardError => e`), and conditionals (`case ... when`, `unless`).
- **Ruby on Rails & ERB Snippets**:
  - ActiveRecord model definitions (`class Model < ApplicationRecord`), associations (`has_many`, `belongs_to`, `has_one`, `has_and_belongs_to_many`), validations (`validates :field, presence: true`), callbacks (`before_action`), and scopes.
  - ActionController helpers (`respond_to`, `render json:`, `params.require(:item).permit(...)`).
  - RESTful routing declarations (`resources :items`, `root to: "home#index"`, `namespace :api`).
  - ERB template tags (`<%= ... %>`, `<% ... %>`, `<% if ... %>`).
- **Toolchain Diagnostics**: Active diagnostics and path resolution checks for the Ruby runtime (`ruby -v`), Bundler (`bundle -v`), and Rails CLI (`rails -v`).

#### 🎨 Exhaustive Syntax Highlighting Across All Languages
- **High-Fidelity Markdown Grammar**: Dedicated XSHD syntax definition covering headings (`#` to `######`), bold (`**`, `__`), italics (`*`, `_`), inline code (`` `code` ``), fenced code blocks (```` ``` ````), blockquotes (`>`), links (`[text](url)`), lists (`*`, `-`, `+`, `1.`), horizontal rules, tables, and inline HTML tags.
- **Modern ECMAScript 2024 & TypeScript Grammar**: Dedicated XSHD grammar supporting modern JS/TS declarations, control flow (`if`, `switch`, `try/catch`, `await`, etc.) in `#C586C0`, template literals with `${expression}` interpolation, arrow functions (`=>`), regex literals (`/.../`), built-in utility types (`Partial`, `Promise`, `Record`, etc.), and function call invocation coloring (`#DCDCAA`).
- **Modern CSS3 / SCSS / LESS Grammar**: Custom XSHD definition supporting CSS custom properties/variables (`--custom-prop`), `var()`, modern pseudo-classes (`:has()`, `:is()`, `:where()`), pseudo-elements, modern at-rules (`@container`, `@media`, `@keyframes`), color formats (`#hex`, `rgb`, `hsl`, `oklch`), and comprehensive CSS units.
- **Modern PHP 8+ Grammar**: Custom XSHD syntax definition supporting match expressions, PHP 8 attributes (`#[Attribute]`), typed properties, union types, variables (`$var`), and double-quoted string interpolation.
- **React JSX & TSX Grammar (`RecluseEdit.Extensions.React`)**: Dedicated XSHD syntax definition distinguishing custom React components (`<Component>`), standard HTML tags (`<div>`), JSX attributes (`className="..."`, `onClick={...}`), embedded JS expressions, and all React Hooks.
- **GraphQL Grammar (`RecluseEdit.Extensions.React`)**: Dedicated XSHD grammar highlighting operations (`query`, `mutation`, `subscription`, `fragment`), schema definition keywords, directives (`@include`, `@skip`), variables (`$var`), and types (`String`, `Int`, `Boolean`, `ID`).
- **Angular HTML & TypeScript Grammars (`RecluseEdit.Extensions.Angular`)**:
  - `AngularHTML`: Dedicated XSHD grammar highlighting modern control flow (`@if`, `@for`, `@switch`, `@defer`), structural directives (`*ngIf`, `*ngFor`), property bindings (`[prop]`), event bindings (`(event)`), two-way bindings (`[(ngModel)]`), and interpolation (`{{ item | pipe }}`).
  - `AngularTS`: Dedicated XSHD grammar highlighting decorators (`@Component`, `@Injectable`, `@Directive`, `@Input`), Signals (`signal`, `computed`, `effect`, `input`, `output`, `model`), and dependency injection (`inject`).
- **Polished Dart & Ruby Grammars**: Added method invocation highlighting, string interpolation (`${expr}` in Dart, `#{expr}` in Ruby), and Rails ActiveRecord macro coloring.
- **Expanded Dark+ Palette Mapping**: Enhanced `ApplyDarkThemeColors()` with rich brushes for control flow, functions, types, variables, regex, headings, bold, tag brackets, and attributes.

#### 🔲 Live Bracket Matching & Highlighting
- **Active Bracket Matching**: Added `BracketHighlightRenderer` to `EditorControl` providing instant visual highlight borders around matching pairs of `()`, `[]`, and `{}` as the caret moves, matching modern VS Code UX.

#### 📜 Software License Extension Coverage
- Updated `LICENSE` and `README.md` to explicitly state that all official language extensions and SDK components (`RecluseEdit.Sdk`) are fully covered under the RecluseEdit Software License clauses.

#### 🗂️ Unified File Dialog Filters
- Modernized `OpenFileDialog` and `SaveFileDialog` in `MainWindow` with comprehensive filters covering all supported file formats: Python & Jinja templates, Web & Script, React & GraphQL, Angular, Flutter & Dart, PHP, Ruby & Rails, C# & XAML, and Markdown.

#### 🧪 49 Passing Automated Tests
- Expanded the automated test suite across all 9 projects to **49 passing tests** with 0 warnings and 0 errors.

---

## [1.0.0] - 2026-09-08

### 🌟 Initial Official Release

The debut release of **RecluseEdit**, a modern, lightweight, high-performance desktop code editor crafted for web applications and cross-platform app engineering using **.NET 10**, **WPF**, and **AvalonEdit**.

### ✨ Added Features

#### 🖥️ Core Editor Workspace
- **Multi-File Tabbed Interface**: Open, edit, and navigate between multiple documents with ease. Middle-click to close tabs, plus tab context menu actions (*Close Others*, *Close to Right*, *Copy Full Path*, *Reveal in Explorer*).
- **Collapsible Workspace Explorer**: Interactive tree view sidebar (`Ctrl+B`) for exploring project directories and opening files (`Ctrl+Shift+O`).
- **Floating Find & Replace Overlay**: Integrated search bar (`Ctrl+F`, `Ctrl+H`) with Find Next (`Enter`), Find Previous (`Shift+Enter`), Match Case toggle, and Replace All.
- **Smart Auto-Closing Pairs & HTML Tags**: Automatic paired closing for `()`, `{}`, `[]`, `""`, `''`, and instant auto-tag closing for HTML/XML elements (e.g., `<div>` &rarr; `</div>`).
- **Visual Editing Aids**: Folding margin for collapsible code blocks, customizable line-number gutter, word-wrapping toggle, and font zooming with `Ctrl` + `MouseWheel`.
- **VS Code Dark+ Palette**: Custom high-contrast theme styling optimized for dark backgrounds (`#1E1E1E`).

#### 🎨 Syntax Highlighting & Grammars
- **Core Formats**: Built-in syntax highlighting for HTML, CSS, JavaScript, TypeScript, XML/XAML, C#, and Markdown.
- **Dedicated JSON XSHD**: Custom XML syntax definition providing property name distinction (`"key":`), string values, numbers, booleans, null, and comment support (`//` and `/* */`).
- **Color Harmonization**: Unified Dark+ color mapping ensuring readable strings (`#CE9178`), comments (`#6A9955`), keywords (`#569CD6`), types (`#4EC9B0`), attributes (`#9CDCFE`), and numbers (`#B5CEA8`).

#### ⚡ Dual Autocomplete System
- **Inline Ghost Text**: Lightweight suggestions rendered directly at the cursor in muted italic text (`Tab` to accept, `Esc` to dismiss).
- **IntelliSense Completion Popup**: Traditional popup list for tags, CSS properties, and JavaScript keywords (`Ctrl+Space`).

#### 🔌 Pluggable Extension Architecture (`RecluseEdit.Sdk`)
- Formalized SDK providing extension lifecycle contracts (`IExtension`, `IExtensionHost`), language definitions (`LanguageDefinition`), completion providers (`IInlineCompletionProvider`, `IIntelliSenseProvider`), and compiler verification (`IToolchainCheck`).
- **Dynamic Extension Discovery**: Automatic runtime loading of extension assemblies placed in `bin/Release/net10.0-windows/Extensions/<Name>/`.
- **Extension & Toolchain Manager UI**: Dedicated manager dialog (`Extensions -> Manage Extensions...`) displaying active extensions and a real-time toolchain diagnostics tab.

#### 📦 Official Language Extensions (Independent Build Targets)
- **⚛️ React, Redux & GraphQL Pack (`RecluseEdit.Extensions.React`)**:
  - File associations for `.jsx`, `.tsx`, and `.graphql` / `.gql`.
  - Inline autocomplete for React Hooks (`useState`, `useEffect`, `useCallback`, `useMemo`, `useRef`, etc.), Redux Toolkit (`createSlice`, `createAsyncThunk`), and GraphQL queries/mutations.
  - Active toolchain checks for Node.js (`node`), npm (`npm`), and TypeScript compiler (`tsc`).
- **🅰️ Angular Language & Framework Pack (`RecluseEdit.Extensions.Angular`)**:
  - Support for Angular HTML templates (`.component.html`) and Angular TypeScript (`.component.ts`, `.service.ts`, `.directive.ts`, etc.).
  - Autocomplete for Angular Signals (`signal`, `computed`, `effect`, `input`, `output`, `model`), modern control flow (`@if`, `@for`, `@switch`, `@defer`), and standalone decorators (`@Component`, `@Injectable`, `@Directive`, `@Pipe`, `inject()`).
  - Active toolchain check for Angular CLI (`ng`).
- **🐦 Flutter & Dart Language Pack (`RecluseEdit.Extensions.Flutter`)**:
  - Support for Dart (`.dart`) with a custom Dart 3 XSHD syntax definition.
  - Widget snippets: `stless` (`StatelessWidget`), `stful` (`StatefulWidget`), `setState()`, and common layout widgets (`Scaffold`, `Column`, `Row`, `Container`, `ListView.builder`, `Navigator`).
  - Active toolchain checks for Flutter SDK (`flutter`) and Dart SDK (`dart`).
- **🐘 PHP Language Pack (`RecluseEdit.Extensions.Php`)**:
  - Support for PHP (`.php`, `.phtml`) featuring modern PHP 8+ constructs (`match`, `enum`, constructor property promotion, typed properties, arrow functions).
  - Active toolchain checks for PHP CLI (`php`) and Composer (`composer`).

#### 🤖 CI/CD & Delivery
- **GitHub Actions Workflow**: Automated build and release pipeline (`.github/workflows/release.yml`) triggered on tag pushes (`v*`) to run the 27-test automated test suite and package a portable distribution archive (`RecluseEdit-windows-<tag>.zip`) attached to GitHub Releases.
- **Git Environment Configuration**: Configured `.gitattributes` to enforce consistent LF/CRLF normalization across development environments and CI runners.

[1.2.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/releases/tag/v1.0.0

