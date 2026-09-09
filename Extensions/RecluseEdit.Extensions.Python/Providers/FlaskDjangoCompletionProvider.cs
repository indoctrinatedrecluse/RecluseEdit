using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Python.Providers;

/// <summary>
/// Provides inline completions and snippets for Flask, Django, FastAPI web frameworks,
/// and Jinja2 / Django template tags.
/// </summary>
public class FlaskDjangoCompletionProvider : IInlineCompletionProvider
{
    public string Id => "python.flask.django.completion";
    public string Name => "Flask, Django & FastAPI Completions";

    public IReadOnlyList<string> SupportedLanguages => ["python", "jinja", "html"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Flask Imports & Setup
        {
            "from flask import ",
            "Flask, request, jsonify, render_template, redirect, url_for, abort"
        },
        {
            "app = Flask(",
            "__name__)"
        },
        {
            "@app.route(",
            "\"/api/v1/resource\", methods=[\"GET\", \"POST\"])\ndef handle_resource():\n    return jsonify({\"status\": \"success\"})"
        },
        {
            "@app.get(",
            "\"/api/v1/items\")\ndef get_items():\n    return jsonify({\"items\": []})"
        },
        {
            "@app.post(",
            "\"/api/v1/items\")\ndef create_item():\n    payload = request.get_json()\n    return jsonify(payload), 201"
        },
        {
            "render_template(",
            "\"index.html\", context=context)"
        },
        {
            "jsonify(",
            "{\"status\": \"ok\", \"data\": {}})"
        },
        {
            "request.args.get(",
            "\"query\", default=\"\", type=str)"
        },

        // FastAPI Setup
        {
            "from fastapi import ",
            "FastAPI, Depends, HTTPException, status"
        },
        {
            "from pydantic import ",
            "BaseModel, Field"
        },
        {
            "app = FastAPI(",
            "title=\"FastAPI Application\", version=\"1.0.0\")"
        },

        // Django Imports & Setup
        {
            "from django.db import ",
            "models"
        },
        {
            "from django.urls import ",
            "path, include"
        },
        {
            "from django.shortcuts import ",
            "render, get_object_or_404, redirect"
        },
        {
            "from django.http import ",
            "JsonResponse, HttpResponse, Http404"
        },
        {
            "class Model(",
            "models.Model):\n    name = models.CharField(max_length=255)\n    created_at = models.DateTimeField(auto_now_add=True)\n\n    def __str__(self):\n        return self.name"
        },
        {
            "urlpatterns = [",
            "\n    path(\"\", views.index, name=\"index\"),\n]"
        },
        {
            "render(request, ",
            "\"index.html\", {\"title\": \"Home\"})"
        },
        {
            "JsonResponse(",
            "{\"status\": \"success\", \"payload\": {}})"
        },

        // Jinja & Django Template Tags
        {
            "{% block ",
            "content %}\n{% endblock %}"
        },
        {
            "{% for ",
            "item in items %}\n    {{ item }}\n{% endfor %}"
        },
        {
            "{% if ",
            "condition %}\n    \n{% endif %}"
        },
        {
            "{% extends ",
            "\"base.html\" %}"
        },
        {
            "{% include ",
            "\"components/navbar.html\" %}"
        },
        {
            "{% csrf_token",
            " %}"
        },
        {
            "{% static ",
            "\"css/main.css\" %}"
        },
        {
            "{{ form.",
            "as_p }}"
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
