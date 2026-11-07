# MapleDash – standalone server readiness & security (research worktree)

This directory contains a **separate candidate**, not a rebuilt game server and not the author's original Unity project. Original full ILSpy source remains unchanged in `../full-source/DecompiledCSharp/`.

## What is established from the decompiled source

- `GameServer.Start` and `GameServer.StartWith` create `InProcessTransport`, so `ITransport` is not yet served over an authenticated network socket.
- `AdminHttpServer` binds `IPAddress.Loopback`, i.e., an **admin endpoint**, not a multiplayer transport.
- `LoginService` in the unpatched binary permits passwordless login for `admin`, `AccountsDao.DefaultGmLevel` grants GM level by that name, and `AdminBackend.EnsureWebAdmin` seeds the well-known `admin / admin` credentials. **Do not expose the original binary to the internet.**
- Several `InProcessTransport` methods accept `accountId` or `characterId` from the calling client. They are trusted locally, but remote callers would need a verified session and ownership authorization.
- The Unity dependency surface is broad. See `docs/source-audit.json`. The original game's resources/scenes and a complete build are not part of this worktree.

## Hardened candidate copies

- `hardening/LoginService.cs`: removes password exemption, disallows missing password, defaults automatic account registration to disabled.
- `hardening/AccountsDao.cs`: never confers GM privileges based on account name.
- `hardening/AdminBackend.cs`: no fixed bootstrap password; requires `MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD` (>=16 characters) for first-time admin setup, and never logs it.

**Important:** Existing databases may contain `admin` with the old weak password, or existing GM privileges in character rows. This code **does not migrate/rotate those existing credentials or clean data**. Protect or delete affected test data and perform an explicit credential rotation before public deployment. The hardening code is deliberately isolated and not wired into the compiled MapleDash APK/EXE.

## Reproduce / verify

From this repository root, after checkout of the decompiled source:

```sh
python reverse-engineering/MapleDash/server-readiness/scripts/harden_sources.py \
  --source reverse-engineering/MapleDash/full-source/DecompiledCSharp \
  --output /tmp/mapledash-hardened-check

python reverse-engineering/MapleDash/server-readiness/scripts/audit_core.py \
  --source reverse-engineering/MapleDash/full-source/DecompiledCSharp \
  --output /tmp/mapledash-audit.json

dotnet run --project reverse-engineering/MapleDash/server-readiness/tests/MapleDash.AuthSmoke/MapleDash.AuthSmoke.csproj -c Release
```

A dedicated GitHub Actions workflow executes the authentication smoke test using .NET 8. The test harness compiles **real decompiled/modified `LoginService.cs` and `AccountsDao.cs`**, with minimal in-memory substitutes for their DbStore APIs. It is **not** a complete Unity build, full account database test, or network multiplayer test.

## Required work before independent server is usable

1. Identify and isolate all non-Unity server-side dependencies (`BotRuntime`, WZ loading, resource lifecycle, `Native*Service` classes) and build a headless .NET assembly.
2. Persist compatible resources and schemas; test `DbStore` transactions, save/load, migrations, and concurrency.
3. Implement a real `ITransport` network bridge, request serialization, framing, versioning, and proper concurrency, with server-authoritative game state.
4. Bind `Login` to a cryptographically strong session; **ignore caller-supplied accountId** on character operations; verify `characterId` ownership and world/channel membership.
5. Apply credential hardening and explicit admin bootstrap, remove legacy privileged rows, then test under multiple simultaneous clients.
6. Resolve Unity engine DLL references and resource project before attempting cross-platform Unity player builds.

No claim of standalone deployability or multiplayer support is made by the included code.