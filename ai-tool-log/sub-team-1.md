# Sub-team 1 — Architecture / Micro-kernel — AI tool usage log

See [`README.md`](README.md) for schema. Newest entries on top.


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

## 2026-06-04 — Architecture layer classification across all refactoring examples

- **Author:** Sean Lynch
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/ARCHITECTURE_LAYERS.md`
- **Prompt summary:** Asked Claude to read all files in `refactoring-examples/` and produce a single Markdown document classifying each file as Domain, Application, Infrastructure, or Plug-in Host layer as defined in the assignment spec (Section 4.1).
- **Where it helped:** Rapidly scanned and categorised ~80 files across all sub-teams into the four-layer table; descriptions and placement matched what we verified manually for the sub-team-1 entries.
- **Where it failed / was wrong:** Nothing to discard from the sub-team-1 entries. Other sub-teams' entries were not independently verified by Sean — those sub-teams are responsible for checking their own rows.
- **Human reviewer:** Sean Lynch

---

## 2026-06-02 — Annotate refactored VolumeDataSetRenderer PlantUML with architectural justifications

- **Author:** Sean Lynch
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `docs/uml/refactored/VolumeDataSetRenderer_refactored.puml`
- **Prompt summary:** Asked Claude to annotate the existing refactored PlantUML with notes justifying each class and interface placement against the Architecture Overview document. When the diagram became too large to display, asked it to adjust the layout and then re-render the justifications as a separate rendered output.
- **Where it helped:** Added clear notes linking each component to a specific section of the Architecture Overview (e.g. Domain isolation, Dependency Inversion) without altering the diagram structure.
- **Where it failed / was wrong:** Annotations initially caused the diagram to overflow the render window — required a second prompt to fix the layout. The rendered justification output was a supplementary step rather than a clean first attempt.
- **Human reviewer:** Sean Lynch

---

## 2026-06-02 — Refactor VolumeDataSetRenderer to layered architecture

- **Author:** Sean Lynch
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/subteam-1/VolumeDataSetRenderer/after/` (multiple files)
- **Prompt summary:** Asked Claude to refactor the monolithic `VolumeDataSetRenderer.cs` god class (1,402 lines) into the layered architecture shown in `docs/uml/refactored/VolumeDataSetRenderer_refactored.puml`. Followed up asking it to justify every structural decision against the Architecture Overview in `docs/`.
- **Where it helped:** Produced the full split into value objects, domain interfaces, application services, and the top-level coordinator. The justification pass correctly cited SRP violations, CBO reduction, and Dependency Inversion principles.
- **Where it failed / was wrong:** The initial refactor misspelled the output directory (`refractoring-examples` in the prompt, corrected at the tool call level). Some method signatures needed manual adjustment to match the actual before-file signatures.
- **Human reviewer:** Sean Lynch

---

## 2026-05-25 — Refactor client-server UML diagrams to match assignment brief

- **Author:** Sean Lynch
- **Tool:** Claude Code (claude-opus-4-7)
- **Where used:** `docs/uml/refactored/client_server_component_refactored.puml`, `docs/uml/refactored/client_server_bdd_refactored.puml`
- **Prompt summary:** Asked Claude (upgraded to Opus 4.7 for this session) to read the existing client-server component and BDD diagrams and refactor them so the architecture precisely matches the requirements in `iDaVIE_Refactoring_Assignment_FINAL_1.pdf`. Followed up to fix a cut-off right-hand section in the component diagram, then requested the BDD refactor separately.
- **Where it helped:** Restructured both diagrams to show the micro-kernel / plug-in host split clearly and added the correct package boundaries expected by the spec. The BDD refactor accurately reflected block ownership across layers.
- **Where it failed / was wrong:** The component diagram was initially too wide for the renderer — the small box in the bottom-right was invisible until a layout adjustment was made. Required two correction prompts before the output was usable.
- **Human reviewer:** Sean Lynch

---

## 2026-05-24 — Client-server component and SysML BDD diagram (initial)

- **Author:** Sean Lynch
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `docs/uml/client_server_component.puml`, `docs/uml/client_server_bdd.puml`
- **Prompt summary:** Asked Claude to produce a top-level component diagram and a SysML Block Definition Diagram showing the client-server split for the iDaVIE architecture. Followed up asking it to render both to PNG.
- **Where it helped:** Generated both PlantUML files showing the Unity VR client, server kernel, plug-in host, and the named-pipe transport boundary. Served as the baseline for the refactored versions produced on 2026-05-25.
- **Where it failed / was wrong:** The PNG render step required PlantUML to be installed locally — Claude could not execute that directly and the step was completed manually.
- **Human reviewer:** Sean Lynch

---

## 2026-05-21 — God class identification and PlantUML diagram generation

- **Author:** Sean Lynch
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `docs/uml/VolumeDataSetRenderer.puml`, `docs/uml/DesktopPaintController.puml`, `docs/uml/CanvassDesktop.puml`, `docs/uml/VolumeInputController.puml`, `docs/uml/VolumeDataSet.puml`; refactored variants in `docs/uml/refactored/`
- **Prompt summary:** Asked Claude to locate god classes in the codebase and generate UML class diagrams for them. After an initial Mermaid attempt, asked it to restart from scratch using PlantUML. Finally asked it to produce refactored "target state" diagrams for each class conforming to ISO/IEC 25010:2023 quality characteristics.
- **Where it helped:** Identified five god classes and produced both current-state and refactored PlantUML diagrams for each; the refactored diagrams were used as the blueprint for the subsequent code refactoring work.
- **Where it failed / was wrong:** Initial diagrams used Mermaid syntax — discarded entirely and restarted when the format was wrong. The first PlantUML pass grouped methods by alphabetical order rather than by responsibility; required an explicit re-prompt to group by concern (SteamVR/Unity, rendering, masking, etc.).
- **Human reviewer:** Sean Lynch

## Sean

### 4 June 2026

- **AI log compilation** — Asked Claude Code to review the git commit history and file contents to reconstruct and write this AI usage log for all previous sessions.
- **CK metrics report** — Asked Claude Code (claude-sonnet-4-6) to perform a static analysis of all 104 C# source files and generate a comprehensive CK metrics table (WMC, DIT, NOC, CBO, RFC, LCOM) for 180+ classes, grouped by architectural layer. Output committed as `docs/team-alpha/Inital Metrics/METRICS.md`. Claude identified the six god-class candidates and produced the coupling analysis and cohesion rankings. Where it failed: LCOM values were given as qualitative ratings (Low/Moderate/High) rather than numeric Henderson-Sellers scores — would need a dedicated static analysis tool (NDepend/Understand) for precise values.
- **NDepend maintainability benchmark** — Ran NDepend analysis of the project; committed the output artefacts (`NDependOut/`) as the initial maintainability baseline.

### 3 June 2026

- **VolumeDataSetRenderer refactoring interfaces** — Asked Claude Code to design and generate the C# interface split for `VolumeDataSetRenderer` as a worked refactoring example. Produced six interfaces: `IVolumeRenderer`, `ICoordinateMapper`, `IMaskController`, `IRegionController`, `IRestFrequencyController`, `IVolumeDataExporter` — each with XML documentation, ISP member counts, and namespace placement. Files committed to `refactoring-examples/subteam-1/VolumeDataSetRenderer/after/Interfaces/`. Also produced updated PlantUML class diagram for the refactored design. Where it failed: the `ICoordinateMapper` interface retained `UnityEngine.Vector3` and `Quaternion` in its contract, which means it cannot be tested outside Unity — a Unity-free coordinate DTO would have been more testable.

### 2 June 2026

- **Refactored UML diagrams (before/after)** — Asked Claude Code to update the existing PlantUML class diagrams to show the refactored "after" state for all five god classes (CanvassDesktop, DesktopPaintController, VolumeDataSet, VolumeDataSetRenderer, VolumeInputController) and to generate refactored client-server component and BDD diagrams. PlantUML source and rendered PNGs committed to `docs/uml/refactored/`. Where it helped: generated correct before/after PlantUML source for all classes and the two architecture diagrams in one session. Where it failed: initial refactored `client_server_component_refactored.puml` contained some placeholder class names that were not in the codebase and required manual correction.

### 25 May 2026

- **Initial UML diagram generation** — Asked Claude Code to read the source files for the five main god classes and generate PlantUML class diagrams showing their methods, fields, and dependencies. Produced `.puml` + rendered `.png` for `CanvassDesktop`, `DesktopPaintController`, `VolumeDataSet`, `VolumeDataSetRenderer`, `VolumeInputController`, `client_server_bdd`, and `client_server_component`. Committed to `docs/uml/`. Where it helped: accurately mapped the public API and key internal fields for each class from source. Where it failed: dependency arrows between classes occasionally missed indirect couplings (e.g. via delegates) that required manual addition.

### 19 May 2026

- **Multi-platform build/download scripts** — Asked Claude Code to update the PowerShell dependency download and build scripts to work across Windows and other platforms, and to integrate with the Unity build pipeline. Output was the updated scripts committed in the two May 19 commits.

# iDaVIE — AI tool usage log

A brief log of recent Claude Code conversations, grouped by date. Newest entries on top.

---

# AI Usage Log

## Sean

## 4 June 2026

- **AI usage log** — Created this log by reconstructing session history from stored conversation records; initialised the persistent memory system under `.claude/projects/.../memory/`.

- **CK metrics report** — Full static analysis of the 104-file C# codebase; generated `METRICS.md` with per-class Chidamber & Kemerer metrics (WMC, DIT, NOC, CBO, RFC, LCOM), inheritance hierarchies, coupling rankings, cohesion analysis, and refactoring recommendations for the six high-LCOM god-object classes.

- **CodeScene web configuration** — Explained that CodeScene Cloud does not auto-read `.codescene/config.json`; component mappings must be done through the web UI's Architectural Component Editor using glob patterns. Clarified that the config file produced earlier is for the CodeScene CLI/self-hosted product, not the SaaS version.

- **CodeScene config optimisation** — Rewrote the initial verbose `ARCHITECTURE.md` into a compact CodeScene-compatible format and generated `.codescene/config.json` with four named components (Domain, Application, Infrastructure, Plugin-Host) and exclusion rules for Unity-generated paths (`.meta`, `ProjectSettings/`, `Library/`, `Temp/`, `Logs/`, `UIElementsSchema/`).

- **Architecture breakdown** — Explored the full repository and produced `ARCHITECTURE.md`: mapped all 101 C# scripts across 14 directories onto four architectural layers (Domain / Application / Infrastructure / Plugin-Host), documented native dependencies (CFITSIO, Starlink AST, cminpack, OpenMP), and stated the dependency rule (Domain never imports from Application or Infrastructure; Plugin-Host has no managed dependencies).