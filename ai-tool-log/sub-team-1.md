# Sub-team 1 — Architecture / Micro-kernel — AI tool usage log

See [`README.md`](README.md) for schema. Newest entries on top.

---
# AI Usage Log

A brief log of recent Claude conversations, grouped by date.

## David

## 4 June 2026

- **Integration overview document** — Reviewed all merged/open PRs and the two CI workflows to author `docs/integration/INTEGRATION.md`: per-sub-team contribution map, CI pipeline gates, current `main` CI failures and causes, cross-team contracts, and outstanding actions.
- **Git commit cleanup** — Committed and then soft-reset the integration document commit on request, preserving the changes.
- **Modifiability passage** — Drafting and tightening a quality-attribute passage on poor modifiability for the ASR document.
- **Branch coverage ≥70%** — What the metric means and how it's enforced on domain/application layer classes.
- **Merge conflicts + dependency cycles** — Whether renaming files avoids merge conflicts; what "0 dependency cycles" means and how interfaces break them.
- **Modularity passage + verifications** — Completing the modularity write-up and suggesting NDepend-based "Verified by" entries.

## 3 June 2026

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

## 2 June 2026

- **SWEBOK Ch.2 + ASRs** — TL;DR of the Software Architecture chapter and what ASRs should outline.
- **Stakeholder expectations** — Summarising the iDaVIE roadmap's stakeholder expectations and long-term aspirations.
- **GitHub PR merge access** — Diagnosing a "need write access" error on a fork-based PR.

## 31 May 2026

- **Linux device input** — Mouse side-button remapping (evdev vs X11, Wayland caveats).

## 25 May 2026

- **FITS render data flow** — Traced what data is passed to the renderer to draw FITS volumetric objects (VolumeData pipeline).

## 21 May 2026

- **God-class UML diagrams** — Identified each god class and produced PlantUML class diagrams of their interactions, plus refactoring suggestions.
- **Architecture layers explained** — Walkthrough of the domain / application / infrastructure / plugin-host layering.

## 20 May 2026

- **Project singletons** — Enumerated the singleton patterns across the codebase.
- **Kernel-class refactor advice** — Whether introducing a kernel class is the right way to untangle the intertwined class structure.

## 19 May 2026

- **VolumeData deep dive** — High-level description of each class in the VolumeData directory and the tight coupling between volume input and VR controls.
- **CI workflow authoring** — Drafted a `ci.yaml` for the repo; clarified Unity (student-license) version from ProjectVersion and that the check targets the native plugin vs the Unity app.

## 18 May 2026

- **CLAUDE.md + project analysis** — Generated the initial CLAUDE.md, reviewed modularity / microkernel feasibility, covered model switching, and drafted a monolithic-architecture README.
- **Spec + CLAUDE.md merge** — Combined the spec and CLAUDE.md documents.
- **C ABI plugin contract** — Purpose of the C ABI plugin contract and the P/Invoke boundary code related to it.
- **Class-architecture domain model** — Built a domain model showing the class architecture and interactions.
- **Dependency setup** — Downloaded the project's required dependencies.

---

The bulk centres on iDaVIE architecture/ASR work, with side threads on Git/GitHub mechanics, Linux input, and one off-topic request.
