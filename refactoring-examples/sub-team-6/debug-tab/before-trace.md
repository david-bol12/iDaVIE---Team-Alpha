# Debug Tab — "Log line emitted → visible in Debug tab" (current path)

Raw code-side trace of the production behaviour as of branch `team6`. Every message is
anchored to a file and line in the live codebase so the resulting before-state sequence
diagram is defensible at the maintainer panel.

> **Correction to `metrics.md` §3.1:** That section states "there is no Debug tab class in
> the current codebase." This is incorrect. `DebugLogging.cs` exists at
> `Assets/Scripts/Debuggers/DebugLogging.cs` and **is** the current Debug tab
> implementation. The before-state analysis below supersedes that description.
> The after-state proposal and CK deltas in `metrics.md` §3.2–3.3 remain valid.

---

## Actors / lifelines

| Lifeline | Backing type | File | Notes |
|---|---|---|---|
| `User` | — | — | Desktop operator |
| `AnySubsystem` | Any `MonoBehaviour` or static class | Various | Any caller of `UnityEngine.Debug.Log/LogWarning/LogError` |
| `UnityEngine.Debug` | Static Unity class | Unity runtime | The single global log sink; all log calls funnel here |
| `Application` | Static Unity class | Unity runtime | Owns `logMessageReceived` — a static event fired after every `Debug.Log*` call |
| `DebugLogging` | `MonoBehaviour` | `Assets/Scripts/Debuggers/DebugLogging.cs` | The existing Debug tab class; subscribes to `Application.logMessageReceived` |
| `TMP_InputField` (logOutput) | Unity UI | Scene asset | The scrollable text area shown in the Debug tab panel |
| `Scrollbar` (debugScrollbar) | Unity UI | Scene asset | Scrollbar reset to bottom on each new entry |
| `StreamWriter` | `System.IO.StreamWriter` | — | File handle opened and closed on **every** log message |

---

## Phase A — Startup (DebugLogging initialises)

| # | Message | Source citation | Notes / smell |
|---|---|---|---|
| A1 | `DebugLogging.Start()` called by Unity lifecycle | `DebugLogging.cs:53` | Standard `MonoBehaviour.Start` |
| A2 | Builds `directoryPath = Outputs/Logs/` relative to `Application.dataPath` | `DebugLogging.cs:55–56` | Hard-coded subdirectory relative to Unity data path |
| A3 | Log-rotation loop: renames existing `iDaVIE_Log_N_*` files to `N+1`; deletes at `maxLogs−1` | `DebugLogging.cs:68–109` | `maxLogs` read from `Config.Instance.numberOfLogsToKeep` |
| A4 | Sets `autosavePath = Outputs/Logs/iDaVIE_Log_0_{timestamp}.log` | `DebugLogging.cs:112` | Timestamped autosave path computed once at startup |
| A5 | `saveButton.onClick.AddListener(saveToFileClick)` | `DebugLogging.cs:124` | Inspector-wired `Button` reference |
| A6 | Calls `Debug.Log("Start debug logging.")`, `Debug.Log($"iDaVIE Version: {Application.version}")`, `DetermineHardware()` | `DebugLogging.cs:121–123` | First log messages — these re-enter `HandleLog` immediately (see Phase B) |

**At end of Phase A:** `DebugLogging` is initialised, the autosave file is open for appending, and the `HandleLog` subscription is live.

---

## Phase B — Subscription is wired (OnEnable)

| # | Message | Source citation | Notes / smell |
|---|---|---|---|
| B1 | `DebugLogging.OnEnable()` | `DebugLogging.cs:147` | Called by Unity when the GameObject becomes active |
| B2 | `Application.logMessageReceived += HandleLog` | `DebugLogging.cs:149` | **★ The design smell.** Subscribes to a static Unity event — a global singleton hook. There is no interface, no abstraction, no way to inject a substitute in tests. Any `Debug.Log` call anywhere in the process fires this handler. |

**At end of Phase B:** `HandleLog` is registered as a global listener. All subsequent `Debug.Log*` calls will invoke it.

---

## Phase C — A log line is emitted (the main flow)

This is the sequence that repeats for every log call site in the codebase (40+ in
`CanvassDesktop` alone, plus all other classes).

| # | Message | Source citation | Notes / smell |
|---|---|---|---|
| C1 | `AnySubsystem` calls `Debug.Log(message)` / `Debug.LogWarning(message)` / `Debug.LogError(message)` | Various (e.g. `CanvassDesktop.cs:351, 844, 1024`) | **★ Smell.** `UnityEngine.Debug` is a static dependency — callers cannot be tested without Unity runner. No structured fields: level, source, and timestamp are not captured. |
| C2 | Unity runtime fires `Application.logMessageReceived(logString, stackTrace, type)` | Unity internals | The static event passes `(string logString, string stackTrace, LogType type)`. No source class, no timestamp, no correlation ID. |
| C3 | `DebugLogging.HandleLog(logString, stackTrace, type)` invoked | `DebugLogging.cs:177` | Handler receives the three raw strings |
| C4 | Formats: `logMessage = "[" + type + "] : " + logString` | `DebugLogging.cs:179` | **★ Smell.** String concatenation — no structured record. Timestamp is not captured here. Level is serialised as the `LogType` enum's `.ToString()` with no normalisation. |
| C5 | `debugLogQueue.Enqueue(logMessage)` | `DebugLogging.cs:180` | **★ Smell.** `Queue` is non-generic (`System.Collections.Queue`) — stores `object`. No type safety, no capacity bound. An unending stream (e.g. per-frame `Debug.Log` calls) will grow this queue unbounded in memory. |
| C6 | `AutoSave(logMessage)` | `DebugLogging.cs:181` | Calls `AutoSave` synchronously before UI update |
| C7 | Inside `AutoSave`: `new StreamWriter(autosavePath, true)` → `writer.Write(message + "\n")` → `writer.Close()` | `DebugLogging.cs:250–253` | **★ Smell.** Opens and closes a `StreamWriter` on **every single log message**. Under high-frequency logging this creates a new file handle per call — GC pressure + I/O syscall on the Unity main thread. |
| C8 | If `type == LogType.Exception`: also enqueues `stackTrace` and calls `AutoSave(stackTrace)` | `DebugLogging.cs:183–187` | Correct handling of exceptions — but same smell: two `StreamWriter` opens per exception |
| C9 | Rebuilds display string: iterates entire `debugLogQueue` via `foreach`, `StringBuilder.Append` | `DebugLogging.cs:189–195` | **★ Smell.** O(N) rebuild of the entire log history on every new entry. With 1000 entries, every new log message iterates all 1000 previous entries. Performance degrades linearly with log history size. |
| C10 | `logOutput.text = builder.ToString()` | `DebugLogging.cs:195` | **★ Smell.** Replaces the entire `TMP_InputField` text string on every log event. Unity's TextMesh Pro must re-layout the entire string — expensive for large logs. |
| C11 | `debugScrollbar.value = 1.0f` | `DebugLogging.cs:196` | Forces scroll to bottom. Correct intent, but the user cannot scroll up while new messages are arriving — the scroll position is reset on every message regardless of whether the user has scrolled away. |
| C12 | **[GUI WALK]** New log line appears at the bottom of the Debug tab scroll area | Scene | Walkthrough: confirm tab label, panel location, text format as shown |

---

## Phase D — User manually saves the log

| # | Message | Source citation | Notes / smell |
|---|---|---|---|
| D1 | **[GUI WALK]** User clicks **Save** button in the Debug tab | Scene asset | Walkthrough: confirm button label and location |
| D2 | `saveToFileClick()` | `DebugLogging.cs:203` | Inspector-wired via `Start()` |
| D3 | Reads `PlayerPrefs.GetString("LastDebugPath")` | `DebugLogging.cs:205` | Same `PlayerPrefs` pattern as `CanvassDesktop` — global mutable state |
| D4 | `StandaloneFileBrowser.SaveFilePanelAsync(...)` | `DebugLogging.cs:217` | Same SFB async callback pattern as file-tab browse — callback closes over `DebugLogging` instance |
| D5 | `SaveToFile(dest)` — iterates `debugLogQueue`, writes to user-chosen path | `DebugLogging.cs:232–242` | `StreamWriter` opened and closed once per save — acceptable here |

---

## Smell summary (feeds SOLID/GRASP audit + before-state class diagram)

| # | Smell | Lines | Principle violated |
|---|---|---|---|
| S1 | `Application.logMessageReceived` is a static global event — untestable, no interface | `DebugLogging.cs:149` | Dependency Inversion; testability (NFR-TST-1) |
| S2 | `HandleLog` receives unstructured `(string, string, LogType)` — no source, no timestamp | `DebugLogging.cs:177–179` | Information Expert — the handler knows least about the log entry's context |
| S3 | Non-generic `Queue` — no type safety, no capacity bound, unbounded memory growth | `DebugLogging.cs:49, 180` | Robustness; LCOM (queue is a shared mutable field accessed by 4 methods) |
| S4 | `StreamWriter` opened and closed per message in `AutoSave` | `DebugLogging.cs:250–253` | Performance; Single Responsibility (I/O management mixed into the log handler) |
| S5 | O(N) full-queue rebuild on every `HandleLog` call | `DebugLogging.cs:189–195` | Performance; fails under high-frequency log streams (NFR-TST Debug tab load test) |
| S6 | `logOutput.text = builder.ToString()` replaces entire TMP_InputField string every message | `DebugLogging.cs:195` | Modularity — View mutation is embedded in the handler; no separation between log model and display |
| S7 | Scroll position forced to bottom on every message — user cannot read while logging | `DebugLogging.cs:196` | Usability; this is the specific UX problem the `AutoScrollEnabled` property in `IDebugTabViewModel` solves |
| S8 | `DebugLogging` mixes: log capture, log storage (autosave), log display, and manual export | Whole class | Single Responsibility — four distinct concerns in one 172-line `MonoBehaviour` |
| S9 | `UnityEngine.Debug` callers (40+ in `CanvassDesktop` alone) have a hidden transitive `UnityEngine` dependency | Various | Dependency Inversion; ACL rule (§4.2.3) |

---

## CK metrics for DebugLogging (before-state)

| Metric | Value | Threshold | Status |
|---|---|---|---|
| WMC | 9 (`Start`, `OnEnable`, `OnDisable`, `DetermineHardware`, `HandleLog`, `saveToFileClick`, `SaveToFile`, `AutoSave` + non-generic Queue iterator) | ≤ 40 | ✅ |
| DIT | 1 (`MonoBehaviour`) | ≤ 4 | ✅ |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | ~10 (`Config`, `TMP_InputField`, `Scrollbar`, `Button`, `StandaloneFileBrowser`, `StreamWriter`, `StringBuilder`, `Application`, `SystemInfo`, `PlayerPrefs`) | ≤ 25 | ✅ |
| RFC | ~18 | ≤ 50 | ✅ |
| LCOM | ~0.45 (9 methods, 5 fields; `debugLogQueue` accessed by 3 methods, `autosavePath` by 2, others by 1) | ≤ 0.50 | ✅ (borderline) |

> `DebugLogging` passes CK thresholds individually — the case for refactoring is **structural
> and testability-driven**, not metric-driven. The critical violation is S1 (static event hook)
> and S8 (four responsibilities in one class). These prevent unit testing without Unity and
> prevent reuse of the log model in non-UI contexts.

---

## How this becomes the before-state sequence diagram

Convert each Phase C row to a `sequenceDiagram` message:

- Show `AnySubsystem → UnityEngine.Debug : Log(message)` as the trigger.
- Show `UnityEngine.Debug → Application : logMessageReceived.Invoke(logString, stackTrace, type)` to make the static event coupling visible.
- Show `Application → DebugLogging : HandleLog(...)` as the subscription dispatch.
- The O(N) queue rebuild (C9) and `TMP_InputField` replacement (C10) are the visual centrepiece — these are what the `ObservableCollection<LogEntry>` + virtualised `ListView` in the after-state replace.
- Show `DebugLogging → StreamWriter : new / Write / Close` on **every** C6 call to make the per-message file handle smell visible.
- The after-state diagram replaces the entire `Application.logMessageReceived` path with `AnySubsystem → ILogStream : Publish(level, source, message)`.

Save the PlantUML diagram as `uml-diagrams/before-debug-sequence-diagram.puml` (parallel to the existing `after-debug-sequence-diagram.puml`).

---

## What's missing before this trace is complete

1. **GUI walkthrough notes** for C12, D1 — exact tab label, panel location, text format as shown in the running app.
2. **Before-state class diagram** (`ex2-debug-tab/before-class-diagram.puml`) showing `DebugLogging`, `Application`, `TMP_InputField`, `Scrollbar`, `Button`, `StreamWriter`, and their relationships — draw from the smell table above.
3. **Before-state sequence diagram** PlantUML file at `uml-diagrams/before-debug-sequence-diagram.puml`.
4. **Before-state DSM** showing `DebugLogging`'s coupling fan-out (parallel to `ex1-file-tab/before-dsm.md`).
5. **SOLID/GRASP audit** for the debug tab — the smell table above is the input; the audit format should match the file-tab audit once that is written.
