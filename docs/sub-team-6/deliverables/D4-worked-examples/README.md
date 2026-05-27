# D4 — Worked Refactoring Examples

Two worked examples showing before/after for the Desktop GUI refactoring proposal. Each example includes comprehensive before/after documentation with UML class diagrams, dependency graphs, CK metrics, sequence diagrams, and skeleton implementations.

All source lives under [`refactoring-examples/sub-team-6/`](../../../../refactoring-examples/sub-team-6/).

---

## Example 1 — File Tab

**Objective:** Refactoring the file-open flow from a direct native-plugin call to a ViewModel command via a service gateway.

**Problem:** `CanvassDesktop` (1899 lines) is a monolithic god-class that directly couples UI logic to native DLL calls via `FitsReader`, scene-graph mutations via `VolumeDataSetRenderer`, and global object lookups via `FindObjectOfType`. No testability, no separation of concerns, no dependency injection.

**Solution:** Split into 8 focused classes with MVVM/Adapter pattern:
- **Domain** (pure C#): `FileTabViewModel` + interfaces (`IFitsService`, `IFileDialogService`, `IVolumeService`)
- **Adapters** (Unity): `FileTabView`, `FitsServiceAdapter`, `FileDialogServiceAdapter`, `VolumeServiceAdapter`, `MemoryProbeAdapter`, `FileTabCompositionRoot`

**Key Metrics (BEFORE → AFTER):**

| Metric | Before | After |
|--------|--------|-------|
| LOC | 1899 | ~1438 across 8 classes |
| WMC | 57 | 27 (FileTabViewModel) |
| CBO | ~32 | 9 (FileTabViewModel) |
| RFC | ~210 | ~50 (FileTabViewModel) |
| LCOM4 | ≥7 | 1 per class |

### Documentation

- [**Before sequence**](../../../../refactoring-examples/sub-team-6/file-tab/before-sequence.md) — Mermaid sequence diagram showing the tangled flow, direct DLL coupling, coroutine reentry, busy-wait loops
- [**After sequence**](../../../../refactoring-examples/sub-team-6/file-tab/after-sequence.md) — Mermaid sequence diagram showing clean MVVM separation with ACL boundary
- [**Class diagram**](../../../../refactoring-examples/sub-team-6/file-tab/class-diagram.md) — Mermaid before/after UML showing god-class decomposition
- [**Dependency graph**](../../../../refactoring-examples/sub-team-6/file-tab/dependency-graph.md) — Module-level DSM proving Section 4.2 compliance (Domain ↛ UnityEngine)
- [**CK metrics**](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md) — Hand-counted deltas (WMC, DIT, NOC, CBO, RFC, LCOM) with thresholds
- [**FileTabViewModel.cs**](../../../../refactoring-examples/sub-team-6/file-tab/skeleton/FileTabViewModel.cs) — ~480-line domain class, fully testable
- [**FileTabViewModelTests.cs**](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) — 34 NUnit tests, zero Unity dependency

### Smells Addressed

| Smell | Before | After |
|-------|--------|-------|
| Direct native-plugin calls from UI | `CD → FR → DLL` arrows | One `IFitsService` interface |
| No interface layer | 8 direct arrows | 8 adapters implementing interfaces |
| Busy-wait coroutine loop | `while (!started) yield WaitForSeconds` | Event-driven `yield StartCoroutine(_startFunc())` |
| Direct field writes | `VDSR.subsetBounds = ...` (no encapsulation) | Contained in adapter, eliminated at VM level |
| Untestable without Unity | `CanvassDesktop` cannot be instantiated outside scene | 34 tests run with `dotnet test`, zero UnityEngine |

### Compliance

- **Section 4.2 (Domain Independence):** ✅ Domain assembly has zero references to UnityEngine
- **Unit testability (NFR-TST-1):** 0 → 34 NUnit tests (~100 ms total)
- **Decomposition:** 1 class → 8 focused classes, single responsibility each

---

## Example 2 — Debug Tab

**Objective:** Refactoring the debug console from inline logging to an Observer of a structured logging stream.

**Problem:** `DebugLogging` (255 lines) subscribes to the process-global static event `Application.logMessageReceived` with zero abstraction, no source/timestamp capture, unbounded non-generic Queue, per-message file I/O, O(N) full-queue rebuild on every log line, and wholesale TMP text replacement. **Untestable without Unity.** All 40+ `Debug.Log*` call sites are scattered with no central coordination.

**Solution:** Introduce Observer pattern with `ILogStream` / `ILogObserver` interfaces:
- **Domain** (pure C#): `LogStream` (thread-safe dispatcher), `DebugTabViewModel` (Observer), `LogEntry` record (DTO)
- **Adapters** (Unity): `UnityLogStreamAdapter` (subscribes to static event, publishes to `ILogStream`), `DebugTabView` (thin view), `DebugTabCompositionRoot` (wiring)

**Key Metrics (BEFORE → AFTER):**

| Metric | Before | After |
|--------|--------|-------|
| LOC | 255 | 382 total across 7 types |
| WMC | 8 | 6 (DebugTabViewModel) / 8 (UnityLogStreamAdapter) |
| CBO | ~10 | 3–7 per class (VM: 3, View: 7) |
| LCOM4 | ~3 (disjoint) | 1 per class (cohesive) |
| Test surface | 0 | 29 NUnit tests |

### Documentation

- [**Before trace**](../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md) — Code-anchored walkthrough of `DebugLogging.cs` with 9 smells identified (S1–S9)
- [**After sequence**](../../../../refactoring-examples/sub-team-6/debug-tab/after-sequence.md) — Mermaid sequence diagram showing non-invasive refactor (44 `Debug.Log*` callers unchanged)
- [**Class diagram**](../../../../refactoring-examples/sub-team-6/debug-tab/class-diagram.md) — Mermaid before/after UML with Observer pattern
- [**Dependency graph**](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md) — Module-level DSM proving Section 4.2 compliance + producer-side non-invasiveness
- [**CK metrics**](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) — Hand-counted deltas with testability analysis
- [**DebugTabViewModel.cs**](../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabViewModel.cs) — ~77-line domain class
- [**DebugTabTests.cs**](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) — 29 NUnit tests, zero Unity dependency

### Smells Addressed

| Smell | Before | After |
|-------|--------|-------|
| Static untestable event hook | `Application.logMessageReceived` with no interface | `ILogStream` interface, single implementation in adapter |
| Unstructured log tuple | `(string, string, LogType)` — no source/timestamp | `LogEntry(Level, Message, Timestamp)` record |
| Unbounded memory growth | Non-generic `Queue`, no cap | Generic `List<LogEntry>` capped at 2000 |
| Per-message file I/O | `StreamWriter` new/write/close per log | Removed from hot path, optional autosave as separate `ILogObserver` |
| O(N) full rebuild | Entire queue rebuilt every message | Capped at 500-line display slice |
| Forced scroll | Scroll to bottom on every message | Contained (remains S7, marked for future fix) |
| Four responsibilities | Capture, store, display, export in one class | Five types, single responsibility each |
| Producer dependency | 40+ `Debug.Log*` callers hardcoded | Unchanged (non-invasive via `UnityLogStreamAdapter`) |

### Compliance

- **Section 4.2 (Domain Independence):** ✅ Domain assembly (`DebugTabSkeleton.csproj`) builds with zero UnityEngine references
- **Non-invasiveness:** ✅ All 44 existing `Debug.Log*` call sites remain unchanged; captured automatically
- **Unit testability (NFR-TST-1):** 0 → 29 NUnit tests (~20 ms total)
- **Observer pattern:** ✅ New observers (autosave, telemetry, filtering) can attach without modifying producers

---

## Metrics Summary

CK metric deltas for both examples side-by-side: [**metrics.md**](metrics.md)

### Key Findings

1. **Testability Driven:** Both refactors are primarily testability plays, not metric-driven. BEFORE already passes 5/6 CK thresholds individually; the case is structural.
2. **Domain Extraction:** Both achieve clean domain/adapter split with ACL boundaries enforced by assembly-level dependency rules.
3. **Zero Unit Tests → Full Coverage:** Example 1 goes 0 → 34 tests; Example 2 goes 0 → 29 tests. Both run without Unity.
4. **Production Unchanged:** Example 2's producer side (all `Debug.Log*` call sites) remains untouched — non-invasive refactor.

---

## Implementation Status

| Item | Status |
|------|--------|
| BEFORE diagrams (Mermaid) — class diagrams and sequence/trace docs | ✅ |
| AFTER diagrams (Mermaid) — class diagrams and sequence diagrams | ✅ |
| Dependency graphs — text-based DSM with Section 4.2 compliance | ✅ |
| CK metrics — hand-counted (pending Day 13 tool verification) | ✅ |
| Skeleton code — `FileTabViewModel`, `DebugTabViewModel`, adapters, composition roots | ✅ |
| Unit tests — 34 file-tab tests, 29 debug-tab tests | ✅ |
| Tool verification — NDepend, Understand, DV8, CodeScene (Day 13) | ⏳ |
