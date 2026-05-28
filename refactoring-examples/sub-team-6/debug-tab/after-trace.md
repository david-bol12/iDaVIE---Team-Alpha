# Debug tab — AFTER trace ("Log line emitted → visible in Debug tab" via Observer + MVVM)

## TL;DR

Observer pattern + MVVM + ACL boundary. Four phases: **A** `CompositionRoot.Awake()` wires VM → adapter → View; **B** any `Debug.Log*` call (44 sites, **unchanged**) hits `UnityLogStreamAdapter` — the only class that touches `Application.logMessageReceived` — which normalises `LogType → LogLevel` and calls `LogStream.Publish` (timestamp captured here); `LogStream` snapshots its observer list under lock and dispatches `LogEntry` records to each `ILogObserver`; `DebugTabViewModel` appends to a bounded `List<LogEntry>` (cap 2000) and raises `EntriesChanged`; `DebugTabView` rebuilds TMP text over a capped 500-line slice; **C** Clear button empties the list; **D** `Dispose` symmetrically `Unsubscribe`s. **Smells eliminated:** S2 (timestamp now captured), S3 (unbounded queue → bounded list), S4 (per-message file I/O gone), S8 (four concerns → five named classes). **Contained:** S1 (static event hook confined to one adapter), S5/S6 (TMP rebuild capped at 500 lines), S9 (44 callers captured automatically via the unchanged static event). **Remaining:** S7 — `_scrollbar.value = 0f` still fires on every `EntriesChanged`. Two open questions surfaced: should `LogEntry` carry a `source` field? should autosave come back as a separate `ILogObserver`?

---

Structural counterpart to [`before-trace.md`](before-trace.md). Every message below is anchored to a file and line in the skeleton (`skeleton/`) or adapter (`adapters/`) code that already lives in this folder, so the AFTER sequence diagram is defensible at the panel.

A Mermaid rendering lives in [`after-sequence.md`](after-sequence.md). Pairs with [`before-sequence.md`](before-sequence.md) for side-by-side panel display.

---

## Actors / lifelines

| Lifeline | Backing type | Notes |
|---|---|---|
| `User` | — | Desktop operator |
| `AnySubsystem` | Any `MonoBehaviour` or static class | Existing callers of `UnityEngine.Debug.Log*` — **unchanged from BEFORE**. The refactor does not require touching any of the 44 call sites catalogued in [`log-origin-trace.md`](log-origin-trace.md). |
| `Application` | Static Unity class | Owns `logMessageReceived`. Now subscribed to by **one** adapter, not by the debug-tab class. |
| `LogStreamAdapter` | `adapters/UnityLogStreamAdapter.cs` | The only class that touches `Application.logMessageReceived`. Translates `LogType` → `LogLevel`, publishes to its inner `LogStream`. |
| `LogStream` | `skeleton/LogStream.cs` | Pure C#. Thread-safe observer list (`lock` + array snapshot). Dispatches each entry to all `ILogObserver`s. |
| `DebugTabVM` | `skeleton/DebugTabViewModel.cs` | Pure C# ViewModel. Implements `IDebugTabViewModel`, `ILogObserver`, `IDisposable`. Caps backing list at 2000 entries. |
| `DebugTabView` | `adapters/DebugTabView.cs` | Thin Unity MonoBehaviour. Subscribes to `EntriesChanged`, rebuilds TMP text from the last 500 entries on each notification. |
| `CompositionRoot` | `adapters/DebugTabCompositionRoot.cs` | The single class permitted to reference both Unity and skeleton assemblies. Wires VM → adapter → View in `Awake`; disposes VM in `OnDestroy`. |

The ACL boundary is the vertical line between the *interfaces* (`ILogStream`, `ILogObserver`, `IDebugTabViewModel`) and the *adapters* (`UnityLogStreamAdapter`, `DebugTabView`, `CompositionRoot`). The VM and `LogStream` sit entirely to the left of it.

---

## Phase A — Startup (CompositionRoot wires the graph)

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| A1 | Unity calls `DebugTabCompositionRoot.Awake()` | `DebugTabCompositionRoot.cs:30` | Replaces `DebugLogging.Start()` (BEFORE A1). All wiring lives here, not interleaved with log-rotation, autosave, and inspector lookups. |
| A2 | `CompositionRoot` constructs `new DebugTabViewModel(_logStreamAdapter)` | `DebugTabCompositionRoot.cs:32` | VM is constructor-injected with the adapter as `ILogStream`. No `FindObjectOfType`, no `PlayerPrefs`, no log-rotation logic on the construction path. |
| A3 | VM constructor calls `_logStream.Subscribe(this)` | `DebugTabViewModel.cs:34` | The VM registers itself as an `ILogObserver` on the stream. Replaces BEFORE B2 (`Application.logMessageReceived += HandleLog`). |
| A4 | `CompositionRoot` calls `_view.BindTo(_vm)` | `DebugTabCompositionRoot.cs:33` | View subscribes to `EntriesChanged`, wires the Clear button. Replaces BEFORE A5 (`saveButton.onClick.AddListener` in `Start`). |
| A5 | `DebugTabView.BindTo` adds `_vm.EntriesChanged += OnEntriesChanged` and `_clearButton.onClick.AddListener(_vm.ClearEntries)` | `DebugTabView.cs:42-47` | All wiring is code-side. No Inspector-wired event handlers, no `transform.Find`. |
| A6 | `DebugTabView.BindTo` calls `OnEntriesChanged()` once to render the initial (empty) state | `DebugTabView.cs:49` | Idempotent initial render. No special "first paint" path. |

**At end of Phase A:** the object graph `CompositionRoot → VM → LogStreamAdapter → inner LogStream` is live; the View is bound; the VM is registered as an observer. No log line has yet been emitted.

---

## Phase B — A log line is emitted (the main flow)

This is the sequence that fires for every one of the 44 call sites catalogued in [`log-origin-trace.md`](log-origin-trace.md). **None of those call sites need to change** — the adapter captures Unity's existing global log event.

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| B1 | `AnySubsystem` calls `Debug.Log(message)` / `Debug.LogWarning(message)` / `Debug.LogError(message)` | Various (e.g. `CanvassDesktop.cs:351, 844, 1024`) — **unchanged** | Existing call sites untouched. The smell is *contained*, not eliminated — see S9 below. |
| B2 | Unity runtime fires `Application.logMessageReceived(message, stackTrace, type)` | Unity internals | Same static event as BEFORE C2 — but now only **one** subscriber sits on it. |
| B3 | `UnityLogStreamAdapter.OnUnityLog(message, stackTrace, type)` invoked | `UnityLogStreamAdapter.cs:34` | Replaces `DebugLogging.HandleLog` (BEFORE C3). The adapter is the only class permitted to know about `LogType`. |
| B4 | Adapter normalises `LogType` → `LogLevel` (`Warning`/`Error`/`Exception`/`Assert` → strong levels; everything else → `Info`) | `UnityLogStreamAdapter.cs:36-43` | Replaces BEFORE C4 (string concatenation `"[" + type + "] : " + logString`). The level is now a typed enum, preserved end-to-end. |
| B5 | Adapter calls `_inner.Publish(level, message)` | `UnityLogStreamAdapter.cs:44` | **★ The replacement.** No `Queue` enqueue, no `StreamWriter`, no `StringBuilder` rebuild. The adapter's job ends here. |
| B6 | `LogStream.Publish` constructs `new LogEntry(level, message, DateTime.UtcNow)` | `LogStream.cs:36` | **Timestamp captured here**, in domain code. Was missing entirely in BEFORE (`HandleLog` discarded `LogType` and never captured time). |
| B7 | `LogStream` snapshots its observer list under `lock`, then iterates the snapshot outside the lock | `LogStream.cs:37-40` | Thread-safe dispatch: an observer that unsubscribes during `OnNext` cannot mutate the iteration. Replaces the BEFORE state where there was no thread-safety contract at all. |
| B8 | For each observer: `observer.OnNext(entry)` | `LogStream.cs:39-40`, `ILogObserver.cs:14` | The Observer pattern made explicit. New consumers (autosave, file export, network telemetry) attach as `ILogObserver` without touching the producer side. |
| B9 | `DebugTabViewModel.OnNext(entry)` delegates to `AppendEntry(entry)` | `DebugTabViewModel.cs:65` | The `ILogObserver` implementation is explicit-interface — `AppendEntry` remains the public method. |
| B10 | `AppendEntry` appends to `_entries` (generic `List<LogEntry>`) | `DebugTabViewModel.cs:47` | **★ Eliminates BEFORE C5** (non-generic `Queue` storing `object`). Type-safe, generic, bounded. |
| B11 | If `_entries.Count > MaxEntries (2000)`, removes index 0 | `DebugTabViewModel.cs:49-50` | **★ Eliminates BEFORE S3** (unbounded queue growth). Memory ceiling is now O(2000) entries regardless of session length. |
| B12 | `AppendEntry` raises `EntriesChanged?.Invoke()` | `DebugTabViewModel.cs:51` | Single notification per entry. Replaces BEFORE C9 (O(N) `StringBuilder` rebuild *inside* the handler). |
| B13 | `DebugTabView.OnEntriesChanged` re-reads `_vm.LogEntries`; rebuilds TMP text over the **last 500 entries** | `DebugTabView.cs:62-82` | **Contained smell.** Still O(N) text rebuild on every entry, but N is now capped at 500 (`MaxDisplayLines`). With a level-coloured `<color=...>` span per entry. |
| B14 | View sets `_logText.text = sb.ToString()` and `_scrollbar.value = 0f` | `DebugTabView.cs:82-83` | Text replacement still happens, but only over the capped slice. Scroll behaviour preserved from BEFORE — listed in [Remaining smells](#smells-eliminated-contained-or-remaining) (S7). |
| B15 | **[GUI WALK]** New log line appears at the bottom of the Debug tab panel | Scene | Walkthrough: same UX as BEFORE C12, but with colour-coded level (warning = yellow, error = red, info = white). |

**At end of Phase B:** the new entry is visible. No `StreamWriter` was opened, no main-thread file I/O occurred, no unbounded buffer grew. The full path involved exactly **one** Unity-side event subscription, regardless of how many downstream consumers (`DebugTabVM`, future autosave, future telemetry) are registered.

---

## Phase C — User clears the log

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| C1 | User clicks **Clear** in the Debug tab | `DebugTabView` Inspector binding to `_clearButton` | Replaces BEFORE D1 (Save button — see [Open question: Save/autosave](#open-question-saveautosave)). |
| C2 | `_clearButton.onClick → _vm.ClearEntries` | `DebugTabView.cs:47` | Code-side subscription set in `BindTo`. |
| C3 | `DebugTabViewModel.ClearEntries()` empties `_entries` and raises `EntriesChanged` | `DebugTabViewModel.cs:55-58` | Single mutation point. No direct view manipulation. |
| C4 | `DebugTabView.OnEntriesChanged` rebuilds (now empty) TMP text | `DebugTabView.cs:62` | Driven by the same event path as B13 — no special "clear" rendering code. |

---

## Phase D — Scene teardown (lifetime hygiene)

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| D1 | Unity calls `DebugTabCompositionRoot.OnDestroy()` | `DebugTabCompositionRoot.cs:36` | Replaces BEFORE's implicit `OnDisable` unsubscription (`DebugLogging.cs:153` in the original). |
| D2 | `CompositionRoot` calls `_vm?.Dispose()` | `DebugTabCompositionRoot.cs:40` | Explicit lifetime hand-off. |
| D3 | `DebugTabViewModel.Dispose()` calls `_logStream.Unsubscribe(this)`; sets `_disposed = true` (idempotent) | `DebugTabViewModel.cs:70-75` | Symmetric with constructor's `Subscribe`. Prevents "dead observer" leaks across hot-reload / scene reload, which the BEFORE design accidentally got right via `OnDisable` but not via any tested contract. |
| D4 | Adapter's `OnDisable` removes its handler from `Application.logMessageReceived` | `UnityLogStreamAdapter.cs:31-32` | Symmetric with `OnEnable`. The static event has exactly zero subscribers after teardown. |

---

## Smells eliminated, contained, or remaining

Mapped to the smell IDs from [`before-trace.md`](before-trace.md):

| ID | Smell (BEFORE) | Status | Where it now lives |
|---|---|---|---|
| S1 | `Application.logMessageReceived` hook untestable | **contained** | One subscription inside `UnityLogStreamAdapter.OnEnable` (`UnityLogStreamAdapter.cs:28-29`). The VM is testable because it depends on `ILogStream`, not the static event. |
| S2 | Unstructured `(string, string, LogType)`; no source, no timestamp | **partially eliminated** | `LogEntry` (skeleton/ILogStream.cs:31) now carries `Level + Message + Timestamp` as a typed record. **`Source` is still missing** — see [Open question: source field](#open-question-source-field). |
| S3 | Non-generic `Queue`, unbounded memory growth | **eliminated** | Generic `List<LogEntry>` with `MaxEntries = 2000` cap (`DebugTabViewModel.cs:22, 49-50`). |
| S4 | `StreamWriter` open/close per message | **eliminated** | Autosave is not in the WE2 scope. If reintroduced, it would attach as an `ILogObserver` (`ILogObserver.cs:14`) holding a single long-lived writer — see [Open question: Save/autosave](#open-question-saveautosave). |
| S5 | O(N) `StringBuilder` rebuild over entire history on every message | **contained** | Still O(N) per entry, but N is capped at `MaxDisplayLines = 500` (`DebugTabView.cs:35, 62-82`). Fix vector: TMP virtualised list — out of scope. |
| S6 | `TMP_InputField.text` replaced entirely every message | **contained** | Same mechanism survives (`DebugTabView.cs:82`), bounded by the 500-line slice. Same fix vector as S5. |
| S7 | Scroll forced to bottom every message — user cannot scroll up while logging | **remaining** | `_scrollbar.value = 0f` still runs on every `OnEntriesChanged` (`DebugTabView.cs:83`). No `AutoScrollEnabled` toggle on `IDebugTabViewModel` yet. Listed in [Known limitations](#known-limitations). |
| S8 | Four responsibilities in one `MonoBehaviour` (capture · store · display · export) | **eliminated** | Now split: `UnityLogStreamAdapter` (capture), `LogStream` (storage/dispatch), `DebugTabViewModel` (selection/cap), `DebugTabView` (display), `CompositionRoot` (wiring). Export = follow-up observer. |
| S9 | 44 unstructured `UnityEngine.Debug.Log*` call sites | **contained** | Captured automatically via `Application.logMessageReceived` without modifying any caller. The structured `ILogStream.Publish(level, message)` pathway exists for new callers (`ILogStream.cs:13`) but is not yet adopted in production code — listed in [Open question: source field](#open-question-source-field). |

---

## Open question: `source` field

[`log-origin-trace.md`](log-origin-trace.md) catalogues 44 call sites and argues the contract should be `Publish(LogLevel, string source, string message)` so the Debug tab can filter by `"FileTab"` / `"VolumeLoader"` / `"HistogramController"` / `"SourcesTab"` / `"DataAnalysis"`. The implemented contract is `Publish(LogLevel, string)` — no `source`.

This is deliberately surfaced rather than papered over. Two paths forward:

1. **Add `source` to `ILogStream.Publish` and `LogEntry`.** Required only for the `ILogStream.Publish(...)` path (direct, structured callers). Captures via `Application.logMessageReceived` cannot recover the source string — Unity does not expose the caller. Worth the schema change only if at least one consumer (e.g. a level-filter or a per-tab error counter) actually uses it.
2. **Defer.** `LogEntry` is an immutable `record` — adding `Source` is a single-line change later. Until a consumer needs it, the field would be unused metadata.

Decision belongs in the desktop client architecture document.

---

## Open question: Save/autosave

`DebugLogging.cs` (BEFORE) had two file-related responsibilities: log-rotation on startup and `StreamWriter`-per-message autosave during `HandleLog`. Neither survives in the AFTER skeleton — there is no autosave path through `LogStream`.

This is intentional for the worked example (the WE2 scope is capture → display, not persistence). If autosave is required in the final architecture, the right shape is:

- A `FileLogObserver : ILogObserver` (one long-lived `StreamWriter`, opened in constructor, closed in `Dispose`).
- Subscribed by `CompositionRoot` at `Awake`, disposed in `OnDestroy`.
- Sees the same `LogEntry` stream as `DebugTabViewModel` — no special hook.

Adding this is one ~30-line class plus one composition-root line. The interface does not change.

---

## Known limitations

Items the panel should expect questions on — surfaced honestly rather than hidden:

1. **Scroll-pin-while-reading (S7) is not fixed.** `DebugTabView` resets the scrollbar to the bottom on every `EntriesChanged`. Adding an `AutoScrollEnabled` property to `IDebugTabViewModel` and gating the `_scrollbar.value = 0f` line on it is ~5 lines of code — but it would change the public interface and warrant a test, so it is listed as a follow-up.
2. **`source` field is not in `LogEntry`.** Captures via `Application.logMessageReceived` cannot recover it; structured `Publish` callers do not yet exist. See [Open question: source field](#open-question-source-field).
3. **TMP text is rebuilt on every entry (S5/S6).** Capped at 500 lines, so practical impact is bounded — but a virtualised list (Unity UI Toolkit `ListView`) would be the structural fix. Out of scope for WE2.
4. **`UnityLogStreamAdapter` and the existing `Debug.Log` call sites are two parallel pathways into `LogStream`.** Today only the capture pathway is active. A future cleanup pass could migrate high-value sites (e.g. P/Invoke errors `E1`–`E10` in `log-origin-trace.md`) to direct `ILogStream.Publish` for structured filtering — but this is a code change to 10+ files, deferred to post-pitch.

All four items live entirely behind the existing interfaces and require no test changes to the 29 existing debug-tab tests.

---

## How this becomes the Mermaid diagram

See [`after-sequence.md`](after-sequence.md). The conversion follows the same rules as the file-tab pair:

- Phases A (startup) and B (log emitted) are drawn as one continuous diagram with a `Note` separator labelled "wiring complete — log subscription live".
- The ACL boundary is rendered as a `box` around `[UnityLogStreamAdapter, DebugTabView, CompositionRoot, Application]` — every message *into* the box from `DebugTabVM` or `LogStream` goes through an interface.
- `activate` bars only on `LogStream` (during dispatch) and `DebugTabVM` (during `AppendEntry`). Phase C (clear) and Phase D (teardown) are drawn as small follow-on blocks.
- Contained smells (S5, S6, S7) are annotated with `Note right of DebugTabView` so the diagram is honest about what survives.
