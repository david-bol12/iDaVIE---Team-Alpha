# ADR-0005: Worked Example 2 (Debug tab) scope boundary — in scope, out of scope, and producer-side handoff

- **Status:** proposed
- **Date:** 2026-05-25
- **Authors:** [Sub-team 6 — author TBD; drafted via AI assistance, see `docs/sub-team-6/ai-log.md`]
- **Backlog:** [pending — propose ARCH-13]
- **Supersedes:** —

## Context

Worked Example 2 demonstrates the Observer-pattern extraction of the Debug-tab log-display workflow from `Assets/Scripts/Debuggers/DebugLogging.cs` (255 lines). The skeleton lives at `refactoring-examples/sub-team-6/debug-tab/`. The production behaviour it replaces is traced in [`before-trace.md`](../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md); the 44 producer call sites are catalogued in [`log-origin-trace.md`](../../../refactoring-examples/sub-team-6/debug-tab/log-origin-trace.md).

`DebugLogging` mixes four concerns — log capture, log storage (autosave + rotation), log display, manual export — and subscribes directly to Unity's process-global `Application.logMessageReceived` event. The capture seam is a static singleton with no interface, making the class untestable without a Unity test runner. Three of the four concerns (storage, manual export, and the producer-side migration of the 44 `Debug.Log*` call sites) are *triggered by* the Debug-tab workflow but logically belong to separate refactoring slices. Threading every one of them into the WE2 skeleton would obscure the Observer/ACL pattern; silently omitting them leaves the panel asking "where did X go?". This ADR fixes the boundary explicitly so each omission has a written, defensible answer.

**Forces in tension:**

- **Pattern clarity vs. completeness.** The skeleton must *prove the Observer + ACL pattern*, not *re-implement the whole Debug tab*. The capture → publish → observe → render path already demonstrates the pattern; adding autosave + log-rotation + manual export buries it.
- **Defensible omission vs. silent omission.** CLAUDE.md mandates "Any AI-generated artefact must be defensible by a human author on the panel." The same standard applies to scope decisions: an articulated boundary survives the interview; an undocumented absence does not.
- **Design-only mandate vs. code-fix temptation.** CLAUDE.md is explicit: "This is a design proposal, not a code change. Do not refactor production scripts under `Assets/`." The 44 existing `Debug.Log*` call sites must *stay untouched*; the new architecture must capture them by construction via the unchanged static event.
- **Non-invasive consumer refactor vs. structured producer migration.** The `log-origin-trace.md` analysis identifies four distinct `source` clusters (`"FileTab"`, `"VolumeLoader"`, `"HistogramController"`, `"SourcesTab"`, `"DataAnalysis"`) and argues for a `source` field on `ILogStream.Publish`. Adding the field forces a producer-side migration we have not yet sized. Leaving it out keeps WE2 non-invasive but defers the filterability win.

**Named dependencies for the producer-side handoff (§"Decision" below):**

- Sub-team 7 (Persistence) — owns the future log-persistence schema if autosave / log-rotation / manual export are reintroduced. The WE2 skeleton intentionally leaves these unhooked so Sub-team 7 can define the storage contract without inherited assumptions from `DebugLogging`'s per-message `StreamWriter` pattern.
- Producer-side sub-teams (per call site cluster) — `CanvassDesktop` callers (40 sites) belong to whichever sub-team eventually owns the post-`CanvassDesktop` split; `DataAnalysis` callers (4 sites, the canonical ACL violations) belong to whoever owns the P/Invoke wrapper refactor. No surface contract is required from them for WE2 — the capture path is non-invasive.

## Decision

**The WE2 boundary:**

| Behaviour (before state) | Source citation | Decision | New owner |
|---|---|---|---|
| Global log capture via `Application.logMessageReceived` | `DebugLogging.cs:149` | **In scope, contained behind `UnityLogStreamAdapter`** | The adapter is the **only** class permitted to subscribe to the static event. `LogType → LogLevel` normalisation happens here. The 44 existing `Debug.Log*` callers (`log-origin-trace.md`) are captured **without modification**. |
| Direct `ILogStream.Publish(level, message)` from new structured callers | `ILogStream.cs:13` | **In scope as design surface; no production caller yet** | The skeleton interface exposes a structured-publish path for future use. The producer-side migration of any specific call site (e.g. the 10 P/Invoke errors E1–E10 in `log-origin-trace.md`) is a separate refactor — see "Producer-side migration" below. |
| Source-tagged routing (`Publish(level, source, message)`) — argued for in `log-origin-trace.md` | `log-origin-trace.md` §"What the origin trace proves" | **Open question; deferred to first consumer that needs it** | `LogEntry` is an immutable `record(Level, Message, Timestamp)`. Adding `Source` is a one-line schema change. The deferred decision is binding for the WE2 deliverable; either path — add now, or defer until a consumer (level-filter, per-tab error counter, …) requests it — is acceptable. The risk of deferring is asymmetric: structured callers can be migrated later; rip-and-replace of an embedded `Source` field is more expensive. |
| In-memory log storage with bounded growth | `DebugLogging.cs:49, 180` | **In scope** | `List<LogEntry>` capped at `MaxEntries = 2000` in `DebugTabViewModel` replaces the unbounded non-generic `Queue`. Memory ceiling is now O(2000) entries regardless of session length. |
| Per-message file persistence (`AutoSave`) | `DebugLogging.cs:181, 250-253` | **Out of scope for WE2 (Persistence-owned)** | Becomes a follow-up `ILogObserver`-shaped adapter: one long-lived `StreamWriter`, opened in ctor, closed in `Dispose`, subscribed at composition root. The interface contract does not change. Sub-team 7 owns the storage schema (rotation policy, file format, sync vs. async writes). |
| Log-rotation on startup (`Outputs/Logs/iDaVIE_Log_N_*` renames) | `DebugLogging.cs:68-112` | **Out of scope for WE2 (Persistence-owned)** | Tied to the autosave decision above. The rotation strategy is a Persistence concern, not a Debug-tab concern. |
| Manual export ("Save" button + file picker) | `DebugLogging.cs:124, 203-242` | **Out of scope for WE2 (Persistence-owned)** | The skeleton provides a "Clear" button instead (`DebugTabView` line 47 binds `_clearButton.onClick` to `IDebugTabViewModel.ClearEntries`). Manual export is reintroduced when the Persistence schema is defined; the `IDebugTabViewModel.LogEntries` read-only list is the data surface for any future "Export" command. |
| Producer-side migration of 44 `Debug.Log*` call sites to structured `Publish` | `log-origin-trace.md` (full catalogue) | **Out of scope for WE2** | Each producer cluster becomes its own refactoring slice owned by whichever sub-team owns that producer. The Debug-tab refactor is structured so the legacy `Debug.Log*` capture and the structured `Publish` pathway coexist behind the same `ILogStream` interface — file-by-file migration is possible without breaking the consumer side. |
| Scroll-pin-while-reading (the `AutoScrollEnabled` toggle proposed in `before-trace.md` smell S7) | `DebugLogging.cs:196`, also `DebugTabView.cs:83` | **Out of scope for WE2 (UX follow-up)** | Adding `AutoScrollEnabled : bool` to `IDebugTabViewModel` and gating the `_scrollbar.value = 0f` line on it is ~5 lines plus one test. Deferred because it changes the public interface; logged as a follow-up in [`after-trace.md` → Known limitations](../../../refactoring-examples/sub-team-6/debug-tab/after-trace.md#known-limitations). |
| TMP virtualised list (instead of `text =` rebuild) | `DebugLogging.cs:195`, also `DebugTabView.cs:62-82` | **Out of scope for WE2 (UI follow-up)** | TMP text rebuild remains, capped at `MaxDisplayLines = 500`. Replacing it with a Unity UI Toolkit `ListView` is a Sub-team 6 follow-up; the `IDebugTabViewModel.LogEntries` contract does not change. |

**Producer-side handoff — the (deliberately) thin contract:**

```
Existing producers                         WE2 skeleton                    Future structured callers
─────────────────                          ───────────                     ─────────────────────────
CanvassDesktop (40 sites)                                                  (any class that wants
DataAnalysis   (4 sites)                                                    structured logging)
       │                                                                          │
       │ Debug.Log* (UNCHANGED)                                                   │ ILogStream.Publish(level, message)
       ▼                                                                          ▼
UnityEngine.Debug → Application.logMessageReceived ──► UnityLogStreamAdapter ◄────┘
                                                              │
                                                              │ ILogStream.Publish
                                                              ▼
                                                          LogStream
                                                              │
                                                              │ ILogObserver.OnNext(LogEntry)
                                                              ▼
                                                       DebugTabViewModel
                                                              │
                                                              │ EntriesChanged
                                                              ▼
                                                          DebugTabView
```

WE2 produces the **capture and consumer sides**. Producer-side migration to structured `Publish` is an opt-in, file-by-file refactor. Each migrated caller becomes testable in isolation (its `ILogStream` dependency is injectable); unmigrated callers continue to work through the unchanged static event.

**Authority of this boundary:**

The decisions above are binding for the WE2 skeleton at `refactoring-examples/sub-team-6/debug-tab/`. The skeleton's README links to this ADR so a reader arriving at the code sees the boundary before reading the implementation.

## Consequences

**Positive:**

- The worked example has a list-shaped answer to the inevitable panel question "where did X go?" — every omission is a row in a table with a citation and a new owner.
- **The capture refactor is non-invasive at the producer side.** The 44 existing `Debug.Log*` call sites (`log-origin-trace.md`) are untouched. The panel can verify this by `grep` — `Application.logMessageReceived` has exactly one subscriber after the refactor (`UnityLogStreamAdapter.OnEnable`), where before it had `DebugLogging.HandleLog`.
- **Smell S1 contained, smell S8 eliminated.** The static-event seam is confined to one Unity-side class; the four BEFORE concerns are now five named types with LCOM4 = 1 each. See [`ck-metrics.md`](../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md).
- **Section 4.2 compliance for the slice is mechanically verifiable.** `dotnet build refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabSkeleton.csproj` succeeds with zero `UnityEngine` references (verified on the team6 branch, 0 warnings 0 errors). `dotnet test` runs 29 NUnit tests in ~20 ms with no Unity test runner.
- The `ILogStream` / `ILogObserver` interface pair gives future consumers (autosave, telemetry, level-filter, per-tab error counter) a stable surface to attach to without modifying `LogStream` or any producer.

**Negative:**

- **The `source` field deferral is asymmetric in cost.** Adding `Source` to `LogEntry` and `ILogStream.Publish` later requires migrating any structured caller that landed in the interim. If a consumer ships before the schema decision, that consumer eats the migration cost. Mitigation: no consumer is structured yet, so the migration cost is currently zero — but the window will close as soon as the first structured caller lands.
- **No autosave means a Debug-tab feature regression.** `DebugLogging` writes every message to disk; the WE2 skeleton does not. If autosave is required before the Persistence schema is defined, a temporary `FileLogObserver` ~30-line class can be added as a stop-gap — see "Alternatives considered". The skeleton's structure supports this without changing any contract.
- **The skeleton's display is a TMP text rebuild over a 500-line slice, not virtualised.** This is acceptable under expected log frequencies but degrades visibly under a flood (e.g. per-frame `Debug.Log` from a hot loop). Mitigation: the cap is enforced at both the VM (2000) and View (500) layers; the underlying smell is contained, not eliminated.
- **`UnityLogStreamAdapter` is the new single point of failure for capture.** If its `OnEnable` is missed (e.g. attached to an inactive GameObject), no log lines arrive. Same risk profile as BEFORE — `DebugLogging` had the same `OnEnable` dependency — but the new code path is shorter and more obvious to a reviewer.

**Operational:**

- `refactoring-examples/sub-team-6/debug-tab/README.md` (to be added) links to this ADR as the first paragraph.
- The skeleton's `IDebugTabViewModel` is the data surface for any future autosave / export / virtualised-display follow-up. Subsequent ADRs that touch the Debug tab cite this one.
- This ADR feeds the pitch slide for slot 5 (or slot 6 if WE1 takes both) — the "what we deferred" half-slide reads directly from the decisions table.
- The two open questions (`source` field, autosave) are surfaced in [`after-trace.md` → Open questions](../../../refactoring-examples/sub-team-6/debug-tab/after-trace.md#open-question-source-field) so a reader arriving at the code (not the ADR) still sees them.

## Alternatives considered

- **Thread every BEFORE behaviour into the WE2 skeleton (capture + autosave + rotation + manual export + AutoScrollEnabled + virtualised list).** Most faithful, most defensive against the "where did X go?" question. Rejected: scope creep on a design-only deliverable; the Observer/ACL pattern would be buried under autosave coroutines and rotation logic. The skeleton would stop being a proof and start being a re-implementation of `DebugLogging` with extra interfaces.
- **Add `Source` to `LogEntry` and `ILogStream.Publish` now.** Aligns the implementation with the `log-origin-trace.md` analysis. Rejected (for WE2 deliverable only): no consumer needs it yet; the capture pathway through `UnityLogStreamAdapter.OnUnityLog` cannot recover the source string (Unity doesn't expose the caller), so the field would carry data only for structured callers — of which there are zero today. Decision deferred to the first consumer that asks for it; the schema change is one line.
- **Patch `DebugLogging.cs` directly to add an interface and a backing list.** Most "satisfying" engineering response. Rejected: CLAUDE.md mandates design-only — "Do not refactor production scripts under `Assets/`." The proposal cites the structural defects as motivation; the new architecture *replaces them by construction* (S1 contained behind `UnityLogStreamAdapter`, S3 eliminated by `MaxEntries` cap, S8 eliminated by the five-way class split); production code stays untouched until the panel approves the refactor.
- **Add a stop-gap `FileLogObserver` to preserve the autosave feature now.** Would close the autosave regression at the cost of pre-empting Sub-team 7's storage schema decision. Rejected unless Sub-team 7 confirms the schema would not be invalidated. The `ILogObserver` shape is stable; a stop-gap can be added later without breaking the skeleton.
- **Keep `DebugLogging` and only extract the Observer interface around it.** Cheapest path: rename `DebugLogging` to implement `ILogStream`, leave the MonoBehaviour in place. Rejected: does not eliminate smell S8 (four concerns still in one class) and does not satisfy Section 4.2 (no domain assembly emerges). The MVVM + ACL pattern is the panel-facing deliverable; an adapter-only extraction would not be a worked example of that pattern.

## References

- [ADR-0001 — MVVM split](./0001-mvvm-split.md) — parent decision; this ADR scopes one of its worked examples.
- ADR-0003 — Anti-corruption layer (reserved; referenced from `DebugTabViewModel.cs` and `UnityLogStreamAdapter.cs` comment headers).
- [ADR-0004 — WE1 (File tab) scope boundary](./0004-we1-file-tab-scope-boundary.md) — sibling decision; same structure, same level of artefact citation.
- [WE2 — Debug tab skeleton](../../../refactoring-examples/sub-team-6/debug-tab/) — the artefact this boundary governs.
- [`before-trace.md`](../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md) — production behaviour traced (S1–S9 smell anchors for each line citation above).
- [`log-origin-trace.md`](../../../refactoring-examples/sub-team-6/debug-tab/log-origin-trace.md) — catalogue of 44 producer call sites; argument for the `source` field deferred above.
- [`after-trace.md`](../../../refactoring-examples/sub-team-6/debug-tab/after-trace.md) — AFTER call path; open questions section restates the two deferrals above.
- [`class-diagram.md`](../../../refactoring-examples/sub-team-6/debug-tab/class-diagram.md), [`dependency-graph.md`](../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md), [`ck-metrics.md`](../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) — structural and numeric evidence.
- `Assets/Scripts/Debuggers/DebugLogging.cs` — line citations: 49 / 68-112 / 124 / 149 / 177-196 / 203-242 / 250-253.
- 44 producer call sites — see `log-origin-trace.md` categories E (errors), W (warnings), I (info), V (validation, eliminated).
