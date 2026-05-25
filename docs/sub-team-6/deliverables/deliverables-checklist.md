# Sub-team 6 (Die Boks / Team Alpha) — Total Deliverables Checklist

**Scope:** Desktop GUI and Client Shell (Section 6.6).
**Purpose:** Single authoritative list of every artefact, ceremony, and evidence item this sub-team owes against the assignment spec, with the team-level and per-student items we also feed into. Use this as the master tracker.

**Reading order:** Section 1 is everything that must be ready by the pitch on Thu 4 June 11:00. Sections 2 onward are everything else that exists in scope.

**Conventions:**
- `[ ]` = not started or in progress
- `[~]` = drafted, needs completion
- `[x]` = complete to a defensible standard
- `[R]` - For Review
- File pointers are relative to repo root.

---

## 1. Final-Days Deliverables (frozen Thu 4 June 11:00 — pitch start)

### 1.1 The Pitch itself (T5)
- [ ] **40-min live presentation** to the iDaVIE maintainer panel (Team Alpha slot: Thu 4 June 11:00–12:00, lecture theatre, not our workspace). (https://docs.google.com/presentation/d/1VInnKnJoL806wzDpulDFbLr45mpyG5KGsksA2Of4olI/edit?slide=id.p#slide=id.p)
- [ ] **20-min Q&A** — panel can direct questions at any named team member. Tech Leads and Architecture sub-team must be present. Sub-team 6 should also be present and named for our slice. (https://docs.google.com/document/d/1A3z_kfBT1E9uLr8vEefJQ4q9AmiOH4mw4iaSNQ2o5pY/edit?tab=t.jdmnunofissj)

- [ ] **Slide deck as PDF** (recording made for moderation).
- [ ] **Suggested structure (Appendix C) — our slice contributes to:**
  - [ ] 4 min — iDaVIE pain points with metrics *(our slice: `CanvassDesktop.cs` 1899 lines, transform.Find chains, FindObjectOfType coupling, no test coverage)*
  - [ ] 10 min — target architecture (C4 — context, container, component) *(our slice: client shell + MVVM + service gateway)*
  - [ ] 12 min — worked refactoring examples with before/after CK numbers *(File tab + Debug tab — likely our primary on-stage moment)*
  - [ ] 6 min — testability + Unity 6 migration plan *(ViewModel unit tests no-Unity + UI-Toolkit page-object pattern)*
  - [ ] 5 min — trade-offs + risk
  - [ ] 3 min — summary
- [ ] **Rehearsals** — internal team-level (no formal time allocated; teams "rehearse as they see fit").
- [ ] **Named speaker(s) per slide** — anyone on the slide must be able to defend it under Q&A (Section 10.5 #4: inability to explain is a fail signal).

### 1.2 The two worked refactoring examples (the centrepiece of our scope)

#### Example 1 — File tab: direct native-plugin call → ViewModel command via service gateway

**Before-state artefacts** *(target: `refactoring-examples/sub-team-6/file-tab/before/`)*
- [x] UML class diagram of the current file-load slice of `CanvassDesktop.cs` (PlantUML, source-controlled) (`docs/sub-team-6/deliverables/D4-worked-examples/ex1-file-tab/before-class-diagram.puml`)
- [ ] Dependency graph — transitive Unity/SteamVR/native dependencies visible
- [x] CK metrics table: WMC, DIT, NOC, CBO, RFC, LCOM (real numbers from Understand) — `docs/sub-team-6/deliverables/D9-ck-baseline/SK_BNCH.md` covers CanvassDesktop + 7 sibling classes (CanvassDesktop: WMC 63 🔴 / DIT 1 / NOC 0 / CBO 47 🔴 / RFC 118 🔴 / LCOM 0.955 🔴)
- [~] Code smells catalogue for the affected slice — SonarQube half done (`docs/sub-team-6/deliverables/D9-ck-baseline/SonarQube Baseline report.md` Top-10 smells + tech debt + maintainability rating); CodeScene churn/hotspots still owed
- [ ] Sequence diagram: "user clicks Open → file loaded → cube visible" (current path)

**After-state artefacts** *(target: `refactoring-examples/sub-team-6/file-tab/after/`)*
- [ ] UML class diagram: FileTabView + FileTabViewModel + IFileService gateway
- [ ] Dependency graph — proves no transitive `UnityEngine`/`SteamVR` dependency from the ViewModel (Section 4.2 #3)
- [ ] CK metrics table — projected, supported by the skeleton code (Section 7: "speculative numbers without evidence are not accepted")
- [ ] Sequence diagram: same scenario, new architecture
- [ ] SOLID/GRASP audit (S/O/L/I/D + Information Expert, Indirection, Protected Variations, Polymorphism …)

**Code skeleton** *(target: `refactoring-examples/sub-team-6/file-tab/code/`)*
- [ ] `FileTabView.cs` — Unity 6 UI Toolkit binding only, no domain logic
- [ ] `FileTabViewModel.cs` — pure C#, no UnityEngine import
- [ ] `IFileService.cs` interface — service gateway boundary
- [ ] `FakeFileService.cs` — test double (Section 4.2 #4: every public API has at least one test double)
- [ ] At least one ViewModel unit test runnable with no Unity

**Delta worksheet** — WMC/DIT/NOC/CBO/RFC/LCOM before → after with justification (Section 7.1 thresholds: ≤20/≤4/≤5/≤14/≤50/≤0.5 for domain)

#### Example 2 — Debug tab: Observer of a structured logging stream

**Before-state artefacts** *(target: `refactoring-examples/sub-team-6/debug-tab/before/`)*
- [ ] UML class diagram of the current debug tab path
- [ ] Dependency graph
- [ ] CK metrics (six metrics, real numbers)
- [ ] Sequence diagram — current path

**After-state artefacts** *(target: `refactoring-examples/sub-team-6/debug-tab/after/`)*
- [ ] UML: `IDebugLogSource` → `DebugTabViewModel` (Observer) → `DebugTabView`
- [ ] Dependency graph
- [ ] CK metrics — projected, evidenced
- [x] Sequence diagram — drafted at `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml`; matching before-state diagram still owed
- [ ] SOLID/GRASP audit

**Code skeleton** *(target: `refactoring-examples/sub-team-6/debug-tab/code/`)*
- [x] `IDebugLogSource.cs` interface — implemented as `ILogStream.cs` + `ILogObserver.cs` in `refactoring-examples/sub-team-6/debug-tab/skeleton/`
- [x] `DebugTabViewModel.cs` — Observer, pure C# (`refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabViewModel.cs`; verified Unity-free, subscribes to `ILogStream`)
- [ ] `DebugTabView.cs` — UI Toolkit binding
- [ ] `FakeDebugLogSource.cs` — test double (current `LogStream.cs` is the concrete producer, not a test fake)
- [ ] Unit test that subscribes ViewModel to fake source and asserts behaviour without Unity

**Delta worksheet** — same six CK metrics before → after.

#### Shared contracts *(target: `refactoring-examples/sub-team-6/contracts/`)*
- [ ] `IFileService.cs` — aligned with Sub-team 1 (Apaties I) service-gateway contract
- [ ] `IDebugLogSource.cs`
- [ ] `IViewModel` / `INotifyPropertyChanged` glue or equivalent binding contract
- [ ] Service-gateway entry interface — the client-side adapter onto JSON-RPC over named pipes (per ADR 0002)

### 1.3 Team-level deliverables we contribute to (T1–T8)

| # | Deliverable | Form | Due | Our contribution |
|---|---|---|---|---|
| **T1** [x] | GitHub fork with full assessment history | Repository | Continuous; frozen 4 Jun 11:00 | Our commits, ADRs, diagrams, examples | 
| **T2** [~] | Baseline maintainability benchmark report | PDF + raw tool exports | Day 2 (19 May) | [~] CK baseline (`D9-ck-baseline/SK_BNCH.md`) + SonarQube smells (`D9-ck-baseline/SonarQube Baseline report.md`) DONE for our 8 classes; NDepend-equivalent baseline DONE (`D9-ck-baseline/ndepend-equivalent-baseline.md` — derived, see methodology note); CodeScene churn/hotspots + DV8 still owed |
| **T3** [ ] | **Architecture overview** (C4 + ADR log + plug-in ABI spec) | PDF | **Day 10 (29 May)** | [ ] Client-shell C4 view (context/container/component); our ADRs feed into team ADR log |
| **T4** [ ] | Consolidated refactoring proposal report (max 60 pp excl. appendices) | PDF | Frozen 4 Jun 11:00; submitted 5 Jun 14:00–16:00 | [ ] Our chapter: client architecture, MVVM policy, both worked examples, CK deltas |
| **T5** [ ] | Pitch | Live + slide PDF | 4 Jun 11:00–12:00 | (Section 1.1 above) |
| **T6** [ ] | CI/CD pipeline + dashboard URL | GitHub Actions + URL | Operational by Day 3; final 4 Jun 11:00 | [ ] Our QC sits on Quality Guild; our slice must run through it cleanly |
| **T7** [ ] | Team Integration & Metrics Report (signed by all 7 Tech Leads) | PDF | Frozen 4 Jun 11:00 | [ ] Our TL signs; we supply desktop-shell metrics + integration evidence |
| **T8** [~] | AI tool usage log + reflection (max 8 pp; one team doc signed by all sub-teams) | PDF | Frozen 4 Jun 11:00 | [ ] Our `ai-tool-log/sub-team-6.md` consolidates into team doc |

---

## 2. Sub-team Deliverables (Section 9.2 + Section 6.6)

| # | Deliverable | Spec page target | File pointer | State |
|---|---|---|---|---|
| **D1** [ ] | Sub-team requirements document | 1–2 pp | `docs/sub-team-6/requirements.md` | [~] REQ-1 current behaviour partially filled; needs all five tabs (File, Render, Stats, Sources, Debug) + direct file I/O behaviour + long-term Python console + workspace state requirements |
| **D2** [ ] | Sub-team design document (Desktop client architecture) | 5–10 pp | `docs/sub-team-6/architecture.md` | [~] Exists; needs full build-out — MVVM split, View/ViewModel/Gateway layering, transport contract (JSON-RPC over named pipes local; gRPC future remote), composition root, panel composition strategy, anti-corruption layer around UnityEngine/SteamVR |
| **D3** [~] | MVVM binding policy | (standalone deliverable) | `docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md` | [~] Scaffold drafted 2026-05-21 — full section structure in place (property change, commands, collections, threading, wiring, lifecycle, DTOs, forbidden patterns, worked examples, CI enforcement, glossary); _TODO_ markers identify the operational decisions still owed (CommunityToolkit vs hand-rolled, `IUIDispatcher` shape, collection strategy for Debug tab, lifecycle ownership) |
| **D4** [ ] | Worked refactoring examples × 2 | (Section 1.2 above) | `refactoring-examples/sub-team-6/{file-tab,debug-tab,contracts}/` | [ ] Folders exist, empty |
| **D5** [ ] | Sub-team test strategy | 2–4 pp | `docs/sub-team-6/test-strategy.md` | [~] Exists; needs ViewModel unit tests (no Unity), UI-Toolkit page-object pattern for integration, mocking strategy for `IFileService`/`IDebugLogSource`, coverage targets (≥70% domain branch/line, ≥50% overall, Unity-bound tracked not strict), ISP ≤ 7 members, dependency isolation index, mocking-difficulty notes |
| **D6** [ ] | Kanban/Trello snapshots × 3 | end of each sprint | `docs/sub-team-6/kanban-snapshots/sprint-{1,2,3}.{md,png}` *(to create)* | [ ] Backlog files exist (`backlog.md`, `backlog.csv`); need dated end-of-sprint snapshots: Day 5 (Fri 22 May), Day 10 (Fri 29 May), Day 12 (Tue 2 Jun) |
| **D7** [ ] | Daily stand-up notes | single shared file | `docs/sub-team-6/standups.md` | [~] File exists; needs daily entry for every working day (15 days total). Note: stand-ups are async on interview days. |
| **D8** [~] | Sub-team AI tool usage log | continuous | `docs/sub-team-6/ai-log.md` | [~] File exists with 5 substantive entries (Days 2–3); continuous updates required — tool, model, prompt class, where helped, where failed, what human did instead |
| **D9** [~] | CK baseline + projected snapshot for our slice | feeds T2 + T4 + T7 | baseline: `docs/sub-team-6/deliverables/D9-ck-baseline/SK_BNCH.md` + `docs/sub-team-6/deliverables/D9-ck-baseline/SonarQube Baseline report.md`; projection: `docs/sub-team-6/metrics/projection.md` *(to create)* | [x] Day 2 CK baseline DONE (SK_BNCH covers 8 classes with WMC/DIT/NOC/CBO/RFC/LCOM); [ ] Day 13 projection still owed |
| **D10** [ ] | Concern map for our slice | 1 diagram | `docs/sub-team-6/concern-map.{puml,mmd}` *(to create alongside existing .png)* | [~] `concern-map.png` exists — **spec violation risk:** Section 10.4 forbids binary-only diagrams; needs text-based source (PlantUML / Mermaid / drawio XML) |
| **D11** [ ] | UML/SysML diagram set | text-based | `docs/sub-team-6/uml-diagrams/` | [~] Has `before-class-diagram.puml`, `after-debug-sequence-diagram.puml`; needs full set per Section 1.2 above + SysML BDD for our slice (Day 6 freeze item) + state-machine diagrams where applicable |
| **D12** [~] | ADRs for our scope | suggested ≥3 | `docs/sub-team-6/adrs/` | [~] `adrs/0001-mvvm-split.md` in place (moved from `D2-Architecture/mvvm-proposal.md` on 2026-05-21); ADR-0002 transport still lives at `deliverables/D2-Architecture/client-server-transport.md` and should be moved/renamed to `adrs/0002-transport.md` for consistency; recommend at least one more ADR (e.g. panel composition / composition-root pattern / View-toolkit choice / anti-corruption layer rationale) |
| **D13** [ ] | **State contract to Persistence** | small interface doc | `refactoring-examples/sub-team-6/contracts/desktop-state-contract.md` *(to create)* | [ ] Section 8.2 Day 9 exit criterion: each sub-team declares what its state looks like for the Persistence aggregate. Hands to Sewe en sestig (Team Alpha Persistence). |
| **D14** [ ] | Trade-off analysis (our slice's contribution) | feeds T4 + pitch | `docs/sub-team-6/trade-offs.md` *(to create)* | [ ] Section 8.3 Day 11 work product; 5-min pitch slot |

### Architectural non-negotiables our deliverables must demonstrate (Section 4.2)
- [ ] No SOLID/GRASP violations — or documented trade-off
- [ ] **Zero circular dependencies** in our slice (package and assembly level)
- [ ] **Domain code must not transitively depend on `UnityEngine` or `SteamVR`** — proven in after-state dependency graphs
- [ ] Every public API boundary expressed as an interface with at least one test double
- [ ] (Plug-in ABI semver — Sub-team 1 owns; our gateway consumes it correctly)

### CK thresholds (Section 7.1) — our after-state must respect
- [ ] WMC ≤ 20 (domain) / ≤ 40 (adapters)
- [ ] DIT ≤ 4
- [ ] NOC ≤ 5
- [ ] CBO ≤ 14 (domain) / ≤ 25 (orchestrators), no cycles
- [ ] RFC ≤ 50
- [ ] LCOM ≤ 0.5
- [ ] ISP — interface size ≤ 7 public members

### Coverage targets (Section 7.2)
- [ ] ≥ 70 % branch/line on domain code
- [ ] ≥ 50 % overall
- [ ] Unity-bound code tracked but not in strict target

---

## 3. Per-Student Deliverables (Section 9.3) — × 4 students in our sub-team

**For each of the 4 of us:**
- [ ] **Two-page individual reflection** — personal sub-team contributions; two software-engineering lessons learned; explicit description of AI tool use, helps, and misses. **Due Fri 5 June by Brightspace deadline.** *AI may NOT be used to author this (Section 10.5 #6).*
- [ ] **Peer-rated contribution table** — submitted privately via Brightspace. **Due Fri 5 June by Brightspace deadline.** *AI may NOT be used (Section 10.5 #6).*

**For each of the 4 of us — individual interview (Section 8.4):**
- [ ] 15 min defence-relevant interview time per student (within a 60-min slot shared with sub-team)
- [ ] Each student must be able to defend any decision, any code, any diagram in our chapter and shared team artefacts
- [ ] Interviews distributed Wed 3 Jun (×6) / Thu 4 Jun (×4) / Fri 5 Jun (×4) — single-track, consecutive
- [ ] AI may NOT be used in interview defence

---

## 4. Process Artefacts & Ceremonies (Sections 10.1, 10.2)

### 4.1 Sub-team role rotation evidence — visible in standups + Kanban
Standard 4-role rotation across 3 sprints; each of the 4 of us holds 3 of 4 roles.

- [ ] **Sprint 1 (Days 1–5):** CK=SM, MG=TL, JF=POL, RH=QC
- [ ] **Sprint 2 (Days 6–10):** A=QC, B=SM, C=TL, D=POL
- [ ] **Sprint 3 (Days 11–13):** A=POL, B=QC, C=SM, D=TL

Each role has weekly evidentiary obligations:
- **Sub-team Scrum Master:** runs daily 09:00 stand-up (08:55 interview days); removes impediments; maintains sub-team Kanban column
- **Sub-team Tech Lead:** technical decisions in sub-team; attends 09:15 cross-sub-team stand-up; sits on Architecture Guild; signs T7
- **Sub-team PO Liaison:** translates Team PO priorities into sub-team backlog
- **Sub-team Quality Champion:** metrics, CI status, baseline benchmark, worked-refactoring evidence; sits on Quality Guild (daily 10:30 huddle + Friday metrics review)

*If any of the 4 of us is elevated to Team PO or Team SM in a sprint, they are seconded out for 5 days and our sub-team operates at 3 members that sprint.*

### 4.2 Scrum ceremonies (each is evidence — write notes into standups.md or sprint folders)
| Ceremony | When | Notes |
|---|---|---|
| [ ] Sub-team daily stand-up | Daily 09:00, 10 min | Async on interview days |
| [ ] Cross-sub-team stand-up | Daily 09:15, 15 min | TL attends |
| [ ] Quality Guild huddle | Daily 10:30, 15 min | QC attends |
| [x] Sprint 1 planning | Day 2 (Tue 19 May) | 2h sub-team + 1h team |
| [ ] Sprint 2 planning | Day 6 (Mon 25 May) | 2h sub-team + 1h team |
| [ ] Sprint 1 review | Day 5 (Fri 22 May) | 15 min per sub-team |
| [ ] Sprint 2 review | Day 10 (Fri 29 May) | + Mid-assessment iDaVIE-team visit (30 min) |
| [ ] Sprint 1 retro | Day 5 | 1h sub-team + 30 min team |
| [ ] Sprint 2 retro | Day 10 | 1h sub-team + 30 min team |
| [ ] Sprint 3 retro | Day 12 (Tue 2 Jun) 16:00–17:00 | **Sub-team only — no team-wide retro for Sprint 3** |
| [x] [ ] Architecture Guild review | Wednesdays 1h | TL attends |
| [x] [ ] Quality Guild metrics review | Fridays 1h before sprint review | QC attends |

### 4.3 Mid-assessment iDaVIE-team visit
- [ ] **Day 10 (Fri 29 May), 30 min per team** — present current state to iDaVIE maintainer panel; receive feedback to be addressed Day 11

---

## 5. Cross-Cutting Obligations Sub-team 6 Contributes To

### 5.1 Architecture Guild (our Tech Lead's seat)
- [ ] Daily 09:15 stand-up attendance (08:55 on interview days)
- [ ] Weekly 1-hour review on Wednesdays
- [ ] Co-signs T7 (Team Integration & Metrics Report)
- [ ] Our ADRs feed into team ADR log (T3)

### 5.2 Quality Guild (our Quality Champion's seat)
- [ ] Daily 10:30 huddle attendance
- [ ] Weekly 1-hour metrics review on Fridays
- [ ] Co-owns CI/CD pipeline + dashboard (T6)
- [ ] Co-owns Team Integration & Metrics Report (T7)
- [ ] Our slice contributes to: SonarQube Cloud, Understand CK reports, NDepend rules, CodeScene hotspots, DV8 DSM

### 5.3 Inter-sub-team integration contracts
- [ ] **Sub-team 1 (Apaties I — Architecture/Micro-kernel):** service gateway contract — our File-tab example consumes it; signed off in cross-sub-team integration review Day 8
- [ ] **Sub-team 4 (Koffiewinkel — Interaction System):** VR-side menus share surface area with our desktop menus; coordinate panel/menu boundary
- [ ] **Sub-team 7 (Sewe en sestig — Persistence):** **state contract** — we declare what our desktop-shell state looks like (Day 9 exit criterion)

### 5.4 CI quality gates — our slice must pass
- [ ] By **end of Day 1**: CI pipeline compiles + runs static analysis on one file
- [ ] By **end of Sprint 1 (Day 5)**: full metric suite on every PR + dashboard delta as PR comment
- [ ] By **Day 10**: PR introducing architecture violation OR circular dependency OR CK threshold breach is **blocked from merge** — our after-state code must satisfy these gates

---

## 6. Format / Compliance Constraints

### 6.1 Modelling notation (Section 10.4)
- [ ] UML default for class, component, sequence, state-machine diagrams
- [ ] SysML for requirements diagrams, BDDs, parametric diagrams where appropriate
- [ ] All diagrams **text-based and source-controlled** (PlantUML, Mermaid, .drawio XML) — **no binary-only diagrams**
- [ ] **Audit:** `concern-map.png` lacks a text-based source — add `.puml` or `.mmd` companion

### 6.2 AI / GenAI policy (Section 10.5)
- [ ] AI usage log maintained continuously: tool, model, prompt class, where helped, where failed, what human did instead
- [ ] No verbatim AI output passed off as human-authored prose in final report
- [ ] Every AI-generated artefact reviewed, understood, defensible by a named human author
- [ ] AI **NOT** used for: peer-rating, contribution log, individual reflection, live pitch/interview defence
- [ ] AI **MAY** be used for: requirements drafting, ADR drafting, code skeletons, test generation, refactoring proposals, diagram generation, metric interpretation, prose editing

### 6.3 Mandated tools touching our slice (Section 7.3 + Appendix B)
- [ ] **SonarQube Cloud** — code smells, complexity, duplication, maintainability rating, technical debt, coverage (every PR + nightly)
- [ ] **Understand** — CK suite + structural metrics for our slice (sprint boundary snapshots)
- [x] **NDepend** — CQLinq architecture rules, instability, propagation cost — baseline derived in `D9-ck-baseline/ndepend-equivalent-baseline.md` (NDepend requires compiled Unity assemblies; metrics derived from Understand CK + source inspection per methodology note in that doc)
- [ ] **CodeScene** — hotspots, churn, knowledge map, code health (daily; full report Day 2 and Day 12)
- [ ] **DV8** — DSM, propagation cost, architectural anti-patterns (end of each sprint)
- [x] **GitHub Actions** — CI orchestration (continuous)

---

## 7. Learning-Outcome & SWEBOK Coverage Check (sanity)

Mapping each spec LO to where our slice provides evidence — if any cell is empty, we have a gap.

| LO | Statement | Our evidence |
|---|---|---|
| LO1 | Benchmark maintainability with CK + static analysis | D9 baseline + projection; T2 contribution |
| LO2 | Elicit + document NFRs against ISO/IEC 25010 | D1 requirements |
| LO3 | Client–server + micro-kernel + layered + plug-in | D2 architecture; ADR 0002 transport |
| LO4 | SOLID + GRASP at class/component | SOLID/GRASP audits in D4 worked examples |
| LO5 | Refactor Unity 5 → Unity 6 with UML, dependency graphs, worked examples | D4 worked examples; D11 UML diagram set |
| LO6 | Testability strategy, coverage, isolation, mocking, CI gates | D5 test strategy + ViewModel unit tests in D4 |
| LO7 | Scrum-of-Scrums sub-team operation | D6 Kanban snapshots, D7 standups, role rotation evidence |
| LO8 | AI tools effective + responsible, defensible, identify failures | D8 AI log; per-student reflections |
| LO9 | Communicate + defend at pitch + interviews | Pitch (Section 1.1); individual interviews (Section 3) |

---

## 8. Ranked punch list — by pitch-day visibility

1. **Two worked examples** (File + Debug) — UML before/after, dependency graphs, CK deltas, SOLID/GRASP audit, skeleton C# in `/refactoring-examples/sub-team-6/`
2. **Pitch slides** — our 12-min worked-examples slot + co-authored architecture/testability slots
3. **Design doc D2 + MVVM binding policy D3** — fully written
4. **Requirements doc D1** — completed past current REQ-1 draft
5. **Test strategy D5** — fully written, with coverage targets and ISP/mocking notes
6. **CK baseline + projection D9** — real numbers, evidenced by the skeleton code
7. **ADRs ×3** — fill rationale stack (current: MVVM split, transport; add at least one more)
8. **Daily standups D7 + Kanban snapshots D6 ×3 + AI log D8** — continuous, evidentiary
9. **State contract D13 to Persistence** — small but mandatory exit criterion
10. **Trade-off analysis D14** — both written and pitch slot
11. **Concern map text-source D10** — fix spec-compliance gap on `concern-map.png`
12. **4 × individual reflections + 4 × peer-rated tables** — per-student, AI-free, Brightspace
13. **Contributions to T2/T3/T4/T6/T7/T8** — our chapter and signatures into team docs
14. **Interview prep** — each of the 4 of us defensible on every named decision

---

## 9. Scope Discipline — What Is NOT Ours

So we don't over-build:
- Plug-in C ABI itself (Sub-team 1 — we consume only)
- CI/CD pipeline machinery (Quality Guild collective; we use it)
- Baseline benchmark for code outside our slice
- Server-side of JSON-RPC transport (we own the client adapter only)
- VR interaction state machines (Sub-team 4)
- Rendering layer (Sub-team 3)
- FITS / WCS plug-ins (Sub-team 2)
- Feature domain (Sub-team 5)
- Persistence aggregate (Sub-team 7 — we only hand them a state-shape contract)
