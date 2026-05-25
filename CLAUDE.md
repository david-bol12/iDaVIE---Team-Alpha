# CLAUDE.md

> Project context for Claude Code. Read this first on every session.

## 1. Who we are and what we own

We are **Sub-team 1** on the iDaVIE refactoring assignment (ISE Year 1, May–June 2026). Identify our specific team (Team Alpha = *Apaties I*, Team Beta = *Terminal Ses*) from the working directory / repo before assuming anything cross-team.

Our work package is the **Architecture and Micro-kernel Core**. We own:

- The **kernel boundary** between client and server.
- The **C ABI plug-in contract** (versioning, error model, threading, memory ownership).
- The **layer dependency policy** (Domain → Application → Infrastructure → Plug-in Host, strict downward only).
- The **integration spine** for the whole 28-person team — every other sub-team integrates through contracts we define.
- The **ADR log** (minimum 8 ADRs).
- The **architecture violation CI checks** that block PRs from merging.

We are not a single sub-team that "does architecture" in isolation — we are the integration custodians. Our Tech Lead is the stable **Integration Lead** for the whole team and chairs the Architecture Guild daily at 09:15 (08:55 on interview days).

## 2. Strict scope rules

This is a **design-only refactoring proposal**. Read this twice:

- **No upstream iDaVIE code is changed.** Ever.
- We demonstrate refactorings with **worked design-level examples**: before/after UML, dependency graphs, CK metrics deltas, code skeletons.
- All worked example code lives in `/refactoring-examples/subteam-1/`.
- If Claude Code suggests "let me just patch this file" on the actual iDaVIE source — stop it. That is not the task.

## 3. Target architecture (mandated, not invented)

Both competing teams target the same style. We compete on execution, not invention.

- **Client–server** at the system level. Unity 6 VR client; server hosts data, domain logic, plug-in execution.
- **Micro-kernel** for the server. Small stable kernel + versioned plug-in contract.
- **Layered** inside the kernel: Domain → Application → Infrastructure → Plug-in Host. Strict downward dependency. No exceptions without an ADR.
- **Anti-corruption layer** around Unity 6 APIs.

### Mandatory constraints (any violation is a fail signal)

1. No SOLID/GRASP violation without a documented trade-off ADR.
2. **Zero** circular dependencies between top-level components.
3. Domain code **must not** transitively depend on `UnityEngine` or SteamVR types. Check this in CI.
4. Every public API boundary between layers is an interface + has at least one test double.
5. Plug-in C ABI is **semver-versioned** and ABI-stable within a major version.

## 4. Our deliverables

| # | Deliverable | Size / form | Sprint |
|---|---|---|---|
| D1 | Architecture overview with C4 model | 10–15 pages PDF | Sprint 2 end |
| D2 | Plug-in ABI specification (versioned) | Spec doc + header samples | Sprint 2 end |
| D3 | ADR log | ≥ 8 ADRs, markdown, source-controlled | Continuous |
| D4 | Layer-violation CI check definitions | NDepend CQLinq / ArchUnit-style rules | Sprint 1 → hardened by Day 10 |
| D5 | Worked refactoring examples | 2 examples, before/after CK metrics | Sprint 2 |
| D6 | Sub-team requirements doc | 1–2 pages | Sprint 1 |
| D7 | Sub-team design doc | 5–10 pages | Sprint 2 |
| D8 | Sub-team test strategy | 2–4 pages | Sprint 2 |

**Required worked example:** annotated before/after UML for a current god-class (`VolumeDataSetRenderer` is the canonical candidate) **plus** a sequence diagram for "user loads FITS cube → requests subcube → exports mask".

## 5. ADR backlog (seed — refine as we go)

Minimum 8 ADRs. Working list:

1. Client–server transport (JSON-RPC over named pipes local; gRPC for remote).
2. Plug-in C ABI: symbol versioning scheme.
3. Plug-in error model (return codes vs. structured error objects).
4. Plug-in threading model (kernel-owned threads vs. plug-in-owned).
5. Memory ownership at the ABI boundary (who allocates, who frees, lifetime tokens).
6. Layer dependency enforcement mechanism (NDepend CQLinq + CI gate).
7. Anti-corruption layer pattern for Unity 6 API surface.
8. Plug-in registry: Service Locator at kernel boundary + constructor injection inside.

ADRs live in `/docs/adr/NNNN-title.md`. Format: Michael Nygard style (Context / Decision / Status / Consequences). Source-controlled. Versioned diagrams (PlantUML / Mermaid / `.drawio` XML) — **no binary-only diagrams**.

## 6. Interface contracts we owe other sub-teams

We are the integration point. By **Day 9 (Thu 28 May) end-of-day**, the Architecture Guild must sign off on the 6 state contracts delivered to Persistence (Sprint 2 exit criterion). Our contracts:

| Consumer sub-team | Contract we provide |
|---|---|
| Sub-team 2 (Data I/O) | Plug-in ABI for FITS / VOTable / WCS / statistics / downsampling |
| Sub-team 3 (Rendering) | Render-pipeline abstraction seam; texture data handoff contract |
| Sub-team 4 (Interaction) | `IInputProvider`, `IVoiceRecogniser` boundary placement in the layered model |
| Sub-team 5 (Feature System) | Feature aggregate location in Domain layer; use-case orchestration seam |
| Sub-team 6 (Desktop GUI) | Service Gateway contract; transport mechanism |
| Sub-team 7 (Persistence) | State contract format and versioning policy |

## 7. Metrics we must hit

Day 2 baseline + Day 13 projection for our allocated code. CK thresholds:

| Metric | Limit |
|---|---|
| WMC | ≤ 20 domain, ≤ 40 adapters |
| DIT | ≤ 4 |
| NOC | ≤ 5 |
| CBO | ≤ 14 domain, ≤ 25 orchestrators. **Cycles forbidden.** |
| RFC | ≤ 50 |
| LCOM (Henderson-Sellers) | ≤ 0.5 |

Layer-violation count and circular-dependency count in the projection: **must be 0**. ISP target on interfaces: **≤ 7 public members**.

Tools the Quality Guild operates (we consume the dashboard, we don't own the pipeline): SonarQube Cloud, Understand, NDepend, CodeScene, DV8. Our specific responsibility: the **NDepend CQLinq rules** that enforce the layer policy.

## 8. Schedule fixed points

- **Day 2 (Tue 19 May):** Baseline benchmark complete. CI skeleton green by EOD.
- **Day 3 (Wed 20 May):** First ADR drafts.
- **Day 9 (Thu 28 May):** Architecture Guild sign-off on 6 state contracts. Sprint 2 exit criterion. **Hard gate.**
- **Day 10 (Fri 29 May):** Architecture overview document (D1) due.
- **Day 12 (Tue 2 Jun) 17:00:** Artefact freeze for everything except packaging.
- **Thu 4 Jun 11:00:** Pitch start = full artefact freeze.
- **Fri 5 Jun 16:00:** Hard stop.

## 9. How Claude Code should help us (and not)

**Use Claude Code for:**

- Drafting ADRs from a context + decision sketch we provide.
- Generating PlantUML / Mermaid source for class, component, sequence diagrams.
- Writing C ABI header skeletons with versioning macros and ABI compatibility comments.
- Drafting NDepend CQLinq rules / ArchUnit-style fitness functions.
- Generating C# interface skeletons for kernel boundaries.
- Producing before/after UML for worked refactoring examples.
- Computing/explaining CK metric deltas given two class structures.
- Editing prose for the architecture overview and design doc.

**Do not use Claude Code for:**

- Modifying any file in the upstream iDaVIE codebase. We are design-only.
- Peer-rating, contribution log, individual reflection, or anything pitch/interview-defence related (assessment policy §10.5).
- Generating prose that goes verbatim into the final report without review (also §10.5).

**AI usage log:** every non-trivial Claude Code session gets a row in our AI tool usage log: tool, model, prompt class, where it helped, where it failed, what we did instead. This feeds the team-level T8 deliverable. Do not skip this — the panel can ask any of us to defend any decision.

**Defensibility rule:** During the pitch and interviews, the iDaVIE panel can ask any team member to explain any design decision or any piece of code in our report. **Inability to explain is a fail signal.** Anything Claude Code produces that goes into a deliverable must be understood by the student whose name is on it. If you don't understand a generated artefact, do not commit it.

## 10. Repo conventions

- `/docs/adr/` — ADRs, numbered `NNNN-kebab-title.md`.
- `/docs/architecture/` — C4 diagrams (PlantUML), component diagrams, SysML BDDs.
- `/docs/abi/` — plug-in ABI spec + sample headers.
- `/refactoring-examples/subteam-1/` — our two worked examples (before/ and after/ subfolders, plus `metrics.md`).
- `/ci/architecture-rules/` — NDepend CQLinq files and any ArchUnit-style rules.
- `/ai-log/` — per-session AI usage entries.

## 11. Daily rhythm

- **09:00** sub-team stand-up (08:55 on interview days).
- **09:15** Architecture Guild stand-up — our Tech Lead chairs as Integration Lead (08:55 on interview days).
- **10:30** Quality Guild huddle — our Quality Champion attends.
- **10:00–10:30, 12:00–13:00, 14:00–14:30** breaks (absorbed on interview days; lunch only).
- **Wednesdays 1h** Architecture Guild deep review.

---

## 12. iDaVIE codebase reference

iDaVIE is a Unity-based VR application for visualising 3D volumetric astronomical data (FITS files). It runs on **Windows only** (VR headset driver limitation) using **Unity 2021.3 LTS** and **SteamVR**. The codebase has two parts: C# Unity scripts and a C++ native plugin.

### Build Process

#### Native Plugin (C++)
The native plugin (`idavie_native.dll`) must be built before opening the project in Unity. It wraps CFITSIO (FITS I/O), Starlink AST (WCS/coordinate transforms), cminpack (optimisation), and OpenMP.

```powershell
# From repo root — builds native plugin and copies DLL to Assets/Plugins/
.\Configure.ps1 "C:\vcpkg" "C:\Program Files\Unity\2021.3.xf1\Editor\Unity.exe"
```

Prerequisites: Visual Studio with `Desktop development with C++` workload (MSVC v142, Windows SDK, CMake Tools), CMake, and vcpkg.

#### Unity Application
1. Open the project in Unity Hub (Unity 2021.3.x LTS).
2. Open `Assets/Scenes/ui.unity`.
3. Run **Window → SteamVR Input → Save and generate** (required once after cloning).
4. Build via **File → Build Settings → Build** (ensure OpenVR Loader is selected under XR Plug-in Management in Player Settings).

### Architecture

#### Native Plugin (`native_plugins_cmake/`)
C++ shared library built with CMake. Entry points are declared in `fits_reader.cpp`, `data_analysis_tool.cpp`, and `ast_tool.cpp`. The compiled DLL goes to `Assets/Plugins/`.

#### C# Plugin Interface (`Assets/Scripts/PluginInterface/`)
P/Invoke bindings to the native plugin using a custom attribute-based loader (`NativePluginLoader.cs`). `FitsReader.cs`, `DataAnalysis.cs`, and `AstTool.cs` declare delegates matching the C++ exports — the `[PluginAttr]` and `[PluginFunctionAttr]` attributes wire them up at runtime.

#### Core Volume Data (`Assets/Scripts/VolumeData/`)
- **`VolumeDataSet.cs`** — loads FITS files, manages raw voxel data, WCS metadata, subcube cropping, and mask I/O. Central data model.
- **`VolumeDataSetRenderer.cs`** — Unity MonoBehaviour that owns the 3D texture, drives the volume ray-marching shader, and manages rendering state (colour map, scaling, threshold, mask mode, projection mode).
- **`VolumeInputController.cs`** — handles SteamVR controller input using a Stateless state machine (`LocomotionState`, `InteractionState`). Detects VR hardware family (Oculus, Vive, WMR) to adjust button bindings.
- **`VolumeCommandController.cs`** — registers Windows speech recognition keywords and routes them to volume operations.
- **`Config.cs`** — JSON config (schema at `idavie.readthedocs.io`) controlling GPU memory limit, ray-marching steps, colour map defaults, foveated rendering, etc.

#### Feature/Source Data (`Assets/Scripts/FeatureData/`)
Manages source catalogues that can be overlaid on the volume. `FeatureSetManager` owns one or more `FeatureSetRenderer` instances; `FeatureTable` and `VoTable` handle VOTable file parsing.

#### Catalog Data (`Assets/Scripts/CatalogData/`)
Handles external point/line catalogue datasets distinct from feature sets. `CatalogDataSetRenderer` drives the `Catalog/CatalogPoint.shader` and `Catalog/CatalogLine.shader`.

#### Menus (`Assets/Scripts/Menu/`)
In-VR menus: `QuickMenuController`, `PaintMenuController`, `MomentMapMenuController`, `SpectralProfileMenuController`, `HistogramMenuController`, `VideoRecordMenuController`. All are MonoBehaviours attached to prefabs in the `ui.unity` scene.

#### Shaders (`Assets/Shaders/`)
- `Volumes/BasicVolume.shader` — ray-marching volume renderer (MIP and average projection modes).
- `Volumes/VolumeMask.shader` — mask overlay rendering.
- `Catalog/` — point and line shaders for catalogue overlays.
- `Features/BasicLine.shader` — feature bounding box / line rendering.

#### Scenes
- `ui.unity` — main scene containing the full application.
- `volumes.unity` / `catalogs.unity` — secondary scenes.
- `benchmark.unity` — performance benchmarking.

### Testing

There is no automated test suite. Testing is entirely manual using the [testing protocol checklist](https://forms.gle/ezLXLHeWR4ZeLmfz7). Always test a **compiled build**, not just the Unity Editor. Before merging, complete the full checklist and add test steps for any new feature.

### Merging and Scene Conflicts

Unity `.unity` scene files require a special merge tool. Configure `.git/config` to use `UnityYAMLMerge`:

```ini
[mergetool "UnityYamlMerge"]
    cmd = '<path\\to\\Unity>\\2021.3.47f1\\Editor\\Data\\Tools\\UnityYAMLMerge.exe' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
    trustExitCode = false
[merge]
    tool = UnityYamlMerge
```

Merging procedure: `git merge --no-commit <branch>`, resolve script conflicts first, then run `git mergetool` for scene file conflicts.

### Pull Requests

PRs must reference the issue they resolve, list changes clearly, include documentation in new code, and be based on a compiled (not just Editor) test. Use the PR template (`.github/pull_request_template.md`).

### License Header

All new source files must include the LGPL v3 license header. See any existing `.cs` or `.cpp` file for the exact format.

---

*If anything here conflicts with the assignment spec PDF, the PDF wins. Flag the conflict and we'll update this file.*
