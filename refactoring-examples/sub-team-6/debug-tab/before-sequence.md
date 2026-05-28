# Debug tab — BEFORE sequence diagram (Mermaid)

## TL;DR

Mermaid `sequenceDiagram` rendering of `before-trace.md`. Four phases drawn as one continuous diagram with `Note over` separators: **A** startup wires log rotation + save button; **B** `OnEnable` subscribes `HandleLog` to the static `Application.logMessageReceived` event; **C** every `Debug.Log*` in the process re-enters via the static hook → `Queue → StreamWriter → StringBuilder → TMP_InputField` chain; **D** manual export reuses the queue. The visual signature of S8 (four-concerns-in-one-class) is the four distinct activity bands inside the single `DL` lifeline — the same structural defect that LCOM hs = 0.95 quantifies. Centrepiece smells visible at a glance: the `Sub → UE → App → DL` static-event triangle (S1), the per-message `new StreamWriter` open/close (S4), and the O(N) `foreach` rebuild over the whole queue on every new entry (S5). Pairs side-by-side with [`after-sequence.md`](after-sequence.md) — every Phase C arrow collapses into the single `LogStream.Publish → ILogObserver.OnNext` dispatch in the AFTER design.

---

This is the Mermaid rendering of [`before-trace.md`](before-trace.md). Every message is sourced from the trace document; line citations live there and in the [smell anchors](#smell-anchors-cross-reference-with-the-trace) table at the bottom of this file. Pair this diagram with the trace when presenting to the panel.

The four phases are drawn as one continuous diagram. Phase A and Phase B together establish wiring; Phase C is the per-log-message dispatch and is the visual centrepiece; Phase D is the manual export. There is deliberately **no ACL boundary box** here — the absence is the point. The AFTER diagram introduces one; this one cannot.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Sub as AnySubsystem<br/>(any Debug.Log caller)
    participant UE as UnityEngine.Debug<br/>(static)
    participant App as Application<br/>(static — owns logMessageReceived)
    participant DL as DebugLogging<br/>(255-line MonoBehaviour)
    participant SW as StreamWriter<br/>(opened per message)
    participant TIF as TMP_InputField<br/>(logOutput, scene asset)
    participant SFB as StandaloneFileBrowser

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE A — Startup (DebugLogging initialises)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(245, 245, 220)
    Note over User,SFB: PHASE A — Startup: log rotation + button wiring
    Note over DL: Unity lifecycle: Start()
    activate DL
    DL->>DL: directoryPath = Application.dataPath + "/Outputs/Logs"

    loop for each existing iDaVIE_Log_N_* file
        DL->>DL: rename N → N+1<br/>delete if N == maxLogs - 1
    end
    Note right of DL: maxLogs read from<br/>Config.Instance.numberOfLogsToKeep<br/>(singleton coupling)

    DL->>DL: autosavePath = "...iDaVIE_Log_0_{timestamp}.log"
    DL->>DL: saveButton.onClick.AddListener(saveToFileClick)<br/>[Inspector-wired Button]
    DL->>UE: Debug.Log("Start debug logging.")
    DL->>UE: Debug.Log("iDaVIE Version: " + Application.version)
    DL->>DL: DetermineHardware() — 4× Debug.Log
    Note right of DL: these immediately re-enter via<br/>Phase C (subscription is live<br/>by this point — OnEnable ran first)
    deactivate DL
    end

    Note over User,SFB: Phase A complete — autosave path set, save button wired

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE B — Subscription is wired (OnEnable)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(230, 245, 230)
    Note over User,SFB: PHASE B — OnEnable: subscribe to the static event
    Note over DL: Unity lifecycle: OnEnable()
    activate DL
    DL->>App: logMessageReceived += HandleLog
    Note right of App: ★ S1 — subscribes to a static<br/>Unity event. No interface, no DI,<br/>no substitute possible in tests.
    deactivate DL
    end

    Note over User,SFB: wiring complete — HandleLog is now a global listener<br/>(fires on every Debug.Log* anywhere in the process)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE C — A log line is emitted (repeats per log call)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(220, 235, 245)
    Note over User,SFB: PHASE C — Debug.Log emitted → re-enters via static hook
    Sub->>UE: Debug.Log(message)<br/>(40+ sites in CanvassDesktop)
    Note right of UE: ★ S9 — 44 unstructured callers<br/>across the codebase
    UE->>App: logMessageReceived.Invoke(<br/>logString, stackTrace, type)
    App->>DL: HandleLog(logString, stackTrace, type)
    activate DL

    DL->>DL: logMessage = "[" + type + "] : " + logString
    Note right of DL: ★ S2 — string concat;<br/>no source, no timestamp,<br/>no structured fields

    DL->>DL: debugLogQueue.Enqueue(logMessage)
    Note right of DL: ★ S3 — non-generic Queue,<br/>stores object, unbounded growth

    DL->>SW: new StreamWriter(autosavePath, append=true)
    DL->>SW: Write(message + "\n")
    DL->>SW: Close()
    Note right of SW: ★ S4 — new file handle on<br/>every single log message<br/>(GC pressure + main-thread I/O)

    opt type == LogType.Exception
        DL->>DL: debugLogQueue.Enqueue(stackTrace)
        DL->>SW: new StreamWriter / Write / Close<br/>(again, for stackTrace)
    end

    loop foreach entry in debugLogQueue
        DL->>DL: StringBuilder.Append(entry)
    end
    Note right of DL: ★ S5 — O(N) rebuild of entire<br/>log history on every new entry

    DL->>TIF: logOutput.text = builder.ToString()
    Note right of TIF: ★ S6 — replaces entire TMP text;<br/>full re-layout every message

    DL->>DL: debugScrollbar.value = 1.0f
    Note right of DL: ★ S7 — forced scroll-to-bottom;<br/>user cannot read while logging

    DL-->>User: new log line visible at bottom of Debug tab
    deactivate DL
    end

    Note over User,SFB: ...time passes — many C-phase entries accumulate in the queue...

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE D — User manually saves the log
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(245, 230, 245)
    Note over User,SFB: PHASE D — User clicks Save (manual export)
    User->>DL: click "Save" button<br/>[Inspector-wired in Phase A]
    activate DL
    DL->>DL: PlayerPrefs.GetString("LastDebugPath")
    DL->>SFB: SaveFilePanelAsync(..., callback)
    deactivate DL

    SFB-->>User: native OS save dialog
    User->>SFB: choose destination
    SFB-->>DL: callback(dest)
    activate DL
    DL->>SW: new StreamWriter(dest)
    loop foreach entry in debugLogQueue
        DL->>SW: Write(entry + "\n")
    end
    DL->>SW: Close()
    Note right of SW: single open/close —<br/>acceptable here (not per-message)
    DL-->>User: log file saved
    deactivate DL
    end
```

---

## Reading guide for the panel

The diagram makes four smells visible at a glance:

1. **The `Sub → UE → App → DL` static-event triangle** — every `Debug.Log*` call anywhere in the process funnels through `Application.logMessageReceived`, a Unity-owned static event. There is no interface between the caller and the handler. This is **S1**, and no CK metric captures it: `Application` is just one of `DebugLogging`'s ten collaborators in CBO.
2. **The `Queue → StreamWriter → StringBuilder → TMP_InputField` chain on every Phase C entry** — four storage/IO/rendering steps execute synchronously per log message, with a new file handle opened and closed in the middle (S4) and an O(N) rebuild over the entire queue at the end (S5). Each message also does a wholesale `TMP_InputField.text =` replacement (S6).
3. **The `debugScrollbar.value = 1.0f` self-message** — runs after every entry regardless of whether the user has scrolled away (S7). The same line that records the smell is the one the AFTER design's `AutoScrollEnabled` property gates.
4. **Four distinct activity bands inside the single `DL` lifeline** — Phase A (wiring), Phase B (subscription), Phase C (dispatch), Phase D (export) all occupy `DL`. They share almost no fields (see the method-field access matrix in `ck-metrics.md`). This is the visual signature of **S8**, the four-concerns-in-one-class smell, and the same structural defect that LCOM hs = 0.95 quantifies.

## Smell anchors (cross-reference with the trace)

| Diagram marker | Trace smell ID | Code citation |
|---|---|---|
| `★ S1` at `logMessageReceived += HandleLog` | S1 | `DebugLogging.cs:149` |
| `★ S2` at `"[" + type + "] : " + logString` | S2 | `DebugLogging.cs:177–179` |
| `★ S3` at `debugLogQueue.Enqueue` | S3 | `DebugLogging.cs:49, 180` |
| `★ S4` at `new StreamWriter` / `Write` / `Close` | S4 | `DebugLogging.cs:250–253` |
| `★ S5` at `foreach` + `StringBuilder.Append` loop | S5 | `DebugLogging.cs:189–195` |
| `★ S6` at `logOutput.text = builder.ToString()` | S6 | `DebugLogging.cs:195` |
| `★ S7` at `debugScrollbar.value = 1.0f` | S7 | `DebugLogging.cs:196` |
| Four activity bands inside the `DL` lifeline | S8 | whole class (Phases A/B/C/D) |
| `★ S9` at `Sub → UE: Debug.Log(message)` | S9 | 44 sites — see [`log-origin-trace.md`](log-origin-trace.md) |

Pair the diagram with [`after-sequence.md`](after-sequence.md) on a side-by-side slide: the entire Phase C arrow chain (`Queue → StreamWriter → StringBuilder → TMP_InputField`) collapses into one `LogStream.Publish → ILogObserver.OnNext` dispatch, and the `Sub → UE → App → DL` static-event triangle is replaced by a single `Sub → ILogStream.Publish` arrow through the ACL boundary.
