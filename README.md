# 💻 RecluseEdit

> *Made with love by indoctrinatedrecluse* ❤️✨

---

## 🌟 Overview & Project Brief

**RecluseEdit** is a lightning-fast, modern desktop code editor crafted specifically for building web applications (HTML, CSS, JavaScript, TypeScript, JSON, XML, and more). Designed with developer agility and productivity in mind, it couples a high-performance text buffer with intelligent inline autocomplete, robust tab management, and an extensible architecture for plug-and-play language engines.

---

## 🎯 Key Objectives & Features

- 🗂️ **Web Workspace Explorer**: Open entire project directories (`Ctrl+Shift+O`), browse files via a collapsible tree sidebar (`Ctrl+B`), and double-click to open.
- 📑 **Multi-File Tabbed Workspace**: Seamlessly open, edit, and switch between multiple tabs. Middle-click tab to close, and right-click for tab context actions (*Close Others*, *Close to Right*, *Copy Path*, *Reveal in Explorer*).
- 🎨 **Code & Syntax Highlighting**: Comprehensive syntax coloring for web languages (HTML, CSS, JavaScript, TypeScript, JSON, XML, Markdown, C#).
- ⚡ **Dual Autocomplete System**:
  - **Inline Ghost Text**: Intelligent suggestions inline at the caret in faded italic text (<kbd>Tab</kbd> to accept, <kbd>Esc</kbd> to dismiss).
  - **IntelliSense Popup**: Rich completion list with tags, CSS properties, and JS APIs (<kbd>Ctrl+Space</kbd>).
- 🔄 **Auto-Closing Pairs & HTML Tags**: Automatic closure for `()`, `{}`, `[]`, `""`, `''` and auto-tag closing for HTML (e.g. `<div>` &rarr; `</div>`).
- 🔍 **Built-In Find & Replace Overlay**: Floating top-right search panel with Next (<kbd>Enter</kbd>), Previous (<kbd>Shift+Enter</kbd>), Match Case, and Replace All (<kbd>Ctrl+F</kbd>, <kbd>Ctrl+H</kbd>).
- 📐 **Code Folding & Bracket Matching**: Expand/collapse blocks and sections for HTML/XML and highlight matching brackets.
- 🔢 **Visual Line Numbers & Formatting**: Customizable line-number gutter, word wrapping toggle, and font scaling with <kbd>Ctrl</kbd> + <kbd>MouseWheel</kbd>.
- 🔌 **Pluggable Extension Architecture**: Dynamic plugin discovery from the `Extensions/` directory and dedicated UI manager (`Extensions -> Manage Extensions...`).
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
   - Double-click `RecluseEdit.slnx` or `RecluseEdit.csproj` to launch in **Visual Studio 2026 Enterprise**.
3. **Restore NuGet Packages**:
   - Visual Studio restores NuGet packages automatically upon solution load.
   - Alternatively, right-click the solution in **Solution Explorer** &rarr; select **Restore NuGet Packages**.
4. **Build the Solution**:
   - Select **Build &rarr; Build Solution** from the top menu, or press <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>B</kbd>.
   - Verify the Output window reports `Build succeeded: 0 Warning(s), 0 Error(s)`.
5. **Run & Test**:
   - Set active configuration to `Debug` or `Release` with target `x64` or `Any CPU`.
   - Press <kbd>F5</kbd> (Start Debugging) or <kbd>Ctrl</kbd> + <kbd>F5</kbd> (Start Without Debugging).

---

### ⌨️ Building from the Command Line (`dotnet CLI`)

You can also compile and run directly using the .NET 10 SDK:

```powershell
# Restore dependencies
dotnet restore

# Build debug configuration
dotnet build

# Launch the editor
dotnet run
```

---

## 🧩 Building Language Extensions

RecluseEdit features a decoupled extension mechanism. To write an extension:

1. Create a C# class library targeting `net10.0-windows`.
2. Reference `RecluseEdit.dll`.
3. Implement `IExtension`:
   ```csharp
   using RecluseEdit.Core.Models;
   using RecluseEdit.Extensions;

   public class MyLanguageExtension : IExtension
   {
       public string Id => "recluse.mylang";
       public string Name => "My Language Pack";
       public string Version => "1.0.0";
       public string Description => "Adds support for My Language";
       public string Author => "indoctrinatedrecluse";

       public void Initialize(IExtensionContext context)
       {
           context.RegisterLanguage(new LanguageDefinition
           {
               Id = "mylang",
               DisplayName = "My Language",
               Extensions = [".ml", ".mylang"]
           });

           context.RegisterInlineCompletion(new MyCompletionProvider());
       }

       public void Deinitialize() { }
   }
   ```
4. Place your compiled `.dll` into the `Extensions/` directory (open quickly from **Extensions &rarr; Open Extensions Folder** in the app menu).

---

## 📜 License

Copyright © 2026 **indoctrinatedrecluse**. All rights reserved.

This project is licensed under the **RecluseEdit Software License**. 
- ✅ **Free to Use**: You may freely download, install, and run this editor for personal, educational, and commercial projects.
- ✉️ **Modification & Redistribution**: Modifying, creating derivative works, or redistributing the software (in source or binary form) requires prior explicit written permission. Please contact [indoctrinatedrecluse](https://github.com/indoctrinatedrecluse/RecluseEdit) for permission requests.

See the [LICENSE](LICENSE) file for complete legal terms.

