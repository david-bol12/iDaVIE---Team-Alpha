# Debug Tab — In-Depth Explainer

## What the debug tab does

The debug tab shows a live scrolling log panel in the iDaVIE desktop UI. Every time any part of the application calls `Debug.Log / LogWarning / LogError`, that message appears in the panel. The user can scroll through it and save it to a file.

---

## CK Metrics — what each one actually means

These are the Chidamber–Kemerer object-oriented metrics. Here is what each one measures in plain terms and why the threshold exists.

**WMC — Weighted Methods per Class**
Counts the number of methods in a class. With unit weight (NOM-style) it is just a method count. With McCabe complexity weight, methods with lots of branching (if/for/catch) count more. The threshold is ≤ 20 for domain classes and ≤ 40 for adapters. A class with WMC=40 has either 40 simple methods or fewer, heavier ones — both signal "too much responsibility."

**DIT — Depth of Inheritance Tree**
How many levels of inheritance you are down from `System.Object`. `MonoBehaviour` alone adds 4 levels (`Object → Component → Behaviour → MonoBehaviour`). Threshold ≤ 4. High DIT means you inherit state and behaviour from many parent classes, making the class hard to reason about in isolation.

**NOC — Number of Children**
How many classes directly subclass this one. Threshold ≤ 5. High NOC means a class is a base that many others depend on — changes ripple.

**CBO — Coupling Between Objects**
Counts how many distinct types a class directly names in its implementation (excluding primitives like `int`/`string` and the class's own type). Threshold ≤ 14 for domain, ≤ 25 for adapters. CBO is the "import footprint" of a class. High CBO means changing any of those dependencies can break this class.

**RFC — Response For a Class**
WMC plus the number of distinct external methods called. It measures the total "reactive surface" — how many different things can this class invoke? Threshold ≤ 50.

**LCOM — Lack of Cohesion of Methods (Henderson-Sellers)**
Formula: `(M − avg_mA) / (M − 1)` where M = method count and avg_mA = average number of methods that access each instance field. Result is 0–1; ≤ 0.5 is the threshold. **This is the most important metric for the debug tab.** A score near 1.0 means the methods in the class barely share any fields — they are operating on disjoint data, which means the class is doing multiple independent jobs and should be split. A score near 0 means every method touches overlapping data — the class has one coherent concern.

---

## How the user is involved

In the **before** state:
- The user opens the Debug tab panel in the iDaVIE desktop window.
- Log messages appear automatically as any part of the app logs things.
- The panel scrolls to the bottom on every single new message — even if the user has scrolled up to read something.
- The user can click **Save** to get a native file-picker dialog and export the log to disk.

In the **after** state:
- Same visual experience — log messages appear, the panel scrolls to bottom.
- **New:** the user can toggle `AutoScrollEnabled` off to freeze the scroll position and read historical entries while new ones continue arriving. The before state made this impossible.
- **New:** a **Clear** button empties the panel.
- **Save** is out of scope for this worked example but described as a future `ILogObserver` attachment.

---

## The system BEFORE — `DebugLogging.cs` (255 lines, one class)

The entire debug tab was a single `MonoBehaviour` called `DebugLogging` at `Assets/Scripts/Debuggers/DebugLogging.cs`. It had four completely separate jobs squeezed into one class:

**Job 1 — Startup wiring (`Start()`, 93 LOC, CC=9)**
Read `Config.Instance.numberOfLogsToKeep` (singleton), build a path for the autosave log file under `Application.dataPath/Outputs/Logs/`, run a log-rotation loop that renames existing files (`Log_1` → `Log_2`, delete the oldest), wire the Save button via `saveButton.onClick.AddListener(saveToFileClick)`. This method alone has cyclomatic complexity 9 — more than all other methods combined.

**Job 2 — Log capture (`OnEnable` / `OnDisable` / `HandleLog`)**
`OnEnable` subscribes to `Application.logMessageReceived += HandleLog`. This is a **static Unity event** — a process-global singleton. Every `Debug.Log*` call anywhere in the process fires this handler. `HandleLog` received an unstructured `(string logString, string stackTrace, LogType type)` tuple. It formatted the message as `"[" + type + "] : " + logString` (no timestamp captured) and `Enqueue`d it into a non-generic `Queue` (stores `object`, no type safety, no size limit).

**Job 3 — Display (`HandleLog` continued)**
After enqueueing, it iterated the entire queue via `foreach` and `StringBuilder.Append` — an O(N) rebuild of the whole log history on every single new message. With 1000 entries, message 1001 iterates all 1000 previous messages. Then it replaced the entire `TMP_InputField.text` string with the result (forcing Unity's TextMesh Pro to re-layout everything). Then it set `debugScrollbar.value = 1.0f` on every message with no condition — the user could never scroll up while logging was active.

**Job 4 — Autosave and manual export (`AutoSave`, `saveToFileClick`, `SaveToFile`)**
`AutoSave` opened a `new StreamWriter(autosavePath, append:true)`, wrote one line, then closed it — on every single log message. This is a file-system syscall per message on the Unity main thread. `saveToFileClick` read `PlayerPrefs.GetString("LastDebugPath")`, opened a `StandaloneFileBrowser` async dialog, and called `SaveToFile` in the callback.

**Why only LCOM failed:** The CK metrics measured `DebugLogging` at WMC=8, DIT=4, NOC=0, CBO≈10, RFC≈25 — all within thresholds. But LCOM was 0.95 (threshold ≤ 0.5). The computation: 6 instance fields, 8 methods. Average methods per field = 1.33. LCOM = (8 − 1.33) / (8 − 1) = **0.95**. The four concern clusters barely shared any fields — `OnEnable`/`OnDisable`/`DetermineHardware` shared zero fields with anything else. That is the metric signature of a class doing multiple unrelated jobs.

**The real problem CK couldn't capture:** `Application.logMessageReceived` is a static global event. CBO counts `Application` as just one collaborator. It does not know that collaborator is a non-substitutable singleton — you cannot inject a fake `Application` in a test. The result: **zero NUnit tests possible without a running Unity scene.**

### Smell inventory

| # | Smell | Lines | Principle violated |
|---|---|---|---|
| S1 | `Application.logMessageReceived` is a static global event — untestable, no interface | `DebugLogging.cs:149` | Dependency Inversion; testability |
| S2 | `HandleLog` receives unstructured `(string, string, LogType)` — no source, no timestamp | `DebugLogging.cs:177–179` | Information Expert |
| S3 | Non-generic `Queue` — no type safety, no capacity bound, unbounded memory growth | `DebugLogging.cs:49, 180` | Robustness |
| S4 | `StreamWriter` opened and closed per message in `AutoSave` | `DebugLogging.cs:250–253` | Performance; SRP |
| S5 | O(N) full-queue rebuild on every `HandleLog` call | `DebugLogging.cs:189–195` | Performance |
| S6 | `logOutput.text` replaced entirely every message | `DebugLogging.cs:195` | Modularity |
| S7 | Scroll forced to bottom every message — user cannot scroll up while logging | `DebugLogging.cs:196` | Usability |
| S8 | `DebugLogging` mixes: log capture, log storage, log display, and manual export | Whole class | Single Responsibility |
| S9 | `UnityEngine.Debug` callers (40+ in `CanvassDesktop` alone) have hidden transitive `UnityEngine` dependency | Various | Dependency Inversion; ACL rule |

---

## The system AFTER — Observer + MVVM + ACL boundary

The 255-line class is split into seven focused types across two assemblies.

### Domain assembly (`iDaVIE.Desktop.DebugTab`) — pure C#, zero Unity dependency

**`skeleton/ILogObserver.cs`** — one method: `void OnNext(LogEntry entry)`. This is the Observer interface. Any class that wants to receive log entries implements this. The ViewModel implements it. A future autosave class would implement it. They do not know about each other.

**`skeleton/ILogStream.cs`** — `Publish(level, message)` / `Publish(level, message, timestamp)` / `Subscribe(observer)` / `Unsubscribe(observer)`. This is the producer contract. Anything that wants to emit log entries calls `Publish`. The ViewModel calls `Subscribe` on this interface in its constructor — it never names the concrete `LogStream` class or the Unity adapter. Also defines the `LogLevel` enum (`Info`/`Warning`/`Error`) and the `LogEntry` immutable record (`Level + Message + Timestamp`).

**`skeleton/LogStream.cs`** — Thread-safe observer dispatch. Holds a `List<ILogObserver>` under a `lock`. On `Publish`: creates a `new LogEntry(level, message, DateTime.UtcNow)`, snapshots the observer list inside the lock, then iterates the snapshot *outside* the lock. This means an observer can call `Unsubscribe(itself)` inside `OnNext` without corrupting the loop. WMC=4, CBO=3, LCOM=25%.

**`skeleton/IDebugTabViewModel.cs`** — The ViewModel contract the View sees. Exposes `IReadOnlyList<LogEntry> LogEntries`, `void AppendEntry(LogEntry)`, `void ClearEntries()`, `event Action EntriesChanged`, `bool AutoScrollEnabled`. The View binds to this interface, not the concrete ViewModel.

**`skeleton/DebugTabViewModel.cs`** — The domain centrepiece. Implements `IDebugTabViewModel`, `ILogObserver`, and `IDisposable`. Constructor takes an `ILogStream` and calls `Subscribe(this)` on it immediately. Holds a `List<LogEntry>` with a hard cap of `MaxEntries = 2000` — when the cap is exceeded it removes index 0 (drops the oldest entry). `AppendEntry` adds the entry, enforces the cap, raises `EntriesChanged`. `ClearEntries` empties the list, raises `EntriesChanged`. `Dispose` calls `Unsubscribe(this)` so no entries arrive after teardown. WMC=6, CBO=2 (only `ILogStream` and `LogEntry`). CBO dropped from 9 (before) to 2. LCOM=66% (exceeds threshold but not a concern split — it is one class with slightly fragmented field access across three fields; the metric note in `ck-metrics.md` explains why this is an artifact, not an SRP violation).

### Adapters assembly (`iDaVIE.Desktop.Adapters.DebugTab`) — Unity-side

**`adapters/GatewayLogStreamAdapter.cs`** — The Anti-Corruption Layer. Implements `ILogStream`. On construction, subscribes to `IServiceGateway.OnNotification` — the server-pushed notification event from Sub-team 1's gateway. When a notification arrives, filters for method `"log.emit"`, deserialises the JSON payload (`{level, msg, ts}`), maps the wire-level string (`"WARN"`, `"WARNING"`, `"ERROR"`, `"ERR"`, `"FATAL"`, anything else → Info) to the typed `LogLevel` enum, parses the ISO-8601 timestamp (falls back to `DateTime.UtcNow` if malformed — keeps the entry), then calls `_inner.Publish(level, msg, ts)`. The inner `LogStream` is a private field — the adapter owns and wraps it. `Dispose` removes the `OnNotification` handler so no entries arrive after teardown. This is the only class that knows the wire format. The ViewModel sees only `ILogStream`. WMC=8, CBO=5, LCOM=72%.

**`adapters/DebugTabView.cs`** — Thin MonoBehaviour View. Has three Inspector-assigned fields (`TMP_Text _logText`, `Scrollbar _scrollbar`, `Button _clearButton`) and one private `IDebugTabViewModel? _vm`. `BindTo(vm)` subscribes to `vm.EntriesChanged`, wires `_clearButton.onClick → vm.ClearEntries`, calls `OnEntriesChanged()` once for the initial empty render. `OnEntriesChanged` reads `_vm.LogEntries`, takes the last `MaxDisplayLines = 500` entries, builds a `StringBuilder` with colour-coded TMP rich-text tags (`#FFFF00` for Warning, `#FF4444` for Error, `#FFFFFF` for Info), sets `_logText.text`. Then: `if (_vm.AutoScrollEnabled) _scrollbar.value = 0f`. The scroll-to-bottom is **gated** — S7 is eliminated. The O(N) rebuild still happens (S5/S6 contained, not eliminated) but N is capped at 500 regardless of session length. WMC=3, CBO=7, LCOM=41%.

**`adapters/DebugTabCompositionRoot.cs`** — The only class allowed to reference both Domain concrete types and Adapters at once. `Configure(IServiceGateway gateway)` must be called before `Awake` — the outer scene composition root owns ordering. `Awake` creates `new GatewayLogStreamAdapter(gateway)`, then `new DebugTabViewModel(_logStreamAdapter)`, then calls `_view.BindTo(_vm)`. `OnDestroy` disposes the VM first (unsubscribes from the stream), then disposes the adapter (detaches from the gateway). Dispose order matters — flipping it would mean the gateway could fire one more notification into a stream whose observer is already dead. WMC=3, CBO=6, LCOM=41%.

---

## Why each change was made

| Change | Reason |
|---|---|
| Replace `Application.logMessageReceived` with `IServiceGateway.OnNotification` | The static event was the root cause of zero testability. `IServiceGateway` is injectable — tests use `FakeGateway`. |
| `(string, string, LogType)` → `LogEntry(Level, Message, Timestamp)` record | Structured, typed, carries a timestamp. The before state never captured a timestamp at all. |
| Non-generic `Queue` → `List<LogEntry>` with cap 2000 | Type safety + bounded memory. An unbounded queue on a per-frame-capable log path is a memory leak waiting to happen. |
| Remove `new StreamWriter` per message | That was the performance time-bomb — one file-system syscall per log message on the main thread. Autosave reintroduced later as a `ILogObserver` with a single long-lived writer. |
| Cap display rebuild at 500 lines | O(N) over 500 is acceptable; O(N) over 50,000 is not. The structural fix (virtualised ListView) is out of scope but the interface is designed to support it. |
| `AutoScrollEnabled` property | Eliminates S7. The user can now scroll up to read while the log is still streaming. Defaults to `true` so existing behaviour is unchanged. |
| `LogStream` snapshot-under-lock dispatch | Makes concurrent subscribe/unsubscribe safe. The before state had no thread-safety contract. |
| `DebugTabCompositionRoot` Pure-DI | Replaces the `Start()` mess of `FindObjectOfType`, `AddListener`, log-rotation, and autosave path building all in one method. Each concern now lives in the class that owns it. |
| Domain assembly has zero `UnityEngine` references | Section 4.2 of the assignment spec is a hard rule. `dotnet build` on the skeleton `.csproj` enforces it — there is no `UnityEngine` package reference, so violating it would be a compile error. |
| 35 NUnit tests, ~20 ms, no Unity runner | This is the proof. Zero tests existed before. The test suite covers construction, stream dispatch, the 2000-entry cap, dispose/unsubscribe symmetry, AutoScrollEnabled toggle, the gateway adapter wire-format cases, and the non-matching notification filter. |

---

## Numbers at a glance

| | Before (`DebugLogging`) | After (worst class) |
|---|---|---|
| LOC | 255 (one class) | 86 (`DebugTabView`) |
| WMC | 8 (NOM) / 21 (McCabe) | 8 (`GatewayLogStreamAdapter`) |
| CBO domain VM | ~9 | **2** (`DebugTabViewModel`) |
| LCOM | **95% — fails** | 72% max (artifact, not concern split) |
| Tests without Unity | **0** | **35, ~20 ms** |
| Section 4.2 | ❌ | ✅ |
| Cycles | 0 | 0 |
| Assembly layers | 1 (everything in Assembly-CSharp) | 3 (Domain / Adapters / Producers) |

The headline for the panel: **this was a testability refactor, not a metrics rescue.** `DebugLogging` passed 5 of 6 CK checks — the one it failed (LCOM=95%) was the metric that caught the four-concerns smell. The real problem was structural: a static non-substitutable event hook, an unbounded queue, per-message file I/O, and four independent jobs in one class with no seam for tests. The after design eliminates those structural problems and proves it with 35 passing NUnit tests.
