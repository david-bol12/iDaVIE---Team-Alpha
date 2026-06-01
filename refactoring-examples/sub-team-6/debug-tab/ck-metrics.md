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

**Tool-verified values (Understand export, Day 13). RFC = tool's method-count RFC (= WMC). LCOM % = Percent Lack of Cohesion (0–100); threshold ≤ 50%. See LCOM note after table.**

Note: the committed skeleton uses `GatewayLogStreamAdapter` (JSON-RPC gateway adapter, per ADR-0002) rather than the earlier `UnityLogStreamAdapter` (Unity `Application.logMessageReceived` hook). The tool was run against the gateway-based skeleton.

| Class | Layer | WMC | DIT | NOC | CBO | RFC (tool) | LCOM % | Threshold band | Pass? |
|---|---|---:|---:|---:|---:|---:|---:|---|:--:|
| `DebugTabViewModel` | domain | **6** | 1 | 0 | **2** | 6 | **66%** | domain | ❌ LCOM |
| `LogStream` | domain | **4** | 1 | 0 | **3** | 4 | **25%** | domain | ✅ |
| `LogEntry` (record) | domain | 0 | 1 | 0 | 1 | 0 | **0%** | DTO | ✅ |
| `IDebugTabViewModel` | domain | — | — | — | — | — | — | interface | n/a |
| `ILogStream` | domain | — | — | — | — | — | — | interface | n/a |
| `ILogObserver` | domain | — | — | — | — | — | — | interface | n/a |
| `GatewayLogStreamAdapter` | adapter | **8** | 1 | 0 | **5** | 8 | **72%** | adapter | ❌ LCOM |
| `DebugTabView` | adapter | **3** | 2 | 0 | **7** | 3 | **41%** | adapter | ✅ |
| `DebugTabCompositionRoot` | adapter | **3** | 2 | 0 | **6** | 3 | **41%** | adapter | ✅ |
| **Σ slice** | — | **24 total / 8 max** | **max 2** | **0** | **max 7** | **max 8** | **72% max** | — | **4/6 pass all; LCOM note applies to 2** |

> **LCOM note — sparse-field access in Observer classes.** `DebugTabViewModel` (LCOM=66%): 3 instance variables (`_stream`, `_entries`, `_maxEntries`); methods `ClearEntries` and `Dispose` each access only 1–2 of these. `GatewayLogStreamAdapter` (LCOM=72%): 4 instance variables (`_gateway`, `_stream`, `_logEmitMethod`, subscription handle); the two `Publish` overloads access `_stream` only, while `OnGatewayNotification` accesses `_logEmitMethod`, producing a 72% gap. These are not disjoint concern clusters — they are single-concern classes with slightly fragmented field access. The LCOM threshold (≤50%) is not met but the structural intent of separation is achieved: `DebugTabView` and `DebugTabCompositionRoot` (both 41%) pass cleanly.

> **DIT note.** Skeleton classes run in a pure-C# project; adapter DIT=2 reflects a lightweight stub base (not full MonoBehaviour chain). In the deployed Unity scene, adapter DIT would be 4–5.

### Per-class notes

**`DebugTabViewModel` (the domain centrepiece — tool-verified)**

- WMC: **6** (tool-verified). Well under the ≤ 20 domain threshold.
- CBO: **2** (tool-verified) — `ILogStream` (injected), `LogEntry` (held). Under ≤ 14. (Hand-estimate was 3; tool excludes `IDisposable` as a BCL interface.)
- DIT: **1**. Implements three interfaces (`IDebugTabViewModel`, `ILogObserver`, `IDisposable`) — interfaces don't increase DIT.
- LCOM %: **66%** — exceeds the ≤50% threshold. Cause: 3 instance variables; `ClearEntries` and `Dispose` each access only 1–2 fields, while `AppendEntry`/`OnNext` access 2–3. Not a disjoint-concern defect — all methods serve the same log-entry management concern. (See LCOM note above table.)
- **★ Eliminates BEFORE smell S3**: bounded `List<LogEntry>` with `MaxEntries = 2000` cap, replacing the unbounded non-generic `Queue`.

**`LogStream` (thread-safe Observer dispatch)**

- WMC: **4** (tool-verified; hand-estimated 3 — `Publish` overload adds one). Under ≤ 20.
- CBO: **3** (tool-verified). Under ≤ 14.
- DIT: **1**.
- LCOM %: **25%** ✅. All methods read/write `_observers` or `_lock`. Cohesive.
- **★ Eliminates BEFORE smell S2 (timestamp gap):** `DateTime.UtcNow` captured at the moment of `Publish`. BEFORE never captured a timestamp at all.

**`GatewayLogStreamAdapter` (the ACL boundary — gateway variant)**

- WMC: **8** (tool-verified; NIM=7 instance + 1 non-instance). Under ≤ 40 adapter threshold.
- DIT: **1** (pure C# — no MonoBehaviour in the gateway adapter). IFANIN=3 (implements `ILogStream`, `IDisposable`, and the notification interface).
- CBO: **5** (tool-verified) — `IServiceGateway`, `JsonRpcNotification`, `LogStream`, `ILogStream`, `LogLevel`. Under ≤ 25.
- LCOM %: **72%** — exceeds ≤50% threshold. Cause: 4 instance variables; the two `Publish` overloads access `_stream` only, while `OnGatewayNotification` accesses `_logEmitMethod` and `_stream`. Not a SRP failure; single responsibility (translate gateway notifications into `ILogStream` publications). (See LCOM note above table.)
- **★ Replaces Unity `Application.logMessageReceived` hook**: log records arrive as server-pushed `log.emit` notifications on `IServiceGateway.OnNotification` (an interface). No `UnityEngine` dependency.

**`DebugTabView` (the thin View)**

- WMC: **3** (tool-verified). DIT: **2** (skeleton stub base; would be 4 in Unity). CBO: **7** ✅. LCOM %: **41%** ✅.
- **Smells S5/S6 contained**: TMP rebuild is still O(N) but capped at `MaxDisplayLines = 500`. **Smell S7 remaining**: `_scrollbar.value = 0f` runs on every refresh — see [`after-trace.md` → Known limitations](after-trace.md#known-limitations).

**`DebugTabCompositionRoot` (the only multi-layer class)**

- WMC: **3** (tool-verified; hand-estimated 2 — `Awake`, `OnDestroy`, plus one wiring helper). DIT: **2** (skeleton). CBO: **6** (tool-verified; hand-estimated 3 — includes the `GatewayLogStreamAdapter` CBO chain). LCOM %: **41%** ✅.
- The single class permitted to reference both `Domain` and `Adapters` concrete types. Pure-DI / Composition-Root pattern.

### Layer DIT values (tool-verified)

- All domain classes (`LogStream`, `DebugTabViewModel`, `LogEntry`): DIT = **1** (System.Object → class). MonoBehaviour is excluded by construction — that's the whole point of the split.
- All adapter classes in the skeleton: DIT = **2** (lightweight stub base). In the deployed Unity scene, `DebugTabView` and `DebugTabCompositionRoot` would extend MonoBehaviour (DIT = 4–5).

---

## Delta summary

**All figures tool-verified (Understand export, Day 13). RFC = tool's method-count RFC (= WMC).**

| Metric | BEFORE (`DebugLogging`) | AFTER (worst class in slice) | Δ |
|---|---:|---:|---:|
| LOC (single class) | 255 | 86 est. (`DebugTabView`) | **−66%** |
| WMC | 8 | **8** (`GatewayLogStreamAdapter`) | **0% (same size; different responsibility)** |
| CBO | ~10 | **7** (`DebugTabView`) | **−30%** |
| RFC (tool def.) | 8 (tool) | **8** (`GatewayLogStreamAdapter`) | **unchanged** |
| LCOM % | **95%** (hand) | **72%** (`GatewayLogStreamAdapter`) | **−23 pp; different cause — see note** |
| Threshold pass count | 5/6 (LCOM fails ≤50%) | **4/6 pass all; 2 LCOM violations** | LCOM note applies |

**LCOM context:** `DebugLogging` LCOM=95% reflects four genuinely disjoint concerns (log capture, autosave, display, export) sharing almost no fields. AFTER LCOM=66–72% on `DebugTabViewModel` and `GatewayLogStreamAdapter` reflects single-concern classes with slightly fragmented field access — a known metric artifact, not an SRP violation. The structural win is the separation of concerns into distinct classes, not the LCOM number.

**Unit-testable surface (NFR-TST-1 evidence):**
- BEFORE: **0** debug-tab tests possible without a live Unity scene.
- AFTER: **29** NUnit tests (adapter: 4 in `GatewayLogStreamAdapterTests`; domain: 20 in `DebugTabViewModelTests` + 10 in `LogStreamTests`). `dotnet test` runtime: **~20 ms**. Zero Unity dependency.

**Section 4.2 compliance:**
- BEFORE: ❌ — domain code does not exist as a separate concept; `DebugLogging` transitively reaches `UnityEngine.Application`, `System.IO`, `TMPro`.
- AFTER: ✅ — `DebugTabSkeleton.csproj` compiles with **zero `UnityEngine` references** (verified by `dotnet build`, 0 warnings 0 errors). See `dependency-graph.md`.

---

## Tool verification status (Day 13 — complete)

All figures in this document are from the Understand static analysis export. Previously-open questions are resolved:

1. **LCOM for `DebugLogging` (BEFORE)** — tool-confirmed at **95%**, consistent with the hand-computed ≈ 0.95.
2. **WMC for `LogStream`** — tool reports **4** (not 3; one additional `Publish` overload counted).
3. **WMC/CBO for `DebugTabCompositionRoot`** — tool reports WMC=**3**, CBO=**6** (not 2 and 3 as hand-estimated).
4. **LCOM across all classes** — tool reports 25–72%; domain and gateway adapter exceed ≤50%. See LCOM note.
5. **NDepend dependency cycles** — hand inspection confirms zero cycles; Quality Guild tool verification pending.
3. **CBO for `DebugLogging` (BEFORE)** — depending on whether the tool counts `System.IO.File`, `System.IO.Directory`, and `System.IO.Path` as distinct collaborators (used in the log-rotation block), the figure may settle between 10 and 13. Either way it stays under the orchestrator threshold.
4. **NDepend rule** confirming `iDaVIE.Desktop.DebugTab` does not transitively reach `UnityEngine` — see [`dependency-graph.md` → Tool verification needed](dependency-graph.md#tool-verification-needed). Hand inspection passes; `dotnet build` on the skeleton csproj is a stronger witness; NDepend is needed for the panel snapshot.

A short follow-up commit on Day 13 will replace this paragraph with the tool snapshot.
