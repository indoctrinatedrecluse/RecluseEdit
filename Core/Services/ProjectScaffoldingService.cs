using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Service that scaffolds new project starter structures from built-in templates.
/// </summary>
public class ProjectScaffoldingService
{
    private static readonly List<ProjectTemplate> Templates =
    [
        new()
        {
            Id = "static-web",
            Name = "Modern Static Web Starter",
            Description = "Pure HTML5, modern CSS3 variables/flexbox reset, and modular ES6 JavaScript.",
            Category = "Frontend",
            IconGlyph = "🌐",
            Tags = ["HTML5", "CSS3", "JavaScript", "Zero Config"],
            EntrypointRelativePath = "index.html"
        },
        new()
        {
            Id = "vite-react-ts",
            Name = "Vite + React 19 + TypeScript",
            Description = "Lightning-fast React 19 application with TypeScript, modern hooks, and Vite bundler.",
            Category = "Frontend",
            IconGlyph = "⚛️",
            Tags = ["React", "TypeScript", "Vite", "TSX"],
            EntrypointRelativePath = "src/App.tsx"
        },
        new()
        {
            Id = "vite-vue-ts",
            Name = "Vite + Vue 3 + TypeScript",
            Description = "Vue 3 Single File Component starter with script setup, Composition API, and Vite.",
            Category = "Frontend",
            IconGlyph = "💚",
            Tags = ["Vue 3", "SFC", "TypeScript", "Vite"],
            EntrypointRelativePath = "src/App.vue"
        },
        new()
        {
            Id = "vite-svelte-ts",
            Name = "Vite + Svelte 5 + TypeScript",
            Description = "Modern Svelte 5 starter with modern runes ($state, $derived), TS, and Vite.",
            Category = "Frontend",
            IconGlyph = "🧡",
            Tags = ["Svelte 5", "Runes", "TypeScript", "Vite"],
            EntrypointRelativePath = "src/App.svelte"
        },
        new()
        {
            Id = "vite-solid-ts",
            Name = "Vite + SolidJS + TypeScript",
            Description = "Fine-grained reactive SolidJS frontend with signals and Vite bundler.",
            Category = "Frontend",
            IconGlyph = "🔷",
            Tags = ["SolidJS", "Signals", "TypeScript", "Vite"],
            EntrypointRelativePath = "src/App.tsx"
        },
        new()
        {
            Id = "fastify-api",
            Name = "Fastify Microservice API",
            Description = "High-performance Node.js/TypeScript backend service with Fastify schema validation.",
            Category = "Backend",
            IconGlyph = "⚡",
            Tags = ["Fastify", "Node.js", "TypeScript", "REST API"],
            EntrypointRelativePath = "src/server.ts"
        },
        new()
        {
            Id = "markdown-docs",
            Name = "Markdown Documentation Site",
            Description = "Multi-page technical documentation workspace with live markdown preview wrapper.",
            Category = "Documentation",
            IconGlyph = "📚",
            Tags = ["Markdown", "Docs", "Technical Writing"],
            EntrypointRelativePath = "README.md"
        }
    ];

    public IReadOnlyList<ProjectTemplate> GetTemplates() => Templates.AsReadOnly();

    public async Task<string> ScaffoldProjectAsync(string templateId, string destinationDirectory, string projectName, bool initGit = true)
    {
        var template = Templates.FirstOrDefault(t => t.Id == templateId) ?? Templates[0];
        var projectDir = Path.Combine(destinationDirectory, projectName);
        Directory.CreateDirectory(projectDir);

        var files = GenerateTemplateFiles(template.Id, projectName);
        foreach (var (relPath, content) in files)
        {
            var fullPath = Path.Combine(projectDir, relPath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(fullPath, content);
        }

        if (initGit)
        {
            try
            {
                var psi = new ProcessStartInfo("git", "init")
                {
                    WorkingDirectory = projectDir,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                if (p != null) await p.WaitForExitAsync();
            }
            catch
            {
                // Ignore git init failure if git is not on path
            }
        }

        return projectDir;
    }

    public static Dictionary<string, string> GenerateTemplateFiles(string templateId, string projectName)
    {
        return templateId switch
        {
            "vite-react-ts" => GenerateViteReactTs(projectName),
            "vite-vue-ts" => GenerateViteVueTs(projectName),
            "vite-svelte-ts" => GenerateViteSvelteTs(projectName),
            "vite-solid-ts" => GenerateViteSolidTs(projectName),
            "fastify-api" => GenerateFastifyApi(projectName),
            "markdown-docs" => GenerateMarkdownDocs(projectName),
            _ => GenerateStaticWeb(projectName)
        };
    }

    private static Dictionary<string, string> GenerateStaticWeb(string name) => new()
    {
        ["index.html"] = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{name}</title>
          <link rel="stylesheet" href="style.css">
        </head>
        <body>
          <main class="container">
            <header>
              <div class="logo">⚡</div>
              <h1>{name}</h1>
              <p>Welcome to your modern web project crafted in RecluseEdit.</p>
            </header>
            <section class="card">
              <h2>Quick Start</h2>
              <p>Edit <code>index.html</code>, <code>style.css</code>, or <code>app.js</code> and watch the Live Preview update instantly!</p>
              <button id="counterBtn">Clicks: 0</button>
            </section>
          </main>
          <script src="app.js"></script>
        </body>
        </html>
        """,
        ["style.css"] = """
        :root {
          --bg-color: #0f172a;
          --card-bg: #1e293b;
          --text-color: #f8fafc;
          --accent-color: #38bdf8;
          --font-family: system-ui, -apple-system, sans-serif;
        }

        * {
          box-sizing: border-box;
          margin: 0;
          padding: 0;
        }

        body {
          background-color: var(--bg-color);
          color: var(--text-color);
          font-family: var(--font-family);
          min-height: 100vh;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 2rem;
        }

        .container {
          max-width: 600px;
          width: 100%;
          text-align: center;
        }

        .logo {
          font-size: 3.5rem;
          margin-bottom: 1rem;
        }

        h1 {
          font-size: 2.25rem;
          color: var(--accent-color);
          margin-bottom: 0.5rem;
        }

        .card {
          margin-top: 2rem;
          padding: 2rem;
          background: var(--card-bg);
          border-radius: 12px;
          box-shadow: 0 10px 25px rgba(0,0,0,0.3);
          border: 1px solid rgba(255,255,255,0.1);
        }

        .card code {
          background: rgba(255,255,255,0.1);
          padding: 0.2rem 0.4rem;
          border-radius: 4px;
          color: var(--accent-color);
        }

        button {
          margin-top: 1.5rem;
          padding: 0.75rem 1.5rem;
          background: var(--accent-color);
          color: #0f172a;
          border: none;
          border-radius: 8px;
          font-weight: 600;
          cursor: pointer;
          transition: transform 0.1s ease, opacity 0.2s ease;
        }

        button:hover {
          opacity: 0.9;
          transform: translateY(-2px);
        }
        """,
        ["app.js"] = """
        console.log("App initialized via RecluseEdit Live Preview!");

        let count = 0;
        const btn = document.getElementById('counterBtn');

        btn?.addEventListener('click', () => {
          count++;
          btn.textContent = `Clicks: ${count}`;
          console.log(`Button clicked! New count: ${count}`);
        });
        """,
        [".gitignore"] = "node_modules/\n.DS_Store\nThumbs.db\n",
        ["README.md"] = $"""
        # {name}

        Modern static web application scaffolded with **RecluseEdit**.

        ## Development
        - Open `index.html` in RecluseEdit.
        - Press `Ctrl+Shift+V` to launch the **Live Web Preview**.
        """
    };

    private static Dictionary<string, string> GenerateViteReactTs(string name) => new()
    {
        ["package.json"] = $$"""
        {
          "name": "{{name}}",
          "private": true,
          "version": "0.1.0",
          "type": "module",
          "scripts": {
            "dev": "vite",
            "build": "tsc -b && vite build",
            "preview": "vite preview"
          },
          "dependencies": {
            "react": "^19.0.0",
            "react-dom": "^19.0.0"
          },
          "devDependencies": {
            "@types/react": "^19.0.0",
            "@types/react-dom": "^19.0.0",
            "@vitejs/plugin-react": "^4.3.0",
            "typescript": "^5.7.0",
            "vite": "^6.0.0"
          }
        }
        """,
        ["vite.config.ts"] = """
        import { defineConfig } from 'vite';
        import react from '@vitejs/plugin-react';

        export default defineConfig({
          plugins: [react()],
          server: { port: 3000 }
        });
        """,
        ["index.html"] = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{name}</title>
        </head>
        <body>
          <div id="root"></div>
          <script type="module" src="/src/main.tsx"></script>
        </body>
        </html>
        """,
        ["src/main.tsx"] = """
        import React from 'react';
        import ReactDOM from 'react-dom/client';
        import { App } from './App';
        import './index.css';

        ReactDOM.createRoot(document.getElementById('root')!).render(
          <React.StrictMode>
            <App />
          </React.StrictMode>
        );
        """,
        ["src/App.tsx"] = """
        import { useState } from 'react';

        export function App() {
          const [count, setCount] = useState(0);

          return (
            <main className="container">
              <header>
                <div className="logo">⚛️</div>
                <h1>{name}</h1>
                <p>React 19 + TypeScript running with Vite</p>
              </header>
              <div className="card">
                <button onClick={() => setCount(c => c + 1)}>Count is {count}</button>
              </div>
            </main>
          );
        }
        """.Replace("{name}", name),
        ["src/index.css"] = "body { margin: 0; background: #121212; color: #fff; font-family: sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; }\n.container { text-align: center; }\n.logo { font-size: 3rem; margin-bottom: 1rem; }\nbutton { padding: 0.75rem 1.5rem; background: #61dafb; color: #000; font-weight: bold; border: none; border-radius: 8px; cursor: pointer; }",
        ["tsconfig.json"] = "{\n  \"compilerOptions\": {\n    \"target\": \"ES2022\",\n    \"module\": \"ESNext\",\n    \"moduleResolution\": \"bundler\",\n    \"jsx\": \"react-jsx\",\n    \"strict\": true\n  }\n}",
        [".gitignore"] = "node_modules/\ndist/\n"
    };

    private static Dictionary<string, string> GenerateViteVueTs(string name) => new()
    {
        ["package.json"] = $$"""
        {
          "name": "{{name}}",
          "private": true,
          "version": "0.1.0",
          "type": "module",
          "scripts": {
            "dev": "vite",
            "build": "vue-tsc -b && vite build",
            "preview": "vite preview"
          },
          "dependencies": {
            "vue": "^3.5.0"
          },
          "devDependencies": {
            "@vitejs/plugin-vue": "^5.1.0",
            "typescript": "^5.7.0",
            "vite": "^6.0.0",
            "vue-tsc": "^2.1.0"
          }
        }
        """,
        ["vite.config.ts"] = """
        import { defineConfig } from 'vite';
        import vue from '@vitejs/plugin-vue';

        export default defineConfig({
          plugins: [vue()]
        });
        """,
        ["index.html"] = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{name}</title>
        </head>
        <body>
          <div id="app"></div>
          <script type="module" src="/src/main.ts"></script>
        </body>
        </html>
        """,
        ["src/main.ts"] = """
        import { createApp } from 'vue';
        import App from './App.vue';

        createApp(App).mount('#app');
        """,
        ["src/App.vue"] = """
        <script setup lang="ts">
        import { ref } from 'vue';

        const count = ref(0);
        </script>

        <template>
          <main class="container">
            <div class="logo">💚</div>
            <h1>{name}</h1>
            <p>Vue 3 SFC + Composition API</p>
            <button @click="count++">Count: {{ count }}</button>
          </main>
        </template>

        <style scoped>
        .container {
          text-align: center;
          font-family: system-ui, sans-serif;
          color: #eee;
          background: #181818;
          min-height: 100vh;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
        }
        .logo { font-size: 3rem; margin-bottom: 1rem; }
        button {
          margin-top: 1rem;
          padding: 0.6rem 1.2rem;
          background: #42b883;
          color: #fff;
          border: none;
          border-radius: 6px;
          cursor: pointer;
        }
        </style>
        """.Replace("{name}", name),
        ["tsconfig.json"] = "{\n  \"compilerOptions\": {\n    \"target\": \"ES2022\",\n    \"module\": \"ESNext\",\n    \"moduleResolution\": \"bundler\",\n    \"strict\": true\n  }\n}",
        [".gitignore"] = "node_modules/\ndist/\n"
    };

    private static Dictionary<string, string> GenerateViteSvelteTs(string name) => new()
    {
        ["package.json"] = $$"""
        {
          "name": "{{name}}",
          "private": true,
          "version": "0.1.0",
          "type": "module",
          "scripts": {
            "dev": "vite",
            "build": "vite build"
          },
          "devDependencies": {
            "@sveltejs/vite-plugin-svelte": "^4.0.0",
            "svelte": "^5.0.0",
            "typescript": "^5.7.0",
            "vite": "^6.0.0"
          }
        }
        """,
        ["vite.config.ts"] = """
        import { defineConfig } from 'vite';
        import { svelte } from '@sveltejs/vite-plugin-svelte';

        export default defineConfig({
          plugins: [svelte()]
        });
        """,
        ["index.html"] = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{name}</title>
        </head>
        <body>
          <div id="app"></div>
          <script type="module" src="/src/main.ts"></script>
        </body>
        </html>
        """,
        ["src/main.ts"] = """
        import { mount } from 'svelte';
        import App from './App.svelte';

        const app = mount(App, {
          target: document.getElementById('app')!
        });

        export default app;
        """,
        ["src/App.svelte"] = """
        <script lang="ts">
          let count = $state(0);
          let doubled = $derived(count * 2);
        </script>

        <main class="container">
          <div class="logo">🧡</div>
          <h1>{name}</h1>
          <p>Svelte 5 with Runes ($state, $derived)</p>
          <button onclick={() => count++}>Count: {count} (Doubled: {doubled})</button>
        </main>

        <style>
          :global(body) { margin: 0; background: #1a1a1a; color: #fff; font-family: sans-serif; }
          .container { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 100vh; }
          .logo { font-size: 3rem; }
          button { padding: 0.75rem 1.5rem; background: #ff3e00; color: white; border: none; border-radius: 8px; cursor: pointer; }
        </style>
        """.Replace("{name}", name),
        [".gitignore"] = "node_modules/\ndist/\n"
    };

    private static Dictionary<string, string> GenerateViteSolidTs(string name) => new()
    {
        ["package.json"] = $$"""
        {
          "name": "{{name}}",
          "private": true,
          "version": "0.1.0",
          "type": "module",
          "scripts": {
            "dev": "vite",
            "build": "vite build"
          },
          "dependencies": {
            "solid-js": "^1.9.0"
          },
          "devDependencies": {
            "typescript": "^5.7.0",
            "vite": "^6.0.0",
            "vite-plugin-solid": "^2.11.0"
          }
        }
        """,
        ["vite.config.ts"] = """
        import { defineConfig } from 'vite';
        import solidPlugin from 'vite-plugin-solid';

        export default defineConfig({
          plugins: [solidPlugin()]
        });
        """,
        ["index.html"] = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{name}</title>
        </head>
        <body>
          <div id="root"></div>
          <script type="module" src="/src/index.tsx"></script>
        </body>
        </html>
        """,
        ["src/index.tsx"] = """
        import { render } from 'solid-js/web';
        import { App } from './App';

        render(() => <App />, document.getElementById('root')!);
        """,
        ["src/App.tsx"] = """
        import { createSignal } from 'solid-js';

        export function App() {
          const [count, setCount] = createSignal(0);

          return (
            <main style="text-align: center; padding-top: 20vh; font-family: sans-serif; color: #fff; background: #141414; min-height: 100vh;">
              <div style="font-size: 3rem;">🔷</div>
              <h1>{name}</h1>
              <p>SolidJS with fine-grained reactivity</p>
              <button onClick={() => setCount(c => c + 1)} style="padding: 0.75rem 1.5rem; background: #446b9e; color: #fff; border: none; border-radius: 8px; cursor: pointer;">
                Clicks: {count()}
              </button>
            </main>
          );
        }
        """.Replace("{name}", name),
        [".gitignore"] = "node_modules/\ndist/\n"
    };

    private static Dictionary<string, string> GenerateFastifyApi(string name) => new()
    {
        ["package.json"] = $$"""
        {
          "name": "{{name}}",
          "version": "1.0.0",
          "description": "Fastify Microservice API created with RecluseEdit",
          "main": "dist/server.js",
          "scripts": {
            "build": "tsc",
            "start": "node dist/server.js",
            "dev": "tsx watch src/server.ts"
          },
          "dependencies": {
            "fastify": "^5.0.0"
          },
          "devDependencies": {
            "@types/node": "^22.0.0",
            "tsx": "^4.19.0",
            "typescript": "^5.7.0"
          }
        }
        """,
        ["src/server.ts"] = """
        import Fastify from 'fastify';
        import { itemRoutes } from './routes/items';

        const server = Fastify({ logger: true });

        server.register(itemRoutes, { prefix: '/api' });

        server.get('/', async () => {
          return { status: 'ok', service: 'Fastify Microservice' };
        });

        const start = async () => {
          try {
            await server.listen({ port: 3000, host: '0.0.0.0' });
            console.log('Server listening on http://localhost:3000');
          } catch (err) {
            server.log.error(err);
            process.exit(1);
          }
        };

        start();
        """,
        ["src/routes/items.ts"] = """
        import { FastifyInstance } from 'fastify';

        interface Item {
          id: string;
          name: string;
        }

        const items: Item[] = [
          { id: '1', name: 'Item Alpha' },
          { id: '2', name: 'Item Beta' }
        ];

        export async function itemRoutes(fastify: FastifyInstance) {
          fastify.get('/items', async () => {
            return { data: items };
          });

          fastify.post('/items', async (request, reply) => {
            const body = request.body as { name: string };
            const newItem: Item = { id: String(items.length + 1), name: body.name || 'New Item' };
            items.push(newItem);
            reply.status(201).send(newItem);
          });
        }
        """,
        ["tsconfig.json"] = "{\n  \"compilerOptions\": {\n    \"target\": \"ES2022\",\n    \"module\": \"NodeNext\",\n    \"moduleResolution\": \"NodeNext\",\n    \"outDir\": \"dist\",\n    \"rootDir\": \"src\",\n    \"strict\": true\n  }\n}",
        [".gitignore"] = "node_modules/\ndist/\n"
    };

    private static Dictionary<string, string> GenerateMarkdownDocs(string name) => new()
    {
        ["README.md"] = $"""
        # {name} Documentation

        Welcome to the documentation for **{name}**.

        ## 📖 Contents
        1. [Getting Started](docs/getting-started.md)
        2. [Architecture Overview](docs/architecture.md)
        3. [API Reference](docs/api-reference.md)

        ---
        *Generated with RecluseEdit.*
        """,
        ["docs/getting-started.md"] = """
        # Getting Started

        Follow these quick instructions to begin working with the project.

        ## Prerequisites
        - Node.js 20+
        - Git

        ## Installation
        ```bash
        git clone <repo-url>
        cd project
        npm install
        ```
        """,
        ["docs/architecture.md"] = """
        # Architecture Overview

        This document details the components and data flow of the application.

        ```
        Client (Browser) <---> API Gateway <---> Services <---> Database
        ```
        """,
        ["docs/api-reference.md"] = """
        # API Reference

        ### `GET /api/v1/health`
        Returns the system operational health metrics.

        **Response:**
        ```json
        {
          "status": "healthy",
          "uptime": 3600
        }
        ```
        """
    };
}
