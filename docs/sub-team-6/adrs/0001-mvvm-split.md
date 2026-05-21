# ADR-0001: MVVM split for the desktop client shell

- **Status:** proposed
- **Date:** 2026-05-19
- **Authors:** Mark Geary (Sub-team 6 Tech Lead, Sprint 1)
- **Backlog:** ARCH-1
- **Supersedes:** —

## Context

`Assets/Scripts/UI/CanvassDesktop.cs` is a ~1900-line `MonoBehaviour` that mixes menu structure, panel state, file dialogs, configuration, threshold maths, and direct native-plugin calls in a single class.

**Quantified pain (baseline — Day 2 CK measurement, SK_BNCH Sprint 1):**

| Metric | CanvassDesktop (measured) | §7.1 threshold | Status |
|---|---|---|---|
| WMC | 63 | ≤ 40 (orchestrator) | **violation** |
| DIT | 1 | ≤ 4 | within |
| NOC | 0 | ≤ 5 | within |
| CBO | 47 | ≤ 25 (orchestrator) | **violation** |
| RFC | 118 | ≤ 50 | **violation** |
| LCOM_HS | 0.955 | ≤ 0.50 | **violation** |

CanvassDesktop's CBO of 47 breaks down as 23 project types (e.g. `VolumeDataSetRenderer`, `FitsReader`, `DataAnalysis`, `FeatureMapping`), 13 Unity/TMPro UI types, 7 System library types, and 4 Valve.VR types. Three fields (`_restFrequency`, `inPaintMode`, `_tabsManager`) are declared but never accessed — dead weight confirmed by the LCOM field-access count of 189 across 63 methods and 67 fields.

**Forces in tension:**

- **Testability vs MonoBehaviour lifecycle.** Unity's `MonoBehaviour` requires a running engine to instantiate; any logic embedded in it cannot be unit-tested with NUnit alone. The assignment mandates ≥ 70 % branch/line coverage on domain code (NFR-TST-1).
- **Unity 2021.3 → Unity 6 migration.** The legacy `UnityEngine.UI` Canvas system is the current runtime; the target is UI Toolkit. Any UI code that is not isolated behind an interface will need to be rewritten in full during migration.
- **First-year team defence risk.** The panel will probe the decision at interview; the split must be explainable in one sentence and verifiable in the artefacts.

**Named dependencies:**

- Sub-team 1 (Architecture/Micro-kernel) owns the **service gateway contract** — this ADR depends on that interface existing; the dependency is raised as DEPS-1 and tracked on the integration risk register (R01 already open).
- Sub-team 7 (Persistence) owns the **desktop-shell state schema** — ARCH-11 (Day 9) hands them the list of state that the client persists; this ADR's ViewModel layer defines what that state is.
- Sub-team 4 (Interaction System) owns **VR-side menus** — the command vocabulary must be shared (R04 on risk register).

The assignment requires a client–server + micro-kernel target style (§4.1) with an anti-corruption layer around Unity 6 APIs and domain code that does not transitively depend on `UnityEngine`/`SteamVR` (§4.2.3). Section 6.6 explicitly prescribes an MVVM split.

## Decision

Adopt a three-layer split inside the desktop client, each in its own C# assembly:

| Assembly | Contents | Unity dependency |
|---|---|---|
| `iDaVIE.Client.View` | `UIDocument` components, USS/UXML, Unity event wiring | Yes — allowed |
| `iDaVIE.Client.ViewModel` | Observable properties, `ICommand` implementations, validation logic | **None** |
| `iDaVIE.Client.Gateway` | `IServiceGateway` adapters, transport, anti-corruption layer | Adapter layer only |

**Dependency direction rule (CI-enforced via NDepend):**

```
iDaVIE.Client.View  →  iDaVIE.Client.ViewModel  →  iDaVIE.Client.Gateway
```

Reverse-direction references are a CI failure. This enforces §4.2.2 (no cycles) and §4.2.3 (no UnityEngine in domain).

**Binding mechanism:** ViewModels implement `INotifyPropertyChanged` for observable state and expose `ICommand` for actions. The View binds to ViewModel properties via a thin `UnityBinder<T>` shim (Unity 2021.3) or native UI Toolkit data-binding (Unity 6). View code must not mutate ViewModel state directly — all mutations go through commands.

**Anti-corruption layer placement:** The ACL lives in `iDaVIE.Client.Gateway` only. ViewModels receive plain DTOs; they never see `UnityEngine.*`, `Valve.VR.*`, or `[DllImport]` types. The composition root (a single `MonoBehaviour` in the View assembly) wires adapters to interfaces at startup, replacing all `FindObjectOfType<>` calls.

**Proof via worked examples:**

- **WE1 (File tab)** — demonstrates the ViewModel command + gateway round-trip: `BrowseImageCommand` → `IFileDialogService` → `IFitsService` → `FileTabViewModel` state update → View binding refresh. Before/after UML and CK delta at `refactoring-examples/sub-team-6/file-tab/`.
- **WE2 (Debug tab)** — demonstrates the Observer/push side: `ILogStream` publishes `LogEntry` events; `DebugTabViewModel` subscribes and maintains a bound collection; the View scrolls automatically. Before/after at `refactoring-examples/sub-team-6/debug-tab/`.

## Consequences

**Positive:**

- `iDaVIE.Client.ViewModel` is unit-testable with NUnit + Moq, no Unity Editor required — satisfies LO6 and NFR-TST-1 (≥ 70 % branch/line on domain code).
- Unity 2021.3 → Unity 6 UI Toolkit migration is scoped to `iDaVIE.Client.View` only — satisfies LO5 and ARCH-7.
- Composition root replaces all `FindObjectOfType<>` singleton lookups; dependencies are explicit and mockable.
- WMC, RFC, CBO, and LCOM for the refactored classes are projected to fall within §7.1 thresholds (see `docs/sub-team-6/refactor.md` §9 for per-class estimates).

**Negative:**

- Extra ceremony: three assemblies, interface boilerplate, `UnityBinder<T>` shim. Mitigation: code skeletons in `refactoring-examples/sub-team-6/` serve as the canonical pattern.
- Risk of "leaky" ViewModels accidentally importing `UnityEngine` via transitive references if assembly definitions are misconfigured. Mitigation: NDepend CQLinq rule fails the build on any forbidden import.
- `INotifyPropertyChanged` is more verbose than Unity's `[SerializeField]` + `OnValidate` pattern that the team is familiar with.

**Operational:**

- NDepend CQLinq rule added in T6 (Quality Guild sprint): forbids `UnityEngine.*` import inside `iDaVIE.Client.ViewModel`.
- PR check blocks merge if any ViewModel class has a direct or transitive reference to `UnityEngine`, `Valve.VR`, or `System.Runtime.InteropServices.DllImportAttribute`.
- The binding shim is reviewed in ARCH-9 (MVVM binding policy, Sprint 2) — this ADR is the seed.

## Alternatives considered

- **MVP (Model–View–Presenter)** — Presenter holds a direct reference to the View interface, making View replacement harder and requiring more test setup to mock the view. UI Toolkit's binding model is built around observable properties, not presenter calls; MVVM is a more natural fit. Rejected.
- **MVU / Elm-style unidirectional flow** — Elegant immutability and predictable state, but mismatches UI Toolkit's two-way binding primitives. Adds significant unfamiliarity risk for a first-year team that must defend the pattern under interview. Rejected for this sprint; could be revisited for a future sub-system.
- **Classical MVC** — Controller and View conflate in Unity practice (the `MonoBehaviour` ends up being both). Drawing a clean ACL boundary is harder when the controller still holds scene references. Rejected.
- **Status quo (God-canvas `MonoBehaviour`)** — `CanvassDesktop.cs` as-is fails §4.2.3 (domain code transitively depends on `UnityEngine` and native P/Invoke) and §4.2.4 (no testable interfaces on public API boundaries). Not a valid target architecture. Rejected.
- **Reactive MVVM (Rx/UniRx)** — Observable streams are a compelling fit for the Debug tab's log push model, but Rx adds a library dependency and a steep learning curve. Deferred: the `ILogStream` event model in WE2 is compatible with an Rx migration later without changing the ViewModel interface.

## References

- §4.1 architectural style (client–server + micro-kernel).
- §4.2 mandatory architectural constraints (§4.2.2 no cycles, §4.2.3 no Unity in domain, §4.2.4 interfaces on public APIs).
- §6.6 sub-team work package brief (MVVM prescription).
- §7.1 CK metric thresholds.
- [ADR-0002 — Client–server transport](../deliverables/D2-Architecture/client-server-transport.md) — transport layer this ViewModel gateway sits behind.
- ARCH-8 — interface contracts proposed to Sub-team 1 (gateway surface).
- ARCH-9 — MVVM binding policy (Sprint 2) — operating manual for the View↔ViewModel binding shim.
- [WE1 — File tab](../../../refactoring-examples/sub-team-6/file-tab/) — proof of ViewModel command + gateway round-trip.
- [WE2 — Debug tab](../../../refactoring-examples/sub-team-6/debug-tab/) — proof of Observer/push pattern.
- DEPS-1 — gateway contract dependency on Sub-team 1, recorded on the [integration risk register](../../team-alpha/integration-risk-register.md).
