# Team Alpha — Combined AI Tool Usage Log (T8)

All seven sub-team AI logs compiled into a single document for the alpha submission. Each sub-team's original format is preserved; sections are separated by horizontal rules. Sub-teams with no log entries are noted as such.

**Sources:**
- `ai-tool-log/sub-team-1.md` through `sub-team-7.md` (canonical logs)
- `docs/team-5/ai-log.md` (sub-team 5 local log — content not in the canonical stub)
- `docs/sub-team-6/ai-log.md` and root `AI-USAGE-LOG.md` are duplicates of the canonical sub-team-6 / sub-team-3 logs and are omitted here.

**Sub-team ↔ work-package mapping** (Team Alpha internal numbering): 1 Architecture / Micro-kernel · 2 Native Plug-ins · 3 Rendering & Compute · 4 Interaction System · 5 Networking / Server · 6 Desktop GUI & Client Shell · 7 Persistence & Data.

**Policy reminders (apply to every section):**
- AI output must be reviewed, understood, and defensible by a named human author.
- AI MAY NOT be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence (§10.5.6).
- Verbatim AI prose must not be passed off as human-authored in the final report (§10.5.3).

---

# Sub-team 1 — Architecture / Micro-kernel

A brief log of recent Claude (Claude Code) conversations, grouped by date. Newest entries on top. Author: David.

## David

### 4 June 2026
- **Integration overview document** — Reviewed all merged/open PRs and the two CI workflows to author `docs/integration/INTEGRATION.md`: per-sub-team contribution map, CI pipeline gates, current `main` CI failures and causes, cross-team contracts, and outstanding actions.
- **Git commit cleanup** — Committed and then soft-reset the integration document commit on request, preserving the changes.
- **Modifiability passage** — Drafting and tightening a quality-attribute passage on poor modifiability for the ASR document.
- **Branch coverage ≥70%** — What the metric means and how it's enforced on domain/application layer classes.
- **Merge conflicts + dependency cycles** — Whether renaming files avoids merge conflicts; what "0 dependency cycles" means and how interfaces break them.
- **Modularity passage + verifications** — Completing the modularity write-up and suggesting NDepend-based "Verified by" entries.

### 3 June 2026
- **Optimal technical debt level** — CodeScene Code Health bands and trend-based targets.
- **Open–closed principle** — OCP explained with a C# example; IEEE context for McCabe's metric.
- **Simplifying modularity** — Plain-English breakdown of the modularity requirements, linked to SWEBOK.
- **Software / data independence** — Definitions of software-independence (voting systems) and physical/logical data independence.
- **Public API contracts** — Purpose of documented contracts at public boundaries, progressively reworded.
- **Domain class Unity imports** — Finishing the rationale for zero UnityEngine imports in domain code.
- **Stakeholder expectations ↔ ASRs** — Traceability analysis of the iDaVIE roadmap against existing ASRs/NFRs.
- **PR deleting files** — Diagnosing deletions in a PR diff (merge base, bad merges/rebases).
- **SonarQube on docs** — Why SonarQube isn't suited to documentation analysis; better alternatives.
- **CodeScene technical debt** — How Code Health × Hotspots produces prioritised debt; rewording "reduce tech debt".
- **Codebase testability** — Levers for improving testability; Unity sources of nondeterminism.
- **Sheep video** — Asked for a video; explained no video capability, offered alternatives.

### 2 June 2026
- **SWEBOK Ch.2 + ASRs** — TL;DR of the Software Architecture chapter and what ASRs should outline.
- **Stakeholder expectations** — Summarising the iDaVIE roadmap's stakeholder expectations and long-term aspirations.
- **GitHub PR merge access** — Diagnosing a "need write access" error on a fork-based PR.

### 31 May 2026
- **Linux device input** — Mouse side-button remapping (evdev vs X11, Wayland caveats).

### 25 May 2026
- **FITS render data flow** — Traced what data is passed to the renderer to draw FITS volumetric objects (VolumeData pipeline).

### 21 May 2026
- **God-class UML diagrams** — Identified each god class and produced PlantUML class diagrams of their interactions, plus refactoring suggestions.
- **Architecture layers explained** — Walkthrough of the domain / application / infrastructure / plugin-host layering.

### 20 May 2026
- **Project singletons** — Enumerated the singleton patterns across the codebase.
- **Kernel-class refactor advice** — Whether introducing a kernel class is the right way to untangle the intertwined class structure.

### 19 May 2026
- **VolumeData deep dive** — High-level description of each class in the VolumeData directory and the tight coupling between volume input and VR controls.
- **CI workflow authoring** — Drafted a `ci.yaml` for the repo; clarified Unity (student-license) version from ProjectVersion and that the check targets the native plugin vs the Unity app.

### 18 May 2026
- **CLAUDE.md + project analysis** — Generated the initial CLAUDE.md, reviewed modularity / microkernel feasibility, covered model switching, and drafted a monolithic-architecture README.
- **Spec + CLAUDE.md merge** — Combined the spec and CLAUDE.md documents.
- **C ABI plugin contract** — Purpose of the C ABI plugin contract and the P/Invoke boundary code related to it.
- **Class-architecture domain model** — Built a domain model showing the class architecture and interactions.
- **Dependency setup** — Downloaded the project's required dependencies.

*The bulk centres on iDaVIE architecture/ASR work, with side threads on Git/GitHub mechanics, Linux input, and one off-topic request.*

---

# Sub-team 2 — Native Plug-ins

Newest entries on top. Author: Conor Healy.

## 2026-06-02 — Conformance test plan for native plug-ins

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/ConformancePlan.md`
- **Prompt summary:** Asked Claude to draft a conformance test plan table covering each ABI function, input type, and pass criteria for the three native plug-ins.
- **Where it helped:** Generated the table structure and populated entries for FITS file ops and WCS transforms quickly.
- **Where it failed / was wrong:** Several pass criteria for WCS round-trip tolerance were placeholder values — had to research and replace with values from the Starlink AST documentation.
- **Human reviewer:** Conor Healy

## 2026-06-02 — CK metrics analysis for plugin layer (LCOM, DIT, NOC, RFC, CBO)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Verbal/session analysis — figures fed into design document and pitch preparation
- **Prompt summary:** Asked Claude to calculate LCOM HS, DIT, NOC, RFC, and CBO for `FitsReader`, `DataAnalysis`, and `AstTool` in the pre-recast state. Also asked it to explain each metric and the adapter/domain threshold distinction.
- **Where it helped:** Correctly calculated LCOM HS = 1.0 for FitsReader and DataAnalysis; identified RFC = 74 for FitsReader as a threshold failure; confirmed NOC = 0 via grep; explained DIT N/A for static classes.
- **Where it failed / was wrong:** Initially calculated DIT = 0 when NDepend reports 1 (it counts System.Object). Also initially claimed LCOM 1.0 for FitsReader was just an adapter artefact and not a design flaw — had to push back on this, it is a genuine flaw.
- **Human reviewer:** Conor Healy

## 2026-06-02 — Worked refactoring examples (FitsReader memory ownership + WCS Unity removal)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/worked-examples/fits-memory/before.cs`, `after.cs`; `refactoring-examples/data-plugins/worked-examples/wcs-plugin/before.cs`, `after.cs`
- **Prompt summary:** Asked Claude to produce before/after worked examples for FitsReadSubImageFloat memory ownership and AstTool Unity dependency removal, using actual git history as source material.
- **Where it helped:** Spotted the inverted-condition memory leak bug in VolumeDataSet.cs lines 431–434. Accurately reconstructed the before/after from git commits. AstTool example correctly showed the DLL rename from `idavie_native` to `idavie_ast`.
- **Where it failed / was wrong:** The AstTool before.cs included a `TryTransform(Vector3)` helper that never existed in the original — fabricated as an illustration. Kept as a pattern example but not a direct quote.
- **Human reviewer:** Conor Healy

## 2026-06-02 — Plugin design document (honest reflection of actual recast)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/plugin-design-document.md`
- **Prompt summary:** Gave Claude our draft design doc (written in future tense) and asked it to rewrite it as a past-tense reflection of what was actually built, grounded in the git commit history.
- **Where it helped:** Rewrote in past tense accurately. Caught the two-pattern inconsistency between `[DllImport]` direct and `NativePluginLoader` attribute approaches. Produced the interaction diagram.
- **Where it failed / was wrong:** An earlier version of this doc (plugin-registry.md) invented `IFitsPlugin`, `IAstPlugin`, `IDataPlugin` interfaces and a `PluginRegistry` class that don't exist anywhere in the codebase. Had to be completely discarded.
- **Human reviewer:** Conor Healy

## 2026-06-02 — Architecture document — kernel ABI (superseded)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/architecture/plugin-registry.md` (superseded)
- **Prompt summary:** Asked Claude to produce an architecture document describing the kernel ABI and plug-in boundary before we had confirmed what was actually built.
- **Where it helped:** The two-boundary diagram and the GetSourceStats AST entanglement explanation were accurate and carried forward into the final doc.
- **Where it failed / was wrong:** Generated `IFitsPlugin`, `IAstPlugin`, `IDataPlugin` interfaces and a `PluginRegistry` class as if they existed — none of them do. The whole document had to be replaced once we checked the actual code.
- **Human reviewer:** Conor Healy

## 2026-05-30 — Property-based test examples for FITS round-trip

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/FitsRoundTripTests.cs`
- **Prompt summary:** Asked for property-based tests using FsCheck covering FITS read/write round-trips with synthetic header and pixel data.
- **Where it helped:** FsCheck generator scaffolding and test structure were good, saved a lot of setup time.
- **Where it failed / was wrong:** Tests assumed an in-memory FITS buffer, which CFITSIO does not support — rewrote to use temp files on disk.
- **Human reviewer:** Conor Healy

## 2026-05-28 — SRP split design for FitsReader

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/fits-reader/srp-split.md`
- **Prompt summary:** Asked Claude to propose how to split the 59-method FitsReader monolith based on responsibility boundaries in the source.
- **Where it helped:** Identified the five boundaries (file lifecycle, header, image, table, mask) that matched what we implemented. Descriptions were accurate.
- **Human reviewer:** Conor Healy

## 2026-05-26 — Isolation strategy for plug-in tests without Unity

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/PluginIsolationStrategy.md`
- **Prompt summary:** Asked how to load and test the native DLLs in a plain .NET test runner with no Unity installation.
- **Where it helped:** Correct explanation of `SetDllDirectory` and `NativeLibrary.SetDllImportResolver`. Useful starting point for the strategy doc.
- **Where it failed / was wrong:** Boilerplate assumed NUnit — had to replace with the actual test runner we're using.
- **Human reviewer:** Conor Healy

## 2026-05-22 — Strategy pattern design for downsampling

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/data-analysis/patterns.md`
- **Prompt summary:** Asked Claude to refactor the `bool maxDownsampling` parameter in `DataCropAndDownsample` into a Strategy pattern and show the before/after interface design.
- **Where it helped:** `IDownsampleStrategy` interface and the two concrete implementations were clean and used directly.
- **Human reviewer:** Conor Healy

## 2026-05-20 — CK metric explanations for baseline

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Design document intro section
- **Prompt summary:** Asked Claude to explain each CK metric in plain language to help interpret the NDepend baseline output.
- **Where it helped:** Clear explanations, used almost verbatim in the doc intro.
- **Where it failed / was wrong:** Gave slightly different threshold values to what Section 7.1 of the spec states — cross-checked and corrected.
- **Human reviewer:** Conor Healy

## 2026-05-19 — Initial codebase exploration

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Sprint planning notes
- **Prompt summary:** Asked Claude to read the plugin interface directory and summarise what each class does, method counts, and external dependencies.
- **Where it helped:** Quick summary of FitsReader (59 methods, CFITSIO, Unity), DataAnalysis (delegate pattern), AstTool (Starlink AST). Saved manually reading ~800 lines.
- **Human reviewer:** Conor Healy

---

# Sub-team 3 — Rendering & Compute (Cache Me If You Can)

> **Purpose:** Documents all AI-assisted work completed by each team member for transparency and academic integrity.

## Ciallian Bain

### Session 1 — iDaVIE Setup & Requirements
**Tool:** Claude (Cowork)
**Task:** Understanding how to build iDaVIE from source.
**Prompt summary:** Asked how to set up and build iDaVIE on Unity 2021, including all dependencies.
**Output:** Full build requirements list (Unity 2021.3 LTS, Visual Studio C++ workload, CMake, vcpkg, SteamVR/OpenVR). Sourced from official iDaVIE docs and GitHub BUILD.md.
**Files affected:** None (informational).

### Session 2 — SOLID Violation Audit
**Tool:** Claude (Cowork)
**Task:** Structured SOLID audit of `VolumeDataSetRenderer`.
**Prompt summary:** Requested a SOLID violation audit using CONTEXT.md and the dependency map as source material. Asked AI to clarify scope before starting.
**Output:** Produced a structured SOLID audit document covering all five SOLID principles with specific line-number references to violations in `VolumeDataSetRenderer`.
**Files affected:** SOLID audit document (docs/team3/).

### Session 3 — Architecture Diagram Syntax Fixes
**Tool:** Claude (Cowork)
**Task:** Fix PlantUML syntax errors across diagram files.
**Prompt summary:** Diagrams were failing to render due to multiple syntax errors.
**Output:** Fixed 14+ issues across `architecture.puml` — replaced Unicode dashes with ASCII `--`, escaped angle brackets, fixed arrow glyphs, added component aliases for multi-line names.
**Files affected:** `diagrams/architecture.puml`.

### Session 4 — CK Metrics Tooling & Golden Image Testing
**Tool:** Claude (Cowork)
**Task:** Two questions: (1) what is golden image regression testing and can AI produce one; (2) how to document that CK metrics tools failed.
**Prompt summary:** Asked what "golden image regression suite across mask modes and colour maps" means and whether it could be done. Also asked how to honestly document that Understand, NDepend, and SonarQube free trials failed to work with the Unity project structure.
**Output:** Explanation of golden image testing and its scope.

### Session 5 — CK Metrics in NDepend
**Tool:** Claude (Cowork)
**Task:** Locating CK metrics within NDepend's interface.
**Prompt summary:** Asked where to find CK metrics in an NDepend report.
**Output:** Explained where each CK metric maps in NDepend (dashboard, Queries & Rules panel, CQLinq), and provided a sample CQLinq query to extract per-type metrics directly.
**Files affected:** None (informational).

### Session 6 — Metrics Consistency Pass (Correct Understand Values)
**Tool:** Claude (Cowork)
**Task:** Apply verified Understand metric values consistently across all documents.
**Prompt summary:** Correct CK values (from the Understand tool run) needed to replace earlier estimates across all docs and code files.
**Output:** Updated metrics-worksheet, design-document, both refactoring example READMEs, all five `after/*.cs` files, PROGRESS.md, CONTEXT.md, and two PlantUML diagrams. Documented three classes (VolumeRenderCoordinator, VolumeTextureManager, VolumeMaterialBinder) exceeding LCOM 0.5 target as a lifecycle-phase artefact, not a design flaw.
**Files affected:** `docs/team3/metrics-worksheet.md`, `docs/team3/design-document.md`, both example READMEs, all `after/*.cs` files, `PROGRESS.md`, `CONTEXT.md`, `diagrams/class-before.puml`, `diagrams/vdsr-dependencies.puml`.

### Session 7 — Sprint 2 Task Triage & Document Completion
**Tool:** Claude (Cowork)
**Task:** Multiple tasks — ClickUp task status review, document sections written, merge conflict resolved, interview study guide created.
**Prompt summary (multiple prompts in one session):** Tracked ClickUp to-do list and ticked off done vs remaining; verified work done on "compute CK metrics" and "SOLID/GRASP annotation pass"; assisted with various Kanban-board tasks.
**Output:** Task triage (8 complete, 4 unblocked, 3 blocked); `design-document.md` §6.2/§6.3 written (Day 13 10-class projection table + delta narrative WMC 74→20, CBO cycle broken, LCOM 0.81→0); `metrics-worksheet.md` populated; `PROGRESS.md` merge conflict (lines 64–112) resolved.
**Files affected:** `docs/team3/design-document.md`, `docs/team3/metrics-worksheet.md`, `PROGRESS.md`, both example READMEs.

## Cathal Ging

### Session 1 (2026-05-24)
- Used Claude to validate `IRenderPipeline` interface design (6 methods, DIP reasoning).
- Drafted `NullRenderPipeline` test double with Claude's help.
- Sketched `IMaskMode` interface + three strategy implementations.
- Documented design rationale for test doubles in `stubs/`.

### Session 2 (2026-05-25)
- Used Claude to confirm CBO count in the dependency map (28 coupled classes).
- Drafted diagrams: `architecture.puml`, `class-before.puml` with real metrics.
- Annotated `before/` code with SOLID/GRASP violation markers.

### Session 3 (2026-05-25)
- Used Claude to extract and annotate `VolumeDataSetRenderer.cs` (20+ inline markers).
- Documented 8 responsibilities and mapping to target classes.

### Session 4 (2026-05-25)
- Drafted the design document outline with Claude (10-section structure, brief-aligned).

### Session 5 (2026-05-25)
- Created `architecture.puml` component diagram with Claude (5-layer, DIP callout).
- Expanded `sequence-render-frame.puml` (7-step frame loop, alt blocks).
- Created `class-after.puml` skeleton with projected CK annotations.

### Session 6 (2026-05-26)
- Drafted `VolumeMaterialBinder.cs` with Claude (16 WMC, 11 CBO, 0.05 LCOM).
- Validated CK projections against targets.
- Extracted `ShaderID` nested class (no public exposure of 20+ properties).

### Session 7 (2026-05-26)
- Drafted `FoveatedSamplingPolicy.cs` with Claude (7 WMC, 6 CBO, 0.0 LCOM).
- Sketched `IGazeProvider` placeholder for the Sub-team 4 dependency.
- Documented `FoveationZone` enum and `FoveationParameters` struct.

### Session 8 (2026-05-27)
- Drafted `VolumeCameraDriver.cs` with Claude (9 WMC, 4 CBO, `VolumeCoordinateService` helper).
- Finalised `IMaskMode.cs` + three mask mode implementations.
- Enriched the SOLID/GRASP violation table with code locations (file/class/method).

### Session 9 (2026-06-02)
- Updated all code comments for the Sprint 3 wire-up.

## Chris Jo Gibson

### Session 1 — SonarQube Setup & Code Health
**Tool:** Claude Code
**Task:** Configure SonarQube to scan the iDaVIE project locally.
**Prompt summary:** Asked Claude to make SonarQube work on the full project so results could be viewed on localhost.
**Output:** Configured the SonarQube scanner to run against the iDaVIE codebase and display results via the local SonarQube server.
**Files affected:** SonarQube configuration files (scanner setup).

### Session 2 — Unit Tests for Refactoring Examples
**Tool:** Claude Code
**Task:** Write unit tests against the refactored C# code and document what is being tested.
**Prompt summary:** Asked Claude to find the refactoring examples and generate unit tests for the new refactored code, place them in the refactoring folder, and create a markdown file explaining what each test covers and why.
**Output:** Unit test file(s) for refactored classes; accompanying markdown explanation of test coverage rationale.
**Files affected:** `refactoring-examples/team3/` (new test file and markdown).

### Session 3 — Test Strategy Updated to Reflect New Tests
**Tool:** Claude Code
**Task:** Align `test-strategy.md` with the unit tests written in Session 2.
**Prompt summary:** Asked Claude to update the test strategy document to reflect the new tests in the refactoring examples, keeping it concise and relevant, and to note short- and long-term codebase impact.
**Output:** Revised `test-strategy.md` with updated test coverage description and impact analysis.
**Files affected:** `docs/team3/test-strategy.md`.

### Session 4 — Revised Design Document (Section 5 Merge)
**Tool:** Claude Code
**Task:** Merge Section 5 content from the original design document into the revised design document.
**Prompt summary:** Asked Claude to read the design document, extract Section 5, and write it into the revised design document matching existing style; also to check length before committing.
**Output:** `revised_design_document.md` updated with Section 5 content; committed to branch.
**Files affected:** `docs/team3/revised_design_document.md` (or equivalent).

### Session 5 — VolumeDataSetRenderer Top-Down Explanation
**Tool:** Claude Code
**Task:** Understand the existing `VolumeDataSetRenderer.cs` class before refactoring.
**Prompt summary:** Asked for a top-down explanation of what `VolumeDataSetRenderer.cs` does, its purpose, dependencies, and downstream effects.
**Output:** Detailed explanation covering its role in the rendering pipeline, key dependencies (Unity, SteamVR, shader property bindings), and downstream effects on camera, masking, and texture systems.
**Files affected:** None (informational).

### Session 6 — Sequence Diagrams (Before & After)
**Tool:** Claude Code
**Task:** Create PlantUML sequence diagrams for one render frame, before and after refactoring.
**Prompt summary:** Asked Claude to create a new sequence diagram using the revised design document, the previous sequence diagram, and the refactored examples; plus an initial sequence diagram for the original `VolumeDataSetRenderer`.
**Output:** Two PlantUML sequence diagrams: original monolithic render-frame flow and refactored multi-class flow.
**Files affected:** `diagrams/sequence-render-frame.puml` and a before-state diagram file.

### Session 7 — Brief Compliance Evaluation & Document Push
**Tool:** Claude Code
**Task:** Evaluate whether Sub-team 3's deliverables meet the brief; push deliverable to GitHub.
**Prompt summary:** Provided the assignment brief PDF and asked Claude to evaluate compliance; followed up to re-check after a pull and action the first gap.
**Output:** Compliance gap analysis; first gap actioned; deliverable pushed.
**Files affected:** Deliverable document (docs/team3/); GitHub push.

### Session 8 — Unity 6 Migration Plan in Test Strategy
**Tool:** Claude Code
**Task:** Ensure the Unity 6 migration plan is reflected in `test-strategy.md`.
**Prompt summary:** Asked Claude to check whether a Unity 6 migration plan was included; confirmed adding a Section 10 after verifying it was referenced in the design document.
**Output:** Section 10 (Unity 6 Migration Testing) added.
**Files affected:** `docs/team3/test-strategy.md`.

### Session 9 — AI Usage Log Population
**Tool:** Claude Code
**Task:** Populate the AI usage log for Chris Jo from Claude session history.
**Prompt summary:** Asked Claude to read the existing log for format reference and fill in all sessions.
**Output:** Sessions 1–10 written into `AI-USAGE-LOG.md` under Chris Jo's section.
**Files affected:** `AI-USAGE-LOG.md`.

## Damien O Brien

### Session 1 — Design Document Questions (URP/HDRP & No-ops)
**Tool:** Claude (Cowork)
**Task:** Understand URP vs HDRP rendering pipelines and the concept of no-ops.
**Prompt summary:** Asked the difference between URP and HDRP pipelines and what "no-ops" means.
**Output:** Explanation of URP vs HDRP and a definition of no-ops in the rendering context.
**Files affected:** None (informational).

### Session 2 — URP/HDRP Redundant Code Identification
**Tool:** Claude (Cowork)
**Task:** Identify redundant URP/HDRP no-op code in the codebase.
**Prompt summary:** Asked Claude to find redundant code relating to URP/HDRP no-ops and confirm whether the flagged functions are used elsewhere.
**Output:** Identified four affected files with snippets that silently break under URP/HDRP (`OnRenderObject`, `OnRenderImage`, `Graphics.DrawProceduralNow`, `Graphics.Blit`); confirmed removal is fully localised. Summary document produced.
**Files affected:** `docs/urp_hdrp_no-ops_analysis.md` (new).

### Session 3 — Mask Mode Strategy Pattern (Design Doc §5.4 & IMaskMode.cs)
**Tool:** Claude (Cowork)
**Task:** Write the Mask Mode Strategy Pattern design section and finalise `IMaskMode.cs`.
**Prompt summary:** Asked Claude to write §5.4 (OCP violation, Strategy decision, interface, four implementations, rationale) and finalise `IMaskMode.cs` with `DisabledMaskMode` as a Null Object plus three fully annotated implementations.
**Output:** §5.4 written; `IMaskMode.cs`, `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`, `DisabledMaskMode.cs` finalised with SOLID/GRASP annotations and projected CK metrics.
**Files affected:** `docs/design-document.md` §5.4 and the four `after/` mask-mode files.

### Session 4 — VolumeMaterialBinder Draft & VolumeTextureManager Documentation
**Tool:** Claude (Cowork)
**Task:** Draft `VolumeMaterialBinder.cs` and document `VolumeTextureManager`.
**Prompt summary:** Asked Claude to draft `VolumeMaterialBinder.cs` (with a `VolumeRenderState` readonly struct, 7-member interface, sealed implementation, annotations, projected CK metrics) and document `VolumeTextureManager`.
**Output:** `VolumeMaterialBinder.cs` drafted (WMC=16, CBO≤11); `VolumeTextureManager` documented.
**Files affected:** `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder.cs` and decisions doc.

### Session 5 — Remaining Tasks Assessment & Sprint 2 Progress
**Tool:** Claude (Cowork)
**Task:** Review remaining sprint tasks.
**Prompt summary:** Asked Claude to assess complete vs outstanding tasks from PROGRESS.md and project files.
**Output:** Status summary identifying unblocked work and items blocked on other sub-teams.
**Files affected:** None (informational/planning).

### Session 6 — Design Document Scope & Requirements Sections
**Tool:** Claude (Cowork)
**Task:** Write and revise §3 (Scope) and §4 (Requirements Recap).
**Prompt summary:** Asked for condensed §3 and §4 (bullet-free) across multiple iterations.
**Output:** §3 Scope and §4 Requirements Recap written in condensed prose.
**Files affected:** `docs/design-document.md` §3, §4.

### Session 7 — Design Document Condensed & Word Export
**Tool:** Claude (Cowork)
**Task:** Condense the full design document and export as Word.
**Prompt summary:** Asked Claude to condense the document (~1,400 → ~290 lines) keeping all tables, code interfaces, and brief requirements, then export to .docx.
**Output:** Document rewritten to 290 lines; `iDaVIE-Rendering-Design-Document.docx` generated; PROGRESS.md updated.
**Files affected:** `docs/design-document.md`, `iDaVIE-Rendering-Design-Document.docx` (new), `PROGRESS.md`.

### Session 8 — Deliverables Review & Baseline CK Metrics Fix
**Tool:** Claude (Cowork)
**Task:** Review deliverables status and fix TBD baseline CK metrics.
**Prompt summary:** Pasted git output for a detached-HEAD situation, asked for a status check, then to fix TBD entries in `rendering-layer-design.md`.
**Output:** Deliverables status summary; TBD baselines replaced with measured Understand values (WMC=44, CBO=45, RFC=89, LCOM≈0.81, DIT=1).
**Files affected:** `docs/rendering-layer-design.md`.

### Session 9 — Standup Notes
**Tool:** Claude (Cowork)
**Task:** Create and format standup notes for Weeks 1–3.
**Prompt summary:** Asked for a 5-day standup template across three weeks with rotating Scrum Master roles; followed up to fix spacing.
**Output:** `standup-notes.md` with Weeks 1–3, all four members, rotating roles, blank Week 3 templates.
**Files affected:** `docs/standup notes.md`, `standup/standup-log.md`.

### Session 10 — Golden-Image Regression Testing Explanation
**Tool:** Claude (Cowork)
**Task:** Understand "golden-image regression suite across mask modes and colour maps".
**Prompt summary:** Asked what the phrase means in regard to testing.
**Output:** Explanation of golden-image regression testing applied across mask mode and colour map combinations.
**Files affected:** None (informational).

### Session 11 — Unity Test Framework Setup
**Tool:** Claude (Cowork)
**Task:** Set up Unity Test Framework Edit Mode and Play Mode tests.
**Prompt summary:** Asked how to implement play-mode and edit-mode tests; asked Claude to write `EditModeTests.cs` and `PlayModeTests.cs`.
**Output:** `EditModeTests.cs` (7 tests) and `PlayModeTests.cs` (play-mode tests); guidance on assembly definition setup and naming conflicts.
**Files affected:** `refactoring-examples/team3/tests/EditModeTests.cs`, `PlayModeTests.cs`.

### Session 12 — CodeScene Report Guidance
**Tool:** Claude (Cowork)
**Task:** Understand how to generate a CodeScene report.
**Prompt summary:** Asked how to write a CodeScene report given an existing account.
**Output:** Step-by-step guidance on generating an on-demand CodeScene PDF report and using Code Health scores for before/after impact.
**Files affected:** None (informational).

### Session 13 — Git Workflow Support
**Tool:** Claude (Cowork)
**Task:** Git commands throughout the project.
**Prompt summary:** Multiple git questions: pulling, branching, pushing, discarding changes, resolving detached HEAD during rebase.
**Output:** Git commands provided; resolved detached HEAD with `git rebase --continue` then push; resolved push rejection with `git pull --rebase`.
**Files affected:** None (git operations only).

### Session 14 — Presentation Preparation
**Tool:** Claude (Cowork)
**Task:** Prepare for the Sprint 2 presentation/interview.
**Prompt summary:** Asked Claude to assess project files and identify done vs open work, and where to start for the presentation.
**Output:** Status summary, CK metric deltas (WMC 97→12, LCOM 0.95→0.0, 46-file cycle broken), and the four core design decisions (DD-01 to DD-04).
**Files affected:** None (informational/planning).

### Session 15 — AI Usage Log
**Tool:** Claude (Cowork)
**Task:** Review all sessions and commits and populate the AI usage log.
**Prompt summary:** Asked Claude to review all prompts and commits and update the log with Damien's work.
**Output:** Sessions 1–15 written into `AI-USAGE-LOG.md` under Damien O Brien's section.
**Files affected:** `AI-USAGE-LOG.md`.

---

# Sub-team 4 — Interaction System

Newest entries on top.

## 2026-05-27 — Interface contract documents for cross-team dependencies

**Author:** Liang Chen Yu (Sprint 2 — PO Liaison) · **Tool:** Claude · **Model:** Sonnet 4.6
**Where used:** Interface contract messages sent to Sub-teams 1, 3, 5, and 7 (Architecture, Rendering, Feature System, Persistence)
**Prompt summary:** Asked Claude to draft tailored interface contract messages for each dependency direction, plus an InteractionSystemState schema for the Persistence sub-team (fields, version, fallback behaviour).
**Where it helped:** Produced four tailored contracts and the InteractionSystemState schema; saved ~45 minutes of drafting.
**Where it failed / was wrong:** Consistently used wrong sub-team numbers — labelled by spec-assigned number rather than the internal Team Alpha identities.
**Human reviewer:** Corrected all sub-team numbers against spec before sending; added missing paint sub-state fields (brush size, active source ID, mask mode).

## 2026-05-27 — NUnit unit test generation for all three worked examples

**Author:** Arnav (Sprint 2 — Quality Champion) · **Tool:** Claude · **Model:** Sonnet 4.6
**Where used:** `refactoring-examples/sub-team-4/` — GazeProviderTests.cs, CollaboratorTests.cs, VoiceCommandTests.cs
**Prompt summary:** Asked Claude to generate NUnit unit test prompts for Cursor with exact class names, method signatures, and assert conditions for all three test files.
**Where it helped:** Produced all three test files with correct signatures and asserts — directly usable as unit-test evidence.
**Where it failed / was wrong:** Generated a test asserting `GazeRay.direction`, invalid after `IGaze` was updated to use `GazeFocusPoint`.

## 2026-05-26 — VolumeInputController decomposition into collaborator classes

**Author:** Arnav (Sprint 2 — Quality Champion) · **Tool:** Cursor (Claude backend) · **Model:** Sonnet 4.6
**Where used:** `refactoring-examples/sub-team-4/2-volume-input/` — LocomotionController.cs, InteractionController.cs, BrushController.cs, VignetteController.cs, CursorInfoFormatter.cs, QuickMenuPositioner.cs
**Prompt summary:** Asked Cursor to decompose VolumeInputController into six focused collaborators with specified responsibilities, interface names, and delegation wiring.
**Where it helped:** Correct interface structure, delegation pattern, and `BuildCollaborators()` wiring; reduced VolumeInputController to a thin router.
**Where it failed / was wrong:** InteractionController had a 16-parameter telescope constructor; IBrushController had 10 public members (exceeds ISP limit of 7).
**Human reviewer:** Arnav — restructured parameter passing with Action/Func delegates; documented the IBrushController trade-off.

## 2026-05-26 — VolumeInputController refactor to SteamVR router

**Author:** Colin Forde (Sprint 2 — Tech Lead) · **Tool:** Cursor · **Model:** Agent (Composer)
**Where used:** `Assets/Scripts/VolumeData/VolumeInputController.cs`, `Assets/Scripts/Interaction/`
**Prompt summary:** Asked Cursor to refactor VolumeInputController to a SteamVR router only, extracting locomotion, interaction, brush, vignette, cursor, menu, and gaze into separate collaborators.
**Where it helped:** Created the full Interaction/ folder (~631-line router, 7 interfaces, all implementations) in one session.
**Where it failed / was wrong:** Editor sometimes showed the old 1635-line buffer rather than the on-disk file, causing apparent regressions.
**Human reviewer:** Colin Forde reloaded each file from disk and verified compilation in Unity before committing.

## 2026-05-26 — State pattern applied to LocomotionState for Worked Example 1

**Author:** Shea (Sprint 2 — Scrum Master) · **Tool:** Claude · **Model:** Sonnet 4.6
**Where used:** Worked Example 1 — `refactoring-examples/sub-team-4/2-volume-input/`, UML state machine diagram
**Prompt summary:** Asked Claude to explain why the enum switch is not a real State pattern and produce ILocomotionState plus skeletons for IdleState, MovingState, ScalingState.
**Where it helped:** Explained the distinction, produced ILocomotionState and correct skeletons — directly usable.
**Where it failed / was wrong:** None.
**Human reviewer:** Shea verified all six states and entry/exit actions against the existing LocomotionState enum.

## 2026-05-25 — Voice command refactor implementation

**Author:** Colin Forde (Sprint 2 — Tech Lead) · **Tool:** Cursor · **Model:** Agent (Composer)
**Where used:** `Assets/Scripts/VolumeData/Voice/`, `VolumeCommandController.cs`
**Prompt summary:** Asked Cursor to refactor voice commands using the Command pattern and IVoiceRecogniser interface with minimal out-of-scope changes.
**Where it helped:** Created the full Voice/ package and thinned VolumeCommandController to an orchestrator.
**Where it failed / was wrong:** Generated a static VoiceCommandRegistry API conflicting with the instance-based test spec.
**Human reviewer:** Colin Forde kept the static production API and added a test-local registry to bridge the mismatch.

## 2026-05-20 — CK metrics interpretation for god class argument

**Author:** Liang Chen Yu (Sprint 1 — Quality Champion) · **Tool:** Claude · **Model:** Sonnet 4.6
**Where used:** Codebase Analysis Report §5.1 (P-01 God Class), docs/sub-team-4/
**Prompt summary:** Asked Claude to explain WMC=79, CBO=31, RFC=79 in plain language and why they breach thresholds.
**Where it helped:** Clear explanations; used directly to frame §5.1.
**Where it failed / was wrong:** Could not independently verify the numbers — worked only from values in the prompt.
**Human reviewer:** Cross-checked all CK numbers against Understand output and the Day 2 baseline before inclusion.

## 2026-05-19 — AI tool usage log template generation

**Author:** Liang Chen Yu (Sprint 1 — Quality Champion) · **Tool:** Claude · **Model:** Opus 4.7
**Where used:** AI Tool Usage Log document (initial template)
**Prompt summary:** Asked Claude to create a six-column template (tool, model, prompt class, where it helped, where it failed, what the human did instead).
**Where it helped:** Produced a Word template with all six columns, saving ~30 minutes.
**Where it failed / was wrong:** Chose .docx without being asked; added 14 empty placeholder rows and boilerplate.
**Human reviewer:** Liang Chen Yu removed the unrequested rows and matched the repo schema.

## 2026-05-19 — Initial repo scope mapping

**Author:** Colin Forde (Sprint 1 — Scrum Master) · **Tool:** Cursor · **Model:** Agent (Composer)
**Where used:** Sprint 1 codebase exploration — team scope mapping
**Prompt summary:** Asked Cursor to map the repo with focus on the interaction system (VolumeInputController, menus, voice, VR keyboard, rendering links).
**Where it helped:** Mapped the interaction-system scope and identified cross-team rendering dependencies.
**Where it failed / was wrong:** Could not replace reading SteamVR input bindings directly in the Unity scene.
**Human reviewer:** Colin Forde traced SteamVR references and cross-checked against brief §6.4.

---

# Sub-team 5 — Networking / Server

*Source: `docs/team-5/ai-log.md` (the canonical `ai-tool-log/sub-team-5.md` has no entries).*

One row per substantive AI assist.

| Date | Author | Tool / Model | Where it helped | Where it failed |
|---|---|---|---|---|
| 2026-05-20 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Summarised product owner responsibilities from the project brief — backlog management, sprint prioritisation, and acceptance criteria ownership | Generic Scrum duties not relevant to the project included and removed manually |
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | Produced the initial three-way split of `FeatureSetManager` into `FeatureCatalog`, `FeatureSetService`, and `FeatureVisualiser` with correct layer assignments; surfaced the dirty-event coupling issue between `Feature` and `FeatureSetRenderer` | Suggested a `FeatureFactory` that wasn't needed; constructor parameter names didn't match the codebase (`cubeMin`/`cubeMax` vs `cornerMin`/`cornerMax`) and were fixed manually |
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | Generated the before/after PlantUML class diagram for `FeatureData` with correct associations, multiplicities, and namespace notes | |
| 2026-05-25 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Generated the initial CK Metrics analysis for the `FeatureSetManager` refactoring (WMC, DIT, NOC, CBO, LCOM from the Understand CSV export) and before/after deltas | LCOM values required manual cross-checking — column mapping diverged from the chosen CK variant |
| 2026-05-26 | Mark Mannion | Claude Code / Sonnet 4.6 | Guided incremental migration of `FeatureSetManager` callers across 8 files; helped resolve compilation errors as static fields were relocated | Minor namespace suggestions didn't match conventions and were adjusted manually |
| 2026-05-27 | Mark Mannion | Claude Code / Sonnet 4.6 | Scaffolded the VOTable refactoring example (`example-2-votable-export`) including before/after excerpts, interfaces, and PlantUML diagram | |
| 2026-05-27 | Mark Mannion | Claude Code / Sonnet 4.6 | Assisted in updating refactoring examples to align with ADR 008 layering rules | |
| 2026-05-27 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Drafted the CBO before/after comparison table for the VOTable export refactoring; identified the coupling reduction from the new `IVOTableExporter` interface | RFC column absent from the Understand export — left as placeholder and estimated manually |
| 2026-05-27 | Aaron Byrne | Claude Code / Sonnet 4.6 | Drafted `MomentMapRequest` and `MomentMapResult` | |
| 2026-05-28 | Mark Mannion | Claude Code / Sonnet 4.6 | Helped resolve remaining compile-time errors across the feature domain after migration; ensured namespace and interface usage matched ADR 008 | |
| 2026-05-28 | Harry Kennedy | Claude Code / Sonnet 4.6 | Generated the `IFeature` interface and stub `Feature` implementation; assisted with scenario test updates | |
| 2026-05-28 | Harry Kennedy | Claude Code / Sonnet 4.6 | Drafted `IFeatureSystemPort` and `FeatureSystemPort` adapter for the team4 interface contract | |
| 2026-05-30 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Assisted in writing the CK Metrics summary section — explained WMC and CBO delta improvements aligned with the ADR 008 rationale | Initial prose overstated the DIT improvement; moderated after checking raw metric values |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | Drafted `SubTeam5_CK_Metrics.md`: mapped Understand CSV columns onto the CK suite and explained before/after deltas for the Moment Maps and VOTable Export examples | No native RFC column in the export, so RFC was estimated/flagged manually; mapping caveats needed human confirmation |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | Drafted `SubTeam5_Testing_Strategy.md` documenting the three test levels, the tooling table, and coverage gaps | Tooling versions and passing test count verified against `SubTeam5_Tests.csproj` and an actual test run |

---

# Sub-team 6 — Desktop GUI & Client Shell

§10.5 + §9.1 T8. One row per substantive AI assist.

| Date | Author | Tool / Model | Prompt class | Where it helped | Where it failed | What the human did instead | Artefact ID |
|---|---|---|---|---|---|---|---|
| 2026-05-19 | Con | Claude Code (Opus 4.7) | Backlog drafting | Produced full product + sprint backlog mapped to LOs and pitch slots; cited spec sections | Initially confused Team 6 (numeric allocation) with §6.6 (work package); needed correction | Clarified team identity; corrected memory entries | backlog.md, CLAUDE.md |
| 2026-05-19 | Con | Claude Code (Opus 4.7) | backlog-drafting | As week-1 SM, formatted empty stand-up presets for sprints 1–3 and drafted a cross-sub-team Kanban overview | Produced generic per-team rows without each sub-team's real status | Filled in real status from each sub-team's SM after the day-2 sync | standups.md, team-alpha Kanban |
| 2026-05-19 | Mark | Gemini (image generation) | diagram-generation | Clearly displayed the CanvassDesktop structure as a visual diagram | Initial image was disorganised and hard to read | Wrote a more organised, detailed prompt to guide layout | CanvassDesktop diagram |
| 2026-05-19 | Rory | Claude (Opus 4.6, chat) | tool-setup | Walked through SonarQube Cloud project creation, sonar-scanner CLI install, env var config, and scanner command construction | Guessed organisation/project keys wrong (rory-harrington vs roryh06; iDavie-RH vs RoryH06_iDavie-RH) | Retrieved correct keys from SonarQube; ran the scanner on second attempt | SonarQube Cloud project |
| 2026-05-19 | Rory | Claude Code | metric-interpretation | Computed WMC, DIT, NOC, CBO, RFC, LCOM for all 8 Desktop GUI classes; found 12 dead fields and the copy-paste bug at DesktopPaintController line 306 | RFC counts were approximate (~118, ~99) | Cross-referenced RFC estimates against source; accepted with ± note | BNCH-1: ck-metrics.md |
| 2026-05-20 | Jimmy | Perplexity (Comet) | diagram-generation | Generated PlantUML AFTER sequence diagram for the Debug-tab log-line flow using team interface contracts | First attempt omitted ServiceGateway and didn't reference team NFRs | Reviewed against architecture.md; added NFR-MOD-2 and T4 notes | WE2-3: after-debug-sequence-diagram.puml |
| 2026-05-20 | Jimmy | Perplexity (Comet) | code-skeleton | Scaffolded full C# skeleton targeting net8.0 with zero UnityEngine dependencies | Placed LogEntry record inside ILogStream.cs; missed EntriesChanged event | dotnet build locally; restructured LogEntry; confirmed event signature | WE2-4: debug-tab/skeleton/ |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | review | Read §1–10 + Appendices A–C and produced a sub-team-6 deliverables checklist mapping artefacts to spec/LO/SWEBOK | First pass missed several spec items; cited a stale ai-tool-log path | Flagged peer-assessment + Kanban snapshots; corrected pointer | deliverables-checklist.md |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | review | Explained terse backlog tickets (DEPS-1, DEPS-2, ARCH-1) into action lists and drafted command-list questions for Sub-team 4 | Invented example command verbs that were guesses | Flagged as examples; raised the real questions with Sub-team 4 | backlog.md tickets, Sub-team 4 query |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | prose-editing | Converted baseline metric PDFs to markdown, reorganised deliverables into per-section folders, staged/committed in batches | — | Reviewed each diff; held main-branch push until verified | deliverables/ reorg |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Researched the File-tab dependency graph and ran the faithful trace of the Open→load→cube path | First trace used placeholder GUI labels | Walked the live GUI for real button labels/scene paths | ex1-file_tab before-trace |
| 2026-05-20 | Mark | Claude Code (Sonnet 4.6) | ADR-drafting | Drafted ADR-0001 (MVVM split) and ADR-0002 (transport) including context, consequences, compliance tables | Used placeholder CK figures | Inserted actual Day 2 baseline values | adrs/0001, 0002 |
| 2026-05-20 | Rory | Claude (Opus 4.6, chat) | metric-interpretation | Per-file cyclomatic/cognitive complexity, smells, duplication %, debt, A–E rating for 8 classes; top-10 smells incl. axis duplication in DesktopPaintController | Cognitive complexity and duplication % were approximate | Continued attempting SonarQube file-level filtering for official output | BNCH-2 |
| 2026-05-20 | Rory | Claude (Opus 4.6, chat) | tool-setup | Guided CodeScene account creation, repo connection, and hotspot view | N/A | Took the hotspot screenshot, committed as BNCH-3 | BNCH-3 |
| 2026-05-21 | Jimmy | Perplexity (Sonar) | prose-editing | Rewrote the Die Boks sub-team brief for clarity | Initial prompt too vague | Specified "restructure and reformat"; reviewed against spec | brief.md |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | review | Audited existing docs vs the brief to find which design docs existed and what was missing | Reported some concerns "covered" when only partial | Cross-checked each claim against files | gap audit |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | requirements-drafting | Drafted the "direct file I/O that belongs server-side" section and reformatted the Python Console requirement | — | Reviewed against §6.6 split | CanvassDesktop.md, Long-Term-Python-Console.md |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | test-generation | Drafted the UI-Toolkit page-object-pattern testing doc (D5) | First scoped too broadly off the backlog | Directed it to start from D5 docs only | D5-testing/ui-toolkit.md |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | requirements-drafting | Drilled requirements toward the 1–2 page deliverable; advised retiring the orphan table | — | Made the retire/rewrite calls | D1-requirements |
| 2026-05-21 | Rory | Claude (Opus 4.6, chat) | metric-interpretation | Built a Dependency Structure Matrix; fan-in/out, propagation cost; 3 cycles (CD↔TM, CD↔HH, HH↔HMC) with code evidence | DSM from static analysis, not NDepend/DV8 as mandated | Attempted NDepend (failed — Assembly-CSharp.dll won't compile outside Unity); accepted DSM with a note | BNCH-4 |
| 2026-05-21 | Rory | Claude (Opus 4.6, chat) | tool-setup | Guided NDepend install, VS workload setup, .sln generation, project config | Didn't know Unity won't generate Assembly-CSharp.dll outside its compiler; ~2 hours lost | Abandoned NDepend; used static source analysis; logged as retro item | N/A |
| 2026-05-22 | Jimmy | Perplexity (Sonar) | prose-editing | Folded the Technical Work section into the deliverables | Kept it separate initially | Prompted "incorporate the technical work into the deliverables" | brief.md |
| 2026-05-22 | Con | Claude Code (Opus 4.7) | prose-editing | Drafted the week-1 sprint review and sub-team retro | First review thin; needed a re-read of every deliverable | Re-scanned the deliverables folder and rewrote | week1-sprint-review.md, retro |
| 2026-05-22 | Con | Claude Code (Opus 4.7) | prose-editing | Drafted the pitch spine (40-min structure, 25010 slide, God Class slide) + Q&A appendix | — | Reviewed claims (1899-line count, sub-characteristics) against source | pitch-spine.md, qa-practice.md |
| 2026-05-23 | Con | Claude Code (Opus 4.7) | review | Planned the opening 6-min deck section; advised a second mandatory-standards (ISO) slide | — | Decided the extra slide was worth the budget | deck structure |
| 2026-05-25 | Jimmy | Perplexity (Sonar) | review | Identified 6 missing brief items (CK metrics, AI log, Kanban deps, role rotation, diagram rule, diagram placement); prioritised list | Didn't cross-reference all spec sections first pass | Confirmed genuinely-missing vs implicit | brief.md |
| 2026-05-25 | Mark | Claude Code (Sonnet 4.6) | code-skeleton | Fleshed out debug-tab skeleton files from the scaffold | Updates didn't apply on first run (sync issue) | Re-ran; applied cleanly | debug-tab/ |
| 2026-05-25 | Mark | Claude Code (Sonnet 4.6) | code-skeleton | Wrote `debug-tab/before-metrics.md`; corrected rough WMC/CBO/RFC in before-trace.md | Proposed wrong folder; needed to read DebugLogging.cs to confirm method counts | Confirmed location; verified figures against source | debug-tab/before-metrics.md |
| 2026-05-25 | Mark | Claude Code (Sonnet 4.6) | diagram-generation | Audited committed examples after Gaps 1/2/3 closed; updated after-class/dependency diagrams; fixed stale counts | Didn't spot dependency-graph.md was stale until prompted | Pointed out the drift; verified PlantUML renders | after-*.puml, dependency-graph.md |
| 2026-05-25 | Con | Claude Code (Opus 4.7) | review | Built the File-tab scope inventory and a fresh-session verification prompt | Claimed "100% of File-tab logic in CanvassDesktop.cs" vs a teammate's 95% | Challenged the figure; had it re-measure | file-tab-scope.md |
| 2026-05-25 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Produced the File-tab "after" refactor (skeleton + adapters), class diagram, CK metrics | Needed several passes to clarify skeleton vs adapters vs contracts | Installed dotnet SDK, built/ran the skeleton | file-tab/{skeleton,adapters}, ck-metrics.md |
| 2026-05-25 | Con | Claude Code (Opus 4.7) | diagram-generation | Iterated File/Debug-tab Mermaid sequence diagrams to fix parse errors | Repeated failures from HTML entities and `<br/>` in labels | Fed each error back; rendered in VS Code | sequence diagrams |
| 2026-05-26 | Jimmy | Perplexity (Sonar) | review | Incorporated full gap analysis into the brief; final document with all 9 deliverables | — | Reviewed against checklist | brief.md |
| 2026-05-26 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Mirrored the Debug-tab worked example off File-tab; before/after trace+sequence (DTO, smells table, subscription leak) | Some trace rows needed re-checking against source lines | Read each trace/sequence against CanvassDesktop.cs | debug-tab/, file-tab traces |
| 2026-05-26 | Con | Claude Code (Opus 4.7) | metric-interpretation | Ran before/after CK metrics for the File tab; mapped diagrams to brief requirements | — | Confirmed which §-requirements each artefact closes | file-tab/ck-metrics.md, etc. |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | test-generation | Wrote the 12-section `test-strategy.md` (~4 pages); added NDepend artefacts to .gitignore | Integrated existing stub; gitignore gap not spotted until prompted | Prompted the gitignore fix; reviewed all 12 sections | test-strategy.md, .gitignore |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Guided the NDepend measurement workflow; replaced estimates with live tool output for CanvassDesktop | Tried editing .ndproj directly — NDepend overwrote it | Used GUI config; verified each metric against the panel | ndepend-baseline.md |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Hand-counted AFTER metrics for all 10 file-tab classes; updated metrics.md §2.2/§2.3 | Only targeted ck-metrics.md first; metrics.md §2.2 also stale | Confirmed 10-class breakdown matched the files | metrics.md, file-tab/ck-metrics.md |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | prose-editing | Repo crossover audit; merged three MVVM sources into `mvvm-binding-policy.md`; repointed 6 stale refs | Ranked adrs/ refs as medium not error; needed confirmation to retire refactor.md | Approved retire after checking references | mvvm-binding-policy.md |
| 2026-05-27 | Jimmy | Perplexity (Sonar) | review | Checked repo + ClickUp against the brief; prioritised action list (ADR-0002, contracts/, architecture, C4, CI/CD, README) | Couldn't access ClickUp (login); GitHub client-rendered | Connected browser; logged in; verified against backlog.md | deliverables-checklist.md |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | requirements-drafting | Consolidated D1 requirements to 1–2 pages with plain-English NFR explanations | Over-linked to T4/T7 | Had the T4/T7 links removed | requirements.md |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | review | Investigated why FeatureTable.cs was changed the prior week | — | Used the explanation to follow up with the owning team | FeatureTable.cs investigation |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Completed the Debug-tab example; File-vs-Debug parity check; removed duplicate before-metrics.md | Self-flagged a missing Debug-tab ADR stubbed as "not yet written" | Removed the dangling ADR reference | debug-tab/ |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | review | Converted ADR_Log_Improved.docx to markdown; audited ADR-009; fixed transport-contract-no-consumer gap | — | Drove the audit; approved each fix | ADR_Log_Improved.md, architecture.md |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | test-generation | Audited and completed the D5 test strategy; wrote the ViewModel unit tests | Naming drift (iDaVIE.Client.* vs iDaVIE.Desktop.*) | Aligned namespaces across D5 | test-strategy.md, viewmodel-unit-tests.md |
| 2026-05-27 | Mark | Claude Code (Sonnet 4.6) | refactoring-proposal | Gap-analysed D2; drafted the 438-line architecture.md (C4 L3, 25010 drivers, baseline evidence, NFR, GRASP/SOLID) | GRASP only implicit; C4 Mermaid syntax issues | Added GRASP section manually; verified Mermaid renders | architecture.md |
| 2026-05-27 | Mark | Claude Code (Sonnet 4.6) | diagram-generation | Fixed broken PlantUML `implements`; converted both diagrams to Mermaid; added §5.4 GRASP table | First fix didn't render | Pivoted to Mermaid; verified renders | architecture.md |
| 2026-05-27 | Mark | Claude Code (Sonnet 4.6) | review | Found 3 D3 errors (dangling Supersedes, two broken D4 links, duplicate smell rows); added §3.3/§3.4 split tables and UnityBinder<T> | File updated by a teammate mid-session — stale state | Re-read before final edits; verified split tables | mvvm-binding-policy.md |
| 2026-05-27 | Rory | Claude (Opus 4.6, chat) | diagram-generation | Generated UML Component Diagram and SysML BDD (MVVM layers, Service Gateway, ACL, external deps) | Initial diagrams were generic templates | Iterated for iDaVIE-specific detail; rendered via plantuml.com | Component Diagram, SysML BDD |
| 2026-05-27 | Rory | Claude (Opus 4.6, chat) | prose-editing | Slide-by-slide pitch content + speaker notes referencing pitch-spine.md | Some notes too long for a timed talk | Trimmed; made layout decisions in Slides | Pitch deck |
| 2026-05-28 | Jimmy | Perplexity (Sonar) | review | Compiled and formatted all sub-team 6 AI entries into ai-log.md | — | Verified dates and artefact IDs against commit history | ai-log.md |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | diagram-generation | Generated the text-based concern map (PlantUML) mapping 8 concerns to SRP homes, arrows labelled by principle | pitch note said "nine concerns" while architecture.md lists eight | Verified mappings against §5.1/§5.2/§5.4; corrected the slide to eight | concern-map.puml |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | metric-interpretation | Explained LCOM4 vs the brief's normalised LCOM (0–1, ≤0.5) and corrected the metric files | Skeleton files used integer LCOM4 | Switched every metric file to the brief's LCOM | metrics.md, ck-metrics.md |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | review | Read-only repo-wide audit mapping every §6.6 deliverable to the brief; per-problem fix prompts | Told to close gaps #10/#12 but #12 is Day-13 CK re-measurement not yet done | Refused to close unmet gaps; flagged them | gap audit, fix prompts |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | review | Located the Unity 6 migration strategy; built a pitch/Q&A prep plan | — | Used to structure his own prep (defence human-authored per §10.5.6) | pitch prep notes |
| 2026-05-28 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Located all LCOM4 occurrences across 7 files, renamed to LCOM hs, corrected scale (integer → 0–1 HS) | Initial pass was a label rename only — values still on the wrong scale | Prompted correction of numbers and thresholds across all 7 files | ck-metrics.md + 5 others |
| 2026-05-29 | Con | Claude Code (Opus 4.8) | requirements-drafting | Scaffolded Sub-team 1 (gateway) and Sub-team 4 (interaction) interface-contract files with open questions | Couldn't confirm Sub-team 4's real verb/DTO shapes | Will send the open questions to Team 4 | interaction-contract/ |
| 2026-05-29 | Con | Claude Code (Opus 4.8) | review | Back-filled this log's Con/Claude-Code rows by reading session transcripts | Can only see Con's sessions — Mark/Jimmy's Perplexity/Gemini use out of scope | Con to review every back-filled row before freeze | ai-log.md |
| 2026-05-29 | Rory | Claude (Opus 4.6, chat) | metric-interpretation | Static analysis on the original codebase via a Python script; CK metrics for 8 classes; 13 violations across 6 classes | CBO had regex false positives; needed a refined pass | Cross-checked against earlier analysis and the Sprint 1 baseline | CK_Metrics_Static_Analysis_Original.md |
| 2026-05-29 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Identified deltas between hand-counted and Understand values (CanvassDesktop WMC 57→63, CBO/RFC); propagated tool-verified values into 4 metric files | Mixed RFC definitions (CK vs Understand); needed a disambiguation caveat | Provided raw Understand figures; verified each delta; added the RFC note | metrics.md, ck-metrics.md (×3) |
| 2026-05-29 | Mark | Claude Code (Sonnet 4.6) | review | Sub-team 6 + team-level gap analysis (Day 10); Sprint 3 to-do with priority tiers; confirmed ADR-0004 already covered | Mixed blockers with already-covered items | Asked follow-ups to separate real gaps; verified Unity 6 coverage | Sprint 3 to-do |
| 2026-05-30 | Con | Claude Code (Opus 4.8) | review | Checked D1 requirements vs §6.6/§9.2.1/LO2; built a D1 explainer | Asserted "logged in ai-log.md" before any entry existed; surfaced missing elicitation/exclusions subsection | Added this row; left the exclusions decision to a human | D1-requirements-explainer.md |
| 2026-05-30 | Con | Claude Code (Opus 4.8) | review | Reviewed D3 vs brief + D2; reconciled four contract/naming inconsistencies; added "where is the Model" callout | Assumed D2 canonical, but the compiled skeletons showed D2 wrong on IMemoryProbe/LogEntry | Treated committed code as truth; corrected D2 and D3 | architecture.md, mvvm-binding-policy.md |
| 2026-05-30 | Con | Claude Code (Opus 4.8) | review | Walked D3 section-by-section and captured the mental model as an explainer | — | Reviewed each concept; live defence human-authored per §10.5.6 | D3-mvvm-explainer.md |
| 2026-05-30 | Rory | Claude (Opus 4.6, chat) | review | Interview prep material per role (QC metrics, PO requirements tracing), structured LLM-use answers, concepts to explain cold | N/A — prep, not deliverable | Used as a rehearsal framework; answers are the student's own per §10.5.6 | Interview prep notes |
| 2026-05-31 | Con | Claude Code (Opus 4.8) | test-generation | Propagated the corrected ILogStream Observer contract into D5 and the FileDialogAdapter rename into D4 | — | Verified rewritten snippets against committed DebugTabTests.cs/LogStream | test-strategy.md, viewmodel-unit-tests.md, metrics.md |
| 2026-05-31 | Con | Claude Code (Opus 4.8) | prose-editing | Synthesised both AI logs into an AI-usage explainer for cold defence at the panel | — | Con to review against the logs before freeze; live defence human-authored | AI-usage-explainer.md |
| 2026-06-01 | Mark | Claude Code (Opus 4.8) | prose-editing + metric-interpretation | Swept all 5 debug-tab docs to close the S7 gap; corrected test count 29→35; fixed a ck-metrics.md sum error (rows summed 34, header said 29) | Didn't spot the sum error until prompted; missed one doc on the count | Identified the discrepancy; verified the count across all docs | debug-tab/*.md |

---

# Sub-team 7 — Persistence & Data ("Sewe en sestig")

Newest entries on top. Author: Sean Corrigan. All assists: Claude Code (Sonnet 4.6) unless noted.

| Date | Author | Prompt class | Where it helped | Where it failed | What the human did instead |
|---|---|---|---|---|---|
| 2026-06-02 | Sean Corrigan | Data formatting | Formatted the raw SciTools Understand CK-metrics dump into a Google-Docs-pasteable table for the persistence classes | — | Verified figures against the Understand export |
| 2026-06-01 | Sean Corrigan | Artefact generation | Generated two sprint Kanban boards (one per working week) from the Day 1–10 daily stand-up notes | — | — |
| 2026-05-29 | Sean Corrigan | Onboarding / concept | Explained C# record types in the context of the immutable persistence DTOs | — | — |
| 2026-05-28 | Sean Corrigan | Code comprehension | Guided a fixed 9-step walkthrough of the persistence layer (DTOs → serialization → workspace metadata → remaining steps), building design-rationale understanding step by step | — | Drove the walkthrough order and confirmed each step against the source |
| 2026-05-28 | Sean Corrigan | Code comprehension / design rationale | Explained why strings are used instead of enums in the persistence DTOs | — | — |
| 2026-05-27 | Sean Corrigan | Investigation / analysis | Used subagents to read `SPEC.md`, `SUBTEAM.md` and every contract in `state-contracts/` and consolidate them into a single findings file | — | — |
| 2026-05-27 | Sean Corrigan | Investigation / analysis | Identified what to look for when reviewing each team's state contracts, from the persistence perspective | Claude Code launch failed — PowerShell "term not recognized" (PATH) | Fixed the PATH so Claude Code would launch |
| 2026-05-21 | Sean Corrigan | Artefact generation | Built a Kanban board; Day 3/4 catch-up plans; requirements document (7 FRs + 7 NFRs traced to ISO/IEC 25010); explained aggregate invariants; per-state-group class identification; scope/ownership diagram; ready-to-paste CK-baseline prompt | `npm install` failed — "npm not recognised" blocked Claude Code setup | Resolved the npm PATH / install manually |
| 2026-05-20 | Sean Corrigan | Onboarding / code comprehension | Fork & branch strategy; reading the codebase through a persistence lens; the astronomy domain (FITS, WCS, HDU selection, feature catalogues); CK metrics framework; checked for an existing defaults/preferences framework; Worked Example 1 (extracting `RenderViewState`) | — | — |
| 2026-05-18 | Sean Corrigan | Onboarding / concept | Explained C#-unique keywords against Java/C analogues to build language fundamentals before reading the persistence codebase | — | Checked explanations against actual repo code |

---

## Collation status

| # | Sub-team | Entries | Source |
|---|---|---|---|
| 1 | Architecture / Micro-kernel | 28 (David; grouped by date) | `ai-tool-log/sub-team-1.md` |
| 2 | Native Plug-ins | 11 | `ai-tool-log/sub-team-2.md` |
| 3 | Rendering & Compute | 40 (4 authors) | `ai-tool-log/sub-team-3.md`|
| 4 | Interaction System | 9 | `ai-tool-log/sub-team-4.md` |
| 5 | Networking / Server | 15 | `docs/team-5/ai-log.md` |
| 6 | Desktop GUI & Client Shell | 70 | `ai-tool-log/sub-team-6.md` |
| 7 | Persistence & Data | 10 | `ai-tool-log/sub-team-7.md` |
