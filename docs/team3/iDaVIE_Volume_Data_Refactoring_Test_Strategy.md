# iDaVIE — Sub-Team Test Strategy
## Volume Data Code Refactoring Initiative

| Field | Detail |
|---|---|
| **Project** | iDaVIE---Team-Alpha |
| **Initiative** | Volume Data Code Refactoring |
| **Document Type** | Sub-Team Test Strategy |
| **Version** | 1.0 |
| **Date** | May 28, 2026 |
| **Based on** | CodeScene Code Health Overview Report — May 28, 2026 |
| **Team Size** | 4 active developers |

---

## 1. Purpose and Scope

This document defines the test strategy for the iDaVIE sub-team undertaking the Volume Data Code Refactoring initiative. It establishes how the team will validate that refactored code is correct, that existing functionality is not broken, and that code health improves to a measurable degree over the course of the work.

The strategy is grounded in the CodeScene Code Health Overview Report (May 28, 2026), which shows that the codebase currently sits at an **Average Code Health of 6.8 (Problematic)** with a declining trend. Only **32% of the codebase is healthy**, while **21% (6,460 LOC) is classified as unhealthy (red)**. Unhealthy code carries a documented risk of **15× more defects**, **2× longer development time**, and **9× lower certainty** in delivery. The refactoring initiative directly targets this risk, making a rigorous, structured test strategy essential.

---

## 2. Objectives

The testing effort for this initiative has four primary objectives:

1. **Validate correctness** — Confirm that all refactored modules produce identical outputs to pre-refactoring behaviour under equivalent inputs.
2. **Prevent regression** — Ensure that changes to volume data handling do not silently break adjacent or dependent functionality.
3. **Reduce defect risk in hotspots** — Focus testing effort on the files most likely to contain and propagate defects, as identified by CodeScene's hotspot analysis.
4. **Measure code health improvement** — Use post-refactoring CodeScene scans as a quantitative quality gate, targeting an improvement in Hotspot Code Health from the current **4.8** toward ≥6.0, and moving the Worst Performer score above **1.9**.

---

## 3. Current Code Health Baseline

The following baseline metrics, drawn from the CodeScene report, inform prioritisation and exit criteria throughout this strategy.

| Metric | Current Score | Status | 1 Year Ago | Trend |
|---|---|---|---|---|
| Hotspot Code Health | 4.8 | Problematic | 5.5 | Declining |
| Average Code Health | 6.8 | Problematic | 7.0 | Declining |
| Worst Performer | 1.9 | Unhealthy | 1.9 | Flat |
| Healthy code (%) | 32% (9,844 LOC) | — | — | — |
| Problematic code (%) | 47% (14,459 LOC) | — | — | — |
| Unhealthy code (%) | 21% (6,460 LOC) | — | — | — |

**Active risks to note:**
- 14 files are actively declining in code health.
- 4 files are rising on the hotspot ranking while simultaneously declining in code health — these are the highest-priority candidates for test coverage.
- 13 possible ex-developers have been identified, creating knowledge gaps and elevated risk in the areas of code they owned.

---

## 4. Test Approach

The strategy uses three complementary testing layers. Together they provide confidence from the smallest unit of logic up to the full integration path.

### 4.1 Unit Testing

**Goal:** Ensure each refactored function, class, or module behaves correctly in isolation, and that no logic was lost or altered during refactoring.

**Approach:**

Unit tests should be written or updated alongside every refactoring change, not after the fact. Each piece of code that is modified must have a corresponding unit test that covers its primary logic path before the PR is considered complete.

For the volume data context, unit tests should specifically cover:

- Data parsing and transformation functions (boundary values: empty datasets, single-record datasets, maximum expected dataset sizes).
- Calculation or aggregation routines operating on volume data fields — test both typical inputs and edge cases such as nulls, zeros, and large numeric values.
- Any utility or helper functions extracted during refactoring — new extracted functions are a common source of regressions if not tested independently.

**Coverage target:** Refactored files should reach a minimum of **80% line coverage** before merge. Files that currently carry an Unhealthy (red) code health score should be held to **90% coverage** given the documented 15× higher defect rate in this code.

**Framework guidance:** Use the existing test framework already adopted by the team. If no unit test suite exists for a module being refactored, establishing one is a prerequisite for that refactoring work being marked complete.

**Definition of done for unit tests:**
- All tests pass in CI.
- No new test skips or commented-out assertions are introduced.
- Coverage thresholds are met and reported.

---

### 4.2 Risk-Based Testing (Hotspot Focus)

**Goal:** Concentrate the most rigorous testing effort on the files that carry the highest combined risk — those that are both heavily worked on (hotspots) and declining in code health.

**Approach:**

CodeScene identifies hotspots as files with the highest development activity. The current Hotspot Code Health score of **4.8 (Problematic)** — down from 5.5 a year ago — means the files the team touches most frequently are also the most fragile. Four files are specifically flagged as climbing the hotspot ranking while their health is declining; these warrant the highest testing priority.

Risk-based test prioritisation for this initiative should operate as follows:

**Tier 1 — Critical (test first and most thoroughly):**
Files appearing on both the hotspot list and the declining health list. Any change to these files must be accompanied by full unit test coverage of touched functions plus an integration smoke test of the downstream path they feed into.

**Tier 2 — High (test before merge):**
Files with code health below 4.0 (Unhealthy range) that are touched during refactoring, even if they are not top hotspots. Given the Worst Performer score of 1.9, at least one file falls into critical territory and should be treated with special care — a dedicated review and test session before any changes are committed.

**Tier 3 — Standard (normal test coverage):**
Files in the Problematic (4.0–8.9) range that are modified. Unit tests and regression checks apply, but no special escalation is needed unless they appear on the hotspot list.

**Knowledge gap risk:** With 13 possible ex-developers identified, some of the riskiest code may have no active team member who is deeply familiar with it. Before refactoring any file in this category, the sub-team should assign a designated code owner and conduct a brief knowledge-transfer session. This reduces the likelihood of introducing subtle regressions in poorly understood code paths.

---

### 4.3 Integration and Regression Testing

**Goal:** Confirm that refactored volume data components work correctly end-to-end, and that no existing functionality has been inadvertently broken.

**Approach:**

Refactoring by definition changes structure without changing behaviour. The integration test suite is the primary mechanism for proving that this contract has been upheld.

**Pre-refactoring baseline:** Before any refactoring begins on a module, the team must document the module's expected integration-level behaviour. Where integration tests already exist, run them and confirm they pass. Where they do not exist, write a minimal set of characterisation tests that capture current behaviour — these become the regression baseline.

**Regression test areas for volume data refactoring:**

- **Data ingestion paths:** Any pipeline that reads, loads, or pre-processes volume data must be tested end-to-end with both small and realistic large datasets to confirm throughput and correctness are preserved.
- **Data output / export paths:** Any function that produces output from volume data (files, API responses, UI data structures) must be tested to verify the format and values are unchanged post-refactoring.
- **Cross-module dependencies:** Given the coupling risks inherent in a codebase with declining code health, the team should map first-order dependencies for each refactored file and include those dependent modules in regression runs.
- **Error handling:** Refactored code must preserve or improve existing error handling. Test that invalid or malformed volume data inputs still fail gracefully and produce the same error outputs as before.

**Regression test cadence:**

| Stage | When to Run | Scope |
|---|---|---|
| Pre-refactor | Before starting work on a module | Smoke test of the full affected path |
| Per PR | Automated, on every pull request | Unit tests + targeted integration tests |
| Sprint end | End of each sprint | Full regression suite against the main branch |
| Post-release | After deployment to staging/production | End-to-end validation with production-representative data |

**Pull Request integration:** The CodeScene report notes that PR Integration is not currently configured. Enabling CodeScene's PR Integration is **strongly recommended** as part of this initiative. It will automatically flag code health degradations before merge, ensuring that refactoring work does not inadvertently worsen the health of files it touches.

---

## 5. Test Entry and Exit Criteria

### Entry Criteria (before refactoring work begins on a module)

- A baseline CodeScene health score is recorded for the module.
- Existing integration tests pass (or characterisation tests are written where none exist).
- A code owner is assigned for any module flagged as lacking active knowledge coverage.
- The module is listed in the sprint backlog with acceptance criteria that include test coverage requirements.

### Exit Criteria (before a refactoring PR is merged)

- All unit tests pass in CI with no failures or skips.
- Coverage thresholds are met (80% general, 90% for Unhealthy files).
- The full regression suite passes with no new failures.
- A post-refactor CodeScene scan shows the file's code health score has not declined (and ideally has improved).
- A peer code review has been completed by a team member other than the author.

---

## 6. Roles and Responsibilities

| Role | Responsibility |
|---|---|
| Developer (refactoring owner) | Write unit tests alongside refactoring changes; meet coverage thresholds; run local regression checks before raising a PR |
| Peer reviewer | Verify test coverage and quality during code review; confirm that integration paths are covered |
| Team lead / Tech lead | Monitor CodeScene health trends sprint-over-sprint; approve exit criteria are met before marking a module's refactoring as complete |
| All team members | Raise issues immediately if a regression is discovered during the sprint; do not defer regression failures |

---

## 7. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Refactoring worsens hotspot health | Medium | High | CodeScene PR Integration gate; mandatory post-refactor scan |
| Knowledge gaps cause silent regressions in ex-developer code | High | High | Assign code owners before touching flagged modules; write characterisation tests first |
| 4 rising hotspot files become more fragile during refactoring | High | High | Tier 1 testing treatment; full unit + integration coverage before any merge |
| Worst Performer file (health: 1.9) causes defect cascade | Medium | Very High | Dedicated review session; 90% coverage required; paired programming recommended |
| No PR Integration means health declines go undetected | High | Medium | Configure CodeScene PR Integration as a sprint-zero task for this initiative |
| Average code health continues declining below 6.8 | Medium | Medium | Track average health as a sprint metric; block merges that reduce it |

---

## 8. Success Metrics

The following metrics will be tracked at the end of each sprint to measure progress:

| Metric | Baseline (May 2026) | Target (end of initiative) |
|---|---|---|
| Hotspot Code Health | 4.8 | ≥ 6.0 |
| Average Code Health | 6.8 | ≥ 7.5 |
| Worst Performer | 1.9 | ≥ 3.0 |
| Unhealthy code (%) | 21% | ≤ 10% |
| Files with declining health | 14 | 0 |
| Unit test coverage on refactored files | Unknown | ≥ 80% (≥ 90% for red files) |
| Regression failures on merge | Not tracked | 0 |

---

## 9. Tooling Summary

| Tool | Purpose |
|---|---|
| CodeScene | Code health monitoring, hotspot identification, post-refactor quality gating |
| Existing unit test framework | Unit and integration test execution |
| CI pipeline | Automated test runs on every PR |
| CodeScene PR Integration *(to be configured)* | Automated delta analysis on each PR to catch health degradations before merge |

---

*This document should be reviewed at the start of each sprint and updated to reflect any changes in team composition, scope, or CodeScene findings.*
