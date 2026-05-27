# iDaVIE Refactoring — Sub-team 3: Rendering Engine
## Team Alpha | Cache Me If You Can

---

## What This Project Is

A 15-day design-only refactoring proposal for the iDaVIE open-source VR codebase.
iDaVIE is a Unity-based VR application for visualising 3D astronomical data (FITS files).

**We do NOT change production code.** We produce a proposal document showing *how* the
code would be refactored, with worked before/after examples.

The final output is a pitch to the iDaVIE maintainer panel on Thu 4 June 2026.

- **Team:** Team Alpha (28 students, 7 sub-teams of 4)
- **Our sub-team:** Sub-team 3 — Rendering Engine ("Cache Me If You Can")
- **Duration:** Mon 18 May – Fri 5 June 2026
- **Codebase:** https://github.com/idia-astro/iDaVIE

---

## Our Sub-team's Scope

We own the **volume rendering layer**:
- Ray-marching shaders
- 3D texture management
- Foveated rendering
- Colour mapping
- Migration of all of the above to Unity 6 Scriptable Render Pipeline (URP/HDRP)

**Key class we are refactoring:** `VolumeDataSetRenderer` (currently a monolithic ~1400-line class)

---

## Deliverables We Must Produce

### Sub-team Deliverables (Section 9.2 of brief)
1. `docs/requirements.md` — Sub-team requirements document (1–2 pages)
2. `docs/design-document.md` — Sub-team design document (5–10 pages)
3. `refactoring-examples/` — Two worked refactoring examples with before/after CK metrics
4. `docs/test-strategy.md` — Sub-team test strategy (2–4 pages)
5. `kanban/` — Kanban snapshots at end of each sprint (Sprint 1, 2, 3)
6. `standup/standup-log.md` — Daily stand-up notes (single running file)

### Section 6.3 Specific Deliverables
- `docs/rendering-layer-design.md` — Rendering layer design document
- `docs/shader-asset-policy.md` — Shader/asset organisation policy for Unity 6
- `docs/metrics-worksheet.md` — Before/after CK metrics worksheet for the renderer

---

## Key Technical Requirements

### Current Invariants (must never be violated)
- 90 fps minimum frame rate
- 4 GB Unity texture limit
- 368 MB default cube memory budget
- Blocky (nearest-neighbour) texture filtering
- Foveated rendering support

### Future Requirements (design must not preclude these)
- Iso-contours / iso-surfaces
- Multi-cube and time-series data

---

## Target Architecture

### The Split We Must Design
Break `VolumeDataSetRenderer` into four focused classes:

| New Class | Responsibility |
|-----------|---------------|
| `VolumeMaterialBinder` | Shader/material property management |
| `VolumeTextureManager` | 3D texture upload, caching, eviction |
| `VolumeCameraDriver` | Camera matrix calculations, clip planes |
| `FoveatedSamplingPolicy` | Foveated rendering rate decisions |

### Render Pipeline Abstraction
- Create `IRenderPipeline` interface — our core must NOT import URP/HDRP types directly
- URP and HDRP are concrete implementations of this interface
- Enables edit-mode unit testing without a full Unity context

### Mask Mode Pattern
- Replace the switch statement on mask modes with Strategy pattern
- `IMaskMode` interface with three implementations: `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`
- Open-Closed Principle: new mask modes = new class, nothing existing changes

---

## CK Metrics We Must Measure and Report

| Metric | Meaning | Target |
|--------|---------|--------|
| WMC | Weighted Methods per Class | ≤ 20 (domain), ≤ 40 (adapters) |
| DIT | Depth of Inheritance Tree | ≤ 4 |
| NOC | Number of Children | ≤ 5 |
| CBO | Coupling Between Objects | ≤ 14 (domain), ≤ 25 (orchestrators) |
| RFC | Response For a Class | ≤ 50 |
| LCOM | Lack of Cohesion in Methods | ≤ 0.5 |

We need a **Day 2 baseline** snapshot and a **Day 13 projected** snapshot.

---

## Dependencies on Other Sub-teams

| Sub-team | What we need from them | What we give them |
|----------|----------------------|-------------------|
| Sub-team 2 (Data I/O) | Texture data format contract (`RawVolumeData` struct) | Nothing upstream |
| Sub-team 4 (Interaction) | `IGazeProvider` interface for foveated rendering | Camera state |

---

## Sprint Plan

| Sprint | Dates | Focus |
|--------|-------|-------|
| Sprint 1 | 18–22 May | Understand codebase, baseline CK metrics, requirements document | ✅ Complete |
| Sprint 2 | 25–29 May | Design document, both worked refactoring examples, diagrams, Section 6.3 docs, SOLID/GRASP audit | 🔄 **Current sprint** |
| Sprint 3 | 1–3 June | Finalise, address feedback, polish for pitch | ⏳ Upcoming |

**Artefact freeze:** Thu 4 June 11:00  
**Pitch:** Thu 4 June (Team Alpha 11:00–12:00)  
**Submission:** Fri 5 June 14:00–16:00

---

## Roles (rotate each sprint)

| Role            | Sprint 1 | Sprint 2 | Sprint 3 |
|-----------------|----------|----------|----------|
| Scrum Master    | Cathal   | Damien   | Ciallian |
| Tech Lead       | Damien   | Cathal   | Chris    |
| PO Liaison      | Ciallian | Chris    | Cathal   |
| Quality Champion| Chris    | Ciallian | Damien   |



---

## Tools We Use

- **SonarQube Cloud** — code smells, complexity, duplication
- **Understand** — CK metrics suite
- **NDepend** — architecture violation rules
- **CodeScene** — hotspots, churn
- **DV8** — Dependency Structure Matrix
- **PlantUML / Mermaid** — diagrams (source-controlled)
- **GitHub Actions** — CI/CD pipeline

All diagrams must be in PlantUML, Mermaid, or .drawio XML format — no binary-only files.

---

## Architectural Constraints (from brief Section 4.2)

1. No SOLID or GRASP violations — flag and refactor, or document as a trade-off
2. No circular dependencies between components
3. Domain rendering math must NOT transitively depend on `UnityEngine` or `SteamVR` types
4. Every public API boundary must be expressed as an interface with at least one test double
5. Plug-in contracts must be semantically versioned and ABI-stable within a major version

---

## File Structure

```
idavie-subteam3/
├── CLAUDE.md                    ← YOU ARE HERE — read this first every session
├── CONTEXT.md                   ← Detailed technical context about iDaVIE rendering
├── PROGRESS.md                  ← Running log of what's done / in progress / blocked
├── docs/
│   ├── requirements.md          ← Deliverable 1: requirements doc
│   ├── design-document.md       ← Deliverable 2: design doc (5–10 pages)
│   ├── test-strategy.md         ← Deliverable 4: test strategy
│   ├── rendering-layer-design.md
│   ├── shader-asset-policy.md
│   └── metrics-worksheet.md
├── refactoring-examples/
│   ├── example1-VolumeDataSetRenderer/
│   │   ├── README.md
│   │   ├── before/
│   │   └── after/
│   └── example2-MaskModes/
│       ├── README.md
│       ├── before/
│       └── after/
├── diagrams/
│   ├── architecture.puml
│   ├── class-before.puml
│   ├── class-after.puml
│   └── sequence-render-frame.puml
├── tests/
│   └── test-strategy-notes.md
├── kanban/
│   ├── sprint1-snapshot.md
│   ├── sprint2-snapshot.md
│   └── sprint3-snapshot.md
└── standup/
    └── standup-log.md
```

---

## Sprint 2 Kanban

Full task breakdown: `kanban/sprint2-greenfield.md` (57 tasks, ~55 person-hours)
ClickUp import file: `kanban/sprint2-clickup.csv` (import with DD/MM/YYYY date format; time estimates in minutes)

**Sprint 2 carry-overs (close Mon 25 May EOD):**
- Team review + finalise `docs/requirements.md`
- Confirm CBO count in VDSR dependency map
- Agree two refactoring examples (document in `PROGRESS.md`)
- Tool smoke-test + version docs
- Chase Sub-team 2 and Sub-team 4 for interface contracts

---

## How to Use This Folder with Cowork

- **Start every session** by reading `CLAUDE.md` (this file) and `PROGRESS.md`
- **After every session** update `PROGRESS.md` with what changed
- **When writing a deliverable**, check the section of the brief it maps to (referenced in each file)
- **For diagrams**, output PlantUML or Mermaid — never PNG/SVG only
