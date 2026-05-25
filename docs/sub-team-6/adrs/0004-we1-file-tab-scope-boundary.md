# ADR-0004: Worked Example 1 (File tab) scope boundary — in scope, out of scope, and cross-tab handoff

- **Status:** proposed
- **Date:** 2026-05-25
- **Authors:** [Sub-team 6 — author TBD; drafted via AI assistance, see `docs/sub-team-6/ai-log.md`]
- **Backlog:** [pending — propose ARCH-12]
- **Supersedes:** —

## Context

Worked Example 1 demonstrates the MVVM extraction of the File-tab image-loading workflow from `Assets/Scripts/UI/CanvassDesktop.cs` (1899 lines). The skeleton lives at `refactoring-examples/sub-team-6/file-tab/`. The production behaviour it replaces is traced in `before-trace.md` and itemised in `file-tab-info-docs/file-tab-scope.md`.

Some production behaviours that are *triggered by* the File-tab workflow are owned by other concerns (server-side capacity checks, cross-tab UI choreography, async load synchronisation, and an aspect-ratio control that is intra-tab but trivially small). Threading every one of these into the skeleton would obscure the pattern; silently omitting them leaves the panel asking "where did X go?". This ADR fixes the boundary explicitly so each omission has a written, defensible answer.

**Forces in tension:**

- **Pattern clarity vs. completeness.** The skeleton must *prove the MVVM pattern*, not *re-implement the whole tab*. Three threaded input categories (paths, dropdowns, subset bounds) already demonstrate the pattern; adding eight more side-effects buries it.
- **Defensible omission vs. silent omission.** CLAUDE.md mandates "Any AI-generated artefact must be defensible by a human author on the panel." The same standard applies to scope decisions: an articulated boundary survives the interview; an undocumented absence does not.
- **Design-only mandate vs. code-fix temptation.** CLAUDE.md is explicit: "This is a design proposal, not a code change. Do not refactor production scripts under `Assets/`." Real defects discovered during the trace (rest-frequency subscription leak, polling busy-wait) must be *cited as motivation*, not *patched in production* — the new architecture replaces them by construction without touching `Assets/`.

**Named dependencies for the cross-tab handoff (§"Decision" below):**

- Sub-team 1 (Architecture/Micro-kernel) — `IServiceGateway` surface includes the cross-tab event channel (`IClientShellEvents` or equivalent). Tracked under DEPS-1 on the integration risk register.
- Sub-team 3 (Domain Services) — owns the long-term `IVolumeService` semantics (task completes when the renderer is ready, future authoritative `CanFit` capacity check). The WE1 adapter already eliminates the busy-wait locally by yielding the renderer's existing `_startFunc` coroutine — no Sub-team 3 surface change is required for that defect; the dependency is only for future remote-mode transport and capacity gating.
- Sub-team 7 (Persistence) — `PlayerPrefs["LastPath"]` is shared with Sources and Mapping tabs (`file-tab-scope.md` §"Persistence"). Migration to a persisted state schema is Sub-team 7's surface, not WE1's.

## Decision

**The WE1 boundary:**

| Behaviour (before state) | Source citation | Decision | New owner |
|---|---|---|---|
| Client-side RAM feasibility check (`CheckMemSpaceForCubes`) | `CanvassDesktop.cs:995–1013` | **In scope as non-blocking client hint** | Threaded through `IMemoryProbe` (ACL boundary over `SystemInfo.systemMemorySize`, implemented by `MemoryProbeAdapter`) and `FitsFileInfo.EstimatedBytes` (computed by `FitsServiceAdapter` from NAXIS sizes). `FileTabViewModel.BuildMemoryWarning` surfaces a `ValidationMessage` when projected size exceeds available RAM, but the load still proceeds — matching the BEFORE "Using virtual memory" behaviour. Authoritative capacity remains server-owned (future `IVolumeService.CanFit(LoadCubeRequest)`); WE1 demonstrates the client-hint pattern only. |
| Cross-tab post-load cascade (`postLoadFileFileSystem`) — seven UI side-effects on Stats / Rendering / Sources / Paint / VR view / mask dropdown / rest-freq panel | `CanvassDesktop.cs:935–987` | **Out of scope for WE1 (Client-Shell-owned)** | Becomes an event boundary: `FileTabViewModel.LoadAsync` ends with publishing a `CubeLoaded` event; the Client Shell composition root subscribes the other tabs' ViewModels. WE1 demonstrates the publish side only. Subscribers are shown by analogy in the later tab refactors. |
| Async load synchronisation via polling (`while (!volDSRender.started) yield return WaitForSeconds(.1f)`) | `CanvassDesktop.cs:1116–1119` | **Eliminated by construction (in WE1)** | `VolumeServiceAdapter` yields the renderer's own `_startFunc` coroutine (`yield return StartCoroutine(renderer._startFunc())`) — `_startFunc` sets `started = true` at its terminating yield (`VolumeDataSetRenderer.cs:541-542`), so the await resumes exactly when the renderer is ready. No 100 ms polling, no Sub-team-3 surface change, no production-code edit required. `IVolumeService.LoadCubeAsync` returns a `Task` that completes when this coroutine finishes. |
| Aspect-ratio dropdown (`_ratioDropdownIndex`) — set on the file-load modal, read once in the load coroutine to compute zScale | `CanvassDesktop.cs:91, 1028–1039, 1134–1136` | **In scope — threaded as fourth input category** | Threaded through `RatioMode` enum + `IFileTabViewModel.RatioMode` / `RatioModeOptions` + `LoadCubeRequest.ZScale`. `FileTabViewModel.ComputeZScale` is the pure replacement for the inline arithmetic at `CanvassDesktop.cs:1028-1039`. Closes the 11/11 control coverage gap and demonstrates that adding a new control in the new architecture is mechanical (one DTO field + one VM property + one binding). |

**Cross-tab handoff — the event shape:**

```
File-tab VM completes LoadAsync
   └── publishes CubeLoaded(DataSetHandle) on IClientShellEvents
       ├── StatsTabViewModel.OnCubeLoaded(handle)      → repopulate stats, regenerate histogram
       ├── RenderingTabViewModel.OnCubeLoaded(handle)  → enable mask dropdown, populate colormap + rest-freq
       ├── SourcesTabViewModel.OnCubeLoaded(handle)    → unlock Sources tab button
       ├── PaintTabViewModel.OnCubeLoaded(handle)      → unlock Paint tab button
       └── MenuBarViewModel.OnCubeLoaded(handle)       → switch to VR view display
```

WE1 produces the publisher. Each subscriber is responsible for its own subscription lifetime via `IDisposable` — a clean replacement for the current `postLoadFileFileSystem` lines 958–959 which re-subscribe rest-frequency handlers on every load without unsubscribing the previous renderer's (resolved anomaly #5 in `file-tab-scope.md` §10 — a real subscription leak).

**Authority of this boundary:**

The decisions above are binding for the WE1 skeleton at `refactoring-examples/sub-team-6/file-tab/`. The skeleton's README links to this ADR so a reader arriving at the code sees the boundary before reading the implementation.

## Consequences

**Positive:**

- The worked example has a list-shaped answer to the inevitable panel question "where did X go?" — every omission is a row in a table with a citation and a new owner.
- Two real production defects are addressed *by construction* without code changes in `Assets/`: the polling busy-wait at `CanvassDesktop.cs:1116-1119` (replaced by yielding `_startFunc` in `VolumeServiceAdapter`), and the rest-frequency subscription leak at `postLoadFileFileSystem` lines 958-959 (replaced by `IDisposable` subscriber lifetime on the `CubeLoaded` event). The memory check is preserved as a non-blocking client hint via `IMemoryProbe`; authoritative server-side gating is a future surface.
- The cross-tab handoff is reduced from seven hard-coded `transform.Find(…)` chains in `postLoadFileFileSystem` to one event publish, formalising the seam between the File tab and the future Client Shell.
- The rest-frequency leak (`file-tab-scope.md` §10 #5) becomes a closed problem: per-subscriber `IDisposable` lifetime is the new contract.

**Negative:**

- The `IClientShellEvents` surface is named but not yet specified — its full surface depends on Sub-team 1's gateway design (DEPS-1). If that surface ships with constraints we haven't anticipated, the boundary in this ADR may need a small revision.
- "Client-Shell-owned" choreography is named but not illustrated in code. A reader who skips this ADR may not realise the subscriber-side is intentionally absent from WE1.
- `IMemoryProbe` is a hint, not authoritative — `SystemInfo.systemMemorySize` reports total system RAM, not available RAM under current GC/process load. A "fits" hint can still be followed by a server-side OOM. Mitigation: the hint is non-blocking (warning + proceed), matching the BEFORE behaviour; the authoritative gate moves to future `IVolumeService.CanFit` on the server.

**Operational:**

- `refactoring-examples/sub-team-6/file-tab/README.md` (to be added) links to this ADR as the first paragraph.
- The skeleton README also enumerates the 11 controls inventoried in `file-tab-scope.md` and marks each as "threaded" / "pattern-equivalent" / "subscriber-side" so the boundary is visible at the code surface.
- DEPS-1 on the integration risk register is amended to include `IClientShellEvents` surface as part of the gateway contract.
- This ADR feeds the pitch slide for slot 4 (worked-example walkthrough) — the "what we deferred" half-slide reads directly from the decisions table.

## Alternatives considered

- **Thread every before-state behaviour into the skeleton.** Most faithful, most defensive against the "where did X go?" question. Rejected: scope creep on a design-only deliverable; eight `postLoadFileFileSystem` side-effects buried under three lines of pattern-demonstrating code. The skeleton would stop being a proof and start being a re-implementation.
- **Silently omit and rely on `file-tab-scope.md` §10 anomaly list to absorb panel questions.** Cheaper to write; defensible-in-principle because the anomaly list is on the record. Rejected: panels look for ADRs, not for buried anomaly tables. The anomaly list is an analysis artefact, not a decision record.
- **Patch the two defects (busy-wait, rest-frequency subscription leak) directly in `Assets/Scripts/UI/CanvassDesktop.cs`.** Most "satisfying" engineering response. Rejected: CLAUDE.md mandates design-only — "Do not refactor production scripts under `Assets/`." The proposal cites these defects as motivation; the architecture *replaces them by construction* (busy-wait by yielding the renderer's `_startFunc` coroutine in `VolumeServiceAdapter`, leak by `IDisposable` subscriber lifetime on `CubeLoaded`); production code stays untouched until the panel approves the refactor.

## References

- [ADR-0001 — MVVM split](./0001-mvvm-split.md) — parent decision; this ADR scopes one of its worked examples.
- [ADR-0002 — Client–server transport](../deliverables/D2-Architecture/client-server-transport.md) — the gateway this ADR's events ride on top of.
- ADR-0003 — Anti-corruption layer (reserved; referenced from `FileTabViewModel.cs` comment header).
- [WE1 — File tab skeleton](../../../refactoring-examples/sub-team-6/file-tab/) — the artefact this boundary governs.
- [`before-trace.md`](../../../refactoring-examples/sub-team-6/file-tab/before-trace.md) — production behaviour traced (B-row anchors for each line citation above).
- [`file-tab-scope.md`](../deliverables/D4-worked-examples/ex1-file-tab/file-tab-info-docs/file-tab-scope.md) — inventory of 11 controls (§"User-visible controls"); resolved anomalies §10 #4 (AspectRatio) and §10 #5 (rest-frequency leak).
- [`metrics.md`](../deliverables/D4-worked-examples/metrics.md) §2.3 — CK delta evidence that the after-state stays within §7.1 thresholds even with the boundary applied.
- `Assets/Scripts/UI/CanvassDesktop.cs` — line citations: 91 / 935–987 / 995–1013 / 1028–1039 / 1116–1119 / 1134–1136.
- DEPS-1 — gateway contract dependency on Sub-team 1, integration risk register.
