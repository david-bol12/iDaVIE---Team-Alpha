# Sub-team 6 — Test Strategy

**Owner:** Sub-team 6 (Die Boks / Team Alpha)
**Spec refs:** §6.6 ST · §9.2.4 · §4.2 · §7.1–7.2 · LO6
**Last revised:** 2026-05-26 (Day 7)
**Status:** Complete. Detail specs live under `deliverables/D5-testing/`.

---

## 1. Purpose and Scope

This document defines the testability strategy for the **Desktop GUI and Client Shell** work package — the MVVM split of `CanvassDesktop.cs` into a Unity-free ViewModel layer, a UI Toolkit View layer, and a Service Gateway client. It:

- demonstrates that the after-state design produces classes that are independently testable (LO6, §4.2.4, NFR-TST-1/2);
- evidences the improvement over the before-state using mocking-difficulty counts (BNCH-6) and CK metric deltas;
- specifies concrete test shapes for the two worked examples (File tab, Debug tab);
- maps to the assignment's four coverage / testability metrics families (§7.2).

**In scope:** `ViewModel/`, `ServiceGateway/` client, View integration via page-objects, manual smoke flows.
**Out of scope:** server-side code (Sub-teams 1–4); render / stats / sources tabs (no AFTER skeleton in D4); pixel-level visual testing.

---

## 2. Layered Test Architecture

The four-tier pyramid below mirrors the architectural layer boundaries. Each tier is owned by a distinct test project so that Unity cannot bleed upward and the domain coverage gate is enforced independently.

| Tier | Layer under test | Framework | Unity required? | Coverage gate |
|---|---|---|---|---|
| 1 — ViewModel unit | `ViewModel/` (pure C#) | NUnit 3 + Moq 4 | No — standalone `.NET` project | ≥ 70 % branch + line |
| 2 — Gateway unit | `ServiceGateway/` client stub | NUnit 3 + Moq 4 | No | ≥ 70 % branch + line |
| 3 — View integration | `View/` panels via page-objects | Unity Test Framework (Play Mode) | Yes | Tracked, **not** gated |
| 4 — Smoke | Full desktop shell, real file, VR scene | Manual checklist | Yes | Pass/fail checklist |

**Why this split?** The before-state `CanvassDesktop` scores 205 on the mocking-difficulty index (BNCH-6: 163 scene-graph traversals + 36 static P/Invoke calls) — it cannot be unit-tested without a live Unity Editor session. The MVVM split drives that score to **zero** in the ViewModel layer by construction: the standalone `.NET` project does not reference `UnityEngine`, so any leakage is a build error, not a runtime surprise.

---

## 3. Tier 1 — ViewModel Unit Tests

**Detailed spec:** [deliverables/D5-testing/viewmodel-unit-tests.md](deliverables/D5-testing/viewmodel-unit-tests.md)

### 3.1 Project structure

```
DesktopClient.Tests/
  DesktopClient.Tests.csproj   ← references only ViewModel + NUnit + Moq + Coverlet
  FileTabViewModelTests.cs
  DebugTabViewModelTests.cs
  ServiceGatewayClientTests.cs
```

The `.csproj` has `<WarningsAsErrors>true</WarningsAsErrors>` and a `<Forbidden>UnityEngine</Forbidden>` NDepend / custom analyser rule. Any `using UnityEngine` breaks the build.

### 3.2 Test shapes — File tab ViewModel

Tests follow **MethodUnderTest\_Scenario\_ExpectedBehaviour** naming and Arrange / Act / Assert structure.

```csharp
[Category("ViewModel")]
public class FileTabViewModelTests
{
    [Test]
    public void OpenCube_HappyPath_CallsGatewayWithExpectedArgs()
    {
        var gateway = new Mock<IServiceGateway>();
        gateway.Setup(g => g.LoadCubeAsync("/data/cube.fits", CancellationToken.None))
               .ReturnsAsync(new CubeMetadata { AxisCount = 3 });

        var vm = new FileTabViewModel(gateway.Object);
        await vm.OpenCubeCommand.ExecuteAsync("/data/cube.fits");

        gateway.Verify(g => g.LoadCubeAsync("/data/cube.fits", CancellationToken.None), Times.Once);
        Assert.That(vm.IsLoading, Is.False);
        Assert.That(vm.ErrorMessage, Is.Null);
    }

    [Test]
    public void OpenCube_GatewayThrows_SetsErrorMessageAndClearsIsLoading()
    {
        gateway.Setup(g => g.LoadCubeAsync(...))
               .ThrowsAsync(new GatewayException("timeout"));

        await vm.OpenCubeCommand.ExecuteAsync("/bad/path.fits");

        Assert.That(vm.ErrorMessage, Is.EqualTo("timeout"));
        Assert.That(vm.IsLoading, Is.False);
    }

    [Test]
    public void OpenCube_WhileLoading_CommandIsIgnoredAndNoDoubleCall() { ... }

    [Test]
    public void BrowseMask_AxesMismatch_SetsValidationMessage() { ... }

    [Test]
    public void IsLoadable_FalseUntilValidCubeSelected() { ... }
}
```

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

### 3.4 Minimum required test count

| Class | Required tests | Rationale |
|---|---|---|
| `FileTabViewModel` | ≥ 5 | Happy path, error path, double-click guard, mask mismatch, `IsLoadable` gate |
| `DebugTabViewModel` | ≥ 3 | Append, order, clear |
| `ServiceGatewayClient` (stub) | ≥ 3 | Serialise request, deserialise response, timeout |

---

## 4. Tier 2 — Gateway Unit Tests

The `ServiceGateway` client serialises `IServiceGateway` calls to JSON-RPC over a named pipe. Tests mock the transport (`ITransport`) and assert that:

- a `LoadCubeAsync("/data/cube.fits")` call serialises to the correct JSON-RPC method and params;
- a successful JSON-RPC response deserialises into the expected `CubeMetadata` DTO;
- a transport timeout maps to a `GatewayException` with a user-readable message.

No running server is needed. The transport mock returns canned byte sequences. These tests live in the same `DesktopClient.Tests/` project, under `[Category("Gateway")]`.

---

## 5. Tier 3 — View Integration Tests (Page-Object Pattern)

**Detailed spec:** [deliverables/D5-testing/ui-toolkit.md](deliverables/D5-testing/ui-toolkit.md)

### 5.1 Pattern

One **Page Object** class per UXML panel (`FileTabPage`, `DebugTabPage`). Each page object:

- is constructed from a `VisualElement` root — the test owns the `UIDocument` lifecycle;
- exposes intent-only methods and properties (`PickImage(string)`, `ClickLoad()`, `ValidationText`, `IsLoadButtonEnabled`);
- encapsulates all `root.Q<T>("name")` selector strings — no selector leaks into test bodies;
- has ≤ 7 public members (ISP target; BNCH-7 audit).

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

SM-1 through SM-7 map directly to the five rows in the [requirements behaviour catalogue](requirements.md §2) and the two user-observable outcomes in the worked examples (File tab and Debug tab).

---

## 7. Coverage Targets

| Assembly / namespace | Branch target | Line target | Gated in CI? | Measured by |
|---|---|---|---|---|
| `iDaVIE.Client.ViewModel` | **≥ 70 %** | **≥ 70 %** | Yes — build fails below threshold | Coverlet + ReportGenerator |
| `iDaVIE.Client.ServiceGateway` (client) | **≥ 70 %** | **≥ 70 %** | Yes | Coverlet + ReportGenerator |
| `iDaVIE.Client.View` (UI Toolkit) | tracked | tracked | No | Unity Test Framework + Coverlet |
| **Overall (all three)** | **≥ 50 %** | **≥ 50 %** | Yes | SonarQube aggregate |

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
| **Moq 4** | ≥ 4.20 | Interface mocking (`IServiceGateway`, `ILogStream`, `IFitsService`, `IDialogService`) | Sub-team 6 |
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
2. **All external services behind interfaces.** `IServiceGateway`, `ILogStream`, `IFitsService`, `IDialogService`, `IPanel`. Moq mocks these in every tier 1/2 test. No concrete service class is imported into `ViewModel/`.
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
- **Unity play-mode tests:** run manually before each sprint review (not in automated CI — Unity Editor license not available on the GitHub Actions runner). Results are screen-captured and committed to `deliverables/D5-testing/`.

---

## 11. Testability-Improvement Evidence

The gap between before and after is quantified in two places:

| Artefact | What it shows |
|---|---|
| [BNCH-6 — Mocking-difficulty count](deliverables/other/T2-baseline-benchmark/BNCH-3.md) | Before: 205 call sites in `CanvassDesktop` that require a live Unity scene or native DLL to test. After: 0 in the ViewModel layer. |
| [BNCH-7 — Interface-size audit](deliverables/BNCH-6.md) | Every interface produced by the MVVM split has ≤ 7 public members (ISP target). Each has ≥ 1 test double in the tier 1 test suite. |
| CK metric projection ([metrics.md](deliverables/D4-worked-examples/metrics.md)) | WMC, RFC, LCOM, CBO deltas for `FileTabViewModel` and `DebugTabViewModel` vs the monolithic `CanvassDesktop`. |

---

## 12. Traceability

| Item | Reference |
|---|---|
| MVVM split decision | [ADR-0001](adrs/0001-mvvm-split.md) |
| File-tab worked example | [deliverables/D4-worked-examples/ex1-file-tab/](deliverables/D4-worked-examples/ex1-file-tab/) |
| Debug-tab worked example | [deliverables/D4-worked-examples/ex2-debug-tab/](deliverables/D4-worked-examples/ex2-debug-tab/) |
| ViewModel unit-test detail spec | [deliverables/D5-testing/viewmodel-unit-tests.md](deliverables/D5-testing/viewmodel-unit-tests.md) |
| UI Toolkit page-object detail spec | [deliverables/D5-testing/ui-toolkit.md](deliverables/D5-testing/ui-toolkit.md) |
| Mocking-difficulty baseline | [deliverables/BNCH-6.md](deliverables/BNCH-6.md) |
| CK metric baseline + projection | [deliverables/D4-worked-examples/metrics.md](deliverables/D4-worked-examples/metrics.md) |
| Testability NFRs | [requirements.md §3 — NFR-TST-1/2/3](requirements.md) |
| Assignment spec | §6.6 ST · §9.2.4 · §4.2 #4 · §7.1–7.2 · LO6 |
