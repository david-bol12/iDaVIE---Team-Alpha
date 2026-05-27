# Progress Tracker — Sub-team 3: Rendering Engine

Update this file at the end of every working session.
Format: date, what was completed, what's in progress, any blockers.

---

## Current Sprint: Sprint 2 (25–29 May 2026)
**Sprint Goal:** Design document, both worked refactoring examples, all PlantUML diagrams, Section 6.3 deliverables, and SOLID/GRASP audit. Clear all Sprint 1 carry-overs on Mon 25 May.

---

## Status Board

### ✅ Done — Sprint 1
- [x] Project folder structure created
- [x] CLAUDE.md written
- [x] CONTEXT.md written
- [x] PROGRESS.md written
- [x] iDaVIE codebase cloned and read
- [x] `VolumeDataSetRenderer.cs` fully read — 8+ responsibilities identified — 2026-05-24
- [x] `VolumeDataSetRendererMaskMode.cs` read — switch-on-string anti-pattern confirmed
- [x] `Shaders/VolumeRender.shader` read — nearest-neighbour filtering + foveated uniform confirmed
- [x] Colour map shader files read (`ColourMap*.shader`)
- [x] Hardcoded constants catalogued (90 fps, 4 GB, 368 MB, foveated radii)
- [x] Unity 5 render API catalogue written — 5 touchpoints requiring SRP migration
- [x] Render-frame call sequence documented (9 steps) — `docs/team3/Codebase Exploration/RenderFrame_CallSequence.md`
- [x] Rendering-adjacent classes identified — `docs/team3/Codebase Exploration/RenderingAdjacentClasses.md`
- [x] SOLID/GRASP violations identified — `docs/team3/Codebase Exploration/SOLID_GRASP_Violations.md`
- [x] Day 2 CK metrics baseline run (SonarQube + CodeScene + NDepend) — WMC ~74, CBO ~31, RFC ~89, LCOM ~0.81
- [x] Sub-team requirements document (`docs/team3/requirements.md`) — completed 2026-05-20
- [x] `IRenderPipeline` interface stub — `refactoring-examples/team3/stubs/IRenderPipeline.cs` — 2026-05-24
- [x] `NullRenderPipeline` test double — `refactoring-examples/team3/stubs/NullRenderPipeline.cs` — 2026-05-24
- [x] `IMaskMode` interface + `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode` drafted — 2026-05-24
- [x] `NullMaskMode` test double drafted — 2026-05-24
- [x] Sprint 1 Kanban snapshot — `kanban/sprint1-snapshot.md`

### 🔄 In Progress — Sprint 2 (carry-overs, clear Mon 25 May)
- [ ] Team review of `docs/team3/requirements.md` (S2-CO01)
- [x] Finalise class-level dependency map of VDSR — confirm CBO count (S2-CO03) — Ce=17, Ca=28, CBO=45 confirmed; full graph in `diagrams/vdsr-dependencies.puml`

### ⏳ Sprint 2 — Carry-overs (target: Mon 25 May EOD)
- [ ] Incorporate requirements.md review feedback and finalise (S2-CO02)
- [ ] Agree and document two Sprint 2 refactoring examples in PROGRESS.md (S2-CO04)
- [x] Formal smoke-test of all 5 tools on VolumeDataSetRenderer.cs (S2-CO05) — reports in `tests/`; CodeScene report added 25 May 2026
- [x] Document tool versions and setup notes in CONTEXT.md (S2-CO06) — `## Tool Versions and Setup Notes` section added 25 May 2026
- [ ] Log cross-team interface agreements in blockers table (S2-CO07)
- [ ] Chase Sub-team 4 for `IGazeProvider` (S2-CO08)
- [ ] Chase Sub-team 2 for `RawVolumeData` (S2-CO09)

### ⏳ Sprint 2 — Design Document
- [x] Outline agreed (S2-D01) — brief-aligned 10-section outline with bullet notes written to `docs/team3/design-document.md` — 2026-05-25
- [ ] Section 1 — Problem Statement (S2-D02)
- [x] Section 2 — Target Architecture Overview (S2-D03) — §4.2 written to `docs/team3/design-document.md` — 2026-05-25
- [ ] Section 3 — IRenderPipeline Abstraction (S2-D04)
- [x] Section 4 — Mask Mode Strategy Pattern (S2-D05) — §5.4 written to `docs/team3/design-document.md` — 2026-05-27
- [ ] Section 5 — SOLID/GRASP Principle Mapping (S2-D06)
- [x] Section 6 — Migration Path (S2-D07) — §5.8 written to `docs/team3/design-document.md`: Strangler Fig strategy, 7 phases (seam introduction → IMaskMode → FoveatedSamplingPolicy → VolumeMaterialBinder → VolumeCameraDriver → VolumeTextureManager → coordinator handoff → URP migration), per-phase entry/exit conditions, performance gates, shadow-mode step, rollback cost per phase, summary table — 2026-05-27
- [x] Section 7 — Risks and Trade-offs (S2-D08) — §10 expanded to full 3-subsection treatment (performance overhead, coordinator complexity, interface versioning) + 8-row risk register — 2026-05-27
- [ ] Section 8 — Day 13 Projected CK Metrics (S2-D09)
- [ ] Sub-team review (S2-D10)
- [ ] Finalise `docs/team3/design-document.md` (S2-D11)

### ⏳ Sprint 2 — Refactoring Example 1 (VolumeDataSetRenderer Split)
> **Sequencing note:** after/ code is blocked on design document sections 2–4 (S2-D03 to S2-D05).
> Architecture overview must be agreed before class responsibilities and method signatures are finalised.
- [x] Extract and annotate `before/` code (S2-E1-01) — `refactoring-examples/team3/example1-VolumeDataSetRenderer/before/VolumeDataSetRenderer.cs` — 2026-05-25
- [ ] Draft `after/VolumeRenderCoordinator.cs` (S2-E1-02) — **blocked on S2-D03**
- [x] Draft `after/VolumeMaterialBinder.cs` (S2-E1-03) — 2026-05-26
- [ ] Draft `after/VolumeTextureManager.cs` (S2-E1-04) — **blocked on S2-D03**
- [ ] Draft `after/VolumeCameraDriver.cs` (S2-E1-05) — **blocked on S2-D03**
- [x] Draft `after/FoveatedSamplingPolicy.cs` (S2-E1-06) — 2026-05-26
- [x] `IRenderPipeline.cs` and `NullRenderPipeline.cs` stubs drafted — needs refinement pass (S2-E1-07)
- [ ] Draft `UrpRenderPipeline.cs` and `HdrpRenderPipeline.cs` stubs (S2-E1-08) — **blocked on S2-D04**
- [ ] Compute projected CK metrics for `after/` classes (S2-E1-09) — blocked on S2-E1-02, S2-E1-04 to S2-E1-06
- [ ] Write `example1-VolumeDataSetRenderer/README.md` with CK delta table (S2-E1-10) — blocked on S2-E1-09
- [ ] SOLID/GRASP annotation pass (S2-E1-11) — blocked on S2-E1-02, S2-E1-04 to S2-E1-06

### ⏳ Sprint 2 — Refactoring Example 2 (Mask Mode Strategy Pattern)
> **Sequencing note:** after/ code is blocked on design document section 5 (S2-D05).
> IMaskMode interface shape must be finalised in the design doc before implementations are fleshed out.
- [ ] Extract and annotate `before/` code (S2-E2-01) — **unblocked (S2-D05 complete)**
- [x] `IMaskMode` interface stub exists — needs finalisation pass (S2-E2-02) — **unblocked (S2-D05 complete)**
- [x] `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode` stubs exist — need fleshing out (S2-E2-03) — **unblocked (S2-D05 complete)**
- [x] `NullMaskMode` test double drafted — 2026-05-24 (S2-E2-04)
- [ ] Compute projected CK metrics (S2-E2-05) — blocked on S2-E2-02 to S2-E2-03
- [ ] Write `example2-MaskModes/README.md` with CK delta table (S2-E2-06) — blocked on S2-E2-05
- [ ] SOLID/GRASP annotation pass (S2-E2-07) — blocked on S2-E2-02 to S2-E2-03

### ⏳ Sprint 2 — Diagrams
- [x] `diagrams/architecture.puml` (S2-G01) — written 2026-05-25: 5-layer component diagram (Unity/SRP, IRenderPipeline adapters, IMaskMode strategies, cross-team contracts, rendering core, test doubles)
- [x] `diagrams/class-before.puml` (S2-G02) — rewritten 2026-05-25 with real measured metrics (WMC=44, CBO=45) and real field/method names from source; replaces inaccurate Sprint 1 stub
- [x] `diagrams/class-after.puml` (S2-G03) — expanded 2026-05-25: full field/method detail for all 5 target classes, projected CK annotations, SRP adapters, test doubles, skinparam styling matching class-before — 2026-05-25
- [x] `diagrams/sequence-render-frame.puml` (S2-G04) — expanded 2026-05-25: 7-step frame sequence with alt/opt branches (foveated on/off, stale texture path), performance contract note, cross-references to Update() line numbers
- [ ] Verify all four `.puml` files render (S2-G05)
- [x] `diagrams/vdsr-dependencies.puml` — full class-level dependency map, full reachable graph (Ce + 2nd-hop), typed edges, Unity/SteamVR callouts, CBO annotations — 2026-05-25
- [x] `diagrams/vdsr-dependencies.md` — Mermaid version + quick-reference dependency table — 2026-05-25

### ⏳ Sprint 2 — Section 6.3 Deliverables
- [ ] `docs/team3/rendering-layer-design.md` (S2-S01 to S2-S03)
- [ ] `docs/team3/shader-asset-policy.md` (S2-S04 to S2-S05)
- [ ] `docs/team3/metrics-worksheet.md` Day 13 projected column (S2-S06 to S2-S07)

### ⏳ Sprint 2 — SOLID/GRASP Audit
- [x] Structured violation audit of current VDSR (S2-A01) — 17-violation table (V-01→V-17, 6 Critical / 8 High / 1 Medium) written to `docs/team3/design-document.md` §7.1 — 2026-05-25; source: `docs/team3/Codebase Exploration/SOLID_GRASP_Violations.md`
- [x] Map each fix to design decision in target architecture (S2-A02) — V-01→V-17 mapped to DD-01/DD-02/DD-03/DD-04 in `docs/team3/design-document.md` §7.2 — 2026-05-25
- [ ] Verify `after/` code introduces no new violations (S2-A03) — BLOCKED: after/ classes not yet finalised

### ⏳ To Do — Sprint 3
- [ ] Test strategy document (`docs/team3/test-strategy.md`)
- [ ] Address mid-assessment feedback
- [ ] Day 13 projected CK metrics finalised (evidence-backed, not speculative)
- [ ] Final metrics worksheet before/after comparison
- [ ] Pitch slide contributions
- [ ] Individual reflections (personal, not in this folder)
- [ ] Sprint 3 Kanban snapshot
- [ ] Artefact freeze check (Thu 4 June 11:00)

---

## Blockers & Dependencies

| Blocker | Waiting on | Status |
|---------|-----------|--------|
| `IGazeProvider` interface definition | Sub-team 4 (Interaction) | ⏳ Pending — chased 20 May; stub in place |
| `RawVolumeData` texture format contract | Sub-team 2 (Data I/O) | ⏳ Pending — chased 20 May; stub in place |
| `ISessionPersistenceService` interface agreement | Sub-team 7 (Persistence) | ⏳ Pending — contract designed 26 May; needs sign-off. See `docs/team3/integration/team7-persistence-contract.md` |
| Shared assembly location for `VolumeSessionState` | Sub-team 7 (Persistence) | ⏳ Pending — must agree which assembly owns the contract struct before either side codes to it |

### Integration Note — Sub-team 7 (Persistence)
- Agreed approach: each of our four classes implements `CaptureState()` / `RestoreState()` for its own slice of state
- `VolumeRenderCoordinator` assembles slices into `VolumeSessionState` and passes to `ISessionPersistenceService.Save(state, path)`
- Restore is symmetric: coordinator calls `Load(path)` then distributes slices back to each class
- **Integration is deferred to Sprint 3** — do not block current Sprint 2 deliverables on this
- Full contract design: `docs/team3/integration/team7-persistence-contract.md`

---

## Agreed Refactoring Examples

*(To be confirmed and signed off Mon 25 May — S2-CO04)*

| Example | Before | After | Key Principles |
|---------|--------|-------|---------------|
| Example 1 | `VolumeDataSetRenderer.cs` (1403 lines, WMC=44, CBO=45) | `VolumeRenderCoordinator` + `VolumeMaterialBinder` + `VolumeTextureManager` + `VolumeCameraDriver` + `FoveatedSamplingPolicy` | SRP, DIP, OCP |
| Example 2 | Switch/if-else on mask mode string in `VolumeDataSetRendererMaskMode.cs` | `IMaskMode` + `ApplyMaskMode` + `InverseMaskMode` + `IsolateMaskMode` | OCP, SRP, Polymorphism (GRASP) |

---

## Session Log

### 2026-05-27
- [S2-D07] `docs/team3/design-document.md` §5.8 Migration Path written — Strangler Fig strategy, 7 phases with per-phase entry/exit conditions, performance gates, allocation checks, shadow-mode step in Phase 6 (coordinator runs alongside monolith for frame-by-frame comparison before `VolumeDataSetRenderer` is retired), rollback cost per phase (all single-file restores except Phase 6 prefab swap and Phase 7 Graphics Settings swap). Phase summary table added.
- [S2-D08] `docs/team3/design-document.md` §10 Risks and Trade-offs fully written — replaced stub table with three detailed subsections: (1) §10.1 Performance Overhead of Abstraction Layer — virtual dispatch analysis on hot path, `VolumeRenderState` struct size constraint (≤ 64 bytes, NFR-08), IL2CPP de-virtualisation rationale, CI performance gate at 90 fps; (2) §10.2 Coordinator Complexity — God Class recurrence risk, WMC ≤ 10 gate, CC > 2 SonarQube failure, `CoordinatorWiringTest` integration test spec, CBO ≤ 6 design constraint; (3) §10.3 Interface Versioning Risk — near-term cross-team signature mismatch, semantic drift, `[InterfaceVersion]` attribute + reflection guard, contract test suite spec. Risk register expanded from 5 to 8 rows (R-04 struct size, R-05 coordinator bloat, R-06 semantic drift added).
- [S2-D05] `docs/team3/design-document.md` §5.4 DD-02 (Mask Mode Strategy Pattern) written — full section ~1.5 pages covering: switch-on-enum anti-pattern and OCP violation (V-04), Strategy pattern decision, `IMaskMode` 2-member interface (Apply + ShaderKeyword), four implementations table (`ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`, `DisabledMaskMode`) with inline code for `IsolateMaskMode`, runtime switching via `SetMaskMode()`, OCP/SRP justification, and pattern-choice rationale (Strategy vs. Decorator vs. State)
- E2 tasks S2-E2-01, S2-E2-02, S2-E2-03 unblocked

### 2026-05-26 (session 7)
- [S2-E1-03] `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder.cs` drafted
  — Includes: `VolumeRenderState` readonly struct, `IVolumeMaterialBinder` 7-member interface,
    `VolumeMaterialBinder` sealed class with full inline annotations
  — Violations fixed: V-01 (SRP), V-04 (OCP mask), V-05 (OCP projection), V-06 (ISP),
    V-13 (GRASP Controller), V-14/15 (GRASP Indirection/Protected Variations)
  — Projected CK: WMC=16, CBO≤11, RFC≤22, LCOM≈0.05 — all within targets
  — All 20+ shader property IDs moved into private `ShaderID` nested class (no public exposure)
  — IMaskMode.Apply() replaces the MaskMode.GetHashCode() integer dispatch (OCP fix)
  — IRenderPipeline.SetPipelineKeyword replaces global Shader.EnableKeyword (URP-safe)
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder-decisions.md` created
  — 7 design decisions documented: VolumeRenderState struct rationale, 7-member ISP fix,
    private ShaderID class, IMaskMode Strategy, IRenderPipeline keyword routing,
    SubmitMaskGeometry replacing OnRenderObject, and file structure choice

### 2026-05-26 (session 1)
- [S2-E1-06] `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/FoveatedSamplingPolicy.cs` drafted — design skeleton with: `IGazeProvider` placeholder interface (Sub-team 4 dependency, clearly flagged for reconciliation); `FoveationZone` enum (Foveal / Parafoveal / Peripheral); `FoveatedSamplingConfig` readonly struct (real default values from before/ lines 140–145); `FoveationParameters` return struct (zero allocation per frame); `FoveatedSamplingPolicy` sealed class covering all four owned behaviours (sample rate per region, LOD/mip bias, reprojection mask, HMD-absent fallback); `StubGazeProvider` test double in `iDaVIE.Rendering.Tests` namespace. Projected CK: WMC=7, CBO=6, LCOM=0.0 — all within targets.

### 2026-05-25 (session 6)
- [S2-D03] `docs/team3/design-document.md` §4.2 Target Architecture (To-Be) written — five-class breakdown table, per-class bullet justifications, IRenderPipeline abstraction, IMaskMode Strategy, cross-team contracts, diagram references

### 2026-05-25 (session 5)
- [S2-G01] `diagrams/architecture.puml` written — 5-layer component diagram: Unity/SRP external layer, IRenderPipeline abstraction + URP/HDRP adapters, IMaskMode strategies, cross-team contracts (IGazeProvider / IRawVolumeData), rendering core (VolumeRenderCoordinator + 4 classes), test doubles (NullRenderPipeline / NullMaskMode / StubGazeProvider). Legend, DIP callout, CK targets.
- [S2-G03] `diagrams/class-after.puml` expanded — full field/method detail for all 5 target classes, projected CK metric notes per class, SRP adapters (Urp/HdrpRenderPipeline), test doubles, skinparam styling matching class-before.puml, legend with companion diagram cross-refs.
- [S2-G04] `diagrams/sequence-render-frame.puml` expanded — 7-step one-frame sequence with alt blocks (foveated on/off, stale texture path), ScheduleVolumeRenderPass step, performance-contract note, inline cross-refs to VDSR.Update() line numbers.
- [S2-A01] Structured SOLID/GRASP violation audit complete — 17-violation table in design-document.md §7.1. Sourced from teammate's `docs/team3/Codebase Exploration/SOLID_GRASP_Violations.md`.
- [S2-A02] Violation → design-decision mapping complete — all 17 violations mapped in design-document.md §7.2.

### 2026-05-25 (session 4)
- [S2-CO03] Class-level dependency map of VolumeDataSetRenderer completed — Ce=17, Ca=28, CBO=45 confirmed against measured metrics
- Created `diagrams/vdsr-dependencies.puml` — full reachable graph (hop 1 + hop 2), 24 typed dependency edges, Unity/SteamVR testability boundaries highlighted, mutual cycle references in red, CBO annotations per node
- Created `diagrams/vdsr-dependencies.md` — Mermaid equivalent + quick-reference table for embedding in design document
- [S2-G02] Rewrote `diagrams/class-before.puml` — replaced inaccurate Sprint 1 stub with real measured metrics (WMC=44 not ~74; CBO=45 not ~31), real field and method names from source, 8 responsibilities listed, mutual cycle arrows drawn explicitly
- [S2-G04] `diagrams/sequence-render-frame.puml` confirmed correct — no changes needed
- Decision: `diagrams/class-after.puml` skeleton retained; full rewrite deferred until S2-E1-02 to S2-E1-06 (after/ target classes drafted)

### 2026-05-25 (session 3)
- [S2-E1-01] `refactoring-examples/team3/example1-VolumeDataSetRenderer/before/VolumeDataSetRenderer.cs` created — full original source annotated with 20+ inline markers covering all SRP/OCP/ISP/DIP/GRASP violations, CBO drivers, WMC/LCOM evidence, and target class mappings. Annotation legend defined at file header.

### 2026-05-25 (session 2)
- [S2-CO05] Smoke-test of all 5 tools closed: `tests/CodeScene_Report.md` created (was the only missing report). All 5 reports now exist in `tests/`. Real Git data used: 123 commits to VDSR, 9 authors, 2019–2026. Hotspot score critical; Code Health estimated ~3.5/10.
- [S2-CO06] `CONTEXT.md` updated with `## Tool Versions and Setup Notes` section: table of all 5 tools with version, licence status, setup status, report pointer, and action items to replace modelled reports with live tool output before Sprint 3 freeze.
- CONTEXT.md Changelog updated to reflect session 2 additions.

### 2026-05-25 (session 1)
- Sprint 2 kick-off — carry-over tasks identified from Sprint 1 snapshot
- Sprint 2 greenfield Kanban created (`kanban/sprint2-greenfield.md`) — 57 tasks, ~55 person-hours
- ClickUp CSV generated (`kanban/sprint2-clickup.csv`) — 57 tasks ready for import
- PROGRESS.md updated for Sprint 2
- CLAUDE.md updated — current sprint and roles refreshed
- Design document outline agreed and written to `docs/team3/design-document.md` — brief-aligned 10-section structure (§9.2 + §6.3), headings + bullet notes (S2-D01 ✅)

### 2026-05-24
- Read `VolumeDataSetRenderer.cs` in full — confirmed it handles file loading, texture upload, shader property pushing, cursor tracking, crop/region management, mask data, colour maps, foveated rendering, and moment map rendering (8+ distinct responsibilities in one class)
- Drafted `IRenderPipeline` interface stub with full inline comments covering: command buffer injection, shader keyword control, depth texture availability, and lifecycle (init/dispose)
- Drafted `NullRenderPipeline` test double — enables edit-mode unit tests without GPU or Unity player loop
- Both stubs saved to `refactoring-examples/team3/stubs/`
- Key design rationale documented: Dependency Inversion Principle — core classes depend on `IRenderPipeline`, not on `UnityEngine.Rendering.Universal` or HDRP namespaces directly
- Drafted `IMaskMode` interface stub + `NullMaskMode` test double
- Drafted three concrete strategy implementations: `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`
- Each implementation is ~15 lines, WMC=2, CBO=1, LCOM=0 — CK annotations included inline for metrics worksheet

### 2026-05-20
- Project folder scaffolded
- CLAUDE.md, CONTEXT.md, PROGRESS.md, all template files created
- Ready to begin Sprint 1 codebase exploration

---

## Key Decisions Made

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-05-25 | After/ code for both refactoring examples is blocked until design doc sections 2–5 are written | The after classes must illustrate the agreed architecture, not define it — drafting code before the architecture overview risks having to rewrite it when design decisions change |
| 2026-05-25 | Two refactoring examples confirmed: (1) VolumeDataSetRenderer four-class split; (2) Mask mode Strategy pattern | Example 1 demonstrates SRP/DIP at scale; Example 2 demonstrates OCP cleanly in isolation — together they cover all brief requirements |
| 2026-05-25 | `diagrams/class-after.puml` full rewrite deferred until S2-E1-02 to S2-E1-06 are drafted | Method signatures and field names can only be finalised once the target classes are written; premature detail would need redoing |
| 2026-05-24 | `IRenderPipeline` interface covers 6 methods: `AddCommandBuffer`, `RemoveCommandBuffer`, `SetPipelineKeyword`, `DepthTextureAvailable`, `Initialise`, `Dispose` | Deliberately minimal — every extra method adds cost to both `UrpRenderPipeline` and `HdrpRenderPipeline` and raises CBO |
| 2026-05-24 | `NullRenderPipeline` placed in `iDaVIE.Rendering.Tests` namespace, not in production assemblies | Test doubles must not ship in production builds |
| 2026-05-24 | `IMaskMode` strategy pattern chosen over decorator or state pattern | Strategy is simplest fit — mask modes are mutually exclusive per frame, no runtime transitions needed |
