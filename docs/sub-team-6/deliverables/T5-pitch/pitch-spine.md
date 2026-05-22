# T5 — Pitch Spine (Sub-team 6 / Team Alpha — Desktop GUI & Client Shell)

> **Status:** working spine — drafted 2026-05-22 (Day 5). Not slides yet. This document defines, for every minute of the 40-min pitch, the **claim** we make, the **theory** that backs it, the **evidence** we point at, and the **risk** the panel could press on. Where evidence is missing we flag it `[EVIDENCE-GAP]` — those are the holes we backfill next.
>
> **Audience:** iDaVIE maintainer panel (Section 8.5). They will probe design decisions, not implementations. Section 10.5 #4 says **inability to defend a slide is a fail signal**; every speaker note below is written so any of the four of us can defend it cold.
>
> **Scope of our slot:** the assignment pitch is **team-wide** (40 min Thu 4 June 11:00–12:00). This spine is **only our slice**. The 4 / 10 / 12 / 6 / 5 / 3-min budget is the team-wide structure; our slice contributes to each section as flagged in the deliverables checklist §1.1. The 12 minutes of worked examples is the moment we are unambiguously on stage. We co-author the architecture, testability, trade-offs and summary sections with the other sub-teams.
>
> **Conventions in the spine:** every slide block lists the design driver (the *why*), not the operation (the *what*). When two patterns disagree we name both and say why we picked one. When the audience can interrupt and ask "but isn't this just X?" — the **Risk if challenged** field is the answer rehearsed.

---

## 0. Narrative arc (memorise this before reading slides)

The story is one sentence:

> *The desktop client today violates all four §4.2 non-negotiables because one class owns every concern; the MVVM split with a service gateway is the smallest change that makes each non-negotiable satisfiable, and the CK numbers prove it.*

Everything in the deck is in service of that sentence. Trim ruthlessly: if a slide does not advance "the four non-negotiables now hold," it does not belong.

The four non-negotiables (§4.2) are the spine of the spine:

1. No SOLID/GRASP violation without a documented trade-off.
2. Zero circular dependencies.
3. Domain code must not transitively depend on `UnityEngine` / `SteamVR`.
4. Every public API boundary is an interface with at least one test double.

We map every architectural choice back to one of these. The panel cannot disagree with the constraints — they wrote them.

---

## Section 1 — Pain points with metrics (4 min, ~3–4 slides)

**Section goal:** earn the right to propose anything by showing the *structural* unliveability of the current code. Not "ugly", not "long" — **structurally hostile to every quality attribute in ISO/IEC 25010 maintainability**.

**Our contribution to the team-wide pain section:** the **desktop client slice**. Other sub-teams own their own pain. We bring the worst single class in the repo.

---

### Slide 1.1 — Frame the audit, not the complaint (~45 sec)

**Claim:** We measured maintainability against **ISO/IEC 25010**, not taste. Five sub-characteristics, all five red on our slice.

**Why this matters:** the panel must reject any "the code is bad" handwave. The criteria for *bad* are: Modularity, Reusability, Analysability, Modifiability, Testability — and each one has a **measurable** acceptance test (NFR table in `docs/sub-team-6/requirements.md` §3).

**Theory anchor:**
- ISO/IEC 25010 — Maintainability is the product characteristic; the five sub-characteristics are the only legitimate language we have for *what is wrong*.
- LO1 (benchmark maintainability with CK + static analysis) and LO2 (elicit NFRs against ISO 25010) — the first two LOs of the course; framing in their language is non-negotiable.

**Evidence:**
- `docs/sub-team-6/requirements.md` §3 — the NFR table (NFR-MOD-1..3, NFR-REU-1..3, NFR-ANA-1..3, NFR-MOF-1..3, NFR-TST-1..3) covers all five sub-characteristics with tool-backed acceptance metrics.
- `docs/sub-team-6/deliverables/other/D9-ck-baseline/SK_BNCH.md` — the measurements.

**Speaker note:** open with "we audited eight classes in the desktop client against §7.1 CK thresholds and ISO/IEC 25010 maintainability sub-characteristics; this isn't an opinion piece". Cite the eight classes (CanvassDesktop, DesktopPaintController, PaintMenuController, VideoUiManager, HistogramMenuController, HistogramHelper, SourceRow, TabsManager) by name so the panel knows the scope is fixed.

**Risk if challenged — "why ISO 25010 and not ISO 9126?":** 9126 was withdrawn in 2011; 25010 is the current standard and is the one explicitly named in our requirements deliverable.

---

### Slide 1.2 — `CanvassDesktop.cs` is a textbook God Class (~75 sec)

**Claim:** A single `MonoBehaviour` of 1 899 lines owns FITS I/O, HDU parsing, histogram maths, colour maps, subset bounds, source mapping, paint-mode wiring, statistics, threshold controls, and configuration. That is not a class.

**Why this matters:** this single fact is what makes every quality attribute red. Until the class is split, no other change matters. The God Class is *not* a smell; it is the cause of every other smell on this slice.

**Theory anchor:**
- **SRP (Single Responsibility Principle).** "A class should have one reason to change." `CanvassDesktop` has at least nine. Robert Martin's framing — a class belongs to a single *actor*. Ours belongs to the file-loading actor, the rendering actor, the QA actor, the persistence actor and several others.
- **GRASP — High Cohesion / Low Coupling.** Henderson-Sellers LCOM of **0.955** is the operational proof that cohesion has collapsed: 63 methods touch 67 fields with only 189 total field-method accesses; methods operate on disjoint slices of the field set.
- **Fowler — Long Method, Large Class.** Refactoring catalogue.

**Evidence (real numbers, all from `SK_BNCH.md` and `SonarQube Baseline report.md`):**

| Metric | Measured | §7.1 threshold | Status |
|---|---|---|---|
| LOC | 1 899 | — | n/a |
| WMC | **63** | ≤ 40 (orchestrator) | violation +23 |
| CBO | **47** | ≤ 25 (orchestrator) | violation +22 |
| RFC | **118** | ≤ 50 | violation +68 |
| LCOM_HS | **0.955** | ≤ 0.50 | violation |
| Cyclomatic complexity (max method `checkSubsetBounds`) | **31** | ≤ 15 (SonarQube default) | violation |
| Dead fields | `_restFrequency`, `inPaintMode`, `_tabsManager` | 0 | violation |
| SonarQube maintainability rating | **D** | A | violation |

**Speaker note:** read the row that hurts most: "this one class is coupled to forty-seven other types — twenty-three project classes, thirteen Unity / TMPro types, seven System types, four Valve.VR types — and three of its own fields are declared but never accessed." Pause. The audience needs that pause.

**Risk if challenged — "isn't CBO=47 unfair on a Unity MonoBehaviour?":** the threshold for orchestrators is 25, not 14; we are already grading on a curve. Even the orchestrator curve is broken almost 2×.

---

### Slide 1.3 — The four non-negotiables already fail today (~60 sec)

**Claim:** Every architectural non-negotiable in §4.2 is *already* violated by the current desktop client. We are not improving good code; we are removing fail-state code.

**Why this matters:** §4.2 is the panel's contract with us. If we cannot point at four boxes and tick them in the after-state, we have failed the assignment regardless of how clever the design is.

**Theory anchor:** §4.2 is the assignment's encoding of:
- **DIP / SDP (Robert Martin — Stable Dependencies Principle).** Forbidding domain dependence on Unity is the Dependency-Inversion / Stable-Dependencies idea at the assembly level.
- **GRASP — Pure Fabrication + Indirection.** The reason interfaces with test doubles are mandatory is that they are the *seams* that make every other property testable.
- **Lakos — Levelisation.** Zero circular dependencies is a precondition for incremental build, parallel work, and reasoning.

**Evidence (current state vs §4.2):**

| §4.2 non-negotiable | Current state | Verdict |
|---|---|---|
| 1. No SOLID/GRASP violations | SRP, OCP, DIP all broken; LCOM 0.955 = cohesion collapsed | **FAIL** |
| 2. Zero circular dependencies | DV8 / NDepend not yet run end-to-end at slice level [EVIDENCE-GAP-1.3a]; `HistogramHelper → CanvassDesktop → HistogramMenuController → HistogramHelper` strongly suspected from SonarQube Rank 10 finding | **suspected fail** |
| 3. Domain code free of `UnityEngine`/`SteamVR` | `CanvassDesktop` directly imports `UnityEngine`, `TMPro`, `Valve.VR`; no domain isolation exists | **FAIL** |
| 4. Every public API has interface + test double | Zero interfaces on `CanvassDesktop` public surface | **FAIL** |

**Speaker note:** "three boxes are unambiguous fails. Box two is a fail pending DV8 — we will have the cycle report by Day 10. The point is not the audit; the point is that *no incremental fix* satisfies box three or box four. The only path is a split."

**Risk if challenged — "you don't have the cycle report":** acknowledge openly; commit to Day 10. The other three non-negotiables already justify the change.

**[EVIDENCE-GAP-1.3a]** DV8 / NDepend cycle report for our 8-class slice. Owner: Quality Champion. Due: Day 10 sprint review.

---

### Slide 1.4 — The cost is real, not theoretical (~60 sec)

**Claim:** Untestable god classes ship silent bugs. We found one. (`UpdateMaxValue` writes to `minVal`.)

**Why this matters:** the panel believes design proposals only when they connect to incident risk. We can point at a real, in-tree, currently-shipping data-corruption bug whose only protection would have been a unit test that the architecture forbids us from writing.

**Theory anchor:**
- **Beck — Test-Driven Development.** "If it's hard to test, the design is wrong." Inverting: an undesignable test surface produces this exact class of silent fault.
- **DIP.** A `MonoBehaviour` is not constructible outside the Unity engine; therefore no unit test can be written against it; therefore copy-paste bugs in its methods cannot be caught.

**Evidence:**
- `docs/sub-team-6/deliverables/other/D9-ck-baseline/SonarQube Baseline report.md` Rank 3 — `DesktopPaintController.UpdateMaxValue(float value) { minVal = value; }` at line 306.
- `SK_BNCH.md` page 7 — the bug is in a class with **WMC 57, CBO 21, LCOM 0.940**, and zero unit tests exist for it (NFR-TST-1 coverage = 0% on this class).

**Speaker note:** "the cost of being unable to write a unit test is in the tree, today. Line 306 of `DesktopPaintController`: a method called `UpdateMaxValue` assigns to `minVal`. The class is 1 558 lines; the bug is undetectable by reading. It would be detectable by any of the three unit tests we will write for the equivalent `FileTabViewModel`."

**Risk if challenged — "have you raised this with upstream?":** this is a design proposal, not a code change. We surface the bug as evidence of cost. Upstream issue tracking is out of our scope (§6.6).

---

## Section 2 — Target architecture, C4 levels 1–3 (10 min, ~7–8 slides)

**Section goal:** show that the four non-negotiables *force* a specific shape — we did not invent the shape, we read it off the constraints. The architecture is then a series of named pattern choices defended individually.

**Our contribution to the team-wide architecture section:** the **desktop client slice**. The team-level C4 is owned by Sub-team 1 (Architecture). Our slice plugs into it via the `IServiceGateway` contract.

---

### Slide 2.1 — C4 Level 1 (Context): the desktop client is one client of many (~60 sec)

**Claim:** The desktop client is **not** "the app". It is one front-end against a server kernel that the VR client, the (future) Python console, and (future) workspace persistence also consume.

**Why this matters:** this is the framing that justifies the **service gateway** boundary inside the client. If the desktop were the only client, the gateway would be over-engineering; because it is not, the gateway is the cheapest design.

**Theory anchor:**
- **Conway's law applied in reverse.** The team is split into seven sub-teams along the kernel/client/persistence axis (§5.5); the architecture must align with the team split or integration is impossible.
- **Hexagonal / Ports & Adapters (Cockburn).** The kernel is the inside; the desktop client, VR client, Python console are outside adapters. Our `IServiceGateway` is one such port.
- **Long-term roadmap drivers (§6.6 — Python console, workspace persistence).** Translated into requirements as ARQ-1 and ARQ-2 in `requirements.md` §4.

**Evidence:**
- `docs/sub-team-6/architecture.md` §3 — C4 Level 1 placeholder (PlantUML at `diagrams/c4-context.puml` [EVIDENCE-GAP-2.1a]).
- `docs/sub-team-6/requirements.md` §4 — ARQ-1, ARQ-2.
- `Assignment-Docs/iDaVIE_Refactoring_Assignment_FINAL_1.md` §4.1 — client–server style.

**Speaker note:** "the desktop tab is one client. The VR scene is another. The Python console will be a third. The persistence layer reads state from all three. Any design that hardcodes 'desktop' into the kernel breaks the assignment's roadmap (§6.6). The boundary between client and kernel is therefore a contract, not a method call."

**Risk if challenged — "isn't this just a sub-system, not a 'server'?":** in local mode, yes; in remote mode, no. ADR-0002 keeps the same `IServiceGateway` surface across both; the gateway *is* the abstraction over that distinction.

**[EVIDENCE-GAP-2.1a]** PlantUML C4 Level 1 diagram for our slice. Owner: TL. Backlog: ARCH-3.

---

### Slide 2.2 — C4 Level 2 (Container): three assemblies because dependency budgets differ (~90 sec)

**Claim:** The client decomposes into **three C# assemblies** — View, ViewModel, Gateway — because each must be allowed a different set of dependencies. This is not aesthetic separation; it is **mechanically enforced** by `.asmdef` references and **CI-checked** by NDepend CQLinq.

**Why this matters:** "MVVM" said in the abstract is a slogan. MVVM at the assembly boundary is a build error if you violate it. The panel will ask: how do you stop developers re-introducing `UnityEngine` in a ViewModel? Answer: they cannot — the assembly does not reference `UnityEngine`. The compiler refuses.

**Theory anchor:**
- **DIP (Dependency Inversion).** High-level (ViewModel) does not depend on low-level (View / Unity); both depend on abstractions (the interfaces in Gateway).
- **SDP (Stable Dependencies Principle, Martin).** Instability `I = Ce/(Ca+Ce)` must decrease as you move *toward* the domain. View is the most unstable (it changes the most often); ViewModel is more stable; the contracts are most stable.
- **CCP (Common Closure Principle).** Things that change together live together. UI Toolkit churn is in View; binding policy churn is in ViewModel; wire-format churn is in Gateway.

**Evidence:**

| Assembly | Allowed references | Forbidden references | CI rule |
|---|---|---|---|
| `iDaVIE.Client.View` | `UnityEngine`, `UnityEngine.UIElements`, `iDaVIE.Client.ViewModel` | server-side types, native P/Invoke | NDepend rule: no `DllImport` |
| `iDaVIE.Client.ViewModel` | `System.*` only | `UnityEngine`, `Valve.VR`, `System.Runtime.InteropServices` | NDepend CQLinq rule already drafted (`mvvm-binding-policy.md` §10.1) |
| `iDaVIE.Client.Gateway` | `System.*`, transport library | `UnityEngine` (Unity types belong to View only) | NDepend rule |

- ADR-0001 `Decision` section — the table above is the ADR.
- `mvvm-binding-policy.md` §10 — the CQLinq rule is drafted.
- `architecture.md` §10 — compliance check table (one row per §4.2 non-negotiable).

**Speaker note:** "this slide is the answer to *how do you prevent the team from re-creating the god class?* The compiler does it. The CI does it. There is no honour system."

**Risk if challenged — "isn't three assemblies overkill for a desktop tab refactor?":** the cost is three `.asmdef` files and one NDepend rule; the value is mechanical enforcement of §4.2.3. The dependency graph (after-state) shows zero `UnityEngine` reaching the ViewModel — this is the slide that proves it.

---

### Slide 2.3 — C4 Level 3 (Component): composition root + ACL + binder (~90 sec)

**Claim:** Inside each assembly, three named patterns do the load-bearing work: the **Composition Root** wires the graph at startup, the **Anti-Corruption Layer** lives in Gateway, the **Binder** marshals between View and ViewModel.

**Why this matters:** the panel will ask "where does `new` happen?" — the answer is "exactly once, in the composition root". This kills `FindObjectOfType<>`, kills static singletons, and makes every dependency explicit and mockable.

**Theory anchor:**
- **Composition Root (Mark Seemann, *Dependency Injection in .NET*).** The single place in the application where the object graph is composed. Everywhere else takes its dependencies via constructor injection.
- **Anti-Corruption Layer (Evans, *Domain-Driven Design*).** When two models meet at a boundary, an explicit translation layer prevents one model's vocabulary from polluting the other. Here: Unity vocabulary (`Vector3`, `GameObject`, `Coroutine`) does not enter ViewModel.
- **MVVM Binder (Gossman, originally Microsoft Avalon).** A glue layer the developer does not write — UI Toolkit's binding system in Unity 6, or a thin `UnityBinder<T>` shim in Unity 2021.3.

**Evidence:**
- `architecture.md` §6 — interface contracts list (`IServiceGateway`, `IFileTabViewModel`, `IDebugTabViewModel`, `ILogStream`, `ILogObserver`, `IPanel`).
- `adrs/0001-mvvm-split.md` `Decision` section — "composition root replaces all `FindObjectOfType<>` singleton lookups; dependencies are explicit and mockable".
- `mvvm-binding-policy.md` §5 — "Composition root owns instantiation".

**Speaker note:** "the composition root is a single `MonoBehaviour` in the View assembly. It calls `new FitsServiceAdapter()`, `new FileTabViewModel(fits, dialog, ...)`, attaches the ViewModel to the `UIDocument`'s `userData`, and is done. There are no static singletons. There are no scene-graph lookups. There is one `new` site per type, and the test suite can replace any of them with a fake."

**Risk if challenged — "what's the difference between an ACL and just a service interface?":** the ACL is the *translation* responsibility; the service interface is the *contract*. The ACL is what implements the translation (Unity `Vector3` → DTO `(float, float, float)`). Both exist; they sit at the same boundary; they have different jobs.

---

### Slide 2.4 — Transport: JSON-RPC over named pipes, gRPC later (~75 sec)

**Claim:** The transport is **JSON-RPC 2.0 over named pipes** for local mode (Day 1), with a path to gRPC over HTTP/2 for future remote streaming. The `IServiceGateway` interface is **transport-agnostic** — switching transports is a composition-root decision.

**Why this matters:** Section 6.6 explicitly names this transport choice. Defending it as ours-by-coincidence is weak; defending it as ours-by-reason is required.

**Theory anchor:**
- **OCP (Open–Closed).** The gateway is open for extension (new transport) and closed for modification (interface unchanged). Achieved by separating *contract* (`IServiceGateway`) from *adapter* (`JsonRpcPipeGateway` vs future `GrpcGateway`).
- **Protected Variations (GRASP).** The variation we anticipate is *the wire*. The variation point is the gateway interface.
- **Versioning via wireVersion (ADR-0002 §A.6).** Semver discipline at the protocol level; consumers refuse incompatible majors.

**Evidence:**
- `docs/sub-team-6/deliverables/D2-Architecture/client-server-transport.md` — full ADR.
- Appendix A — wire spec: pipe naming `\\.\pipe\idavie.<session-id>`, length-prefixed framing, method catalogue (`session.hello`, `file.open`, `file.close`, `dataset.getAxes`, `log.subscribe`, `log.emit`, `progress.update`), error model (`-32010`..`-32030`).
- Cross-link to ADR-0001 — the gateway's *placement* is in ADR-0001; its *wire* is in ADR-0002.

**Speaker note:** "the panel will likely ask: why not gRPC on Day 1? Answer: debuggability. A first-year cohort can `tail` a JSON pipe and see what is happening; they cannot easily `tail` a Protobuf stream. The interface is stable across the change; the cost of switching later is a new adapter, not a new design."

**Risk if challenged — "why not REST?":** in-process HTTP server adds a port-binding + auth concern and is strictly heavier than a pipe. JSON-RPC is the smallest envelope around our request/response and notification needs. Rejected and recorded in ADR-0002 Alternatives.

---

### Slide 2.5 — Mapping each §4.2 non-negotiable to its enforcement mechanism (~75 sec)

**Claim:** Every architectural non-negotiable has **one and exactly one** enforcement mechanism we can point at. Not policy; not "we'll review it"; a thing the build or a tool refuses to accept.

**Why this matters:** the panel hates aspirational diagrams. The slide that wins them is the one that converts §4.2 from "we promise" to "the CI refuses".

**Theory anchor:**
- **Fitness functions (Ford, *Building Evolutionary Architectures*).** Each architectural property is encoded as an automated test that fails the build if the property is violated.
- **Shift-left enforcement.** A rule that fires at PR time is worth ten rules in a review document.

**Evidence:**

| §4.2 non-negotiable | Enforcement mechanism | Tool | Owner | Status |
|---|---|---|---|---|
| 1. No SOLID/GRASP violation | CK thresholds + SOLID/GRASP audit in worked examples | Understand + manual audit | Quality Champion | baseline done, projection owed |
| 2. Zero circular dependencies | NDepend CQLinq `WarnIf cycle exists` | NDepend | Quality Guild | rule drafted [EVIDENCE-GAP-2.5a] |
| 3. Domain code free of Unity/SteamVR | NDepend CQLinq rule (drafted in `mvvm-binding-policy.md` §10.1) | NDepend | Quality Guild | rule drafted, not yet wired to CI [EVIDENCE-GAP-2.5b] |
| 4. Public API has interface + test double | Coverage gate + NDepend "public type without interface" rule | NDepend + SonarQube coverage | Quality Champion | rule pending [EVIDENCE-GAP-2.5c] |

**Speaker note:** "every row has a tool and an owner. The rules exist; the wiring to CI is owed by Day 10 (Sprint 2). This is the slide where we say: *we don't trust ourselves either*."

**Risk if challenged — "what if NDepend disagrees with DV8 on cycles?":** they measure differently (NDepend = static reference graph, DV8 = DSM with runtime annotations). Both must be green. Disagreements are surfaced in the Day 12 DV8 report and reconciled before pitch freeze.

**[EVIDENCE-GAP-2.5a / b / c]** NDepend rules wired into CI. Owner: Quality Guild. Due: Day 10.

---

### Slide 2.6 — What we *removed*: God-class concerns, redistributed (~60 sec)

**Claim:** The 1 899 lines of `CanvassDesktop` are not deleted — they are **redistributed** across five named units, each with one responsibility. This is the SRP audit (`architecture.md` §7).

**Why this matters:** the panel will ask "where did the code go?" A diagram that shows the old concerns mapped onto new units, with each arrow labelled by the *reason* for the move, is the answer.

**Theory anchor:**
- **SRP (Martin).** Each unit has one *actor*: the menu actor, the panel-state actor, the file-dialog actor, the configuration actor, the threshold-maths actor.
- **GRASP — Information Expert.** The class that *owns the data* owns the operation. Subset bounds maths moves to `SubsetBoundsViewModel` because the bounds *are* its state.
- **Concern-map convention** (Section 10.4 — must be text-based; the existing `concern-map.png` violates that rule [EVIDENCE-GAP-2.6a]).

**Evidence:**
- `architecture.md` §7 — SRP audit placeholder.
- `concern-map.png` exists (binary only — fix owed per Section 10.4).
- After-class diagrams: `D4-worked-examples/ex1-file-tab/after-class-diagram.puml`.

**Speaker note:** "the slide shows the old class as a centre node with nine concerns radiating out, and an arrow from each concern to its new home. Every arrow has a label: SRP, Information Expert, Indirection. No code is lost — what is lost is the *coincidence* of all nine concerns being in the same class."

**Risk if challenged — "did you measure this redistribution didn't just move the god class somewhere else?":** yes — `after-dsm.md` and the CK projection in §3 are the numerical answer. Show the table in Slide 3.4 / 4.4.

**[EVIDENCE-GAP-2.6a]** Convert `concern-map.png` to PlantUML / Mermaid source (Section 10.4 compliance). Owner: TL. Due: Day 7.

---

### Slide 2.7 — ADR stack on this slice (~60 sec)

**Claim:** Three named decisions, each defensible in isolation, compose to the architecture above. The stack is **ADR-0001 (MVVM split)** → **ADR-0002 (transport)** → **ADR-0003 (anti-corruption layer / Unity 6 UI Toolkit migration)** [EVIDENCE-GAP-2.7a].

**Why this matters:** the panel reads ADRs as the audit trail of decisions. Three is the recommended minimum per sub-team. Each ADR must list Alternatives Considered with rejection reasons — that is the signal of a defensible decision, not a chosen one.

**Theory anchor:**
- **Nygard's ADR convention.** Context → Decision → Consequences → Alternatives.
- **Reversibility (Fowler — irreversibility as the gate for ADR-grade decisions).** A decision that costs significantly to reverse is ADR-grade. MVVM split, transport, ACL — all three pass.

**Evidence:**
- `adrs/0001-mvvm-split.md` — Status proposed, with Alternatives section covering MVP, MVU, MVC, status quo, Reactive MVVM.
- `D2-Architecture/client-server-transport.md` — ADR-0002, Alternatives covering gRPC-on-Day-1, REST, in-process.
- ADR-0003 owed [EVIDENCE-GAP-2.7a].

**Speaker note:** "we did not write three ADRs because the assignment says three. We wrote three because three reversible decisions exist on our slice. We rejected MVP, MVU, MVC and Reactive MVVM by name with stated reasons in ADR-0001 Alternatives — that section is where the panel will direct most questions."

**Risk if challenged — "why is MVU rejected? Elm-style is cleaner.":** named in ADR-0001 Alternatives: mismatch with UI Toolkit's two-way binding primitives; unfamiliarity risk for a first-year team that must defend the pattern at interview. Defensibility under questioning was an explicit decision criterion.

**[EVIDENCE-GAP-2.7a]** ADR-0003 on Unity 6 UI Toolkit migration / ACL rationale. Owner: TL. Due: Day 8.

---

## Section 3 — Worked example 1: File tab (6 of 12 min, ~5 slides)

**Section goal:** convert everything in Section 2 from claims to a *diff*. The audience must be able to see, in two pictures, that the after-state is materially different from the before-state.

The pattern of each worked-example block: **before** (1 slide showing pain) → **after class diagram** (1 slide showing structure) → **after sequence diagram** (1 slide showing flow) → **CK delta table** (1 slide showing numbers) → **SOLID / GRASP audit** (1 slide showing theory).

---

### Slide 3.1 — File tab today: every `transform.Find` is a scene-renaming bomb (~60 sec)

**Claim:** "Open Cube" today is direct native-plugin call + `FindObjectOfType<>` + four-level `transform.Find("…/…/…/…")` chain — 30+ such chains in the class, each a silent `NullReferenceException` waiting for a scene rename.

**Why this matters:** the audience must feel the *brittleness* before the *solution* lands. A scene rename produces a runtime exception that no compiler caught. This is the cost of the god class made concrete.

**Theory anchor:**
- **Coupling type (Yourdon & Constantine).** Scene-path coupling is *content coupling* (the worst kind): the class depends on the internal name of another component's tree.
- **Connascence of Name (Page-Jones).** Stringly-typed scene paths are connascence-of-name at runtime — the strongest form of connascence, weakly observable.

**Evidence:**
- `SonarQube Baseline report.md` Rank 8: 30+ locations use chains like `renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport")…GetComponent<TMP_Dropdown>()`.
- `before-dsm.md` — CanvassDesktop depends directly on `VolumeCommandController`, `StandaloneFileBrowser`, `FitsReader`, `UnityEngine`.
- `before-class-diagram.puml`, `before-dependency-graph.puml` (already in tree).

**Speaker note:** "if any of those GameObjects is renamed in the editor, the compiler is silent and the runtime throws. The class is structurally vulnerable to a one-line UI edit — that is the cost of stringly-typed scene-graph coupling."

**Risk if challenged — "isn't this just a Unity convention?":** it is a Unity *anti-pattern*. The `[SerializeField]` mechanism exists precisely so editor references are compile-checked. The class chose not to use it.

---

### Slide 3.2 — After: `FileTabView` ↔ `FileTabViewModel` ↔ `IFileService` (~75 sec)

**Claim:** Three units replace the file-loading slice of the god class. The View binds to ViewModel properties and commands; the ViewModel calls a `IFileService` interface; the adapter behind the interface is the only place native code is invoked.

**Why this matters:** this is the moment the panel must be able to *see* the SRP split. A diagram with three boxes and labelled arrows is enough.

**Theory anchor:**
- **SRP.** `FileTabView` owns binding; `FileTabViewModel` owns command logic + validation; `FileServiceAdapter` owns the native call.
- **DIP.** ViewModel depends on `IFileService` (interface), not `FileServiceAdapter` (concrete).
- **GRASP Indirection.** The interface is the indirection that breaks the direct coupling.
- **MVVM (Gossman, Microsoft).** The View binds to ViewModel via property change + command dispatch, not method calls.

**Evidence:**
- `D4-worked-examples/ex1-file-tab/after-class-diagram.puml`.
- `D4-worked-examples/ex1-file-tab/after-dependency-graph.puml`.
- `mvvm-binding-policy.md` §9.1 — File tab walkthrough [EVIDENCE-GAP-3.2a, the `_TODO` in §9.1].

**Speaker note:** "the View knows about a ViewModel property called `SelectedDataset`. The ViewModel knows about an `IFileService` method called `OpenAsync(path)`. Neither knows that under the interface lies a P/Invoke call into a C plug-in. Each layer can be tested in isolation."

**Risk if challenged — "this is just plain dependency injection — what does MVVM add?":** MVVM is DI plus *binding semantics* (`INotifyPropertyChanged` + `ICommand`). DI alone would still require the View to imperatively pull from the ViewModel. The binding contract is what makes the View declarative.

**[EVIDENCE-GAP-3.2a]** `mvvm-binding-policy.md` §9.1 walkthrough fully written, citing skeleton file paths. Owner: TL. Due: Day 7.

---

### Slide 3.3 — After: command-driven sequence (~60 sec)

**Claim:** Sequence diagram of "user clicks Open → file loaded → cube visible" in the new architecture. Three actors (View, ViewModel, Gateway); each step labelled with the rule it satisfies.

**Why this matters:** static class diagrams do not show *time*. The panel must see that the new architecture has the same observable behaviour as the old, just through different bones.

**Theory anchor:**
- **Behaviour preservation (Fowler — refactoring definition).** External behaviour unchanged; internal structure changed.
- **Command-Query Separation (Meyer).** `Open` is a command; it returns nothing; its observable effect is a property change.
- **Asynchrony (Async/await).** Gateway calls are `Task`-returning; ViewModel exposes `IsBusy` so the View disables controls during the round-trip (`mvvm-binding-policy.md` §2.2).

**Evidence:**
- After-state sequence diagram [EVIDENCE-GAP-3.3a] (no `ex1-file-tab/after-sequence-diagram.puml` exists yet; the debug-tab equivalent does at `uml-diagrams/after-debug-sequence-diagram.puml`).
- `mvvm-binding-policy.md` §2 — command semantics.

**Speaker note:** "every arrow in the sequence is labelled with the rule it satisfies — DIP at the ViewModel → IFileService boundary, ACL at the IFileService → native boundary, INPC at the ViewModel → View notification. The diagram is the architecture, viewed in time."

**Risk if challenged — "why async if this is local?":** because the underlying call is filesystem + native parse, which can take seconds for a large FITS cube. Blocking the UI thread breaks NFR-TST-2 (no `Thread.Sleep` / synchronous wait in ViewModel) and also breaks user experience.

**[EVIDENCE-GAP-3.3a]** Before- and after-sequence diagrams for `ex1-file-tab/`. Owner: TL. Due: Day 8.

---

### Slide 3.4 — CK delta — the numbers (~75 sec)

**Claim:** Replace `CanvassDesktop` *for the file-tab slice* with three classes; the projected CK numbers all fall within §7.1 thresholds.

**Why this matters:** Section 7 of the assignment is unambiguous — "speculative numbers without evidence are not accepted". The projection must be supported by the skeleton code, not by hope.

**Theory anchor:**
- **CK suite (Chidamber & Kemerer 1994).** WMC / DIT / NOC / CBO / RFC / LCOM. The metrics are designed to detect god classes; they detect ours.
- **Threshold-vs-trend reading.** Thresholds for absolute pass/fail; trend (Day 2 → Day 13) for the proposal's value claim.
- **Henderson-Sellers LCOM.** Normalises across class size; the right LCOM variant for comparing classes of different LOC.

**Evidence (from `after-dsm.md` CK projection table):**

| Class | WMC before | WMC after | CBO before | CBO after | RFC before | RFC after | LCOM before | LCOM after | Threshold (domain) |
|---|---|---|---|---|---|---|---|---|---|
| CanvassDesktop (post-split, composition-root shell) | 63 | ~8 | 47 | ~4 | 118 | ~12 | 0.955 | ~0.30 | ≤ 40 (orchestrator) |
| FileTabViewModel | — | ~12 | — | ~5 | — | ~25 | — | ~0.25 | ≤ 20 |
| SubsetBoundsViewModel | — | ~8 | — | ~1 | — | ~15 | — | ~0.20 | ≤ 20 |
| FitsServiceAdapter | — | ~10 | — | ~6 | — | ~20 | — | ~0.30 | ≤ 40 (adapter) |

**Speaker note:** "the projection is evidenced by the skeleton code in `refactoring-examples/sub-team-6/file-tab/`. The numbers are not aspirational — they are countable from the skeleton. The full Understand re-run on the skeleton is owed by Day 13."

**Risk if challenged — "projected numbers, not measured":** acknowledge. Per §7, projection is acceptable if supported by skeleton code that can be re-measured. We commit to a re-measurement by Day 13 (D9 projection deliverable).

---

### Slide 3.5 — SOLID + GRASP audit on the file-tab slice (~60 sec)

**Claim:** Every SOLID principle and the relevant GRASP patterns are *named* in the after-state. We do not gesture at SOLID; we point at the class that exemplifies each letter.

**Why this matters:** LO4 ("apply SOLID + GRASP at class/component level") is a learning outcome. The panel will check that we can connect the theory to the artefact.

**Theory anchor:**

| Letter | Principle | Embodied by |
|---|---|---|
| **S** | Single Responsibility | `FileTabViewModel` (no maths, no I/O); `SubsetBoundsViewModel` (no UI, only validation); `FitsServiceAdapter` (no UI, only translation). |
| **O** | Open–Closed | `IFileService` is closed; new adapters (e.g. HDF5) implement the interface without changing the ViewModel. |
| **L** | Liskov Substitution | Any `IFileService` implementation can replace another without breaking `FileTabViewModel`. Test doubles depend on this. |
| **I** | Interface Segregation | `IFileService` and `IFileDialogService` are split; `FileTabViewModel` does not depend on dialog methods. Each interface ≤ 7 members (NFR-REU-2). |
| **D** | Dependency Inversion | `FileTabViewModel` depends on `IFileService`, not on `FitsServiceAdapter`. |

| GRASP | Embodied by |
|---|---|
| **Information Expert** | `SubsetBoundsViewModel` owns the bounds *data*; therefore owns the *validation*. |
| **Indirection** | `IFileService` is the indirection between ViewModel and native code. |
| **Protected Variations** | The variation we anticipate (transport, file format, dialog) is each behind a stable interface. |
| **Low Coupling / High Cohesion** | CBO drops 47 → ~4 on the composition-root shell; LCOM 0.955 → ~0.30. |
| **Polymorphism** | `IFileService` adapters dispatch on file type — no `if (extension == ...)` switch in the ViewModel. |
| **Pure Fabrication** | `FileTabViewModel` is a *fabricated* class with no real-world counterpart; it exists only to make the ViewModel layer testable. |

**Evidence:**
- `after-class-diagram.puml`.
- ADR-0001 references each letter in the Decision section.

**Speaker note:** "every SOLID letter and every GRASP pattern in scope is named with a class that embodies it. If the panel asks 'where is LSP exercised?', we point at the test double swap. The audit is not decorative — it is the bridge from theory to artefact."

**Risk if challenged — "isn't naming a principle for every class a bit forced?":** the assignment requires (LO4) that we can identify SOLID and GRASP at the class level. We do not claim every class embodies every principle; we claim each principle is embodied by *some* class in the after-state. The table is the proof.

---

## Section 4 — Worked example 2: Debug tab (6 of 12 min, ~5 slides)

**Section goal:** show a *different* pattern (Observer) under the same MVVM frame, to prove the architecture is general — not bespoke to "open a file".

The reason the assignment names *two* worked examples (§6.6) is so we cannot pass with only command-style flow. Observer is push, not pull; it stresses the threading model, the collection-binding model, and the lifecycle model in ways the File tab does not.

---

### Slide 4.1 — Debug tab today: IMGUI popup + static logger access (~60 sec)

**Claim:** Debug output today is legacy `OnGUI` IMGUI inside the same god class, reading from static logger access. There is no log *stream* — there is a log *snapshot* re-read every frame.

**Why this matters:** the architectural failure here is *the absence of an event surface*. Observer is impossible because there is nothing to observe.

**Theory anchor:**
- **Push vs pull (Hohpe & Woolf, Enterprise Integration Patterns).** Pull semantics force the consumer to poll; push allows the producer to fan out at its own rate.
- **Temporal coupling (anti-pattern).** Frame-driven polling couples the consumer to the rendering tick — wrong granularity for a log.

**Evidence:**
- `requirements.md` §2 table — Debug tab row: "tool-state read-out, OnGUI popup toggles, internal-state log".
- [EVIDENCE-GAP-4.1] — `ex2-debug-tab/before-class-diagram.puml` and `before-dependency-graph.puml` are owed (the equivalents for File tab exist). Owner: TL. Due: Day 8.

**Speaker note:** "the Debug tab is the worst panel in the slice because nothing about it is event-driven. The fix is not a button rewrite — it is *creating an event in the first place*."

**Risk if challenged — "what's wrong with OnGUI?":** it is deprecated in Unity 6's UI Toolkit world; it is not testable; it polls every frame regardless of whether log content changed. Three independent problems, all solved by Observer.

---

### Slide 4.2 — After: `ILogStream` → `DebugTabViewModel` (Observer) → `DebugTabView` (~75 sec)

**Claim:** A `ILogStream` interface publishes `LogEntry` events; `DebugTabViewModel` subscribes (Observer) and maintains a bound `ObservableCollection<LogEntry>`; the View shows the collection via `ListView` virtualisation.

**Why this matters:** Observer is one of the canonical Gang of Four patterns. Its application here is not novel — the panel needs to see that we *recognised* the pattern, not that we invented one.

**Theory anchor:**
- **Observer (GoF).** Subject (`ILogStream`) notifies registered Observers (`DebugTabViewModel`) of state changes. Decouples producer from consumer.
- **OCP.** New log sinks (file, network) can register without modifying `DebugTabViewModel`.
- **GRASP — Polymorphism.** Every observer implements the same interface; the stream does not know its observers' types.

**Evidence:**
- `refactoring-examples/sub-team-6/debug-tab/skeleton/ILogStream.cs` + `ILogObserver.cs` (exist).
- `refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabViewModel.cs` (exists; pure C#; subscribes to `ILogStream`).
- `uml-diagrams/after-debug-sequence-diagram.puml` (exists).

**Speaker note:** "Observer is the textbook fit. We did not pick it because it is fashionable — we picked it because the data flow is push and the producers can multiply (file sink, network sink) without the ViewModel changing. That is OCP in action."

**Risk if challenged — "why not use Rx / UniRx for this?":** named in ADR-0001 Alternatives — Rx is a compelling fit but adds a library dependency and a learning curve. The `ILogStream` event model is *forward-compatible* with an Rx migration later because the ViewModel surface does not change.

---

### Slide 4.3 — Threading: a producer thread is not a UI thread (~60 sec)

**Claim:** Log entries originate on background threads (server thread, native callback thread). The Observer pattern is correct; what makes it *safe* is the marshalling to the UI thread via `IUIDispatcher`.

**Why this matters:** this is the slide that proves we understand the *operational* cost of Observer, not just the diagram. The panel will probe threading because Unity's main-thread rule is non-negotiable.

**Theory anchor:**
- **Thread confinement (Goetz, *Java Concurrency in Practice*).** UI Toolkit (like every retained-mode UI) requires its tree to be mutated on a single thread.
- **Marshalling primitive.** Abstracted as `IUIDispatcher.Post(Action)` — testable; the production implementation lives in the View assembly, the ViewModel takes the interface (`mvvm-binding-policy.md` §4.2).
- **`ConfigureAwait(false)` discipline at Gateway boundaries** (`mvvm-binding-policy.md` §4.3) — every async hop is explicit.

**Evidence:**
- `mvvm-binding-policy.md` §4 — single-UI-thread rule, marshalling primitive, forbidden patterns (`UnityMainThreadDispatcher` static singleton).
- `mvvm-binding-policy.md` §3.2 — collection mutation rules (bulk loads atomic, UI thread only).

**Speaker note:** "the slide is one line of code that fails and one that passes. The failing line: `LogEntries.Add(entry)` from a background thread — Unity throws. The passing line: `dispatcher.Post(() => LogEntries.Add(entry))` — safe, testable because `IUIDispatcher` is an interface."

**Risk if challenged — "what if the dispatcher is slow under tracing rates?":** named as an open decision in `mvvm-binding-policy.md` §3.1 — vanilla `ObservableCollection` vs ring-buffer vs virtualised incremental collection. The pitch states the decision is owed; the choice is informed by load testing in Sprint 2.

---

### Slide 4.4 — CK delta — Debug tab numbers (~60 sec)

**Claim:** The Debug tab slice is *small* — and that is the point. After the split, `DebugTabViewModel` is a 30-line class with WMC ~4, CBO ~3, RFC ~10, LCOM ~0.20.

**Why this matters:** the File tab slide shows that CK numbers *drop*; the Debug tab slide shows that they *stay low* under a different pattern. Same architecture, different forces — same result.

**Theory anchor:**
- **Repeatability of metric improvement under the same architecture.** If the architecture is good, applying it to a different concern should also produce in-bounds CK numbers. If only the File tab improves, we got lucky; if both do, the architecture is the explanation.

**Evidence (projected from skeleton):**

| Class | WMC | CBO | RFC | LCOM | Threshold (domain) |
|---|---|---|---|---|---|
| DebugTabViewModel | ~4 | ~3 | ~10 | ~0.20 | ≤ 20 / ≤ 14 / ≤ 50 / ≤ 0.5 |
| ILogStream (interface) | n/a | n/a | n/a | n/a | ISP ≤ 7 members |
| LogStream (adapter) | ~6 | ~4 | ~15 | ~0.30 | ≤ 40 (adapter) |
| FakeDebugLogSource (test double) | ~3 | ~1 | ~5 | ~0.10 | n/a |

- Skeleton: `refactoring-examples/sub-team-6/debug-tab/skeleton/`.
- [EVIDENCE-GAP-4.4] — full Understand re-measure on the debug-tab skeleton owed by Day 13.

**Speaker note:** "every class in the after-state is small, focused, and within the §7.1 thresholds. The proof that the architecture is general is that two different patterns produce two different sets of small, focused classes."

**Risk if challenged — "30-line classes — over-decomposition?":** ISP and SRP both encourage many small interfaces and many small classes. The cost is one extra file per concern; the value is mockability. The trade-off is named in ADR-0001 Consequences ("Extra ceremony: three assemblies, interface boilerplate, `UnityBinder<T>` shim").

---

### Slide 4.5 — SOLID + GRASP audit on the debug-tab slice (~60 sec)

**Claim:** Different patterns, same principles. Observer adds two principles that File tab did not exercise: **OCP** at the stream surface (new sinks plug in) and **Polymorphism** at the observer surface (sinks have different types but conform to the same contract).

**Why this matters:** the panel can ask "did you really use two patterns or is this just the same example twice?" The audit shows the differential.

**Theory anchor:**

| Principle | File tab (3.5) | Debug tab |
|---|---|---|
| SRP | ViewModel / ViewModel / Adapter split | Stream / ViewModel / View split |
| OCP | `IFileService` adapters | `ILogStream` sinks — **the differentiator** |
| LSP | Test double swap on `IFileService` | Test double swap on `ILogStream` (`FakeDebugLogSource`) |
| ISP | `IFileService` + `IFileDialogService` split | `ILogStream` + `ILogObserver` split |
| DIP | ViewModel ↔ interfaces only | ViewModel ↔ interfaces only |
| Information Expert | `SubsetBoundsViewModel` owns bounds | `DebugTabViewModel` owns the bound log collection |
| Polymorphism | `IFileService.OpenAsync` dispatches by file type | **Multiple `ILogObserver` implementations**, dispatched uniformly |
| Indirection | `IFileService` | `ILogStream` |
| Observer pattern | n/a | **the defining pattern of this example** |

**Evidence:**
- `refactoring-examples/sub-team-6/debug-tab/skeleton/`.
- `mvvm-binding-policy.md` §9.2 — Debug tab walkthrough [EVIDENCE-GAP-4.5, the `_TODO`].

**Speaker note:** "two patterns, one architecture. The SOLID matrix shows the differential — OCP and Polymorphism do real work in Debug that File tab does not exercise. If we only had one worked example, we could not claim the architecture is general."

**Risk if challenged — "do you actually need *two* worked examples?":** the assignment requires it (§6.6). The deeper reason: a single example proves only that the pattern fits one problem. Two examples with different forces prove the architecture is not bespoke.

**[EVIDENCE-GAP-4.5]** `mvvm-binding-policy.md` §9.2 walkthrough fully written. Owner: TL. Due: Day 7.

---

## Section 5 — Testability + Unity 6 migration (6 min, ~4–5 slides)

**Section goal:** answer "what does this architecture buy you that the god class did not?" The answer in two words: *unit tests* (NFR-TST family) and *a migration path* (LO5).

**Our contribution to the team-wide section:** the **client-shell testability slice** and the **UI Toolkit migration path**. Other sub-teams own their own coverage and migration; we are responsible for the desktop client's portion of both.

---

### Slide 5.1 — Three test layers, each with a Unity dependency budget (~75 sec)

**Claim:** The split produces three test layers — **ViewModel unit tests (no Unity)**, **gateway unit tests with mock transport (no Unity)**, **View integration tests via UI Toolkit page-object pattern (Unity required)**. The first two satisfy the strict coverage target (NFR-TST-1: ≥ 70 % branch + line); the third is tracked, not gated.

**Why this matters:** the panel will ask "how do you actually unit-test Unity code?" The answer is: we don't. We unit-test the code that we engineered to *not be* Unity code.

**Theory anchor:**
- **Test pyramid (Cohn).** Many unit tests at the bottom (fast, no Unity); fewer integration tests in the middle (slow, Unity required); a thin smoke layer at the top.
- **Page Object (Fowler, *Page Object*).** A View-side abstraction with intent-named queries (`ClickOpenCubeButton()`, `EnterPath(string)`); the integration test does not couple to UXML structure.
- **Test double taxonomy (Meszaros).** We use *fakes* (`FakeFileService`) and *stubs* (`FakeDebugLogSource`); no mocks-as-spies in ViewModel tests.

**Evidence:**
- `test-strategy.md` §2 — layered test approach table.
- `D5-testing/viewmodel-unit-tests.md` — ViewModel test pattern.
- `D5-testing/ui-toolkit.md` — page-object pattern.

**Speaker note:** "the test pyramid for our slice has a fat base of ViewModel + gateway unit tests that need no Unity. The middle layer is page-object integration tests in the Unity Test Framework. The smoke layer is manual end-to-end runs. The coverage gate hits only the base — because that is where the design forces are."

**Risk if challenged — "why not Edit Mode tests in the Unity Test Framework instead?":** Edit Mode tests still require Unity to be installed. NUnit + Moq runs on any CI agent without a Unity licence. The cost of Unity in CI is real — licences, agent time. Avoiding Unity for the ViewModel layer is a deliberate cost decision.

---

### Slide 5.2 — Mocking surface = ISP × DIP (~60 sec)

**Claim:** Tests are cheap to write because the mocking surface is small. Every interface a ViewModel depends on has ≤ 7 members (NFR-REU-2 / ISP); every dependency is injected via constructor (DIP). The cost of a new unit test is one `Mock<I…>()` line.

**Why this matters:** the difference between "we have an MVVM design" and "we have a *testable* MVVM design" is whether the interfaces are sized to be moqued in one line. The §7.2 testability family encodes this.

**Theory anchor:**
- **ISP (Robert Martin).** "Clients should not be forced to depend on methods they do not use." Operationalised here as ≤ 7 public members per interface.
- **Mocking-difficulty count (Section 7.2).** A custom NDepend rule counts static / Unity API calls per class — zero is the target on ViewModels. Quantitative.

**Evidence:**
- `requirements.md` NFR-REU-2 (ISP ≤ 7), NFR-TST-2 (mocking difficulty = 0).
- `test-strategy.md` §7 — Interface-size audit (BNCH-7).
- `test-strategy.md` §8 — Mocking-difficulty count (BNCH-6).
- [EVIDENCE-GAP-5.2] — BNCH-6 and BNCH-7 audit tables owed.

**Speaker note:** "the proof that the design is testable is not 'we wrote unit tests'. The proof is 'the interfaces are small enough that any developer can write a test in five lines'. ISP and DIP, working together, are the operational testability lever."

**Risk if challenged — "is ≤ 7 a magic number?":** it is the threshold the assignment specifies (§7.2). The literature varies — 5 to 10 are all defensible. We picked the assignment number and made it a hard gate.

**[EVIDENCE-GAP-5.2]** BNCH-6 mocking-difficulty count + BNCH-7 ISP audit tables filled with real numbers. Owner: Quality Champion. Due: Day 9.

---

### Slide 5.3 — Worked test specification: `OpenCubeCommand` (~60 sec)

**Claim:** Three test cases that *would not exist* without the split. Happy path, error path, and concurrency path — all running in NUnit, no Unity.

**Why this matters:** the panel wants concreteness. A three-row test table whose rows make sense in plain English is the proof that the design lets us *describe* behaviours, not just observe them.

**Theory anchor:**
- **Given–When–Then (Behaviour-Driven Development).** Each test has a clear arrangement, action, and assertion. The interface boundary is what makes Arrange-Act-Assert mechanical.
- **Equivalence partitioning + boundary value analysis.** Three tests are three partitions of behaviour space, not three random calls.

**Evidence:**
- `test-strategy.md` §6:

```csharp
[Test] public void OpenCube_HappyPath_CallsGatewayWithExpectedArgs() { ... }
[Test] public void OpenCube_GatewayThrows_ShowsErrorState() { ... }
[Test] public void OpenCube_WhileLoading_IsIgnored() { ... }
```

**Speaker note:** "test 1 is the contract — the ViewModel calls the gateway with the right argument. Test 2 is the error path — the gateway throws, the ViewModel exposes the error state without throwing out of the command (`mvvm-binding-policy.md` §2.3). Test 3 is the concurrency contract — re-entering an in-flight command is a no-op (`IsBusy` gate). All three are NUnit; none touches Unity."

**Risk if challenged — "where is the implementation of these tests?":** the skeleton is owed in `refactoring-examples/sub-team-6/file-tab/code/` by Day 10. Pre-commit; defensible if challenged on the day.

---

### Slide 5.4 — Unity 5 → Unity 6 UI Toolkit migration: scoped, not heroic (~75 sec)

**Claim:** Migration from `UnityEngine.UI` Canvas (Unity 2021.3) to UI Toolkit (Unity 6) is scoped to **one assembly** (`iDaVIE.Client.View`). ViewModel and Gateway are unchanged because they contain no Unity types.

**Why this matters:** LO5 requires us to demonstrate Unity 5 → Unity 6 migration with UML, dependency graphs, and worked examples. The architecture is what makes migration tractable — without the split, every line of UI code would need to be ported atomically.

**Theory anchor:**
- **Bounded change (Fowler — *Refactoring*).** A change with bounded blast radius is migration; a change with unbounded blast radius is rewrite. The split converts a rewrite into a migration.
- **Strangler Fig pattern (Fowler).** Old and new Views can coexist behind the same ViewModel — incremental panel-by-panel migration is feasible.

**Evidence:**
- `architecture.md` §9 — Unity 5 → Unity 6 migration plan placeholder.
- ADR-0003 [EVIDENCE-GAP-2.7a] — UI Toolkit as View tech + migration plan.
- `D5-testing/ui-toolkit.md` — page-object pattern in UI Toolkit.

**Speaker note:** "the migration is panel-by-panel. We can port the File tab to UI Toolkit, leave the Render tab on Canvas, ship both in the same build. The ViewModel is identical across both. This is the strangler fig pattern with the architecture as its enabler."

**Risk if challenged — "Unity 6 still allows uGUI; why migrate at all?":** UI Toolkit is the strategic direction for Unity's UI system, and the assignment specifies Unity 6 with UI Toolkit explicitly (§6.6). The migration is mandated; the architecture makes it cheap.

---

### Slide 5.5 — Coverage targets and CI gates (~45 sec)

**Claim:** ViewModel ≥ 70 % branch + line (NFR-TST-1); overall ≥ 50 %; Unity-bound code tracked but not gated; cycle detection and "no Unity in ViewModel" rules block merge by Day 10.

**Why this matters:** the panel asks "how is this enforced?" — the answer is the CI gates. Numbers are aspirational without gates.

**Theory anchor:**
- **Quality gate (Kruchten — Architectural Decision tracking).** A gate is the explicit threshold below which a PR is rejected.
- **CI feedback loop (Humble & Farley, *Continuous Delivery*).** Fast feedback at PR time is structurally more effective than fortnightly review.

**Evidence:**
- `requirements.md` NFR-TST-1 to NFR-TST-3.
- `deliverables-checklist.md` §5.4 — CI gates owed by Day 10.

**Speaker note:** "by Day 10, a PR that introduces a circular dependency, a CK threshold breach, or a `UnityEngine` import in a ViewModel assembly is blocked from merge. The Quality Guild owns the wiring; we own the rules."

**Risk if challenged — "what about flaky coverage?":** branch + line, not statement; numerator/denominator both reported; the trend (Day 2 → Day 13) is what we defend, not the absolute on a noisy day.

---

## Section 6 — Trade-offs + risk (5 min, ~4 slides)

**Section goal:** the panel will *not* believe a clean story. The honest section beats the polished one. Name three things we paid for, three things we are afraid of, and the mitigation for each.

**Our contribution to the team-wide section:** the **desktop client slice's trade-offs**. Other sub-teams' trade-offs are not ours to defend.

---

### Slide 6.1 — Trade-off 1: MVVM ceremony cost (~75 sec)

**Claim:** Three assemblies, `INotifyPropertyChanged` boilerplate, `UnityBinder<T>` shim, composition root wiring — all of these are *cost we did not pay before*. We paid it because the alternative (the god class) cost more.

**Why this matters:** an architecture with no costs is suspicious. Naming the cost — and showing the mitigation — converts the cost into a deliberate choice.

**Theory anchor:**
- **Cost of abstraction.** Every interface is a runtime indirection and a maintenance cost; the trade is mockability and substitutability.
- **Source generators as boilerplate erasers (CommunityToolkit.Mvvm `[ObservableProperty]`).** Recognised in `mvvm-binding-policy.md` §1.1 as the decision-pending mitigation.

**Evidence:**
- ADR-0001 Consequences — "Extra ceremony: three assemblies, interface boilerplate, `UnityBinder<T>` shim. Mitigation: code skeletons in `refactoring-examples/sub-team-6/` serve as the canonical pattern."
- `mvvm-binding-policy.md` §1.1 `_TODO` — source-gen vs hand-rolled.

**Speaker note:** "the ceremony is a cost. We mitigate with skeletons (one canonical example, copied for new panels) and with source-gen (decision pending — trade-off recorded in §1.1)."

**Risk if challenged — "this is over-engineering for a desktop tab refactor":** if the desktop client were the only consumer, yes. The roadmap (ARQ-1 Python console; ARQ-2 workspace persistence) and the team split (VR client, server kernel) make the ceremony pay for itself in the second consumer.

---

### Slide 6.2 — Trade-off 2: ViewModel leak risk (~60 sec)

**Claim:** A developer can accidentally re-introduce `UnityEngine` into a ViewModel through a transitive reference if an `.asmdef` is misconfigured. The architecture's central rule could fail by misclick.

**Why this matters:** any rule that is enforceable only by review fails. We must show that the rule is enforceable mechanically.

**Theory anchor:**
- **Fitness function (Ford).** Architectural rules must be automated, not aspirational.
- **Defence in depth.** Three independent enforcement layers — `.asmdef` reference list, NDepend CQLinq rule, Roslyn analyzer — each catches a different class of misconfiguration.

**Evidence:**
- ADR-0001 Operational — "NDepend CQLinq rule added in T6 (Quality Guild sprint): forbids `UnityEngine.*` import inside `iDaVIE.Client.ViewModel`. PR check blocks merge if any ViewModel class has a direct or transitive reference to `UnityEngine`, `Valve.VR`, or `System.Runtime.InteropServices.DllImportAttribute`."
- `mvvm-binding-policy.md` §10.1 — CQLinq rule sketch.
- `mvvm-binding-policy.md` §10.2 — Roslyn analyzer.

**Speaker note:** "the rule has three layers: the `.asmdef` doesn't list `UnityEngine` (build error), NDepend warns on transitive imports (PR warning), Roslyn analyzer flags suspicious patterns (IDE warning). A single layer would be aspirational; three layers is engineering."

**Risk if challenged — "what if NDepend / Roslyn produces false positives?":** acknowledged. The PR-check rule is owed by Day 10; tuning against false positives happens in Sprint 2. The CQLinq rule sketch is in `mvvm-binding-policy.md` §10.1.

---

### Slide 6.3 — Trade-off 3: Sub-team 1 dependency (~45 sec)

**Claim:** The `IServiceGateway` contract is owned by Sub-team 1 (Apaties I — Architecture/Micro-kernel). Until they ship the interface surface, our File tab worked example consumes a **proposed** contract, not a delivered one.

**Why this matters:** the panel will look for inter-sub-team integration evidence. We must name our dependencies and our mitigation.

**Theory anchor:**
- **Contract-first design (Bertrand Meyer, *Object-Oriented Software Construction*).** Both sides of a contract can design against the contract without either side being implemented. Used here.
- **Stub-and-mock pattern.** Our `FakeFileService` consumes our `IFileService` interface; the real adapter lands when Sub-team 1's `IServiceGateway` is final.

**Evidence:**
- ADR-0001 — DEPS-1 recorded on integration risk register R01.
- `deliverables-checklist.md` §5.3 — "cross-sub-team integration review Day 8".

**Speaker note:** "we are blocked on nothing because the contract is the artefact. Sub-team 1's day 8 integration review is when their interface lands; ours conforms to it then. Until then, our skeleton consumes a fake."

**Risk if challenged — "what if Sub-team 1's interface differs from your assumption?":** acknowledged risk. ADR-0001 lists ARCH-8 (interface contracts proposed to Sub-team 1) as the seed; if their surface differs, we change our skeleton, not our architecture.

---

### Slide 6.4 — Risk: pitch defence + cohort buy-in (~45 sec)

**Claim:** The architecture is only good if a developer reading the after-state can produce a new panel without re-reading the ADR. We mitigate by skeletons (canonical example) and binding policy (rule sheet).

**Why this matters:** the panel sees a lot of architectures fail at the *team adoption* stage. We must show that we thought about it.

**Theory anchor:**
- **Cognitive load (Sweller).** A design with high intrinsic load (many concepts) needs low extraneous load (clear examples).
- **Conventions over configuration.** `mvvm-binding-policy.md` is the convention sheet; the skeletons are the convention exemplars.

**Evidence:**
- `mvvm-binding-policy.md` §8 — Forbidden patterns table (the convention sheet).
- `mvvm-binding-policy.md` §10.3 — PR checklist (reviewer-facing).
- Skeletons in `refactoring-examples/sub-team-6/`.

**Speaker note:** "a new developer writes a new tab by copying the file-tab skeleton, renaming the classes, and re-running the PR checklist. If the checklist passes, the binding policy is satisfied. If it does not, the rule that failed is named in plain English."

**Risk if challenged — "what if the team doesn't follow the policy?":** the NDepend / Roslyn rules block merge. Policy without mechanical enforcement is decoration; we have both.

---

## Section 7 — Summary (3 min, ~2–3 slides)

**Section goal:** restate the one sentence from §0 with the numbers and the names attached. Not new content — *resolved* content.

---

### Slide 7.1 — Before → after, four non-negotiables (~60 sec)

**Claim:** Every §4.2 non-negotiable failed in the before-state and passes in the after-state. Five sub-characteristics red → five green.

**Why this matters:** the close of the pitch is the close of the audit. Same table as Slide 1.3, with the right column flipped.

**Evidence:**

| §4.2 non-negotiable | Before | After (projected) |
|---|---|---|
| SOLID/GRASP | violated (SRP, OCP, DIP, LCOM) | satisfied with named audits (Slides 3.5, 4.5) |
| Zero cycles | suspected violation | zero — NDepend rule on every PR |
| No Unity in domain | direct `UnityEngine`, `Valve.VR` imports | ViewModel assembly contains no `UnityEngine` (NDepend rule) |
| Interface + test double on public API | zero interfaces | every public boundary an interface with ≥ 1 fake |

| ISO 25010 sub-char | Before (D rating) | After (target A) |
|---|---|---|
| Modularity | CBO 47, cycles | CBO ≤ 14 / 25, zero cycles |
| Reusability | no interfaces | ISP ≤ 7 on every boundary |
| Analysability | WMC 63, CC 31 | WMC ≤ 20 / 40, CC ≤ 15 |
| Modifiability | propagation cost high (DV8 owed) | ≥ 30 % drop (NFR-MOF-2) |
| Testability | 0 % coverage | ≥ 70 % branch + line on ViewModel |

**Speaker note:** "the close is the four boxes. They were red. They are now green. Every green is a number a tool produces, not a number we picked."

---

### Slide 7.2 — Forward look (~60 sec)

**Claim:** This architecture pays for itself in three places — gRPC remote mode (ADR-0002 forward-compat), Python console (ARQ-1), workspace persistence (ARQ-2). Each is a roadmap item §6.6 named explicitly.

**Why this matters:** the panel rewards architectures that *extend*. Naming three extensions, each already linked to a requirement, is the close.

**Evidence:**
- ADR-0002 §A.7 — gRPC `.proto` reuses method names and error codes; coordination with Sub-team 1.
- `requirements.md` ARQ-1 — Python console via non-Unity ViewModel.
- `requirements.md` ARQ-2 — workspace state via JSON-serialisable DTO.
- D13 state contract to Sub-team 7 — Day 9 exit criterion.

**Speaker note:** "the ViewModel layer is callable from a Python process today, in principle. The gateway is gRPC-ready today, in interface. The state is JSON-serialisable today, in DTO design. The roadmap is not 'we hope' — it is *what the architecture already allows*."

---

### Slide 7.3 — Named owners (~30 sec)

**Claim:** Every artefact in this pitch has a named author. Section 10.5 #4 is satisfied — every speaker can defend their slide cold.

**Why this matters:** the close ends with names. Section 8.4 interviews follow within 48 hours; any name on a slide is a name interviewed against that slide.

**Evidence:**
- `adrs/0001-mvvm-split.md` — author named.
- `D2-Architecture/client-server-transport.md` — author owed [EVIDENCE-GAP-7.3a].
- `mvvm-binding-policy.md` — author owed [EVIDENCE-GAP-7.3b].
- Pitch speaker assignments per slide owed [EVIDENCE-GAP-7.3c].

**[EVIDENCE-GAP-7.3a/b]** Author names on ADR-0002 and mvvm-binding-policy. Owner: TL. Due: Day 7.
**[EVIDENCE-GAP-7.3c]** Per-slide speaker assignment table. Owner: SM. Due: Day 12 (rehearsal day).

---

## Appendix A — Evidence gap punch-list (the holes this spine surfaces)

Ranked by pitch-day visibility. Each gap names an artefact, an owner, and a due date.

| # | Gap | Artefact | Owner | Due | Slide(s) |
|---|---|---|---|---|---|
| 1 | DV8 / NDepend cycle report on 8-class slice | `docs/sub-team-6/metrics/cycles-day10.md` | Quality Champion | Day 10 | 1.3, 7.1 |
| 2 | PlantUML C4 Level 1 diagram for our slice | `docs/sub-team-6/diagrams/c4-context.puml` | TL | Day 8 (ARCH-3) | 2.1 |
| 3 | C4 Level 2 + Level 3 PlantUML | `docs/sub-team-6/diagrams/c4-container.puml`, `c4-component.puml` | TL | Day 8 | 2.2, 2.3 |
| 4 | NDepend rules wired into CI (cycles, no-Unity-in-VM, no-static-singleton) | `tools/ndepend/rules.cqlinq` | Quality Guild | Day 10 | 2.5, 6.2 |
| 5 | Concern map text-source (replace `.png`) | `docs/sub-team-6/concern-map.puml` or `.mmd` | TL | Day 7 | 2.6 |
| 6 | ADR-0003 (ACL + Unity 6 UI Toolkit migration) | `docs/sub-team-6/adrs/0003-acl-uitk-migration.md` | TL | Day 8 | 2.7, 5.4 |
| 7 | `mvvm-binding-policy.md` §9.1 + §9.2 walkthroughs filled | `…/D3-MVVM-binding-policy/mvvm-binding-policy.md` | TL | Day 7 | 3.2, 4.5 |
| 8 | Before- and after-sequence diagrams for file-tab | `…/D4-worked-examples/ex1-file-tab/*-sequence-diagram.puml` | TL | Day 8 | 3.3 |
| 9 | Debug-tab before-state UML | `…/D4-worked-examples/ex2-debug-tab/before-*.puml` | TL | Day 8 | 4.1 |
| 10 | BNCH-6 mocking-difficulty + BNCH-7 ISP audit tables | `docs/sub-team-6/metrics/bnch-6.md`, `bnch-7.md` | Quality Champion | Day 9 | 5.2 |
| 11 | File-tab skeleton + 3 `OpenCubeCommand` unit tests | `refactoring-examples/sub-team-6/file-tab/code/` | Sub-team | Day 10 | 5.3 |
| 12 | Day-13 CK re-measurement on skeleton (Understand re-run) | `docs/sub-team-6/metrics/projection.md` | Quality Champion | Day 13 | 3.4, 4.4 |
| 13 | Author names on ADR-0002, mvvm-binding-policy | inline | TL | Day 7 | 7.3 |
| 14 | Per-slide speaker assignment + rehearsal plan | `docs/sub-team-6/deliverables/T5-pitch/speakers.md` | SM | Day 12 | 7.3 |

---

## Appendix B — Cross-references from this spine

- ADR-0001 — `docs/sub-team-6/adrs/0001-mvvm-split.md`
- ADR-0002 — `docs/sub-team-6/deliverables/D2-Architecture/client-server-transport.md`
- D1 Requirements — `docs/sub-team-6/requirements.md`
- D2 Architecture — `docs/sub-team-6/architecture.md`
- D3 MVVM Binding Policy — `docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md`
- D4 File-tab worked example — `docs/sub-team-6/deliverables/D4-worked-examples/ex1-file-tab/`
- D4 Debug-tab skeleton — `refactoring-examples/sub-team-6/debug-tab/skeleton/`
- D5 Test strategy — `docs/sub-team-6/test-strategy.md`
- D9 CK baseline — `docs/sub-team-6/deliverables/other/D9-ck-baseline/SK_BNCH.md`
- D9 SonarQube baseline — `docs/sub-team-6/deliverables/other/D9-ck-baseline/SonarQube Baseline report.md`
- Deliverables checklist — `docs/sub-team-6/deliverables/deliverables-checklist.md`
- Assignment spec — `Assignment-Docs/iDaVIE_Refactoring_Assignment_FINAL_1.md`
