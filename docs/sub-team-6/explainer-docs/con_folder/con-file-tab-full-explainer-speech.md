# File Tab Refactor — Full Explainer Speech (learning version)

> **What this is.** Not the timed pitch — this is the *learn-it-cold* version: a complete, spoken-style walkthrough of the File-tab worked example that teaches the **why** behind every move, so I can compress it to any length on the day and still defend any sentence. Drill the reasoning, not the wording.
>
> **AI-policy note.** AI-assisted study aid, logged in `ai-log.md`. AI may **not** be used in the live pitch/defence (§10.5 #6). I must be able to say all of this in my own words.
>
> **Numbers source of truth.** Tool-verified Day-13 Understand export, canonical in `refactoring-examples/sub-team-6/file-tab/ck-metrics.md`. Stale figures to never quote: `CBO 47`, `RFC 118`, `LCOM 0.955→0.12`, `WMC 27`, `WMC 63→~8`, and "path-string over handle." Always: **63→40, 30→19, 47 tests, server `datasetId` handle.**

---

## 0. Why the File tab is the right thing to explain (the framing)

"I want to walk you through the File tab, and I want to start by saying *why* it's the example worth your time.

The File tab is the one path that exercises the entire architecture end-to-end. A user opens an image, the system describes it, and then loads it into the renderer. That single journey touches file dialogs, native FITS reading, metadata parsing, validation logic, the transport to the server, and the volume renderer. If the proposed architecture can carry that journey cleanly, it can carry anything else in the desktop client. That's why §6.6 names it by hand as worked example one, and that's why I anchored our proposal on it.

So I'm going to do four things: show you the File tab as it exists today and why it's structurally unliveable; show you what it becomes; walk the open-image flow through the new design; and then give you the numbers that prove the change is real and not cosmetic."

---

## 1. The File tab today — one class that owns everything

"Today there is no File tab class. The File tab lives *inside* `CanvassDesktop` — a single Unity `MonoBehaviour` that is **1,899 lines long**. That one class owns file I/O, FITS axis logic, subset-bounds maths, histogram maths, colour maps, rendering wiring, statistics, threshold controls, source mapping, paint-mode wiring, configuration, and the component lifecycle. The 16 methods that *are* the File tab — browse image, browse mask, read the header, validate the subset, load the cube — are just woven into that tangle.

Let me make the pain concrete rather than abstract, because 'big class' isn't an argument on its own.

**First — opening a cube is a direct native call.** When you pick a file, `CanvassDesktop` calls a static `FitsReader` wrapper, which does a `[DllImport]` straight into a C/C++ native plug-in. The raw native handle — an `IntPtr` — comes back up into the `MonoBehaviour` and is held in a field across coroutines. The UI layer is touching unmanaged memory directly. That's the boundary that should never have been crossed in the client.

**Second — the UI is wired to the scene by string.** To reach a dropdown, the code does a four-level chain like `transform.Find("HeaderTitle_container/Hdu_container/Hdu_dropdown").GetComponent<Dropdown>()`. There are around thirty of those chains in the class. Each one is a stringly-typed dependency on the *name* of a GameObject in the scene tree. Rename a GameObject in the editor and the compiler stays completely silent — you get a `NullReferenceException` at runtime instead. In coupling terms that's content coupling, the worst kind, and connascence of name at runtime, the strongest and least visible kind. The class is structurally vulnerable to a one-line UI edit.

**Third — and this is the one that should worry a maintainer most — none of this can be unit-tested.** A `MonoBehaviour` cannot be constructed outside the Unity engine. So there is no way to write a unit test against any of these 16 methods. And the cost of that isn't hypothetical: there is a real, currently-shipping bug in the sibling `DesktopPaintController` where a method called `UpdateMaxValue` actually writes to `minVal`. A one-line copy-paste error, in a 1,558-line class, that no human will catch by reading and no test exists to catch automatically — *because the architecture forbids the test from being written.* That is the true cost of the god class.

As one class, here's what it measures, and these are tool-verified, not my opinion:
- **WMC 63**, against an orchestrator ceiling of 40.
- **CBO 30** — coupled to thirty other types — against a ceiling of 25.
- **LCOM 95%**, against 50%. Cohesion has effectively collapsed.
- The single worst method, `checkSubsetBounds`, has a cyclomatic complexity of **31**.

[If pressed that CBO 30 is unfair on a Unity MonoBehaviour: the orchestrator ceiling is already 25, not the domain 14 — we're grading on a curve and it still fails. Even excluding every Unity value type, it's still at least 20.]"

---

## 2. Why this is structurally bad, not just ugly (the theory)

"I want to name the theory, because the assignment asks us to connect smells to principles, not just point and wince.

The root violation is the **Single Responsibility Principle** — a class should have one reason to change, one *actor* it answers to. `CanvassDesktop` answers to the file-loading actor, the rendering actor, the QA actor, the persistence actor, and several more. That's why the LCOM of 95% is meaningful: it's the operational proof that the methods operate on disjoint slices of the field set. 63 methods touch 67 fields, but the field-method accesses are sparse and clustered — file-loading methods touch file-loading fields, rendering methods touch rendering fields, and the two groups barely overlap. That's not one class; it's several classes sharing a file by accident.

And it maps directly onto the four §4.2 non-negotiables — the panel's own contract with us — every one of which the File tab currently fails:
1. **No SOLID/GRASP violations** — SRP, OCP, and DIP are all broken.
2. **Zero circular dependencies** — there are cycles, e.g. through `VolumeCommandController`.
3. **Domain code free of `UnityEngine`/`SteamVR`** — the file logic imports `UnityEngine`, `TMPro`, and `Valve.VR` directly; there is no domain isolation at all.
4. **Every public API is an interface with a test double** — there are zero interfaces on this surface.

The key insight I want to land here: **no incremental fix satisfies boxes three and four.** You cannot tidy your way out of 'domain code must not depend on Unity' when the domain code *is* a Unity component. The only path is to split the class so that the logic lives somewhere that Unity can't reach it. The architecture isn't a preference — it's forced by the constraints."

---

## 3. The after — eleven focused classes across three assemblies

"After the split, the File-tab slice becomes **eleven focused classes**, organised across **three C# assemblies**. The three-assembly structure is the load-bearing idea, so let me explain it before the classes.

The assemblies are **View**, **ViewModel**, and **Gateway**, and they exist as separate assemblies because each is *allowed a different set of dependencies* — and that allowance is enforced by the compiler, not by a coding convention.
- The **View** assembly may reference `UnityEngine` and UI Toolkit.
- The **ViewModel** assembly may reference `System.*` only. It does *not* reference `UnityEngine`.
- The **Gateway** assembly holds the contracts and the transport.

This is the answer to the question every maintainer asks: *how do you stop a developer re-introducing the god class six months from now?* The answer is that they can't. A `using UnityEngine` inside a ViewModel doesn't get caught in code review — it fails to compile, because the assembly doesn't reference Unity. There's no honour system. The build refuses.

Within that, the three classes that matter for the File tab:
- **`FileTabView`** — a thin Unity `MonoBehaviour`. It holds no logic. It binds to the ViewModel's properties and commands and renders them. It is the *only* File-tab class that knows the scene exists.
- **`FileTabViewModel`** — **pure C#, zero `UnityEngine` references**, sitting behind the `IFileTabViewModel` interface. This is where the command logic and validation live. Because it's pure C#, it can be constructed in a test with `new FileTabViewModel(...)`.
- **The I/O behind three service interfaces** — **`IFitsService`** for FITS reading, **`IFileDialogService`** for the file picker, **`IVolumeService`** for the renderer — each with an adapter implementation behind it, plus **`IMemoryProbe`** for the RAM check.

The remaining classes fill out the slice: `SubsetBoundsViewModel` (the bounds-validation logic, extracted as its own ViewModel), `FitsMetadataHelper` (three pure-static FITS helpers), the `RelayCommand`/`AsyncRelayCommand` command helpers, the adapters themselves, and a `FileTabCompositionRoot` that wires the whole graph together at startup.

That composition root is worth one sentence, because it answers 'where does `new` happen?' It happens *exactly once*, at startup, in the composition root. That single change kills `FindObjectOfType` and kills the static singletons — every dependency becomes explicit and injectable, which is precisely what makes every class mockable.

And the proof that the ViewModel really is Unity-free isn't a claim — it's **47 NUnit tests** that run against `FileTabViewModel` and `SubsetBoundsViewModel` with zero Unity dependency. Before the split, that number was zero, because zero was the only number possible.

One honest detail I'll volunteer before you ask: `FileTabViewModel` is classified as an **orchestrator**, not a domain class, because it coordinates four injected services. So it's measured against the orchestrator bands — WMC ≤ 40 and CBO ≤ 25 — not the stricter domain bands of ≤ 20 and ≤ 14. I'll show you it clears the orchestrator bands honestly."

---

## 4. The open-image flow, step by step

"Let me walk what actually happens when a user clicks **Browse Image**, because the diagram is the architecture viewed in time.

**The pick.** The View raises `BrowseImageCommand`. The ViewModel calls `IFileDialogService.PickFileAsync` to get a path — that adapter is the only thing that touches the OS picker and `PlayerPrefs`. The ViewModel sets `IsLoading = true`, and `PropertyChanged` turns the spinner on in the View.

**The read — and this is the important part.** The ViewModel calls `IFitsService.OpenImageAsync(path)`. Behind that interface, `FitsServiceAdapter` is **not** a P/Invoke wrapper anymore — it's a **gateway proxy**. There is **no `IntPtr` and no P/Invoke on the client at all.** It sends two JSON-RPC calls over a named pipe to the server kernel:
- `file.open`, which returns a server-assigned `datasetId`;
- then `dataset.getAxes`, which returns the HDU list, the axes, and the header text.

The server runs the native FITS plug-in. The client only ever holds an **opaque handle** — a `RemoteFitsHandle` wrapping that `datasetId` string. No native pointer ever exists on the client side.

**The state update.** The adapter assembles a `FitsFileInfo` DTO and hands it back. The ViewModel sets `ImagePath`, `HeaderText`, the HDU options, resets the subset bounds, and recomputes `IsLoadable` — all pure C# state mutation. `PropertyChanged` drives the View: the HDU dropdown populates, the header appears, and the **Load** button lights up.

**The load.** **Load** calls `IVolumeService.LoadCubeAsync` with a progress callback — and this phase deliberately **stays client-side**, because the volume renderer genuinely is local Unity work; there's no point shipping it to a server. The adapter runs the coroutine, instantiates the prefab, and reports progress back.

**The error path is explicit.** If the FITS read throws, an `alt` fragment sets `ValidationMessage` and drops `IsLoading` back to false — the command never throws out to the caller. And when the user swaps files, disposing the old handle fires a best-effort `file.close` to the server so no dataset leaks.

The one-line summary of the whole change: every BEFORE `CanvassDesktop → FitsReader → DLL` triangle becomes `ViewModel → adapter → gateway → server`. The native call didn't disappear — it moved to where it belongs, and the client now speaks only JSON-RPC."

---

## 5. The numbers

"Now the numbers, tool-verified by Understand on the committed skeleton — measured, not projected.

**The headline: god class to worst successor class, WMC 63 → 40, CBO 30 → 19. Both down 37%.**

I quote the *worst* successor class on purpose — that's `FileTabViewModel`, the biggest thing left standing — because anyone can make an average look good by hiding the big class in it. The worst class clears the orchestrator bands: **WMC 40 ≤ 40, CBO 19 ≤ 25.** Every other successor is smaller. `SubsetBoundsViewModel` is WMC 20, CBO 1. `FitsServiceAdapter` is WMC 6, CBO 7.

And the testability number, which is the one I actually care about most: **0 → 47 unit tests**, running with no Unity engine, in under a tenth of a second.

There's one number I'll get ahead of, because it looks like it contradicts my own story. **LCOM stays high — 91% on the ViewModel.** I told you the before-state's 95% LCOM proved cohesion had collapsed, so why is the after-state still at 91%? Because it's a *different cause with the same number*. The 95% on `CanvassDesktop` was genuine disjoint-concern collapse — file I/O and rendering sharing a class by accident. The 91% on `FileTabViewModel` is a **property-backing-field artifact** of MVVM: every bindable property has exactly one backing field, touched only by its own getter and setter. That mechanically drives LCOM toward 100% even when every property is about the same thing — file loading. It's a known limitation of the metric on property-heavy patterns, and it's documented as such in `ck-metrics.md`. Same number range, opposite structural meaning. I'd rather tell you that than have you find it."

---

## 6. SOLID and GRASP, named at class level

"LO4 asks us to name the principles at class level, not gesture at them, so quickly:
- **S — Single Responsibility:** `FileTabViewModel` does command logic, `SubsetBoundsViewModel` does bounds validation, `FitsServiceAdapter` does the native translation. Three actors, three classes.
- **O and D — Open-Closed and Dependency Inversion:** the ViewModel depends on `IFitsService`, not on the adapter. A new file format like HDF5 is a new adapter and zero ViewModel changes.
- **L — Liskov:** the test doubles *are* the Liskov substitution — the 47 tests swap a `StubFitsService` for the real one and the ViewModel can't tell.
- **I — Interface Segregation:** `IFitsService` and `IFileDialogService` are separate, each small; the ViewModel doesn't depend on dialog methods to do FITS work.
- **GRASP:** **Indirection** is the interface itself; **Information Expert** is `SubsetBoundsViewModel` owning the bounds data and therefore owning the validation of it; **Low Coupling** is the 30 → 19 drop made concrete."

---

## 7. The decisions I owned, and the honest caveats

"Three decisions on this slice were mine to make and defend as Tech Lead.

**One — anchor the proposal on the File tab.** It's the request/response example; it proves the MVVM-to-gateway chain end to end. The Debug tab proves the Observer pattern. Together they show the architecture is general, not bespoke to opening a file.

**Two — move the FITS read server-side, behind the gateway.** I want to be straight about this, because it's an evolution. Our first cut in Sprint 2 kept the read client-side and passed a path string, for v1 simplicity. The later gateway rewire — ADR-0002 and ADR-009 — moved it to the server: `file.open`, `dataset.getAxes`, `dataset.getHeader`, `file.close`, against an opaque `datasetId`. The payoff is concrete: no `IntPtr` or P/Invoke on the client at all, and HDU switches stop reopening the file from disk. The old `ChangeHduSelection` reopened the whole file on every dropdown change; now it's a single `dataset.getHeader` call against the already-open handle.

**Three — keep the platform out of the domain.** The UI-thread marshalling goes through a two-method `IUIDispatcher` seam, not a Unity static. The memory probe stays in an adapter, not the ViewModel. That's what keeps `FileTabViewModel` free of any `UnityEngine` or `SteamVR` reference — which is NFR-REU-3, and the thing the 47 Unity-free tests actually depend on to exist.

And one honest caveat about what *remains*: the field writes onto `VolumeDataSetRenderer` in Phase B still happen — but they're now *contained* inside `VolumeServiceAdapter` instead of scattered across the god class. The fix vector is clean: when Sub-team 3 introduces an `IRendererCommand`, those field writes become a command emit, and not one of the 47 ViewModel tests has to change. I'd rather name the smell that's contained than pretend the refactor is perfect."

---

## 8. Close

"So, to put a bow on it: one god class of 1,899 lines that fails every single §4.2 non-negotiable, turned into eleven focused classes where the worst one clears its thresholds, where the domain layer has no Unity in it at all, and where there are 47 unit tests that simply could not have existed before. The numbers are tool-measured, not hoped for. The native call moved to where it belongs. And a maintainer can now, for the first time, write a test for the File tab. That's the File tab."

---

## Three things to never say
- The stale numbers: 47 / 118 / 0.955→0.12 / WMC 27 / WMC 63→~8. Always **63→40, 30→19, 47 tests**.
- "Path-string over handle" — superseded by the server `datasetId` handle.
- "It works in practice" — it's a design proposal with a measured skeleton, not a production trial.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal learning aid for Con Kirby. Not a deliverable, not a substitute for the human-authored individual reflection, and not for live use during the defence.*
