using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MapleDash.Multiplayer.Client;

static void Check(bool ok, string label)
{
    if (!ok) throw new Exception("FAIL " + label);
    Console.WriteLine("PASS " + label);
}

static async Task<WorldEvent> Expect(RemoteWorldClient client, string type, CancellationToken ct)
{
    for (int i = 0; i < 15; ++i)
    {
        var message = await client.ReceiveAsync(ct);
        if (message == null) throw new Exception("Remote closed before " + type);
        if (message.Type == type) return message;
    }
    throw new Exception("Event missing: " + type);
}

try
{
    using var forbidden = new RemoteWorldClient(new Uri("http://example.net:5088"));
    throw new Exception("Expected HTTP nonloopback to be rejected");
}
catch (ArgumentException) { Check(true, "remote non-TLS URL rejected"); }

var portProbe = new TcpListener(IPAddress.Loopback, 0);
portProbe.Start();
int port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
portProbe.Stop();
string origin = $"http://127.0.0.1:{port}";
string work = Path.Combine(Path.GetTempPath(), "mapledash-sdk-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(work);
string serverProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server/MapleDash.Multiplayer.Server.csproj"));
Check(File.Exists(serverProject), "server project found");
var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
start.ArgumentList.Add("run"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(serverProject);
start.ArgumentList.Add("--configuration"); start.ArgumentList.Add("Release");
start.ArgumentList.Add("--no-launch-profile");
start.Environment["MAPLEDASH_BIND_URL"] = origin;
start.Environment["MAPLEDASH_DATA_DIR"] = work;
start.Environment["MAPLEDASH_REGISTRATION_INVITE"] = "sdk-test-local-long-invite-secret";
using var server = Process.Start(start) ?? throw new Exception("server failed to launch");
try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    using var http = new HttpClient();
    bool alive = false;
    for (int i = 0; i < 160; ++i)
    {
        if (server.HasExited) throw new Exception("server exited: " + server.ExitCode);
        try
        {
            using var response = await http.GetAsync(origin + "/health", timeout.Token);
            if (response.IsSuccessStatusCode) { alive = true; break; }
        }
        catch (HttpRequestException) { }
        await Task.Delay(250, timeout.Token);
    }
    Check(alive, "server is healthy");

    using var alice = new RemoteWorldClient(new Uri(origin));
    using var bob = new RemoteWorldClient(new Uri(origin));
    await alice.RegisterAsync("sdk_alice", "SafeAlicePass12345", "sdk-test-local-long-invite-secret", timeout.Token);
    await bob.RegisterAsync("sdk_bob", "SafeBobbyPass12345", "sdk-test-local-long-invite-secret", timeout.Token);
    Check(true, "SDK account registration");
    try
    {
        await alice.LoginAsync("sdk_alice", "WrongAlicePass123", timeout.Token);
        throw new Exception("Wrong login unexpectedly worked");
    }
    catch (HttpRequestException) { Check(true, "SDK wrong password rejected"); }

    await alice.LoginAsync("sdk_alice", "SafeAlicePass12345", timeout.Token);
    await bob.LoginAsync("sdk_bob", "SafeBobbyPass12345", timeout.Token);
    Check(alice.AccountName == "sdk_alice" && bob.AccountName == "sdk_bob", "SDK login obtained server identity");
    await alice.ConnectAsync(timeout.Token);
    await bob.ConnectAsync(timeout.Token);
    var hello = await Expect(alice, "welcome", timeout.Token);
    var snapshot = await Expect(alice, "snapshot", timeout.Token);
    await Expect(bob, "welcome", timeout.Token);
    await Expect(bob, "snapshot", timeout.Token);
    Check(hello.Payload.GetProperty("name").GetString() == "sdk_alice" &&
          snapshot.Payload.GetProperty("players").GetArrayLength() >= 1, "SDK received handshake and snapshot");

    await alice.MoveAsync(12, 8, timeout.Token);
    var movement = await Expect(bob, "move", timeout.Token);
    Check(movement.Payload.GetProperty("name").GetString() == "sdk_alice" &&
          movement.Payload.GetProperty("x").GetInt32() == 12, "SDK move observed by other player");
    await alice.MoveAsync(900, 900, timeout.Token);
    var invalid = await Expect(alice, "error", timeout.Token);
    Check(invalid.Payload.GetProperty("code").GetString() == "move_too_far", "SDK receives movement rejection");
    await alice.ChatAsync("hello sdk world", timeout.Token);
    var chat = await Expect(bob, "chat", timeout.Token);
    Check(chat.Payload.GetProperty("name").GetString() == "sdk_alice" &&
          chat.Payload.GetProperty("text").GetString() == "hello sdk world", "SDK chat observed by other player");
    await alice.PingAsync(timeout.Token);
    await Expect(alice, "pong", timeout.Token);
    Check(true, "SDK ping-pong");

    await alice.DisconnectAsync(timeout.Token);
    await bob.DisconnectAsync(timeout.Token);
    alice.Logout(); bob.Logout();
    Check(!alice.IsConnected && alice.AccountName == null, "SDK logout and cleanup");
    Console.WriteLine("ALL SDK INTEGRATION TESTS PASSED");
}
finally
{
    if (!server.HasExited)
    {
        server.Kill(entireProcessTree: true);
        await server.WaitForExitAsync();
    }
    try { Directory.Delete(work, recursive: true); } catch (IOException) { }
}
