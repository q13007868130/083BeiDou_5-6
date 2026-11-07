# P1: first real remotely connected C# client for MapleDash multiplayer

This is the **first checked network client library** matching the separately running `Server/`.
It remains an experimental **sidecar world-events connection**, not a replacement for MapleDash's synchronous `ITransport`.

## What's implemented

- `Client/`: .NET Standard 2.1 C# client SDK, intended for Unity 2022 compatibility (Unity runtime validation remains outstanding). Uses HTTP for account registration/login and authenticated WebSockets for world events.
- `ClientSmoke/`: actually runs the .NET 8 server and **two instances of this SDK**; checks registration, wrong password, login identity, welcome, snapshots, movement from one client seen on another, chat, rejection of invalid movement, ping and clean disconnect.
- `UnityBridge/`: optional scene-side `MonoBehaviour` candidate which runs network I/O off the Unity main thread and dispatches events to the main thread via `Update`.
- `BridgeCompile/`: compiles the Unity bridge against minimal test stubs, **not** a substitute for building it in the Unity Editor.

## Run

```bash
dotnet build reverse-engineering/MapleDash/variants/multiplayer/Client/MapleDash.Multiplayer.Client.csproj -c Release
dotnet build reverse-engineering/MapleDash/variants/multiplayer/BridgeCompile/MapleDash.Multiplayer.UnityBridgeCompile.csproj -c Release
dotnet run --project reverse-engineering/MapleDash/variants/multiplayer/ClientSmoke/MapleDash.Multiplayer.ClientSmoke.csproj -c Release
```

### Usage in a regular C# client

```csharp
using var client = new MapleDash.Multiplayer.Client.RemoteWorldClient(new Uri("http://127.0.0.1:5088"));
await client.LoginAsync("my_account", "my_password");
await client.ConnectAsync();
var welcome = await client.ReceiveAsync();
var snapshot = await client.ReceiveAsync();
await client.MoveAsync(12, 8);
await client.ChatAsync("hello");
```

`LoginAsync` does not print or persist credentials. Use HTTPS/WSS for non-loopback endpoints. Use `ReceiveAsync` from exactly **one** receive loop per connection; do not concurrently read a WebSocket from separate threads. `SendAsync` has a lock.

### Unity prototype limitations

The `UnityRemoteWorldBridge` code is **not yet injected into or invoked by an actual MapleDash scene**; user-provided resource pack and editable Unity project are missing. No remote actors are rendered. The SDK uses `System.Text.Json` NuGet on .NET Standard 2.1, so a Unity project must include the appropriate package/transitive assemblies and validate IL2CPP and AOT behavior, especially on Android. Note that Unity WebGL generally lacks the same ClientWebSocket APIs; a platform-specific transport would be needed.

The recovered `GameSession.Boot()` still creates a local `GameServer` and sets `Net = Server.Transport`. It cannot be switched to the new SDK directly because the original `ITransport` exposes **more than 50 synchronous game RPCs**, while this protocol only handles a demo world (presence, movement, chat). Build an authenticated server-authoritative game RPC gateway before enabling any save, inventory, trade or character mutation remotely. In particular, never accept arbitrary account/character IDs from a remote caller.

This is progress toward actual multiplayer integration, **not** yet a playable cross-platform multiplayer MapleStory client.
