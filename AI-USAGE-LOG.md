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

## Chris Jo

_Add your AI usage log here. Use the same format as above — one entry per session, with prompt summary, output description, and files affected._

---

## Damien O Brien

_Add your AI usage log here. Use the same format as above — one entry per session, with prompt summary, output description, and files affected._
