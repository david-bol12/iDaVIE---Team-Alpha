# Debug tab — AFTER trace ("Log line emitted → visible in Debug tab" via Observer + MVVM)

## TL;DR

Observer pattern + MVVM + ACL boundary. Four phases: **A** `CompositionRoot.Awake()` wires VM → adapter → View; **B** server pushes a `log.emit` JSON-RPC notification (ADR-0002), `GatewayLogStreamAdapter` filters and deserialises the payload (level, message, ISO-8601 timestamp preserved end-to-end), calls `LogStream.Publish`; `LogStream` snapshots its observer list under lock and dispatches `LogEntry` records to each `ILogObserver`; `DebugTabViewModel` appends to a bounded `List<LogEntry>` (cap 2000) and raises `EntriesChanged`; `DebugTabView` rebuilds TMP text over a capped 500-line slice; **C** Clear button empties the list; **D** `Dispose` symmetrically unsubscribes. **Smells eliminated:** S1 (static event hook gone — injectable `IServiceGateway`), S2 (timestamp preserved end-to-end from server), S3 (unbounded queue → bounded list), S4 (per-message file I/O gone), S8 (four concerns → five named classes). **Contained:** S5/S6 (TMP rebuild capped at 500 lines). **Not captured:** S9 (44 client-side `Debug.Log*` callers — gateway receives server logs only; a bridge adapter can be added later as a second `ILogObserver`). **Remaining:** S7 — `_scrollbar.value = 0f` still fires on every `EntriesChanged`. Two open questions surfaced: should `LogEntry` carry a `source` field? should autosave come back as a separate `ILogObserver`?

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
| `LogStreamAdapter` | `adapters/GatewayLogStreamAdapter.cs` | Subscribes to `IServiceGateway.OnNotification` on construction. Filters `log.emit` notifications, deserialises the JSON payload (`level`, `msg`, `ts`), republishes via the inner `LogStream`. No `UnityEngine` dependency — testable via `FakeGateway`. |
| `LogStream` | `skeleton/LogStream.cs` | Pure C#. Thread-safe observer list (`lock` + array snapshot). Dispatches each entry to all `ILogObserver`s. |
| `DebugTabVM` | `skeleton/DebugTabViewModel.cs` | Pure C# ViewModel. Implements `IDebugTabViewModel`, `ILogObserver`, `IDisposable`. Caps backing list at 2000 entries. |
| `DebugTabView` | `adapters/DebugTabView.cs` | Thin Unity MonoBehaviour. Subscribes to `EntriesChanged`, rebuilds TMP text from the last 500 entries on each notification. |
| `CompositionRoot` | `adapters/DebugTabCompositionRoot.cs` | The single class permitted to reference both Unity and skeleton assemblies. Wires VM → adapter → View in `Awake`; disposes VM in `OnDestroy`. |

The ACL boundary is the vertical line between the *interfaces* (`ILogStream`, `ILogObserver`, `IDebugTabViewModel`) and the *adapters* (`GatewayLogStreamAdapter`, `DebugTabView`, `CompositionRoot`). The VM and `LogStream` sit entirely to the left of it.

---

## Phase A — Startup (CompositionRoot wires the graph)

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| A1 | Unity calls `DebugTabCompositionRoot.Awake()` | `DebugTabCompositionRoot.cs:47` | Replaces `DebugLogging.Start()` (BEFORE A1). All wiring lives here, not interleaved with log-rotation, autosave, and inspector lookups. |
| A2 | `CompositionRoot` constructs `new GatewayLogStreamAdapter(_gateway)` then `new DebugTabViewModel(_logStreamAdapter)` | `DebugTabCompositionRoot.cs:53-54` | VM is constructor-injected with the adapter as `ILogStream`. No `FindObjectOfType`, no `PlayerPrefs`, no log-rotation logic on the construction path. |
| A3 | VM constructor calls `_logStream.Subscribe(this)` | `DebugTabViewModel.cs:34` | The VM registers itself as an `ILogObserver` on the stream. Replaces BEFORE B2 (`Application.logMessageReceived += HandleLog`). |
| A4 | `CompositionRoot` calls `_view.BindTo(_vm)` | `DebugTabCompositionRoot.cs:55` | View subscribes to `EntriesChanged`, wires the Clear button. Replaces BEFORE A5 (`saveButton.onClick.AddListener` in `Start`). |
| A5 | `DebugTabView.BindTo` adds `_vm.EntriesChanged += OnEntriesChanged` and `_clearButton.onClick.AddListener(_vm.ClearEntries)` | `DebugTabView.cs:45,47` | All wiring is code-side. No Inspector-wired event handlers, no `transform.Find`. |
| A6 | `DebugTabView.BindTo` calls `OnEntriesChanged()` once to render the initial (empty) state | `DebugTabView.cs:49` | Idempotent initial render. No special "first paint" path. |

**At end of Phase A:** the object graph `CompositionRoot → VM → LogStreamAdapter → inner LogStream` is live; the View is bound; the VM is registered as an observer. No log line has yet been emitted.

---

## Phase B — A log entry arrives (the main flow)

The trigger is a **server-pushed `log.emit` JSON-RPC notification** arriving on the named-pipe transport (ADR-0002). The 44 client-side `Debug.Log*` call sites catalogued in [`log-origin-trace.md`](log-origin-trace.md) are **not** captured via this pathway — see S9 below.

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| B1 | Server pushes a `log.emit` notification: `{"jsonrpc":"2.0","method":"log.emit","params":{"level":"WARN","msg":"...","ts":"2026-05-21T09:14:02Z"}}` | ADR-0002 §"Message catalogue" | Structured, typed, timestamped from the point of origin. Replaces the unstructured `(string, string, LogType)` tuple of BEFORE C2–C4. |
| B2 | `IServiceGateway` fires `OnNotification` event; all registered handlers are invoked | `IServiceGateway.cs` (contracts-team1) | Observable interface — injectable, testable via `FakeGateway`. Replaces the static `Application.logMessageReceived` event of BEFORE B2. |
| B3 | `GatewayLogStreamAdapter.OnGatewayNotification(notification)` invoked | `GatewayLogStreamAdapter.cs:64` | Replaces `DebugLogging.HandleLog` (BEFORE C3). The adapter is the only class that knows the wire format. |
| B4 | Filter: if `notification.Method != "log.emit"`, return immediately | `GatewayLogStreamAdapter.cs:66` | Other gateway notifications (e.g. `progress.update`) arrive on the same fan-out and must not leak into the log stream — tested in `GatewayLogStreamAdapterTests`. |
| B5 | Deserialise `notification.Params` → `LogEmitParams(Level, Msg, Ts)` | `GatewayLogStreamAdapter.cs:69` | Replaces BEFORE C4 (string concatenation). Level is a wire string mapped to a typed enum; timestamp is ISO-8601. |
| B6 | `ParseLevel(payload.Level)`: `"WARN"/"WARNING"` → `Warning`; `"ERROR"/"ERR"/"FATAL"` → `Error`; anything else → `Info` | `GatewayLogStreamAdapter.cs:85-96` | Lenient mapping — unknown future server levels do not drop the entry. |
| B7 | Parse `payload.Ts` as ISO-8601 UTC; fall back to `DateTime.UtcNow` if missing or malformed | `GatewayLogStreamAdapter.cs:76-80` | **★ Timestamp preserved end-to-end** — the server-emitted time survives to `LogEntry.Timestamp` unchanged. Was missing entirely in BEFORE. |
| B8 | `_inner.Publish(level, payload.Msg, ts)` — delegates to inner `LogStream` | `GatewayLogStreamAdapter.cs:82` | **★ The replacement.** No `Queue` enqueue, no `StreamWriter`, no `StringBuilder` rebuild. The adapter's job ends here. |
| B9 | `LogStream.Publish` constructs `new LogEntry(level, message, timestamp)` | `LogStream.cs:39` | Immutable value-object record — type-safe, timestamp-carrying. |
| B10 | `LogStream` snapshots observer list under `lock`, iterates outside the lock | `LogStream.cs:41-42` | Thread-safe dispatch: an observer that unsubscribes during `OnNext` cannot corrupt the iteration. Replaces the BEFORE state where there was no thread-safety contract at all. |
| B11 | For each observer: `observer.OnNext(entry)` | `LogStream.cs:43-44`, `ILogObserver.cs:14` | Observer pattern. Additional consumers (autosave, telemetry) attach as `ILogObserver` without modifying the producer. |
| B12 | `DebugTabViewModel.OnNext(entry)` delegates to `AppendEntry(entry)` | `DebugTabViewModel.cs:65` | Explicit-interface implementation — `AppendEntry` is the public method; `ILogObserver` is an internal subscription concern. |
| B13 | `AppendEntry` appends to `_entries` (generic `List<LogEntry>`) | `DebugTabViewModel.cs:47` | **★ Eliminates BEFORE C5** (non-generic `Queue` storing `object`). Type-safe, generic, bounded. |
| B14 | If `_entries.Count > MaxEntries (2000)`, removes index 0 | `DebugTabViewModel.cs:49-50` | **★ Eliminates BEFORE S3** (unbounded queue growth). Memory ceiling is now O(2000) entries regardless of session length. |
| B15 | `AppendEntry` raises `EntriesChanged?.Invoke()` | `DebugTabViewModel.cs:51` | Single notification per entry. Replaces BEFORE C9 (O(N) `StringBuilder` rebuild *inside* the handler). |
| B16 | `DebugTabView.OnEntriesChanged` re-reads `_vm.LogEntries`; rebuilds TMP text over the **last 500 entries** | `DebugTabView.cs:62-82` | **Contained smell.** Still O(N) text rebuild on every entry, but N is now capped at 500 (`MaxDisplayLines`). Colour-coded by level. |
| B17 | View sets `_logText.text = sb.ToString()` and `_scrollbar.value = 0f` | `DebugTabView.cs:82-83` | Text replacement bounded by the 500-line slice. Scroll behaviour preserved — see S7 in [Remaining smells](#smells-eliminated-contained-or-remaining). |
| B18 | **[GUI WALK]** New log line appears at the bottom of the Debug tab panel | Scene | Colour-coded: warning = yellow, error = red, info = white. Same UX as BEFORE C12. |

**At end of Phase B:** the new entry is visible. No `StreamWriter` was opened, no file I/O occurred, no unbounded buffer grew. The full path involved exactly **one** gateway notification subscription (`GatewayLogStreamAdapter`), regardless of how many downstream observers (`DebugTabVM`, future autosave, future telemetry) are registered.

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
| D4 | `GatewayLogStreamAdapter.Dispose()` removes its `OnNotification` handler from the gateway | `GatewayLogStreamAdapter.cs:121` | `_gateway.OnNotification -= _handler`. The gateway has zero `log.emit` listeners after teardown. Replaces BEFORE's `OnDisable` unsubscription from `Application.logMessageReceived`. |

---

## Smells eliminated, contained, or remaining

Mapped to the smell IDs from [`before-trace.md`](before-trace.md):

| ID | Smell (BEFORE) | Status | Where it now lives |
|---|---|---|---|
| S1 | `Application.logMessageReceived` hook untestable | **eliminated** | `GatewayLogStreamAdapter` subscribes to `IServiceGateway.OnNotification` — an injectable interface. No static event. Testable via `FakeGateway` without a Unity runtime (`GatewayLogStreamAdapterTests`). |
| S2 | Unstructured `(string, string, LogType)`; no source, no timestamp | **partially eliminated** | `LogEntry` (skeleton/ILogStream.cs:31) now carries `Level + Message + Timestamp` as a typed record. **`Source` is still missing** — see [Open question: source field](#open-question-source-field). |
| S3 | Non-generic `Queue`, unbounded memory growth | **eliminated** | Generic `List<LogEntry>` with `MaxEntries = 2000` cap (`DebugTabViewModel.cs:22, 49-50`). |
| S4 | `StreamWriter` open/close per message | **eliminated** | Autosave is not in the WE2 scope. If reintroduced, it would attach as an `ILogObserver` (`ILogObserver.cs:14`) holding a single long-lived writer — see [Open question: Save/autosave](#open-question-saveautosave). |
| S5 | O(N) `StringBuilder` rebuild over entire history on every message | **contained** | Still O(N) per entry, but N is capped at `MaxDisplayLines = 500` (`DebugTabView.cs:35, 62-82`). Fix vector: TMP virtualised list — out of scope. |
| S6 | `TMP_InputField.text` replaced entirely every message | **contained** | Same mechanism survives (`DebugTabView.cs:82`), bounded by the 500-line slice. Same fix vector as S5. |
| S7 | Scroll forced to bottom every message — user cannot scroll up while logging | **remaining** | `_scrollbar.value = 0f` still runs on every `OnEntriesChanged` (`DebugTabView.cs:83`). No `AutoScrollEnabled` toggle on `IDebugTabViewModel` yet. Listed in [Known limitations](#known-limitations). |
| S8 | Four responsibilities in one `MonoBehaviour` (capture · store · display · export) | **eliminated** | Now split: `UnityLogStreamAdapter` (capture), `LogStream` (storage/dispatch), `DebugTabViewModel` (selection/cap), `DebugTabView` (display), `CompositionRoot` (wiring). Export = follow-up observer. |
| S9 | 44 unstructured `UnityEngine.Debug.Log*` call sites | **not captured (by design)** | The gateway adapter receives server-pushed `log.emit` notifications only — client-side `Debug.Log*` calls do not appear in the debug tab under this design. The structured `ILogStream.Publish(level, message)` pathway (`ILogStream.cs:13`) exists for new callers. A `UnityLogStreamAdapter` bridging `Application.logMessageReceived` could be added as a second `ILogObserver` without changing any existing interface. |

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

5. **`DebugTabViewModel._entries` has no synchronisation.** `LogStream.Publish` dispatches observers on whatever thread calls it — which is the `IServiceGateway` read-loop thread for `GatewayLogStreamAdapter`. `AppendEntry` mutates `_entries` on that thread while `LogEntries` may be read by the UI thread. A future `IUIDispatcher` injection (marshal to main thread before calling `AppendEntry`) would resolve this; until then, production builds with high-frequency logging carry a low-probability race. All 29 existing tests run single-threaded and are unaffected.

All five items live entirely behind the existing interfaces and require no test changes to the 29 existing debug-tab tests.

---

## How this becomes the Mermaid diagram

See [`after-sequence.md`](after-sequence.md). The conversion follows the same rules as the file-tab pair:

- Phases A (startup) and B (log emitted) are drawn as one continuous diagram with a `Note` separator labelled "wiring complete — log subscription live".
- The ACL boundary is rendered as a `box` around `[GatewayLogStreamAdapter, DebugTabView, CompositionRoot, IServiceGateway]` — every message *into* the box from `DebugTabVM` or `LogStream` goes through an interface.
- `activate` bars only on `LogStream` (during dispatch) and `DebugTabVM` (during `AppendEntry`). Phase C (clear) and Phase D (teardown) are drawn as small follow-on blocks.
- Contained smells (S5, S6, S7) are annotated with `Note right of DebugTabView` so the diagram is honest about what survives.
