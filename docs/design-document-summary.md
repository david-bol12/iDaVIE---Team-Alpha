# Design Document Summary & Presentation Guide
**Sub-team 3: Rendering Engine — Cache Me If You Can**
*Sprint 2 | Based on design-document.md, 25 May 2026*

---

## Overview

This document summarises each section of the Sub-team 3 design document and highlights the most presentation-worthy material for two audiences: the **iDaVIE maintainer team** and **college lecturers**.

---

## Section-by-Section Summary

### Section 1 — Executive Summary

The document analyses the iDaVIE rendering codebase using Chidamber–Kemerer (CK) software metrics to identify design flaws, then proposes a concrete refactoring plan. The central finding is that `VolumeDataSetRenderer` — a 1,403-line C# class — is a textbook **God Class** that violates multiple SOLID principles. The proposal splits it into four focused classes behind clean interfaces. No production code is changed in Sprint 2; this is a design-only proposal.

---

### Section 2 — Problem Statement

This is the analytical core of the document. Day 2 static analysis reveals that `VolumeDataSetRenderer` fails four of the six CK metric targets:

| Metric | Measured | Target | Excess |
|--------|----------|--------|--------|
| WMC (Weighted Methods per Class) | **74** | ≤ 20 | 3.7× over |
| CBO (Coupling Between Objects) | **31** | ≤ 14 | 2.2× over |
| RFC (Response For a Class) | **89** | ≤ 50 | 1.8× over |
| LCOM (Lack of Cohesion in Methods) | **0.81** | ≤ 0.5 | 1.6× over |

All four failing together is the diagnostic signature of a **God Class**. Each metric has a direct engineering consequence:

- **WMC 74** → the worst single method has a cyclomatic complexity of 28 — statistically predicting high defect density in VR context where frame-rate bugs are zero-tolerance.
- **CBO 31** → any change to this class forces consideration of 31 other files; regression risk is disproportionate to edit size.
- **RFC 89** → impossible to reason about the effect of any call without reading the entire class.
- **LCOM 0.81** → 81% of method pairs share no common fields, confirming the class has at least four unrelated internal clusters.

A responsibility inventory identifies **eight distinct responsibilities** crammed into this one class — shader management, texture upload, camera math, foveation, mask mode logic, crop management, cursor painting, and FITS I/O. The document also identifies a critical **render pipeline lock-in**: three API calls (`OnRenderObject`, `Graphics.DrawProceduralNow`, `Shader.EnableKeyword`) are incompatible with Unity 6 URP and are embedded directly in domain logic.

---

### Section 3 — Scope

Precisely defines what Sub-team 3 owns and what is out of bounds. In-scope: the full GPU-side rendering layer, including the proposed new classes, interfaces, adapters, test doubles, and shader assets. Out of scope: FITS data ingest (Sub-team 2), VR interaction/gaze SDK (Sub-team 4), and the application shell (scene lifecycle, menus, session management). The section includes a mapping table showing how each part of the academic brief (§6.3 and §9.2) is satisfied by the document's sections.

---

### Section 4 — Requirements Recap

Links back to `docs/requirements.md`. Highlights the six invariants that must survive any refactoring:

- **INV-01**: 90 fps minimum frame rate
- **INV-02/03**: 4 GB texture limit, 368 MB per data cube budget
- **INV-04**: Nearest-neighbour texture filtering
- **INV-05/06**: Foveated rendering and mask mode correctness

Two design-driving functional requirements are called out: `FR-06` (`IRenderPipeline` abstraction) and `FR-08` (`IGazeProvider` abstraction). Non-functional CK targets (WMC ≤ 20, CBO ≤ 14, LCOM ≤ 0.5) are the acceptance gates for the refactoring.

---

### Section 5 — Design Decisions

The architectural heart of the document. Contains five major sub-decisions.

**DD-01 — `IRenderPipeline` (Render Pipeline Abstraction)**
Introduces a six-member interface to isolate all Unity render-pipeline-specific calls from domain logic. Two concrete adapters (`UrpRenderPipeline`, `HdrpRenderPipeline`) are the only files permitted to import pipeline-specific namespaces. A `NullRenderPipeline` test double enables edit-mode unit tests without a GPU. Solves the URP migration lock-in and satisfies DIP and the GRASP Protected Variations pattern.

**DD-02 — `IMaskMode` (Strategy Pattern)**
Replaces a sprawling if/else chain for mask modes with the Strategy pattern. Each of the four mask behaviours (Apply, Inverse, Isolate, Disabled) becomes a small, sealed, stateless class implementing a two-member interface. Adding a new mask mode in the future requires writing one new class only — zero existing files change. Resolves the OCP violation where adding one mask mode previously required touching four different files.

**DD-03 — Class Split (SRP)**
`VolumeDataSetRenderer` is replaced by five collaborating classes:

| Class | Single Responsibility | Projected WMC | Projected CBO |
|---|---|---|---|
| `VolumeRenderCoordinator` | Wires the four classes; drives per-frame loop | ≤ 10 | ≤ 6 |
| `VolumeMaterialBinder` | Shader keywords, material properties, colour maps | ~16 | ≤ 11 |
| `VolumeTextureManager` | 3D texture upload, LRU caching, 368 MB budget | ≤ 15 | ≤ 6 |
| `VolumeCameraDriver` | Camera matrix, clip planes, projection mode | ≤ 12 | ≤ 6 |
| `FoveatedSamplingPolicy` | Per-frame sample rate from gaze direction | ≤ 8 | ≤ 2 |

**DD-04 — Foveated Rendering Extraction (`FoveatedSamplingPolicy` + `IGazeProvider`)**
Extracts the foveation calculation into a dedicated class and introduces `IGazeProvider` to decouple it from the SteamVR SDK. Sub-team 3 owns the interface; Sub-team 4 provides the concrete implementation. A `MockGazeProvider` stub covers all tests until that contract is delivered. Resolves a DIP violation and protects the codebase from future HMD SDK changes.

**Shader/Asset Organisation Policy**
Establishes strict naming conventions and folder structure under `Assets/Rendering/`. The URP and HDRP shader variants are selected at build time by the adapter class only — no domain class ever references a shader asset directly. Shader variant stripping ensures only the three active mask mode keywords ship in production builds.

**Migration Path — Strangler Fig Pattern**
A seven-phase incremental migration plan. Each phase extracts one responsibility, compiles and runs at full frame rate before the next begins, and can be rolled back by restoring a single file. The invariant throughout is INV-01 (90 fps), enforced at CI after every phase. Ends with Phase 7, where the URP adapter swap is a two-file change with zero domain code modifications.

---

### Section 6 — CK Metrics Worksheet

Quantifies the before-and-after impact. Every proposed class meets or beats all target thresholds:

| Class | WMC | CBO | LCOM | Meets targets? |
|---|---|---|---|---|
| `VolumeRenderCoordinator` | 3 | 6 | 0.00 | ✅ all |
| `VolumeMaterialBinder` | 16 | 11 | 0.05 | ✅ all |
| `VolumeTextureManager` | 20 | 8 | 0.05 | ✅ all |
| `VolumeCameraDriver` | 9 | 4 | 0.00 | ✅ all |
| `FoveatedSamplingPolicy` | 7 | 6 | 0.00 | ✅ all |

Compared to the monolith: WMC drops from 74 to max 20 per class; CBO drops from 31 to max 11; LCOM collapses from 0.81 to ≤ 0.05. The 46-file dependency cycle (39.8% propagation cost) is broken entirely.

---

### Section 7 — Class and Sequence Diagrams

References four PlantUML diagram source files in `diagrams/`:
- `class-before.puml` — the current monolith and its 31-file coupling web
- `class-after.puml` — the five proposed classes and their interface boundaries
- `architecture.puml` — component/assembly diagram annotating the `IRenderPipeline` boundary
- `sequence-render-frame.puml` — the 8-step per-frame render sequence (Update → camera → foveation → texture → material → mask → pipeline → GPU)

---

### Section 8 — SOLID/GRASP Audit

A comprehensive audit of the current codebase identifies **17 confirmed violations** — 6 Critical, 8 High, 1 Medium — across all SOLID principles except LSP, and 7 of 9 GRASP patterns. Each violation is catalogued with the specific class, line numbers, and severity. The second half of the section maps every violation to the design decision that resolves it. Two trade-offs are acknowledged honestly: the coordinator's unavoidable CBO of ~6 (acceptable under brief rules), and the thin `MonoBehaviour` shell that must carry a `UnityEngine` dependency (unavoidable on Unity; mitigated by keeping it under 30 lines with zero domain logic).

---

### Section 9 — Sub-team Dependencies

Defines the cross-team interface contracts:
- **Sub-team 2 (Data I/O)**: `VolumeTextureManager` consumes data via `IRawVolumeDataSource`. Status: pending; `StubVolumeDataSource` in place.
- **Sub-team 4 (Interaction)**: `FoveatedSamplingPolicy` consumes gaze data via `IGazeProvider`. Status: pending; `MockGazeProvider` in place.
- Sub-team 3 has no upstream dependencies on Sub-teams 1, 5, 6, or 7.

---

### Section 10 — Risks and Trade-offs

Three substantive risks are analysed in depth:

- **Performance overhead of the abstraction layer** — virtual dispatch on the hot path. Mitigated by sealing all concrete classes (enabling IL2CPP de-virtualisation), capping `VolumeRenderState` at 64 bytes, and a CI performance gate that fails the build if per-frame CPU time exceeds 2 ms.
- **Coordinator complexity growth** — risk the coordinator becomes a second God Class over time. Mitigated by a SonarQube gate that fails any coordinator method with cyclomatic complexity > 2, a 100-line class limit, and the `CoordinatorWiringTest` integration test.
- **Interface versioning risk** — cross-team interfaces may drift. Mitigated by `[InterfaceVersion]` attributes with a runtime reflection guard, shared stub files in a neutral location requiring co-review before change, and contract test suites for each interface.

A risk register table summarises 8 risks with likelihood, impact, and mitigation for each.

---

### Section 11 — Appendices

References: diagram source files, interface stubs in `refactoring-examples/stubs/`, a glossary of CK metric abbreviations and domain terms, and external references including the iDaVIE GitHub repository.

---

---

## Presentation Highlights

### For the iDaVIE Maintainer Team

These are the findings and proposals most relevant to the people who own and maintain the iDaVIE codebase long-term.

**1. The God Class is a concrete, quantified problem — not an opinion.**
The four failing CK metrics (WMC 74, CBO 31, RFC 89, LCOM 0.81) give a precise, defensible description of why `VolumeDataSetRenderer` is hard to change. This reframes refactoring as engineering necessity, not aesthetics. Lead with the table in §2.1 — it makes the case in seconds.

**2. Unity 6 URP migration is currently blocked by architectural coupling.**
The three incompatible API calls (`OnRenderObject`, `Graphics.DrawProceduralNow`, `Shader.EnableKeyword`) embedded in domain logic mean that a future URP migration would require modifying the God Class itself. The `IRenderPipeline` abstraction (DD-01) turns that into a two-file adapter swap — nothing in domain logic changes. Frame this to the maintainer team as a future-proofing decision that removes the platform-migration risk.

**3. Adding new mask modes currently requires touching four files — including the God Class.**
The OCP violation (V-04) means that every feature extension carries the full regression risk of a 1,403-line class. After the Strategy pattern refactor (DD-02), new mask modes are a single new class file. This directly impacts the team's ability to deliver future astronomy-specific visualisation features without fear.

**4. Cross-team contracts are explicit, stub-protected, and version-guarded.**
The interfaces for Sub-team 2 (data) and Sub-team 4 (gaze/VR) are designed as stable contracts with mock doubles in place. The `[InterfaceVersion]` attribute and runtime reflection guard convert silent contract mismatches into loud, actionable errors. For a multi-team project this reduces integration risk substantially.

**5. The migration plan is zero-risk at every step.**
The Strangler Fig migration (§5.8) never leaves the codebase in a broken state. Each phase merges to main independently, compiles, and runs at 90 fps before the next phase begins. Any phase can be rolled back by reverting a single file. This is the answer to "how do we actually get from here to there without breaking the product."

**6. Metrics after refactoring are all green — with documented headroom.**
The Day 13 projection table (§6.2) shows every proposed class meeting its CK targets, with the two closest to their limits (`VolumeMaterialBinder` CBO = 11, `VolumeTextureManager` WMC = 20) carrying documented per-member inventories that signal there is intentionally no room left for further accumulation. This is a self-enforcing design.

---

### For College Lecturers

These are the areas that demonstrate software engineering rigour, design pattern fluency, and academic grounding.

**1. CK Metric Suite as an Analytical Tool (§2, §6)**
The document applies the complete Chidamber–Kemerer suite (WMC, DIT, NOC, CBO, RFC, LCOM) to a real production codebase and interprets the results in engineering terms — not just as numbers. The connection drawn between LCOM and structural cohesion, and between CBO and regression risk propagation cost, demonstrates fluency with the theoretical meaning of the metrics, not just mechanical measurement. The before/after delta (§6.3) provides a clean empirical argument for the refactoring.

**2. SOLID Principles — Applied with Evidence (§8)**
The audit in §8.1 catalogues 17 violations across SRP, OCP, DIP, and ISP, each with specific line numbers and a severity rating. More importantly, §8.2 maps each violation to the design decision that resolves it, demonstrating that the architecture is not derived from intuition but from a systematic violation-resolution process. This is the kind of traceable design rationale that distinguishes an engineering document from an ad-hoc design.

**3. GRASP Patterns — Beyond the Textbook (§8)**
The audit covers seven GRASP patterns (Information Expert, Creator, Controller, Indirection, Protected Variations, Low Coupling, High Cohesion). The analysis of V-14 (no indirection layer between Unity lifecycle hooks and domain logic) and V-15 (three variation points with zero interface protection) shows application of GRASP beyond simple identification — the patterns are used to diagnose architectural risk.

**4. Design Patterns — Strategy and Adapter (§5.3, §5.4)**
DD-01 (`IRenderPipeline`) is a textbook Adapter pattern, with a worked explanation of why Adapter was chosen over the alternatives and exactly which methods it must expose to remain minimal. DD-02 (`IMaskMode`) is a textbook Strategy pattern with an explicit comparison against Decorator and State — explaining why those alternatives are inadequate for this specific context. Both justify the pattern choice rather than just naming it.

**5. Dependency Inversion — Practical Application (§5.3, §5.6)**
DIP is demonstrated concretely: before, `VolumeDataSetRenderer` (high-level policy) depends on `UnityEngine.Rendering.Universal` (low-level detail). After, `VolumeMaterialBinder` depends on `IRenderPipeline` (an abstraction it controls), and `UrpRenderPipeline` (low-level detail) implements the abstraction. The dependency arrows are explicitly drawn and justified. The same pattern is applied to `IGazeProvider` for the gaze/foveation boundary.

**6. Performance vs. Abstraction Trade-off Analysis (§10.1)**
The risk analysis in §10.1 engages with a genuine trade-off that textbooks often avoid: introducing interface boundaries in a hard real-time system (90 fps VR) does carry a theoretically measurable cost. The analysis works through virtual dispatch cost (~1–3 ns), IL2CPP de-virtualisation via `sealed` classes, `VolumeRenderState` struct sizing (36 bytes, within the 64-byte L1 cache line threshold), and the CI performance gate as the empirical backstop. This demonstrates awareness that design principles must be applied with domain constraints in mind.

**7. Strangler Fig Migration Pattern (§5.8)**
The seven-phase incremental migration plan applies the Strangler Fig architectural pattern to a real refactoring problem. The phasing rationale (interfaces first, lowest-risk extractions first, highest-risk last, shadow-mode parallel run before cutover) demonstrates that the team understands how to manage architectural change in a live system — not just how to design from scratch.

**8. Honest Acknowledgement of Trade-offs (§8.3, §10)**
The document explicitly acknowledges two violations it cannot fully eliminate (`MonoBehaviour` coupling, coordinator CBO of ~6) and explains why they are acceptable within the brief's constraints. The risk register in §10.4 rates eight risks honestly, including one where the proposal itself could be rejected by the maintainer panel (R-08). Intellectual honesty in a design document is itself a mark of engineering maturity.

---

## Quick Reference: Key Numbers to Know

| Fact | Value |
|---|---|
| Lines in the God Class | 1,403 |
| WMC of God Class | 74 (target ≤ 20) |
| CBO of God Class | 31 (target ≤ 14) |
| LCOM of God Class | 0.81 (target ≤ 0.5) |
| SOLID/GRASP violations found | 17 (6 Critical, 8 High, 1 Medium) |
| Files in dependency cycle | 46 (39.8% propagation cost) |
| Proposed replacement classes | 5 core + 4 mask strategies + 2 pipeline adapters |
| Migration phases | 7 (each independently rollback-able) |
| Frame rate invariant | 90 fps (enforced in CI at every phase) |
| VolumeRenderState struct size | 36 bytes (safe under 64-byte L1 cache line) |

---

*Document prepared from: `docs/design-document.md` (Sub-team 3 — Cache Me If You Can, Sprint 2)*
