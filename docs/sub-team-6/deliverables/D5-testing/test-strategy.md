# Sub-team 6 — Test Strategy

**Owner:** Sub-team 6 (Die Boks / Team Alpha)
**Spec refs:** §6.6 ST · §9.2.4 · §4.2 · §7.1–7.2 · LO6
**Last revised:** 2026-05-26 (Day 7)
**Status:** Complete. Tier-1 and tier-3 detail specs live alongside this doc — [`viewmodel-unit-tests.md`](viewmodel-unit-tests.md) and [`ui-toolkit.md`](ui-toolkit.md).

---

## 1. Purpose and Scope

This document defines the testability strategy for the **Desktop GUI and Client Shell** work package — the MVVM split of `CanvassDesktop.cs` into a Unity-free ViewModel layer and a UI Toolkit View layer, with the four split service interfaces that today stand in for a future Service Gateway. It:

- demonstrates that the after-state design produces classes that are independently testable (LO6, §4.2.4, NFR-TST-1/2);
- evidences the improvement over the before-state using mocking-difficulty counts (BNCH-6) and CK metric deltas;
- specifies concrete test shapes for the two worked examples (File tab, Debug tab);
- maps to the assignment's four coverage / testability metrics families (§7.2).

**In scope:** `ViewModel/` (pure C#), the four split service interfaces consumed by the ViewModels, View integration via page-objects, manual smoke flows.
**Out of scope:** server-side code (Sub-teams 1–4); render / stats / sources tabs (no AFTER skeleton in D4); pixel-level visual testing.

---

## 2. Layered Test Architecture

The four-tier pyramid below mirrors the architectural layer boundaries. Each tier is owned by a distinct test project so that Unity cannot bleed upward and the domain coverage gate is enforced independently.

| Tier | Layer under test | Framework | Unity required? | Coverage gate |
|---|---|---|---|---|
| 1 — ViewModel unit | `ViewModel/` (pure C#) | NUnit 3 + Moq 4 | No — standalone `.NET` project | ≥ 70 % branch + line |
| 2 — Gateway unit | JSON-RPC client + transport | NUnit 3 + Moq 4 | No | Owned by Sub-team 1 — out of scope here (see §4) |
| 3 — View integration | `View/` panels via page-objects | Unity Test Framework (Play Mode) | Yes | Tracked, **not** gated |
| 4 — Smoke | Full desktop shell, real file, VR scene | Manual checklist | Yes | Pass/fail checklist |

**Why this split?** The before-state `CanvassDesktop` scores 205 on the mocking-difficulty index (BNCH-6: 163 scene-graph traversals + 36 static P/Invoke calls) — it cannot be unit-tested without a live Unity Editor session. The MVVM split drives that score to **zero** in the ViewModel layer by construction: the standalone `.NET` project does not reference `UnityEngine`, so any leakage is a build error, not a runtime surprise.

---

## 3. Tier 1 — ViewModel Unit Tests

**Detailed spec:** [viewmodel-unit-tests.md](viewmodel-unit-tests.md)

### 3.1 Project structure

```
refactoring-examples/sub-team-6/
  file-tab/
    skeleton/FileTabSkeleton.csproj   ← ViewModel + NUnit + Moq + Coverlet; no UnityEngine reference
    tests/FileTabViewModelTests.cs    ← 34 NUnit tests, zero Unity dependency
  debug-tab/
    skeleton/DebugTabSkeleton.csproj
    tests/DebugTabTests.cs            ← 29 NUnit tests, ~20 ms total
```

`dotnet build` on either skeleton csproj completes with **0 warnings 0 errors and zero `UnityEngine` references** — the Section 4.2 #3 invariant is enforced by the project file, not by convention. See [`refactoring-examples/sub-team-6/debug-tab/dependency-graph.md`](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md).

### 3.2 Test shapes — File tab ViewModel

`FileTabViewModel` is constructed with four split service interfaces (no consolidated `IServiceGateway` façade is shipped — that consolidation is left as a future Sub-team 1 concern; see §4). Tests follow **MethodUnderTest\_Scenario\_ExpectedBehaviour** naming and Arrange / Act / Assert structure.

```csharp
[Category("ViewModel")]
public class FileTabViewModelTests
{
    [Test]
    public async Task BrowseImageAsync_HappyPath_PopulatesHeaderAndEnablesLoad()
    {
        var fits   = new Mock<IFitsService>();
        var dialog = new Mock<IFileDialogService>();
        var volume = new Mock<IVolumeService>();
        var memory = new Mock<IMemoryProbe>();

        dialog.Setup(d => d.PickFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
              .ReturnsAsync("/data/cube.fits");
        fits.Setup(f => f.OpenImageAsync("/data/cube.fits"))
            .ReturnsAsync(new FitsFileInfo { /* 3-axis cube */ });

        var vm = new FileTabViewModel(fits.Object, dialog.Object, volume.Object, memory.Object);
        await vm.BrowseImageCommand.ExecuteAsync(null);

        Assert.That(vm.ImagePath,  Is.EqualTo("/data/cube.fits"));
        Assert.That(vm.IsLoadable, Is.True);
        Assert.That(vm.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task BrowseImageAsync_ServiceThrows_SurfacesValidationMessage()
    {
        fits.Setup(f => f.OpenImageAsync(It.IsAny<string>()))
            .ThrowsAsync(new FitsServiceException("invalid header"));

        await vm.BrowseImageCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorMessage, Is.EqualTo("invalid header"));
        Assert.That(vm.IsLoadable,   Is.False);
    }
}
```

The committed suite at `refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs` carries 34 such tests covering happy paths, error paths, axis-mismatch validation, RAM-feasibility (`IMemoryProbe`), the `CubeLoaded` event on `IVolumeService`, and the `IsLoadable` gate.

### 3.3 Test shapes — Debug tab ViewModel (Observer pattern)

The Debug tab ViewModel subscribes to `ILogStream.OnLogEntry`. Tests fire events via Moq's `Raise` — no Unity, no static logger, no thread.

```csharp
[Category("ViewModel")]
public class DebugTabViewModelTests
{
    [Test]
    public void OnLogEntry_SingleWarning_AppendsToEntries()
    {
        var logStream = new Mock<ILogStream>();
        var vm = new DebugTabViewModel(logStream.Object);

        logStream.Raise(l => l.OnLogEntry += null,
            new LogEntry { Level = LogLevel.Warning, Message = "VR init slow" });

        Assert.That(vm.Entries, Has.Count.EqualTo(1));
        Assert.That(vm.Entries[0].Message, Is.EqualTo("VR init slow"));
    }

    [Test]
    public void ClearEntries_EmptiesCollection() { ... }

    [Test]
    public void MultipleEntries_PreserveArrivalOrder() { ... }
}
```

### 3.4 Delivered test count (Day 7 snapshot)

| Class | Required | **Delivered** | Evidence |
|---|---:|---:|---|
| `FileTabViewModel` | ≥ 5 | **34** | [`file-tab/tests/FileTabViewModelTests.cs`](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) |
| `DebugTabViewModel` + `LogStream` | ≥ 3 | **29** | [`debug-tab/tests/DebugTabTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) |
| **Total** | — | **63** | `dotnet test` runtime ~20 ms (debug-tab measured) — zero Unity dependency |

---

## 4. Tier 2 — Gateway Unit Tests (out of scope)

A consolidated `IServiceGateway` façade is **not** implemented in either worked example — File tab depends on four split service interfaces, Debug tab depends on `ILogStream`. The JSON-RPC client and named-pipe transport are owned by **Sub-team 1** ([architecture.md](../D2-Architecture/architecture.md) — Service Gateway section); their unit tests live with their code. If a gateway façade is later introduced over the four interfaces, the tier-2 slot here is reserved for those tests with the same NUnit + mock-transport pattern.

---

## 5. Tier 3 — View Integration Tests (Page-Object Pattern)

**Detailed spec:** [ui-toolkit.md](ui-toolkit.md)

### 5.1 Pattern

One **Page Object** class per UXML panel (`FileTabPage`, `DebugTabPage`). Each page object:

- is constructed from a `VisualElement` root — the test owns the `UIDocument` lifecycle;
- exposes intent-only methods and properties (`PickImage(string)`, `ClickLoad()`, `ValidationText`, `IsLoadButtonEnabled`);
- encapsulates all `root.Q<T>("name")` selector strings — no selector leaks into test bodies;
- has ≤ 7 public members (ISP target; interface-size audit lives in each worked example's `ck-metrics.md`).

Tests run inside **Unity Test Framework Play Mode**. The composition root is bypassed: each test constructs a real ViewModel from mocked services, attaches it to a fresh `UIDocument`, and drives via the page object. Every event dispatch is followed by `yield return null` to flush UI Toolkit's binding pass before asserting.

### 5.2 Required integration tests

**File tab (`FileTabPage`):**

| Test | Assertion |
|---|---|
| `BrowseImage_HappyPath_ShowsPathAndHeader` | `ImagePathText` and header visible after dialog mock resolves |
| `BrowseImage_ServiceThrows_ShowsValidationMessage_LoadButtonDisabled` | Validation banner visible, Load button disabled |
| `BrowseMask_AxesMismatch_ShowsValidationMessage` | Mismatch error rendered in View |
| `LoadCommand_Disabled_UntilIsLoadable` | Load button reflects ViewModel's `IsLoadable` |
| `LoadCommand_WhileLoading_ButtonIsDisabled` | `IsLoading` → `CanExecute` chain reaches View |

**Debug tab (`DebugTabPage`):**

| Test | Assertion |
|---|---|
| `StreamEmitsEntry_AppearsInListView` | Single entry published → ListView renders it |
| `MultipleEntries_AppearInOrder` | Order preserved end-to-end |
| `Clear_EmptiesListView` | `ClearEntries()` empties the bound list |
| `EntriesChanged_TriggersRebind` | Observer wiring confirmed on View side |

---

## 6. User Flows — Smoke Test Checklist

Smoke tests are manual at this stage. They exercise the full desktop shell (real VR scene, real file, real pipe) and run on a reference build before any pitch demo. Each item is a pass/fail check; no coverage percentage is associated.

| ID | Flow | Entry condition | Pass criterion |
|---|---|---|---|
| SM-1 | Load a valid FITS cube end-to-end | File tab open, no cube loaded | Cube renders in VR viewport; File tab shows axis metadata |
| SM-2 | Load an invalid file | File tab open | Validation message shown; app remains responsive |
| SM-3 | Load a mask with mismatched axes | Cube already loaded | Mask is rejected; error surfaced in File tab; cube unaffected |
| SM-4 | Debug tab receives live log entries | Debug tab visible during cube load | Entries appear in order; timestamps present |
| SM-5 | Debug tab clear | Debug tab with ≥ 3 entries | Entries cleared; list view empty |
| SM-6 | Switch desktop tabs | File tab active | All five tabs render without error; active tab state preserved |
| SM-7 | Close and reopen desktop panel | Panel visible | Panel reopens to previous tab; no null reference exceptions in console |

SM-1 through SM-7 map directly to the five rows in the [requirements behaviour catalogue](../D1-requirements/requirements.md) §2 and the two user-observable outcomes in the worked examples (File tab and Debug tab).

---

## 7. Coverage Targets

| Assembly / namespace | Branch target | Line target | Gated in CI? | Measured by |
|---|---|---|---|---|
| `iDaVIE.Client.ViewModel` | **≥ 70 %** | **≥ 70 %** | Yes — build fails below threshold | Coverlet + ReportGenerator |
| `iDaVIE.Client.View` (UI Toolkit) | tracked | tracked | No | Unity Test Framework + Coverlet |
| **Overall (client slice)** | **≥ 50 %** | **≥ 50 %** | Yes | SonarQube aggregate |

The View layer is tracked but not gated. Justification: UI Toolkit binding boilerplate and UXML configuration are configuration-heavy; a strict gate would inflate to noise or force testing of the framework itself. The ViewModel gate is the load-bearing metric for §7.2 and NFR-TST-1.

Run coverage locally:

```
dotnet test DesktopClient.Tests/ --collect:"XPlat Code Coverage"
reportgenerator -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html;Cobertura
```

---

## 8. Tooling

| Tool | Version | Role | Who owns |
|---|---|---|---|
| **NUnit 3** | ≥ 3.14 | Test runner + assertions for tiers 1 & 2 | Sub-team 6 |
| **Moq 4** | ≥ 4.20 | Interface mocking (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IFitsHandle`, `ILogStream`, `ILogObserver`) | Sub-team 6 |
| **.NET 7 SDK** (standalone) | match Unity 2021 Mono | Build + run tier 1 & 2 tests outside Unity | Sub-team 6 |
| **Coverlet** | latest | Branch + line coverage for standalone project | Sub-team 6 |
| **ReportGenerator** | latest | HTML + Cobertura report for CI | Sub-team 6 |
| **Unity Test Framework** | bundled with Unity 2021.3 | Hosts tier 3 Play-Mode tests | Sub-team 6 |
| **SonarQube Cloud** | SaaS | Aggregate coverage badge, cognitive-complexity gate | Quality Guild |
| **NDepend** | licensed | `UnityEngine` transitive-dependency rule, CBO/RFC/LCOM metrics | Quality Guild |
| **Understand (Scitools)** | licensed | WMC, DIT, NOC, LCOM baseline and projection | Quality Guild |

Sub-team 6 owns the `DesktopClient.Tests/` project and the Unity `IntegrationTests` assembly. The Quality Guild owns the CI/CD pipeline that gates on our coverage reports.

---

## 9. Dependency Isolation and Anti-Corruption Enforcement

Four hard rules, enforced structurally (not by convention):

1. **`ViewModel/` project does not reference `UnityEngine`.** The standalone `.csproj` has no Unity SDK reference; `using UnityEngine` is a compile error. This is the primary enforcement of §4.2.3 and NFR-REU-3.
2. **All external services behind interfaces.** File tab depends on `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IFitsHandle`; Debug tab depends on `ILogStream` and `ILogObserver`. Every public boundary is an interface and every interface has ≥ 1 Moq test double in the committed suites — §4.2 #4 satisfied.
3. **No `FindObjectOfType` or `MonoBehaviour` in ViewModel.** These types do not exist in the standalone project; no enforcement rule needed.
4. **Static clock injected.** Any time-dependent logic uses `ISystemClock` so tests are deterministic.

The mocking-difficulty count (BNCH-6) provides the before/after evidence:

| Metric | Before (`CanvassDesktop`) | After (ViewModel layer) | Delta |
|---|---|---|---|
| Static / Unity call sites | 205 | **0** | −205 |
| `FindObjectOfType` calls | 6 | **0** | −6 |
| P/Invoke / `StandaloneFileBrowser` | 36 | **0** (moved to View/Adapter) | −36 |

This delta is the testability improvement the MVVM split is designed to produce. It directly satisfies NFR-TST-2 ("static / Unity API call count per ViewModel class = 0").

---

## 10. CI Integration

The Quality Guild owns the team-wide CI/CD pipeline (`T6`). Sub-team 6 plugs into it as follows:

- **PR gate:** `dotnet test DesktopClient.Tests/` must pass; coverage must meet the §7 thresholds. The gate runs on every PR to `main`.
- **Coverage upload:** Coverlet Cobertura XML is uploaded to SonarQube Cloud for the aggregate badge.
- **NDepend rule:** a `no-unityrefs-in-viewmodel` CQLinq rule asserts that no type in `iDaVIE.Client.ViewModel` has a transitive dependency on `UnityEngine.*`. This runs on `main` only (NDepend licence is shared).
- **Unity play-mode tests:** run manually before each sprint review (not in automated CI — Unity Editor license not available on the GitHub Actions runner). Results are screen-captured and committed alongside this doc.

---

## 11. Testability-Improvement Evidence

Every row below points at an artefact a panel reviewer can open in this repo.

| Artefact | What it shows |
|---|---|
| [BNCH-6 — Mocking-difficulty count](../BNCH-6.md) | Before: 205 call sites in `CanvassDesktop` that require a live Unity scene or native DLL to test. After: 0 in the ViewModel layer (205 → 0 static/Unity; 6 → 0 `FindObjectOfType`; 36 → 0 P/Invoke / `StandaloneFileBrowser`). |
| [`FileTabViewModelTests.cs`](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) | **34 NUnit tests** on `FileTabViewModel` — zero `using UnityEngine`, mocks four split service interfaces. |
| [`DebugTabTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) | **29 NUnit tests** on `DebugTabViewModel` + `LogStream` — zero `using UnityEngine`, `dotnet test` runtime **~20 ms**. |
| [`debug-tab/dependency-graph.md`](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md) | `dotnet build` on `DebugTabSkeleton.csproj` completes with **0 warnings, 0 errors, zero `UnityEngine` references** — §4.2 #3 enforced structurally. |
| CK metric projection ([metrics.md](../D4-worked-examples/metrics.md), [file-tab/ck-metrics.md](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md), [debug-tab/ck-metrics.md](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md)) | WMC, RFC, LCOM, CBO deltas for `FileTabViewModel` and `DebugTabViewModel` vs the monolithic `CanvassDesktop`. |
| Interface-size audit (in [`debug-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) and [`file-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md)) | Every interface produced by the MVVM split has ≤ 7 public members (ISP target). Each has ≥ 1 Moq test double in the committed tier-1 suites. |

---

## 12. Traceability

| Item | Reference |
|---|---|
| MVVM split decision | [D2 Architecture](../D2-Architecture/architecture.md) (ADR rationale lives inside the architecture doc — no separate ADR files) |
| File-tab worked example | [`refactoring-examples/sub-team-6/file-tab/`](../../../../refactoring-examples/sub-team-6/file-tab/) |
| Debug-tab worked example | [`refactoring-examples/sub-team-6/debug-tab/`](../../../../refactoring-examples/sub-team-6/debug-tab/) |
| ViewModel unit-test detail spec | [`viewmodel-unit-tests.md`](viewmodel-unit-tests.md) |
| UI Toolkit page-object detail spec | [`ui-toolkit.md`](ui-toolkit.md) |
| Mocking-difficulty baseline | [`BNCH-6.md`](../BNCH-6.md) |
| CK metric baseline + projection | [`D4-worked-examples/metrics.md`](../D4-worked-examples/metrics.md) |
| Testability NFRs | [`D1-requirements/requirements.md` §3 — NFR-TST-1/2/3](../D1-requirements/requirements.md) |
| Assignment spec | §6.6 ST · §9.2.4 · §4.2 #4 · §7.1–7.2 · LO6 |
