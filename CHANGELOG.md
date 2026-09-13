# 📜 Changelog

All notable changes to **RecluseEdit** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [4.0.0] - 2026-09-13

### 🚀 Major Release: Extensible Theme System, Public Theme API, 9 Built-In Themes & Dedicated Themes Menu

#### 🧩 1. Public Theme SDK (`RecluseEdit.Sdk`)
- **`IThemeDefinition` & `ThemeDefinitionBase`**: Clean provider interfaces and abstract base classes for extension developers to register custom themes.
- **`ThemeColors` Specification**: Fine-grained color token schema covering primary surfaces, foregrounds, borders, accents, margins, status bars, activity bars, caret, selection, bracket match, terminals, and Markdown preview, plus extensible `CustomTokens` dictionary.
- **`ThemeType` Classification**: Support for `Dark`, `Light`, `HighContrast`, and `Creative` categories.
- **`IExtensionHost.RegisterTheme(IThemeDefinition)`**: First-class theme registration hook in the extension host.

#### 🖌️ 2. Core Theme Engine & 9 Built-In Creative Themes
- **`ThemeManager`**: Core management engine featuring dynamic WPF resource mutation, live preview, rollback, and preference persistence (`%APPDATA%/RecluseEdit/theme_settings.json`).
- **9 Handcrafted Built-In Themes**:
  1. **Recluse Dark+ (Default)**: Deep slate grays with classic VS blue accents.
  2. **Recluse Light+ (Daylight)**: Clean alabaster daylight palette for high-ambient lighting.
  3. **Cyberpunk 2077 (Neon Dusk)**: High-contrast midnight purple (`#120E24`) with hot pink (`#FF2A6D`) and laser cyan (`#05D9E8`) accents.
  4. **Monokai Pro**: Warm charcoal olive with pastel magenta, lime green, and canary yellow highlights.
  5. **Solarized Dark**: Ethan Schoonover's precision low-glare cyan-teal palette for extended sessions.
  6. **Dracula**: Classic gothic purple and vampire pink theme with vibrant accents.
  7. **Nord**: Cool polar slate, snow storm whites, and frost blues inspired by arctic nights.
  8. **Retro Matrix**: Phosphor green on CRT obsidian black for hacker aesthetics.
  9. **High Contrast Black**: W3C AAA-accessible pure black with vivid borders and high visibility.

#### 🎛️ 3. Interactive Theme Picker & UX Integration (`Ctrl+K, Ctrl+T`)
- **Interactive Theme Picker Dialog (`ThemePickerDialog`)**: Filterable theme list with 4-swatch color dots, keyboard navigation, and instant live preview on item selection.
- **Live Preview & Safe Rollback**: Preview any theme instantaneously; pressing <kbd>Esc</kbd> or closing the dialog cleanly rolls back to the previous theme, while <kbd>Enter</kbd> commits and persists it.
- **Status Bar Integration**: Clickable `StatusTheme` indicator showing the current active theme with one-click modal opening.
- **Keyboard Chord & Command Palette**: Standard two-key chord <kbd>Ctrl+K, Ctrl+T</kbd>, direct shortcut <kbd>Ctrl+Alt+T</kbd>, and Command Palette entries (`Preferences: Color Theme`, `Select Color Theme...`).

#### 🌐 4. Editor & Live Preview Synchronization
- **AvalonEdit Reactivity**: Text editor background, foreground, line numbers, caret brush, selection brush, current line highlight, and bracket matching borders adapt dynamically to the active theme.
- **Markdown Live Preview Harmonization**: Live Preview Markdown pane inherits background, text, heading, link, and code block styles dynamically from the active theme.

#### 🧩 5. Extension Demonstration & Automated Verification
- **`VueEmeraldTheme`**: Extension theme implemented in `RecluseEdit.Extensions.Frontend` demonstrating third-party theme creation via `ThemeDefinitionBase`.
- **Automated Test Suite**: Added comprehensive unit test coverage in `ThemeManagerTests.cs`, bringing total automated tests to 183 with 100% pass rate.

---

## [3.0.0] - 2026-09-13

### 🚀 Major Release: Live Preview, Formatting Pipeline, Project Scaffolding & Web Console

#### 🌐 1. Chromium-Powered Live Web & Markdown Preview (`Ctrl+Shift+V`)
- **Embedded WebView2 Engine**: Integrated `Microsoft.Web.WebView2` for high-fidelity split-pane live preview with real-time DOM rendering.
- **Debounced Keystroke Sync**: 300ms debounced text-change synchronization with immediate refresh button and navigation reload.
- **Relative Asset Resolution**: Injected `<base href="file://...">` header allowing local CSS, JS, fonts, and images to resolve naturally.
- **Responsive Viewport Switcher**: Quickly test responsive layouts across standard viewports:
  - **Responsive**: 100% fluid container width.
  - **Mobile**: 375px (iPhone / Pixel).
  - **Tablet**: 768px (iPad portrait).
  - **Desktop**: 1200px (Desktop / Laptop).
- **GitHub Dark-Themed Markdown Rendering**: Full-featured Markdown renderer with GitHub Dark typography, tables, alerts (`> [!NOTE]`, `> [!TIP]`, `> [!WARN]`), task lists, syntax-highlighted code blocks, and scroll sync.

#### 🎨 2. Document Formatting & Syntax Diagnostics Pipeline (`Shift+Alt+F`)
- **Extensible `IDocumentFormatter` SDK**: Pluggable formatting architecture allowing extensions to register custom language prettifiers.
- **Built-in Pure C# Formatters**:
  - **JSON**: Formatted with configurable indentation (2/4 spaces or tabs) and escaping.
  - **CSS / SCSS / LESS**: Clean bracket placement, property indentation, and selector spacing.
  - **HTML / XML / XAML**: Hierarchical tag indentation with intelligent self-closing void tag handling.
  - **SQL**: Standardized capitalization of SQL clauses (`SELECT`, `FROM`, `WHERE`, `ORDER BY`, `JOIN`, `GROUP BY`) with structured line breaks.
  - **JavaScript / TypeScript**: Automated bracket indentation and multi-line alignment.
  - **Markdown**: Heading whitespace normalization, code block preservation, and list cleanup.
- **Format on Save**: Option under `Edit -> Format on Save` to automatically prettify files upon saving.
- **Real-Time Syntax Diagnostics (`DiagnosticService`)**:
  - JSON parser syntax validation with exact line/column error localization.
  - Bracket, brace, and parenthesis balance verification with stack tracking.
  - HTML tag mismatch and unclosed element auditing.

#### 🚀 3. Project Scaffolding Wizard (`Ctrl+Shift+N`)
- **Template Generation Wizard (`NewProjectDialog`)**: Interactive modal accessible via `File -> New Project...` (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>) with category filtering, template description, and project naming.
- **7 Production-Ready Templates**:
  - **Static Web Starter**: HTML5, CSS3 with responsive grid, and JavaScript counter demo.
  - **Vite + React 19 + TypeScript**: Modern React 19 with JSX, state hook, and Vite dev server.
  - **Vite + Vue 3 + TypeScript**: Vue 3 SFC with `<script setup lang="ts">`, Composition API, and Vite.
  - **Vite + Svelte 5 + TypeScript**: Svelte 5 with modern Runes (`$state`, `$derived`) and Vite.
  - **Vite + SolidJS + TypeScript**: SolidJS fine-grained reactivity with signals and Vite.
  - **Fastify Microservice API**: High-performance Node.js/TypeScript backend service with Fastify routing and schema validation.
  - **Markdown Documentation Site**: Multi-page structured technical docs with `getting-started.md`, `architecture.md`, and `api-reference.md`.
- **Integrated Workspace & Git Initialization**: Automatically initializes Git repository (`git init`), opens the new workspace in Explorer, and loads the entrypoint file directly into the editor and Live Preview.

#### 💻 4. Integrated Web Developer Console & Problems Dock
- **Multi-Tab Bottom Dock**: Consolidated bottom pane docking **Terminal** (<kbd>Ctrl+`</kbd>), **Web Console**, and **Problems**.
- **Injected Console Bridge (`LivePreviewBridge`)**: JavaScript shim capturing `console.log`, `console.info`, `console.warn`, `console.error`, unhandled runtime exceptions, and unhandled promise rejections directly into the IDE.
- **Filterable Web Console Viewer (`WebConsoleControl`)**: Severity filtering (All, Info, Warnings, Errors), message search filter, timestamps, and one-click clear.
- **Centralized Problems View (`ProblemsPanelControl`)**: Aggregated file diagnostics with severity icons, line/column coordinates, and double-click navigation that jumps directly to the erroneous line in the code editor.

### 🧪 Automated Testing
- Added unit test suites for `DocumentFormattingServiceTests`, `DiagnosticServiceTests`, `ProjectScaffoldingServiceTests`, and `LivePreviewBridgeTests`.
- All **175 automated unit tests** passing with **0 warnings and 0 errors** across all 17 projects in the solution.

---

## [2.2.0] - 2026-09-13

### ✨ Added Features

#### ⚡ Frontend Frameworks & Node Tooling Pack (`RecluseEdit.Extensions.Frontend`)
- **Vue 3 SFC Highlighting & Idioms**: Custom XSHD syntax grammar for `.vue` files supporting `<template>`, `<script lang="ts">`, `<style scoped>`, Vue directives (`v-if`, `v-for`, `v-model`, `@click`, `:bind`), and interpolations (`{{ ... }}`). Autocomplete provider for `<script setup lang="ts">`, reactivity APIs (`ref`, `reactive`, `computed`, `watchEffect`), and macros (`defineProps`, `defineEmits`, `defineModel`).
- **Svelte 5 Runes Highlighting & Idioms**: Dedicated XSHD syntax grammar for `.svelte` files highlighting modern Svelte 5 runes (`$state`, `$derived`, `$effect`, `$props`), control flow blocks (`{#if}`, `{#each}`, `{#await}`), and bindings (`bind:`, `on:`). Autocomplete provider with Svelte 5 boilerplate and reactive constructs.
- **Astro Syntax Highlighting & Idioms**: Dedicated XSHD grammar for `.astro` components supporting frontmatter fences (`---`), component hydration directives (`client:load`, `client:idle`, `client:visible`), `<slot />`, and embedded styles.
- **Modern Web Frameworks & Bundler Configs**: Completions for SolidJS fine-grained reactivity (`createSignal`, `createEffect`, `<For>`, `<Show>`), Next.js App Router conventions (`layout.tsx`, `page.tsx`, `'use server'`, route handlers), Remix `loader`/`action`, and bundler configuration boilerplate (`vite.config.ts`, `webpack.config.js`, `next.config.js`, `astro.config.mjs`).
- **Frontend Toolchain Diagnostics**: Active CLI detection for `vite`, `next`, `astro`, `turbo`, `pnpm`, and `bun`.

#### 🌐 Node Backend & Microservices Pack (`RecluseEdit.Extensions.NodeBackend`)
- **NestJS Enterprise Architecture**: Completions for decorator-driven modules, controllers, providers, guards, and DTO validation with `class-validator` (`@Controller`, `@Get`, `@Post`, `@Injectable`, `@Module`, `ValidationPipe`).
- **Fastify Web Framework**: Completions for high-performance route declarations, JSON Schema validation (`querystring`, `params`, `body`, `response`), custom plugins (`fastifyPlugin`), and lifecycle hooks (`preHandler`, `onRequest`).
- **Koa Web Framework**: Snippets for cascading async middleware chains (`async (ctx, next) => { ... }`), context responses (`ctx.body`, `ctx.status`), and `@koa/router` routing.
- **Socket.io Realtime Services**: Autocomplete for WebSocket server initialization (`new Server(httpServer)`), connection lifecycle (`io.on('connection')`), room broadcasting, and client event emitters/listeners (`socket.emit`, `socket.on`).
- **Strict Collision Isolation**: Meticulously ensured zero conflict with pre-existing extensions by strictly excluding Flask, FastAPI, Django, and Express from NodeBackend.
- **Node Backend Toolchain Diagnostics**: Active CLI detection for `nest`, `pm2`, and `fastify`.

### 🧪 Automated Testing
- Added comprehensive unit test suites in `FrontendExtensionTests.cs` and `NodeBackendExtensionTests.cs`, including strict verification against rule collisions with Express/Python.
- Expanded the automated test suite to **152 passing tests, 0 warnings, 0 errors** across all 17 projects in `RecluseEdit.slnx`.

---

## [2.1.0] - 2026-09-12

### ✨ Added Features

#### 🌿 Git Diff Gutter Indicators & Source Control Panel (`Ctrl+Shift+G`)
- **Live Diff Margin Indicators**: Added interactive AvalonEdit gutter indicators rendering real-time line additions (green bar), modifications (blue bar), and deletions (red triangle indicator) calculated directly against Git `HEAD`.
- **Activity Bar Navigation**: Added 46px VS Code-style left Activity Bar dock allowing seamless switching between 📁 **Explorer** (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>E</kbd>) and 🌿 **Source Control** (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd>).
- **Dedicated Source Control Panel**:
  - Live branch status badge with remote sync metrics (`ahead` / `behind` commit counters).
  - Commit message composer with <kbd>Ctrl+Enter</kbd> fast commit action.
  - Segregated collapsible lists for **Staged Changes** and **Changes** (working tree).
  - One-click Stage (+), Unstage (-), Discard/Restore changes, and Commit & Push actions.
- **Dynamic Right Dock Tabs**: Multi-side-panel tab header supporting simultaneous docking of DeepSeek AI, Database Explorer, and REST Client workbench.

#### 🗄️ Database & SQL Explorer Pack (`RecluseEdit.Extensions.Database`)
- **Dialect-Agnostic SQL Syntax Highlighting**: Custom XSHD grammar supporting ANSI SQL, SQLite, PostgreSQL, MySQL, and T-SQL in Dark+ palette.
- **Inline SQL Completions**: Ghost-text completions for `SELECT`, `INSERT INTO`, `UPDATE`, `DELETE FROM`, `CREATE TABLE IF NOT EXISTS`, `ALTER TABLE`, `INNER JOIN`, `LEFT JOIN`, `CREATE INDEX`, and transaction blocks.
- **Interactive Database Side Panel**: Dedicated right dock panel with SQLite file picker, live schema inspector showing tables and column types, multi-line query editor with <kbd>Ctrl+Enter</kbd> execution, responsive DataGrid for tabular query results, and one-click **CSV Export**.
- **Database Toolchain Checks**: Automatic diagnostics and detection for `sqlite3`, PostgreSQL (`psql`), and MySQL (`mysql`) CLI tools.

#### ⚡ REST Client & API Workbench Pack (`RecluseEdit.Extensions.RestClient`)
- **HTTP / REST Syntax Highlighting**: Dedicated XSHD grammar for `.http` and `.rest` files highlighting HTTP verbs, boundaries (`###`), headers, variables (`{{...}}`), URLs, and JSON payloads.
- **Inline HTTP Completions**: Suggestions for request headers (`Content-Type: application/json`, `Authorization: Bearer`), HTTP verbs, and request templates.
- **Integrated API Workbench Dock**: Dedicated right dock panel with HTTP method dropdown (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `HEAD`, `OPTIONS`), URL bar, <kbd>Ctrl+Enter</kbd> send action, status code badge (green 2xx, yellow 3xx, orange 4xx, red 5xx), latency metrics (ms), size counter, request headers/payload tabs, and formatted JSON response viewer with one-click clipboard copying.
- **cURL Toolchain Check**: Active verification for `curl` CLI on system `PATH`.

### 🧪 Automated Testing
- Added automated test suites for `GitService` (status parsing, staged/unstaged separation, diff hunk extraction), `DatabaseExtension` (syntax, completion, toolchains, panel), and `RestClientExtension` (syntax, HTTP parser, JSON formatter, completions, toolchain).
- Total automated test suite expanded to **134 passing tests** with 0 warnings.

---

## [2.0.0] - 2026-09-12

### ✨ Added Features

#### ⚡ Universal Command Palette (`Ctrl+Shift+P`, `F1`, `Ctrl+P`, `Ctrl+G`)
- **Modern Modal Search Overlay**: Implemented a centered VS Code Dark+ styled command palette with smooth drop shadow, subtle borders, real-time keyboard navigation, and fuzzy search filtering.
- **Prefix-Based Mode Switching**:
  - **Command Mode (`>`)** (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> or <kbd>F1</kbd>): Search and execute all editor actions across File, Edit, Line, View, Extensions, Terminal, DeepSeek AI, and Help menus.
  - **Quick Open Mode (default)** (<kbd>Ctrl</kbd>+<kbd>P</kbd>): Fast file and tab switcher across all open tabs and workspace files.
  - **Go To Line Mode (`:`)** (<kbd>Ctrl</kbd>+<kbd>G</kbd>): Jump to any target line and column (`:line` or `:line:col`) with real-time feedback and validation.
  - **Help Mode (`?`)**: Discovers available prefix modes and shortcut hints.
- **Dynamic File Enumeration**: Queries workspace directories asynchronously to index and surface workspace files for instant navigation.

#### ✂️ Core QoL Editor & Multiline Editing Operations
- **Toggle Comments (<kbd>Ctrl</kbd>+<kbd>/</kbd>)**: Context-aware line comment toggling supporting `//` (C/C++, C#, JS, TS, Go, Rust, Dart, PHP), `#` (Python, Ruby, Bash, PowerShell), `--` (Lua, SQL), and fallback multi-line blocks.
- **Move Lines Up / Down (<kbd>Alt</kbd>+<kbd>&uarr;</kbd>, <kbd>Alt</kbd>+<kbd>&darr;</kbd>)**: Move current line or multiline block up or down with automatic caret repositioning.
- **Duplicate Lines (<kbd>Shift</kbd>+<kbd>Alt</kbd>+<kbd>&uarr;</kbd>, <kbd>Shift</kbd>+<kbd>Alt</kbd>+<kbd>&darr;</kbd>)**: Duplicate active line or selection above or below.
- **Delete Line (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd>)**: Atomically deletes entire current line and newline.
- **Join Lines (<kbd>Ctrl</kbd>+<kbd>J</kbd>)**: Merges the current line with the next, stripping leading whitespace and inserting a single separator space.
- **Case Transformations (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>U</kbd>, <kbd>Ctrl</kbd>+<kbd>U</kbd>)**: Converts selection or active word to UPPERCASE or lowercase.
- **Sort Lines**: Alphabetically sorts selected lines in ascending order.
- **Trim Trailing Whitespace**: Automatically cleans all trailing spaces and tabs across the document while preserving caret position.
- **Column / Box Selection Cursor Editing (<kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>&uarr;</kbd>, <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>&darr;</kbd>)**: Expands box selection vertically for multi-line column typing, backspacing, and deletion.
- **Go To Line Dialog**: Standalone dark dialog accessible via **Edit &rarr; Go to Line...** or <kbd>Ctrl</kbd>+<kbd>G</kbd>.

#### ⌨️ Searchable Keyboard Shortcuts Reference Window
- **Interactive Shortcuts Window**: New dedicated Dark+ window accessible via **Help &rarr; Keyboard Shortcuts** or <kbd>Ctrl</kbd>+<kbd>K</kbd>, <kbd>Ctrl</kbd>+<kbd>S</kbd>.
- **Search & Filter**: Real-time filtering across shortcut combinations, command titles, descriptions, and categories (`Editor`, `Navigation`, `File`, `View`, `AI & Terminal`, etc.).
- **Line Operations Menu**: Added dedicated `Line Operations` submenu under `Edit` menu with all keyboard mappings visible.

#### 🎨 Custom Application Icon & Window Branding
- **Custom Application Icon**: Designed and integrated a bespoke brand icon featuring a glowing neon cyan & violet monogram `R` intertwined with code brackets `< / >` and subtle circuit traces on a dark squircle badge.
- **Multi-Resolution Windows Executable Icon (`.ico`)**: Generated a crisp multi-tier Windows icon (`Assets/app.ico`) embedding 16x16, 24x24, 32x32, 48x48, 64x64, 128x128, and 256x256 resolution frames. Configured `<ApplicationIcon>` in `RecluseEdit.csproj` for native Windows Explorer, desktop shortcut, Alt+Tab, and taskbar rendering.
- **WPF Window Icon Integration**: Bound assembly resource icon (`pack://application:,,,/Assets/app.ico`) to `MainWindow`, `ExtensionManagerWindow`, `ConfigureShellDialog`, `KeyboardShortcutsWindow`, and `GoToLineDialog` for consistent visual presentation across all application surfaces.
- **High-Resolution Asset**: Bundled 512x512 high-resolution master asset (`Assets/app.png`) as an embedded assembly resource.

### 🧪 Automated Testing
- Added 16 new automated unit tests covering `EditorOperations` (line movements, duplications, deletions, comment toggling, joins, case transforms, trims) and `CommandRegistry` (mode matching, search filtering, line jumping), increasing test suite to **119 passing tests** with 0 warnings.

---

## [1.5.0] - 2026-09-11

### ✨ Added Features

#### 📜 Scripting & Systems Language Pack (`RecluseEdit.Extensions.Scripting`)
- **Multi-Language Support**: Comprehensive official extension adding first-class language support for **Rust**, **Lua**, **PowerShell**, and **Bash** in a single high-performance package.
- **Dark+ AvalonEdit XML Syntax Highlighting Definitions (XSHD)**:
  - **Rust (`.rs`)**: Highlighting for lifetimes (`'a`), macros (`println!`, `vec!`, `panic!`), attributes (`#[derive(...)]`), raw strings (`r#"..."#`), byte strings, types, and operators (`::`, `->`, `=>`).
  - **Lua (`.lua`)**: Highlighting for block comments, multiline literal strings (`[[ ... ]]`), standard libraries (`string`, `table`, `math`, `io`, `os`, `coroutine`, `utf8`), special variables (`_G`, `self`), and operators (`..`, `~=`, `//`, `#`).
  - **PowerShell (`.ps1`, `.psm1`, `.psd1`)**: Highlighting for cmdlet Verb-Noun pairs (`Get-Process`, `Invoke-WebRequest`), parameters (`-Path`, `-Force`), type accelerators (`[string]`, `[hashtable]`), variables (`$env:PATH`, `$_`), and comparison operators (`-eq`, `-match`, `-replace`).
  - **Bash / POSIX Shell (`.sh`, `.bash`, `.zsh`, `.ksh`, `.command`)**: Highlighting for shebang lines (`#!/usr/bin/env bash`), parameter expansion (`$VAR`, `${VAR:-default}`), shell builtins, Unix utilities (`grep`, `awk`, `sed`, `curl`, `chmod`), and test brackets (`[[ ]]`).
- **Contextual Autocomplete & Snippet Providers**:
  - `RustCompletionProvider`: Functions, pattern matching (`match`, `if let`, `while let`), tests, structs, enums, derive macros, and standard collections.
  - `LuaCompletionProvider`: Local and member functions, `for pairs`/`ipairs` loops, modules with metatables, and `pcall` error handling.
  - `PowerShellCompletionProvider`: Advanced cmdlet functions with `[CmdletBinding()]`, `param()` blocks, loops, and `try/catch/finally`.
  - `BashCompletionProvider`: Strict mode templates (`set -euo pipefail`), functions, conditionals, loops, heredocs, and error trapping (`trap`).
- **Toolchain Environment Diagnostics**:
  - Probes and status reporting for `rustc`, `lua`/`luajit`, `pwsh`/`powershell`, and `bash`.
- **Expanded Test Suite**:
  - Added 11 automated unit and integration tests covering syntax rules, completions, and toolchain checks, expanding the test suite to **103 passing tests** with 0 warnings.

---

## [1.4.1] - 2026-09-10

### 🐛 Fixed & Enhanced

#### 💻 Terminal PowerShell Exit Deadlock Fix
- **Deadlock Elimination**: Fixed an application freeze that occurred when issuing the `exit` command in PowerShell. Replaced `-NoExit` startup argument with `-NoLogo` and converted stream redirect events from synchronous `Dispatcher.Invoke` to non-blocking `Dispatcher.BeginInvoke`.
- **Background Tree Termination**: Spanned process tree disposal onto background tasks with timeouts, ensuring the WPF UI message pump is never blocked during terminal exit or tab teardown.
- **Command Safety Timer & Re-entrancy Protection**: Integrated safety close timer for `exit` and `quit` commands alongside `IsClosing` flags on tab models to prevent race conditions during rapid terminal closes.

#### 🐚 Multi-Shell Catalog with Grayed-Out Unavailable Shells
- **Full Shell Catalog**: `ShellDetector` now returns the complete canonical catalog of supported shells (PowerShell 7+, Windows PowerShell, Command Prompt, Git Bash, WSL Linux, Cygwin Bash, MSYS2 Bash, plus PATH-discovered shells).
- **Unavailable Shell Dimming**: Shells not detected on disk or PATH are retained in the dropdown selector and styled with dimmed opacity, italic text, and a `(not found)` badge.

#### ⚙️ Custom Executable Configuration & Verification (`ConfigureShellDialog`)
- **Interactive Configuration Dialog**: Clicking an unavailable shell in the dropdown or clicking the new toolbar gear button (`⚙️`) opens a dedicated Dark+ modal to configure the executable path via manual entry or `OpenFileDialog`.
- **Automated Binary Verification (`ShellVerifier`)**: Validates candidate executables before accepting: checks file existence, executable format (`.exe`, `.cmd`, `.bat`), expected binary naming, and runs a timed non-interactive probe process.
- **Clear Diagnostic Feedback**: Rejects invalid binaries with detailed inline diagnostics, or accepts valid binaries, updates availability in real time, and immediately spawns the session.
- **Persistent User Settings (`ShellSettingsService`)**: Saves custom shell paths in `%APPDATA%\RecluseEdit\terminal_settings.json` so custom configurations persist across restarts.

#### 🧪 Expanded Test Suite
- Added 5 new automated tests covering shell catalog retention, availability flags, binary acceptance and rejection, settings persistence, and PowerShell clean exit, bringing the test suite to **92 passing tests** with 0 warnings.

---

## [1.4.0] - 2026-09-09

### ✨ Added Features

#### 💻 Integrated Terminal Panel & Multi-Shell Support
- **Bottom Pane Terminal**: Built-in, draggable bottom terminal panel with a horizontal splitter, dark console styling, keyboard shortcut (<kbd>Ctrl</kbd>+<kbd>`</kbd>), `View -> Toggle Terminal` menu toggle, and quick toolbar button (`💻 Terminal`).
- **Multi-Shell Auto-Detection (`ShellDetector`)**: Automatically scans system environment and PATH for installed shells:
  - PowerShell 7+ (`pwsh.exe`)
  - Windows PowerShell (`powershell.exe`)
  - Git Bash (`bash.exe` in Git directories or via `git.exe`)
  - Windows Subsystem for Linux (`wsl.exe`)
  - Command Prompt (`cmd.exe`)
  - Cygwin & MSYS2 (`bash.exe`)
- **Shell Selector & Tabbed Sessions**: Allows users to spawn multiple concurrent terminal sessions across different shells (`+` button and shell selector dropdown) with custom icons and titles (`⚡ PowerShell`, `🐚 Git Bash`, `🐧 WSL`, `>_ Command Prompt`).
- **Interactive Console & History**: Monospace console output with auto-scrolling, text selection, command prompt prefix (`PS > `, `$ `, `> `), and command history navigation (<kbd>Up</kbd>/<kbd>Down</kbd> arrows).
- **Clean Process Lifecycle**: Terminating via the `exit` command or clicking the tab close button (`✕`) cleanly terminates the entire process tree (`entireProcessTree: true`), preventing orphaned `conhost.exe` or shell processes from lingering in the background. All active terminal sessions are safely terminated upon editor exit.

#### 🤖 DeepSeek AI Assistant Enhancements
- **Rich Markdown Response Rendering (`MarkdownBlockRenderer`)**: DeepSeek assistant responses are now formatted with rich WPF visual blocks including headings (`#`, `##`, `###`), syntax-styled monospace code blocks with language headers and background borders, inline code (`` `code` ``), bold/italic formatting, blockquotes, and bullet/numbered lists instead of raw plain text.
- **Query-Lifecycle Tool Log Cleanup**: Intermediate tool execution status badges (`⚡ Executing: read_file...`, `execute_command`, etc.) display live during the query lifecycle to provide real-time feedback and are automatically cleaned up once the final response is produced, leaving only clean user prompts and formatted Markdown responses.

#### 🧪 Expanded Test Suite
- Added 9 new automated unit and integration tests covering shell detection, process lifecycle, command execution, and Markdown block parsing, expanding the test suite to **87 passing tests** with 0 warnings.

---

## [1.3.0] - 2026-09-09

### ✨ Added Features

#### 🤖 DeepSeek AI Chat Assistant Extension (`RecluseEdit.Extensions.DeepSeek`)
- **Right Pane AI Interface**: Dedicated, collapsible right pane in the main editor window with full Dark+ styling, keyboard shortcut (<kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>A</kbd>), View menu toggle, and quick toolbar button (`🤖 AI Chat`).
- **Configurable API Endpoint & Encrypted Secret Storage**: Built-in configuration drawer allowing users to customize the API Endpoint URL (defaulting to `https://api.deepseek.com/chat/completions` or local Ollama/OpenAI-compatible models) and API key/secret. All secrets are stored strictly in encrypted formats on disk (`%APPDATA%\RecluseEdit\deepseek_settings.json`) using the Windows Data Protection API (DPAPI, `CurrentUser` scope) with a machine-bound AES fallback. Plaintext keys are never written to disk, and legacy unencrypted configurations are automatically migrated to ciphertext upon loading.
- **Streaming Responses**: Live Server-Sent Events (SSE) token streaming for real-time assistant responses.
- **Autonomous Tool Calling**:
  - `read_file`: Reads text contents of workspace files for contextual understanding.
  - `write_file`: Directly creates or updates files in the project workspace with editor tab sync.
  - `list_files`: Explores and lists project directory structures.
  - `execute_command`: Executes terminal commands in the workspace.
- **Interactive Security Confirmation Dialog**: Every shell command invocation requires explicit interactive user confirmation (`[Yes]` / `[No]` prompt dialog) before any process is executed, ensuring complete user control and safety.
- **Active File Context**: One-click attachment of the currently active document's path and buffer contents into the prompt.
- **Side Panel SDK Extension (`ISidePanelProvider`)**: Extensible side panel architecture in `RecluseEdit.Sdk` and `ExtensionManager` enabling any extension to mount custom UI views into editor panels.
- **Automated Tests**: Added comprehensive unit and integration tests expanding the test suite to **78 passing tests** across 12 projects.

### 🛡️ Resilience & Bug Fixes

#### 🎨 Syntax Highlighting & Editor Error Handling
- **Graceful Highlighting Fallback**: Files with unrecognized, malformed, or failing syntax definitions now open smoothly as plain text without throwing exceptions or crashing the editor.
- **Cascading Modal Waterfall Elimination**: Implemented re-entrancy protection and rate-limiting debounce in `App.xaml.cs` to prevent infinite modal message-box storms when unhandled exceptions occur during WPF render/layout passes.
- **AvalonEdit Regex `#` Escaping**: Fixed a critical issue where AvalonEdit compiles XSHD regexes with `RegexOptions.IgnorePatternWhitespace`, treating unescaped `#` as comments and causing zero-length match infinite-loop exceptions (`InvalidOperationException: A highlighting rule matched 0 characters`). Escaped `#` across Markdown, Modern PHP, Python, Ruby, GraphQL, CSS, and Angular HTML grammars.
- **Escape Span Modernization**: Replaced zero-length `end=""` string escape spans across all core and extension grammars with standard `<Span begin="\\" end="." />` rules.
- **Syntax Test Suite**: Added dedicated automated test suites covering Markdown headings and formatting, JSON document escapes, PHP 8 attributes and hash comments, extension grammars, and graceful fallback behavior.

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

[Unreleased]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v4.0.0...HEAD
[4.0.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v3.0.0...v4.0.0
[3.0.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v2.2.0...v3.0.0
[2.2.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v2.1.0...v2.2.0
[2.1.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.5.0...v2.0.0
[1.5.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.4.1...v1.5.0
[1.4.1]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/indoctrinatedrecluse/RecluseEdit/releases/tag/v1.0.0

