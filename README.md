# 💻 RecluseEdit

> *Made with love by indoctrinatedrecluse* ❤️✨

---

## 🌟 Overview & Project Brief

**RecluseEdit** is a lightning-fast, modern desktop code editor crafted specifically for building web applications (HTML, CSS, JavaScript, TypeScript, JSON, XML, and more). Designed with developer agility and productivity in mind, it couples a high-performance text buffer with intelligent inline autocomplete, robust tab management, and an extensible architecture for plug-and-play language engines.

---

## 🎯 Key Objectives

- 📑 **Multi-File Tabbed Workspace**: Seamlessly open, edit, and switch between multiple files with real-time dirty-state tracking (`*`) and safe unsaved-change guards.
- 🎨 **Code & Syntax Highlighting**: Comprehensive syntax coloring for web languages (HTML, CSS, JavaScript, TypeScript, JSON, XML, Markdown, C#).
- 🔢 **Visual Line Numbers & Formatting**: Customizable line-number gutter and word wrapping for comfortable reading and editing.
- ⚡ **Inline Autocomplete (Ghost Text)**: Instant inline suggestions displayed directly at the caret in faded italic text—press <kbd>Tab</kbd> to accept or <kbd>Esc</kbd> to dismiss.
- 🔌 **Pluggable Extension Architecture**: Modular plugin system (`IExtension`, `IExtensionContext`, `IInlineCompletionProvider`) allowing external language packs, custom grammars, and completions to be loaded dynamically from the `Extensions/` directory.
- 🌙 **Modern Dark UI**: VS Code-inspired sleek dark theme (`#1E1E1E`), complete with menu bar, quick-action toolbar, and informative status bar.

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

Created by **indoctrinatedrecluse**. All rights reserved.
