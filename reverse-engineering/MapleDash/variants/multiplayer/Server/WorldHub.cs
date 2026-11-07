using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MapleDash.Multiplayer;

/// <summary>
/// Executable independent networking prototype. It does not yet route MapleDash's
/// original 50+ ITransport calls or load Unity map collision/resource data.
/// </summary>
public sealed class WorldHub
{
    private sealed class Player
    {
        public Player(string name, WebSocket socket) { Name = name; Socket = socket; }
        public string Name { get; }
        public WebSocket Socket { get; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public long LastSequence { get; private set; }
        public DateTimeOffset LastMove { get; private set; } = DateTimeOffset.MinValue;
        public DateTimeOffset LastChat { get; private set; } = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim _send = new(1, 1);
        private readonly object _state = new();
        public object Snapshot { get { lock (_state) return new { name = Name, x = X, y = Y }; } }

        public bool TryMove(int x, int y, long seq, out string error)
        {
            lock (_state)
            {
                if (seq <= LastSequence) { error = "old_sequence"; return false; }
                if (x < -10000 || x > 10000 || y < -10000 || y > 10000)
                { error = "out_of_bounds"; return false; }
                if (Math.Abs((long)x - X) > 25 || Math.Abs((long)y - Y) > 25)
                { error = "move_too_far"; return false; }
                if (DateTimeOffset.UtcNow - LastMove < TimeSpan.FromMilliseconds(30))
                { error = "move_too_fast"; return false; }
                X = x; Y = y; LastSequence = seq; LastMove = DateTimeOffset.UtcNow;
                error = "";
                return true;
            }
        }

        public bool TryChat()
        {
            lock (_state)
            {
                if (DateTimeOffset.UtcNow - LastChat < TimeSpan.FromMilliseconds(350)) return false;
                LastChat = DateTimeOffset.UtcNow;
                return true;
            }
        }

        public async Task SendAsync(object message, CancellationToken ct)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(message);
            await _send.WaitAsync(ct);
            try
            {
                if (Socket.State == WebSocketState.Open)
                    await Socket.SendAsync(new ArraySegment<byte>(json), WebSocketMessageType.Text, true, ct);
            }
            finally { _send.Release(); }
        }
    }

    private readonly ConcurrentDictionary<string, Player> _players = new(StringComparer.OrdinalIgnoreCase);
    public int PlayerCount => _players.Count;

    public async Task HandleAsync(string authenticatedName, WebSocket socket, CancellationToken ct)
    {
        var player = new Player(authenticatedName, socket);
        if (!_players.TryAdd(authenticatedName, player))
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "already_connected", CancellationToken.None);
            return;
        }
        try
        {
            await player.SendAsync(new { type = "welcome", protocol = 1, name = authenticatedName }, ct);
            await player.SendAsync(new { type = "snapshot", players = _players.Values.Select(x => x.Snapshot).ToArray() }, ct);
            await BroadcastAsync(new { type = "presence", action = "joined", name = authenticatedName },
                                 except: authenticatedName, ct: ct);
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                string? payload = await ReadMessageAsync(socket, ct);
                if (payload is null) break;
                try
                {
                    using var doc = JsonDocument.Parse(payload);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("type", out var typeElement) ||
                        typeElement.ValueKind != JsonValueKind.String)
                    { await player.SendAsync(new { type = "error", code = "missing_type" }, ct); continue; }
                    string type = typeElement.GetString()!;
                    if (type == "ping")
                    {
                        await player.SendAsync(new { type = "pong" }, ct);
                    }
                    else if (type == "move")
                    {
                        if (!GetInt32(root, "x", out int x) || !GetInt32(root, "y", out int y) ||
                            !GetInt64(root, "seq", out long seq))
                        { await player.SendAsync(new { type = "error", code = "invalid_move" }, ct); continue; }
                        if (!player.TryMove(x, y, seq, out var reason))
                        { await player.SendAsync(new { type = "error", code = reason }, ct); continue; }
                        // "name" is always derived from the authenticated WebSocket identity.
                        await BroadcastAsync(new { type = "move", name = player.Name, x, y, seq }, except: null, ct: ct);
                    }
                    else if (type == "chat")
                    {
                        if (!root.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String ||
                            string.IsNullOrWhiteSpace(text.GetString()) || text.GetString()!.Length > 200 ||
                            text.GetString()!.Any(char.IsControl) || !player.TryChat())
                        { await player.SendAsync(new { type = "error", code = "invalid_or_rate_limited_chat" }, ct); continue; }
                        await BroadcastAsync(new { type = "chat", name = player.Name, text = text.GetString() }, except: null, ct: ct);
                    }
                    else await player.SendAsync(new { type = "error", code = "unknown_type" }, ct);
                }
                catch (JsonException)
                { await player.SendAsync(new { type = "error", code = "bad_json" }, ct); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (WebSocketException) { }
        finally
        {
            if (((ICollection<KeyValuePair<string, Player>>)_players).Remove(new KeyValuePair<string, Player>(authenticatedName, player)))
                await BroadcastAsync(new { type = "presence", action = "left", name = authenticatedName },
                                     except: authenticatedName, ct: CancellationToken.None);
        }
    }

    private async Task BroadcastAsync(object data, string? except, CancellationToken ct)
    {
        foreach (Player target in _players.Values)
        {
            if (except != null && target.Name.Equals(except, StringComparison.OrdinalIgnoreCase)) continue;
            try { await target.SendAsync(data, ct); }
            catch (WebSocketException) { /* disconnected peer will be cleaned up */ }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }

    private static bool GetInt32(JsonElement obj, string prop, out int val)
    {
        val = 0;
        return obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out val);
    }
    private static bool GetInt64(JsonElement obj, string prop, out long val)
    {
        val = 0;
        return obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out val);
    }

    private static async Task<string?> ReadMessageAsync(WebSocket socket, CancellationToken ct)
    {
        byte[] buffer = new byte[4096];
        using var content = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (socket.State == WebSocketState.CloseReceived)
                    await socket.CloseOutputAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                                  result.CloseStatusDescription, CancellationToken.None);
                return null;
            }
            if (result.MessageType != WebSocketMessageType.Text || content.Length + result.Count > 4096)
            {
                await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message_too_large", CancellationToken.None);
                return null;
            }
            content.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        return new UTF8Encoding(false, true).GetString(content.ToArray());
    }
}