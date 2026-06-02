# Architecture — In-Depth Explainer

## What the architecture document is for

The architecture deliverable (D2) is the team's answer to: "how do you restructure iDaVIE so it can be maintained, tested, and migrated to Unity 6 without rewriting everything?" It owns the component structure, the dependency rules, the transport choices, and the design decisions behind all of them. Every other deliverable (MVVM policy, worked examples, test strategy) maps back to it.

---

## The problem the architecture is solving

`CanvassDesktop.cs` is 1,899 lines, a single `MonoBehaviour` class. It is the primary problem. Here is what that means concretely:

**CK violations on Day 2 (measured):**

| Metric | `CanvassDesktop` | Threshold | Status |
|---|---|---|---|
| WMC | 63 | ≤ 40 | violation |
| CBO | 47 | ≤ 25 | violation |
| RFC | 118 | ≤ 50 | violation |
| LCOM | 0.955 | ≤ 0.50 | violation |
| DIT | 1 | ≤ 4 | within |
| NOC | 0 | ≤ 5 | within |

What those numbers mean in practice: CBO of 47 means this one class directly names 47 different types — 23 project types, 13 Unity/TMPro types, 7 System library types, 4 SteamVR types. Change any of those 47 and you have to re-analyse `CanvassDesktop`. LCOM of 0.955 means the 63 methods are operating on almost entirely disjoint sets of fields — quantitative proof that the class has eight independent jobs crammed into one object.

The propagation cost is 87.5% of the monitored slice. That means a change anywhere in this class forces re-analysis of almost the entire sub-system.

**What `CanvassDesktop` actually does in the current code:**

- FILE tab — opens data and mask files, calls CFITSIO via P/Invoke directly
- RENDER, STATS, SOURCES tabs — adjusts settings, pushes renderer values into sliders every frame in `Update()`
- DEBUG tab — subscribes to `Application.logMessageReceived`
- Scene-hierarchy wiring — 20+ `transform.Find("A/B/C").GetComponent<T>()` chains
- Coroutine lifecycle — manages load coroutines inline
- Singleton hunting — `FindObjectOfType<VolumeInputController>()` in `Start()`
- Paint-mode state — toggle logic mixed with everything else

**Why it cannot be tested:** `MonoBehaviour` cannot be instantiated without a running Unity engine. So any logic inside it cannot be reached by a standard NUnit test. Zero testable methods today.

**The circular dependency problem:** Two cycles exist in the current code — `CanvassDesktop ↔ VolumeCommandController` and one other. Section 4.2 of the spec says zero circular dependencies is a non-negotiable. Any architecture that does not break these cycles fails the assignment.

---

## The target architecture — four styles in one

The brief mandates a specific target style: **client–server + micro-kernel + layered + plug-in**. Here is what each piece means and why it is there.

### Client–server split

Unity (the VR client) handles rendering, input, and presentation. The server hosts data, domain logic, plug-in execution, and long-running computations. Today iDaVIE runs as one process — a Unity app that calls native C/C++ DLLs directly on the main thread. That is why loading a large FITS file freezes the UI (B-08): the main thread is blocked in CFITSIO.

The split moves the heavy work off the client. The client becomes a thin presentation layer. The server does the loading, analysis, and computation. They communicate over a transport (JSON-RPC over named pipes — see ADR-0002 below).

### Micro-kernel for the server

The server is not a monolith either. A small, stable kernel exposes a versioned plug-in contract. Data formats, domain operations, and coordinate systems are implemented as C/C++ plug-ins. The kernel loads and manages the plug-ins without knowing their internals. This matches iDaVIE's existing DLL structure (FitsReader, DataAnalysis, AstTool) — the refactor makes that contract explicit and versioned rather than implicit and fragile.

### Layered architecture inside the kernel

Strict downward dependency: Domain → Application → Infrastructure → Plug-in host. A layer can only call the layer below it, never the layer above. This is enforced in CI — a reverse-direction reference is a build failure, not a code review comment.

### Anti-corruption layer (ACL) on the client

Domain code in the client must not transitively depend on `UnityEngine` or `SteamVR` types. This is a hard rule in Section 4.2 of the spec.

Today `CanvassDesktop` directly names 13 Unity/TMPro types and 4 SteamVR types in its domain logic. That is why no ViewModel can be unit-tested without spinning up Unity. The ACL is a boundary — domain code lives on one side (pure C#), Unity and SteamVR live on the other side. Classes in the adapter layer are the only ones allowed to cross that boundary.

---

## The three-assembly split on the client

The client is divided into three C# assemblies. Assembly boundaries enforce the dependency rules — they are not just folder organisation. If the ViewModel assembly has no reference to `UnityEngine`, then adding a `using UnityEngine` in a ViewModel will not compile.

```
iDaVIE.Client.View  →  iDaVIE.Client.ViewModel  →  iDaVIE.Client.Gateway
```

Arrows show allowed dependency direction. Reverse references fail CI via NDepend.

**`iDaVIE.Client.View`** — UIDocument components, USS/UXML, Unity event wiring. Contains all `MonoBehaviour` subclasses. Allowed to use Unity. Not allowed to contain logic.

**`iDaVIE.Client.ViewModel`** — Observable properties, `ICommand` implementations, validation logic. Zero `UnityEngine` references. This is where `FileTabViewModel`, `DebugTabViewModel` etc. live. Can be compiled and tested with a standard `dotnet test` command with no Unity present.

**`iDaVIE.Client.Gateway`** — The ACL. Two kinds of class:
- **Gateway proxies** — pure C#, own the JSON-RPC wire format, unit-testable.
- **Unity adapters** — the only classes allowed to touch the Unity SDK. They implement domain interfaces so the ViewModel never names a Unity type.

---

## C4 Level 3 — what the client shell looks like after refactoring

Six tabs (File, Render, Stats, Sources, Paint, Debug), each decomposed into:
- A **View** — thin MonoBehaviour, binds to the ViewModel interface, forwards UI events, no logic.
- A **ViewModel** — pure C# class, owns state and commands, talks only through interfaces.
- A **Gateway adapter** — implements the interface the ViewModel needs, handles the transport details.

One **`CanvassDesktopShell`** composition root wires it all at startup. This replaces the current `CanvassDesktop.Start()` which does inspector lookups, log-rotation, button wiring, and object graph construction all mixed together.

---

## Architecture Decision Records

The architecture document contains formal ADRs. These are the key ones for our work package:

**ADR-0001 — MVVM with three-assembly split (2026-05-19)**
Decision: adopt MVVM with `View / ViewModel / Gateway` assemblies. CI-enforced dependency direction. Reason: `MonoBehaviour` cannot be unit-tested; assembly boundaries enforce the no-Unity-in-domain rule at compile time, not just by convention.

**ADR-0002 — Client–server transport: JSON-RPC over named pipes (2026-05-20)**
Decision: JSON-RPC 2.0 over named pipes for local mode; gRPC considered for future remote streaming. Reason: JSON-RPC is simpler to debug and implement than gRPC. Named pipes are low-latency for same-machine communication. The transport is hidden behind `IServiceGateway` — swapping to gRPC later does not change any ViewModel code.

**ADR-0003 — ACL enforced by assembly reference, not convention (2026-05-21)**
Decision: the ViewModel assembly project file lists no Unity package reference. Consequence: any accidental `using UnityEngine` in a ViewModel is a compile error, not a code-review catch. NDepend validates this on every PR.

**ADR-0004 — Observer pattern for structured log stream (2026-05-21)**
Decision: the Debug tab uses the GoF Observer pattern (`ILogObserver.OnNext`) rather than a C# `event`. Reason: the brief's §6.6 explicitly says "Debug tab as Observer of a structured logging stream." The Observer interface also lets future consumers (autosave, telemetry, error counter) attach without modifying any producer.

---

## How the architecture fixes the known bugs

This is the most important argument for the panel. Every structural choice maps to a real, documented defect.

| Defect | Cause | Fix |
|---|---|---|
| **B-02** (Debug tab crash during load) | `Application.logMessageReceived` fires while the main thread is blocked in CFITSIO | Debug tab becomes an Observer of `ILogStream`, marshalled via `IUIDispatcher`. No main-thread block. |
| **B-08** (UI freeze during file load) | Synchronous native read on the Unity main thread | `LoadAsync` awaits `IVolumeService` behind the gateway; `IsLoading` drives a spinner; main thread never blocks. |
| **B-03** (slider sync broken between Render/Stats/VR) | Render and Stats share mutable `CanvassDesktop` fields with no notification | Both observe `IVolumeService` events (single source of truth, broadcast via events). |
| **B-04** (percentile freeze on large cubes) | Exact percentile computed synchronously | `ApplyPercentileCommand` → `ComputeHistogramAsync` with `IProgress<float>`; VM exposes `IsRecomputing`. |
| **Cycles** (§4.2 non-negotiable) | `CanvassDesktop ↔ VolumeCommandController` | After refactoring: topological sort shows zero back-edges. Validated by NDepend. |

---

## The Unity 2021.3 → Unity 6 migration problem

The current code is on Unity 2021.3 using the legacy `UnityEngine.UI` Canvas system. The target is Unity 6 using UI Toolkit.

In the current design, UI code and domain logic are completely mixed inside `CanvassDesktop`. Migrating to UI Toolkit means rewriting the UI layer — but since that layer is fused to the domain logic, you have to carefully rewrite both at the same time, with high risk of introducing bugs.

In the proposed architecture, only the **View layer** changes during migration. The ViewModels are UI-agnostic pure C# — they do not know whether the UI is Canvas, UI Toolkit, or something else. A full migration means rewriting UIDocument files and View MonoBehaviours. The ViewModel code and its tests remain untouched.

---

## ACL — the border-crossing metaphor

The ACL works like a border crossing. Domain and ViewModel code live on one side. Unity and SteamVR live on the other. ViewModels communicate only through clean interfaces. Unity and SteamVR are accessed only through adapter classes in the ACL.

In practice:
- `IFileDialogService` — the interface. `FileDialogServiceAdapter` — the Unity adapter that calls `StandaloneFileBrowser`.
- `IFitsService` — the interface. `FitsServiceAdapter` — the gateway proxy that sends `file.open` over JSON-RPC.
- `ILogStream` — the interface. `GatewayLogStreamAdapter` — the adapter that subscribes to `IServiceGateway.OnNotification` and fans log entries out to observers.

Static analysis tools (NDepend, DV8) enforce this automatically. A CI rule using NDepend's CQLinq query language fails the build if any type in the ViewModel namespace transitively reaches `UnityEngine`. This is not a code review catch — it is a compile-time constraint.

---

## Projected CK improvement (Day 13, tool-verified)

After full MVVM extraction across all six tabs plus the composition root:

| Metric | Before (CanvassDesktop) | After (max across ~25 classes) |
|---|---|---|
| WMC | 63 | 43 (FileTabViewModel — documented exception, remediation planned) |
| CBO | 47 | 19 |
| RFC | 118 | 43 |
| LCOM | 0.955 | 0.91 max (MVVM artifact — see note) |
| Circular cycles | 2 | **0** |
| Testable classes | 0 | 25+ (all ViewModels and domain classes) |

The LCOM readings above 0.5 on some MVVM classes are an artifact of the pattern: each observable property has its own backing field, accessed by at most two methods (getter and `PropertyChanged` notifier). This is not the same as a God-class with disjoint concerns — it is a known measurement effect of the property-notification pattern.

---

## Honest caveats

- `FileTabViewModel` WMC=27 exceeds the ≤ 20 domain threshold. Accepted as-is with a documented remediation (extract a `FileTabCommands` helper). Not masked.
- Stats, Sources, Paint, and Rendering are **design gestures** only — the interfaces are named, the split-from-current-code tables are done, but no tested skeleton exists for those tabs. File and Debug are the two full worked examples.
- The actual transform-math fix for B-05 (WCS vs voxel offset) is server-side and out of our scope. Our design makes the coordinate choice explicit and testable; the arithmetic correction lives in `IFeatureSetService`.
