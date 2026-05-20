# Sub-team 6 — Requirements (1–2 pp)

**Status:** stub — REQ-4 in the backlog. Draft by Day 4 (Thu 21 May 2026).

## 1. Scope

The Desktop GUI and Client Shell: `CanvassDesktop`, file/mask loaders, parameter panels, debug consoles, and the client-side composition root that wires everything to the (future) server.

## 2. Current behaviour catalogue (REQ-1)

| Tab | Current behaviour | Direct file I/O? | Notes |
|---|---|---|---|
| File | Provides file/mask loader UI: browse and load FITS/HDF5 volume files, select HDU, define data subsets, and apply image/mask overlays | Yes | Directly reads file paths; tightly coupled to native plugin loader |
| Render | Exposes volume rendering controls: colour map selection, transfer function thresholds, rest frequency dropdown, and histogram-based brightness adjustment | No | Drives rendering parameters on the active volume |
| Stats | Displays statistical summary of the loaded volume: min, max, mean, standard deviation, sigma clipping, and percentile values | No | Read-only panel; recalculates on volume load |
| Sources | Loads and displays catalogue source overlays: imports source mapping files, shows source table with sky coordinates, and toggles source visibility | Yes | Reads catalogue/mapping files from disk |
| Debug | Exposes runtime diagnostic information: active tool states, OnGUI popup toggles, internal state change logs, and performance counters | No | Development/diagnostic panel; not exposed in release builds |

## 3. Non-functional requirements traced to ISO/IEC 25010 maintainability (REQ-2)

| NFR ID | Sub-characteristic | Statement | Acceptance metric | Source |
|---|---|---|---|---|
| NFR-MOD-1 | Modularity | View, ViewModel and Service Gateway must be in separate assemblies / namespaces with no circular dependencies. | Cycle count = 0 (NDepend / DV8) | §4.2.2 |
| NFR-MOD-2 | Modularity | ViewModel layer must have zero transitive dependency on UnityEngine or SteamVR. | NDepend rule pass on PR | §4.2.3 |
| NFR-ANA-1 | Analysability | No class in our slice exceeds WMC 20 (domain) or 40 (adapter). | CK from Understand | §7.1 |
| NFR-MOD-3 | Modifiability | All public API boundaries expressed as interfaces with >= 1 test double. | Coverage report | §4.2.4 |
| NFR-TST-1 | Testability | ViewModel branch + line coverage >= 70 %. | SonarQube | §7.2 |
| NFR-TST-2 | Testability | Interface size <= 7 public members (ISP). | Audit table (BNCH-7) | §7.2 |

_(Expand with at least one NFR per sub-characteristic.)_

## 4. Roadmap drivers (REQ-3)

- **Python console (long-term):** PYTHON tab in desktop GUI, scripting access to session state.
- **Workspace / state saving (long-term):** durable session restore. State contract delivered to Sub-team 7 (Persistence) by Day 9 — see ARCH-11.

## 5. Out of scope for this sub-team

- The server-side service implementation (Sub-team 1).
- The VR-side menu system (Sub-team 4).
- Native plug-in internals (Sub-team 2).
- Volume rendering (Sub-team 3).
- Feature/source domain model (Sub-team 5).
- Persistence schema and lifecycle (Sub-team 7).
