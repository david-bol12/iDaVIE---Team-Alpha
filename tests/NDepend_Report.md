# NDepend — Static Analysis Report

_Code metrics, rule violations, and technical-debt estimation for a single C# type, modeled on NDepend's CQLinq default rule set and Code Health metrics._

| Field | Value |
|---|---|
| Target file | VolumeDataSetRenderer.cs |
| Repository | github.com/idia-astro/iDaVIE |
| Path | Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs |
| Language | C# (Unity / .NET) |
| Analysis date | 25 May 2026 |

## 1. Technical Debt Summary

NDepend estimates technical debt as remediation time to fix rule violations, expressed as a debt ratio against estimated rewrite effort. Rating follows NDepend's default SQALE-style thresholds (A best).

**Debt Rating for file: B**

| Metric | Value |
|---|---|
| Estimated technical debt | ~4h 10m |
| Debt ratio | ~7% |
| Rule violations (this file) | 29 |
| Critical rule violations | 2 |
| Issues with severity High+ | 6 |

## 2. Code Metrics

| Metric (NDepend name) | Value | Recommended |
|---|---|---|
| # Lines of Code (LOC, logical est.) | 1127 | — |
| # Methods | 44 | < 20 per type |
| # Fields | 103 | < 20 per type |
| Cyclomatic Complexity (sum) | 192 | — |
| Average CC per method | 4.36 | < 5 |
| Max CC (single method) | 28 | ≤ 15 |
| Methods with CC > 15 | 3 | 0 |
| Max nesting depth | 3 | ≤ 3 |
| Comment ratio (% comment) | 8.9% | ≥ 20% |

## 3. Type-Level Observations

`VolumeDataSetRenderer` aggregates rendering, masking, region selection, cursor handling, moment-map control, and file I/O. NDepend flags such types under several default rules:

- **Types with too many methods/fields** — 44 methods and ~103 field declarations exceed the default guideline (20). Indicative of a God Type / low cohesion.
- **Type should not have low cohesion (LCOM)** — broad responsibility spread suggests LCOM above the 0.8 threshold; members operate on disjoint field subsets (mask vs. region vs. cursor state).
- **Avoid types too big** — 1127 logical lines in a single type exceeds the ~200 LOC soft guideline.

## 4. Method-Level Rule Violations

Methods ranked by cyclomatic complexity. Rules "Methods too complex" (CC>15), "Methods too big" (#lines>~60), and "Methods with too many nested branches" apply to highlighted rows.

| Method | CC | Lines | Params | Rules triggered |
|---|---|---|---|---|
| `_startFunc` | 28 | 185 | 0 | CC>15, Lines>60 |
| `SaveMask` | 19 | 83 | 1 | CC>15, Lines>60, Nest≥3 |
| `SetCursorPosition` | 17 | 54 | 2 | CC>15, Nest≥3 |
| `Update` | 12 | 97 | 0 | CC>10, Lines>60 |
| `SetRegionPosition` | 8 | 28 | 2 | Nest≥3 |
| `UpdateRegionBounds` | 8 | 33 | 0 | — |
| `LoadRegionData` | 8 | 34 | 2 | — |
| `PaintMask` | 8 | 23 | 2 | — |
| `InitialiseMask` | 7 | 23 | 0 | — |
| `OnDestroy` | 7 | 9 | 0 | — |
| `SetVideoCursorLocPosition` | 5 | 26 | 1 | — |
| `RegenerateCubes` | 4 | 11 | 0 | — |

## 5. Critical Rule Violations

- **`_startFunc` — "Methods too complex - critical".** CC 28 (limit 15) over 185 lines. This single coroutine-style initialiser dominates the type's complexity budget. Highest-priority refactor target.
- **`SaveMask` — "Methods too complex" + "Methods too big".** CC 19, 83 lines, nesting 1. Mixes I/O, validation, and state mutation.

## 6. Coupling & Dependencies

Most-referenced collaborators within the type (proxy for efferent coupling, Ce). Heavy concentration on a few external objects increases fragility to changes in those collaborators.

| Collaborator | References | Note |
|---|---|---|
| `_maskDataSet` | 92 | Very high — candidate for extraction |
| `_featureManager` | 29 | High |
| `_momentMapRenderer` | 14 | Moderate |
| `VolumeDataSet` | 8 | Moderate |
| `volumeInputController` | 4 | Moderate |
| `FeatureSetManager` | 3 | Moderate |
| `MeshRenderer` | 2 | Moderate |
| `VolumeInputController` | 2 | Moderate |

The dominant dependency on `_maskDataSet` (92 references) suggests mask-related behaviour should be extracted into its own collaborator, reducing responsibilities and improving testability.

## 7. Debt Breakdown by Rule

| Rule | Violations | Est. debt |
|---|---|---|
| Methods too complex | 3 | ~2h |
| Methods too big | 4 | ~1h |
| Type too big / too many members | 1 | ~40m |
| Avoid public fields | 14 | ~20m |
| Avoid commented-out code | 4 | ~10m |
