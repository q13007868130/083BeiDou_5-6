# MapleDash P2 — verified IL-to-C# partial recovery

## What this contains

This package contains **partial reconstructions**, not the original Unity project and **not a compilable, complete game/server**. They were generated from `Assembly-CSharp.dll` (SHA-256 `cfaa6d7f088a66259eb6a29c6e55dacfe2c6d3029c21d813daa7951d030c14a1`) using the real method bodies previously extracted in P1.

- `*.partial.cs`: C# snippets emitted only when every instruction of an eligible straight-line IL method could be translated. Auto-property getter/setter pairs are represented as a C# property. Methods with unresolved tokens, branches, exception handling or unsupported instructions are **omitted**, NOT replaced with `throw new NotImplementedException()`.
- `recovery-report.json`: per-method status with metadata tokens/RVAs.
- `dotnet_metadata.py`: no-dependency ECMA-335 metadata reader for signatures, parameters, fields and tokens.
- `recover_csharp_subset.py`: conservative IL-to-C# subset translator. It has not been formally proven equivalent to the original binary; inspect reconstructed methods before execution.
- `decompile-windows.ps1`: optional Windows-side full ILSpy recovery script using the official command-line decompiler. It needs .NET SDK 10+ and network access for the tool installation.

## Selected results

| Type | Recovered entries | Checked method bodies |
|---|---:|---:|
| InProcessTransport | 30 | 55 |
| GameServer | 26 | 37 |
| GameSession | 25 | 53 |
| AdminCtx | 6 | 10 |
| DbStore | 2 | 34 |
| LoginService | 1 | 5 |
| WorldService | 1 | 3 |

The source files reference many types not yet recovered or imported (including UnityEngine types); compiling these fragments alone is not supported. Constructor and call forwarding code from InProcessTransport is especially useful for isolating transport boundaries.

## Verify and reproduce the P2 subset

On a machine with Python 3.10+:

1. Extract `Windows/MapleDash_Data/Managed/Assembly-CSharp.dll` from the original Windows package into the same folder as `dotnet_metadata.py` and `recover_csharp_subset.py`.
2. Extract the existing P1 archive into `il-recovery/` (containing `Core/`, `Other/` and `Server/`).
3. Run `python recover_csharp_subset.py`.
4. Compare the regenerated `csharp-recovery-p2/` with this package.

For **complete C# recovery**, use `decompile-windows.ps1` on a Windows machine; the decompiler result still requires Unity references and assets to build.

## Boundaries

- The Windows binary uses Mono/.NET IL. Android `libil2cpp.so` is a distinct native IL2CPP build; P2 covers Windows DLL only.
- Not tested in the actual Unity runtime; not yet integrated with the existing BeiDou Java server.
- Do not infer remote multiplayer capability from `InProcessTransport` or `AdminHttpServer` alone.
- Preserve attribution and comply with the original author's permissions and applicable game asset rights before publishing/redistributing reconstructed work.