# Team Alpha — Integration Overview

**Owner:** Integration Lead (Sub-team 1 / Architecture & Micro-kernel Core)
**Last updated:** 2026-06-04
**Source of truth:** this document is derived from the merged pull requests
(`gh pr list --state all`) and the two CI workflows in `.github/workflows/`.

## How to read this

This is the single page that ties together *what each sub-team shipped*, *how CI
gates it*, and *what is currently broken or outstanding*. It does not duplicate the
contract specs or the risk register — it links to them. Start at the
[contribution map](#1-sub-team-contribution-map), then check
[current CI status](#3-current-ci-status--blockers) for what is red and why.

---

## 1. Sub-team contribution map

| Sub-team | Behavioural area | Merged PRs | Primary location in repo |
|----------|------------------|-----------|--------------------------|
| **1 — Architecture / Micro-kernel** | Plug-in C ABI, micro-kernel core, Linux port | #1, #13, #14, #17 | `refactoring-examples/sub-team-1/`, `refactoring-examples/subteam-1/`, `MICROKERNEL_ARCHITECTURE.md`, `native_plugins_cmake/` |
| **2 — Data I/O** | FITS reading, AST/WCS transformation | #3, #9, #10 | `refactoring-examples/Sub-Team-2/` (`Fits Reader`, `WCS Transformation Without Unity`, `tests`) |
| **3 — Rendering / Docs** | Volume rendering examples, design docs, AI-usage log | #12, #21 | `refactoring-examples/team3/`, `docs/team3/` |
| **4 — Interaction** | Voice commands, gaze, locomotion, brush/cursor | #23 (merged); **#20 OPEN** | `Assets/Scripts/Interaction/`, `Assets/Scripts/VolumeData/Voice/`, `Team-4-examples/` |
| **5 — Feature & Domain models** | Feature system, domain models | #18 (merged); #16 closed (superseded) | `refactoring-examples/sub-team-5/` |
| **6 — GUI** | File/Debug tabs, service-gateway contracts, deliverable PDFs | #4, #5, #6, #22 | `refactoring-examples/sub-team-6/` |
| **7 — Persistence** | Session save/load, autosave, recovery, migrations | #19, #24 | `refactoring-examples/Persistence/`, `SUBTEAM.md` |

**Notes**

- **PR #20 (Team 4) is still OPEN.** It re-homes the Team-4 refactor into
  `Team-4-Examples/Sub-Team-4-refactored-examples/` and also touches live
  `Assets/Scripts/...`. It overlaps with already-merged #23 and deletes a large set
  of `refactoring-examples/sub-team-6/` and `handoffs/` files — **review for conflict
  before merging or closing.**
- Superseded / closed PRs (no action): #7, #8, #11, #16.

---

## 2. CI pipeline

Two workflows define a single unified pipeline (see
[§3](#3-current-ci-status--blockers) on why there are two). Job topology:

```
detect-changes ─────────────────────────────────────────────────────────────┐
     └── human-review-required  (PR only — intentional blocking gate)         │
build-unity ──────────────────────────────────────────────────────────────── ┤
     └── unity-tests                                                          │──── pr-comment
build-dotnet ──┬── arch-tests       ──────────────────────────────────────── ┤      (PR only)
(fail-fast)    ├── ck-metrics ──── sprint-metrics  (push/main only)           │
               ├── circular-deps   ──────────────────────────────────────── ─┤
               └── sonar           ──────────────────────────────────────── ─┤
diagram-validation  (warn-only, continue-on-error) ───────────────────────── ┤
document-checks     ──────────────────────────────────────────────────────── ┘
```

`ci.yaml` additionally defines a **`persistence-tests`** job (Team 7);
`team5ci.yaml` does not.

**Triggers (both):** `push: ["**"]` and `pull_request: [main]`.

**What each gate enforces**

| Job | Enforces |
|-----|----------|
| `detect-changes` | Classifies the diff (uml / docs / csharp / unity) for downstream jobs |
| `human-review-required` | **Intentional blocking gate** — fails on any `*.puml/*.mmd/*.drawio/*.md/*.pdf` in a PR diff; merge needs a human approval (Tech Lead / Quality Champion) |
| `build-dotnet` | Compiles the `refactoring-examples` / `arch-tests` C# (fail-fast for the metrics track) |
| `arch-tests` | Architectural fitness — forbids `UnityEngine.*` leaking into domain assemblies (Risk R05) |
| `circular-deps` | No circular dependencies between modules |
| `ck-metrics` / `sprint-metrics` | Chidamber–Kemerer metrics; sprint metrics published on push to `main` |
| `sonar` | SonarQube Cloud static analysis |
| `build-unity` / `unity-tests` | Unity build + EditMode/PlayMode tests |
| `persistence-tests` *(ci.yaml only)* | Team 7 persistence unit tests |
| `diagram-validation` | PlantUML/Mermaid syntax (warn-only, `continue-on-error`) |
| `document-checks` | Document presence/format checks |
| `pr-comment` | Posts an aggregated status comment on PRs (`if: always`) |

`basic-ci.yml` is a legacy placeholder that only triggers on the `team6` branch — now
effectively dead.

---

## 3. Current CI status & blockers

**Status: every recent run on `main` is `failure`** — both `ci.yaml` and `team5ci.yaml`,
including the post-merge runs on 2026-06-04 (`gh run list`). Root causes and recommended
actions:

| # | Blocker | Effect | Recommended action |
|---|---------|--------|--------------------|
| a | **Duplicate workflows** `ci.yaml` + `team5ci.yaml` | Every push runs two near-identical pipelines → double cost, double red X | Consolidate into one `ci.yaml` (keep the `persistence-tests` superset); delete `team5ci.yaml` |
| b | **Unset secrets** `UNITY_LICENSE/EMAIL/PASSWORD`, `SONAR_TOKEN/ORG` | `build-unity`, `unity-tests`, `sonar` fail | Add secrets in Settings → Secrets and variables → Actions |
| c | **`SONAR_PROJECT_KEY` placeholder** (`TODO-SONAR`) | `sonar` job misconfigured | Replace with the real SonarQube Cloud project key |
| d | **Dead `basic-ci.yml`** | Confusing; never runs on `main` | Remove it |
| e | **`human-review-required` gate** | Fails by design on any doc/diagram PR | Not a bug — keep; merge via human approval as intended |

> Until (a)–(c) are resolved, a green `main` is not achievable. (e) is expected
> behaviour and must remain.

---

## 4. Cross-team contracts & interfaces

Contracts are already specified — reference, do not recreate.

| Contract | Owner(s) | Spec file | Resolution note | Status |
|----------|----------|-----------|-----------------|--------|
| `RawVolumeData` (voxel handoff) | 2 → 3 | `state-contracts/data-io-state-contract.md` | `docs/integration/meeting-subteam2.md` | ✅ Resolved (2026-06-02) |
| `IGaze` (gaze provider) | 4 → 3 | `state-contracts/interaction-state-contract.md` | `docs/integration/meeting-subteam4.md` | ✅ Resolved (2026-06-02) |
| `ISessionPersistenceService` / `VolumeSessionState` | 7 ↔ 3 | `state-contracts/rendering-state-contract.md` | `docs/integration/meeting-subteam7.md` | ⏳ Awaiting sign-off |
| `IServiceGateway` / JSON-RPC ABI | 1 → 3,4,5,6 | `refactoring-examples/sub-team-6/contracts-team1/` (placeholder stub) | Risk register DEPS-1 / R01 | ⚠️ Stub — to be ratified by Sub-team 1 |
| Feature state | 5 | `state-contracts/feature-state-contract.md` | — | Drafted |
| GUI state | 6 | `state-contracts/gui-state-contract.md` | — | Drafted |

---

## 5. Integration risks

The live risk register is `docs/team-alpha/integration-risk-register.md` (rows R01–R07
and DEPS-1). Highest-severity open items relevant to current integration:

- **R01 / DEPS-1** — service-gateway contract not yet frozen; clients code against a
  placeholder stub (see [§4](#4-cross-team-contracts--interfaces)).
- **R02** — Plug-in C ABI semver not enforced in CI (no exports-list check yet).
- **R05** — `UnityEngine.*` leakage; mitigated by the `arch-tests` fitness function.
- **R06** — CK-metric / Sonar tooling not operational (mirrors CI blockers b/c above).

See the register for affected sub-teams, owners, and full mitigation status.

---

## 6. Outstanding integration actions

- [ ] **Resolve PR #20** — merge or close after conflict review against #23 and the
      deleted `sub-team-6` / `handoffs` files.
- [ ] **Consolidate CI** — single `ci.yaml`; delete `team5ci.yaml`.
- [ ] **Remove** dead `basic-ci.yml`.
- [ ] **Configure secrets** — Unity (`UNITY_LICENSE/EMAIL/PASSWORD`) and
      Sonar (`SONAR_TOKEN/ORG`); replace `TODO-SONAR` project key.
- [ ] **Enable branch protection** on `main` with required checks
      (`build-dotnet`, `arch-tests`, `ck-metrics`, `circular-deps`, `sonar`,
      `human-review-required`) per the workflow setup checklist.
- [ ] **Ratify the `IServiceGateway` ABI** (Sub-team 1) to retire the placeholder stub.
- [ ] **Add ABI exports-list CI check** (Risk R02).
