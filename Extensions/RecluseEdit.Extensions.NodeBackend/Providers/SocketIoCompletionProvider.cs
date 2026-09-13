using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.NodeBackend.Providers;

/// <summary>
/// Provides inline completions for Socket.io WebSocket server events,
/// connections, room management, and broadcasts.
/// </summary>
public class SocketIoCompletionProvider : IInlineCompletionProvider
{
    public string Id => "nodebackend.socketio.inline";
    public string Name => "Socket.io WebSocket Server & Events";

    public IReadOnlyList<string> SupportedLanguages => ["javascript", "typescript", "js", "ts"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "const io = new Server(",
            "httpServer, {\n  cors: {\n    origin: '*'\n  }\n});"
        },
        {
            "io.on('connection', (",
            "socket) => {\n  console.log('Client connected:', socket.id);\n\n  socket.on('disconnect', () => {\n    console.log('Client disconnected:', socket.id);\n  });\n});"
        },
        {
            "socket.on('message', (",
            "data) => {\n  io.emit('broadcast', data);\n});"
        },
        {
            "socket.join(",
            "roomId);\nio.to(roomId).emit('notification', { msg: 'User joined' });"
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
            if (prefix.EndsWith(trigger, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

