# Week 2 — Tech Lead Role Explainer (Con Kirby)

**Purpose:** internal study aid documenting the **Tech Lead** work Con Kirby owned during **Sprint 2 (Week 2, Mon 25 – Fri 29 May 2026, Days 6–10)**. It captures *what* the role covered, *why* each technical decision was taken, *how* the worked examples and contracts were driven to completion, and the *decisions* owned — so the work can be defended to the iDaVIE panel.

> **Role note.** Per the standard rotation (§10.1), Con moved from Scrum Master (Sprint 1) to **Sub-team 6 Tech Lead** for Sprint 2. The TL responsibilities are: **technical decisions, the 09:15 cross-sub-team stand-up (technical side), and the Architecture Guild.** See [`week1-scrum-master-explainer.md`](week1-scrum-master-explainer.md) for the Sprint 1 hat.

> **AI-policy note.** This explainer was AI-assisted (reconstructing the role narrative from our own `week2-sprint-review.md`, `week2-sprint-retro.md`, `week2-kanban-snapshot.md`, `standups.md`, `architecture.md`, and `mvvm-binding-policy.md`) and is logged in `ai-log.md`. It is a **process/role study aid, not an individual reflection, peer-rating, or contribution log** — those remain human-authored per the brief, and AI may **not** be used for the live pitch/interview defence. Everything below must be defensible by Con on his own.

**Companion to:** `deliverables/D2-Architecture/architecture.md`, `D3-MVVM-binding-policy/mvvm-binding-policy.md`, `D4-worked-examples/ex1-file-tab/`, `D4-worked-examples/ex2-debug-tab/`, `Sprint-Documents/week2-sprint-review.md`, and `concern-map.puml`.

---

## 1. What the role covered (the inventory)

The Tech Lead carried the architecture-and-contracts spine of Sprint 2. Backlog cards / action items owned as TL:

| ID | Item | Status | Evidence |
|---|---|---|---|
| **WE1-4** | File-tab sequence diagram | Done | `ex1-file-tab/file-tab-sequence-diagram.puml` |
| **WE2-1** | Debug-tab BEFORE class diagram | Done | `ex2-debug-tab/before-class-diagram.puml` |
| **WE2-3** | Debug-tab AFTER sequence diagram (carried from Sprint 1) | Done | `uml-diagrams/after-debug-sequence-diagram.puml` |
| **WE2-4** | Debug-tab AFTER skeleton (carried from Sprint 1) | Done | `refactoring-examples/sub-team-6/debug-tab/skeleton/` |
| **ARCH-2** | Finalise ADR-0002 — author line + state contract | Done | `architecture.md` §4, §8 |
| **ARCH-9** | Resolve the 6 MVVM-binding-policy `_TODO_`s (action item A5) | Done | `mvvm-binding-policy.md` |
| **ARCH-11** | State contract delivered to Sub-team 7 (Day 9) | Done | `architecture.md` §8 |
| **ARCH-8** | Interface contracts confirmed with Sub-team 1 (Day 8, action item A8) | Done | `architecture.md` §6 integration log |
| **DESN-2** | concern-map text-source companion | Done | `concern-map.puml` |

Plus the daily TL work visible in `standups.md` Days 6–10: bringing the **File-tab worked example into scope**, **bringing Debug-tab up to par with File-tab**, working through the **sequence and phasing documentation**, finalising outstanding documentation, contributing to the **requirements document**, and organising the **mock interview** + Q&A material.

---

## 2. Why each technical decision was taken

**Core argument:** Sprint 2's goal was *"produce the design proposal in full."* The TL's job was to (a) close every open architectural `_TODO_` so the proposal has no soft edges, and (b) lock the two cross-team contracts so the design can't be invalidated by another sub-team's surface. Each decision below maps to one of those.

**The 6 MVVM-binding-policy decisions (A5):**
- **CommunityToolkit-Mvvm over hand-rolled `ViewModelBase`** — a maintained, tested library beats bespoke plumbing for a maintainability proposal; removes a class we'd otherwise have to test ourselves.
- **`RelayCommand` / `AsyncRelayCommand` shapes confirmed** — gives the View a uniform command surface and keeps async I/O (the file-open path) off the UI thread by contract.
- **`ObservableCollection<T>` over a ring buffer for the Debug tab** — simplest correct default; ring buffer kept explicitly as a Sprint-3 option if log-volume perf data warrants (decision is reversible, and recorded as such).
- **`IUIDispatcher` two-method contract (`RunOnUI`, `RunOnUIAsync`)** — the single seam that lets ViewModels marshal to the UI thread without referencing UnityEngine, which is what keeps the ViewModel assembly Unity-free (NFR-REU-3).
- **Lifecycle disposal owned by `FileTabViewModel.Dispose`** — pins ownership so subscriptions (e.g. the log stream) can't leak.
- **Forbidden-patterns table completed (8 rows) + NDepend CQLinq rule wording** — turns the policy from advice into a CI-enforceable gate.

**The 3 ADR-0002 open questions (resolved during Sprint 2):**
- **Handle vs path across the pipe → path string.** Simpler wire contract; no server-side handle lifecycle to manage in v1.
- **Header caching → `FitsService` owns the cache.** Keeps caching server-side, client stays stateless on FITS metadata.
- **Memory-check ownership → client-side guard retained in the adapter.** The memory probe stays in the ACL adapter, not the ViewModel, so the domain layer has no platform dependency.

**The two contracts:**
- **Interface contracts with Sub-team 1 (Day 8 integration review, A8)** — confirmed `IFitsService`, `IVolumeService`, `IFileDialogService`, `ILogStream` against their gateway surface; **no rework required**, closing the Sprint-1 R5 risk.
- **State contract to Sub-team 7 (Day 9)** — JSON, field catalogue per ViewModel, versioning tag `"schema_v": 1`; closes DEPS-4.

---

## 3. How the work was driven (mechanics)

1. **Brought the worked examples into scope and to parity.** File-tab example was tightened to our actual slice and its supporting docs aligned; Debug-tab was raised to the same artefact depth (BEFORE/AFTER class + dep + DSM + sequence + CK delta) so the two examples form a matched pair — File exercises request/response RPC, Debug exercises the server-pushed log stream.
2. **Authored the sequence diagrams** for the File-tab open-image happy path (with an `alt` error fragment for `FitsException`) and confirmed the Debug-tab Observer sequence.
3. **Worked the architecture `_TODO_`s to closure** in `architecture.md` and `mvvm-binding-policy.md`, then cross-checked the finalised `IUIDispatcher` shape back against the TEST-1 mock patterns.
4. **Ran the two integration touchpoints** — Day 8 (Sub-team 1) and Day 9 (Sub-team 7) — and recorded outcomes in the architecture integration log.
5. **Organised the mock interview** and contributed to the Q&A documents supporting the mid-assessment iDaVIE-team visit.

---

## 4. Sprint 1 → Sprint 2 action items owned as TL

| # | Action | Outcome |
|---|---|---|
| **A5** | Resolve 6 `_TODO_`s in MVVM binding policy | All 6 resolved (§2) |
| **A8** | Formal interface-contract hand-off to Sub-team 1 at integration review | Completed Day 8 — no rework |
| **A10** (→ Sprint 3) | Confirm artefact-freeze requirements ahead of pitch deadline | Carried as TL into Sprint 3 |

---

## 5. Decisions made (and owned) as TL

| Decision | Rationale |
|---|---|
| **Library (CommunityToolkit-Mvvm) over bespoke base class** | Less code we own and must test; standard, maintained surface. |
| **Path string over handle across the pipe** | Simplest correct v1 wire contract; defers handle-lifecycle complexity. |
| **Memory check stays in the ACL adapter, not the ViewModel** | Keeps the ViewModel assembly free of any platform/probe dependency (NFR-REU-3). |
| **`ObservableCollection<T>` now, ring buffer deferred** | Simplest default; reversal path documented for Sprint 3 if perf data demands it. |
| **Two worked examples held to matched depth** | Together they prove the transport contract has a real consumer on both the request/response and server-push paths. |
| **Close both cross-team contracts before proposal sign-off** | A design proposal that can be invalidated by another sub-team's surface isn't done; Day-8 and Day-9 reviews removed that risk. |

---

## 6. Risks carried into Sprint 3 (TL view)

1. **Post-refactor re-measurement (BNCH-1′/2′)** — CK deltas are projections; actual NDepend + SonarQube run on the refactored assembly owed in Sprint 3.
2. **`IInteractionService` + Sub-team 4** and **`ISourceCatalogueService` + Sub-team 5** — no contract agreed yet; flag at the Day-11 cross-sub-team window.
3. **Skeleton → full implementation** — 16 C# skeleton files are interfaces + stubs; promoting them is the heaviest remaining technical risk.
4. **ObservableCollection vs ring buffer** — revisit if Debug-tab log volume causes UI-thread pressure.

---

## 7. Likely panel questions (self-test)

- "What did the Tech Lead actually decide?" → §2/§5: the 6 MVVM-policy choices, the 3 ADR-0002 wire questions, and the two cross-team contract sign-offs.
- "How is the ViewModel kept free of Unity?" → §2: the `IUIDispatcher` two-method seam + memory check held in the ACL adapter, enforced by the NDepend CQLinq rule.
- "Why path-string, not a handle, across the pipe?" → §2: simplest correct v1; avoids server-side handle lifecycle.
- "Why are there two worked examples and not one?" → §3/§5: File = request/response RPC, Debug = server-pushed stream; the pair proves the transport contract on both paths.
- "Did the Sub-team 1 dependency cause rework?" → §2/§4: no — confirmed at the Day-8 integration review, closing Sprint-1 risk R5.
- "What's still open?" → §6: post-refactor re-measurement, two un-agreed interfaces (Sub-teams 4 & 5), and skeleton-to-implementation.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal role explainer for Con Kirby's Week 2 (Tech Lead) work. Not a deliverable itself, and not a substitute for the human-authored individual reflection / contribution log.*
