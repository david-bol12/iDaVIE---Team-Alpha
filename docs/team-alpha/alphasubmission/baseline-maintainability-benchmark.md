# iDaVIE — Initial Maintainability Benchmark Report

**Project:** iDaVIE---Team-Alpha  
**Tool:** NDepend v2026.1.5  
**Unity Version:** 2021.3.45f2  
**Baseline:** Same code-base snapshot
**Report Prepared:** 4 June 2026

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Analysis Scope](#2-analysis-scope)
3. [Overall Maintainability Rating](#3-overall-maintainability-rating)
4. [Technical Debt Overview](#4-technical-debt-overview)
5. [Quality Gate Results](#5-quality-gate-results)
6. [Issue Distribution by Severity](#6-issue-distribution-by-severity)
7. [Rule Violations by Category](#7-rule-violations-by-category)
8. [Code Size Metrics](#8-code-size-metrics)
9. [Code Complexity Metrics](#9-code-complexity-metrics)
10. [Architecture & Structure Metrics](#10-architecture--structure-metrics)
11. [Third-Party Dependencies](#11-third-party-dependencies)
12. [Code Coverage Gap](#12-code-coverage-gap)
13. [Key Findings & Risk Areas](#13-key-findings--risk-areas)
14. [Recommendations](#14-recommendations)
15. [Metric Reference](#15-metric-reference)

---

## 1. Executive Summary

This report establishes the **initial maintainability baseline** for the iDaVIE Unity VR project. It was produced from a single NDepend static analysis run over 37 compiled assemblies and 3,051 source files.

| Indicator | Value | Status |
|-----------|-------|--------|
| Total Issues | 20,031 | Requires attention |
| Critical Issues | 29 | **FAIL** (threshold: 10) |
| Technical Debt | 430.63 man-days (10.29%) | Moderate–High |
| Annual Interest on Debt | 295.03 man-days | High ongoing cost |
| Breaking Point | ~1.46 years | Urgent |
| Quality Gates Failed | 3 of 12 | **FAIL** |
| Rules Violated | 110 of 174 (63%) | Significant |
| Critical Rules Violated | 12 | **FAIL** (threshold: 0) |
| Code Coverage Data | Not imported | Gap |

**Key takeaway:** The codebase carries a meaningful initial debt load. Three quality gates are already failing. The most urgent risks are the 29 critical-severity issues, 12 critical rule violations, and 13 namespaces that exceed the acceptable debt rating. Without intervention, the annual cost of servicing this debt (~295 man-days/year) will surpass the one-time repayment cost within approximately **1.46 years**.

---

## 2. Analysis Scope

### Assemblies Analysed (37)

| # | Assembly |
|---|----------|
| 1 | Assembly-CSharp.dll *(application runtime code)* |
| 2 | Assembly-CSharp-Editor.dll *(application editor code)* |
| 3 | Nobi.UiRoundedCorners.dll |
| 4 | SteamVR.dll |
| 5 | SteamVR_Actions.dll |
| 6 | SteamVR_Editor.dll |
| 7 | SteamVR_Input_Editor.dll |
| 8 | SteamVR_Windows_EditorHelper.dll |
| 9 | Unity.CollabProxy.Editor.dll |
| 10 | Unity.Mathematics.dll |
| 11 | Unity.Mathematics.Editor.dll |
| 12 | Unity.PlasticSCM.Editor.dll |
| 13 | Unity.Rider.Editor.dll |
| 14 | Unity.TextMeshPro.dll |
| 15 | Unity.TextMeshPro.Editor.dll |
| 16 | Unity.Timeline.dll |
| 17 | Unity.Timeline.Editor.dll |
| 18–26 | Unity.VisualScripting.* (Core, Flow, State, Shared — runtime & editor) |
| 27 | Unity.VisualStudio.Editor.dll |
| 28 | Unity.VSCode.Editor.dll |
| 29 | Unity.XR.Management.dll |
| 30 | Unity.XR.Management.Editor.dll |
| 31 | Unity.XR.OpenVR.dll |
| 32 | Unity.XR.OpenVR.Editor.dll |
| 33 | UnityEditor.SpatialTracking.dll |
| 34 | UnityEditor.UI.dll |
| 35 | UnityEditor.XR.LegacyInputHelpers.dll |
| 36 | UnityEngine.SpatialTracking.dll |
| 37 | UnityEngine.UI.dll / UnityEngine.XR.LegacyInputHelpers.dll |

**Filtered out:** UnityEngine.TestRunner, UnityEditor.TestRunner (test assemblies excluded from analysis).

**NDepend analysis duration:** 1 minute 19 seconds.

---

## 3. Overall Maintainability Rating

NDepend's debt model expresses maintainability as a **percentage of estimated effort needed to fix all issues** relative to the total effort to build the system.

| Metric | Value | Benchmark Guidance |
|--------|-------|--------------------|
| Debt Percentage | **10.29%** | < 5% = Good; 5–10% = Moderate; > 10% = High |
| Debt Amount | 430.63 man-days | ~1.72 developer-years |
| Annual Interest | 295.03 man-days | ~1.18 developer-years/year ongoing |
| Breaking Point | **1.46 years** | < 2 years = Urgent action recommended |
| Breaking Point (Critical/High only) | **~0.99 years** | < 1 year = Immediate action required |

The project sits just above the 10% threshold, placing it in the **High** debt tier. The breaking point for the full debt is 1.46 years, and for blocker/critical/high issues alone it is already under 1 year, indicating the most severe issues will soon cost more to carry than to fix.

---

## 4. Technical Debt Overview

### Debt Composition

```
Total Debt:  430.63 man-days
├─ Blocker issues        0 man-days
├─ Critical issues      ~18 man-days  (estimated from 29 issues)
├─ High issues        ~274 man-days  (estimated from 4,455 issues)
├─ Medium issues      ~107 man-days  (estimated from 13,067 issues)
└─ Low issues          ~31 man-days  (estimated from 2,480 issues)
```

### Debt Trajectory

Since this is the **initial baseline** (v0.0), no delta from a previous snapshot exists:

- New Debt since Baseline: 0 man-days
- New Annual Interest since Baseline: 0 man-days
- Lines of Code Added since Baseline: 37

All debt present is **inherited debt** from the existing codebase. Future analyses will measure incremental changes against this snapshot.

---

## 5. Quality Gate Results

Of the 12 quality gates evaluated, **3 failed** and **3 were skipped** (no coverage data). No gates were in a "warn" state.

### Failed Quality Gates

| Quality Gate | Measured Value | Fail Threshold | Severity |
|-------------|---------------|----------------|----------|
| Critical Issues | **29 issues** | 10 issues | FAIL |
| Critical Rules Violated | **12 rules** | 0 rules | FAIL |
| Debt Rating per Namespace | **13 namespaces** | 0 namespaces | FAIL |

#### Details

- **Critical Issues (29 vs threshold 10):** Twenty-nine individual code locations carry NDepend's critical severity rating. This is nearly three times the acceptable threshold and must be prioritised for remediation before new feature development.

- **Critical Rules Violated (12 vs threshold 0):** Twelve entire rule categories are violated at a critical level. Zero tolerance is the standard, so any violation here represents a process or design-level concern.

- **Debt Rating per Namespace (13 vs threshold 0):** Thirteen namespaces carry a debt rating exceeding the acceptable ceiling. These namespaces are hotspots that disproportionately drive overall technical debt.

### Skipped Quality Gates (Coverage Data Required)

| Quality Gate | Reason Skipped |
|-------------|----------------|
| Percentage Coverage | No code coverage data imported |
| Percentage Coverage on New Code | No code coverage data imported |
| Percentage Coverage on Refactored Code | No code coverage data imported |

Additionally, **13 coverage-dependent rules** were skipped entirely.

---

## 6. Issue Distribution by Severity

| Severity | Count | % of Total | NDepend Definition |
|----------|-------|------------|-------------------|
| Blocker | **0** | 0.0% | Must be fixed before release |
| Critical | **29** | 0.1% | Should be fixed in current sprint |
| High | **4,455** | 22.2% | Should be fixed soon |
| Medium | **13,067** | 65.2% | Should be addressed over time |
| Low | **2,480** | 12.4% | Fix when convenient |
| **Total** | **20,031** | 100% | |

### Observations

- The absence of Blocker issues is positive.
- The **Critical** count of 29 already breaches the quality gate threshold.
- The dominant severity tier is **Medium (65%)**, suggesting a broadly diffuse code quality problem rather than a few isolated hotspots.
- **High-severity issues (4,455)** account for the largest share of estimated debt cost and represent the most impactful remediation target after criticals.
- Combining Blocker + Critical + High: **4,484 issues** (22.4% of all issues) represent the priority remediation backlog.

### Suppressed Issues

**0 issues suppressed.** The project does not use `SuppressMessageAttribute`, so the raw issue count is unfiltered.

---

## 7. Rule Violations by Category

**110 of 174 rules (63.2%) are violated.** The NDepend rule set for this project covers 17 categories. Below is the breakdown of which categories contribute to the violations.

### Rule Category Summary

| Category | Rules in Set | Relevant Findings |
|----------|-------------|-------------------|
| Code Smells | 9 | Large types/methods, poor cohesion, unmaintainable code |
| Object Oriented Design | 14 | Singleton pattern, virtual method calls in constructors, init cycles |
| Design | 13 | Disposable types, obsolete usage, unimplemented NotImplementedException stubs |
| Architecture | 4 | Mutually dependent namespaces, dependency cycles, poor cohesion |
| Dead Code | 3 | Potentially dead types, methods, and fields |
| Security | 2 | System.Random for security, publicly visible pointers |
| Visibility | 8 | Over-exposed methods, types, fields, and constructors |
| Immutability | 9 | Non-readonly fields, mutable static fields, impure property getters |
| Naming Conventions | 16 | Interface naming, abstract base suffixes, capitalisation |
| Source Files Organisation | 5 | Multiple types per file, namespace/file mismatches |
| System | 8 | GC.Collect calls, improper exception types, ApplicationException derivation |
| System.Collections | 2 | Non-generic ArrayList/HashTable usage, writable collection properties |
| System.Threading | 3 | Explicit thread creation, dangerous threading methods |
| System.Xml | 1 | XmlDocument derivation |
| System.Globalization | 1 | Culture-unaware float/date parsing |
| System.Reflection | 2 | Version mismatches, assemblies referenced in multiple versions |
| Unity (project-specific) | 7 | Non-generic GetComponent, empty Unity messages, Time.fixedDeltaTime in Update |

### Critical Rules Violated (12)

Twelve rules are flagged at the critical level. Based on the rule set and the failing quality gate, these are drawn from the categories most likely to introduce runtime defects, security issues, or severe maintainability regressions. The Unity-specific rules (ND3200–ND3208) are particularly relevant given the project domain.

Notable Unity-specific critical rule areas include:
- **ND3202 — Avoid empty Unity messages:** Empty `Awake`, `Update`, `Start`, etc. impose runtime overhead in Unity's message dispatch system.
- **ND3201 — Avoid using non-generic GetComponent:** Non-generic lookups use boxing and reflection.
- **ND3203 — Avoid using Time.fixedDeltaTime with Update:** Misuse causes frame-rate-dependent physics calculations.

---

## 8. Code Size Metrics

| Metric | Value |
|--------|-------|
| Total Lines of Code | 154,641 |
| JustMyCode LoC | 153,586 (99.3%) |
| NotMyCode (generated) LoC | 1,055 (0.7%) |
| Lines of Comments | 73,922 |
| **Comment Ratio** | **32.34%** |
| Source Files | 3,051 |
| IL Instructions | ~905,133 |
| Line Feeds (blank lines) | 522,045 |

### Comment Coverage — Positive Signal

A **32.34% comment ratio** is notably healthy. It indicates the team has invested in code documentation, which helps on-boarding and maintainability. Standard guidance suggests 15–30% as acceptable; iDaVIE is above this range.

However, NDepend rule **ND1006 (Avoid methods potentially poorly commented)** is included in the rule set. If this rule is violated, some of the comment coverage may be concentrated in less critical areas while key complex methods remain undocumented.

### Codebase Scale Context

At ~153,600 JustMyCode lines across 3,051 files, this is a **medium-to-large** Unity project. The ratio of methods (38,794) to LoC (154,641) gives approximately **4 LoC per method on average**, which is compact but can hide complexity in outlier methods.

---

## 9. Code Complexity Metrics

### Method-Level Complexity

| Metric | Value | Threshold Guidance |
|--------|-------|-------------------|
| Max LoC per Method (JustMyCode) | **1,396** | > 60 = Too large |
| Average LoC per Method | 4.13 | Acceptable |
| Average LoC per Method (≥3 LoC) | 9.96 | Acceptable |
| **Max Cyclomatic Complexity** | **575** | > 25 = Unmaintainable |
| Average Cyclomatic Complexity | 1.95 | Good |
| Max IL Cyclomatic Complexity | 502 | > 25 = Unmaintainable |
| Average IL Cyclomatic Complexity | 2.29 | Good |
| **Max IL Nesting Depth** | **161** | > 8 = Very deep |
| Average IL Nesting Depth | 0.50 | Good |

### Type-Level Complexity

| Metric | Value | Threshold Guidance |
|--------|-------|-------------------|
| Max LoC per Type (JustMyCode) | **3,422** | > 500 = Too large |
| Average LoC per Type | 45.42 | Acceptable |
| Max Methods per Type | **1,970** | > 20 = Too many |
| Average Methods per Type | 8.61 | Acceptable |
| Max Methods per Interface | 48 | > 15 = Interface too large |
| Average Methods per Interface | 3.8 | Good |

### Complexity Risk Summary

While the **averages are healthy** across all complexity dimensions, the **maximum values reveal extreme outlier code entities**:

- A single method has **1,396 lines of code** — this is roughly 23× the recommended maximum of 60.
- A single method has a **cyclomatic complexity of 575** — this is 23–57× the standard acceptable range of 10–25. Methods above ~25 are generally considered untestable and unmaintainable.
- A single type contains **3,422 lines of code** — 6.8× the recommended maximum.
- A single type has **1,970 methods**, which may indicate an auto-generated or deeply polymorphic type.
- A maximum IL nesting depth of **161** indicates deeply nested logic in at least one code path.

These outliers disproportionately drive the critical and high issue counts and should be identified and refactored as a priority.

---

## 10. Architecture & Structure Metrics

| Metric | Value |
|--------|-------|
| Assemblies | 37 |
| Namespaces | 152 |
| Types | 4,992 |
| Public Types | 3,095 (62.0%) |
| Classes | 3,899 |
| Abstract Classes | 215 (5.5% of classes) |
| Interfaces | 277 |
| Structures | 418 |
| Methods | 38,794 |
| Abstract Methods | 1,318 (3.4%) |
| Concrete Methods | 37,486 (96.6%) |
| Fields | 12,534 |

### Structural Observations

**Public surface area:** 62% of all types are public. Industry guidance typically recommends minimising public surface area to reduce coupling. The architecture rules (ND1800–ND1808 visibility rules) are likely generating a significant share of the 20,031 issues. Reducing unnecessary public exposure would improve encapsulation and lower both issue count and debt.

**Namespace dependency issues:** The architecture quality gate failure on "Debt Rating per Namespace (13 namespaces)" indicates 13 namespaces are architectural hotspots. Mutually dependent namespaces (ND1400) and namespace dependency cycles (ND1401) are among the architecture rules in the rule set.

**No assembly-level dependency cycles detected** — this is a positive finding confirmed in the InfoWarnings log: "No dependency cycle detected in assemblies reference graph."

**Interfaces are reasonably sized on average** (3.8 methods), but a maximum of 48 methods on a single interface violates the principle of interface segregation and triggers ND1200 (Avoid interfaces too big).

---

## 11. Third-Party Dependencies

| Metric | Value |
|--------|-------|
| Third-Party Assemblies Used | 53 |
| Third-Party Namespaces Used | 225 |
| Third-Party Types Used | 1,816 |
| Third-Party Methods Called | 5,772 |
| Third-Party Fields Used | 446 |

The project depends on **53 third-party assemblies** (compared to 37 analysed assemblies). This is a relatively high ratio. Key third-party dependency groups include Unity engine modules, SteamVR/OpenVR SDK, TextMeshPro, XR Management, and the Visual Scripting framework.

A high external dependency count increases:
- Upgrade risk when Unity or plugin versions change
- The likelihood of rule violations in categories such as System.Reflection (ND2802 — Assemblies Referenced in Multiple Versions) and Design (ND1311 — Don't use obsolete types, methods or fields)

---

## 12. Code Coverage Gap

### Coverage Data: Not Imported

NDepend attempted to evaluate **13 coverage-dependent rules** and **3 coverage-related quality gates**, but all were **skipped** because no code coverage data was provided to the analysis.

The skipped rules include:

| Rule | Description |
|------|-------------|
| Code should be tested | Base coverage requirement |
| Complex Methods should be 100% tested | High-complexity methods must be covered |
| Methods should have a low C.R.A.P score | Change Risk Anti-Patterns score |
| New Types and Methods should be tested | Coverage gate for new code |
| Assemblies and Namespaces should be tested | Assembly-level coverage |
| Types 100% covered should be tagged | Regression guard |
| (+ 7 more) | Various coverage thresholds |

### Risk Implications

Given that:
- The maximum cyclomatic complexity is **575** (indicating extremely hard-to-test code paths)
- There are **4,484 high-severity issues** that likely include complex methods
- **154,641 lines are flagged as uncoverable** (LoC Uncoverable = 154,641, matching total LoC — this indicates coverage instrumentation was never run)

The absence of test coverage data represents a **significant blind spot**. Without coverage data:
- The true risk profile of complex methods is unknown
- C.R.A.P scores (Complexity × (1 – Coverage)²) cannot be evaluated
- Regressions in untested code cannot be detected via static analysis

**Recommendation:** Integrate Unity Test Runner results (or an external coverage tool such as OpenCover or dotCover) with NDepend in the next analysis cycle.

---

## 13. Key Findings & Risk Areas

### Finding 1 — Extreme Method & Type Outliers (Critical Risk)
One or more methods have cyclomatic complexity of **575** and up to **1,396 lines of code**. Based on the highlighted source files in the report, `SteamVR_Action.cs` is a likely candidate for extreme size and complexity. These outliers are untestable, highly bug-prone, and virtually impossible to review safely.

### Finding 2 — Three Quality Gates Failing (High Risk)
The codebase fails on Critical Issues, Critical Rule Violations, and Namespace Debt Rating at the very first measurement. This means any PR gate that checks these quality gates would already be blocking. Establishing a fix plan for the 29 critical issues is the immediate priority.

### Finding 3 — Breaking Point Under 1 Year for Critical/High Issues (High Risk)
The breaking point for blocker/critical/high issues alone is **~0.99 years**. If the highest-severity debt is not addressed, the annual cost of carrying it will exceed the one-time cost of fixing it before the next major release cycle.

### Finding 4 — No Code Coverage Baseline (Medium Risk)
The project has no coverage data integrated with static analysis. With a maximum cyclomatic complexity of 575, even partial coverage could leave thousands of code paths untested. This gap means high-complexity code is being shipped without a safety net.

### Finding 5 — High Public Surface Area (Medium Risk)
62% of types are public (3,095 of 4,992). This inflates coupling and makes refactoring harder. Visibility rules (ND1800–ND1808) are a significant source of issues and debt.

### Finding 6 — 13 Namespace Architectural Hotspots (Medium Risk)
Thirteen namespaces exceed the acceptable debt rating ceiling, indicating they are architectural concentrations of technical debt. These namespaces should be identified in the NDepend report and scheduled for architectural review.

### Finding 7 — Unity-Specific Rule Violations (Medium Risk)
Unity-specific rules (ND3201–ND3208) are in the active rule set and some are violated. Empty Unity messages (Awake, Start, Update, FixedUpdate) impose unnecessary per-frame overhead in Unity's internal message dispatch, which is especially costly in a real-time VR application where frame budget is critical.

### Finding 8 — Healthy Comment Ratio (Positive Finding)
A 32.34% comment ratio is above the industry average and indicates good documentation practice. This should be maintained as a team standard going forward.

### Finding 9 — No Assembly-Level Dependency Cycles (Positive Finding)
NDepend confirmed no circular dependencies at the assembly level, which is a strong architectural foundation.

---

## 14. Recommendations

Recommendations are ordered by priority (immediate → short-term → medium-term).

### Immediate (Sprint 1–2)

| # | Action | Target |
|---|--------|--------|
| R1 | **Identify and triage the 29 critical-severity issues.** Use the NDepend report to list them. Assign each to a developer for resolution before new features are merged. | Critical Issues QG |
| R2 | **Address the 12 critical rule violations.** Review the NDepend rule report for the specific rules. Most can be resolved by refactoring the offending code entities. | Critical Rules QG |
| R3 | **Identify the 13 over-budget namespaces.** Determine which namespaces fail the Debt Rating gate and assign ownership for cleanup sprints. | Namespace Debt QG |

### Short-Term (Sprint 3–6)

| # | Action | Target |
|---|--------|--------|
| R4 | **Refactor the highest-complexity methods.** Locate the method(s) with cyclomatic complexity > 25 (especially the one scoring 575). Break them into smaller, single-responsibility methods. | Complexity |
| R5 | **Refactor the largest types.** Identify types exceeding 500 LoC (max is 3,422) and apply the Single Responsibility Principle. | Code Smells |
| R6 | **Integrate code coverage into the NDepend pipeline.** Run Unity Test Runner and export coverage data. Re-run NDepend with coverage to unlock the 13 skipped rules and 3 quality gates. | Coverage Gap |
| R7 | **Remove or implement empty Unity messages.** Audit all `Awake`, `Start`, `Update`, `FixedUpdate`, etc. Empty messages should be deleted; implemented ones confirmed correct. Critical for VR frame performance. | Unity Rules |
| R8 | **Replace non-generic GetComponent calls.** Migrate to `GetComponent<T>()` throughout the codebase to eliminate boxing overhead in hot paths. | Unity Rules |

### Medium-Term (Ongoing)

| # | Action | Target |
|---|--------|--------|
| R9 | **Reduce public surface area.** Apply the principle of least privilege to type and method visibility. Target a reduction from 62% public types toward 40–50%. | Visibility Rules |
| R10 | **Enforce naming conventions.** Address the naming convention violations (ND2000–ND2022) through automated style enforcement (e.g., .editorconfig + Roslyn analyzers). | Naming Rules |
| R11 | **Introduce NDepend quality gate as a CI check.** Gate pull requests on the three failing quality gates. Once all three pass, no PR may cause them to fail again. | Process |
| R12 | **Schedule architectural review of mutually dependent namespaces.** Resolve any namespace dependency cycles found in rules ND1400–ND1401. | Architecture Rules |
| R13 | **Establish a debt budget.** Target a maximum debt percentage of 5% (currently 10.29%). Allocate 1–2 days per sprint for debt repayment until the breaking point recedes beyond 3 years. | Debt Sustainability |

---

## 15. Metric Reference

| Metric | Definition |
|--------|-----------|
| Technical Debt | Estimated effort (man-days) to fix all rule violations |
| Debt Percentage | Debt as a % of estimated effort to build the codebase from scratch |
| Annual Interest | Estimated ongoing maintenance overhead per year due to carrying the debt |
| Breaking Point | Time until annual interest cost equals the one-time debt repayment cost |
| Cyclomatic Complexity | Number of linearly independent paths through a method (McCabe metric) |
| IL Nesting Depth | Depth of nested scopes in compiled IL code |
| JustMyCode | Code written by the project team (excludes generated/third-party code) |
| C.R.A.P Score | Change Risk Anti-Patterns: Complexity × (1 – Coverage)² |
| Quality Gate | A pass/fail threshold check applied to a specific metric |
| LoC | Lines of Code (non-blank, non-comment) |

---

*Report generated from NDepend v2026.1.5 analysis results. All metric values are point-in-time as of 20 May 2026. This document serves as the v0.0 baseline against which future analyses will be compared.*
