# MapleDash · 联机版（独立网络原型）

**Branch:** `mapledash/multiplayer`. This branch is independent of the original offline gameplay branch `mapledash/singleplayer`.

## What actually runs

`Server/` is a self-contained **.NET 8** loopback multiplayer networking prototype built from newly written code, informed by the ILSpy-recovered `GameServer` and `ITransport` architecture. It does **not** yet run the recovered MapleDash game services. Features actually implemented:

- JSON account storage, PBKDF2-SHA256 passwords with per-account random salt (210,000 iterations), invite-gated registration **disabled by default**, one-hour random session tokens; no `admin/admin` credential.
- Login endpoint and `Authorization: Bearer ...` for every WebSocket, without trusting any client-provided player/character ID.
- In-memory authoritative **demo** world with player-presence broadcast, initial snapshot, bounded movement, monotonic sequence numbers and chat; 4 KiB maximum incoming message, simple chat/move rate limits.
- Two-player .NET integration smoke test verifying authentication, synchronized movement and chat, identity spoofing resistance, invalid movement rejection. Both branches retain the original decompiled C# as reference.

## Build / local test

```sh
dotnet build reverse-engineering/MapleDash/variants/multiplayer/Server/MapleDash.Multiplayer.Server.csproj -c Release
dotnet run --project reverse-engineering/MapleDash/variants/multiplayer/Smoke/MapleDash.Multiplayer.Smoke.csproj -c Release
```

To use manually, run the server with **localhost binding only** by default:

```sh
MAPLEDASH_REGISTRATION_INVITE='a-long-random-secret-at-least-16-chars' \
MAPLEDASH_DATA_DIR='./mapledash-private' \
dotnet run --project reverse-engineering/MapleDash/variants/multiplayer/Server
```

`POST /api/register` accepts `{"name":"user_123","password":"12+ character secret"}` and requires header `X-Registration-Invite`. `POST /api/login` returns a one-hour token. `ws://127.0.0.1:5088/ws` requires the `Authorization: Bearer <token>` header.

Example messages:

```json
{"type":"move","x":20,"y":10,"seq":1}
{"type":"chat","text":"hello"}
{"type":"ping"}
```

The server's push notifications include `welcome`, `snapshot`, `presence`, `move`, `chat`, `pong`, and `error`. Every actor name is **server-derived from the authenticated account**.

## Known limits / production blockers

1. **Not yet the original MapleDash multiplayer game.** The original `ITransport` contains more than 50 operations (character selection, inventory, skill, NPC, quest, trade, bots, etc.). The .NET prototype handles only presence, movement and chat; the Unity client currently still calls `InProcessTransport`, not this service. The client adapter and complete RPC routing remain to be implemented.
2. The scene/Map/WZ/resource data was not uploaded. Collision, physics, monsters, equipment, character saves, persistent world snapshots, and cross-server login are **not** implemented in this prototype. No Android player APK has been modified.
3. **Do not expose it directly to the public internet.** Default is HTTP and 127.0.0.1. Production deployment requires HTTPS/WSS, reverse proxy, rate limits, production auth and secure password recovery, auditing, authorization of all recovered game RPC operations, robust persistence and scalability.
4. The recovered Unity Mono C# project is missing scenes/assets/engine references and cannot yet be compiled into a playable game.
5. `MapleDash` is a third-party reverse-engineered project; follow source and asset licensing requirements when distributing or deploying.

## Integration architecture

```
Unity offline player (existing) -> InProcessTransport -> Unity GameServer

Unity online player (future)    -> authenticated remote client -> .NET gateway -> server-side game services
                                                                -> durable DbStore
```

We have intentionally not exposed `GameServer` via a raw JSON reflection endpoint. Original requests carry `accountId` and `characterId` fields and are unsafe unless ownership is enforced by a server-held session. The new server is a **verified transport and state-sync foundation**, not a claim that MapleDash is already playable online.