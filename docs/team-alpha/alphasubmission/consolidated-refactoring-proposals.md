# Consolidated Refactoring Proposal Report — Team Alpha

**iDaVIE: Refactoring the Codebase for Maintainability against ISO/IEC 25010:2023**

_Team-level deliverable **T4**. Maximum 60 pages excluding appendices._
_Companion deliverables: T2 (baseline benchmark), T3 (architecture overview + ADR log + plug-in ABI), T7 (integration & metrics), T8 (AI usage). This report consolidates and references them; it does not replace them._

| | |
|---|---|
| Codebase | iDaVIE — Immersive Data Visualization Interactive Explorer (C# + C/C++ native plug-ins on Unity) |
| Target style | Client–server · micro-kernel server · layered kernel · C/C++ plug-in host · anti-corruption layer around Unity 6 |
| Standard | ISO/IEC 25010:2023 maintainability — Modularity, Analysability, Modifiability, Testability, Reusability |
| Baseline tool run | NDepend v2026.1.5 over 37 assemblies / 3,051 files (20 May 2026) |
| Mode | Design-only proposal; worked examples show how code *would* change. No production code is modified. |

---

## Contents

1. Executive summary
2. The problem — baseline maintainability and pain points
3. Requirements — NFRs and architectural drivers
4. Target architecture
5. Architecture decision records (summary)
6. Worked refactoring examples (seven work packages)
7. Consolidated metrics and projected improvement
8. Testability strategy and CI quality gates
9. Unity 5 → Unity 6 migration plan
10. Cross-sub-team integration
11. Trade-offs and risk
12. AI usage and conclusion
- Appendices A–F (not counted toward the 60-page limit)

> **A note on this report's relationship to its companion deliverables.** Sections 2, 4–5,
> 7, 10 and 12 summarise the standalone deliverables T2, T3, T7 and T8 respectively. Where a
> figure or table is reproduced here it is curated for the argument; the authoritative, full
> versions live in those documents and in the appendices. All CK numbers in this report are
> reproduced verbatim from each sub-team's own deliverable as consolidated in
> `metrics.md`; nothing is estimated or invented, and gaps are shown as gaps.

---

## 1. Executive Summary

iDaVIE is a mature, scientifically valuable VR application, but its current architecture
carries a maintainability debt that an imminent Unity 5 → Unity 6 migration will sharply
amplify. An NDepend baseline over the whole codebase records **20,031 issues**, a technical
debt of **430.63 man-days (10.29 %, the "High" tier)**, and a debt breaking-point of
**~1.46 years** — under **one year** when only critical and high-severity issues are
counted. Three of twelve quality gates already fail at the first measurement. The debt is
concentrated in a small number of **god classes** — `VolumeDataSetRenderer` (~1,400 LOC),
`CanvassDesktop` (1,899 LOC), `VolumeInputController` (~1,635 LOC) — that intermix Unity
lifecycle, domain logic, native-plugin calls and presentation in single units.

Team Alpha proposes the target architecture mandated by the assignment: a **client–server
split**, a **micro-kernel server** exposing a **versioned C ABI** to native plug-ins, a
**strictly layered kernel** (Domain → Application → Infrastructure → Plug-in Host) with
downward-only dependencies, and an **anti-corruption layer (ACL)** isolating Unity and
SteamVR so domain logic becomes testable without a headset or the Editor. Twelve ADRs
(Appendix A) record the decisions and their trade-offs.

The proposal is evidenced by **worked refactoring examples across all seven work packages**,
each with before/after class structure, design-pattern justification, and — where measured —
Chidamber & Kemerer (CK) metric deltas. The headline results, drawn from the sub-teams' own
tool exports:

- **`VolumeDataSetRenderer`**: WMC ≈ 138 → 22, CBO ≈ 17 → 9, RFC ≈ 72 → 24, LCOM ≈ 0.82 → 0.20 (worst successor class; Sub-team 1 projection). Sub-team 3, measuring the same class with the Understand tool, records WMC 97 → 12 and RFC 97 → 12.
- **`FitsReader`** (NDepend-measured): the 55-WMC / 126-RFC / 1.0-LCOM monolith is recast into five classes; LCOM drops to **0** across all of them.
- **`CanvassDesktop` file tab** (Understand, tool-verified): WMC 63 → 40, CBO 30 → 19, LOC 1,899 → ~450, and **dependency cycles 2 → 0** — with **47 NUnit tests** now possible where none were before.

We are candid about two gaps: the **Interaction** work package (WP4) did not record
after-refactor CK values, and the **Persistence** work package (WP7) recorded only target
thresholds, not measured baselines. Both are presented honestly in §6 and §7 rather than
filled with speculative numbers, consistent with the assignment's rule that projected
snapshots must be evidence-backed.

The remainder of this report substantiates these claims and sets out the testability
strategy, the Unity 6 migration plan, the integration model across sub-teams, and the
trade-offs the panel should weigh.

---

## 2. The Problem — Baseline Maintainability and Pain Points

_Source: T2 baseline maintainability benchmark (NDepend v2026.1.5; full report in Appendix E)._

### 2.1 Headline figures

| Indicator | Value | Status |
|---|---|---|
| Total issues | 20,031 | Requires attention |
| Critical issues | 29 | **FAIL** (gate threshold 10) |
| Technical debt | 430.63 man-days (10.29 %) | High tier (> 10 %) |
| Annual interest on debt | 295.03 man-days | ~1.18 developer-years / year |
| Breaking point (all debt) | ~1.46 years | Urgent |
| Breaking point (critical/high only) | ~0.99 years | Immediate |
| Quality gates failed | 3 of 12 | **FAIL** |
| Rules violated | 110 of 174 (63 %) | Significant |

The three failing gates are **critical issues** (29 vs 10), **critical rules violated**
(12 vs 0) and **debt rating per namespace** (13 namespaces over budget). A further three
coverage gates were *skipped* because no coverage data exists — itself a finding (§8).

### 2.2 Complexity outliers

Averages are healthy (mean cyclomatic complexity 1.95, mean 4.13 LOC/method), but the
**maxima reveal the god-class problem**:

- A single method of **1,396 LOC** (≈ 23× the recommended 60).
- A single method with **cyclomatic complexity 575** (the unmaintainable threshold is ~25).
- A single type of **3,422 LOC** and a type with **1,970 methods**.
- Maximum IL nesting depth of **161**.

These outliers drive a disproportionate share of the critical/high issue counts and are
exactly the classes the worked examples in §6 dismantle.

### 2.3 Pain points mapped to ISO 25010

The assignment names seven maintainability pressures (spec §1.1). Each maps to a measurable
symptom and a 25010 sub-characteristic:

| Pain point | Baseline symptom | 25010 sub-characteristic |
|---|---|---|
| MonoBehaviour ↔ scene coupling | Domain logic uninstantiable outside Unity | Testability |
| Heavy singleton reliance | `Config.Instance`, `FindObjectOfType` hidden deps | Modularity, Testability |
| Long monolithic classes | WMC/RFC/LCOM breaches in god classes | Analysability, Modifiability |
| Mixed concerns in render/interaction | LCOM ≈ 0.95 on key classes | Modularity |
| Unity lifecycle ∩ domain logic | `Update()`/`Start()` carry business logic | Testability, Modifiability |
| Thin abstraction over native plug-ins | Direct P/Invoke, unversioned ABI, no isolation | Modularity, Reusability |
| Limited test coverage | No coverage data importable; 154,641 LOC "uncoverable" | Testability |

### 2.4 The amplifier

A migration from Unity 5-era patterns to Unity 6 (new Input System, scriptable render
pipelines, package architecture, UI Toolkit) is imminent. Because domain logic is today
fused to `UnityEngine` and `SteamVR` types, every API change ripples through the entire
codebase. The target architecture's central promise (§4, §9) is to **confine that ripple to
a thin adapter layer**.

### 2.5 Positive baselines to preserve

Two findings are strengths to retain: a **32.34 % comment ratio** (above the 15–30 %
industry guidance) and **no assembly-level dependency cycles** at baseline. The proposal
must not regress either.

---

## 3. Requirements — NFRs and Architectural Drivers

Every sub-team translated the four ISO 25010 maintainability sub-characteristics (plus
Reusability) into testable non-functional requirements. The drivers converge on a common,
tool-checkable threshold set drawn from spec §7.1–7.2:

| Driver | Threshold | Tool / gate |
|---|---|---|
| WMC | ≤ 20 domain; ≤ 40 adapter | Understand |
| DIT / NOC | ≤ 4 / ≤ 5 | Understand |
| CBO | ≤ 14 domain; ≤ 25 orchestrator/adapter; cycles = 0 | Understand / NDepend |
| RFC | ≤ 50 | Understand |
| LCOM (Henderson-Sellers) | ≤ 0.5 | Understand |
| Cognitive complexity / method | ≤ 15 | SonarQube |
| Duplication | ≤ 3 % (30-line block) | SonarQube |
| Branch + line coverage | ≥ 70 % domain; ≥ 50 % overall | SonarQube coverage |
| Static/Unity calls per ViewModel/domain class | 0 | Custom NDepend rule |
| Interface size (ISP) | ≤ 7 public members | Audit |
| Dependency cycles | 0 at namespace and assembly level | NDepend / DV8 DSM |

The clearest worked NFR set is Sub-team 6's, which assigns a MoSCoW priority and an
acceptance metric to every requirement and ties each to a real defect (e.g. NFR-REU-3
"ViewModel has zero transitive `UnityEngine` dependency", verified by NDepend on every PR;
motivated by bug B-02, the debug-tab crash during cube load). Sub-team 7 marks NFR1
(compile/run outside Unity) and NFR2 (every public boundary an interface with ≥ 1 test
double) as **mandatory**, sourced directly from spec §4.2 constraints 3 and 4. Sub-team 3
records the rendering system invariants that act as performance drivers: **90 fps floor**,
**4 GB texture cap**, **368 MB default cube budget**, **nearest-neighbour filtering only**.

The architecturally significant requirements (ASRs) — kernel load time, ABI stability,
plug-in failure isolation, hot-reload, observability — are owned by Sub-team 1 and realised
by the architecture in §4.

---

## 4. Target Architecture

_Source: T3 architecture overview; `MICROKERNEL_ARCHITECTURE.md`; ABI spec; C4 diagrams._

### 4.1 Style summary

iDaVIE moves from a single-process Unity monolith to a **client–server system**:

- **Client** (Unity 6 process): VR rendering, input, presentation. Contains the Desktop GUI subsystem (MVVM), the VR Interaction subsystem, the VR Rendering subsystem, and the anti-corruption layer.
- **Server** (micro-kernel): data, domain logic, plug-in execution, long-running computation.

The seam between them is a **Service Gateway** speaking **JSON-RPC 2.0 over named pipes**
locally, with a **gRPC** upgrade path for remote streaming. Today both sides share one Unity
process; the interface layer is the seam for a future out-of-process split.

### 4.2 C4 view

- **Level 1 (context):** `iDaVIESystem` = `iDaVIEClient` + `iDaVIEServer`.
- **Level 2 (containers):** client-side — Desktop GUI (Sub-team 6), VR Interaction (Sub-team 4), VR Rendering (Sub-team 3), ACL (Sub-team 1); server-side — Domain, Application, Infrastructure, and Plug-in Host layers, plus the Service Gateway.
- **Level 3 (components):** Sub-team 6 contributes the Unity-client L3 (View / ViewModel / Gateway assemblies with the six tab View–ViewModel pairs: File, Render, Stats, Sources, Paint, Debug). C4 L1/L2 are owned by Sub-team 1.

> Figures: `docs/uml/client_server_bdd.puml` (L1), `docs/uml/refactored/client_server_component_refactored.puml` (L2), `docs/sub-team-6/deliverables/D2-Architecture/architecture.md` C4Component block (L3).

> 🟠 **Authoring note:** the diagrams and the L3 chapter exist; a consolidated written C4
> **L1/L2** narrative still needs to be lifted from the pumls + `MICROKERNEL_ARCHITECTURE.md`
> into Appendix C before freeze. (`architecture-overview.md` is the ADR log, not the C4 prose.)

### 4.3 Micro-kernel server

The kernel is a minimal, stable core providing three services; everything domain-specific
(paint, moment maps, spectral profiles, features, voice, catalog) becomes a **plug-in**:

- **`AppKernel`** — service registry replacing all `FindObjectOfType<>()` calls; components register at `Awake()` and resolve by interface.
- **`CommandBus`** — typed event dispatcher replacing the ~200-line if/else chain in `VolumeCommandController.ExecuteVoiceCommand()`. Commands are reified records (`SetColorMapCommand`, `SetThresholdCommand`, `CropToSelectionCommand`, `ActivateToolCommand`, …).
- **Stable interfaces** — `IVolumeRenderer` (implemented by `VolumeDataSetRenderer`) and `IInputSource` (implemented by `VolumeInputController`), so tools hold interfaces, never concrete classes.

Tools implement **`IAnalysisTool`** (`Name`, `VoiceCommands`, `HandleCommand`,
`BuildMenuPanel`, `Activate`, `Deactivate`) and register with a **`ToolRegistry`** that
aggregates voice phrases and builds menu tabs dynamically — eliminating the hardcoded panel
fields in `QuickMenuController`. "The kernel knows nothing about astronomy, painting, or
moment maps."

### 4.4 Layered kernel and the downward-only rule

Inside the kernel, dependencies flow **Domain → Application → Infrastructure → Plug-in Host**,
downward only:

- **Domain** — aggregate roots and value objects with **no `UnityEngine`/`SteamVR` references** (`VolumeDataSet`, `Mask`, `WCSCoordinateSystem`, `Feature`, `Workspace`). Fan-in only.
- **Application** — use cases (`LoadFitsCubeUseCase`, `ExportMaskUseCase`, `MomentMapUseCase`, `FeatureSetService`, `WorkspaceService`).
- **Infrastructure** — gateways, repositories, logging, config, texture factories.
- **Plug-in Host** — `PluginRegistry` (Service Locator; GRASP Indirection / Protected Variations — the one accepted Service-Locator exception), `PluginLoader` (hot-reload, fault isolation), `IPluginABI`.

Upward references and dependency cycles are a **CI fail condition** (§5 ADR-005, ADR-008).

### 4.5 Anti-corruption layer

A set of stable C# interfaces lets domain/application code depend on abstractions, with
Unity/SteamVR confined to adapter implementations: `IInputProvider`, `IPointer`,
`IGripInput`, `IVoiceInput`, `IHaptics`, `IVoiceRecogniser`, `IRenderPipeline`,
`IRenderContext`, `ITextureUploader`. Named adapters: `SteamVRInputAdapter` (the only file
importing SteamVR), `UnityInputSystemAdapter`, `URP/HDRP RenderPipelineAdapter`,
`UIToolkitBridge`. On the desktop side, eleven interfaces (`IFitsService`, `ILogStream`,
`IVolumeService`, `IFileDialogService`, `IConfigService`, `IMemoryProbe`, …) quarantine
`StandaloneFileBrowser`, `PlayerPrefs`, and the JSON-RPC gateway. NDepend/DV8 CI rules reject
any PR importing `UnityEngine` from a ViewModel/domain assembly.

### 4.6 Plug-in C ABI (versioned)

_Source: `refactoring-examples/sub-team-1/abi/ABI_SPEC.md` + `CONFORMANCE.md` (DRAFT v0.1.0)._

The current native DLLs (FitsReader, DataAnalysis, AstTool) are called with two binding
styles, leak third-party types (`fitsfile*`, `AstFrameSet*`) through public headers, expose
three different `Free*` functions, and suffer struct-layout drift (e.g. `spectralProfileSize`
is `int64_t` natively but `int` in C# — a latent corruption bug). The new ABI fixes this:

- **SemVer 2.0.0**: MAJOR for any breaking change (symbol removal/rename, signature/struct change, enum renumbering, calling-convention change); MINOR for backwards-compatible additions; PATCH for documentation/semantics. Compile-time constants plus a runtime `idavie_abi_version()` the host calls **before binding any other symbol**; a MAJOR mismatch refuses the load.
- **Error model**: single `idavie_status_t` (`IDAVIE_OK == 0`, positive error codes); thread-local `idavie_last_error_message()`; **no exceptions, no `errno`, no CFITSIO codes cross the boundary**.
- **Threading**: re-entrant by default (different handles, different threads safe); not thread-safe on the same handle; a progress/cancellation callback fires on the worker thread and returns `IDAVIE_ERR_CANCELLED` cooperatively.
- **Memory ownership**: two patterns only — caller-allocated (preferred) or a plug-in-allocated `idavie_buffer_t` freed by value through a single `idavie_free`. One allocator owns all cross-boundary memory, removing the Windows DLL-heap-mismatch bug class. Handles are created/destroyed by exactly one `_open`/`_close` pair and wrapped C#-side in a `SafeHandle`.
- **Conformance** (8 clauses C1–C8): version/error/free exports, defined-status-only returns, exception translation, single-allocator freeing, threading contract, clean compile under `/W4 /WX -fvisibility=hidden`, and full required-symbol resolution at load (not deferred to first call). A non-conformant plug-in is **rejected at load time**. Two CI gates run the conformance harness per-plug-in and at integration.

---

## 5. Architecture Decision Records (Summary)

Twelve ADRs (all *Proposed*, pending Architecture Guild ratification) record the decisions;
the full log with context, consequences and alternatives is **Appendix A**.

| ID | Title | LOs |
|---|---|---|
| ADR-001 | Adopt layered micro-kernel architecture | LO3, LO4 |
| ADR-002 | Introduce anti-corruption layer around Unity and SteamVR | LO3, LO4, LO6 |
| ADR-003 | Replace singleton-based services with dependency injection | LO4, LO6 |
| ADR-004 | Standardise plug-in ABI for native C/C++ extensions | LO3, LO5 |
| ADR-005 | Enforce architecture rules in CI | LO1, LO6, LO7 |
| ADR-006 | Separate domain logic from MonoBehaviour lifecycle | LO4, LO5, LO6 |
| ADR-007 | Adopt event-driven communication for cross-system integration | LO3, LO4 |
| ADR-008 | Define and enforce package/namespace dependency rules | LO3, LO7 |
| ADR-009 | Adopt MVVM for the desktop GUI client shell | LO4, LO5 |
| ADR-010 | Formalise State and Command patterns for the interaction system | LO4, LO5 |
| ADR-011 | Define Feature as a first-class domain aggregate | LO4, LO5 |
| ADR-012 | Establish a versioned workspace persistence contract | LO4, LO5, LO6 |

Four keystone ADRs in brief:

- **ADR-001 (micro-kernel)** is the root decision; all others trace to it. CK targets (WMC ≤ 20, CBO ≤ 14, LCOM ≤ 0.5) become CI fitness functions. Trade-off: added indirection and onboarding cost, accepted because the 90 fps floor is GPU-bound, not logic-bound.
- **ADR-002 (ACL)** makes pure-C# unit testing possible and confines the Unity 6 migration to adapters. Trade-off: more adapter classes; risk of leaky abstraction, mitigated by CI rules.
- **ADR-003 (DI over singletons)** removes hidden coupling; expected to deliver the largest CBO/LCOM drops. The kernel plug-in registry is the *only* permitted Service-Locator exception.
- **ADR-005 (CI enforcement)** blocks any PR that introduces a cycle, an upward layer reference, a CK breach, or a new `UnityEngine` using above Infrastructure — hardening from warnings (Day 1) to hard blocks (Day 10).

---

## 6. Worked Refactoring Examples

This is the core of the proposal (LO4/LO5). Each work package shows the before god class, the
after structure, the CK delta (verbatim from the team's deliverable, consolidated in
`metrics.md`), the patterns/principles applied, and the testability gain. **LCOM is reported
in each team's own unit** (0–1 Henderson-Sellers, or Understand's "Percent Lack of Cohesion"),
as noted per table.

### 6.1 WP1 — Architecture & Micro-kernel Core: `VolumeDataSetRenderer`

**Before.** A 1,402-LOC MonoBehaviour with ~45 methods and 152 public members acting as a hub
in the dependency graph: ray-march driver, threshold/scale/gamma parameters, foveation,
vignette, colour mapping, mask painting, moment-map orchestration, feature ownership, cursor
readback, six scattered coordinate-conversion methods, and all `FitsReader` P/Invoke calls in
`SaveMask()`. "Any class that held a reference to it pulled in the full Unity runtime."

**After.** A thin orchestrator plus six services, each behind an interface of ≤ 7 members,
with immutable value-object parameters (`RenderingParameters`, `MaskParameters`,
`FoveationParameters`, `VignetteParameters` as `readonly struct`). The single class that
touches `Material`/`Shader` is `VolumeRenderingService`; the single class calling
`FitsReader.*` is `VolumeDataExportService`; `Transform` is confined to
`VolumeCoordinateMapper`.

| CK (Sub-team 1 — before ≈ approximations; after = per-class projection) | WMC | DIT | CBO | RFC | LCOM |
|---|---|---|---|---|---|
| `VolumeDataSetRenderer` (god class) | ≈ 138 | 1 | ≈ 17 | ≈ 72 | ≈ 0.82 |
| `VolumeDataSetRenderer` (thin) | 18 | 1 | 9 | 22 | 0.20 |
| `VolumeRenderingService` | 22 | 0 | 8 | 24 | 0.15 |
| `MaskControllerService` | 12 | 0 | 4 | 14 | 0.10 |
| `VolumeCoordinateMapper` | 10 | 0 | 3 | 12 | 0.05 |
| `RegionControllerService` | 18 | 0 | 6 | 22 | 0.18 |
| `RestFrequencyService` | 9 | 0 | 3 | 11 | 0.08 |
| `VolumeDataExportService` | 10 | 0 | 5 | 18 | 0.12 |

**Delta:** WMC 138 → 22 (−84 %); CBO 17 → 9 (−47 %); RFC 72 → 24 (−67 %); LCOM 0.82 → 0.20 (−76 %).

**Principles/patterns:** SRP (six-service split), ISP (six narrow interfaces), DIP
(`RegionControllerService` receives `ICoordinateMapper`, no upward Unity dependency), Value
Object, Observer (`RestFrequencyService.FrequencyChanged`), Strategy (`IProjectionStrategy`),
Adapter/ACL (`IVolumeMaterialBinder`), GRASP Information Expert and Low Coupling.

**Testability:** plain-C# services instantiate in NUnit edit-mode with no scene; `PaintMask`
is testable with only a `VolumeDataSet` stub, making the ≥ 70 % domain branch-coverage target
reachable.

> Figures: `refactoring-examples/subteam-1/god-classes/05-volume-data-set-renderer.puml` (responsibility clusters); narrative in `…/VolumeDataSetRenderer/justification.md`.

### 6.2 WP2 — Data I/O & FITS/WCS Plug-ins: `FitsReader`

**Before.** A single static `FitsReader` (≈ 59 methods) carrying a hard `using UnityEngine`,
mixing file lifecycle, header, image, table and mask concerns; `NativePluginLoader` was a
MonoBehaviour singleton tying plug-in loading to the Unity scene; `Vector3Int` leaked Unity
into the P/Invoke binding (the `UpdateMaskVoxel` violation).

**After.** SRP split into five focused static classes — `FitsFile`, `FitsHeader`, `FitsImage`,
`FitsTable`, `FitsMask` — plus a static (Unity-free) `NativePluginLoader` whose only Unity
touch-point is a single `PluginBootstrapper` MonoBehaviour. Legacy whole-cube methods are kept
`[Obsolete]` (OCP). A `WcsTransformer` wraps AstTool behind `TransformPoint`/`InverseTransformPoint`
(still uses `Vector3` — a noted residual coupling to sever via the `RawVolumeData` boundary).

| CK (Sub-team 2 — NDepend; LCOM 0–1) | WMC | DIT | CBO | RFC | LCOM |
|---|---|---|---|---|---|
| `FitsReader` (before) | 55 | 1 | 27 | 126 | 1.0 |
| `DataAnalysis` (before) | 13 | N/A | 39 | 22 | 1.0 |
| `FitsFile` (after) | 2 | N/A | 7 | 2 | 0 |
| `FitsHeader` (after) | 5 | N/A | 13 | 20 | 0 |
| `FitsImage` (after) | 0 | N/A | 7 | 0 | 0 |
| `FitsTable` (after) | 7 | N/A | 12 | 28 | 0 |
| `FitsMask` (after, heaviest) | 41 | N/A | 19 | 82 | 0 |

**Delta:** the 55-WMC / 126-RFC / 1.0-LCOM monolith becomes five classes; the heaviest
successor (`FitsMask`) is WMC 41 / RFC 82, and **LCOM falls to 0 across all of them**.

**Principles/patterns:** SRP (five boundaries), Strategy (`IDownsampleStrategy` for
downsampling), ISP, OCP (`[Obsolete]` legacy + new sub-image extension points), DIP
(loader de-MonoBehaviour-ed), GRASP Protected Variations (`[PluginAttr]` reflection loading).

**Testability:** `NativeLibrary.SetDllImportResolver`/`SetDllDirectory` allow loading native
DLLs in a plain .NET runner; tests split into unit (no DLL) and integration (requires
`idavie_fits.dll`). `WcsTransformer`'s `Vector3` dependency is the remaining blocker.

> Honesty note (citable for LO8): an earlier AI-drafted design invented `IFitsPlugin`/`IAstPlugin`/`PluginRegistry` types that do not exist in the codebase; the document was discarded after a human check. No diagrams (`.puml`) exist for WP2; the CK source is `…/Fits Reader/metrics/Metrics Comparison.pdf`.

### 6.3 WP3 — Rendering Engine: `VolumeDataSetRenderer` + `MaskMode`

**Before (Understand-measured, Day 2).** The same 1,403-LOC renderer, here measured at
WMC 97 / CBO 28 / RFC 97 / LCOM 0.95 across eight responsibility clusters; `_startFunc()` is
185 lines (CC 28), `SaveMask()` 83 lines (CC 19); 4× `FindObjectOfType`;
`Graphics.DrawProceduralNow` (URP-incompatible); inside a 46-file cycle with 39.8 %
propagation cost. The mask-mode switch (lines 1072–1094) requires editing four files to add
one mode.

**After — Example 1 (four-class split).** `VolumeRendererBehaviour` (MonoBehaviour shell /
composition root), `VolumeRenderCoordinator` (pure-C# orchestrator that "never calls `new` on
a concrete domain type"), `VolumeMaterialBinder` (7-member interface), `VolumeTextureManager`
(6-member; sole owner of `Texture3D` and the 368 MB budget), `VolumeCameraDriver`,
`FoveatedSamplingPolicy`, `VolumeCoordinateService`, and an `IRenderPipeline` ACL
(`Urp`/`Hdrp`).

**After — Example 2 (MaskMode Strategy).** `IMaskMode` (2 members) with `ApplyMaskMode`,
`InverseMaskMode`, `IsolateMaskMode`, `DisabledMaskMode` (Null Object), `NullMaskMode` (test
double).

| CK (Sub-team 3 — Understand; LCOM 0–1) | WMC | DIT | CBO | RFC | LCOM |
|---|---|---|---|---|---|
| `VolumeDataSetRenderer` (before) | 97 | 2 | 28 | 97 | 0.95 |
| `VolumeRenderCoordinator` | 11 | 1 | 15 | 11 | 0.69 |
| `VolumeMaterialBinder` | 10 | 1 | 12 | 10 | 0.57 |
| `VolumeTextureManager` | 12 | 1 | 4 | 12 | 0.67 |
| `VolumeCameraDriver` | 4 | 1 | 4 | 4 | 0.25 |
| `VolumeCoordinateService` (static) | 3 | 1 | 3 | 3 | 0.00 |
| `FoveatedSamplingPolicy` | 6 | 1 | 6 | 6 | 0.33 |
| `ApplyMaskMode` / `InverseMaskMode` / `IsolateMaskMode` / `DisabledMaskMode` | 2 | 1 | 2 | 2 | 0.00 |

**Delta:** WMC 97 → 12 (worst successor, −85); RFC 97 → 12; CBO 28 → 15; LCOM 0.95 → 0.69.
The residual LCOM on the coordinator/binder is documented as a lifecycle-phase artefact
(Initialise/Tick/Dispose), not disjoint concerns.

**Principles/patterns:** SRP, OCP (`IRenderPipeline`, `IMaskMode`), ISP (152-member surface →
≤ 7), DIP (constructor injection; `IRenderPipeline` abstracts URP/HDRP), LSP (mask modes
substitutable), GRASP Protected Variations/Indirection/Information Expert. A correctness win:
`SubmitMaskGeometry` replaces `OnRenderObject`, which URP never invokes — the mask draw was
"silently broken on any URP project."

**Testability:** coordinate math takes `Matrix4x4` (pure NUnit); material binding tested via
`NullRenderPipeline`; budget logic is pure integer arithmetic; foveation via `StubGazeProvider`;
mask-keyword mutual-exclusion invariant ("exactly one keyword active") tested across a
six-mode round-robin.

> Figures: `docs/team3/diagrams/{class-before,class-after,vdsr-dependencies,sequence-render-frame-before,sequence-render-frame}.puml`.

### 6.4 WP4 — Interaction System (VR, voice): `VolumeInputController` + `VolumeCommandController`

**Before.** `VolumeInputController` (~1,635 LOC) is a god class: SteamVR wiring + locomotion +
interaction FSM + painting + cursor UI + vignette + quick-menu attachment, with two state
machines in one file (a `LocomotionState` enum-switch in `Update()` and a Stateless-library
`InteractionStateMachine`). Direct SteamVR types, scene `Find`, and `Camera.main` make it
untestable outside SteamVR Play mode; a bug applied `VignetteIntensity` twice per frame.
`VolumeCommandController.ExecuteVoiceCommand` is a ~190-line if/else chain owning
`KeywordRecognizer` directly.

**After.** `VolumeInputController` becomes a thin SteamVR router whose `Update()` delegates to
`_locomotionController` and `_vignetteController`. Six collaborators are extracted behind
interfaces — `ILocomotionController`, `IInteractionController`, `IBrushController`,
`IVignetteController`, `ICursorInfoFormatter` (pure C#), `IQuickMenuPositioner` — plus the
cross-team `IGaze`/`IGazeProvider` (`CameraGazeProvider`) consumed by Sub-team 3, and a
`SteamVRInputBridge` that is the *only* class containing SteamVR code. The voice path is
reified: `IVoiceRecogniser`/`WindowsVoiceRecogniser`, `IVoiceCommand`/`DelegateVoiceCommand`,
`VoiceCommandRegistry`, `IVoiceCommandContext`, `VoiceDesktopUiSync`. The duplicate-vignette
bug is fixed in the extraction.

| CK (Sub-team 4) | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| `VolumeInputController` (before) | 79 | not recorded | not recorded | 31 | 79 | not recorded |
| After | — | — | — | — | — | — |

> 🔴 **Honest gap (after-CK not recorded).** Team 4 built the refactored example (router + 7
> interfaces + implementations + three test files, present at
> `Team-4-examples/koffiewinkel-refactored/`) but **did not measure post-refactor CK values**,
> and the example currently sits **outside** the standard `refactoring-examples/sub-team-4/`
> tree (spec §10.3). Per the assignment's rule that projected snapshots must be
> evidence-backed, we present the before figures and the qualitative improvement only; no
> after numbers are fabricated. **Pre-freeze action:** relocate the folder and run Understand
> on the refactored classes.

**Principles/patterns:** Facade (router keeps the public API), Strategy (locomotion modes),
State (`IInteractionController` Stateless FSM; `ILocomotionState`/`IdleState`/`MovingState`
skeletons), Adapter (`IGaze`/`CameraGazeProvider`, `SteamVRInputBridge`), Command + Registry
(voice), Context Object, DI. Domain events (`GripEvent`, `PinchEvent`, `MenuButtonEvent`)
decouple SteamVR from domain logic.

**Testability:** `CursorInfoFormatter` is pure C#; `MockGazeProvider` enables gaze tests
without VR hardware; `GazeProviderTests`, `CollaboratorTests`, `VoiceCommandTests` exist.

> Figures: `docs/uml/VolumeInputController.puml` (before), `docs/uml/refactored/VolumeInputController_refactored.puml` (after).

### 6.5 WP5 — Feature System & Domain Model: `MomentMapRenderer` + `VoTableSaver`

**Example 1 — `MomentMapRenderer`.** Before: a MonoBehaviour mixing pure calculation, GPU
rendering and UI plotting, with `Config.Instance`, `ComputeShader`/`RenderTexture`, a direct
`AstTool.Transform3D` `IntPtr` call, and the wrong namespace. After: a three-layer split with
a pure `MomentMapCalculator` used as test ground-truth against the GPU adapter.

| CK (Sub-team 5 — Understand; RFC ~ estimated; LCOM 0–1) | WMC | DIT | CBO | RFC | LCOM |
|---|---|---|---|---|---|
| `MomentMapRenderer` (before) | 27 | 2 | 17 | ~22 | 0.48 |
| `MomentMapCalculator` (Domain, static) | 22 | 1 | 2 | ~3 | 0.0 |
| `MomentMapService` (Application) | 4 | 1 | 5 | ~2 | 0.0 |
| `MomentMapRequest` / `MomentMapResult` (DTOs) | 6 / 5 | 1 | 2 | ~9 | 0.0 |
| `MomentMapRendererAdapter` (Infra, Unity) | 19 | 2 | 14 | ~8 | 0.75 |

**Example 2 — `VoTableSaver`.** Before: a static class whose single 90-line method (CC 7) did
document building, headers, rows and I/O at once — "the numbers look clean only because there
is one method." After: ports-and-adapters with `IVoTableExporter`/`ICoordinateTransformer`/
`IAstFrame` (the last hiding `IntPtr` so "domain code never sees IntPtr"), the method split
into `BuildDocument`/`BuildHeaders`/`BuildRow`/`Export`.

| CK (Sub-team 5) | WMC | DIT | CBO | RFC | LCOM |
|---|---|---|---|---|---|
| `VoTableSaver` (before) | 7 | 1 | 6 | ~1 | 0.0 |
| `FeatureCatalog` (Domain) | 4 | 1 | 3 | ~2 | 0.0 |
| `VoTableExportService` (Infra) | 14 | 1 | 8 | ~4 | 0.0 |

**Principles/patterns:** Ports & Adapters / Hexagonal, DIP, SRP, OCP (new export format = new
`IVoTableExporter`), ISP, Law-of-Demeter correction (breaks `renderer.VolumeRenderer.SourceStatsDict`).
Aligns with ADR-002/003/006/008/011. **Testability:** `MomentMapCalculator` (CBO 0) and
`VoTableExportService` (only `System.Xml.Linq`) run in NUnit with stub transformers; the
former is the GPU adapter's test oracle.

> Figures: `refactoring-examples/sub-team-5/example-1-moment-maps/diagram.puml`, `…/example-2-votable-export/diagram.puml`, `docs/team-5/UML Diagrams/*`. (RFC values were not exported by Understand and are estimates; LCOM ≈ 0.48 "before" is borderline.)

### 6.6 WP6 — Desktop GUI & Client Shell: File tab + Debug tab (`CanvassDesktop`)

**Example 1 — File tab.** Before: `CanvassDesktop`, a 1,899-LOC MonoBehaviour mixing file I/O,
FITS axis logic, slider wiring, subset-bounds maths, coroutine lifecycle, scene-graph
mutation, native-memory cleanup and button wiring, with smells S1 (`IntPtr` FITS handle leak),
S3/S4 (`transform.Find`/`FindObjectOfType`), S5 (direct field writes onto the renderer), S6
(busy-wait coroutine). After: an 8-class MVVM split across a pure-C# Domain package
(`IFileTabViewModel`/`FileTabViewModel`, `SubsetBoundsViewModel`, four service interfaces,
`IFitsHandle`, DTOs, command infrastructure) and a Unity Adapters package (`FileTabView`,
`FitsServiceAdapter` over JSON-RPC with an opaque `RemoteFitsHandle`, `FileDialogServiceAdapter`,
`VolumeServiceAdapter`, `MemoryProbeAdapter`, `FileTabCompositionRoot`).

| CK (Sub-team 6 — Understand; RFC = method count; LCOM %) | WMC | DIT | CBO | RFC | LCOM % |
|---|---|---|---|---|---|
| `CanvassDesktop` (before) | 63 | 2 | 30 | 63 | 95 % |
| `FileTabViewModel` (worst successor) | 40 | 1 | 19 | 40 | 91 % |
| `FitsServiceAdapter` | 6 | 1 | 7 | 6 | 33 % |
| `FileTabView` | 8 | 2 | 14 | 8 | 69 % |
| `FileTabCompositionRoot` | 2 | 2 | 12 | 2 | 33 % |

**Delta:** WMC 63 → 40 (−37 %); CBO 30 → 19 (−37 %); LOC 1,899 → ~450 (−76 %);
**dependency cycles 2 → 0**; **0 → 47 NUnit tests** on the domain layer. The residual
`FileTabViewModel` LCOM 91 % is an MVVM property-backing-field artefact (one cohesive concern,
17 backing fields each touched by ≤ 2 methods), structurally distinct from the god class's
95 % (genuinely disjoint clusters).

**Example 2 — Debug tab.** Before: `DebugLogging`, a 255-LOC MonoBehaviour subscribing to the
static `Application.logMessageReceived` — "no interface, no test double possible." It passes
5/6 CK thresholds; only LCOM hs ≈ 0.95 fails. After: an Observer split — `ILogStream`/
`ILogObserver`/`LogStream`, `DebugTabViewModel` (implements `ILogObserver`), immutable
`LogEntry` record, and a `GatewayLogStreamAdapter` receiving server-pushed `log.emit`
notifications (no `UnityEngine`).

| CK (Sub-team 6 — Understand; LCOM %) | WMC | DIT | CBO | RFC | LCOM % |
|---|---|---|---|---|---|
| `DebugLogging` (before) | 8 | 4 | ~10 | ~25 | ~95 % |
| `DebugTabViewModel` | 6 | 1 | 2 | 6 | 66 % |
| `LogStream` | 4 | 1 | 3 | 4 | 25 % |
| `GatewayLogStreamAdapter` (worst) | 8 | 1 | 5 | 8 | 72 % |
| `DebugTabView` | 3 | 2 | 7 | 3 | 41 % |

**Delta:** LOC 255 → 86 (−66 %); CBO ~10 → 7 (−30 %); domain ViewModel CBO 9 collaborators →
1; **0 → 35 NUnit tests** running in ~20 ms. This is framed honestly as a *testability*
refactor, not a metric one.

**Principles/patterns:** MVVM, ACL, Composition Root (Pure DI), Command, Observer, IDisposable
symmetric lifetime, SRP/OCP/DIP/ISP. **Testability:** both domain layers compile with **zero
`UnityEngine` references** (verified by `dotnet build`).

> Figures: `docs/uml/CanvassDesktop.puml`, `docs/uml/refactored/CanvassDesktop_refactored.puml`, `docs/sub-team-6/deliverables/D2-Architecture/concern-map.puml`, and Mermaid blocks in `refactoring-examples/sub-team-6/{file-tab,debug-tab}/*.md`.

### 6.7 WP7 — Persistence & Workspace State

**Before.** No durable session persistence exists beyond writing the FITS mask file; render
settings, features, camera and the cube itself were neither saved nor recovered. The original
`ExitController` directly instantiated `VolumeDataSet`/`FeatureSetManager`, so it "could only
run inside Unity and was coupled to the internals of multiple MonoBehaviours."

**After.** A four-layer, Unity-free persistence library
(`refactoring-examples/Persistence/`): a `WorkspaceAggregate` capturing an immutable
`WorkspaceSnapshot` (a `sealed record` with a `WorkspaceMetadata` envelope: semver schema
version, UTC timestamp, SHA-256 checksum, profile, partial-recovery flags); a
`SaveWorkspaceUseCase` (collect → stub → validate → serialize → ring-push, guarded against
concurrent saves); an `AutosaveService` (`System.Threading.Timer`, 20 s default, `TriggerNow`);
a `SnapshotRing` (FIFO, atomic `.tmp`→rename write); a `SnapshotSerializer` (two-pass SHA-256,
mismatch → `InvalidDataException`); a `MigrationCoordinator` (semver chain, `Migration_1_0_to_1_1`);
a `CrashDetector` (`session.lock`) and `CrashRecoveryOrchestrator`/`RestoreOrchestrator`
(strict restore order: Data I/O → Rendering → Features → Interaction → GUI); a
`WorkspaceValidator` (35 clamping/default rules). Five `WorkspaceSlice` adapter interfaces let
each sub-team declare its own state.

> 🟠 **Honest gap (no measured CK).** WP7 recorded only **NFR3 target thresholds**, not a
> measured `Config.cs`/`ExitController.cs` baseline or post-refactor values. Targets below;
> no measurements are fabricated.

| NFR3 target (Sub-team 7) | WMC | CBO | LCOM |
|---|---|---|---|
| Workspace snapshot aggregate | ≤ 5 | 0 | ≤ 0.1 |
| Snapshot serialiser / repository | ≤ 10 | ≤ 5 | ≤ 0.3 |
| State collection orchestrator | ≤ 15 | ≤ 8 | ≤ 0.4 |
| Autosave service | ≤ 8 | ≤ 4 | ≤ 0.3 |
| `Config` (post-split) | ≤ 3 | 0 | ≤ 0.1 |

**Principles/patterns:** DIP (use cases depend on `IWorkspaceStateCollector`, not Unity), SRP
(load produces a validated snapshot; restore applies it), OCP (new slices = new interfaces),
ISP (five narrow adapters), schema-migration Strategy. **Testability:** the whole pipeline
runs in a plain NUnit project (NFR1) — 78 tests across validator, ring, serializer, stub
factory and migration (§8).

> **Pre-freeze action:** run Understand on `Config.cs`/`ExitController.cs` (before) and the new aggregate/serialiser/autosave classes (after) to convert the targets into a measured delta.

### 6.8 Worked-example coverage at a glance

| WP | Subject | Before measured | After measured | Status |
|---|---|---|---|---|
| 1 | `VolumeDataSetRenderer` | ✅ (≈) | ✅ projection | Complete |
| 2 | `FitsReader` | ✅ NDepend | ✅ NDepend | Complete |
| 3 | `VolumeDataSetRenderer` + `MaskMode` | ✅ Understand | ✅ projection | Complete |
| 4 | `VolumeInputController` | ✅ | ❌ not recorded | **Before only** |
| 5 | `MomentMapRenderer` + `VoTableSaver` | ✅ | ✅ | Complete |
| 6 | File tab + Debug tab | ✅ | ✅ | Complete |
| 7 | Persistence (`Config`/`ExitController`) | ❌ targets only | ❌ targets only | **No measurements** |

---

## 7. Consolidated Metrics and Projected Improvement

_Source: `metrics.md` (full per-class tables in Appendix B)._

Across the five fully-measured work packages, every refactoring drives the dominant CK
offenders toward the spec thresholds:

- **WMC** falls sharply on every god class: 138 → 22, 97 → 12, 63 → 40, 27 → 22 (worst successors).
- **LCOM** is the most consistent win — `FitsReader`'s assembly drops to **0** across all five split classes, and `MomentMapCalculator`/`MomentMapService`/`VoTableExportService` all reach 0.
- **RFC** drops 72 → 24 (WP1) and 97 → 12 (WP3).
- **Dependency cycles** go **2 → 0** on the desktop slice; the 46-file rendering cycle is broken.

Two honest caveats travel with these numbers, exactly as recorded in `metrics.md`:

1. **WP4 has no after-values** — the Interaction refactoring is real (code + tests exist) but post-refactor CK was never measured.
2. **WP7 has no measurements at all** — only NFR3 targets were defined.

Mixed provenance is also disclosed: WP1/WP3 "after" figures are per-class **projections**;
WP2 is NDepend tool-measured; WP5/WP6 are Understand tool-measured (WP5 RFC estimated; WP6 RFC
= method count, ~2–4× lower than traditional CK RFC). LCOM is reported per team in its own unit.
A residual handful of after-classes still exceed a single threshold (e.g. `VolumeRenderingService`
WMC 22; `FileTabViewModel` LCOM 91 %; adapter LCOMs), each with a documented structural cause
rather than a hidden defect.

At the system level, the proposal's CK targets become **CI fitness functions** (§5 ADR-005),
so the projected improvements are not one-off claims but gates that prevent regression.

---

## 8. Testability Strategy and CI Quality Gates

The single biggest testability lever is the ACL (ADR-002) + DI (ADR-003) + MonoBehaviour
separation (ADR-006): once domain logic is pure C#, it is unit-testable without Unity. The
worked examples already realise this — **47** file-tab tests, **35** debug-tab tests, **78**
persistence tests, plus the rendering and interaction suites — all where **zero** tests were
previously possible.

**Layered test model (representative, Sub-team 7).**
- *Tier 1 — pure NUnit (no Unity):* domain/application/infrastructure as a .NET 8 library; `dotnet test` runs in seconds in any CI runner, hermetically (temp dirs per test).
- *Tier 2 — Unity Edit/Play mode:* full stack incl. adapters, no VR hardware (autosave-interval, ring-eviction, crash-lock-via-OS-kill).
- *Tier 3 — scenario/UX:* VR flows (select → crop → paint → save).

**Dependency isolation.** Every Unity adapter implements an Application-layer interface;
collaborators are injected (often via deferred factories, e.g. `Func<IWorkspaceStateCollector>`).
Test doubles: `NullRenderPipeline`, `StubGazeProvider`, `MockGazeProvider`, `NullMaskMode`,
`StubCoordinateTransformer`.

**Property-based / contract tests.** WCS round-trip and FITS read/write (Sub-team 2); mask-mode
keyword mutual-exclusion across a six-mode round-robin (Sub-team 3); migration field-preservation
(Sub-team 7); feature-statistics invariants (centroid in bbox, flux ≥ 0, W20 ≤ W50, Sub-team 5).

**CI quality gates (ADR-005, T6 pipeline).** GitHub Actions runs on every PR and push:
`build-dotnet` (fail-fast) → `arch-tests` (forbid `UnityEngine.*` in domain), `ck-metrics`,
`circular-deps`, `sonar`; plus `unity-tests`, `persistence-tests`, `diagram-validation`
(warn-only), and a `human-review-required` blocking gate on diagram/doc/PDF diffs. Gates harden
from warnings (Day 1) to hard blocks on cycles, layer violations and CK breaches (Day 10). The
pipeline posts a CK before/after delta as a PR comment.

**Coverage targets:** ≥ 70 % branch+line on domain/ViewModel layers, ≥ 50 % overall; Unity-bound
code tracked but not strictly targeted. Closing the baseline coverage gap (§2.1) by importing
Unity Test Runner results into the NDepend pipeline unlocks the three skipped coverage gates.

---

## 9. Unity 5 → Unity 6 Migration Plan

The architecture is designed so the migration is **routine** because the migration surface is
confined to the Infrastructure and adapter layers. The incremental path (from
`MICROKERNEL_ARCHITECTURE.md` §4) keeps the application functional throughout:

| Phase | Change | Risk |
|---|---|---|
| 1 | Introduce `AppKernel`; migrate all `FindObjectOfType<>()` to registry lookups | Low |
| 2 | Introduce `CommandBus` + typed commands; voice dispatch publishes commands | Low |
| 3 | Replace `Transform.Find()` chains with UI subscriptions on `IVolumeRenderer` events | Medium |
| 4 | Extract `IVolumeRenderer` from `VolumeDataSetRenderer` | Low |
| 5 | Extract `IInputSource`; make the interaction FSM private | Medium |
| 6 | Lift `PaintTool` to `IAnalysisTool` (first full tool extraction) | Medium |
| 7 | Lift remaining tools one by one | Low per tool |
| 8 | `QuickMenuController` builds tabs dynamically from `ToolRegistry` | Medium |

The specific Unity-6 targets each land behind an adapter: **Input System** via
`UnityInputSystemAdapter`/`IInputProvider` (ADR-002, ADR-010); **URP/HDRP** via `IRenderPipeline`
(the `OnRenderObject` → `SubmitMaskGeometry` fix already prepares this); **UI Toolkit** via the
MVVM Views and `UIToolkitBridge` (ADR-009). Because domain code has no `UnityEngine` usings,
the Unity-6 lifecycle and managed-stripping changes touch only the thin adapter shells.

---

## 10. Cross-Sub-team Integration

_Source: T7 integration overview; full register in `integration-risk-register.md` (Appendix)._

Integration happens through **interface contracts and namespace boundaries**, not shared object
graphs (ADR-007, ADR-008). Each sub-team merged its work via PRs gated by the shared CI
pipeline; the contribution map and contract status are maintained by the Integration Lead.

**Contract status (selected):**

| Contract | Owners | Status |
|---|---|---|
| `RawVolumeData` (voxel handoff) | 2 → 3 | ✅ Resolved (2 Jun) |
| `IGaze` / `IGazeProvider` | 4 → 3 | ✅ Resolved (2 Jun) |
| `ISessionPersistenceService` / `VolumeSessionState` | 7 ↔ 3 | ⏳ Awaiting sign-off |
| `IServiceGateway` / JSON-RPC ABI | 1 → 3,4,5,6 | ⚠️ Stub — to be ratified by Sub-team 1 |
| Feature / GUI state contracts | 5 / 6 | Drafted |

**Highest open risks:** R01/DEPS-1 (service-gateway contract not yet frozen; clients code
against a placeholder stub), R02 (plug-in ABI semver not yet enforced in CI), R05
(`UnityEngine.*` leakage — mitigated by the `arch-tests` fitness function), R06 (CK/Sonar
tooling not fully operational). These are tracked with owners in the register and are the
candid subject of §11.

---

## 11. Trade-offs and Risk

The proposal is deliberately honest about its costs:

- **Indirection vs. simplicity.** The micro-kernel, ACL and DI add interfaces and classes (e.g. `CanvassDesktop`: 8 → ~25 classes). Accepted because the alternative entrenches every known defect and makes the Unity 6 migration high-risk; the 90 fps floor is GPU-bound, so the abstraction overhead is not on the hot path.
- **Service Locator exception.** Permitted *only* at the kernel plug-in boundary; general use is rejected as reintroducing hidden coupling (ADR-003).
- **Event-driven debugging.** Domain events reduce coupling but make control flow implicit; mitigated by an observable event bus in dev mode and structured logging (ADR-007).
- **Residual metric exceedances.** A few after-classes still breach one threshold each (WMC 22, LCOM 91 %, adapter LCOMs); each is documented with a structural cause and, where relevant, a named remediation (e.g. extract a `FileTabCommands` helper).
- **Honest evidence gaps.** WP4 lacks after-CK and WP7 lacks measurements (§6.4, §6.7); both are open pre-freeze actions rather than concealed. The JSON-RPC transport adds latency for high-frequency calls, mitigated by batching / in-process shortcuts.
- **Tooling reality.** NDepend would not analyse the Unity project without `Assembly-CSharp.dll`; some teams fell back to static source analysis and the Understand tool (logged as retro items). This is disclosed rather than smoothed over.

---

## 12. AI Usage and Conclusion

_Source: T8 AI-tool usage log and reflection (full document; per-team logs in `ai-tool-log/`)._

AI tools (Claude Code on Sonnet 4.6; Claude chat on Opus 4.6/4.7/4.8; Cursor; Perplexity;
Gemini) were used across the lifecycle, with ~183 logged entries. The strongest wins were
**analytical and structural**: SOLID/CK audits of the god classes, skeleton scaffolding for the
refactored layers, and fast PlantUML/Mermaid generation — "AI turned ~800-line source files
into navigable responsibility maps in minutes." It also caught real defects (e.g. the inverted
memory leak in `VolumeDataSet.cs:431–434`).

The failures are logged as carefully as the wins, because they are the evidence that the human
is the author of record: **hallucinated artefacts** (invented `IFitsPlugin`/`PluginRegistry`
types — whole documents discarded); **unverifiable numbers** (AI cannot compute CK; it
miscounted DIT and confused LCOM4 with the normalised LCOM, putting files on the wrong scale);
**cross-team confusion** (sub-team-number mislabelling); and **tooling dead-ends** (confident
but wrong NDepend/npm advice costing hours). The team's policy: "We treated AI metric and
architecture output as a hypothesis to verify, never as a source of truth," and AI was not used
for peer-rating, contribution logs, reflections, or live defence (spec §10.5.6).

**Conclusion.** iDaVIE's maintainability debt is real, measurable and on a sub-two-year
breaking-point trajectory that the Unity 6 migration will accelerate. Team Alpha's proposal
adopts the mandated client–server micro-kernel architecture, isolates Unity behind an ACL,
versions the native plug-in ABI, and proves the approach with worked examples that move the
dominant CK metrics decisively toward target — most strikingly LCOM to 0 on the FITS plug-in
and a 2 → 0 cycle reduction with 47 new tests on the desktop shell. We are candid about the two
evidence gaps and the open integration contracts. We ask the panel to adopt this proposal as
the basis for the structured refactoring of iDaVIE.

---

## Appendices (not counted toward the 60-page limit)

| App | Content | Source |
|---|---|---|
| A | Full 12-ADR log (context, consequences, alternatives) | `architecture-overview.md` |
| B | Full per-class CK tables, all work packages, with provenance | `metrics.md` + per-team `CK_Metrics.md` |
| C | Full C4 (L1–L3) + all before/after UML, sequence and dependency diagrams | `docs/uml/**`, `docs/team3/diagrams/**`, `refactoring-examples/**` |
| D | Plug-in ABI specification + conformance suite | `refactoring-examples/sub-team-1/abi/` |
| E | Full baseline NDepend export | T2 + `Inital Maintainabily Benchmark/` |
| F | AI-tool usage log (all entries) | T8 + `ai-tool-log/*.md` |

---

### Pre-freeze action list (Thu 4 June 11:00)

1. 🔴 **WP4:** relocate `Team-4-examples/` into `refactoring-examples/sub-team-4/` (§10.3) and run Understand on the refactored classes to record after-CK.
2. 🟠 **WP7:** measure `Config.cs`/`ExitController.cs` (before) and the new persistence classes (after) to convert NFR3 targets into a delta.
3. 🟠 **§4:** author the consolidated C4 L1/L2 prose into Appendix C from the pumls + `MICROKERNEL_ARCHITECTURE.md`.
4. Freeze the `IServiceGateway` JSON-RPC contract (R01/DEPS-1) and sign off the 7↔3 persistence contract.
5. Render all diagrams from source `.puml`/`.mmd` and embed/attach as figures.
6. Humanising/defensibility pass on all prose (§10.5.3); confirm every section has a named owner who can defend it.
7. Export to PDF; confirm body ≤ 60 pp with appendices excluded.
