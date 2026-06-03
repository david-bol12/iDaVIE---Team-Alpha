# Project: iDaVIE Refactoring Proposal — Sub-team 6 (Desktop GUI & Client Shell)

This repository is a working fork of [iDaVIE](https://github.com/idia-astro/iDaVIE) used for the **ISE EPIC: Refactoring the iDaVIE Codebase for Maintainability** assignment (Mon 18 May – Fri 5 June 2026, 15 working days).

> **Team identity (resolved 2026-05-19).** We are **Team Alpha**, allocated to the **Desktop GUI and Client Shell** work package (Section 6.6 of the spec). In Section 5.5's cohort allocation table that's **Sub-team 5 "Die Boks"**. We informally refer to our work package as "Team 6" because the brief we read is Section 6.6 — the spec overloads "Sub-team N" to mean both allocation ID and work package number. When external coordination needs a number, use **Alpha / Sub-team 5 / Die Boks**.

## Assignment one-liner

**Design-only** refactoring proposal — no upstream code is changed. Sub-teams demonstrate maintainability gains using **before/after worked examples**, UML, CK metrics, ADRs, and a testability strategy. The target style is **client–server + micro-kernel + layered + plug-in**, with an **anti-corruption layer** around Unity 6 APIs.

- **Cohort:** Team Beta (27 students, 7 sub-teams). Sub-team 6 = us, 4 people.
- **Final deliverable:** a 40-min pitch to the iDaVIE maintainer panel on **Thu 4 June 2026, 12:00–13:00**.
- **Artefact freeze:** Thu 4 June 11:00 (pitch start). Submission window Fri 5 June 14:00–16:00.

See `Assignment-Docs/iDaVIE_Refactoring_Assignment_FINAL_1.md` for the canonical spec.

## Sub-team 6 scope (Section 6.6)

**Behavioural element owned:** `CanvassDesktop`, file/mask loaders, parameter panels, debug consoles, and the client-side composition root that wires everything to the server.

**Target architecture for our slice:**
- **MVVM split:** View (Unity 6 UI Toolkit) → ViewModel (pure C#, Unity-free) → Service Gateway (talks to the server).
- **Transport contract:** JSON-RPC over named pipes (local mode); gRPC for future remote streaming.
- **SRP on `CanvassDesktop`:** menu structure, panel state, file dialogs and configuration must be separated. No more God-canvas.
- **Anti-corruption layer:** domain code in the client must not transitively depend on `UnityEngine` or `SteamVR` types.

**Mandatory worked refactoring examples (two):**
1. **File tab** — from direct native-plugin call → ViewModel command via service gateway.
2. **Debug tab** — as Observer of a structured logging stream.

**Sub-team deliverables (per Section 6.6 + Section 9.2):**
- Desktop client architecture document (5–10 pages).
- MVVM binding policy.
- Worked refactoring of File and Debug tabs (before/after UML, dependency graph, CK metric deltas).
- Sub-team requirements doc (1–2 pages).
- Sub-team test strategy (2–4 pages).
- End-of-sprint Kanban snapshots ×3.
- Daily stand-up notes (single shared file).

**Dependencies:**
- Sub-team 1 (Architecture/Micro-kernel) for the **service gateway** contract.
- Sub-team 4 (Interaction System) for **VR-side menus**.

## Codebase facts that matter for our scope

- **Unity:** 2021.3.45f2 (legacy `UnityEngine.UI` Canvas system; target is Unity 6 UI Toolkit).
- **Primary file under our care:** `Assets/Scripts/UI/CanvassDesktop.cs` — **1899 lines**, single `MonoBehaviour`. Code smells visible at a glance:
  - Long `transform.Find("A/B/C/...").GetComponent<...>()` chains hardwired to scene hierarchy (fragile, untestable).
  - Mixed concerns: file I/O, FITS axis logic, slider wiring, popup state, coroutine lifecycles, threshold sliders, subset bounds maths.
  - Heavy reliance on `FindObjectOfType<>` (singleton-style coupling).
  - Direct calls to native plug-ins / `VolumeCommandController`.
- **Other UI files in scope:** everything under `Assets/Scripts/UI/` and `Assets/Scripts/Menu/` (TabsManager, RenderingController, OptionController, etc.).
- **CI:** basic sanity checks live at `.github/workflows/ci.yml`. The Quality Guild will harden this team-wide — we plug into it, we do not own it.

## Mandatory metric tools (operational by Day 2, owned by Quality Guild)

SonarQube Cloud · Understand (CK suite) · NDepend · CodeScene · DV8. We must produce a **Day 2 baseline** and a **Day 13 projected snapshot** for our slice of the code.

**CK thresholds (Section 7.1):** WMC ≤ 20 (domain) / ≤ 40 (adapters) · DIT ≤ 4 · NOC ≤ 5 · CBO ≤ 14 (domain) / ≤ 25 (orchestrators) · RFC ≤ 50 · LCOM ≤ 0.5. Cycles forbidden.

**Coverage targets:** ≥ 70 % branch/line on domain code; ≥ 50 % overall. Unity-bound code tracked, not in the strict target.

## Architectural non-negotiables (Section 4.2)

1. No SOLID/GRASP violations without a documented trade-off.
2. **Zero circular dependencies** between top-level components.
3. **Domain code must not transitively depend on UnityEngine / SteamVR.**
4. Every public API boundary expressed as an interface with at least one test double.
5. Plug-in C ABI semver and ABI-stable within a major version.

## Working-style notes

- This is a **design proposal**, not a code change. Worked examples live in `/refactoring-examples/sub-team-6/` (create when needed). Do not refactor production scripts under `Assets/`.
- All diagrams must be **text-based and source-controlled** (PlantUML, Mermaid, .drawio XML). No binary-only diagrams.
- **AI usage is expected and logged.** Any AI-generated artefact must be defensible by a human author on the panel. Maintain a log of prompts + tool + where it helped + where it failed.
- AI may NOT be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence.

## Where to look

| Need | Path |
|---|---|
| Canonical assignment spec | `Assignment-Docs/iDaVIE-Refactoring_Assignment_FINAL_1.md` |
| Our primary refactor target | `Assets/Scripts/UI/CanvassDesktop.cs` |
| Other GUI/Menu scripts | `Assets/Scripts/UI/`, `Assets/Scripts/Menu/` |
| Build & troubleshooting | `BUILD.md`, `BUILD_TROUBLESHOOTING.md` |
| CI workflows | `.github/workflows/ci.yml` |
| Sub-team scratch notes & settings | `.claude/` |
