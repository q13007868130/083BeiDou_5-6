# MapleDash · 单机版 (offline development branch)

**Branch:** `mapledash/singleplayer` — independently maintained from `mapledash/multiplayer`.

This branch retains MapleDash's **existing** local architecture: `GameSession.Boot()` calls `GameServer.Start()`, which creates `InProcessTransport`. Data is stored locally by `DbStore`, so a separate TCP game server is **not required** for the original offline flow.

## Implemented in this branch

- Reproducible generation of an **isolated, complete C# source working tree** from the 1,003-file ILSpy export.
- Three reviewed security-overlay files (`LoginService`, `AccountsDao`, `AdminBackend`) are copied on top, without changing the forensic ILSpy originals.
- Preflight assertions for the local `InProcessTransport` path, security overlay, source completeness, and file-hash manifest.
- Branch-specific GitHub Actions static checks, plus previously established auth smoke tests.

## Generate an offline source working tree

From repository root:

```bash
python3 reverse-engineering/MapleDash/variants/singleplayer/scripts/build_offline_source.py \
  --repo . --output /tmp/mapledash-unity-offline-source
```

On Windows PowerShell, use a new directory instead of `/tmp`. The generator deliberately refuses to overwrite existing output and never modifies `full-source/DecompiledCSharp`.

**This is not a ready-to-play build.** Compiling an actual desktop/mobile player still requires the original Unity project metadata, scenes, prefabs, engine assemblies, required assets, and Unity Editor. The user's huge data pack was not uploaded and is not silently recreated by C# decompilation. The generator's `Assembly-CSharp.csproj` came from ILSpy and has not been verified to build against the Unity SDK.

Security overlay is a *candidate*, not a database migration. Existing `admin/admin` records and GM privileges must be rotated or removed separately before any network exposure. For a single-user local sandbox, keep backend and HTTP admin endpoint bound to loopback.

The multiplayer branch owns **new** server/network code; do not merge it automatically into this branch.