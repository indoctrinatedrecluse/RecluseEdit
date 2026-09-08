# 📜 Changelog

All notable changes to **RecluseEdit** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
