# Sub-team 6 — Test Strategy

**Owner:** Sub-team 6 (Die Boks / Team Alpha)
**Spec refs:** §6.6 ST · §9.2.4 · §4.2 · §7.1–7.2 · LO6
**Last revised:** 2026-05-27 (Day 8 — Tier 2 brought in scope after the gateway rewire closed audit F9 / F10)
**Status:** Complete. Tier-1 and tier-3 detail specs live alongside this doc — [`viewmodel-unit-tests.md`](viewmodel-unit-tests.md) and [`ui-toolkit.md`](ui-toolkit.md). Tier-2 spec is §4 of this document.

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
| 2 — Gateway and adapter | Wire framing, `IServiceGateway` contract, gateway-proxy adapters | NUnit 3 + `FakeGateway` | No — standalone `.NET` project | Tracked; see §4 |
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
    tests/FileTabViewModelTests.cs    ← 47 NUnit tests, zero Unity dependency
  debug-tab/
    skeleton/DebugTabSkeleton.csproj
    tests/DebugTabTests.cs            ← 29 NUnit tests, ~20 ms total
```

`dotnet build` on either skeleton csproj completes with **0 warnings 0 errors and zero `UnityEngine` references** — the Section 4.2 #3 invariant is enforced by the project file, not by convention. See [`refactoring-examples/sub-team-6/debug-tab/dependency-graph.md`](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md).

### 3.2 Test shapes — File tab ViewModel

`FileTabViewModel` is constructed with four split service interfaces (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`). The ViewModel does **not** hold an `IServiceGateway` reference directly — the gateway lives behind `IFitsService`, where `FitsServiceAdapter` translates the calls into JSON-RPC dispatches. That keeps tier-1 tests focused on ViewModel logic; the wire-shape assertions live one tier down (§4). Tests follow **MethodUnderTest\_Scenario\_ExpectedBehaviour** naming and Arrange / Act / Assert structure.

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

The Debug tab ViewModel implements `ILogObserver` and subscribes to `ILogStream` via `Subscribe(this)` (the Observer pattern, matching D2 §6 and the committed skeleton). Tests publish through a concrete `LogStream` — no Unity, no static logger, no thread.

```csharp
[Category("ViewModel")]
public class DebugTabViewModelTests
{
    [Test]
    public void Publish_SingleWarning_AppendsToLogEntries()
    {
        var logStream = new LogStream();            // concrete ILogStream — pure C#, no Unity
        var vm = new DebugTabViewModel(logStream);  // ctor calls logStream.Subscribe(this)

        logStream.Publish(LogLevel.Warning, "VR init slow");

        Assert.That(vm.LogEntries, Has.Count.EqualTo(1));
        Assert.That(vm.LogEntries[0].Message, Is.EqualTo("VR init slow"));
    }

    [Test]
    public void MultipleEntries_PreserveArrivalOrder() { ... }

    [Test]
    public void LogEntries_ExposedAsReadOnlyList() { ... }
}
```

### 3.4 Delivered test count (Day 8 snapshot, post-F14 follow-up)

| Class | Required | **Delivered** | Evidence |
|---|---:|---:|---|
| `FileTabViewModel` + `SubsetBoundsViewModel` | ≥ 5 | **47** (34 happy/error path + 13 branch-coverage gate-close) | [`file-tab/tests/FileTabViewModelTests.cs`](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) |
| `DebugTabViewModel` + `LogStream` | ≥ 3 | **29** | [`debug-tab/tests/DebugTabTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) |
| **Tier 1 total** | — | **76** | `dotnet test` runtime ~37 + ~17 ms — zero Unity dependency |

Tier-2 counts (19 tests across framing, gateway double, and the two gateway-proxy adapters) are in §4.4. **Combined Tier 1 + Tier 2: 95 / 95 green, ~200 ms total.**

---

## 4. Tier 2 — Gateway and Adapter Tests

The gateway is the load-bearing seam that the audit's F9 / F10 findings flagged as having no real consumer. Tier 2 closes that gap. Three test projects sit at this tier, all `dotnet test`-able, zero Unity dependency.

### 4.1 Project layout

```
refactoring-examples/sub-team-6/
  contracts/
    GatewayContracts.csproj             ← IServiceGateway, JsonRpcPipeGateway, LengthPrefixFraming, FakeGateway
    tests/
      GatewayContractsTests.csproj      ← LengthPrefixFramingTests + FakeGatewayTests (11 tests)
  file-tab/adapters/
    FileTabAdapters.csproj              ← FitsServiceAdapter (gateway proxy, no Unity)
    tests/
      FileTabAdaptersTests.csproj       ← FitsServiceAdapterTests (4 wire-shape tests)
  debug-tab/adapters/
    DebugTabAdapters.csproj             ← GatewayLogStreamAdapter (gateway proxy, no Unity)
    tests/
      DebugTabAdaptersTests.csproj      ← GatewayLogStreamAdapterTests (4 notification tests)
```

### 4.2 What each project pins

| Project | Pins | Anchored against |
|---|---|---|
| `GatewayContractsTests` | **Framing** (`LengthPrefixFramingTests`): round-trip preserves payload bytes; empty-payload frame is `0\n`; leading-zero in header rejected; CR in header rejected; non-digit rejected; stream-closed-mid-payload surfaces `EndOfStreamException`. **Gateway double** (`FakeGatewayTests`): connect-before-send guard; unstubbed-method trap; `SetResponse` JSON-round-trip; `SetError` raises `JsonRpcException` with code + message; `EmitNotification` fires subscribers. | ADR-0002 §"Framing"; ADR-0002 §"Error model" |
| `FileTabAdaptersTests` | **`OpenImageAsync` two-call protocol** (`file.open` → `dataset.getAxes`) with params shape (`{ path, isMask }`, `{ datasetId }`) and metadata round-trip. **Failure cleanup**: `dataset.getAxes` error fires best-effort `file.close` so the server-allocated dataset id does not leak. **`GetHeaderTextAsync`** dispatches `dataset.getHeader` with `{ datasetId, hduIndex }`. **Handle disposal** fires `file.close`. | ADR-0002 §"Method catalogue (v1)" |
| `DebugTabAdaptersTests` | **`log.emit` happy path**: level + msg + ISO-8601 ts preserved end-to-end into a `LogEntry`. **Lenient level parsing**: unknown levels (`TRACE`) fall back to `Info` rather than dropping the entry. **Method filter**: `progress.update` and other notifications do not leak into `ILogStream`. **Lifecycle**: `Dispose()` detaches from the gateway. | ADR-0002 §"Message shape" |

### 4.3 What's still out of scope here

- **Real named-pipe end-to-end** between `JsonRpcPipeGateway` and a server-side handler. `JsonRpcPipeGateway` is implemented as a production-shaped skeleton (real `NamedPipeClientStream`, real read loop, real concurrent request dispatch); a Sub-team-1-owned server handler does not yet exist, so end-to-end tests are deferred to integration sprints. The framing tests do exercise the wire bytes against `MemoryStream`, which is the load-bearing correctness check available without a server.
- **Server-side `log.emit` production**, FITS plug-in compatibility, and JSON-RPC error-code semantics on the producing side are Sub-team 1 concerns; their tests live with their code.

### 4.4 Delivered test count (Day 8)

| Project | Required | Delivered | Runtime |
|---|---:|---:|---:|
| `GatewayContractsTests` | — | **11** | ~64 ms |
| `FileTabAdaptersTests` | — | **4** | ~44 ms |
| `DebugTabAdaptersTests` | — | **4** | ~29 ms |
| **Tier 2 total** | — | **19** | ~140 ms |

Combined with the 76 tier-1 tests this brings the no-Unity suite to **95 / 95 green** in under 200 ms total — a credible PR-time gate, and the gate currently passes on every gated assembly.

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

## 7. Coverage Targets and Measured Coverage

### 7.1 Targets

| Assembly / namespace | Branch target | Line target | Gated in CI? | Measured by |
|---|---|---|---|---|
| `iDaVIE.Client.ViewModel` (`FileTabSkeleton`, `DebugTabSkeleton`) | **≥ 70 %** | **≥ 70 %** | Yes — build fails below threshold | Coverlet + ReportGenerator |
| `iDaVIE.Client.Gateway` (Tier 2 surface) | tracked | tracked | No | Coverlet + ReportGenerator |
| `iDaVIE.Client.View` (UI Toolkit) | tracked | tracked | No | Unity Test Framework + Coverlet |
| **Overall (client slice)** | **≥ 50 %** | **≥ 50 %** | Yes | SonarQube aggregate |

The View layer and the Gateway transport (`JsonRpcPipeGateway`) are tracked but not gated. View justification: UI Toolkit binding boilerplate and UXML configuration are framework-heavy; a strict gate would force testing of the framework itself. Gateway-transport justification: `JsonRpcPipeGateway` requires a real named pipe with a Sub-team 1 server handler — see §4.3.

### 7.2 Measured coverage (audit F14 close, 2026-05-27 Day 8)

Full per-class numbers, the reproduction command, and the test-list that closed the branch gap live in [`coverage-report.md`](coverage-report.md). Headline:

| Assembly | Line | Branch | Gate status |
|---|---:|---:|---|
| **DebugTabSkeleton** | **100 %** | **100 %** | ✅ both met |
| **FileTabSkeleton** | **89.4 %** | **77.2 %** | ✅ both met (Day 8 follow-up added 13 targeted tests; branch was 67.4 % on Day 6) |
| **iDaVIE.Client.Gateway** | 41.2 % | 41.6 % | tracked (not gated) |
| **Aggregate** | **71.3 %** | **66.5 %** | — |

**Both gated assemblies now clear the ≥ 70 % gate.** The Day-6 measurement showed FileTabSkeleton at 67.4 % branch — below the gate by 2.6 pp. The Day-8 follow-up added 13 NUnit cases against the specific uncovered branches (constructor null-guards, no-op setters, empty-VM `Dispose`, setters-before-image-load, `IsLoadable` axis-count rule, BrowseMask cancel/replace paths, NAXIS1 and NAXIS2 mismatch branches in `MaskAxesMatchImage`), lifting the assembly to 89.4 / 77.2. **Wiring the gate into CI as the Quality Guild's Day-10 task per ADR-0005 will not block any current PR.**

### 7.3 Reproduce locally

```pwsh
foreach ($p in @(
    'file-tab/tests/FileTabTests',
    'file-tab/adapters/tests/FileTabAdaptersTests',
    'debug-tab/tests/DebugTabTests',
    'debug-tab/adapters/tests/DebugTabAdaptersTests',
    'contracts/tests/GatewayContractsTests')) {
    dotnet test "refactoring-examples/sub-team-6/$p.csproj" `
        --collect:'XPlat Code Coverage' `
        --results-directory $env:TEMP\cov\$($p -replace '/','_')
}
dotnet tool install -g dotnet-reportgenerator-globaltool   # one-off
reportgenerator -reports:"$env:TEMP\cov\**\coverage.cobertura.xml" `
                -targetdir:"$env:TEMP\cov-report" `
                -reporttypes:"Html;MarkdownSummary" `
                -classfilters:"-*Tests"
```

---

## 8. Tooling

| Tool | Version | Role | Who owns |
|---|---|---|---|
| **NUnit 3** | ≥ 3.14 | Test runner + assertions for tiers 1 & 2 | Sub-team 6 |
| **Moq 4** | ≥ 4.20 | Interface mocking (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IFitsHandle`, `ILogStream`, `ILogObserver`) | Sub-team 6 |
| **.NET 7 SDK** (standalone) | match Unity 2021 Mono | Build + run tier 1 & 2 tests outside Unity | Sub-team 6 |
| **Coverlet** (`coverlet.collector`) | 6.0.2 | Branch + line coverage via `dotnet test --collect:"XPlat Code Coverage"`; PackageReference on every test csproj | Sub-team 6 |
| **ReportGenerator** | 5.5.10 | Merges Cobertura XMLs across the 5 test projects; emits `Html` / `MarkdownSummary` for the panel and CI | Sub-team 6 |
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
| [BNCH-6 — Mocking-difficulty count](../other/T2-baseline-benchmark/BNCH-6.md) | Before: 205 call sites in `CanvassDesktop` that require a live Unity scene or native DLL to test. After: 0 in the ViewModel layer (205 → 0 static/Unity; 6 → 0 `FindObjectOfType`; 36 → 0 P/Invoke / `StandaloneFileBrowser`). |
| [`FileTabViewModelTests.cs`](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) | **47 NUnit tests** on `FileTabViewModel` — zero `using UnityEngine`, mocks four split service interfaces. |
| [`DebugTabTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) | **29 NUnit tests** on `DebugTabViewModel` + `LogStream` — zero `using UnityEngine`, `dotnet test` runtime **~17 ms**. |
| [`contracts/tests/LengthPrefixFramingTests.cs`](../../../../refactoring-examples/sub-team-6/contracts/tests/LengthPrefixFramingTests.cs) + [`FakeGatewayTests.cs`](../../../../refactoring-examples/sub-team-6/contracts/tests/FakeGatewayTests.cs) | **11 NUnit tests** pinning the wire framing (ADR-0002 §"Framing") and the gateway double's contract. Zero Unity dependency. |
| [`FitsServiceAdapterTests.cs`](../../../../refactoring-examples/sub-team-6/file-tab/adapters/tests/FitsServiceAdapterTests.cs) | **4 NUnit tests** asserting `FitsServiceAdapter` dispatches `file.open` → `dataset.getAxes` → `dataset.getHeader` → `file.close` through `IServiceGateway` with the params shape mandated by ADR-0002. **Closes audit F9** ("transport contract has no consumer"). |
| [`GatewayLogStreamAdapterTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/adapters/tests/GatewayLogStreamAdapterTests.cs) | **4 NUnit tests** asserting `log.emit` notifications materialise as `LogEntry` with level/msg/ts preserved; method-name filtering; lifecycle hygiene. **Closes audit F10** ("server-pushed stream has no consumer"). |
| [`debug-tab/dependency-graph.md`](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md) | `dotnet build` on `DebugTabSkeleton.csproj` completes with **0 warnings, 0 errors, zero `UnityEngine` references** — §4.2 #3 enforced structurally. The same holds for `FileTabSkeleton.csproj`, `GatewayContracts.csproj`, and the two `*Adapters.csproj` gateway-proxy projects. |
| CK metric projection ([metrics.md](../D4-worked-examples/metrics.md), [file-tab/ck-metrics.md](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md), [debug-tab/ck-metrics.md](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md)) | WMC, RFC, LCOM, CBO deltas for `FileTabViewModel` and `DebugTabViewModel` vs the monolithic `CanvassDesktop`. |
| Interface-size audit (in [`debug-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) and [`file-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md)) | Every interface produced by the MVVM split has ≤ 7 public members (ISP target). Each has ≥ 1 Moq test double or `FakeGateway` programmation in the committed tier-1 and tier-2 suites. |

---

## 12. Traceability

| Item | Reference |
|---|---|
| MVVM split decision | [D2 Architecture §4 — ADR-0001](../D2-Architecture/architecture.md) (central registry: ADR-009) |
| Transport contract | [D2 Architecture §4 — ADR-0002](../D2-Architecture/architecture.md) (wire spec, method catalogue, error model) |
| File-tab worked example | [`refactoring-examples/sub-team-6/file-tab/`](../../../../refactoring-examples/sub-team-6/file-tab/) |
| Debug-tab worked example | [`refactoring-examples/sub-team-6/debug-tab/`](../../../../refactoring-examples/sub-team-6/debug-tab/) |
| Gateway contracts and test double | [`refactoring-examples/sub-team-6/contracts/`](../../../../refactoring-examples/sub-team-6/contracts/) |
| ViewModel unit-test detail spec (Tier 1) | [`viewmodel-unit-tests.md`](viewmodel-unit-tests.md) |
| Gateway and adapter test detail (Tier 2) | §4 of this document |
| UI Toolkit page-object detail spec (Tier 3) | [`ui-toolkit.md`](ui-toolkit.md) |
| Mocking-difficulty baseline | [`BNCH-6.md`](../other/T2-baseline-benchmark/BNCH-6.md) |
| CK metric baseline + projection | [`D4-worked-examples/metrics.md`](../D4-worked-examples/metrics.md) |
| Testability NFRs | [`D1-requirements/requirements.md` §3 — NFR-TST-1/2/3](../D1-requirements/requirements.md) |
| Audit close (F9 / F10 — transport has real consumer) | [`docs/sub-team-6/deliverables/adr-009-audit.md`](../adr-009-audit.md) |
| Assignment spec | §6.6 ST · §9.2.4 · §4.2 #4 · §7.1–7.2 · LO6 |
