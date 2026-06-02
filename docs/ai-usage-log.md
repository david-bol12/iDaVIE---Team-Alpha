# AI Usage Log — Damien O'Brien
## Sub-team 3: Rendering Engine | Team Alpha — Cache Me If You Can
## Project: iDaVIE Refactoring (18 May – 5 June 2026)

---

## Tool Used
**Claude Code (Anthropic)** — accessed via the Cowork integration throughout the project.

---

## Log

### 2026-05-20 — Project Setup
**Task:** Scaffold the sub-team project folder.  
**Prompt (reconstructed):** Set up the project folder structure for our sub-team, including CLAUDE.md, CONTEXT.md, PROGRESS.md, and template files for all deliverables listed in the brief.  
**Output used:** CLAUDE.md, CONTEXT.md, PROGRESS.md, and all deliverable template files created.  
**Modifications made:** Filled in team-specific details (roles, sprint dates, scope, CK metric targets).

---

### 2026-05-24 — Codebase Exploration & Interface Stubs
**Task:** Read and annotate `VolumeDataSetRenderer.cs`; draft initial interface stubs.  
**Prompt (reconstructed):** Read `VolumeDataSetRenderer.cs` in full and identify all distinct responsibilities. Then draft an `IRenderPipeline` interface covering command buffer injection, shader keywords, depth texture availability, and lifecycle. Also draft an `IMaskMode` interface with `ApplyMaskMode`, `InverseMaskMode`, and `IsolateMaskMode` strategy implementations.  
**Output used:**
- `refactoring-examples/team3/stubs/IRenderPipeline.cs`
- `refactoring-examples/team3/stubs/NullRenderPipeline.cs`
- `refactoring-examples/team3/stubs/IMaskMode.cs` (initial draft)
- `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs` (initial stubs)

**Modifications made:** Reviewed interface signatures and confirmed they aligned with design doc §5.3 and §5.4 before committing.

---

### 2026-05-25 — Sprint 2 Kick-off, Diagrams & Design Doc Outline
**Task:** Create Sprint 2 Kanban, design document outline, and PlantUML diagrams.  
**Prompt (reconstructed):** Create a Sprint 2 Kanban with ~57 tasks and estimated hours. Write a 10-section design document outline aligned with §9.2 and §6.3 of the brief. Generate PlantUML diagrams for the before-state class diagram, the class-level dependency map of VolumeDataSetRenderer, and the architecture component diagram.  
**Output used:**
- `kanban/sprint2-greenfield.md`
- `docs/design-document.md` (outline with bullet notes)
- `diagrams/architecture.puml`
- `diagrams/class-before.puml` (rewritten with real Understand metrics)
- `diagrams/vdsr-dependencies.puml`
- `diagrams/vdsr-dependencies.md` (Mermaid version)
- `diagrams/class-after.puml` (skeleton)
- `diagrams/sequence-render-frame.puml`

**Modifications made:** Verified CBO=28 count against Understand tool output before accepting the dependency map. Adjusted diagram styling and cross-references manually.

---

### 2026-05-25 — Before/ Code Annotation
**Task:** Annotate the original `VolumeDataSetRenderer.cs` for refactoring example 1.  
**Prompt (reconstructed):** Create an annotated copy of `VolumeDataSetRenderer.cs` for the `before/` folder of refactoring example 1. Add inline markers for all SRP, OCP, ISP, DIP, and GRASP violations, CBO drivers, WMC evidence, and target class mappings.  
**Output used:** `refactoring-examples/team3/example1-VolumeDataSetRenderer/before/VolumeDataSetRenderer.cs`  
**Modifications made:** Checked all violation markers against `docs/Codebase Exploration/SOLID_GRASP_Violations.md`.

---

### 2026-05-25 — SOLID/GRASP Audit (Design Doc §7 & §8)
**Task:** Write the 17-violation audit table and map violations to design decisions.  
**Prompt (reconstructed):** Write a structured SOLID/GRASP violation audit for `VolumeDataSetRenderer` as design doc §7.1 (17-row table: violation ID, principle, severity, description). Then write §8.2 mapping each violation to the design decision that fixes it (DD-01 to DD-04).  
**Output used:** `docs/design-document.md` §7.1 and §8.2  
**Modifications made:** Cross-checked all 17 violations against teammate's `docs/Codebase Exploration/SOLID_GRASP_Violations.md`.

---

### 2026-05-26 — After/ Classes: VolumeMaterialBinder & FoveatedSamplingPolicy
**Task:** Draft two of the four target refactored classes.  
**Prompt (reconstructed):** Draft `VolumeMaterialBinder.cs` with a `VolumeRenderState` readonly struct, an `IVolumeMaterialBinder` 7-member interface, and the sealed implementation with full SOLID/GRASP inline annotations and projected CK metrics. Then draft `FoveatedSamplingPolicy.cs` with `IGazeProvider` placeholder, `FoveationZone` enum, and zero-allocation `FoveationParameters` return struct.  
**Output used:**
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder.cs`
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/Rationale/VolumeMaterialBinder-decisions.md`
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/FoveatedSamplingPolicy.cs`

**Modifications made:** Adjusted CK metric projections after reviewing with Understand output. Confirmed WMC=16, CBO≤11 for VolumeMaterialBinder.

---

### 2026-05-27 — After/ Classes: VolumeCameraDriver & IMaskMode Finalisation
**Task:** Draft `VolumeCameraDriver.cs` and finalise all mask mode strategy classes.  
**Prompt (reconstructed):** Draft `VolumeCameraDriver.cs` with a `CameraFrameState` readonly struct, `IVolumeCameraDriver` interface, and a `VolumeCoordinateService` static pure-C# helper that replaces all `transform.InverseTransformPoint` calls with matrix-only math testable in edit mode. Then finalise `IMaskMode.cs` with `DisabledMaskMode` as a Null Object. Promote `ApplyMaskMode`, `InverseMaskMode`, and `IsolateMaskMode` from stubs to fully annotated implementations with XML docs, null-guards, and mutual keyword exclusion.  
**Output used:**
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeCameraDriver.cs`
- `refactoring-examples/team3/example2-MaskModes/after/IMaskMode.cs`
- `refactoring-examples/team3/example2-MaskModes/after/ApplyMaskMode.cs`
- `refactoring-examples/team3/example2-MaskModes/after/InverseMaskMode.cs`
- `refactoring-examples/team3/example2-MaskModes/after/IsolateMaskMode.cs`

**Modifications made:** Verified WMC=9, CBO≤4 for VolumeCameraDriver against targets. Confirmed `DisabledMaskMode` aligned with §5.8 Phase 1 migration step.

---

### 2026-05-27 — Design Document: Migration Path, Risks, Mask Mode Section
**Task:** Write three major design document sections.  
**Prompt (reconstructed):** Write §5.8 Migration Path using Strangler Fig strategy with 7 phases, per-phase entry/exit conditions, performance gates, and rollback costs. Write §10 Risks and Trade-offs with three subsections (performance overhead, coordinator complexity, interface versioning) and an 8-row risk register. Write §5.4 Mask Mode Strategy Pattern section covering the OCP violation, Strategy decision, interface design, four implementations, and pattern-choice rationale.  
**Output used:** `docs/design-document.md` §5.4, §5.8, §10  
**Modifications made:** Adjusted phase ordering and performance gate thresholds after team discussion.

---

### 2026-05-27 — Day 13 CK Metrics Projection & Metrics Worksheet
**Task:** Write Day 13 projected CK metrics table and populate metrics worksheet.  
**Prompt (reconstructed):** Write §6.2 and §6.3 of the design document with a 10-class Day 13 projected CK metrics table (all 6 CK metrics + meets-target column) and a delta summary paragraph. Then fill Sections 2–5 of `docs/metrics-worksheet.md` with projected values, delta table, and WMC/CBO/LCOM justification paragraphs.  
**Output used:** `docs/design-document.md` §6.2–6.3, `docs/metrics-worksheet.md` Sections 2–5  
**Modifications made:** Replaced speculative CK values with Understand tool output where available.

---

### 2026-05-28 — Example READMEs, SOLID/GRASP Annotation Pass & Section 6.3 Docs
**Task:** Write worked example READMEs, complete annotation pass, and fill Section 6.3 deliverables.  
**Prompt (reconstructed):** Rewrite `example1-VolumeDataSetRenderer/README.md` as a full worked example with CK delta table, 8-responsibility problem statement, design decisions, and test impact table. Fill in TBC values in `example2-MaskModes/README.md`. Complete an inline SOLID/GRASP annotation pass across all after/ files. Write `docs/rendering-layer-design.md` (9 sections, full class API surfaces) and `docs/shader-asset-policy.md` (7 sections: folder structure, naming, variant stripping, URP integration).  
**Output used:**
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/README.md`
- `refactoring-examples/team3/example2-MaskModes/README.md`
- `docs/rendering-layer-design.md`
- `docs/shader-asset-policy.md`

**Modifications made:** Verified all CK values in README tables against Understand tool output.

---

### 2026-05-28 — Standup Notes
**Task:** Write and format standup notes for the full project period.  
**Prompt (reconstructed):** Write standup log entries for weeks 1 and 2 (18 May – 29 May), covering what each team member did, is doing, and is blocked on, based on the PROGRESS.md session log.  
**Output used:** `docs/standup notes.md`, `standup/standup-log.md`  
**Modifications made:** Corrected dates and added missing entries from memory.

---

### 2026-05-29 — Metric Inconsistency Fix
**Task:** Fix WMC and CBO inconsistencies across the design document.  
**Prompt (reconstructed):** Find all occurrences of WMC=74 and CBO=31 in the revised design document and update them to the correct Understand-verified values of WMC=44 and CBO=45.  
**Output used:** `docs/revised_design_document.md` (multiple sections updated)  
**Modifications made:** Verified corrected values against `docs/metrics-worksheet.md`.

---

### 2026-05-30 — Design Document Revisions
**Task:** Revise the design document to reflect the rendering engine refactor architecture.  
**Prompt (reconstructed):** Revise the design document to more accurately reflect the rendering layer architecture, update section headings and content for the rendering engine refactor, and ensure consistency with the after/ code that was finalised in Sprint 2.  
**Output used:** `docs/design-document.md` (multiple revisions — 4 commits)  
**Modifications made:** Made additional edits to align with team feedback after review.

---

## Summary

| Sprint | Sessions | Primary AI Tasks |
|--------|----------|-----------------|
| Sprint 1 (18–22 May) | 1 | Project scaffold, CLAUDE.md/CONTEXT.md/PROGRESS.md, codebase read |
| Sprint 1 (24 May) | 1 | Interface stubs (IRenderPipeline, IMaskMode), codebase annotation |
| Sprint 2 (25–30 May) | ~10 | Design doc, diagrams, all refactoring examples, SOLID/GRASP audit, metrics worksheet, Section 6.3 docs, standup notes |

**Total AI-assisted deliverables:** Design document, both refactoring examples (before/after code + READMEs), all PlantUML diagrams, metrics worksheet, rendering-layer-design.md, shader-asset-policy.md, SOLID/GRASP audit, standup log.

**Degree of AI involvement:** Claude Code generated initial drafts for all written deliverables. All outputs were reviewed, verified against tool data (Understand, SonarQube, CodeScene), and modified before committing.
