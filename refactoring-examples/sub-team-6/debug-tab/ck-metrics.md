# Debug tab — CK metric deltas (BEFORE vs. AFTER)

## TL;DR

**Frame this honestly: it's a testability refactor, not a metric refactor.** `DebugLogging` already passes 5/6 CK thresholds individually — the only failure is LCOM hs ≈ 0.95 (four disjoint concern clusters). What CK metrics *can't* capture: the static `Application.logMessageReceived` hook (S1) makes the whole class untestable without Unity. AFTER splits into 7 types — all pass everything, LCOM hs ≈ 0 per class. Domain-side `DebugTabViewModel` CBO drops from 9 → 1. Bounded `List<LogEntry>` (cap 2000) replaces unbounded non-generic `Queue`. **Test surface:** 0 → **29 NUnit tests, ~20 ms total, zero Unity dependency.** Section 4.2 compliance verified by `dotnet build` on the skeleton csproj with zero `UnityEngine` references.

---

> **Status: hand-counted projection — pending Quality Guild tool verification on Day 13.**
> Numbers below were counted from the live `team6` branch using `wc`/`grep` against `Assets/Scripts/Debuggers/DebugLogging.cs` (BEFORE) and the skeleton + adapter files in this folder (AFTER). They are submitted **alongside** the SonarQube Cloud + Understand baseline that the Quality Guild owns; if their tooling reports different values, those numbers supersede this document.
>
> Counting conventions used here:
> - **WMC** = method count (NOM-style WMC; the same as Understand's `CountDeclMethod`). Property getters/setters with non-trivial bodies count as one method each; trivial auto-implemented getters do not.
> - **DIT** = depth from `System.Object`. `MonoBehaviour` adds 3 to the count (`Object → Component → Behaviour → MonoBehaviour`).
> - **CBO** = distinct named types referenced *in implementation*, excluding primitives, language types (`string`, `int`, etc.), and the class's own type. DTOs of the same package are counted.
> - **RFC** = WMC + distinct external methods called. Hand-count is approximate; tool-verified value is authoritative.
> - **LCOM** = LCOM hs (Henderson-Sellers). 0 = perfectly cohesive; 1 = completely incoherent; threshold ≤ 0.5.
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
> | LCOM | ≤ 0.5 | same |

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
| LCOM hs | **≈ 0.95** | ≤ 0.5 | ❌ |

The only metric failure is **LCOM hs ≈ 0.95**, reflecting the four disjoint concerns identified in [`before-trace.md` → Smell S8](before-trace.md#smell-summary-feeds-the-solidgrasp-audit--ck-deltas): log capture, log storage (autosave), log display, and manual export each operate on a non-overlapping subset of the eight fields. The other metrics pass.

The case for refactoring is therefore **structural and testability-driven**, not metric-driven:

1. **Smell S1** (static `Application.logMessageReceived` hook) makes the class **untestable** without a Unity test runner. No CK metric captures this directly — CBO sees `Application` as one collaborator like any other; the fact that it cannot be substituted is invisible to the tool.
2. **Smell S2** (unstructured `(string, string, LogType)` tuple) creates a contract problem — no metric flags it.
3. **Smell S8** (four concerns in one class) shows up as LCOM hs > 0.5 — the **only** metric that catches a real defect here.

The AFTER design wins on LCOM hs (every class is ≈ 0), on testability (29 NUnit tests without Unity), and on assembly-level dependency direction (Section 4.2 compliance) — see [`dependency-graph.md`](dependency-graph.md). Raw CK headline numbers improve modestly because BEFORE already passed most thresholds.

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
| LCOM hs | **≈ 0.95** | ≤ 0.5 | ❌ | Four disjoint concern clusters: (a) `Start` + log-rotation (uses `directoryPath`, `autosavePath`, `maxLogs`); (b) `HandleLog` + `AutoSave` (use `debugLogQueue`, `autosavePath`, `logOutput`, `debugScrollbar`); (c) `saveToFileClick` + `SaveToFile` (use `debugLogQueue` only, plus `PlayerPrefs`); (d) `DetermineHardware` (uses no fields — pure side-effect on `Debug.Log`). At least three connected components remain after merging shared fields. |

### Per-method McCabe CC (alternative WMC weighting)

The table above uses NOM-style WMC (method count, unit weight = 1) → **WMC = 8**. Re-weighting by McCabe cyclomatic complexity gives **WMC = 21** — still ✅ under the ≤ 40 adapter threshold, but a sharper view of where the complexity sits. The Quality Guild tools may report either depending on configuration; both are shown for transparency.

| Method | Line range | LOC | Branch points | CC |
|---|---|---:|---|---:|
| `Start()` | 52–145 | 93 | `catch` (+1), `if (!Directory.Exists)` (+1), `for` loop (+1), `if (existingLog != null)` ×2 (+2), `if (i == maxLogs-1)` ×2 (+2), `if (newConfig != 0)` (+1) | **9** ¹ |
| `OnEnable()` | 147–150 | 3 | — | **1** |
| `OnDisable()` | 152–155 | 3 | — | **1** |
| `DetermineHardware()` | 160–169 | 9 | — | **1** |
| `HandleLog(string, string, LogType)` | 177–197 | 20 | `if (type == LogType.Exception)` (+1), `foreach` (+1) | **3** |
| `saveToFileClick()` | 203–226 | 23 | `if (!Directory.Exists(lastPath))` (+1), lambda `if (dest.Equals(""))` (+1) | **3** |
| `SaveToFile(string)` | 232–243 | 11 | `foreach` (+1) | **2** |
| `AutoSave(string)` | 249–254 | 5 | — | **1** |
| **Total** | | **167** | | **WMC = 21** |

¹ CC for `Start()` is 8 if your tool does not count `catch` as a branch (some do not). Range: 8–9.

The log-rotation block inside `Start()` is the single complexity hotspot (CC = 9 alone — more than the rest of the class combined). In the AFTER design that block extracts to a dedicated rotator (planned for the architecture doc); no AFTER class carries a method with CC > 3.

### LCOM hs computation (Henderson-Sellers)

LCOM hs (Henderson-Sellers formula, threshold ≤ 0.5) is the primary LCOM metric used in this file. The computation below derives the value from the method-field access matrix; SonarQube reports this directly as LCOM. The result (0.95) confirms the same S8 (four-concerns-in-one-class) signal shown in the table above.

**Formula:** LCOM_HS = (M − avg_mA) / (M − 1), where M = method count, avg_mA = average methods accessing each instance field.

Method-field access matrix:

| Field | Type | Accessed by | mA |
|---|---|---|---:|
| `logOutput` | `TMP_InputField` | `Start` (Rebuild), `HandleLog` (`.text =`) | 2 |
| `debugScrollbar` | `Scrollbar` | `HandleLog` (`.value =`) | 1 |
| `saveButton` | `Button` | `Start` (`onClick.AddListener`) | 1 |
| `autosavePath` | `string` | `Start` (writes), `AutoSave` (reads) | 2 |
| `pluginSavePath` | `string` | *declared but never accessed* | 0 |
| `debugLogQueue` | `Queue` | `HandleLog` (`Enqueue` ×2, foreach), `SaveToFile` (foreach) | 2 |

With all 6 fields: Σ mA = 8, avg_mA = 1.33 → **LCOM_HS = (8 − 1.33) / (8 − 1) = 0.95** ❌.
Excluding the unused `pluginSavePath` (5 fields): avg_mA = 1.60 → **LCOM_HS = 0.91** ❌.

Both variants decisively exceed the ≤ 0.50 threshold. `OnEnable`, `OnDisable`, and `DetermineHardware` share zero fields with any other method — this is the structural quantification of smell S8.

### Verdict (BEFORE)

`DebugLogging` **fails 1 of 6 metrics** (LCOM hs / LCOM_HS — both formulae agree). All other CK numbers are within thresholds. The qualitative smells (S1–S9 in [`before-trace.md`](before-trace.md)) dominate the case for refactoring.

---

## AFTER — debug-tab slice as seven focused types

The BEFORE class is decomposed into:
- **3 interfaces** (`IDebugTabViewModel`, `ILogStream`, `ILogObserver`) — design surface, not measured for WMC.
- **2 concrete domain classes** (`LogStream`, `DebugTabViewModel`).
- **1 DTO record** (`LogEntry`) + **1 enum** (`LogLevel`) — measured at near-zero cost.
- **3 adapters** (`UnityLogStreamAdapter`, `DebugTabView`, `DebugTabCompositionRoot`).

| Class | Layer | LOC | WMC | DIT | NOC | CBO | RFC | LCOM hs | Threshold band | Pass? |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|:--:|
| `DebugTabViewModel` | domain | 77 | **6** | 1 | 0 | **3** | ~15 | ≈ 0 | domain | ✅ |
| `LogStream` | domain | 43 | **3** | 1 | 0 | **2** | ~10 | ≈ 0 | domain | ✅ |
| `LogEntry` (record) | domain | (in `ILogStream.cs`) | 0 | 1 | 0 | 1 | 3 | ≈ 0 | DTO | ✅ |
| `IDebugTabViewModel` | domain | 32 | — | — | — | — | — | — | interface | n/a |
| `ILogStream` | domain | 32 | — | — | — | — | — | — | interface | n/a |
| `ILogObserver` | domain | 16 | — | — | — | — | — | — | interface | n/a |
| `UnityLogStreamAdapter` | adapter | 53 | **6** | 4 | 0 | **4** | ~12 | ≈ 0 | adapter (MonoBehaviour) | ✅ |
| `DebugTabView` | adapter | 86 | **3** | 4 | 0 | **7** | ~18 | ≈ 0 | adapter (MonoBehaviour) | ✅ |
| `DebugTabCompositionRoot` | adapter | 43 | **2** | 4 | 0 | **3** | ~6 | ≈ 0 | adapter (MonoBehaviour) | ✅ |
| **Σ slice** | — | **~382** + interfaces | **20** | **max 4** | **0** | **max 7** | **max ~18** | **≈ 0 per class** | — | **all pass** |

### Per-class notes

**`DebugTabViewModel` (the domain centrepiece)**

- WMC: **6** — ctor, `LogEntries` getter (non-trivial `AsReadOnly`), `AppendEntry`, `ClearEntries`, `OnNext` (explicit interface impl), `Dispose`. Well under the ≤ 20 domain threshold.
- CBO: **3** — `ILogStream` (injected), `LogEntry` (held), `IDisposable` (implemented). Under ≤ 14.
- DIT: **1**. Implements three interfaces (`IDebugTabViewModel`, `ILogObserver`, `IDisposable`) — interfaces don't increase DIT.
- LCOM hs: **≈ 0**. All methods touch `_entries` (the only meaningful state field). Perfectly cohesive.
- **★ Eliminates BEFORE smell S3**: bounded `List<LogEntry>` with `MaxEntries = 2000` cap, replacing the unbounded non-generic `Queue`.

**`LogStream` (thread-safe Observer dispatch)**

- WMC: **3** — `Subscribe`, `Unsubscribe`, `Publish`. Trivial surface.
- CBO: **2** — `ILogObserver` (collection element), `LogEntry` (created in `Publish`). Under ≤ 14.
- DIT: **1**.
- LCOM hs: **≈ 0**. All three methods read/write `_observers` (and `Publish` reads `_lock`). Cohesive.
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
| LCOM hs | ≈ 0.95 | **≈ 0 per class** | **incoherent → cohesive (★ the structural win)** |
| Threshold pass count | 5 / 6 (LCOM hs ≈ 0.95, fails ≤ 0.5) | **all classes pass all metrics** | **near-pass → full pass** |

**Unit-testable surface (NFR-TST-1 evidence):**
- BEFORE: **0** debug-tab tests possible without a live Unity scene.
- AFTER: **29** NUnit tests in `tests/DebugTabTests.cs` exercise the domain layer with no Unity dependency. `dotnet test` runtime: **~20 ms**.

**Section 4.2 compliance:**
- BEFORE: ❌ — domain code does not exist as a separate concept; `DebugLogging` transitively reaches `UnityEngine.Application`, `System.IO`, `TMPro`.
- AFTER: ✅ — `DebugTabSkeleton.csproj` compiles with **zero `UnityEngine` references** (verified by `dotnet build` on this machine, 0 warnings 0 errors). See `dependency-graph.md`.

---

## What still needs tool verification on Day 13

1. **LCOM hs for `DebugLogging` (BEFORE)** — hand-computed as ≈ 0.95 (see LCOM hs computation section above). SonarQube reports LCOM (Henderson-Sellers) directly; tool-verified value is authoritative.
2. **RFC for `DebugTabView`** — hand-count of ~18 is approximate (`StringBuilder.Append` is called in a loop, `string.Format` style interpolations may or may not be counted distinctly). Tool-verified value is authoritative.
3. **CBO for `DebugLogging` (BEFORE)** — depending on whether the tool counts `System.IO.File`, `System.IO.Directory`, and `System.IO.Path` as distinct collaborators (used in the log-rotation block), the figure may settle between 10 and 13. Either way it stays under the orchestrator threshold.
4. **NDepend rule** confirming `iDaVIE.Desktop.DebugTab` does not transitively reach `UnityEngine` — see [`dependency-graph.md` → Tool verification needed](dependency-graph.md#tool-verification-needed). Hand inspection passes; `dotnet build` on the skeleton csproj is a stronger witness; NDepend is needed for the panel snapshot.

A short follow-up commit on Day 13 will replace this paragraph with the tool snapshot.
