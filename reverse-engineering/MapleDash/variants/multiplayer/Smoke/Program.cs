using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

static void Require(bool ok, string reason)
{
    if (!ok) throw new InvalidOperationException(reason);
    Console.WriteLine("PASS " + reason);
}

static async Task<JsonDocument> ReadTypeAsync(ClientWebSocket socket, string wanted, CancellationToken ct)
{
    for (int attempts = 0; attempts < 10; attempts++)
    {
        using var output = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            byte[] b = new byte[4096];
            result = await socket.ReceiveAsync(new ArraySegment<byte>(b), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new Exception("Unexpected close while waiting for " + wanted);
            output.Write(b, 0, result.Count);
        } while (!result.EndOfMessage);
        var doc = JsonDocument.Parse(output.ToArray());
        if (doc.RootElement.GetProperty("type").GetString() == wanted) return doc;
        doc.Dispose();
    }
    throw new Exception("Did not receive: " + wanted);
}

static async Task SendAsync(ClientWebSocket socket, object message, CancellationToken ct)
{
    byte[] data = JsonSerializer.SerializeToUtf8Bytes(message);
    await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, ct);
}

var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;
listener.Stop();
string baseUrl = $"http://127.0.0.1:{port}";
string tmp = Path.Combine(Path.GetTempPath(), "mapledash-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tmp);
var serverProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server/MapleDash.Multiplayer.Server.csproj"));
Require(File.Exists(serverProject), "server project located");
var psi = new ProcessStartInfo("dotnet") { UseShellExecute = false };
psi.ArgumentList.Add("run"); psi.ArgumentList.Add("--no-launch-profile");
psi.ArgumentList.Add("--project"); psi.ArgumentList.Add(serverProject);
psi.ArgumentList.Add("--configuration"); psi.ArgumentList.Add("Release");
psi.Environment["MAPLEDASH_BIND_URL"] = baseUrl;
psi.Environment["MAPLEDASH_DATA_DIR"] = tmp;
psi.Environment["MAPLEDASH_REGISTRATION_INVITE"] = "a-long-random-development-invite-test";
using var process = Process.Start(psi) ?? throw new Exception("could not start server");
try
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    bool ready = false;
    for (int i = 0; i < 160; i++)
    {
        if (process.HasExited) throw new Exception("server exited unexpectedly: " + process.ExitCode);
        try { using var h = await client.GetAsync("/health", cts.Token); if (h.IsSuccessStatusCode) { ready = true; break; } }
        catch (HttpRequestException) { }
        await Task.Delay(250, cts.Token);
    }
    Require(ready, "loopback server health");
    using var inv = new HttpRequestMessage(HttpMethod.Post, "/api/register")
    { Content = JsonContent.Create(new { name = "test_alice", password = "SafeAlicePass12345" }) };
    inv.Headers.Add("X-Registration-Invite", "wrong-invite");
    using var denied = await client.SendAsync(inv, cts.Token);
    Require(denied.StatusCode == HttpStatusCode.Forbidden, "wrong invite denied");

    async Task Register(string name, string password)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/register")
        { Content = JsonContent.Create(new { name, password }) };
        req.Headers.Add("X-Registration-Invite", "a-long-random-development-invite-test");
        using var resp = await client.SendAsync(req, cts.Token);
        Require(resp.IsSuccessStatusCode, "registered " + name);
    }
    await Register("test_alice", "SafeAlicePass12345");
    await Register("test_bob", "SafeBobbyPass12345");
    using var wrong = await client.PostAsJsonAsync("/api/login", new { name = "test_alice", password = "WrongAlicePass123" }, cts.Token);
    Require(wrong.StatusCode == HttpStatusCode.Unauthorized, "wrong password rejected");

    async Task<string> Login(string name, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/login", new { name, password }, cts.Token);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
        string token = body.RootElement.GetProperty("token").GetString()!;
        Require(token.Length == 64, "issued random authenticated session for " + name);
        return token;
    }
    string alice = await Login("test_alice", "SafeAlicePass12345");
    string bob = await Login("test_bob", "SafeBobbyPass12345");
    using var unauthorized = new ClientWebSocket();
    try { await unauthorized.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/ws"), cts.Token); throw new Exception("expected unauthorized websocket rejection"); }
    catch (WebSocketException) { Require(true, "WebSocket without identity denied"); }

    async Task<ClientWebSocket> Connect(string token)
    {
        var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Authorization", "Bearer " + token);
        await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/ws"), cts.Token);
        using var welcome = await ReadTypeAsync(ws, "welcome", cts.Token);
        using var snapshot = await ReadTypeAsync(ws, "snapshot", cts.Token);
        Require(snapshot.RootElement.GetProperty("players").GetArrayLength() >= 1, "received world snapshot");
        return ws;
    }

    using var wsAlice = await Connect(alice);
    using var wsBob = await Connect(bob);
    await SendAsync(wsAlice, new { type = "move", name = "test_bob", x = 20, y = 10, seq = 1 }, cts.Token);
    using var move = await ReadTypeAsync(wsBob, "move", cts.Token);
    Require(move.RootElement.GetProperty("name").GetString() == "test_alice", "movement identity bound to authenticated token");
    Require(move.RootElement.GetProperty("x").GetInt32() == 20, "movement synchronized to second client");
    await SendAsync(wsAlice, new { type = "move", x = 300, y = 100, seq = 2 }, cts.Token);
    using var moveError = await ReadTypeAsync(wsAlice, "error", cts.Token);
    Require(moveError.RootElement.GetProperty("code").GetString() == "move_too_far", "teleport-like move rejected");
    await SendAsync(wsAlice, new { type = "chat", name = "test_bob", text = "hello from alice" }, cts.Token);
    using var chat = await ReadTypeAsync(wsBob, "chat", cts.Token);
    Require(chat.RootElement.GetProperty("name").GetString() == "test_alice", "chat identity bound to authenticated token");
    Require(chat.RootElement.GetProperty("text").GetString() == "hello from alice", "chat synchronized to second client");
    await wsAlice.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
    await wsBob.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
    Console.WriteLine("ALL MULTIPLAYER NETWORK SMOKE TESTS PASSED");
}
finally
{
    if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
    try { Directory.Delete(tmp, recursive: true); } catch (IOException) { }
}