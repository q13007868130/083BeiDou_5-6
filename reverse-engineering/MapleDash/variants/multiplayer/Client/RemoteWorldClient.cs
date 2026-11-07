using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MapleDash.Multiplayer.Client
{
    /// <summary>Protocol-v1 world-events client for MapleDash's experimental network server.
    /// This is a sidecar connection; it does NOT implement MapleDash's synchronous ITransport.</summary>
    public sealed class RemoteWorldClient : IDisposable
    {
        private readonly HttpClient http;
        private readonly Uri baseUri;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private ClientWebSocket? socket;
        private string? token;
        private long sequence;
        private bool disposed;

        public string? AccountName { get; private set; }
        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        public RemoteWorldClient(Uri baseUri)
        {
            if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
            if (!baseUri.IsAbsoluteUri || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException("An absolute http(s) base URL is required", nameof(baseUri));
            if (baseUri.AbsolutePath != "/" || !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment))
                throw new ArgumentException("Use a server origin URL, without an API path", nameof(baseUri));
            // Development HTTP is explicitly limited to local loopback; remote endpoints must use TLS.
            if (baseUri.Scheme == Uri.UriSchemeHttp && !IsLoopback(baseUri.Host))
                throw new ArgumentException("Non-local HTTP is forbidden. Use HTTPS/WSS.", nameof(baseUri));
            this.baseUri = baseUri;
            http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(15) };
        }

        private static bool IsLoopback(string host) =>
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            (IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address));

        private void EnsureActive()
        {
            if (disposed) throw new ObjectDisposedException(nameof(RemoteWorldClient));
        }

        public async Task RegisterAsync(string name, string password, string invite, CancellationToken ct = default)
        {
            EnsureActive();
            if (string.IsNullOrWhiteSpace(invite)) throw new ArgumentException("Registration invite required", nameof(invite));
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/register"))
            {
                request.Content = JsonBody(new { name, password });
                request.Headers.Add("X-Registration-Invite", invite);
                using (var response = await http.SendAsync(request, ct).ConfigureAwait(false))
                {
                    // Deliberately never echo password/token/invite or upstream body in error messages.
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException("Registration rejected (HTTP " + (int)response.StatusCode + ")");
                }
            }
        }

        public async Task LoginAsync(string name, string password, CancellationToken ct = default)
        {
            EnsureActive();
            if (IsConnected) throw new InvalidOperationException("Disconnect before logging into another account");
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/login"))
            {
                request.Content = JsonBody(new { name, password });
                using (var response = await http.SendAsync(request, ct).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException("Login rejected (HTTP " + (int)response.StatusCode + ")");
                    var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using (var json = JsonDocument.Parse(payload))
                    {
                        string? newToken = json.RootElement.GetProperty("token").GetString();
                        string? actualName = json.RootElement.GetProperty("name").GetString();
                        if (string.IsNullOrWhiteSpace(actualName) || newToken == null || newToken.Length != 64)
                            throw new InvalidDataException("Unexpected authentication response");
                        token = newToken;
                        AccountName = actualName;
                        Interlocked.Exchange(ref sequence, 0);
                    }
                }
            }
        }

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            EnsureActive();
            if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("Login first");
            if (socket != null) throw new InvalidOperationException("Close existing socket before reconnecting");
            var scheme = baseUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";
            var builder = new UriBuilder(baseUri) { Scheme = scheme, Path = "/ws" };
            var next = new ClientWebSocket();
            next.Options.SetRequestHeader("Authorization", "Bearer " + token);
            try
            {
                await next.ConnectAsync(builder.Uri, ct).ConfigureAwait(false);
                socket = next;
            }
            catch
            {
                next.Dispose();
                throw;
            }
        }

        public Task MoveAsync(int x, int y, CancellationToken ct = default)
        {
            // Player identity is intentionally absent: the server derives it from the bearer session.
            long nextSequence = Interlocked.Increment(ref sequence);
            return SendAsync(new { type = "move", x, y, seq = nextSequence }, ct);
        }

        public Task ChatAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 200 || text.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new ArgumentException("Invalid chat message", nameof(text));
            return SendAsync(new { type = "chat", text }, ct);
        }

        public Task PingAsync(CancellationToken ct = default) => SendAsync(new { type = "ping" }, ct);

        private async Task SendAsync(object value, CancellationToken ct)
        {
            EnsureActive();
            var active = socket;
            if (active == null || active.State != WebSocketState.Open)
                throw new InvalidOperationException("Not connected");
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await active.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct)
                            .ConfigureAwait(false);
            }
            finally { sendLock.Release(); }
        }

        /// <returns>A cloned JSON payload (valid after the internal JsonDocument is disposed), or null on peer close.</returns>
        public async Task<WorldEvent?> ReceiveAsync(CancellationToken ct = default)
        {
            EnsureActive();
            var active = socket;
            if (active == null) throw new InvalidOperationException("Not connected");
            byte[] buffer = new byte[4096];
            using (var output = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await active.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (active.State == WebSocketState.CloseReceived)
                            await active.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "client_ack", CancellationToken.None).ConfigureAwait(false);
                        return null;
                    }
                    if (result.MessageType != WebSocketMessageType.Text || output.Length + result.Count > 256 * 1024)
                        throw new InvalidDataException("Unexpected or oversized server event");
                    output.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                using (var json = JsonDocument.Parse(output.ToArray()))
                {
                    var type = json.RootElement.GetProperty("type").GetString() ?? "unknown";
                    return new WorldEvent(type, json.RootElement.Clone());
                }
            }
        }

        public async Task DisconnectAsync(CancellationToken ct = default)
        {
            EnsureActive();
            var old = socket;
            socket = null;
            if (old == null) return;
            try
            {
                if (old.State == WebSocketState.Open)
                    await old.CloseAsync(WebSocketCloseStatus.NormalClosure, "client_logout", ct).ConfigureAwait(false);
            }
            catch (WebSocketException) { /* peer disconnected */ }
            finally { old.Dispose(); }
        }

        public void Logout()
        {
            EnsureActive();
            if (socket != null) throw new InvalidOperationException("Disconnect before logging out");
            token = null;
            AccountName = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            socket?.Dispose();
            socket = null;
            token = null;
            AccountName = null;
            http.Dispose();
            sendLock.Dispose();
        }

        private static StringContent JsonBody(object body) =>
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    }

    public sealed class WorldEvent
    {
        public WorldEvent(string type, JsonElement payload) { Type = type; Payload = payload; }
        public string Type { get; }
        public JsonElement Payload { get; }
    }
}
