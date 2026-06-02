# AI Usage Log — Sub-team 3: Rendering Engine (Cache Me If You Can)

> **Purpose:** Documents all AI-assisted work completed by each team member for transparency and academic integrity.
> Each member should add their own section below in the same format.

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
2. Verfied work done on "compute CK metrics" and "SOLID/GRASP annotation pass".
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

_Add your AI usage log here. Use the same format as above — one entry per session, with prompt summary, output description, and files affected._

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

### Session 9 — AI Usage Log Population (current session)
**Tool:** Claude Code
**Task:** Populate the AI usage log for Chris Jo from Claude session history.
**Prompt summary:** Asked Claude to read the existing AI usage log for format reference and fill in all sessions from Claude history.
**Output:** Sessions 1–10 written into `AI-USAGE-LOG.md` under Chris Jo's section.
**Files affected:** `AI-USAGE-LOG.md`.

---

## Damien O Brien

### Session 1 — Read & Annotate VolumeDataSetRenderer.cs
**Tool:** Claude (claude.ai, free tier)
**Date:** 21 May 2026
**Task:** Read and annotate the primary target class `VolumeDataSetRenderer.cs`.
**Prompt summary:** Asked Claude to read `VolumeDataSetRenderer.cs` in full and produce a structured annotation document.
**Output:** Annotation Word document covering: file overview (1,402 lines, ~40 methods, 60+ fields), full method responsibility map across 8 responsibility categories, SOLID violation summary (SRP: 9 responsibilities, OCP, DIP, ISP all violated), preliminary CK metrics (WMC ~60, CBO ~32, RFC ~130, LCOM ~0.90), proposed target class mappings, key inline annotations on 4 worst methods, and Unity 6 migration flags. 
**Files affected:** `VolumeDataSetRenderer annotation.docx` (new)

---

### Session 2 — Read VolumeRender Shaders & Colourbar
**Tool:** Claude (claude.ai, free tier)
**Date:** 22 May 2026
**Task:** Read and annotate `BasicVolume.shader`, `BasicVolume.cginc`, and `Colorbar.cs`.
**Prompt summary:** Asked Claude to read `VolumeRender.shader`. Then pasted the full `BasicVolume.shader` ShaderLab wrapper and asked for a plain-English breakdown and markdown file. Later pasted the full `Colorbar.cs` code and asked for the same treatment.
**Output:** Shader annotation covering: vertex shader ray setup, fragment shader ray march loop (foveated step count, temporal jitter, 8 duplicated loop bodies across 2 projection modes × 4 mask modes), transfer function pipeline, depth intersection, NaN guard. `BasicVolume.shader` annotation markdown produced. `Colorbar.cs` breakdown produced covering: colour strip sprite atlas system, tick label layout, per-frame polling issues, and 5 refactoring problems (GetHashCode() fragility, GetComponentInChildren at runtime, GC allocations from Destroy/Instantiate, Resources.LoadAll coupling, update polling vs event-driven).
**Files affected:** `BasicVolume.shader annotation.md` (new), `Colorbar annotation.md` (implied).

---

### Session 3 — Understand Mask/Ray Marching 
**Tool:** Claude (claude.ai, free tier)
**Date:** 23 May 2026
**Task:** Understand masks and ray marching conceptually.
**Prompt summary:** "what is a mask in relation to this codebase?", "what is ray marching?", and "can you describe it at the most simple level you can?" Then asked for a markdown file covering all discussion on this task.
**Output:** Explanation of ray marching (simple: "shining a torch through fog, pixel by pixel"). 

---

### Session 4 — Read All ColourMap Shader Files 
**Tool:** Claude (claude.ai, free tier)
**Date:** 23 May 2026
**Task:** Read all colour map shader files.
**Prompt summary:** Asked Claude to read all colour map shader files. Followed up asking whether individual colour map shader files exist.
**Output:** Confirmed no dedicated colour map shader files exist — the system is entirely texture-based. Colour map system spans four locations: `ColorMapEnum.cs` (80 maps as enum), `BasicVolume.cginc` (atlas UV sampling pipeline), `Colorbar.cs` (UI sprite display), and `VolumeDataSetRenderer.ShiftColorMap()` (switching logic in the wrong class). Key finding: all 80 maps are stacked as horizontal strips in one 2D texture atlas; switching costs a single integer uniform update. Fragility flagged: `GetHashCode()` used as shader index — inserting a new enum value in the middle silently breaks atlas alignment.
**Files affected:** `ColourMap annotation.md` (new).

---

### Session 5 — Catalogue Unity 5 / Built-in RP APIs 
**Tool:** Claude (claude.ai, free tier)
**Date:** 23 May 2026
**Task:** Catalogue all Unity 5 / Built-in RP API usage across the codebase.
**Prompt summary:** Asked Claude to catalogue Unity 5 / Built-in RP APIs across the codebase. Then asked "what is Built-in RP?" for a conceptual explanation.
**Output:** Full catalogue produced: 5 shader files all using `CGPROGRAM`/`UnityCG.cginc` (zero URP), 15+ legacy texture sampling calls (`tex3Dlod`, `tex2D`, `sampler3D`), 23 `FindObjectOfType` calls (compile error in Unity 6), 2 `OnRenderObject`/`OnRenderImage` callbacks (not called by URP), 3 `Graphics.DrawProceduralNow` calls (incompatible with URP render pass system). Conceptual explanation of Built-In RP vs URP/HDRP and the exact shader syntax changes required for migration.
**Files affected:** `Unity5 BuiltinRP API Catalogue.md` (new).

---

### Session 6 — Write Render-Frame Call Sequence 
**Tool:** Claude (claude.ai, free tier)
**Date:** 24 May 2026
**Task:** Document the full render-frame call sequence with line numbers.
**Prompt summary:** Asked Claude to write the render-frame call sequence. Then asked to add line number references after reviewing the first version.
**Output:** Full call sequence document covering 5 phases: one-time setup (`_startFunc()` coroutine), CPU `Update()` (25+ uniform calls in order), GPU vertex shader (ray setup), GPU fragment shader (full ray march loop with all 8 branches + transfer function pipeline), GPU mask point cloud (`OnRenderObject()` + `DrawProceduralNow`). Line numbers added for all key functions. Key finding: `Update()` pushes all 25+ uniforms every frame regardless of whether values changed — refactored `VolumeMaterialBinder.SyncShaderState()` with dirty flags reduces this to near-zero on a static frame.
**Files affected:** `RenderFrame CallSequence.md` (new).

---

### Session 7 — Identify Other Rendering-Adjacent Classes 
**Tool:** Claude (claude.ai, free tier)
**Date:** 24 May 2026
**Task:** Identify all rendering-adjacent classes across the codebase.
**Prompt summary:** Asked Claude to identify other rendering-adjacent classes.
**Output:** 12 rendering-adjacent classes across 4 tiers identified. Key findings: `VolumeDataSet.cs` at 1,920 lines does GPU texture work (`GenerateVolumeTexture`, `Graphics.CopyTexture`) inside a data model class; vignette fields copy-pasted between `VolumeDataSetRenderer` and `CatalogDataSetRenderer`; 3 classes use `DrawProceduralNow` (all break on URP migration); near-circular dependency between `FeatureSetRenderer` and `VolumeDataSetRenderer`.
**Files affected:** `RenderingAdjacentClasses.md` (new).

---

### Session 8 — List SOLID/GRASP Violations with Evidence 
**Tool:** Claude (claude.ai, free tier)
**Date:** 24 May 2026
**Task:** Produce a comprehensive SOLID/GRASP violation audit with line-number evidence.
**Prompt summary:** Asked Claude to list SOLID/GRASP violations with evidence. Asked follow-up "just briefly what are SOLID and GRASP?" and "hypothetically if I asked you to refactor the whole codebase, would you be able to do it?"
**Output:** Full violations document: SOLID — 9 violations (SRP: 9 responsibilities; OCP: MaskMode requires 4-file edit per new mode; ISP: 152 public members, zero interfaces; DIP: 4 violations including Config.Instance singleton, FindObjectOfType, AddComponent). GRASP — 6 violations with exact line evidence. Headline stats: CBO ~32 (target ≤14), LCOM ~0.90 (target ≤0.5), 152 public members with zero interface coverage. Hypothetical refactoring cost estimated at ~$7–10 via API (~$20 Pro plan would cover it).
**Files affected:** `SOLID GRASP Violations.md` (new).

---

### Session 9 — Sketch Four-Class Split (Kanban Task 14)
**Tool:** Claude (claude.ai, free tier)
**Date:** 24 May 2026
**Task:** Sketch the four-class split as rough design notes — final Sprint 1 task.
**Prompt summary:** Asked Claude to sketch the four-class split (rough notes).
**Output:** Design notes document covering all four target classes with exact fields and methods extracted from `VolumeDataSetRenderer.cs` with line numbers: `VolumeMaterialBinder` (25+ uniforms, IMaskMode strategy, URP anti-corruption layer), `VolumeTextureManager` (4GB budget enforcement, GPU slot management), `VolumeCameraDriver` (Transform wrapper, `VolumeCoordinateService` pure-C# helper), `FoveatedSamplingPolicy` (pure C#, zero Unity dependencies, fully testable). All 14 Kanban tasks completed.
**Files affected:** `FourClassSplit RoughNotes.md` (new).

### Session 10 — Design Document Questions (URP/HDRP & No-ops)
**Tool:** Claude (Cowork)
**Task:** Understand URP vs HDRP rendering pipelines and the concept of no-ops before working on the design document.
**Prompt summary:** Asked what the difference is between the URP and HDRP rendering pipelines, and what "no-ops" means in reference to them.
**Output:** Explanation of URP (broad compatibility, lower overhead, fewer features) vs HDRP (high-end visuals, more features, higher hardware requirements), and a definition of no-ops in the rendering context (operations that execute but have no effect on output).
**Files affected:** None (informational).

---

### Session 11 — Remaining Tasks Assessment & Sprint 2 Progress
**Tool:** Claude (Cowork)
**Task:** Review remaining sprint tasks and assess what was done vs. outstanding.
**Prompt summary:** Asked Claude to assess what sprint tasks were complete and what remained outstanding based on PROGRESS.md and the project files.
**Output:** Status summary of completed vs. remaining tasks, identifying unblocked work and items blocked on other sub-teams.
**Files affected:** None (informational/planning).

---

### Session 12 — Design Document Scope & Requirements Sections
**Tool:** Claude (Cowork)
**Task:** Write and revise §3 (Scope) and §4 (Requirements Recap) of the design document.
**Prompt summary:** Asked for the markdown code for §3 shortened significantly, and §4 without bullet points to condense presentation. Multiple iterations — first draft of §3 produced, then shortened; §4 written with CK metrics targets table and design standards, then condensed to remove bullet points and unnecessary information.
**Output:** §3 Scope (Sub-team 3 ownership, explicit out-of-scope items) and §4 Requirements Recap (invariants, functional requirements, CK metric targets, design standards) written in condensed prose.
**Files affected:** `docs/design-document.md` §3, §4.

---

### Session 13 — Design Document Condensed & Word Document Export
**Tool:** Claude (Cowork)
**Task:** Condense the full design document and export as a Word (.docx) file.
**Prompt summary:** Asked Claude to condense the design document (down from ~1,400 lines to ~290) keeping all tables, code interfaces, and brief requirements. Then asked for a Word document export of the condensed version.
**Output:** `docs/design-document.md` rewritten from 1,406 lines to 290. `iDaVIE-Rendering-Design-Document.docx` generated with blue headings, header/footer, and page numbers. `PROGRESS.md` updated with design document tasks marked complete.
**Files affected:** `docs/design-document.md`, `iDaVIE-Rendering-Design-Document.docx` (new), `PROGRESS.md`.

---

### Session 14 — Deliverables Review 
**Tool:** Claude (Cowork)
**Task:** Review sprint deliverables status 
**Prompt summary:** Pasted git output showing a rebase/detached HEAD situation and asked for help resolving it. Then asked "so how are we looking now in respect to the deliverables?" to get a status check. 
**Output:** Deliverables status summary (design doc, refactoring examples, diagrams, metrics worksheet — all confirmed in good shape). 

---

### Session 15 — Standup Notes
**Tool:** Claude (Cowork)
**Task:** Format standup notes for the full project period (Weeks 1–2).
**Prompt summary:** Asked Claude to format standup notes for the team across first two weeks, with rotating roles. 
**Output:** `standup-notes.md` with Weeks 1–2, all four team members, rotating roles (Cathal SM Week 1, Damien SM Week 2, Ciallian SM Week 3), and blank templates for Week 3.
**Files affected:** `docs/standup notes.md`, `standup/standup-log.md`.

---

### Session 16 — Golden-Image Regression Testing Explanation
**Tool:** Claude (Cowork)
**Task:** Understand what "golden-image regression suite across mask modes and colour maps" means in the context of the test strategy.
**Prompt summary:** Asked what the phrase "Golden-image regression suite across mask modes and colour maps" means in regard to testing.
**Output:** Explanation of golden-image regression testing: comparing current output against approved baseline images to catch unintended visual regressions, applied across all mask mode and colour map combinations.
**Files affected:** None (informational).

---

### Session 17 — Unity Test Framework Setup
**Tool:** Claude (Cowork)
**Task:** Set up Unity Test Framework with Edit Mode and Play Mode tests for the refactored code.
**Prompt summary:** Asked how to implement software testing including play-mode tests under Unity Test Framework for renderer behaviour and edit-mode tests for non-Unity parts. Asked Claude to check the tests folder inside refactoring-examples. Subsequently pasted partial Unity test file setup and asked Claude to write `EditModeTests.cs` and `PlayModeTests.cs` covering mask mode shader keywords, colour map enum sanity, and play-mode material apply behaviour.
**Output:** `EditModeTests.cs` (7 tests covering `ColorMapEnumTests` and `MaskModeShaderKeywordTests`) and `PlayModeTests.cs` (play-mode tests covering apply, disable, swap, and frame-persistence behaviour). Guidance on assembly definition setup and resolving naming conflicts (`iDaVIE.Tests.EditMode`, `iDaVIE.Tests.PlayMode`).
**Files affected:** `refactoring-examples/team3/tests/EditModeTests.cs`, `refactoring-examples/team3/tests/PlayModeTests.cs`.

---

### Session 18 — Git Workflow Support
**Tool:** Claude (Cowork)
**Task:** Git commands for pulling, branching, pushing, and resolving conflicts throughout the project.
**Prompt summary:** Multiple git-related questions across sessions: how to pull the latest branch version, how to see branches, how to discard unstaged changes, how to push to a branch, resolving a detached HEAD state during a rebase, and how to unstage/discard changes.
**Output:** Git commands provided for all scenarios. Resolved detached HEAD by running `git rebase --continue` then `git push origin HEAD:Team3-Docs-and-Examples`. Resolved push rejection by running `git pull origin Team3-Docs-and-Examples --rebase` before pushing.
**Files affected:** None (git operations only).

---

### Session 19 — Presentation Preparation
**Tool:** Claude (Cowork)
**Task:** Prepare the team for the Sprint 2 presentation/interview.
**Prompt summary:** Asked Claude to assess the project files and identify what was already done vs. still open for Sprint 3. Asked for guidance on where to start for the presentation.
**Output:** Status summary: design decisions, CK metrics deltas (WMC 97→12, LCOM 0.95→0.0, 46-file cycle broken), and identification of the four core design decisions (DD-01 to DD-04) as the likely focus. Recommended a team walkthrough of the design document.
**Files affected:** None (informational/planning).

---
