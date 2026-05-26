# Debug Tab — Before-State Metrics

## TL;DR

**Stricter recount that supersedes the CK table in `before-trace.md`.** Uses McCabe CC-weighted WMC (= **21**, not 9) and Henderson-Sellers LCOM_HS (= **0.95**, not ~0.45). Per-method cyclomatic-complexity breakdown shows `Start()` at CC 9 (log-rotation block alone). Full CBO fan-out catalogued as 21 types (10 Unity/UI + 1 app + 2 third-party + 8 BCL — 12 non-BCL). 38-entry RFC response set tabulated. Method-field access matrix proves the LCOM failure mathematically: 6 fields, 4 disjoint clusters, `OnEnable` / `OnDisable` / `DetermineHardware` share zero fields with any other method. **Verdict:** 5/6 metrics pass, LCOM_HS fails decisively — the quantitative signal for the S8 (four-concerns-in-one-class) smell. Ends with per-class AFTER projections (WMC down 19%, CBO ≤4 per class, all LCOM_HS < 0.40).

---

**Source:** `Assets/Scripts/Debuggers/DebugLogging.cs` (255 lines)
**Branch:** `team6`
**Companion files:** `before-trace.md` (smell catalogue), `log-origin-trace.md` (call-site origin)

> **Correction to `before-trace.md` §CK table:** that table uses WMC = 9 (method count,
> unit weight = 1) and LCOM ≈ 0.45 (rough estimate). This file supersedes those figures
> using McCabe CC-weighted WMC and the Henderson-Sellers LCOM_HS formula.

---

## 1. Per-method complexity

CC = McCabe cyclomatic complexity = binary branch count + 1.
Branches counted: `if`, `else if`, `for`, `foreach`, `while`, `catch`, and lambda `if`.

| Method | Line range | LOC | Branch points | CC |
|---|---|---|---|---|
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

---

## 2. Class-level CK metrics

| Metric | Value | Threshold | Status | Notes |
|---|---|---|---|---|
| WMC | 21 | ≤ 40 (adapters) | ✅ | Sum of CC per method. See §1. |
| DIT | 1 | ≤ 4 | ✅ | `DebugLogging → MonoBehaviour` only |
| NOC | 0 | ≤ 5 | ✅ | No subclasses in codebase |
| CBO | 21 all types / **12 non-BCL** | ≤ 25 (adapters) | ✅ | Full breakdown in §3 |
| RFC | **46** | ≤ 50 | ✅ (borderline) | 8 own + 38 external calls; see §4 |
| LCOM_HS | **0.95** | ≤ 0.50 | ❌ | See §5. Four distinct responsibility clusters. |

`DebugLogging` passes every CK threshold **except LCOM_HS**. The primary refactoring
arguments are structural and testability-driven (smells S1, S8 in `before-trace.md`),
now confirmed by the LCOM failure.

---

## 3. CBO coupling fan-out

### 3.1 Unity / UI types (10 types)

| Type | Namespace | Access point |
|---|---|---|
| `MonoBehaviour` | `UnityEngine` | Inheritance — `DebugLogging : MonoBehaviour` |
| `Application` | `UnityEngine` | `logMessageReceived +=/-=`; `dataPath`; `version` |
| `Debug` | `UnityEngine` | `Debug.Log(...)` — 5 distinct call sites within the class itself |
| `SystemInfo` | `UnityEngine` | `processorType`, `systemMemorySize`, `graphicsDeviceName`, `graphicsMemorySize` in `DetermineHardware()` |
| `PlayerPrefs` | `UnityEngine` | `GetInt/GetString/SetInt/SetString/Save` across `Start()` and `saveToFileClick()` |
| `Canvas` | `UnityEngine` | `Canvas.ForceUpdateCanvases()` in `Start()` |
| `LogType` | `UnityEngine` | Parameter type of `HandleLog`; compared in the exception branch |
| `TMP_InputField` | `TMPro` | Field `logOutput`; `Rebuild()` in `Start()`; `.text =` in `HandleLog()` |
| `Scrollbar` | `UnityEngine.UI` | Field `debugScrollbar`; `.value = 1.0f` in `HandleLog()` |
| `Button` | `UnityEngine.UI` | Field `saveButton`; `.onClick.AddListener(saveToFileClick)` in `Start()` |

### 3.2 Application types (1 type)

| Type | Namespace | Access point |
|---|---|---|
| `Config` | `VolumeData` | `Config.Instance.numberOfLogsToKeep` in `Start()` |

### 3.3 Third-party (2 types)

| Type | Namespace | Access point |
|---|---|---|
| `StandaloneFileBrowser` | `SFB` | `SaveFilePanelAsync(...)` in `saveToFileClick()` |
| `ExtensionFilter` | `SFB` | Constructor (×2) in `saveToFileClick()` |

### 3.4 BCL (8 types)

| Type | Namespace | Access point |
|---|---|---|
| `Queue` | `System.Collections` | Field `debugLogQueue`; `Enqueue()` in `HandleLog()`; `foreach` in `HandleLog()` and `SaveToFile()` |
| `StringBuilder` | `System.Text` | Local; `Append/ToString` in `HandleLog()` and `SaveToFile()` |
| `StreamWriter` | `System.IO` | Opened and closed on **every** `AutoSave()` and `SaveToFile()` call — smell S4 |
| `DirectoryInfo` | `System.IO` | `new DirectoryInfo(Application.dataPath)` in `Start()` and `saveToFileClick()` |
| `Directory` | `System.IO` | `Exists / CreateDirectory / GetFiles` in `Start()` and `saveToFileClick()` |
| `File` | `System.IO` | `Delete / Move` in log-rotation block of `Start()` |
| `Path` | `System.IO` | `Combine / GetDirectoryName` in `Start()` and `saveToFileClick()` |
| `Regex` / `Match` | `System.Text.RegularExpressions` | Timestamp extraction in log-rotation block of `Start()` |

**CBO summary:**
- All types: 21
- Non-BCL (Unity + app + third-party): **12** — well within the ≤ 25 adapter threshold

---

## 4. RFC response set

RFC = 8 own methods + 38 distinct externally-called methods/constructors/event accessors.

**RFC = 46** (threshold ≤ 50; borderline).

| # | Signature | Called from |
|---|---|---|
| 1 | `new DirectoryInfo(string)` | `Start`, `saveToFileClick` |
| 2 | `Path.Combine(string, string)` | `Start`, `saveToFileClick` |
| 3 | `Directory.Exists(string)` | `Start`, `saveToFileClick` |
| 4 | `Directory.CreateDirectory(string)` | `Start` |
| 5 | `Directory.GetFiles(string, string)` | `Start` |
| 6 | `Enumerable.FirstOrDefault<string>()` | `Start` |
| 7 | `File.Delete(string)` | `Start` |
| 8 | `new Regex(string)` | `Start` |
| 9 | `Regex.Match(string)` | `Start` |
| 10 | `File.Move(string, string)` | `Start` |
| 11 | `Debug.Log(object)` | `Start`, `DetermineHardware` |
| 12 | `Application.logMessageReceived +=` (add accessor) | `OnEnable` |
| 13 | `Application.logMessageReceived -=` (remove accessor) | `OnDisable` |
| 14 | `Button.onClick.AddListener(UnityAction)` | `Start` |
| 15 | `PlayerPrefs.GetInt(string)` | `Start` |
| 16 | `PlayerPrefs.GetString(string)` | `Start`, `saveToFileClick` |
| 17 | `PlayerPrefs.SetInt(string, int)` | `Start` |
| 18 | `PlayerPrefs.SetString(string, string)` | `saveToFileClick` (lambda) |
| 19 | `PlayerPrefs.Save()` | `Start`, `saveToFileClick` (lambda) |
| 20 | `Canvas.ForceUpdateCanvases()` | `Start` |
| 21 | `TMP_InputField.Rebuild(CanvasUpdate)` | `Start` |
| 22 | `SystemInfo.processorType` (property) | `DetermineHardware` |
| 23 | `SystemInfo.systemMemorySize` (property) | `DetermineHardware` |
| 24 | `SystemInfo.graphicsDeviceName` (property) | `DetermineHardware` |
| 25 | `SystemInfo.graphicsMemorySize` (property) | `DetermineHardware` |
| 26 | `Queue.Enqueue(object)` | `HandleLog` |
| 27 | `new StringBuilder()` | `HandleLog`, `SaveToFile` |
| 28 | `StringBuilder.Append(string)` | `HandleLog`, `SaveToFile` |
| 29 | `StringBuilder.ToString()` | `HandleLog`, `SaveToFile` |
| 30 | `TMP_InputField.text` (property setter) | `HandleLog` |
| 31 | `Scrollbar.value` (property setter) | `HandleLog` |
| 32 | `new ExtensionFilter(string, string)` | `saveToFileClick` |
| 33 | `StandaloneFileBrowser.SaveFilePanelAsync(...)` | `saveToFileClick` |
| 34 | `string.Equals(string)` | `saveToFileClick` (lambda) |
| 35 | `Path.GetDirectoryName(string)` | `saveToFileClick` (lambda) |
| 36 | `new StreamWriter(string, bool)` | `AutoSave`, `SaveToFile` |
| 37 | `StreamWriter.Write(string)` | `AutoSave`, `SaveToFile` |
| 38 | `StreamWriter.Close()` | `AutoSave`, `SaveToFile` |

---

## 5. LCOM analysis

**Formula:** Henderson-Sellers LCOM_HS = (M − avg_mA) / (M − 1)
where M = number of methods, avg_mA = average number of methods that directly access each instance field.

### 5.1 Method-field access matrix

| Field | Type | Accessed by | Count (mA) |
|---|---|---|---|
| `logOutput` | `TMP_InputField` | `Start` (Rebuild), `HandleLog` (.text =) | 2 |
| `debugScrollbar` | `Scrollbar` | `HandleLog` (.value =) | 1 |
| `saveButton` | `Button` | `Start` (onClick.AddListener) | 1 |
| `autosavePath` | `string` | `Start` (writes), `AutoSave` (reads) | 2 |
| `pluginSavePath` | `string` | *declared but never accessed* | 0 |
| `debugLogQueue` | `Queue` | `HandleLog` (Enqueue ×2, foreach), `SaveToFile` (foreach) | 2 |

### 5.2 Computed LCOM_HS

With all 6 declared fields (F = 6):
- Sum of mA = 2 + 1 + 1 + 2 + 0 + 2 = 8
- avg_mA = 8 / 6 = 1.33
- **LCOM_HS = (8 − 1.33) / (8 − 1) = 6.67 / 7 = 0.95** ❌

Excluding `pluginSavePath` (unused field, F = 5):
- avg_mA = 8 / 5 = 1.60
- **LCOM_HS = (8 − 1.60) / 7 = 6.40 / 7 = 0.91** ❌

Both variants exceed the threshold of ≤ 0.50.

### 5.3 Why LCOM fails

The class has four responsibility clusters with minimal shared field access:

| Cluster | Methods | Shared fields |
|---|---|---|
| Lifecycle init (log rotation + wiring) | `Start` | `logOutput`, `saveButton`, `autosavePath` |
| Unity event hook | `OnEnable`, `OnDisable` | none |
| Hardware report | `DetermineHardware` | none |
| Log display + autosave | `HandleLog`, `AutoSave`, `SaveToFile` | `debugLogQueue`, `autosavePath`, `logOutput`, `debugScrollbar` |

`OnEnable`, `OnDisable`, and `DetermineHardware` share zero fields with any other method.
This structural split is the quantitative signal for the SRP failure identified as smell S8.

---

## 6. Smell-to-metric cross-reference

| Smell (from `before-trace.md`) | Metric signal |
|---|---|
| S1 — `Application.logMessageReceived` static hook | CBO: adds `Application`; RFC entries 12 and 13 |
| S2 — unstructured `(string, string, LogType)` tuple | No direct metric signal; motivates `ILogStream.Publish` interface design |
| S3 — non-generic `Queue` (unbounded, untyped) | CBO: `System.Collections.Queue`; RFC entry 26 |
| S4 — `StreamWriter` opened per message in `AutoSave` | RFC: entries 36–38 appear in response set for every log event |
| S5 — O(N) full-queue rebuild on every `HandleLog` call | CC of `HandleLog` = 3 (not a CC signal); performance smell confirmed by LOC and foreach scope |
| S6 — `TMP_InputField.text` whole-string replacement | CBO: `TMP_InputField`; RFC entry 30 |
| S7 — scroll position forced every message | CBO: `Scrollbar`; RFC entry 31 |
| S8 — four responsibilities in one class | **LCOM_HS = 0.91–0.95** ❌ |
| S9 — 40+ `Debug.Log` call sites in `CanvassDesktop` | Not a `DebugLogging` metric; documented in `log-origin-trace.md` |

---

## 7. After-state projection

The after-state proposal splits `DebugLogging` into four classes (skeletons at `debug-tab/skeleton/` and `debug-tab/adapters/`).

| After-state class | Responsibility | Projected WMC | Projected CBO (non-BCL) | Projected LCOM_HS |
|---|---|---|---|---|
| `UnityLogStreamAdapter` | Subscribe to `Application.logMessageReceived`; translate to `ILogStream.Publish` | ~3 | 3 (`Application`, `LogType`, `ILogStream`) | < 0.30 |
| `LogStream` | Thread-safe `ILogStream` impl; `ObservableCollection<LogEntry>` | ~4 | 2 (`LogEntry`, `ILogObserver`) | < 0.30 |
| `DebugTabViewModel` | Subscribe `ILogStream` via `ILogObserver`; expose `IReadOnlyList<LogEntry>` + `ClearEntries` | ~6 | 3 (`ILogStream`, `LogEntry`, `IDebugTabViewModel`) | < 0.40 |
| `DebugTabView` (Unity) | Bind to ViewModel; update `ListView`; wire Save button | ~4 | 4 (`DebugTabViewModel`, `TMP_InputField`, `Scrollbar`, `Button`) | < 0.30 |
| **Totals** | | **~17** (↓ 19 % from 21) | **≤ 4 per class** (↓ from 12 in single class) | **all ✅** |

WMC reduction comes primarily from extracting the log-rotation block (`Start()` CC = 9)
into a dedicated `LogFileRotator` class (not yet written; planned for after-state architecture doc).
