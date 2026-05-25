# Debug tab — AFTER sequence diagram (Mermaid)

Mermaid rendering of [`after-trace.md`](after-trace.md). Pair side-by-side with the BEFORE diagram (`uml-diagrams/before-debug-sequence-diagram.puml`) on the panel slide: every BEFORE `→ HandleLog → Queue.Enqueue → StreamWriter → StringBuilder → TMP_InputField` chain collapses into the single `LogStream.Publish → ILogObserver.OnNext` dispatch here.

The ACL boundary is drawn as a `box` around the Unity-side adapters. The `DebugTabVM` and `LogStream` lifelines never send a message into the box without going through an interface.

A higher-level PlantUML version (architectural overview, no line citations) lives at [`docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml`](../../../docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml). This Mermaid version is the code-anchored one.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Sub as AnySubsystem<br/>(any Debug.Log caller — unchanged)

    box rgb(245, 230, 230) ACL boundary — Unity assembly (adapters)
    participant App as Application<br/>(static logMessageReceived)
    participant LSA as UnityLogStreamAdapter<br/>(MonoBehaviour : ILogStream)
    participant Root as CompositionRoot<br/>(MonoBehaviour)
    participant View as DebugTabView<br/>(MonoBehaviour, thin)
    end

    participant LS as LogStream<br/>(pure C#, thread-safe)
    participant VM as DebugTabVM<br/>(pure C# : ILogObserver, IDisposable)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE A — Startup (CompositionRoot wires the graph)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(235, 245, 230)
    Note over User,VM: PHASE A — Awake() wires VM → adapter → View
    Root->>VM: new DebugTabViewModel(adapter as ILogStream)
    activate VM
    VM->>LSA: ILogStream.Subscribe(this)
    Note right of LSA: delegates to inner LogStream<br/>(adapter wraps LogStream)
    LSA->>LS: Subscribe(observer)
    LS-->>VM: registered
    deactivate VM

    Root->>View: BindTo(vm)
    View->>VM: EntriesChanged += OnEntriesChanged<br/>clearButton.onClick += vm.ClearEntries
    View->>View: OnEntriesChanged()  [initial empty render]
    end

    Note over User,VM: wiring complete — log subscription live

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE B — A log line is emitted (the main flow)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(230, 240, 250)
    Note over User,VM: PHASE B — Capture → publish → observe → render
    Sub->>App: Debug.Log / LogWarning / LogError<br/>[44 call sites unchanged]
    App->>LSA: logMessageReceived(message, stackTrace, type)
    activate LSA
    LSA->>LSA: level = type switch {<br/>  Warning → Warning<br/>  Error / Exception / Assert → Error<br/>  _ → Info<br/>}
    LSA->>LS: Publish(level, message)
    deactivate LSA

    activate LS
    LS->>LS: entry = new LogEntry(level, message, DateTime.UtcNow)
    Note right of LS: ★ Timestamp captured in domain code —<br/>BEFORE never captured it
    LS->>LS: snapshot = observers.ToArray()  [under lock]
    LS->>VM: OnNext(entry)  [for each observer, outside lock]
    deactivate LS

    activate VM
    VM->>VM: entries.Add(entry)
    opt entries.Count > MaxEntries (2000)
        VM->>VM: entries.RemoveAt(0)
    end
    Note right of VM: ★ Bounded memory —<br/>replaces unbounded non-generic Queue
    VM->>VM: EntriesChanged?.Invoke()
    deactivate VM

    VM-->>View: EntriesChanged
    activate View
    View->>VM: get LogEntries  [last 500]
    View->>View: rebuild StringBuilder over slice,<br/>colour-code per Level<br/>(warning=yellow, error=red, info=white)
    Note right of View: ⚠ O(N) rebuild remains —<br/>but N is capped at MaxDisplayLines (500)
    View->>View: _logText.text = sb.ToString()<br/>_scrollbar.value = 0f
    Note right of View: ⚠ scroll forced to bottom —<br/>S7 still remaining
    deactivate View
    View-->>User: log line visible, colour-coded
    end

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE C — User clears the log
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(245, 245, 230)
    Note over User,VM: PHASE C — Clear button
    User->>View: click Clear
    View->>VM: ClearEntries()
    activate VM
    VM->>VM: entries.Clear()
    VM->>VM: EntriesChanged?.Invoke()
    deactivate VM
    VM-->>View: EntriesChanged
    View->>View: OnEntriesChanged()  [empty slice]
    View-->>User: log panel empty
    end

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE D — Scene teardown (lifetime hygiene)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(240, 240, 240)
    Note over User,VM: PHASE D — OnDestroy
    Root->>VM: Dispose()
    activate VM
    VM->>LSA: ILogStream.Unsubscribe(this)
    LSA->>LS: Unsubscribe(observer)
    LS-->>VM: removed
    VM->>VM: _disposed = true  [idempotent]
    deactivate VM

    Note over App,LSA: UnityLogStreamAdapter.OnDisable<br/>removes logMessageReceived handler.<br/>Static event has zero subscribers after teardown.
    end
```

---

## Side-by-side reading guide

Suggested slide layout for the panel:

| BEFORE callout | AFTER replacement |
|---|---|
| `Application.logMessageReceived += DebugLogging.HandleLog` (static-event subscription untestable) | One subscription confined to `UnityLogStreamAdapter.OnEnable` (`adapters/UnityLogStreamAdapter.cs:28-29`). The VM subscribes to `ILogStream`, not the static event. |
| `(string, string, LogType)` unstructured tuple | `LogEntry(Level, Message, Timestamp)` immutable record (`skeleton/ILogStream.cs:31`). |
| Non-generic `Queue` storing `object`, unbounded | Generic `List<LogEntry>` capped at 2000 entries (`skeleton/DebugTabViewModel.cs:22, 49-50`). |
| `StreamWriter` opened + closed per message | No file I/O on the hot path. Autosave reintroduced as a separate `ILogObserver` if needed. |
| `StringBuilder` rebuild over entire log history | Rebuild capped at 500-line slice (`adapters/DebugTabView.cs:35, 62-82`) — contained, not eliminated. |
| `transform.Find` / Inspector-wired button handlers | Code-side `clearButton.onClick.AddListener(vm.ClearEntries)` in `BindTo` (`adapters/DebugTabView.cs:47`). |
| Four responsibilities in one 172-line `MonoBehaviour` | Five named types, single responsibility each: `LogStreamAdapter` · `LogStream` · `DebugTabVM` · `DebugTabView` · `CompositionRoot`. |
| Timestamp never captured | Captured at the moment of `Publish` (`skeleton/LogStream.cs:36`). |
| Scroll forced to bottom every message | **Unchanged** — still in `DebugTabView` (`⚠` annotation in diagram). S7 is the largest remaining smell; see [`after-trace.md` → Known limitations](after-trace.md#known-limitations). |

---

## Mapping of contained smells (honest about what remains)

The two `⚠` annotations in the diagram correspond to items in [`after-trace.md` → Known limitations](after-trace.md#known-limitations):

| Diagram marker | Smell ID | Location | Fix vector |
|---|---|---|---|
| `⚠ O(N) rebuild remains — capped` | S5/S6 | `adapters/DebugTabView.cs:62-82` | Replace TMP text rebuild with a virtualised `ListView` (Unity UI Toolkit). The VM's `LogEntries` contract is unchanged. |
| `⚠ scroll forced to bottom` | S7 | `adapters/DebugTabView.cs:83` | Add `AutoScrollEnabled` (bool) to `IDebugTabViewModel`; gate the `_scrollbar.value = 0f` line on it. Adds one test. |

Both fixes are pure View/VM-side edits and require no change to `LogStream`, `UnityLogStreamAdapter`, or any of the 29 existing debug-tab tests.

---

## What the diagram does *not* show (deliberately)

- **Direct `ILogStream.Publish(...)` callers.** The skeleton interface exposes a structured-publish path (`ILogStream.cs:13`) for new callers, but no production code uses it today — the 44 catalogued sites still go through `Debug.Log → Application.logMessageReceived → UnityLogStreamAdapter`. The diagram only draws the active path. See [`after-trace.md` → Open question: source field](after-trace.md#open-question-source-field).
- **The `source` field.** [`log-origin-trace.md`](log-origin-trace.md) argues for `Publish(LogLevel, string source, string message)`. The implemented contract is `Publish(LogLevel, string)` only; the diagram reflects the code, not the aspirational design.
- **Autosave / file export.** Not in the WE2 scope. Sketched as an `ILogObserver` follow-up in [`after-trace.md` → Open question: Save/autosave](after-trace.md#open-question-saveautosave).
