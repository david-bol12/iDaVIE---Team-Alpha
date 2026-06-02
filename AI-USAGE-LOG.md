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
**Output:** Explanation of golden image testing and its scope. Drafted a methodology disclaimer for the metrics worksheet attributing figures to manual static analysis assisted by Claude, advising numbers be labelled as estimates.
**Files affected:** `docs/team3/metrics-worksheet.md` (methodology note).

---

### Session 5 — CK Metrics in NDepend
**Tool:** Claude (Cowork)
**Task:** Locating CK metrics within NDepend's interface.
**Prompt summary:** Asked where to find CK metrics in an NDepend report.
**Output:** Explained where each CK metric maps in NDepend (dashboard, Queries & Rules panel, CQLinq), and provided a sample CQLinq query to extract per-type metrics directly.
**Files affected:** None (informational).

---

### Session 6 — CK Metrics Python Script
**Tool:** Claude (Cowork)
**Task:** Automate CK metric computation from C# source files.
**Prompt summary:** Requested a script to compute CK metrics from the before/after C# files without a licensed tool.
**Output:** Produced `tools/ck_metrics.py` — a Python script supporting single-file mode, before/after comparison mode, and Markdown output. Documented known deltas vs Understand (WMC ~1.6×, CBO ~25% higher due to regex limitations on struct member names).
**Files affected:** `tools/ck_metrics.py` (new file).

---

### Session 7 — Metrics Consistency Pass (Correct Understand Values)
**Tool:** Claude (Cowork)
**Task:** Apply verified Understand metric values consistently across all documents.
**Prompt summary:** Correct CK values (from the Understand tool run) needed to replace earlier estimates across all docs and code files.
**Output:** Updated metrics-worksheet, design-document, both refactoring example READMEs, all five `after/*.cs` files, PROGRESS.md, CONTEXT.md, and two PlantUML diagrams. Documented three classes (VolumeRenderCoordinator, VolumeTextureManager, VolumeMaterialBinder) exceeding LCOM 0.5 target as a lifecycle-phase artefact, not a design flaw.
**Files affected:** `docs/team3/metrics-worksheet.md`, `docs/team3/design-document.md`, both example READMEs, all `after/*.cs` files, `PROGRESS.md`, `CONTEXT.md`, `diagrams/class-before.puml`, `diagrams/vdsr-dependencies.puml`.

---

### Session 8 — Sprint 2 Task Triage, Document Completion & Interview Prep
**Tool:** Claude (Cowork)
**Task:** Multiple tasks — ClickUp task status review, document sections written, merge conflict resolved, interview study guide created.

**Prompt summary (multiple prompts in one session):**
1. Pasted full ClickUp to-do list and asked which tasks were done vs remaining.
2. Asked why "compute CK metrics" and "SOLID/GRASP annotation pass" tasks didn't produce new files.
3. Asked AI to complete all remaining unblocked tasks.
4. Informed of 1-on-1 interview format and requested a comprehensive study guide.

**Output:**
- Task triage: 8 tasks confirmed complete, 4 identified as unblocked, 3 flagged as blocked on team action.
- Explanation of verification tasks: after/ code already contained full CK projection tables and SOLID/GRASP annotations inline; READMEs surfaced and summarised this evidence.
- `docs/team3/design-document.md` §6.2 and §6.3 written: Day 13 10-class projection table (all 6 CK metrics) and delta narrative (WMC 74→20, CBO cycle broken, LCOM 0.81→0 per class).
- `docs/team3/metrics-worksheet.md` populated: Day 13 projected column, before/worst/best delta table, justification prose for WMC, CBO, and LCOM.
- `PROGRESS.md` merge conflict (lines 64–112) resolved, sprint tasks marked done.
- `docs/INTERVIEW-STUDY-GUIDE.md` created: priority reading order, before/after class breakdown with justifications, SOLID/GRASP examples from actual code, CK metric plain-English explanations, nine interview Q&As with full spoken answers, and a numbers reference card.

**Files affected:** `docs/team3/design-document.md`, `docs/team3/metrics-worksheet.md`, `PROGRESS.md`, `docs/INTERVIEW-STUDY-GUIDE.md` (new), both example READMEs.

---

### Session 9 — Test Strategy to Word Document (current session)
**Tool:** Claude (Cowork)
**Task:** Convert test strategy markdown to a Word (.docx) file.
**Prompt summary:** Asked to convert the test strategy markdown into a separate Word document.
**Output:** Session interrupted before completion.
**Files affected:** TBC.

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

_Add your AI usage log here. Use the same format as above — one entry per session, with prompt summary, output description, and files affected._
