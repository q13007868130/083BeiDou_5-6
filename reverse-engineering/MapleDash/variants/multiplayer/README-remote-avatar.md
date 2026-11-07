# Unity remote avatar debug presentation (experimental)

This integration extends the authenticated MapleDash `RemoteWorldClient` sidecar. It does **not** make the original MapleDash Unity game multiplayer yet.

## New pieces

- `Client/RemoteWorldState.cs`: deterministic, Unity-independent reducer for `welcome`, `snapshot`, `presence`, `move`. Excludes the authenticated local player, clears stale players on reconnect/snapshot, and waits for positional data before showing a joining actor.
- `UnityBridge/UnityRemotePlayerPresenter.cs`: example Unity `MonoBehaviour` subscribing to the existing `UnityRemoteWorldBridge.OnWorldEvent` dispatch (which runs on Unity's main thread). Creates blue **debug** player placeholders with labels, updates positions, and destroys objects on disconnect. This is deliberately separate from the original MapleDash actor renderer.
- `StateSmoke/`: .NET 8 tests for world roster/reconnect/disconnect/invalid payload handling.
- `BridgeCompile/`: compile-only Unity mock API verifies C# shape of both Unity bridge scripts. **This is not a real Unity Editor or Android IL2CPP build.**

## Using in a restored Unity Editor project (when available)

1. Add the compiled `MapleDash.Multiplayer.Client.dll` plus compatible `System.Text.Json` dependencies to the Unity 2022 project. Check AOT/IL2CPP link preservation on Android.
2. Add `UnityRemoteWorldBridge.cs` and `UnityRemotePlayerPresenter.cs` to your Unity project's `Assets/Scripts/` (do **not** add `BridgeCompile/UnityStubs.cs`).
3. On a scene object, add `UnityRemoteWorldBridge`, then `UnityRemotePlayerPresenter`; drag the bridge reference into the presenter inspector field. Configure a parent transform if desired.
4. Trigger `UnityRemoteWorldBridge.LoginAndConnectAsync` with credentials from a secure login UI. Do not put passwords in assets. Map actual player movement into `SendMoveAsync` only after you have defined a shared server/map coordinate system.
5. Placeholder position uses `worldUnitsPerNetworkUnit` and `invertNetworkY`, which need mapping to MapleDash field coordinates. It is not the original character sprite renderer.
6. For remote machines, use HTTPS/WSS. The current local test server binds 127.0.0.1 by default; production deployment and asset/scene restoration are not complete.

## Known limitations

- The network server is a *demo world*: no map-scoped replication, monsters, equipment, quests, combat or authority over original MapleDash `ITransport` requests. These placeholders cannot be considered actual MapleDash players.
- The presenter has not been executed in a Unity scene. Its compile check uses stubs only.
- `GameSession.Net` still points at `InProcessTransport`; offline functionality is intentionally untouched, even on the multiplayer branch.
- The same debug placeholders will show all connected demo users until server map/channel scoping is implemented. Do not ship this as game multiplayer.

## Local checks

```
dotnet build reverse-engineering/MapleDash/variants/multiplayer/Client/MapleDash.Multiplayer.Client.csproj -c Release
dotnet build reverse-engineering/MapleDash/variants/multiplayer/BridgeCompile/MapleDash.Multiplayer.UnityBridgeCompile.csproj -c Release
dotnet run --project reverse-engineering/MapleDash/variants/multiplayer/StateSmoke/MapleDash.Multiplayer.StateSmoke.csproj -c Release
```
