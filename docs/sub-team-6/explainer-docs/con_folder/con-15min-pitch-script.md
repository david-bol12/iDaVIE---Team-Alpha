# Con's 15-Minute Pitch & Defence Script (Con Kirby)

> **What this is.** A first-person rehearsal script for the ~15 minutes I personally carry on pitch day (Thu 4 June 2026): my spoken delivery of the **File-tab worked example** plus my rehearsed answers to the questions most likely aimed at me. Drill it until I can say it without the page.
>
> **AI-policy note.** Like the role explainers in this folder, this is an **AI-assisted study aid**, logged in `ai-log.md`. AI may **not** be used in the live pitch/interview defence (§10.5 #6). I must internalise this and be able to defend every number and claim on my own — do **not** read it off a screen on the day.
>
> **Numbers source of truth.** Every CK figure below is the **tool-verified Day-13 Understand export**, canonical in `refactoring-examples/sub-team-6/file-tab/ck-metrics.md`, and matches the reconciled `pitch-spine.md` / `qa-practice.md`.
>
> ⚠️ **My own older docs are stale — do not rehearse from them.** `week2-tech-lead-explainer.md` and `week1-scrum-master-explainer.md` (and `week2-sprint-review.md`) still quote the **superseded** set: `CBO 47`, `RFC 118`, `WMC 63→~8 / CBO 47→~4`, `LCOM 0.955→~0.12`, `FileTabViewModel WMC ~12 / CBO ~5`, and the "**path-string over handle**" wire decision. Those are all wrong now. If I quote any of them on stage, a panelist flipping to `ck-metrics.md` will catch the contradiction (a §10.5 #4 fail signal). The correct numbers and the correct wire design are below.

---

## Timing map (≈15 min of me talking)

| # | Segment | Target |
|---|---|---|
| 1 | Who I am / what I owned | 1.5 min |
| 2 | File tab today — the pain | 2.0 min |
| 3 | File tab after — the structure | 2.5 min |
| 4 | The open-image flow (sequence) | 2.5 min |
| 5 | The CK numbers | 2.0 min |
| 6 | SOLID / GRASP, in one breath | 1.0 min |
| 7 | The decisions I owned as TL | 2.0 min |
| 8 | Process hat (Sprint-1 SM), briefly | 1.0 min |
| 9 | Close | 0.5 min |
| — | **Q&A drill (separate, not in the 15)** | appendix |

---

## 1 — Who I am and what I owned  *(1.5 min)*

> "I'm Con Kirby. Across the three sprints I held two roles. In Sprint 1 I was **Scrum Master** — both for our sub-team and for Team Alpha overall, so I stood up the cohort-wide board and ran the Scrum-of-Scrums. In Sprint 2 I rotated to **Tech Lead**, and that's where my main technical artefact comes from: I own the **File-tab worked example** — worked example 1, the one §6.6 names.
>
> The File tab is the right thing to anchor on, because it exercises the whole chain end-to-end: a user opens an image, we describe it, we load it. If the architecture survives that path, it survives the proposal. So I'm going to walk you through it before, after, and in numbers."

[CUE: have the File-tab before/after class diagrams up.]

---

## 2 — File tab today: the pain  *(2.0 min)*

> "Today the File tab lives inside `CanvassDesktop` — one `MonoBehaviour`, **1,899 lines**, that owns file I/O, FITS axis logic, subset bounds, histogram maths, rendering wiring, lifecycle, the lot. As one class it measures:
>
> - **WMC 63** against an orchestrator ceiling of 40,
> - **CBO 30** against a ceiling of 25,
> - **LCOM 95%** against 50% — cohesion has collapsed,
> - and the worst single method, `checkSubsetBounds`, sits at cyclomatic **31**.
>
> [PAUSE] And it ships a real bug because of this: `DesktopPaintController.UpdateMaxValue` writes to `minVal`. A one-line copy-paste error that no unit test could ever catch — because the class can't be constructed outside the Unity engine, so there is no unit test.
>
> Concretely on the file path: opening a cube is a direct native plug-in call plus a `FindObjectOfType` plus a four-level `transform.Find("A/B/C/D")` chain — thirty-odd of those, each a silent `NullReferenceException` waiting for someone to rename a GameObject in the editor. That is the cost of the god class, made concrete."

[CUE: if pressed on CBO 30 — "even excluding every Unity value type it's still ≥ 20."]

---

## 3 — File tab after: the structure  *(2.5 min)*

> "After the split, the file-tab slice becomes **eleven focused classes** across three assemblies. The three that matter:
>
> - **`FileTabView`** — a thin Unity `MonoBehaviour`. Holds no logic. It binds to the ViewModel and renders.
> - **`FileTabViewModel`** — **pure C#, zero `UnityEngine` reference**, behind the `IFileTabViewModel` interface. This is where the command logic lives.
> - and the I/O sits behind three service interfaces — **`IFitsService`**, **`IFileDialogService`**, **`IVolumeService`** — with adapters behind each.
>
> The rule that makes it real isn't a convention, it's the **compiler**: the ViewModel assembly does not reference `UnityEngine`, so a `using UnityEngine` in a ViewModel simply fails to build. That's why the File-tab ViewModel is unit-testable without the Editor — and we have **47 NUnit tests** that run with zero Unity dependency to prove it.
>
> One honest detail: `FileTabViewModel` is classified as an **orchestrator**, not a domain class, because it coordinates four injected services. So it's measured against the ≤ 40 / ≤ 25 orchestrator bands, not the ≤ 20 / ≤ 14 domain ones — and I'll show you it clears them."

[CUE: point at the three boxes; the other eight classes are adapters/DTOs/composition root.]

---

## 4 — The open-image flow  *(2.5 min)*

> "Here's what actually happens when the user clicks **Browse Image**. [CUE: after-sequence diagram up.]
>
> The View raises `BrowseImageCommand`. The ViewModel calls `IFileDialogService.PickFileAsync` for the path, then `IFitsService.OpenImageAsync(path)`. Now the important part: behind that interface, **`FitsServiceAdapter` is a gateway proxy — no P/Invoke, no `IntPtr` on the client at all.** It sends two JSON-RPC calls over the named pipe to the server kernel: **`file.open`**, which returns a server-assigned `datasetId`, then **`dataset.getAxes`**, which returns the HDU list, axes and header. The server runs the native FITS plug-in; the client only ever holds an **opaque handle** — a `RemoteFitsHandle` wrapping that `datasetId`.
>
> The adapter assembles a `FitsFileInfo` DTO and hands it back. The ViewModel sets `ImagePath`, `HeaderText`, the HDU options, `IsLoadable` — pure C# state mutation — and `PropertyChanged` drives the view. The HDU dropdown populates, the header shows, **Load** lights up.
>
> Then **Load** calls `IVolumeService.LoadCubeAsync` with a progress callback — and that phase **stays client-side**, because the volume renderer genuinely is local Unity work. The error path is explicit: if the FITS read throws, an `alt` fragment sets `ValidationMessage` and drops `IsLoading` — the command never throws out. And when we swap files, disposing the handle fires a best-effort **`file.close`**. No native pointer ever lived on the client."

[CUE: the win line — "every BEFORE `CanvassDesktop → FitsReader → DLL` triangle becomes `VM → adapter → gateway → server`."]

---

## 5 — The CK numbers  *(2.0 min)*

> "The headline, tool-verified by Understand on the committed skeleton — measured, not projected:
>
> **God class → worst successor class: WMC 63 → 40, CBO 30 → 19. Both down 37%.**
>
> I quote the *worst* successor class deliberately — that's `FileTabViewModel`, the biggest thing left standing — because anyone can make an average look good. The worst class clears the orchestrator bands: **WMC 40 ≤ 40, CBO 19 ≤ 25.** Every other successor is smaller — `SubsetBoundsViewModel` is WMC 20, CBO 1; `FitsServiceAdapter` is WMC 6, CBO 7.
>
> [PAUSE] One thing I'll get ahead of: **LCOM stays high — 91% on the ViewModel.** That looks like it contradicts the cohesion story, and I want to be honest about it. It doesn't. That 91% is a **property-backing-field artifact** of MVVM: every bindable property has one backing field touched only by its getter and setter, which drives LCOM up mechanically. It is *not* the disjoint-concern collapse that drove `CanvassDesktop` to 95%. Same number range, opposite cause — and it's documented as such in `ck-metrics.md`."

[CUE: if they ask why these aren't the 47→4 numbers from an earlier doc — that's Q&A item Q3 below; lead with the revision story, don't hide it.]

---

## 6 — SOLID / GRASP, in one breath  *(1.0 min)*

> "Quickly, because LO4 asks us to name them at class level, not gesture:
> **S** — `FileTabViewModel` does command logic, `SubsetBoundsViewModel` does bounds validation, `FitsServiceAdapter` does the native translation; three actors, three classes.
> **O / D** — the ViewModel depends on `IFitsService`, not the adapter; a new format (HDF5) is a new adapter, no ViewModel change.
> **L** — the test doubles *are* the Liskov substitution; the 47 tests swap `StubFitsService` for the real one.
> **I** — `IFitsService` and `IFileDialogService` are split, each small.
> GRASP: **Indirection** is the interface; **Information Expert** is `SubsetBoundsViewModel` owning the bounds data and therefore the validation; **Low Coupling** is the 30 → 19 drop."

---

## 7 — The decisions I owned as Tech Lead  *(2.0 min)*

> "Three decisions on this slice were mine to make and defend:
>
> 1. **Anchor the whole proposal on the File tab.** It's the request/response example — it proves the MVVM-to-gateway chain end to end. The Debug tab proves Observer; together they show the architecture is general, not bespoke.
>
> 2. **Move the FITS read server-side, behind the gateway.** This is an evolution I want to be straight about: our first cut in Sprint 2 kept the read client-side and passed a path string for v1 simplicity. The later **gateway rewire (ADR-0002 / ADR-009)** moved it to the server — `file.open` / `dataset.getAxes` / `dataset.getHeader` / `file.close` against an opaque `datasetId`. The payoff is concrete: no `IntPtr` or P/Invoke on the client at all, and HDU switches stop reopening the file from disk — the old `ChangeHduSelection` reopened on every dropdown change; now it's one `dataset.getHeader` call against the open handle.
>
> 3. **Keep the platform out of the domain.** The UI-thread marshalling goes through a two-method **`IUIDispatcher`** seam, not a Unity static; the memory probe stays in the adapter, not the ViewModel. That's what keeps `FileTabViewModel` free of any `UnityEngine` or `SteamVR` reference — which is NFR-REU-3, and the thing the 47 Unity-free tests actually depend on."

[CUE: the interfaces trace back to the server-side I/O map in `D1-requirements/CanvassDesktop.md` — that map *is* the spec the after-diagram implements.]

---

## 8 — Process hat, briefly  *(1.0 min)*

> "Before the technical work, in Sprint 1 I was Scrum Master at both levels. The job there was cold-start scaffolding for a 27-person cohort: one ClickUp Team Space with a board per sub-team, a two-tier stand-up — 09:00 local, 09:15 Scrum-of-Scrums — so 27 people are never in one room, and an explicit **Dependency** column so cross-team blockers like the Sub-team 1 gateway contract live on the board, not in chat. I held capacity at ~75% on purpose — 21 cards — because a diagnosis sprint shouldn't over-commit while the tooling is still being stood up. Both cross-team risks went onto the integration register as DEPS-1 and DEPS-2."

---

## 9 — Close  *(0.5 min)*

> "So: one god class that fails every §4.2 non-negotiable, turned into eleven classes where the worst one clears its thresholds, the domain layer has no Unity in it, and there are 47 tests that couldn't exist before. The numbers are tool-measured, not hoped for. That's the File tab."

---

# Appendix — Q&A drill (the questions aimed at me)

Lead with the claim, give the evidence pointer, stop. Target 20–40 seconds each.

**Q1 — "Walk me through opening an image."**
Browse → `IFileDialogService.PickFileAsync` → `IFitsService.OpenImageAsync` → adapter sends `file.open` then `dataset.getAxes` over the pipe, gets a `datasetId`, returns a `FitsFileInfo` with a `RemoteFitsHandle` → VM sets `ImagePath`/`HeaderText`/`IsLoadable`, `PropertyChanged` drives the view → Load calls `IVolumeService.LoadCubeAsync`, client-side. Error → `FitsException` `alt` → `ValidationMessage` set, command never throws.

**Q2 — "How is the ViewModel kept free of Unity?"**
The ViewModel assembly doesn't reference `UnityEngine`, so it's a build error to import it. All I/O is behind service interfaces; UI-thread marshalling is the `IUIDispatcher` seam. Evidence: 47 NUnit tests run with no Unity. That's NFR-REU-3 and TST-1/2.

**Q3 (the trap) — "Your earlier docs say CBO 47 → ~4. Now you say 30 → 19. Which is it?"**
*Lead with honesty.* "30 → 19. The 47→4 was an early **hand-counted projection** on a composition-root-shell framing. The **Day-13 Understand export** superseded it — CBO measured 30 on the god class, and I now quote the **worst successor class**, `FileTabViewModel` at 19, not the flattering shell number. We re-measured and took the less impressive, more honest figure. `ck-metrics.md` is the source of truth." *(Do not get defensive — this answer is a strength.)*

**Q4 — "WMC 40 is over your ≤ 20 — explain."**
`FileTabViewModel` is an **orchestrator** — it coordinates four injected services — so it's graded against ≤ 40, which it meets exactly. It was 43 from the tool; we extracted three pure-static FITS helpers (`GetAxisMaxima`, `ComputeZScale`, `MaskAxesMatchImage`) to `FitsMetadataHelper` to bring it to 40. Don't quote the old "27 / domain / FileTabCommands → 22" — superseded.

**Q5 — "LCOM 91% — didn't you say the split *fixes* cohesion?"**
For `CanvassDesktop`, 95% was genuine disjoint-concern collapse. For the ViewModel, 91% is a **property-backing-field artifact**: each bindable property has one backing field touched by only its getter/setter. Same number, opposite cause. Not an SRP failure — documented in `ck-metrics.md`.

**Q6 — "Why path-string... wait, you said server handle. Which?"**
Server handle, current. v1 in Sprint 2 was path-string for simplicity; the gateway rewire (ADR-0002/009) moved the read server-side with an opaque `datasetId` handle and a `file.close` lifecycle. The win is no P/Invoke or `IntPtr` on the client and no file-reopen on HDU switches.

**Q7 — "The native plug-in is synchronous. How is `OpenImageAsync` async?"**
After the rewire it isn't a client P/Invoke at all — it's a JSON-RPC round-trip over the pipe, which is async by nature; the server runs the native parse. The ViewModel awaits a `Task` and the UI thread never blocks.

**Q8 — "Large cubes are tens of GB — does this assume in-memory data?"**
No. `OpenImageAsync` returns a `FitsFileInfo` with a handle to the open server-side dataset, not bytes. Axes, headers and slices are separate calls. The server keeps the cube; the client sees metadata and the slices it asks for.

**Q9 — "How many tests, and what do they cover?"**
**47**, NUnit, no Unity. Browse contract, Load contract, the error path, cancellation, and `SubsetBoundsViewModel` clamping. They exist *because* the ViewModel is pure C# — the whole point of the split.

**Q10 — "You were SM and TL — what did the SM hat add?"**
Cohort-wide ClickUp, the 09:15 Scrum-of-Scrums, the explicit Dependency column, and ~75% capacity on a diagnosis sprint. The TL hat is the File-tab example; the SM hat is the scaffolding that kept the cross-team gateway dependency visible.

**Q11 — "Where did AI help and where did it fail?"**
Helped: scaffolding diagrams and prose. Failed: early CK projections were confidently wrong — we replaced them with the tool-verified Day-13 export. This script and the role explainers are logged in `ai-log.md`; none of this defence is AI-assisted live.

---

### Three things to bank-avoid
- The stale numbers (47 / 118 / 0.955→0.12 / WMC 27 / WMC 63→~8). Always 63→40, 30→19, 47 tests.
- "Path-string over handle" — superseded by the server `datasetId` handle.
- "It works in practice" — it's a design proposal with a measured skeleton, not a production trial.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal rehearsal aid for Con Kirby. Not a deliverable, not a substitute for the human-authored individual reflection, and not for live use during the defence.*
