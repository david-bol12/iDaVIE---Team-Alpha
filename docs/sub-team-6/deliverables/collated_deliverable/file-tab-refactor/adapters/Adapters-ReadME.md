# File-tab adapters — what lives here

Everything in this folder is an **implementation of an interface that lives in
`skeleton/`**. An adapter's job is to satisfy a domain interface by crossing an
**outward boundary** — either the server transport (JSON-RPC over the gateway)
or the Unity SDK / native UI. The skeleton holds the *contract*; this folder
holds the *plumbing* that honours it.

The test for any class is never "is it an adapter or a view?" — it's
**"does it implement a `skeleton/` interface by talking to Unity or the wire?"**

- Talks to Unity (`UnityEngine`, `TMPro`, SFB, coroutines) or the server transport → it belongs in `adapters/`.
- Pure domain logic, testable without either → it belongs in `skeleton/`.

This is the inverse of the skeleton's membership rule, and it is what keeps the
Unity/native coupling quarantined at the bottom of the dependency graph
(ADR-002 ACL, NFR-MOD-2).

## Two kinds of adapter

Not every adapter needs Unity. The folder holds two sub-kinds:

- **Gateway proxy** — pure C#, no `UnityEngine`. Implements a domain interface by
  translating its calls into the JSON-RPC method catalogue and dispatching them
  over `IServiceGateway`. FITS reading itself runs **server-side** (brief §6.6,
  *"direct file I/O that belongs server-side"*); the proxy only owns the wire
  shape. `FitsServiceAdapter` is the only one in this tab.
- **Unity adapter** — references the Unity SDK directly. These are the *only*
  classes in the whole File-tab slice permitted to `using UnityEngine`, touch
  `PlayerPrefs`, run coroutines, or instantiate prefabs.

## Inventory

| Kind | File | Implements | Unity? | Compiles in CI? |
|---|---|---|---|---|
| **Gateway proxy** | `FitsServiceAdapter.cs` | `IFitsService` (via `IServiceGateway`) | No | **Yes** |
| **Unity adapter** | `FileDialogServiceAdapter.cs` | `IFileDialogService` (wraps `StandaloneFileBrowser` + `PlayerPrefs`) | Yes | No (Unity project) |
| **Unity adapter** | `VolumeServiceAdapter.cs` | `IVolumeService` (`MonoBehaviour`; coroutine host, prefab/renderer lifecycle) | Yes | No (Unity project) |
| **Unity adapter** | `MemoryProbeAdapter.cs` | `IMemoryProbe` (wraps `SystemInfo.systemMemorySize`) | Yes | No (Unity project) |
| **View** | `FileTabView.cs` | thin `MonoBehaviour`; binds to `IFileTabViewModel` | Yes | No (Unity project) |
| **Composition root** | `FileTabCompositionRoot.cs` | `MonoBehaviour`; builds and wires the object graph (Pure DI) | Yes | No (Unity project) |

## The build split (why only one file compiles here)

`FileTabAdapters.csproj` sets `EnableDefaultCompileItems=false` and explicitly
includes **only `FitsServiceAdapter.cs`**. Because the gateway proxy is Unity-free
once rewired through `IServiceGateway`, it builds and runs in CI alongside the
skeleton — proving the FITS path crossed the ACL with no `UnityEngine` or
`[DllImport]` left client-side.

The other five files (`FileTabView`, `FileTabCompositionRoot`,
`FileDialogServiceAdapter`, `VolumeServiceAdapter`, `MemoryProbeAdapter`) need
Unity, so they are **compiled inside the Unity project**, not by this csproj.
Add any new Unity-free adapter as an explicit `<Compile Include="..."/>` entry;
leave the Unity-coupled ones out.

## Why adapters reference the skeleton, never the reverse

An interface belongs to the code that **needs** it, not the code that
**implements** it (Dependency Inversion). `FileTabViewModel` needs to read FITS
files, so `IFitsService` lives in `skeleton/`; `FitsServiceAdapter` implements it
here and references the skeleton — never the other way round.

```
adapters  ──references──▶  skeleton
          (never ◀──)
```

If the dependency ever reversed, the skeleton would have to reference the adapter
assembly, dragging Unity and the transport back into the pure-C# layer — a
circular dependency and a broken ACL. The composition root
(`FileTabCompositionRoot`) is the single place allowed to see **both** sides at
once: it `new()`s the pure adapters, takes the Unity ones from the Inspector,
injects them into the ViewModel, and binds the View.
