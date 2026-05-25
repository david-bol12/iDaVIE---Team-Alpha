# Debug tab — CK metric deltas (BEFORE vs. AFTER)

> **Status: hand-counted projection — pending Quality Guild tool verification on Day 13.**
> Numbers below were counted from the live `team6` branch using `wc`/`grep` against `Assets/Scripts/Debuggers/DebugLogging.cs` (BEFORE) and the skeleton + adapter files in this folder (AFTER). They are submitted **alongside** the SonarQube Cloud + Understand baseline that the Quality Guild owns; if their tooling reports different values, those numbers supersede this document.
>
> Counting conventions used here:
> - **WMC** = method count (NOM-style WMC; the same as Understand's `CountDeclMethod`). Property getters/setters with non-trivial bodies count as one method each; trivial auto-implemented getters do not.
> - **DIT** = depth from `System.Object`. `MonoBehaviour` adds 3 to the count (`Object → Component → Behaviour → MonoBehaviour`).
> - **CBO** = distinct named types referenced *in implementation*, excluding primitives, language types (`string`, `int`, etc.), and the class's own type. DTOs of the same package are counted.
> - **RFC** = WMC + distinct external methods called. Hand-count is approximate; tool-verified value is authoritative.
> - **LCOM** = LCOM4 (connected components of the method-field graph). 1 = perfectly cohesive; >1 = disjoint concerns.
>
> Threshold source: `CLAUDE.md` § *Mandatory metric tools*, Section 7.1 of the brief.
>
> | Metric | Domain threshold | Adapter/Orchestrator threshold |
> |---|---:|---:|
> | WMC | ≤ 20 | ≤ 40 |
> | DIT | ≤ 4 | ≤ 4 |
> | NOC | ≤ 5 | ≤ 5 |
> | CBO | ≤ 14 | ≤ 25 |
> | RFC | ≤ 50 | ≤ 50 |
> | LCOM | ≤ 0.5 (LCOM-HS) / = 1 (LCOM4) | same |

---

## Headline: this is a *testability* refactor, not a metric refactor

`DebugLogging` already passes the CK thresholds individually:

| Metric | DebugLogging BEFORE | Orchestrator threshold | Pass? |
|---|---:|---:|:--:|
| LOC | 255 | — | — |
| WMC | **8** | ≤ 40 | ✅ |
| DIT | **4** | ≤ 4 | ✅ (at limit, MonoBehaviour) |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | **~10** | ≤ 25 | ✅ |
| RFC | **~25** | ≤ 50 | ✅ |
| LCOM4 | **~3** | = 1 | ❌ |

The only metric failure is **LCOM4 ~3**, reflecting the four disjoint concerns identified in [`before-trace.md` → Smell S8](before-trace.md#smell-summary-feeds-the-solidgrasp-audit--ck-deltas): log capture, log storage (autosave), log display, and manual export each operate on a non-overlapping subset of the eight fields. The other metrics pass.

The case for refactoring is therefore **structural and testability-driven**, not metric-driven:

1. **Smell S1** (static `Application.logMessageReceived` hook) makes the class **untestable** without a Unity test runner. No CK metric captures this directly — CBO sees `Application` as one collaborator like any other; the fact that it cannot be substituted is invisible to the tool.
2. **Smell S2** (unstructured `(string, string, LogType)` tuple) creates a contract problem — no metric flags it.
3. **Smell S8** (four concerns in one class) shows up as LCOM4 > 1 — the **only** metric that catches a real defect here.

The AFTER design wins on LCOM4 (every class is 1), on testability (29 NUnit tests without Unity), and on assembly-level dependency direction (Section 4.2 compliance) — see [`dependency-graph.md`](dependency-graph.md). Raw CK headline numbers improve modestly because BEFORE already passed most thresholds.

---

## BEFORE — `DebugLogging` detail

| Metric | Hand-counted value | Threshold (orchestrator) | Pass? | Source / how counted |
|---|---:|---:|:--:|---|
| LOC | **255** | (no hard cap; advisory) | ⚠ | `wc -l Assets/Scripts/Debuggers/DebugLogging.cs` |
| WMC (method count) | **8** | ≤ 40 | ✅ | `Start`, `OnEnable`, `OnDisable`, `DetermineHardware`, `HandleLog`, `saveToFileClick`, `SaveToFile`, `AutoSave` |
| DIT | **4** | ≤ 4 | ✅ (at limit) | `class DebugLogging : MonoBehaviour` |
| NOC | 0 | ≤ 5 | ✅ | No subclasses |
| CBO | **~10** | ≤ 25 | ✅ | `Config`, `Queue`, `TMP_InputField`, `Scrollbar`, `Button`, `StandaloneFileBrowser`, `StreamWriter`, `StringBuilder`, `Application`, `SystemInfo`, `PlayerPrefs` |
| RFC | **~25** | ≤ 50 | ✅ | 8 own methods + ~17 distinct external calls (`Enqueue`, `Write/Close`, `Append/ToString`, `text=`, `value=`, `AddListener`, `Log/LogError`, `GetString/SetString`, `Exists/Delete/Move`, …) |
| LCOM4 | **~3** | = 1 | ❌ | Four disjoint concern clusters: (a) `Start` + log-rotation (uses `directoryPath`, `autosavePath`, `maxLogs`); (b) `HandleLog` + `AutoSave` (use `debugLogQueue`, `autosavePath`, `logOutput`, `debugScrollbar`); (c) `saveToFileClick` + `SaveToFile` (use `debugLogQueue` only, plus `PlayerPrefs`); (d) `DetermineHardware` (uses no fields — pure side-effect on `Debug.Log`). At least three connected components remain after merging shared fields. |

### Verdict (BEFORE)

`DebugLogging` **fails 1 of 6 metrics** (LCOM4). All other CK numbers are within thresholds. The qualitative smells (S1–S9 in [`before-trace.md`](before-trace.md)) dominate the case for refactoring.

---

## AFTER — debug-tab slice as seven focused types

The BEFORE class is decomposed into:
- **3 interfaces** (`IDebugTabViewModel`, `ILogStream`, `ILogObserver`) — design surface, not measured for WMC.
- **2 concrete domain classes** (`LogStream`, `DebugTabViewModel`).
- **1 DTO record** (`LogEntry`) + **1 enum** (`LogLevel`) — measured at near-zero cost.
- **3 adapters** (`UnityLogStreamAdapter`, `DebugTabView`, `DebugTabCompositionRoot`).

| Class | Layer | LOC | WMC | DIT | NOC | CBO | RFC | LCOM4 | Threshold band | Pass? |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|:--:|
| `DebugTabViewModel` | domain | 77 | **6** | 1 | 0 | **3** | ~15 | 1 | domain | ✅ |
| `LogStream` | domain | 43 | **3** | 1 | 0 | **2** | ~10 | 1 | domain | ✅ |
| `LogEntry` (record) | domain | (in `ILogStream.cs`) | 0 | 1 | 0 | 1 | 3 | 1 | DTO | ✅ |
| `IDebugTabViewModel` | domain | 32 | — | — | — | — | — | — | interface | n/a |
| `ILogStream` | domain | 32 | — | — | — | — | — | — | interface | n/a |
| `ILogObserver` | domain | 16 | — | — | — | — | — | — | interface | n/a |
| `UnityLogStreamAdapter` | adapter | 53 | **6** | 4 | 0 | **4** | ~12 | 1 | adapter (MonoBehaviour) | ✅ |
| `DebugTabView` | adapter | 86 | **3** | 4 | 0 | **7** | ~18 | 1 | adapter (MonoBehaviour) | ✅ |
| `DebugTabCompositionRoot` | adapter | 43 | **2** | 4 | 0 | **3** | ~6 | 1 | adapter (MonoBehaviour) | ✅ |
| **Σ slice** | — | **~382** + interfaces | **20** | **max 4** | **0** | **max 7** | **max ~18** | **1 per class** | — | **all pass** |

### Per-class notes

**`DebugTabViewModel` (the domain centrepiece)**

- WMC: **6** — ctor, `LogEntries` getter (non-trivial `AsReadOnly`), `AppendEntry`, `ClearEntries`, `OnNext` (explicit interface impl), `Dispose`. Well under the ≤ 20 domain threshold.
- CBO: **3** — `ILogStream` (injected), `LogEntry` (held), `IDisposable` (implemented). Under ≤ 14.
- DIT: **1**. Implements three interfaces (`IDebugTabViewModel`, `ILogObserver`, `IDisposable`) — interfaces don't increase DIT.
- LCOM4: **1**. All methods touch `_entries` (the only meaningful state field). Perfectly cohesive.
- **★ Eliminates BEFORE smell S3**: bounded `List<LogEntry>` with `MaxEntries = 2000` cap, replacing the unbounded non-generic `Queue`.

**`LogStream` (thread-safe Observer dispatch)**

- WMC: **3** — `Subscribe`, `Unsubscribe`, `Publish`. Trivial surface.
- CBO: **2** — `ILogObserver` (collection element), `LogEntry` (created in `Publish`). Under ≤ 14.
- DIT: **1**.
- LCOM4: **1**. All three methods read/write `_observers` (and `Publish` reads `_lock`). Cohesive.
- **★ Eliminates BEFORE smell S2 (timestamp gap):** `DateTime.UtcNow` captured at the moment of `Publish` (`LogStream.cs:36`). BEFORE never captured a timestamp at all.

**`UnityLogStreamAdapter` (the ACL boundary)**

- WMC: **6** — `OnEnable`, `OnDisable`, `OnUnityLog`, `Publish`, `Subscribe`, `Unsubscribe`. The last three are pure delegation to the inner `LogStream`.
- DIT: **4** (MonoBehaviour chain) — at the limit, same as any other Unity component.
- CBO: **4** — `LogStream` (composed inner), `Application` (subscribed), `LogType` (Unity enum), `ILogObserver` (parameter). Under ≤ 25.
- **★ Eliminates BEFORE smell S1**: this is the **only** class in the system that touches `Application.logMessageReceived`. The VM depends on `ILogStream`, not the static event.

**`DebugTabView` (the thin View)**

- WMC: **3** — `BindTo`, `OnDestroy`, `OnEntriesChanged`. No business logic.
- DIT: **4** (MonoBehaviour). CBO: **7** — `IDebugTabViewModel`, `TMP_Text`, `Scrollbar`, `Button`, `LogEntry`, `LogLevel`, `StringBuilder`. Under ≤ 25.
- **Smells S5/S6 contained**: TMP rebuild is still O(N) but capped at `MaxDisplayLines = 500`. **Smell S7 remaining**: `_scrollbar.value = 0f` runs on every refresh — see [`after-trace.md` → Known limitations](after-trace.md#known-limitations).

**`DebugTabCompositionRoot` (the only multi-layer class)**

- WMC: **2** — `Awake`, `OnDestroy`. Trivial wiring.
- DIT: **4** (MonoBehaviour). CBO: **3** — `DebugTabView`, `UnityLogStreamAdapter`, `DebugTabViewModel`. Under ≤ 25.
- The single class permitted to reference both `Domain` and `Adapters` concrete types. Pure-DI / Composition-Root pattern.

### Layer DIT values

- All domain classes (`LogStream`, `DebugTabViewModel`, `LogEntry`): DIT = **1** (System.Object → class). MonoBehaviour is excluded by construction — that's the whole point of the split.
- All adapter `MonoBehaviour`s: DIT = **4** (Object → Component → Behaviour → MonoBehaviour → adapter). At the limit.

---

## Delta summary

| Metric | BEFORE (`DebugLogging`) | AFTER (worst class in slice) | Δ |
|---|---:|---:|---:|
| LOC (single class) | 255 | 86 (`DebugTabView`) | **−66%** |
| WMC | 8 | 6 (`DebugTabViewModel`, `UnityLogStreamAdapter`) | **−25%** |
| CBO | ~10 | 7 (`DebugTabView`) | **−30%** |
| RFC | ~25 | ~18 (`DebugTabView`) | **−28%** |
| LCOM4 | ~3 | **1 per class** | **disjoint → cohesive (★ the structural win)** |
| Threshold pass count | 5 / 6 (LCOM4 fails) | **all classes pass all metrics** | **near-pass → full pass** |

**Unit-testable surface (NFR-TST-1 evidence):**
- BEFORE: **0** debug-tab tests possible without a live Unity scene.
- AFTER: **29** NUnit tests in `tests/DebugTabTests.cs` exercise the domain layer with no Unity dependency. `dotnet test` runtime: **~20 ms**.

**Section 4.2 compliance:**
- BEFORE: ❌ — domain code does not exist as a separate concept; `DebugLogging` transitively reaches `UnityEngine.Application`, `System.IO`, `TMPro`.
- AFTER: ✅ — `DebugTabSkeleton.csproj` compiles with **zero `UnityEngine` references** (verified by `dotnet build` on this machine, 0 warnings 0 errors). See `dependency-graph.md`.

---

## What still needs tool verification on Day 13

1. **LCOM4 for `DebugLogging` (BEFORE)** — hand-estimate of ~3 needs a real connected-components walk. Understand reports this directly; SonarQube reports LCOM-HS, which has a different scale (0–1) — both are useful, neither replaces the other.
2. **RFC for `DebugTabView`** — hand-count of ~18 is approximate (`StringBuilder.Append` is called in a loop, `string.Format` style interpolations may or may not be counted distinctly). Tool-verified value is authoritative.
3. **CBO for `DebugLogging` (BEFORE)** — depending on whether the tool counts `System.IO.File`, `System.IO.Directory`, and `System.IO.Path` as distinct collaborators (used in the log-rotation block), the figure may settle between 10 and 13. Either way it stays under the orchestrator threshold.
4. **NDepend rule** confirming `iDaVIE.Desktop.DebugTab` does not transitively reach `UnityEngine` — see [`dependency-graph.md` → Tool verification needed](dependency-graph.md#tool-verification-needed). Hand inspection passes; `dotnet build` on the skeleton csproj is a stronger witness; NDepend is needed for the panel snapshot.

A short follow-up commit on Day 13 will replace this paragraph with the tool snapshot.
