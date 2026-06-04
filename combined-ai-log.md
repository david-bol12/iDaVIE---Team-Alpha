# Team Alpha — Combined AI Tool Usage Log

All sub-team AI logs compiled into a single document. Each sub-team's original format is preserved; sections are separated by horizontal rules. Sub-teams with no log entries are noted as such.

Sources:
- `ai-tool-log/sub-team-1.md` through `sub-team-7.md` (canonical logs)
- `docs/team-5/ai-log.md` (sub-team 5 local log — has content not in the canonical stub)
- `docs/sub-team-6/ai-log.md` is identical to the canonical sub-team-6 log and is omitted here
- `AI-USAGE-LOG.md` (root) is identical to the canonical sub-team-3 log and is omitted here

---

---

# Sub-team 1 — Architecture / Micro-kernel

*No log entries recorded.*

---

---

# Sub-team 2 — Persistence & Data

See [`ai-tool-log/README.md`](ai-tool-log/README.md) for schema. Newest entries on top.

---

## 2026-06-02 — Conformance test plan for native plug-ins

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/ConformancePlan.md`
- **Prompt summary:** Asked Claude to draft a conformance test plan table covering each ABI function, input type, and pass criteria for the three native plug-ins.
- **Where it helped:** Generated the table structure and populated entries for FITS file ops and WCS transforms quickly.
- **Where it failed / was wrong:** Several pass criteria for WCS round-trip tolerance were placeholder values — had to research and replace with values from the Starlink AST documentation.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — CK metrics analysis for plugin layer (LCOM, DIT, NOC, RFC, CBO)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Verbal/session analysis — figures fed into design document and pitch preparation
- **Prompt summary:** Asked Claude to calculate LCOM HS, DIT, NOC, RFC, and CBO for `FitsReader`, `DataAnalysis`, and `AstTool` in the pre-recast state. Also asked it to explain each metric and the adapter/domain threshold distinction.
- **Where it helped:** Correctly calculated LCOM HS = 1.0 for FitsReader and DataAnalysis; identified RFC = 74 for FitsReader as a threshold failure; confirmed NOC = 0 via grep; explained DIT N/A for static classes.
- **Where it failed / was wrong:** Initially calculated DIT = 0 when NDepend reports 1 (it counts System.Object). Also initially claimed LCOM 1.0 for FitsReader was just an adapter artefact and not a design flaw — had to push back on this, it is a genuine flaw.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — Worked refactoring examples (FitsReader memory ownership + WCS Unity removal)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/worked-examples/fits-memory/before.cs`, `after.cs`; `refactoring-examples/data-plugins/worked-examples/wcs-plugin/before.cs`, `after.cs`
- **Prompt summary:** Asked Claude to produce before/after worked examples for FitsReadSubImageFloat memory ownership and AstTool Unity dependency removal, using actual git history as source material.
- **Where it helped:** Spotted the inverted-condition memory leak bug in VolumeDataSet.cs lines 431–434. Accurately reconstructed the before/after from git commits. AstTool example correctly showed the DLL rename from `idavie_native` to `idavie_ast`.
- **Where it failed / was wrong:** The AstTool before.cs included a `TryTransform(Vector3)` helper that never existed in the original — fabricated as an illustration. Kept as a pattern example but not a direct quote.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — Plugin design document (honest reflection of actual recast)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/plugin-design-document.md`
- **Prompt summary:** Gave Claude our draft design doc (written in future tense) and asked it to rewrite it as a past-tense reflection of what was actually built, grounded in the git commit history.
- **Where it helped:** Rewrote in past tense accurately. Caught the two-pattern inconsistency between `[DllImport]` direct and `NativePluginLoader` attribute approaches. Produced the interaction diagram.
- **Where it failed / was wrong:** An earlier version of this doc (plugin-registry.md) invented `IFitsPlugin`, `IAstPlugin`, `IDataPlugin` interfaces and a `PluginRegistry` class that don't exist anywhere in the codebase. Had to be completely discarded.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — Architecture document — kernel ABI (superseded)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/architecture/plugin-registry.md` (superseded)
- **Prompt summary:** Asked Claude to produce an architecture document describing the kernel ABI and plug-in boundary before we had confirmed what was actually built.
- **Where it helped:** The two-boundary diagram and the GetSourceStats AST entanglement explanation were accurate and carried forward into the final doc.
- **Where it failed / was wrong:** Generated `IFitsPlugin`, `IAstPlugin`, `IDataPlugin` interfaces and a `PluginRegistry` class as if they existed — none of them do. The whole document had to be replaced once we checked the actual code.
- **Human reviewer:** Conor Healy

---

## 2026-05-30 — Property-based test examples for FITS round-trip

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/FitsRoundTripTests.cs`
- **Prompt summary:** Asked for property-based tests using FsCheck covering FITS read/write round-trips with synthetic header and pixel data.
- **Where it helped:** FsCheck generator scaffolding and test structure were good, saved a lot of setup time.
- **Where it failed / was wrong:** Tests assumed an in-memory FITS buffer, which CFITSIO does not support — rewrote to use temp files on disk.
- **Human reviewer:** Conor Healy

---

## 2026-05-28 — SRP split design for FitsReader

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/fits-reader/srp-split.md`
- **Prompt summary:** Asked Claude to propose how to split the 59-method FitsReader monolith based on responsibility boundaries in the source.
- **Where it helped:** Identified the five boundaries (file lifecycle, header, image, table, mask) that matched what we implemented. Descriptions were accurate.
- **Human reviewer:** Conor Healy

---

## 2026-05-26 — Isolation strategy for plug-in tests without Unity

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/PluginIsolationStrategy.md`
- **Prompt summary:** Asked how to load and test the native DLLs in a plain .NET test runner with no Unity installation.
- **Where it helped:** Correct explanation of `SetDllDirectory` and `NativeLibrary.SetDllImportResolver`. Useful starting point for the strategy doc.
- **Where it failed / was wrong:** Boilerplate assumed NUnit — had to replace with the actual test runner we're using.
- **Human reviewer:** Conor Healy

---

## 2026-05-22 — Strategy pattern design for downsampling

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/data-analysis/patterns.md`
- **Prompt summary:** Asked Claude to refactor the `bool maxDownsampling` parameter in `DataCropAndDownsample` into a Strategy pattern and show the before/after interface design.
- **Where it helped:** `IDownsampleStrategy` interface and the two concrete implementations were clean and used directly.
- **Human reviewer:** Conor Healy

---

## 2026-05-20 — CK metric explanations for baseline

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Design document intro section
- **Prompt summary:** Asked Claude to explain each CK metric in plain language to help interpret the NDepend baseline output.
- **Where it helped:** Clear explanations, used almost verbatim in the doc intro.
- **Where it failed / was wrong:** Gave slightly different threshold values to what Section 7.1 of the spec states — cross-checked and corrected.
- **Human reviewer:** Conor Healy

---

## 2026-05-19 — Initial codebase exploration

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Sprint planning notes
- **Prompt summary:** Asked Claude to read the plugin interface directory and summarise what each class does, method counts, and external dependencies.
- **Where it helped:** Quick summary of FitsReader (59 methods, CFITSIO, Unity), DataAnalysis (delegate pattern), AstTool (Starlink AST). Saved manually reading ~800 lines.
- **Human reviewer:** Conor Healy

---

---

# Sub-team 3 — Rendering & Compute

> **Purpose:** Documents all AI-assisted work completed by each team member for transparency and academic integrity.

---

## Ciallian Bain

### Session 1 — iDaVIE Setup & Requirements
**Tool:** Claude (Cowork)
**Task:** Understanding how to build iDaVIE from source.
**Prompt summary:** Asked how to set up and build iDaVIE on Unity 2021, including all dependencies.
**Output:** Full build requirements list (Unity 2021.3 LTS, Visual Studio C++ workload, CMake, vcpkg, SteamVR/OpenVR). Sourced from official iDaVIE docs and GitHub BUILD.md.
**Files affected:** None (informational).

---

### Session 2 — SOLID Violation Audit
**Tool:** Claude (Cowork)
**Task:** Structured SOLID audit of `VolumeDataSetRenderer`.
**Prompt summary:** Requested a SOLID violation audit using CONTEXT.md and the dependency map as source material. Asked AI to clarify scope before starting.
**Output:** Produced a structured SOLID audit document covering all five SOLID principles with specific line-number references to violations in `VolumeDataSetRenderer`.
**Files affected:** SOLID audit document (docs/team3/).

---

### Session 3 — Architecture Diagram Syntax Fixes
**Tool:** Claude (Cowork)
**Task:** Fix PlantUML syntax errors across diagram files.
**Prompt summary:** Diagrams were failing to render due to multiple syntax errors.
**Output:** Fixed 14+ issues across `architecture.puml` — replaced Unicode dashes with ASCII `--`, escaped angle brackets (`~<`, `~>`), fixed arrow glyphs, added component aliases for multi-line names.
**Files affected:** `diagrams/architecture.puml`.

---

### Session 4 — CK Metrics Tooling & Golden Image Testing
**Tool:** Claude (Cowork)
**Task:** Two questions: (1) what is golden image regression testing and can AI produce one; (2) how to document that CK metrics tools failed.
**Prompt summary:** Asked what "golden image regression suite across mask modes and colour maps" means and whether it could be done. Also asked how to honestly document that Understand, NDepend, and SonarQube free trials failed to work with the Unity project structure.
**Output:** Explanation of golden image testing and its scope.

---

### Session 5 — CK Metrics in NDepend
**Tool:** Claude (Cowork)
**Task:** Locating CK metrics within NDepend's interface.
**Prompt summary:** Asked where to find CK metrics in an NDepend report.
**Output:** Explained where each CK metric maps in NDepend (dashboard, Queries & Rules panel, CQLinq), and provided a sample CQLinq query to extract per-type metrics directly.
**Files affected:** None (informational).

---

### Session 6 — Metrics Consistency Pass (Correct Understand Values)
**Tool:** Claude (Cowork)
**Task:** Apply verified Understand metric values consistently across all documents.
**Prompt summary:** Correct CK values (from the Understand tool run) needed to replace earlier estimates across all docs and code files.
**Output:** Updated metrics-worksheet, design-document, both refactoring example READMEs, all five `after/*.cs` files, PROGRESS.md, CONTEXT.md, and two PlantUML diagrams. Documented three classes (VolumeRenderCoordinator, VolumeTextureManager, VolumeMaterialBinder) exceeding LCOM 0.5 target as a lifecycle-phase artefact, not a design flaw.
**Files affected:** `docs/team3/metrics-worksheet.md`, `docs/team3/design-document.md`, both example READMEs, all `after/*.cs` files, `PROGRESS.md`, `CONTEXT.md`, `diagrams/class-before.puml`, `diagrams/vdsr-dependencies.puml`.

---

### Session 7 — Sprint 2 Task Triage & Document Completion
**Tool:** Claude (Cowork)
**Task:** Multiple tasks — ClickUp task status review, document sections written, merge conflict resolved, interview study guide created.

**Prompt summary (multiple prompts in one session):**
1. Tracked ClickUp to-do list and ticked off which tasks were done vs remaining.
2. Verified work done on "compute CK metrics" and "SOLID/GRASP annotation pass".
3. Assisted in doing various tasks assigned on kanban board.

**Output:**
- Task triage: 8 tasks confirmed complete, 4 identified as unblocked, 3 flagged as blocked on team action.
- Explanation of verification tasks: after/ code already contained full CK projection tables and SOLID/GRASP annotations inline; READMEs surfaced and summarised this evidence.
- `docs/team3/design-document.md` §6.2 and §6.3 written: Day 13 10-class projection table (all 6 CK metrics) and delta narrative (WMC 74→20, CBO cycle broken, LCOM 0.81→0 per class).
- `docs/team3/metrics-worksheet.md` populated: Day 13 projected column, before/worst/best delta table, justification prose for WMC, CBO, and LCOM.
- `PROGRESS.md` merge conflict (lines 64–112) resolved, sprint tasks marked done.

**Files affected:** `docs/team3/design-document.md`, `docs/team3/metrics-worksheet.md`, `PROGRESS.md`, both example READMEs.

---

## Cathal Ging

_No entries recorded._

---

## Chris

### Session 1 — SonarQube Setup & Code Health
**Tool:** Claude Code
**Task:** Configure SonarQube to scan the iDaVIE project locally.
**Prompt summary:** Asked Claude to make SonarQube work on the full project so results could be viewed on localhost.
**Output:** Configured the SonarQube scanner to run against the iDaVIE codebase and display results via the local SonarQube server.
**Files affected:** SonarQube configuration files (scanner setup).

---

### Session 2 — Unit Tests for Refactoring Examples
**Tool:** Claude Code
**Task:** Write unit tests against the refactored C# code and document what is being tested.
**Prompt summary:** Asked Claude to find the refactoring examples and generate unit tests for the new refactored code, place them in the refactoring folder, and create a markdown file explaining what each test covers and why.
**Output:** Unit test file(s) for refactored classes; accompanying markdown explanation of test coverage rationale.
**Files affected:** `refactoring-examples/team3/` (new test file and markdown).

---

### Session 3 — Test Strategy Updated to Reflect New Tests
**Tool:** Claude Code
**Task:** Align `test-strategy.md` with the unit tests written in Session 3.
**Prompt summary:** Asked Claude to update the test strategy document to reflect the new tests in the refactoring examples, keeping it concise, useful, and relevant, and to note short- and long-term codebase impact.
**Output:** Revised `test-strategy.md` with updated test coverage description and impact analysis. Committed to branch.
**Files affected:** `docs/team3/test-strategy.md`.

---

### Session 4 — Revised Design Document (Section 5 Merge)
**Tool:** Claude Code
**Task:** Merge Section 5 content from the original design document into the revised design document.
**Prompt summary:** Asked Claude to read the design document, extract Section 5, and write it into the revised design document in a clear, concise style matching the existing content. Also asked Claude to check the document wasn't too long before committing.
**Output:** `revised_design_document.md` updated with Section 5 content; committed to branch.
**Files affected:** `docs/team3/revised_design_document.md` (or equivalent).

---

### Session 5 — VolumeDataSetRenderer Top-Down Explanation
**Tool:** Claude Code
**Task:** Understand the existing `VolumeDataSetRenderer.cs` class before beginning refactoring work.
**Prompt summary:** Asked for a top-down explanation of what `VolumeDataSetRenderer.cs` does, its purpose, its dependencies, and how it affects other parts of the system.
**Output:** Detailed written explanation of the class covering its role in the rendering pipeline, key dependencies (Unity, SteamVR, shader property bindings), and downstream effects on camera, masking, and texture systems.
**Files affected:** None (informational).

---

### Session 6 — Sequence Diagrams (Before & After)
**Tool:** Claude Code
**Task:** Create PlantUML sequence diagrams for one render frame, both before and after refactoring.
**Prompt summary:** Asked Claude to create a new sequence diagram using the revised design document, the previous sequence diagram file, and the refactored examples. Also asked for an initial sequence diagram based on the original `VolumeDataSetRenderer`.
**Output:** Two PlantUML sequence diagrams: one representing the original monolithic render frame flow, one showing the refactored multi-class flow.
**Files affected:** `diagrams/sequence-render-frame.puml` (new/updated), additional diagram file for before state.

---

### Session 7 — Brief Compliance Evaluation & Document Push
**Tool:** Claude Code
**Task:** Evaluate whether Sub-team 3 has met all requirements in the assignment brief; push deliverable document to GitHub.
**Prompt summary:** Provided the assignment brief PDF and asked Claude to evaluate whether Team 3's rendering engine deliverables met everything in the brief. Followed up asking Claude to re-check after a pull and then action the first identified gap.
**Output:** Compliance gap analysis against the brief. First identified gap actioned. Deliverable document pushed to GitHub.
**Files affected:** Relevant deliverable document (docs/team3/); GitHub push.

---

### Session 8 — Unity 6 Migration Plan in Test Strategy
**Tool:** Claude Code
**Task:** Ensure the Unity 6 migration plan was properly reflected in `test-strategy.md`.
**Prompt summary:** Asked Claude to check if a migration plan for Unity 6 was included in `test-strategy.md`. Confirmed adding a Section 10 covering Unity 6 migration testing after verifying it was also referenced in the design document.
**Output:** Section 10 (Unity 6 Migration Testing) added to `test-strategy.md`.
**Files affected:** `docs/team3/test-strategy.md`.

---

### Session 9 — AI Usage Log Population
**Tool:** Claude Code
**Task:** Populate the AI usage log for Chris Jo from Claude session history.
**Prompt summary:** Asked Claude to read the existing AI usage log for format reference and fill in all sessions from Claude history.
**Output:** Sessions 1–10 written into `AI-USAGE-LOG.md` under Chris Jo's section.
**Files affected:** `AI-USAGE-LOG.md`.

---

## Damien O Brien

### Session 1 — Design Document Questions (URP/HDRP & No-ops)
**Tool:** Claude (Cowork)
**Task:** Understand URP vs HDRP rendering pipelines and the concept of no-ops before working on the design document.
**Prompt summary:** Asked what the difference is between the URP and HDRP rendering pipelines, and what "no-ops" means in reference to them.
**Output:** Explanation of URP (broad compatibility, lower overhead, fewer features) vs HDRP (high-end visuals, more features, higher hardware requirements), and a definition of no-ops in the rendering context (operations that execute but have no effect on output).
**Files affected:** None (informational).

---

### Session 2 — URP/HDRP Redundant Code Identification
**Tool:** Claude (Cowork)
**Task:** Identify redundant code in the iDaVIE codebase specific to URP and HDRP render pipeline no-ops.
**Prompt summary:** Asked Claude to find bits of code that are redundant and could be removed, specifically in relation to no-ops in URP and HDRP render pipelines. Followed up by asking whether the flagged functions are used anywhere else in the codebase.
**Output:** Identified four affected files with code snippets that silently break under URP/HDRP (`OnRenderObject`, `OnRenderImage`, `Graphics.DrawProceduralNow`, `Graphics.Blit`). Confirmed via codebase search that none of the flagged calls appear elsewhere — removal is fully localised. A summary document was produced.
**Files affected:** `docs/urp_hdrp_no-ops_analysis.md` (new).

---

### Session 3 — Mask Mode Strategy Pattern (Design Doc §5.4 & IMaskMode.cs)
**Tool:** Claude (Cowork)
**Task:** Write the Mask Mode Strategy Pattern design document section and finalise `IMaskMode.cs`.
**Prompt summary:** Asked Claude to write §5.4 of the design document covering the OCP violation, Strategy pattern decision, interface design, four implementations, and pattern-choice rationale. Also asked to finalise `IMaskMode.cs` with `DisabledMaskMode` as a Null Object, and promote `ApplyMaskMode`, `InverseMaskMode`, and `IsolateMaskMode` from stubs to fully annotated implementations with XML docs, null-guards, and mutual keyword exclusion.
**Output:** `docs/design-document.md` §5.4 written. `IMaskMode.cs`, `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`, `DisabledMaskMode.cs` finalised with SOLID/GRASP annotations and projected CK metrics.
**Files affected:** `docs/design-document.md` §5.4, `refactoring-examples/team3/example2-MaskModes/after/IMaskMode.cs`, `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`.

---

### Session 4 — VolumeMaterialBinder Draft & VolumeTextureManager Documentation
**Tool:** Claude (Cowork)
**Task:** Draft `VolumeMaterialBinder.cs` and document `VolumeTextureManager`.
**Prompt summary:** Asked Claude to draft `VolumeMaterialBinder.cs` with a `VolumeRenderState` readonly struct, a 7-member `IVolumeMaterialBinder` interface, and the sealed implementation with full SOLID/GRASP inline annotations and projected CK metrics. Also asked for documentation of `VolumeTextureManager`.
**Output:** `VolumeMaterialBinder.cs` drafted with WMC=16, CBO≤11 confirmed against targets. `VolumeTextureManager` documented.
**Files affected:** `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder.cs`, `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/Rationale/VolumeMaterialBinder-decisions.md`.

---

### Session 5 — Remaining Tasks Assessment & Sprint 2 Progress
**Tool:** Claude (Cowork)
**Task:** Review remaining sprint tasks and assess what was done vs. outstanding.
**Prompt summary:** Asked Claude to assess what sprint tasks were complete and what remained outstanding based on PROGRESS.md and the project files.
**Output:** Status summary of completed vs. remaining tasks, identifying unblocked work and items blocked on other sub-teams.
**Files affected:** None (informational/planning).

---

### Session 6 — Design Document Scope & Requirements Sections
**Tool:** Claude (Cowork)
**Task:** Write and revise §3 (Scope) and §4 (Requirements Recap) of the design document.
**Prompt summary:** Asked for the markdown code for §3 shortened significantly, and §4 without bullet points to condense presentation. Multiple iterations — first draft of §3 produced, then shortened; §4 written with CK metrics targets table and design standards, then condensed to remove bullet points and unnecessary information.
**Output:** §3 Scope (Sub-team 3 ownership, explicit out-of-scope items) and §4 Requirements Recap (invariants, functional requirements, CK metric targets, design standards) written in condensed prose.
**Files affected:** `docs/design-document.md` §3, §4.

---

### Session 7 — Design Document Condensed & Word Document Export
**Tool:** Claude (Cowork)
**Task:** Condense the full design document and export as a Word (.docx) file.
**Prompt summary:** Asked Claude to condense the design document (down from ~1,400 lines to ~290) keeping all tables, code interfaces, and brief requirements. Then asked for a Word document export of the condensed version.
**Output:** `docs/design-document.md` rewritten from 1,406 lines to 290. `iDaVIE-Rendering-Design-Document.docx` generated with blue headings, header/footer, and page numbers. `PROGRESS.md` updated with design document tasks marked complete.
**Files affected:** `docs/design-document.md`, `iDaVIE-Rendering-Design-Document.docx` (new), `PROGRESS.md`.

---

### Session 8 — Deliverables Review & Baseline CK Metrics Fix
**Tool:** Claude (Cowork)
**Task:** Review sprint deliverables status and fix TBD baseline CK metrics in `rendering-layer-design.md`.
**Prompt summary:** Pasted git output showing a rebase/detached HEAD situation and asked for help resolving it. Then asked "so how are we looking now in respect to the deliverables?" to get a status check. Followed up by asking Claude to fix TBD entries in `rendering-layer-design.md`.
**Output:** Deliverables status summary (design doc, refactoring examples, diagrams, metrics worksheet — all confirmed in good shape). TBD baseline CK metrics in `rendering-layer-design.md` replaced with measured Understand values (WMC=44, CBO=45, RFC=89, LCOM=~0.81, DIT=1).
**Files affected:** `docs/rendering-layer-design.md`.

---

### Session 9 — Standup Notes
**Tool:** Claude (Cowork)
**Task:** Create and format standup notes for the full project period (Weeks 1–3).
**Prompt summary:** Asked Claude to generate a 5-day standup notes template for the team across three weeks, with rotating Scrum Master roles. Followed up pasting the content back and asking to fix formatting (missing spacing between team member sections).
**Output:** `standup-notes.md` with Weeks 1–3, all four team members, rotating roles (Cathal SM Week 1, Damien SM Week 2, Ciallian SM Week 3), and blank templates for Week 3.
**Files affected:** `docs/standup notes.md`, `standup/standup-log.md`.

---

### Session 10 — Golden-Image Regression Testing Explanation
**Tool:** Claude (Cowork)
**Task:** Understand what "golden-image regression suite across mask modes and colour maps" means in the context of the test strategy.
**Prompt summary:** Asked what the phrase "Golden-image regression suite across mask modes and colour maps" means in regard to testing.
**Output:** Explanation of golden-image regression testing: comparing current output against approved baseline images to catch unintended visual regressions, applied across all mask mode and colour map combinations.
**Files affected:** None (informational).

---

### Session 11 — Unity Test Framework Setup
**Tool:** Claude (Cowork)
**Task:** Set up Unity Test Framework with Edit Mode and Play Mode tests for the refactored code.
**Prompt summary:** Asked how to implement software testing including play-mode tests under Unity Test Framework for renderer behaviour and edit-mode tests for non-Unity parts. Asked Claude to check the tests folder inside refactoring-examples. Subsequently pasted partial Unity test file setup and asked Claude to write `EditModeTests.cs` and `PlayModeTests.cs` covering mask mode shader keywords, colour map enum sanity, and play-mode material apply behaviour.
**Output:** `EditModeTests.cs` (7 tests covering `ColorMapEnumTests` and `MaskModeShaderKeywordTests`) and `PlayModeTests.cs` (play-mode tests covering apply, disable, swap, and frame-persistence behaviour). Guidance on assembly definition setup and resolving naming conflicts (`iDaVIE.Tests.EditMode`, `iDaVIE.Tests.PlayMode`).
**Files affected:** `refactoring-examples/team3/tests/EditModeTests.cs`, `refactoring-examples/team3/tests/PlayModeTests.cs`.

---

### Session 12 — CodeScene Report Guidance
**Tool:** Claude (Cowork)
**Task:** Understand how to generate a CodeScene report for the refactored code.
**Prompt summary:** Asked how to write a CodeScene report for the refactored code given an existing CodeScene account.
**Output:** Step-by-step guidance on generating an on-demand CodeScene PDF report, using Code Health scores to measure before/after refactoring impact, and triggering a fresh analysis once commits are pushed.
**Files affected:** None (informational).

---

### Session 13 — Git Workflow Support
**Tool:** Claude (Cowork)
**Task:** Git commands for pulling, branching, pushing, and resolving conflicts throughout the project.
**Prompt summary:** Multiple git-related questions across sessions: how to pull the latest branch version, how to see branches, how to discard unstaged changes, how to push to a branch, resolving a detached HEAD state during a rebase, and how to unstage/discard changes.
**Output:** Git commands provided for all scenarios. Resolved detached HEAD by running `git rebase --continue` then `git push origin HEAD:Team3-Docs-and-Examples`. Resolved push rejection by running `git pull origin Team3-Docs-and-Examples --rebase` before pushing.
**Files affected:** None (git operations only).

---

### Session 14 — Presentation Preparation
**Tool:** Claude (Cowork)
**Task:** Prepare the team for the Sprint 2 presentation/interview.
**Prompt summary:** Asked Claude to assess the project files and identify what was already done vs. still open for Sprint 3. Asked for guidance on where to start for the presentation.
**Output:** Status summary: design decisions, CK metrics deltas (WMC 97→12, LCOM 0.95→0.0, 46-file cycle broken), and identification of the four core design decisions (DD-01 to DD-04) as the likely focus. Recommended a team walkthrough of the design document.
**Files affected:** None (informational/planning).

---

### Session 15 — AI Usage Log
**Tool:** Claude (Cowork)
**Task:** Review all Claude sessions and git commits for the iDaVIE project and populate the AI usage log.
**Prompt summary:** Asked Claude to review all prompts and commits made regarding the iDaVIE project and update the existing team AI usage log with Damien's work.
**Output:** Sessions 1–15 written into `AI-USAGE-LOG.md` under Damien O Brien's section, drawn from session transcript history and git commit log.
**Files affected:** `AI-USAGE-LOG.md`.

---

---

# Sub-team 4 — Interaction System

See [`ai-tool-log/README.md`](ai-tool-log/README.md) for schema. Newest entries on top.

---

## 2026-05-27 — Interface contract documents for cross-team dependencies

**Author:** Liang Chen Yu (Sprint 2 — PO Liaison)
**Tool:** Claude
**Model:** Sonnet 4.6
**Where used:** Interface contract messages sent to Sub-teams 1, 3, 5, and 7 (Architecture, Rendering, Feature System, Persistence)
**Prompt summary:** Asked Claude to draft tailored interface contract messages for each dependency direction, covering what each sub-team needed from Koffiewinkel and what Koffiewinkel needed in return; also requested an InteractionSystemState schema for the Persistence sub-team including fields, version, and fallback behaviour.
**Where it helped:** Produced four separate tailored contracts and drafted the InteractionSystemState schema with version field, fallback behaviour, and initial field list; saved roughly 45 minutes of drafting time across all four contracts.
**Where it failed / was wrong:** Consistently used wrong sub-team numbers throughout — labelled sub-teams by spec-assigned number (1–7) rather than the internal sub-team identities used within Team Alpha (e.g. confused Sub-team 1/3 Rendering with a different numbering).
**Human reviewer:** Corrected all sub-team numbers against spec before sending; reviewed the state contract schema and added missing paint sub-state fields (brush size, active source ID, mask mode) before the contracts were sent.

---

## 2026-05-27 — NUnit unit test generation for all three worked examples

**Author:** Arnav (Sprint 2 — Quality Champion)
**Tool:** Claude
**Model:** Sonnet 4.6
**Where used:** `refactoring-examples/sub-team-4/` — GazeProviderTests.cs, CollaboratorTests.cs, VoiceCommandTests.cs
**Prompt summary:** Asked Claude to generate NUnit unit test prompts for Cursor with exact class names, method signatures, and assert conditions for all three test files across both worked examples.
**Where it helped:** Produced all three test files with correct class names, method signatures, and assert conditions — directly usable as the unit test evidence in the worked examples.
**Where it failed / was wrong:** Generated a test asserting GazeRay.direction which became invalid after IGaze was updated to remove GazeRay and replace it with GazeFocusPoint — the test did not match the final interface.

---

## 2026-05-26 — VolumeInputController decomposition into collaborator classes

**Author:** Arnav (Sprint 2 — Quality Champion)
**Tool:** Cursor (Claude backend)
**Model:** Sonnet 4.6
**Where used:** `refactoring-examples/sub-team-4/2-volume-input/` — LocomotionController.cs, InteractionController.cs, BrushController.cs, VignetteController.cs, CursorInfoFormatter.cs, QuickMenuPositioner.cs
**Prompt summary:** Asked Cursor to decompose VolumeInputController into six focused collaborator classes using a detailed extraction prompt specifying responsibilities, interface names, and the delegation wiring pattern.
**Where it helped:** Produced correct interface structure, delegation pattern, and BuildCollaborators() wiring for all six classes; VolumeInputController was reduced from a god class to a thin router.
**Where it failed / was wrong:** InteractionController was generated with a 16-parameter constructor (telescope constructor smell); IBrushController had 10 public members, exceeding the ISP limit of 7.
**Human reviewer:** Arnav — manually restructured parameter passing using Action and Func delegates to eliminate the telescope constructor and avoid circular references; documented the IBrushController trade-off explicitly in the worked example.

---

## 2026-05-26 — VolumeInputController refactor to SteamVR router

**Author:** Colin Forde (Sprint 2 — Tech Lead)
**Tool:** Cursor
**Model:** Agent (Composer)
**Where used:** `Assets/Scripts/VolumeData/VolumeInputController.cs`, `Assets/Scripts/Interaction/` (all collaborator files)
**Prompt summary:** Asked Cursor to refactor VolumeInputController to a SteamVR router only, extracting locomotion, interaction, brush, vignette, cursor, menu, and gaze into separate collaborators under `Assets/Scripts/Interaction/`.
**Where it helped:** Created the full Interaction/ folder with ~631-line router, all 7 interfaces, and all collaborator implementations in one session.
**Where it failed / was wrong:** Editor sometimes showed the old 1635-line buffer rather than the updated file on disk, causing apparent regressions mid-session.
**Human reviewer:** Colin Forde reloaded each file from disk after generation; verified the project compiled in Unity before committing.

---

## 2026-05-26 — State pattern applied to LocomotionState for Worked Example 1

**Author:** Shea (Sprint 2 — Scrum Master)
**Tool:** Claude
**Model:** Sonnet 4.6
**Where used:** Worked Example 1 — `refactoring-examples/sub-team-4/2-volume-input/`, UML state machine diagram
**Prompt summary:** Asked Claude to explain why the current enum switch is not a real State pattern and produce the ILocomotionState interface and C# skeletons for IdleState, MovingState, and ScalingState.
**Where it helped:** Explained the distinction between an enum switch and a formal State pattern, produced the ILocomotionState interface definition, and gave correct C# skeletons for all three concrete state classes — directly usable as the Worked Example 1 code skeleton.
**Where it failed / was wrong:** None, output was accurate and usable without correction.
**Human reviewer:** Shea reviewed all three state class skeletons against the existing LocomotionState enum transitions in the source to confirm all six states and entry/exit actions were covered.

---

## 2026-05-25 — Voice command refactor implementation

**Author:** Colin Forde (Sprint 2 — Tech Lead)
**Tool:** Cursor
**Model:** Agent (Composer)
**Where used:** `Assets/Scripts/VolumeData/Voice/` (all files), `Assets/Scripts/VolumeData/VolumeCommandController.cs`
**Prompt summary:** Asked Cursor to refactor voice commands using the Command pattern and IVoiceRecogniser interface with minimal changes outside the interaction/voice scope.
**Where it helped:** Created the full Voice/ package (IVoiceCommand, IVoiceRecogniser, WindowsVoiceRecogniser, VoiceCommandRegistry, VoiceCommandContext, DelegateVoiceCommand, VoiceDesktopUiSync) and thinned VolumeCommandController to an orchestrator.
**Where it failed / was wrong:** Generated a static VoiceCommandRegistry API which conflicted with the test spec expecting an instance-based Register/Lookup pattern.
**Human reviewer:** Colin Forde — kept the static production API; added a test-local registry class inside the test file to bridge the mismatch without changing production code.

---

## 2026-05-20 — CK metrics interpretation for god class argument

**Author:** Liang Chen Yu (Sprint 1 — Quality Champion)
**Tool:** Claude
**Model:** Sonnet 4.6
**Where used:** Codebase Analysis Report — Section 5.1 (P-01 God Class), `docs/sub-team-4/`
**Prompt summary:** Asked Claude to explain WMC=79, CBO=31, RFC=79 in plain language and contextualise why all three breach the spec thresholds (WMC ≤ 20 domain, CBO ≤ 14 domain, RFC ≤ 50) for the god class argument in the Sprint 1 analysis report.
**Where it helped:** Produced clear plain-language explanations of all three metrics and articulated why the combined breach of all three simultaneously strengthens the god class argument; output was used directly to frame Section 5.1 of the report.
**Where it failed / was wrong:** Could not independently verify the numbers — worked only from values reported in the prompt rather than running its own static analysis on the source files.
**Human reviewer:** Cross-checked all quoted CK numbers against the Understand tool output and the Tech Lead's Day 2 baseline document before inclusion in the report.

---

## 2026-05-19 — AI tool usage log template generation

**Author:** Liang Chen Yu (Sprint 1 — Quality Champion)
**Tool:** Claude
**Model:** Opus 4.7
**Where used:** AI Tool Usage Log document (initial template)
**Prompt summary:** Asked Claude to create a template for the team's AI tool usage log with six columns: tool, model, prompt class, where it helped, where it failed, and what the human did instead.
**Where it helped:** Produced a Word document template with all six requested columns, saving approximately 30 minutes of manual formatting.
**Where it failed / was wrong:** Chose .docx format without being asked; added 14 empty placeholder rows and extra boilerplate entries that were not requested.
**Human reviewer:** Liang Chen Yu reviewed the output, removed all unrequested rows and entries, and modified the template to match the sub-team's actual needs and the repo schema.

---

## 2026-05-19 — Initial repo scope mapping

**Author:** Colin Forde (Sprint 1 — Scrum Master)
**Tool:** Cursor
**Model:** Agent (Composer)
**Where used:** Sprint 1 codebase exploration — team scope mapping
**Prompt summary:** Asked Cursor to map the repo with focus on the interaction system: VolumeInputController, menus, voice, VR keyboard, and links to rendering.
**Where it helped:** Mapped the interaction system scope across all relevant files and identified cross-team rendering dependencies used in the dependency table.
**Where it failed / was wrong:** Could not replace reading the SteamVR input bindings directly in the Unity scene — not visible from source files alone.
**Human reviewer:** Colin Forde traced SteamVR references in the Unity project and cross-checked against brief §6.4 before finalising the scope map.

---

---

# Sub-team 5 — Networking / Server

*Source: `docs/team-5/ai-log.md` (the canonical `ai-tool-log/sub-team-5.md` has no entries).*

One row per substantive AI assist.

| Date | Author | Tool / Model | Where it helped | Where it failed |
|---|---|---|---|---|
| 2026-05-20 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Summarised product owner responsibilities from the project brief — backlog management, sprint prioritisation, and acceptance criteria ownership | Generic Scrum duties not relevant to the project included and removed manually |
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | Produced the initial three-way split of `FeatureSetManager` into `FeatureCatalog`, `FeatureSetService`, and `FeatureVisualiser` with correct layer assignments (`iDaVIE.Domain`, `iDaVIE.Application`, `iDaVIE.Infrastructure.Unity`); surfaced the dirty-event coupling issue between `Feature` and `FeatureSetRenderer` | Suggested a `FeatureFactory` helper class that wasn't needed — dismissed; constructor parameter names in the generated `Feature` skeleton didn't match the existing codebase (`cubeMin`/`cubeMax` vs `cornerMin`/`cornerMax`) and had to be fixed manually |
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | Generated the before/after PlantUML class diagram for `FeatureData` (`FeatureData(Before).puml`, `FeatureData(Refactored).puml`) with correct associations, multiplicities, and namespace notes | |
| 2026-05-25 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Generated the initial CK Metrics analysis for the `FeatureSetManager` refactoring, computing WMC, DIT, NOC, CBO, and LCOM values from the SciTools Understand CSV export and summarising the before/after deltas | LCOM values required manual cross-checking — column mapping diverged from the project's chosen CK variant and had to be corrected |
| 2026-05-26 | Mark Mannion | Claude Code / Sonnet 4.6 | Guided the incremental migration of `FeatureSetManager` callers across 8 files to `FeatureVisualiser`/`FeatureCatalog`/`FeatureSetService`; helped resolve compilation errors as static fields were relocated | Minor namespace suggestions didn't match project conventions and were adjusted manually |
| 2026-05-27 | Mark Mannion | Claude Code / Sonnet 4.6 | Scaffolded the VOTable refactoring example (`example-2-votable-export`) including before/after excerpts, interfaces, and PlantUML diagram | |
| 2026-05-27 | Mark Mannion | Claude Code / Sonnet 4.6 | Assisted in updating refactoring examples to align with ADR 008 layering rules | |
| 2026-05-27 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Drafted the CBO before/after comparison table for the VOTable export refactoring; identified the reduction in couplings introduced by the new `IVOTableExporter` interface | RFC column absent from the Understand export — left as a placeholder and estimated manually |
| 2026-05-27 | Aaron Byrne | Claude Code / Sonnet 4.6 | Drafted `MomentMapRequest` and `MomentMapResult` | |
| 2026-05-28 | Mark Mannion | Claude Code / Sonnet 4.6 | Helped resolve remaining compile-time errors across the feature domain after the migration; ensured namespace and interface usage matched ADR 008 | |
| 2026-05-28 | Harry Kennedy | Claude Code / Sonnet 4.6 | Generated the `IFeature` interface and stub `Feature` implementation; assisted with scenario test updates | |
| 2026-05-28 | Harry Kennedy | Claude Code / Sonnet 4.6 | Drafted `IFeatureSystemPort` and `FeatureSystemPort` adapter for the team4 interface contract | |
| 2026-05-30 | Fergus O'Flynn | Claude Code / Sonnet 4.6 | Assisted in writing the CK Metrics summary section — explained WMC and CBO delta improvements in plain language aligned with the ADR 008 rationale | Initial prose overstated the DIT improvement; moderated after checking raw metric values |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | Drafted `SubTeam5_CK_Metrics.md`: mapped SciTools Understand CSV columns onto the CK suite (WMC/DIT/NOC/CBO/LCOM) and explained the before/after deltas for the Moment Maps and VOTable Export examples | Understand's export has no native RFC column, so RFC had to be estimated/flagged manually; mapping caveats needed human confirmation against the actual project |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | Drafted `SubTeam5_Testing_Strategy.md` documenting the three test levels (example-based unit, FsCheck property-based, scenario) for `SubTeam5_Tests`, the tooling table, and coverage gaps | Tooling versions and the passing test count had to be verified against `SubTeam5_Tests.csproj` and an actual test run |

---

---

# Sub-team 6 — Desktop GUI & Client Shell

§10.5 + §9.1 T8. One row per substantive AI assist. Required at team level on Day 14.

| Date | Author | Tool / Model | Prompt class | Where it helped | Where it failed | What the human did instead | Artefact ID |
|---|---|---|---|---|---|---|---|
| 2026-05-19 | Con | Claude Code (Opus 4.7) | Backlog drafting | Produced full product + sprint backlog mapped to LOs and pitch slots; cited spec sections | Initially confused Team 6 (numeric allocation) with §6.6 (work package); needed correction | Clarified team identity; corrected memory entries | backlog.md, CLAUDE.md |
| 2026-05-19 | Con | Claude Code (Opus 4.7) | backlog-drafting | As week-1 Team-Alpha SM, formatted empty stand-up presets for sprints 1–3 and drafted a cross-sub-team Kanban overview (per-team plus shared work) | Produced generic per-team rows without knowing each sub-team's real status; could not invent their actual progress | Filled in real status from each sub-team's SM after the day-2 sync | standups.md, team-alpha Kanban |
| 2026-05-19 | Mark | Gemini (image generation) | diagram-generation | Clearly displayed the CanvassDesktop structure as a visual diagram | Initial image was disorganised and hard to read | Wrote a more organised and detailed prompt to guide the layout | CanvassDesktop diagram |
| 2026-05-19 | Rory | Claude (Opus 4.6, chat) | tool-setup | Walked through SonarQube Cloud project creation, sonar-scanner CLI installation on Windows, environment variable configuration (SONAR_TOKEN), and scanner command construction | Initially guessed organisation key as "rory-harrington" when it was actually "roryh06"; also guessed project key as "iDavie-RH" when it was "RoryH06_iDavie-RH"; took three attempts to get the scan command right | Manually retrieved correct keys from SonarQube Project Information page; ran the scanner successfully on second correct attempt | SonarQube Cloud project (sonarcloud.io) |
| 2026-05-19 | Rory | Claude Code | metric-interpretation | Computed WMC, DIT, NOC, CBO, RFC, LCOM (Henderson-Sellers) for all 8 Desktop GUI classes; identified 12 dead fields across 5 classes; discovered the copy-paste bug at line 306 of DesktopPaintController (UpdateMaxValue assigns to minVal) | RFC counts were approximate (~118, ~99) — estimated external call counts rather than tracing every call site exhaustively | Cross-referenced RFC estimates against method call counts in source; accepted approximations with ± note in the report | BNCH-1: docs/sub-team-6/deliverables/other/T2-baseline-benchmark/ck-metrics.md |
| 2026-05-20 | Jimmy | Perplexity (Comet) | diagram-generation | Generated complete PlantUML AFTER sequence diagram for Debug tab log-line flow using team interface contracts (ILogStream, ILogObserver, IDebugTabViewModel) | Initial prompt was too vague; first attempt omitted ServiceGateway as log producer and did not reference team NFRs | Reviewed diagram against architecture.md; confirmed interface names and added NFR-MOD-2 and T4 notes manually | WE2-3: after-debug-sequence-diagram.puml |
| 2026-05-20 | Jimmy | Perplexity (Comet) | code-skeleton | Scaffolded full C# skeleton (ILogStream, ILogObserver, IDebugTabViewModel, LogStream, DebugTabViewModel, .csproj) targeting net8.0 with zero UnityEngine dependencies; all files committed to correct DoD path | Tool placed LogEntry record inside ILogStream.cs rather than its own file; also initially missed the EntriesChanged event on IDebugTabViewModel | Manually checked each file compiled cleanly using dotnet build locally; restructured LogEntry placement and confirmed event signature | WE2-4: refactoring-examples/sub-team-6/debug-tab/skeleton/ |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | review | Read §1–10 + Appendices A–C of the brief and produced a comprehensive sub-team-6 deliverables checklist mapping every artefact to spec section, LO and SWEBOK; surfaced underweighted items | First pass missed several spec items (state contract D13, trade-off analysis D14, concern-map.png as §10.4 violation, role-rotation matrix as evidentiary); also cited a stale ai-tool-log path | Asked Claude to re-read the brief and explicitly flagged peer-assessment + Kanban snapshots; corrected file pointer | docs/sub-team-6/deliverables-checklist.md |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | review | Explained terse backlog tickets (DEPS-1 gateway dep, DEPS-2 VR-menu coord, ARCH-1 MVVM ADR) into concrete action lists and drafted the command-list / arg-shape questions to put to Sub-team 4 | Invented example command verbs (LoadCube, CreateSelection…) that were guesses, not confirmed Sub-team 4 names | Flagged them as examples and raised the real questions with Sub-team 4 directly | backlog.md tickets, Sub-team 4 query |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | prose-editing | Converted baseline metric PDFs to markdown, reorganised deliverables into per-section folders, and staged/committed the result in batches | — (mechanical conversion + git) | Reviewed each diff before pushing; held the main-branch push until batches were verified | deliverables/ folder reorg, *.md conversions |
| 2026-05-20 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Researched how to build the File-tab dependency graph and before-state sequence diagram, then ran the "faithful trace" of the current Open→load→cube path | First trace used placeholder GUI labels rather than the real scene button names | Con walked the live GUI to capture real button labels/scene paths, then had Claude dress them up | ex1-file_tab before-trace |
| 2026-05-20 | Mark | Claude Code (Sonnet 4.6) | ADR-drafting | Drafted full ADR markdown for ADR-0001 (MVVM split) and ADR-0002 (JSON-RPC/gRPC transport), including context, decision rationale, consequences, and compliance tables mapped to §7.1 CK thresholds and §4.2 non-negotiables | Did not include real baseline CK metric values — used placeholder figures | Fetched actual CK measurements from the Day 2 baseline PDF and inserted correct values into the compliance tables | docs/sub-team-6/adrs/0001-mvvm-split.md, docs/sub-team-6/adrs/0002-transport.md |
| 2026-05-20 | Rory | Claude (Opus 4.6, chat) | metric-interpretation | Produced per-file cyclomatic complexity, cognitive complexity, code smells, duplication %, technical debt estimate, and maintainability rating (A–E) for all 8 Desktop GUI classes; generated a top-10 code smells list ranked by severity including the systematic axis duplication pattern across 8 methods in DesktopPaintController (~240 lines) | Cognitive complexity was estimated to SonarQube-equivalent rules, not computed to exact SonarQube definition; duplication percentages were approximate | Used Claude output as the analytical baseline while continuing to attempt SonarQube Cloud file-level filtering for official tool output | BNCH-2: docs/sub-team-6/deliverables/other/T2-baseline-benchmark/BNCH-1.md |
| 2026-05-20 | Rory | Claude (Opus 4.6, chat) | tool-setup | Guided CodeScene account creation, project connection to GitHub repo (david-bol12/iDaVIE---Team-Alpha), and navigation to the hotspot view | N/A — setup was straightforward once the account was created | Took the hotspot screenshot, committed to repo as BNCH-3 evidence | BNCH-3: docs/sub-team-6/deliverables/other/T2-baseline-benchmark/BNCH-3.pdf |
| 2026-05-21 | Jimmy | Perplexity (Sonar) | prose-editing | Asked to rewrite the Die Boks sub-team brief for clarity and structure | Initial prompt too vague ("write this better"); tool asked for clarification on direction | Specified "restructure and reformat"; reviewed output against original spec requirements | docs/sub-team-6/brief.md |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | review | Audited existing docs vs the brief to find which design docs already existed (MVVM split, JSON-RPC/gRPC transport, SRP on CanvassDesktop, composable panels, VM unit tests, UI-Toolkit page-object) and what was missing | Reported some concerns as "covered" when only partially addressed | Con cross-checked each claim against the actual files before acting | docs/sub-team-6 gap audit |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | requirements-drafting | Drafted the "direct file I/O that belongs server-side" section of the current-GUI-behaviour doc and reformatted the Long-Term Python Console requirement to markdown | — | Reviewed section against the §6.6 split before committing | current-desktop-gui-behaviour/CanvassDesktop.md, D1-requirements/Long-Term-Python-Console.md |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | test-generation | Drafted the UI-Toolkit page-object-pattern testing doc (D5) working outward from the existing deliverables | First scoped too broadly off the backlog; needed reining in to just the one file | Con directed it to start from D5 docs only and stick to the brief | D5-testing/ui-toolkit.md |
| 2026-05-21 | Con | Claude Code (Opus 4.7) | requirements-drafting | Drilled the requirements down toward the 1–2 page deliverable; advised retiring the orphan requirement table | — | Con made the retire/rewrite calls; chose not to add a cross-link | D1-requirements |
| 2026-05-21 | Rory | Claude (Opus 4.6, chat) | metric-interpretation | Analysed dependency relationships between all 8 Desktop GUI classes; built a Dependency Structure Matrix (DSM); calculated fan-in, fan-out, and propagation cost per class; identified 3 dependency cycles (CD↔TM, CD↔HH, HH↔HMC) with specific code evidence (SerializeField references, method calls) | Propagation cost for PaintMenuController needed verification of transitive dependency chains; DSM was produced from static source analysis, not from NDepend/DV8 as mandated | Attempted NDepend setup in parallel (unsuccessful — Assembly-CSharp.dll does not compile outside Unity); accepted Claude-generated DSM as analytical deliverable with a note that NDepend/DV8 confirmation was outstanding | BNCH-4: docs/sub-team-6/deliverables/other/T2-baseline-benchmark/BNCH-4.md |
| 2026-05-21 | Rory | Claude (Opus 4.6, chat) | tool-setup | Guided NDepend installation, Visual Studio workload setup (.NET desktop development + Game development with Unity), .sln file generation via Unity, and NDepend project configuration | Did not know that Unity projects don't generate Assembly-CSharp.dll when opened in Visual Studio without Unity's internal compiler pipeline; confidently suggested multiple approaches (Build Solution, Regenerate project files, Open C# Project from Unity) that all failed; consumed ~2 hours | Abandoned NDepend approach after confirming DLL does not exist in Library/ScriptAssemblies/; used static source analysis via Claude Code instead; logged the tooling limitation as a Sprint 1 retro item | N/A — NDepend setup unsuccessful |
| 2026-05-22 | Jimmy | Perplexity (Sonar) | prose-editing | Folded Technical Work section into appropriate deliverable sections; produced a cleaner single-document structure | Kept Technical Work as a separate section initially rather than integrating it | Prompted explicitly: "incorporate the technical work into the appropriate deliverables" | docs/sub-team-6/brief.md |
| 2026-05-22 | Con | Claude Code (Opus 4.7) | prose-editing | Drafted the week-1 sprint review and a short sub-team retro from all the deliverables created that week | First review was thin; needed a re-read of every deliverable file | Con had it re-scan the deliverables folder and rewrite the review | Sprint-Documents/week1-sprint-review.md, week1-sprint-retro.md |
| 2026-05-22 | Con | Claude Code (Opus 4.7) | prose-editing | Drafted the pitch spine: 40-min structure, ISO/IEC 25010 framing slide, and the "CanvassDesktop is a textbook God Class" slide; plus an anticipated Q&A appendix | — | Con reviewed claims (e.g. 1899-line count, 25010 sub-characteristics) against source before keeping them | T5-pitch/pitch-spine.md, qa-practice.md |
| 2026-05-23 | Con | Claude Code (Opus 4.7) | review | Planned the opening 6-min section of the deck (pain points + intro) and advised adding a second slide on mandatory standards (ISO) alongside the architectural constraints | — | Con decided the extra mandatory-standards slide was worth the time budget | T5-pitch deck structure |
| 2026-05-25 | Jimmy | Perplexity (Sonar) | review | Identified 6 missing items from the brief (CK metrics, AI tool log, Kanban dependencies, role rotation, diagram format rule, component/sequence diagram placement) and produced a gap analysis with a prioritised "what to add" list | Did not cross-reference all spec sections on first pass; some partially-covered items needed manual clarification | Reviewed gap analysis against the spec; confirmed which items were genuinely missing vs. already implicit | docs/sub-team-6/brief.md |
| 2026-05-25 | Mark | Claude Code (Sonnet 4.6) | code-skeleton | Fleshed out debug-tab skeleton files (before-metrics.md and supporting artefacts) from the existing scaffold; produced content matching the DoD path structure | File updates did not apply correctly on first run due to a tooling/sync issue | Re-ran the generation step; output applied cleanly on second attempt | refactoring-examples/sub-team-6/debug-tab/ |
| 2026-05-25 | Mark | Claude Code (Sonnet 4.6) | code-skeleton | Wrote `debug-tab/before-metrics.md` with per-method complexity and coupling figures for `DebugLogging.cs` and `CanvassDesktop.cs` Debug-tab methods; corrected rough WMC/CBO/RFC numbers already present in `before-trace.md` | Initially proposed placing the file in `file-tab/` rather than `debug-tab/`; needed to read `DebugLogging.cs` directly to confirm method counts before writing | Confirmed the correct location; verified the corrected metric figures against the source file before committing | refactoring-examples/sub-team-6/debug-tab/before-metrics.md |
| 2026-05-25 | Mark | Claude Code (Sonnet 4.6) | diagram-generation | Audited committed refactoring-examples against the code after Gaps 1/2/3 were closed (IMemoryProbe, IFitsHandle, RatioMode, CubeLoadedEventArgs, MemoryProbeAdapter, ZScale); updated `after-class-diagram.puml` and `after-dependency-graph.puml` with the new interfaces and adapters; corrected stale test counts and interface counts in `dependency-graph.md`; fixed `AutoScrollEnabled` reference and WMC count in debug-tab `before-metrics.md` | Did not spot that `dependency-graph.md` also had stale content until prompted — initially only targeted the PlantUML diagrams | Pointed out the diagrams had drifted from code after gaps were closed; verified all PlantUML files render cleanly in VS Code before committing | refactoring-examples/sub-team-6/file-tab/after-class-diagram.puml, after-dependency-graph.puml, dependency-graph.md, debug-tab/before-metrics.md |
| 2026-05-25 | Con | Claude Code (Opus 4.7) | review | Built the File-tab scope inventory (line-range by responsibility group) and authored a self-contained verification prompt to re-check it in a fresh, un-primed Claude session | Claimed "100% of File-tab logic lives in CanvassDesktop.cs" when a teammate had said 95% with fan-out | Con challenged the figure; had Claude re-measure and reconcile the claim | D4-worked-examples/ex1-file-tab/.../file-tab-scope.md |
| 2026-05-25 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Produced the File-tab "after" refactor (skeleton + adapters), class diagram and CK metrics; explained skeleton vs adapters vs interface-contracts split | Needed several passes before the skeleton/adapters/contract distinction was clear | Con installed the dotnet SDK and built/ran the skeleton to verify it compiled before committing | refactoring-examples/sub-team-6/file-tab/{skeleton,adapters}, class-diagram.md, ck-metrics.md |
| 2026-05-25 | Con | Claude Code (Opus 4.7) | diagram-generation | Iterated the File- and Debug-tab Mermaid sequence diagrams to fix parse errors | Repeated parse failures from HTML entities and `<br/>` inside message labels | Con fed each parser error back and rendered the diagrams in VS Code to confirm they parsed | file-tab/debug-tab sequence diagrams |
| 2026-05-25 | Rory | Claude (Opus 4.6, chat) | diagram-generation | Generated PlantUML source for Component Diagram (UML) showing MVVM layers, Service Gateway, Anti-Corruption Layer, and external dependencies (Sub-team 1 server, Sub-team 4 interaction); also generated SysML BDD showing block hierarchy, operations, values, and architectural constraints | Initial diagrams were generic MVVM templates; required iteration to include iDaVIE-specific details (IServiceGateway methods, actual adapter names, Sub-team 4 dependency) | Reviewed diagrams against architecture.md and assignment spec; rendered via plantuml.com; committed both .puml source and PNG images to repo | Desktop_GUI_Component_Diagram.puml, Desktop_GUI_SysML_BDD.puml |
| 2026-05-26 | Jimmy | Perplexity (Sonar) | review | Incorporated full gap analysis + "what else is required" section into the brief; produced final complete document with all 9 sub-team deliverables, individual deliverables, team-level deliverables, and architectural constraints | — | Reviewed final output against spec checklist before committing | docs/sub-team-6/brief.md |
| 2026-05-26 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Mirrored the Debug-tab worked example off the File-tab design; wrote TL;DRs for the trace docs and walked through before/after trace+sequence (DTO, smells-eliminated table, anomaly #8 subscription leak) | Some trace rows had to be re-checked against the real source lines | Con read each before/after trace and sequence and confirmed claims against CanvassDesktop.cs line numbers | refactoring-examples/sub-team-6/debug-tab/, file-tab traces |
| 2026-05-26 | Con | Claude Code (Opus 4.7) | metric-interpretation | Ran before/after CK metrics for the File tab and mapped class-diagram + dependency-graph artefacts to the brief requirements they satisfy | — | Con confirmed which §-requirements each artefact actually closes | file-tab/ck-metrics.md, dependency-graph.md, class-diagram.md |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | test-generation | Wrote the full 12-section `test-strategy.md` (~4 pages) covering unit testing, integration testing, coverage targets, tooling stack (NUnit/Moq/Coverlet), and user-flow tests; read existing `viewmodel-unit-tests.md` and requirements doc before writing to avoid duplication; also added `*.ndproj` and `NDependOut/` to `.gitignore` | Existing stub and `viewmodel-unit-tests.md` had to be integrated rather than starting fresh; NDepend gitignore gap was not spotted until prompted by the user | Prompted the gitignore fix; reviewed all 12 sections against the brief requirements before committing | docs/sub-team-6/deliverables/D5-testing/test-strategy.md, .gitignore |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Guided the full NDepend measurement workflow (which GUI panels to use, Ca/Ce/I/A/D instability and CK queries); compared hand-derived vs. real NDepend figures for `CanvassDesktop` and produced 11 targeted edits replacing estimated values (WMC, DIT, LOC, instability metrics) with live tool output; recommended renaming the file | Initially tried to edit `EpicProj.ndproj` directly to fix DLL paths — NDepend overwrote the edits on the next analysis run; had to pivot to GUI-based configuration | Ran NDepend GUI, pasted the raw "Class Info" panel output into chat; verified each replaced metric against the live NDepend panel before accepting; made final rename decision | docs/sub-team-6/deliverables/other/D9-ck-baseline/ndepend-baseline.md (renamed from ndepend-equivalent-baseline.md), deliverables-checklist.md |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Hand-counted AFTER metrics for all 10 actual classes in the committed file-tab skeleton and adapters (replacing the 6-class projected table); computed WMC, CBO, RFC, LCOM per class and updated `metrics.md` §2.2 and §2.3 delta; status updated to "hand-counted from committed code, Day 6" | Initially only targeted the `ck-metrics.md` status line — `metrics.md` §2.2 was also stale with projected numbers and needed a full replacement | Confirmed the 10-class breakdown matched the actual files before accepting the commit | docs/sub-team-6/deliverables/D4-worked-examples/metrics.md, refactoring-examples/sub-team-6/file-tab/ck-metrics.md |
| 2026-05-26 | Mark | Claude Code (Sonnet 4.6) | prose-editing | Produced a full repo crossover/overlap audit (SK_BNCH metrics duplicated in two locations, stale adrs/ references, MVVM content split across three files); merged three MVVM sources (refactor.md, ADR-0001 context, transport fragments) into one canonical `mvvm-binding-policy.md`; repointed 6 stale cross-references across 3 files | Crossover report ranked adrs/ directory references as medium priority rather than errors; took a follow-up question to confirm `refactor.md` was safe to retire | Approved the retire decision after checking all references; confirmed all 6 link updates pointed at the correct sections | docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md, adrs/0001-mvvm-split.md, D4-worked-examples/metrics.md |
| 2026-05-27 | Jimmy | Perplexity (Sonar) | review | Checked GitHub repo and ClickUp board against the brief to identify any remaining gaps; produced a prioritised action list (ADR-0002, contracts/, architecture doc, C4 diagrams, CI/CD pipeline, stale README) | Could not access ClickUp (login required); GitHub content was client-rendered, required browser connection | Connected browser tool; manually logged in to ClickUp; verified gap list against backlog.md | docs/sub-team-6/deliverables-checklist.md |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | requirements-drafting | Consolidated D1 requirements to 1–2 pages with plain-English NFR explanations (ISP, Stable-Dependencies/instability, defect↔NFR linkage, B-06/B-08) | Over-linked the standalone requirements doc to T4/T7 | Con judged the doc should stand alone and had the T4/T7 links removed | D1-requirements/requirements.md |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | review | Investigated why FeatureTable.cs was changed in a commit the prior week (likely should not have been) | — | Con used the explanation to decide on follow-up with the owning team | FeatureTable.cs investigation |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | refactoring-proposal | Completed the Debug-tab worked example, ran a File-vs-Debug parity check, and removed the duplicate before-metrics.md (kept ck-metrics.md) | Self-flagged a missing Debug-tab ADR but had stubbed it as "not yet written" | Con removed the dangling ADR reference and directed the cleanup | refactoring-examples/sub-team-6/debug-tab/ |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | review | Converted ADR_Log_Improved.docx to markdown, audited ADR-009 against the D-deliverables, fixed the transport-contract-has-no-consumer gap, and tightened the architecture container boundaries | — | Con drove the step-by-step audit and approved each fix before it landed | team-alpha/ADR_Log_Improved.md, adr-009-audit, D2-Architecture/architecture.md |
| 2026-05-27 | Con | Claude Code (Opus 4.7) | test-generation | Audited and completed the D5 test strategy (via a scoped fresh-instance prompt) and wrote the ViewModel unit tests | Naming drift between D5 docs (iDaVIE.Client.* vs skeleton iDaVIE.Desktop.*) | Con had the namespaces aligned across D5 before accepting | D5-testing/test-strategy.md, viewmodel-unit-tests.md, ui-toolkit.md |
| 2026-05-27 | Mark | Claude Code (Sonnet 4.6) | refactoring-proposal | Gap-analysed D2 against §6.6 (only ADR-0002 transport existed; full 5–10 page architecture doc was missing); explained C4 levels; drafted the 438-line `architecture.md` including C4 Level 3 Mermaid component diagram, ISO 25010 drivers table, Day 2 CK baseline evidence table, proposed layer structure, MVVM after-state, NFR table, and GRASP/SOLID mapping; folded `client-server-transport.md` into `architecture.md` and updated cross-references across 3 files | Gap analysis flagged GRASP principles only implicitly — Mark had to explicitly request a dedicated GRASP section in a follow-up commit; initial C4 Mermaid syntax had minor parse issues | Added the GRASP section manually after prompt ("Grasp Principles Added to Architecture.md" commit); verified Mermaid diagram rendered correctly in VS Code before pushing | docs/sub-team-6/deliverables/D2-Architecture/architecture.md, D3-MVVM-binding-policy/mvvm-binding-policy.md, T5-pitch/pitch-spine.md |
| 2026-05-27 | Mark | Claude Code (Sonnet 4.6) | diagram-generation | Diagnosed and fixed broken PlantUML `implements` syntax in `architecture.md` §5.2; when the fix still failed to render, converted both diagrams (C4 Level 3 component diagram and SRP class diagram) from `plantuml` to `mermaid` fenced blocks; added Section 5.4 GRASP principles table mapping all 8 principles to concrete design decisions with §4.2 compliance note | First fix (moving `implements` to explicit arrows) did not resolve the render issue — had to pivot to Mermaid entirely | Prompted the pivot to Mermaid after the first fix still failed; explicitly requested the GRASP section; verified Mermaid diagrams rendered correctly in VS Code before committing | docs/sub-team-6/deliverables/D2-Architecture/architecture.md |
| 2026-05-27 | Mark | Claude Code (Sonnet 4.6) | review | Found 3 errors in D3 (dangling Supersedes path, two broken D4 links pointing at subdirectories that don't exist, duplicate Unity-leak smell rows); fixed all errors; added missing split tables for §3.3 (RenderingTabViewModel) and §3.4 (InformationTabViewModel) and the `UnityBinder<T>` code snippet; updated status to "final" | After fixing errors, file had been updated by another team member mid-session — caused a stale-state issue requiring a re-read before final edits | Confirmed status update and verified split tables were accurate before committing | docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md |
| 2026-05-27 | Rory | Claude (Opus 4.6, chat) | diagram-generation | Generated C4 Level 1 (System Context), C4 Level 2 (Container), and Service Gateway contract diagrams in PlantUML for pitch slides; each tailored to the appropriate abstraction level | N/A — diagrams rendered correctly on first attempt | Reviewed diagrams for accuracy against architecture.md; rendered via plantuml.com; inserted into Google Slides pitch deck | Pitch deck slides 19–22 |
| 2026-05-27 | Rory | Claude (Opus 4.6, chat) | prose-editing | Provided slide-by-slide content for pitch deck: title text, bullet points, and speaker notes for Pain Points, Architecture, Transport Contract, §4.2 Non-negotiables, ADRs, God Class Elimination, Trade-offs, and Summary sections; referenced the pitch-spine.md document from the repo | Some speaker notes were too long for a timed presentation; content was sometimes more detailed than a slide should contain | Edited content to fit slide format; trimmed speaker notes to key points; made design/layout decisions in Google Slides independently | Pitch deck (Google Slides) |
| 2026-05-28 | Jimmy | Perplexity (Sonar) | review | Compiled and formatted all sub-team 6 AI usage entries into the ai-log.md table | — | Verified dates and artefact IDs against actual commit history before committing | docs/sub-team-6/ai-log.md |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | diagram-generation | Generated the text-based concern map (PlantUML) mapping CanvassDesktop's 8 concerns to their SRP homes, each arrow labelled by the SOLID/GRASP principle, to retire binary `concern-map.png` per §10.4 | pitch-spine speaker note said "nine concerns" while architecture.md §5.1 lists eight — would have matched the diagram to the wrong count if unchecked | Verified every concern→home mapping and arrow-label principle against architecture.md §5.1/§5.2/§5.4; corrected the slide to "eight concerns" | docs/sub-team-6/deliverables/D2-Architecture/concern-map.puml |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | metric-interpretation | Explained LCOM4 vs the brief's normalised LCOM (0–1, threshold ≤0.5) and corrected the metric files that wrongly reported LCOM4 | Skeleton CK files initially used integer LCOM4, not the brief's LCOM | Con caught the mismatch and had every metric file switched to the brief's LCOM | D4-worked-examples/metrics.md, file-tab + debug-tab ck-metrics.md |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | review | Ran a read-only "crossroads" repo-wide audit mapping every §6.6 deliverable to the brief and surfacing gaps, then generated per-problem fix-and-commit prompts | Audit was told to close gaps #10/#12 but #12 is Day-13 CK re-measurement that has not happened yet | Claude refused to close unmet gaps and flagged them ("fix to reality"); Con accepted the correction | repo-wide gap audit, fix prompts |
| 2026-05-28 | Con | Claude Code (Opus 4.7) | review | Located the Unity 6 migration strategy and built a preparation plan + sample questions for the 40-min pitch and 20-min Q&A | — | Con used these to structure his own prep (defence remains human-authored per §10.5.6) | pitch prep notes |
| 2026-05-28 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Located all LCOM4 occurrences across 7 files and renamed labels LCOM4 → LCOM hs; then identified and corrected the scale mismatch (integer component-count → 0–1 Henderson-Sellers ratio: CanvassDesktop ≥7 → 0.97, AFTER classes 1 → ≈0.0); updated all values and threshold references | Initial pass was a purely textual label rename — did not correct numeric values or thresholds, leaving all files on the wrong scale; only surfaced the mismatch when explicitly asked | Noticed values were still on the LCOM4 scale after the rename and prompted to correct numbers and thresholds across all 7 files | refactoring-examples/sub-team-6/file-tab/ck-metrics.md, debug-tab/ck-metrics.md, and 5 other metric/trace files |
| 2026-05-29 | Con | Claude Code (Opus 4.8) | requirements-drafting | Scaffolded the Sub-team 1 (gateway) and Sub-team 4 (interaction) interface-contract files in separate folders with open questions to send to each team | Could not confirm Sub-team 4's real verb/DTO shapes — those must come from Team 4 directly | Con will send the open questions to Team 4 rather than have them guessed | refactoring-examples/sub-team-6/interaction-contract/ |
| 2026-05-29 | Con | Claude Code (Opus 4.8) | review | Located the AI logs and stand-up files, then back-filled this log's Con/Claude-Code rows by reading the project session transcripts (model confirmed per day from the transcripts) | Can only see Con's Claude Code sessions — Mark/Jimmy's Perplexity/Gemini use is out of scope and was left untouched | Con to review every back-filled row for accuracy before the artefact freeze | docs/sub-team-6/ai-log.md |
| 2026-05-29 | Rory | Claude (Opus 4.6, chat) | metric-interpretation | Ran static analysis on the original iDaVIE codebase (pre-refactoring); computed CK metrics for all 8 Desktop GUI classes directly from the source files using a Python analysis script; identified 13 CK violations across 6 of 8 classes | CBO counts included some false positives from regex-based type detection; required a second refined pass to improve accuracy | Reviewed the output against earlier Claude Code analysis for consistency; cross-checked key numbers (WMC, LCOM) against the Sprint 1 baseline | CK_Metrics_Static_Analysis_Original.md |
| 2026-05-29 | Mark | Claude Code (Sonnet 4.6) | metric-interpretation | Identified key deltas between hand-counted and Understand static analysis export values across all 4 metric files (CanvassDesktop WMC 57→63, CBO and RFC discrepancies); propagated tool-verified values into `metrics.md`, T2 `ck-metrics.md`, `file-tab/ck-metrics.md`, and `debug-tab/ck-metrics.md` with correct LCOM definition (Percent Lack of Cohesion 0–100%) | Mixed RFC definitions across files — traditional CK RFC vs. Understand's own RFC definition; needed explicit guidance to add the disambiguation caveat to the status header | Provided raw Understand export figures; verified each delta matched the Understand report before committing; added the "RFC column = tool def." note independently | docs/sub-team-6/deliverables/D4-worked-examples/metrics.md, other/T2-baseline-benchmark/ck-metrics.md, refactoring-examples/sub-team-6/file-tab/ck-metrics.md, debug-tab/ck-metrics.md |
| 2026-05-29 | Mark | Claude Code (Sonnet 4.6) | review | Produced a complete sub-team 6 + team-level gap analysis against the brief (Day 10 / Sprint 2 close); identified remaining gaps and produced a plain-language Sprint 3 to-do list with priority tiers; confirmed ADR-0004 (Unity 6 / UI Toolkit migration) was already covered in `architecture.md` and no duplicate write-up was needed | Initial gap list mixed high-priority blockers with already-covered items; needed follow-up questions to distinguish genuine gaps from false positives | Asked follow-up questions to separate confirmed gaps from false positives; verified Unity 6 coverage against architecture.md §4 before accepting the "no action needed" verdict | Sprint 3 to-do list (conversation output used to guide work allocation) |
| 2026-05-30 | Con | Claude Code (Opus 4.8) | review | Checked D1 `requirements.md` against brief §6.6/§9.2.1/LO2 — confirmed full coverage — then built a D1 explainer (what/why/how/decisions/exclusions + likely panel questions) | Asserted in the explainer header that it was "logged in ai-log.md" before any entry existed; also surfaced (did not fix) that `requirements.md` has no explicit elicitation-method or exclusions subsection | Con added this log row; left the decision on folding an exclusions subsection into the deliverable to a human | docs/sub-team-6/explainer-docs/D1-requirements-explainer.md |
| 2026-05-30 | Con | Claude Code (Opus 4.8) | review | Reviewed D3 MVVM binding policy vs brief + D2 and reconciled four contract/naming inconsistencies (ILogStream event→Observer, ILogService→ILogStream, StandaloneFileDialogAdapter→FileDialogAdapter, UnityMainThreadDispatcher→UnityUIDispatcher); added the "where is the Model" callout and the Update()≠MVU note | Initial framing assumed D2 was canonical; reading the compiled file-tab/debug-tab skeletons showed D2 itself was wrong on `IMemoryProbe` (missing from §6) and `LogEntryDto` (should be the `LogEntry` record) — blindly matching D2 would have regressed D3 against tested code | Con confirmed the committed skeleton code is the source of truth; corrected D2 (added `IMemoryProbe`, fixed `LogEntry`) and D3 accordingly rather than degrading D3 | D2-Architecture/architecture.md, D3-MVVM-binding-policy/mvvm-binding-policy.md |
| 2026-05-30 | Con | Claude Code (Opus 4.8) | review | Walked the full D3 doc section-by-section and captured the mental model (Model location, ICommand rule, ViewModel-is-a-class, Update()≠MVU, owns/does-NOT-own, worked-example round trip, defect→fix map, decisions, honest caveats) as a D3 explainer | — | Con reviewed each concept against the doc and skeleton code; live pitch/interview defence remains human-authored per §10.5.6 | docs/sub-team-6/explainer-docs/D3-mvvm-explainer.md |
| 2026-05-30 | Rory | Claude (Opus 4.6, chat) | review | Generated interview preparation material: likely panel questions per role (QC metrics defence, PO Liaison requirements tracing), structured answers for "how did you use LLMs" and "where did LLMs fail", and key technical concepts to explain cold (LCOM formula, propagation cost, MVVM rationale) | N/A — preparation material, not deliverable content | Used prompts as rehearsal framework; answers in the actual interview will be the student's own per §10.5.6 | Interview prep notes (personal) |
| 2026-05-31 | Con | Claude Code (Opus 4.8) | test-generation | Propagated the corrected `ILogStream` Observer contract into D5 (`test-strategy.md`, `viewmodel-unit-tests.md`: `OnLogEntry`/Moq `Raise` → `Subscribe`/`Publish`, `Entries`→`LogEntries`, object-init→`LogEntry` record ctor) and the `FileDialogAdapter` rename into D4 `metrics.md` | — (mechanical reconciliation flagged by the prior D3 review); deliberately left the week1/week2 sprint-review snapshots untouched as historical records | Con verified the rewritten test snippets match the committed `DebugTabTests.cs`/`LogStream` API before committing | D5-testing/test-strategy.md, D5-testing/viewmodel-unit-tests.md, D4-worked-examples/metrics.md |
| 2026-05-31 | Con | Claude Code (Opus 4.8) | prose-editing | Synthesised both AI logs into an AI-usage explainer (how/why/where-it-failed/decisions/boundaries/panel-questions) matching the D1/D3 explainer format, for cold defence of our AI usage at the panel | — (distilled from existing log rows; no new claims invented) | Con to review the explainer against the logs before the artefact freeze; live defence remains human-authored per §10.5.6 | docs/sub-team-6/explainer-docs/AI-usage-explainer.md |
| 2026-06-01 | Mark | Claude Code (Opus 4.8) | prose-editing + metric-interpretation | Swept all 5 debug-tab design docs to close the S7 (auto-scroll) gap: removed stale "S7 remaining" annotations, corrected the test count from 29 → 35 across docs, fixed Known-limitations item numbering, and corrected a pre-existing breakdown error in `ck-metrics.md` where individual class rows summed to 34 but the header claimed 29 | Did not independently spot the ck-metrics.md sum error until prompted; initial test-count update missed one doc | Mark identified the ck-metrics.md discrepancy and pointed at it; verified the updated count across all docs before committing | refactoring-examples/sub-team-6/debug-tab/after-sequence.md, after-trace.md, ck-metrics.md, class-diagram.md, dependency-graph.md |

**Prompt classes:** requirements-drafting · ADR-drafting · code-skeleton · test-generation · refactoring-proposal · diagram-generation · metric-interpretation · prose-editing · backlog-drafting · review · tool-setup

**Policy reminders:**
- AI output must be reviewed, understood, and defensible by a named human author.
- AI MAY NOT be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence (§10.5.6).
- Verbatim AI prose must not be passed off as human-authored in the final report (§10.5.3).

---

---

# Sub-team 7 — Native Plug-ins

*No log entries recorded.*
