# MapleDash P3 — Incremental verified C# reconstruction

This folder contains a **new C# recovery pass**, based on the Windows Mono assembly `Assembly-CSharp.dll`. It is a continuation of P1 (all method IL extraction) and P2 (partial C# recovery). It is **not** the original Unity project, not a complete decompilation, and not yet a compilable server or game.

## Input and provenance

- Original input: user-provided `Windows.zip` → `Windows/MapleDash_Data/Managed/Assembly-CSharp.dll`.
- DLL SHA-256: `cfaa6d7f088a66259eb6a29c6e55dacfe2c6d3029c21d813daa7951d030c14a1`.
- P3 is derived from actual method bodies in P1's IL inventory and ECMA-335 type/method metadata.
- P3 emits only **incremental** methods not already marked recovered in the P2 `recovery-report.json`. It does not modify the prior P2 snapshot.

## Summary

- **23** selected types with IL method bodies were inspected.
- **377** methods were checked across those types.
- **51 new methods** are recovered in `source/` relative to P2.
- P2 previously reported **91 recovered method/property entries** in its seven selected core classes. Together, P2 and P3 account for 142 reported entries, without double counting identical method metadata tokens. This is a targeted subset, not a recovery percentage of the complete assembly.
- Methods requiring unsupported branches, generic call specifications, exception handlers, or unproven constructs were skipped with reasons in `report.json`; no fake method bodies or `NotImplementedException` stubs were inserted.
- P3 corrects short integer IL opcode parsing (`ldc.i4.s`), applies explicit C# enum/boolean literal conversions, and recognizes a narrowly verified null-coalescing IL pattern. Earlier P2 files remain historical snapshots; when overlapping code is used downstream, revalidate it against the original IL.

## High-value verified examples

The following methods are present in the binary with reconstructable straight-line bodies:

- `InProcessTransport.Login`, `ServerList`, `CharList`, `ImportLegacy`: call local server business services to handle requests.
- `AccountsDao.FindById`, `AutoRegister`, `Gender`: account/database access (not a complete login policy or network service).
- `CharactersDao.NameTaken`, `Save`: character persistence helpers.
- `DbStore.Create`, `Escape`, `Int`: DB store initialization and utility methods.
- `LoginService.IsNoPasswordAccount`: checks whether a trimmed account name equals `admin` ignoring case. **Security review is recommended** before exposing any login service externally; this function alone does not prove whether passwords can be bypassed.
- `AdminApi` HTTP method routing (`Get`/`Post`/`Put`/`Delete`) and `AdminHttpResponse` constructors.

## How to reproduce and verify

1. Extract `Assembly-CSharp.dll` locally from the user-provided Windows ZIP.
2. Extract the existing P1 `il-recovery/` directory, and obtain P2's `recovery-report.json`.
3. Arrange the files as shown below and run the P3 recovery script (pure static analysis):

```text
work/
├── recover_p3.py
├── MapleDash-CSharp-Recovery-P2/
│   ├── Assembly-CSharp.dll
│   ├── dotnet_metadata.py
│   └── recovery-report.json
└── il-recovery/
    ├── Core/
    ├── Server/
    └── Other/
```

```bash
python recover_p3.py
python verify_p3.py
```

P3 outputs `P3/source/` and `P3/report.json`. The verification script checks metadata mappings, known recovery signatures, old/new token disjointness, output inventory and obvious placeholder defects. **This is not an actual C# compilation test**, because the Unity project, dependent declarations, and a .NET compiler are unavailable in this environment.

## Limitations and cautions

- C# source files are **partial extracts**. Their original classes, fields, property definitions, Unity references, project files, and many complex methods have not been restored.
- A restored straight-line stack expression is not a formal proof of semantic equivalence with the runtime in every context; unverified cases should be tested before use.
- No remote multiplayer server was created or tested. `InProcessTransport` still links game client requests to in-process server logic.
- Binary/client/game-asset licensing must be reviewed before republishing complete reconstructed code or distributing game resources.
- Do not expose the local admin HTTP endpoint or account services publicly without an independent authentication/security review.
