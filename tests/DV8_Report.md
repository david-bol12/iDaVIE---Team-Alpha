# DV8 — Static Analysis Report

_Architecture quality and design-flaw analysis, modeled on DV8's Design Rule Hierarchy, Design Rule Spaces (DRSpaces), and architecture anti-pattern detection._

| Field | Value |
|---|---|
| Target file | VolumeDataSetRenderer.cs |
| Repository | github.com/idia-astro/iDaVIE |
| Path | Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs |
| Language | C# (Unity / .NET) |
| Analysis date | 25 May 2026 |

_Scope note: DV8 analyses architecture at the scale of files and their dependency network. A single file is one node in that network, so this report frames `VolumeDataSetRenderer.cs` as a participant in DV8's anti-pattern flaws, using its internal structure and outbound coupling as evidence._

## 1. Architecture Health Indicators

**File role in architecture: HUB / HIGH-FAN-IN COMPONENT**

| Indicator | Observation for this file |
|---|---|
| Fan-out (outbound deps) | Concentrated on ~8 collaborators; 2 dominate |
| Internal cohesion | Low — multiple unrelated responsibilities |
| Change-proneness risk | High — large, complex, central renderer |
| Anti-patterns participated in | 4 of DV8's standard flaw types (below) |

## 2. Architecture Flaws (Anti-Patterns)

### 2.1 Unstable Interface

A heavily depended-upon file that is itself changing forces ripple changes across dependents. `VolumeDataSetRenderer` exposes 152 public members and 14 public mutable fields — a wide, mutable surface. Combined with its size and complexity, it is a strong Unstable Interface candidate.

### 2.2 Modularity Violation (Crossing)

Structurally independent files that change together signal hidden coupling. Direct manipulation of `_maskDataSet` (92 references) and `_featureManager` (29 references) reaches deep into collaborators' state rather than through narrow interfaces — the precondition for Crossing/Modularity Violation flaws.

### 2.3 Unhealthy Inheritance / God Component

With 44 methods, ~103 fields, and 1127 lines spanning rendering, masking, region selection, cursor handling, moment maps, and file I/O, this file behaves as a God Component — a single point whose modification affects many concerns.

### 2.4 Clique / Cyclic Dependency (confirmed at codebase level)

The whole-codebase analysis confirms this file sits inside a 46-file dependency cycle (see architecture report), validating the suspected clique among `VolumeDataSetRenderer`, `VolumeInputController`, and the Feature/Moment-map managers.

## 3. Design Rule Space (DRSpace) View

| DRH layer | Placement of this file | Implication |
|---|---|---|
| Leading design rules | Likely — wide public surface | Should be stable; currently is not |
| Module independence | Poor — deep collaborator access | Refactor toward interfaces |
| Layering | Cross-cuts render/UI/IO layers | Split by concern |

## 4. Maintainability Debt Contribution

| Debt driver | Evidence | Severity |
|---|---|---|
| Wide mutable interface | 14 public fields, 152 public members | High |
| Concentrated complexity | Max CC 28, 4 methods CC>10 | High |
| Deep collaborator access | _maskDataSet referenced 92× | Medium-High |
| Size / multi-responsibility | 1127 LOC, 44 methods | Medium |

## 5. Architectural Recommendations

- Extract a `MaskManager`/`MaskController` to own mask state, eliminating the dominant `_maskDataSet` coupling (92 references) and shrinking this type's interface.
- Narrow the public surface: convert the 14 public fields to read-only properties or intent-revealing methods, reducing the Unstable Interface flaw.
- Break `_startFunc` and the other high-complexity methods into cohesive private units.
- Break the confirmed cycle among `VolumeDataSetRenderer` ↔ `VolumeInputController` ↔ Feature/Moment managers via interfaces or events (dependency inversion).
- Run DV8 across the full solution with Git history for exact Decoupling Level, Propagation Cost, and dollar-valued Maintainability Debt.
