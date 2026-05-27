# SonarQube — Static Analysis Report

_Reliability, Security, and Maintainability assessment of a single Unity C# source file, modeled on SonarQube / SonarScanner rule sets (Sonar way profile)._

| Field | Value |
|---|---|
| Target file | VolumeDataSetRenderer.cs |
| Repository | github.com/idia-astro/iDaVIE |
| Path | Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs |
| Language | C# (Unity / .NET) |
| Analysis date | 25 May 2026 |

## 1. Quality Gate

Overall status against the default Sonar way quality gate. The gate fails primarily on maintainability and the proportion of complex methods relative to file size.

**Quality Gate: FAILED**

| Condition | Measure | Threshold | Status |
|---|---|---|---|
| Reliability rating | A | ≤ A | Pass |
| Security rating | A | ≤ A | Pass |
| Maintainability rating | A | ≤ A | Pass |
| Cognitive complexity (file) | ~260 | n/a (advisory) | Warn |
| Comment density | 8.9% | ≥ 25% (custom) | Fail |
| Complex methods (CC>15) | 3 | 0 preferred | Fail |

## 2. Ratings Overview

SonarQube assigns letter ratings (A best, E worst) per quality dimension based on issue counts weighted by severity and on remediation effort versus development cost.

| Dimension | Rating | Issues | Remediation effort |
|---|---|---|---|
| Reliability (Bugs) | A | 0 Bug | 0 min |
| Security (Vulnerabilities) | A | 0 Vuln | 0 min |
| Security Review (Hotspots) | — | 2 Hotspots | Review |
| Maintainability (Code Smells) | A | 31 Smells | ~3h 45m |

_Note: Technical-debt ratio remains low because the file is large (1,403 lines), so absolute remediation effort divides over a high development-cost baseline, keeping the maintainability rating at A despite numerous smells._

## 3. Size & Measures

| Metric | Value |
|---|---|
| Lines (physical) | 1403 |
| Lines of code (ncloc) | 1127 |
| Comment lines | 110 |
| Comment density | 8.9% |
| Functions | 44 |
| Classes / types | 4 |
| Cyclomatic complexity (sum) | 192 |
| Cognitive complexity (est.) | ~260 |
| Duplicated blocks | 1 |
| Duplicated lines density | <1% |

## 4. Issues by Severity

SonarQube classifies each issue by type (Bug / Vulnerability / Code Smell) and severity (Blocker, Critical, Major, Minor, Info).

| Severity | Bugs | Vulnerabilities | Code Smells |
|---|---|---|---|
| Blocker | 0 | 0 | 0 |
| Critical | 0 | 0 | 2 |
| Major | 0 | 0 | 12 |
| Minor | 0 | 0 | 14 |
| Info | 0 | 0 | 3 |

## 5. Detailed Findings (Code Smells)

### 5.1 Critical — Methods too complex

- **`_startFunc`** — Cognitive/cyclomatic complexity far above threshold (cyclomatic 28, 185 lines). Rule **csharpsquid:S3776** (Cognitive Complexity, limit 15). Refactor into smaller private helpers.
- **`SaveMask`** — Cyclomatic complexity 19 over 83 lines, nesting depth 1. S3776 triggered; extract file-writing and validation branches.

### 5.2 Major — Complexity, long methods, nesting

- **`SetCursorPosition`** (CC 17) and **`Update`** (CC 12, 97 lines) exceed complexity guidance. **S3776 / S138**.
- Several methods nest control flow three levels deep (**S134**): `SetCursorPosition`, `SaveMask`, `SetRegionPosition`, `PaintCursor`.
- 14 public mutable fields exposed directly (**S1104**). Prefer properties with controlled setters.
- 60 lines exceed 120 characters (**S103**).

### 5.3 Minor — Conventions & clarity

- 21 direct `Debug.Log*` calls (**S106** family). Consider a gated logging abstraction.
- 4 lines of commented-out code (**S125**). Remove dead code; rely on version control.
- Magic numbers in scaling, region, and cursor math (**S109**). Promote to named constants.
- Two private methods share the name `SelectFeature` with differing signatures — verify intentional overloading (**S4144** family).

### 5.4 Info

- Low comment density (8.9%) relative to a 1,400-line file with substantial branching.
- Single `#region` in a long file; heavy reliance on regions can signal a god-class tendency.

## 6. Security Hotspots

Hotspots are security-sensitive code requiring manual review rather than confirmed vulnerabilities.

| Hotspot | Category | Priority |
|---|---|---|
| File path construction in `SaveMask` / `GetMaskSavedFilePath` | Path handling / I/O | Medium |
| Regex usage (`System.Text.RegularExpressions`) | ReDoS review | Low |

Recommendation: validate and canonicalise output paths before writing mask files, and confirm regex patterns are bounded against pathological input.

## 7. Remediation Summary

| Action | Rules addressed | Est. effort |
|---|---|---|
| Decompose `_startFunc`, `SaveMask`, `SetCursorPosition` | S3776, S138, S134 | ~2h |
| Encapsulate public fields as properties | S1104 | ~45m |
| Replace magic numbers with constants | S109 | ~30m |
| Remove commented-out code | S125 | ~10m |
| Wrap/guard logging calls | S106 | ~20m |
