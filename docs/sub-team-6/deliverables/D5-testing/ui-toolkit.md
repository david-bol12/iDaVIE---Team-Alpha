# TEST-2 — UI Toolkit Page-Object Pattern for Integration Tests

---

## 1. Goal

Specify a repeatable integration-test pattern for the View layer of the MVVM split (ADR-0001), so that UXML-backed panels can be exercised against a real `IFileTabViewModel` / `IDebugTabViewModel` without leaking UI Toolkit selector strings into tests. Page objects are the seam: they make selector changes a one-file edit, and they keep test intent readable on the pitch slide.

Together with `viewmodel-unit-tests.md`, this discharges the two Software Testing bullets in §6.6.

---

## 2. Scope

| In scope | Out of scope |
|---|---|
| `iDaVIE.Client.View` panels backed by a `UIDocument` | ViewModel internals (covered by `viewmodel-unit-tests.md`) |
| `FileTabPage`, `DebugTabPage` — one page object per panel with a real AFTER skeleton | Render, Stats, Sources tabs (no AFTER skeleton — out of D4 scope) |
| Bindings between View and ViewModel exercised through page-object methods | Native plug-in I/O; gateway transport |
| UI Toolkit event-driven interaction via `SendEvent` | Pixel / USS visual-diff testing |

This is a **design-only** specification. We document the pattern and a worked snippet; we do not stand up a Unity Test Framework project against production code (the assignment forbids changes under `Assets/`).

---

## 3. Framework stack

| Tool | Version | Role |
|---|---|---|
| **Unity Test Framework** (Play Mode) | bundled with Unity 2021.3 / Unity 6 | Hosts tests that need a live `UIDocument` panel |
| **NUnit 3** | bundled with UTF | Runner + assertions |
| **Moq 4** | ≥ 4.20 | Mocks `IFitsService`, `IFileDialogService`, `IVolumeService`, `ILogStream` |
| **Coverlet** | latest | Coverage on the View assembly (tracked, not gated — see §8) |

Tests live in a Play-Mode test assembly `iDaVIE.Client.View.IntegrationTests` that references:
- `iDaVIE.Client.View` (panels + UXML)
- `iDaVIE.Client.ViewModel` (real ViewModels — not mocked)
- The service interfaces (mocked at the gateway seam)

The composition root is **not** invoked. Each test wires its own ViewModel from mocked services, attaches it to a fresh `UIDocument`, and constructs the page object.

---

## 4. Page-object contract

Five rules, in priority order. Every page object must satisfy all five.

1. **One page object per UXML panel.** Named `{Panel}Page` — `FileTabPage`, `DebugTabPage`. File lives alongside the corresponding View under `…/IntegrationTests/Pages/`.
2. **Constructed from a root `VisualElement`.** The test owns the `UIDocument` lifecycle and passes the root in; the page does not load UXML itself. This keeps pages reusable across editor and runtime test rigs.
3. **Public surface is intent-only.** Methods read like user actions (`PickImage("/data/cube.fits")`, `ClickLoad()`); properties read like user-visible state (`ValidationText`, `IsLoadButtonEnabled`). No `VisualElement`, no `Q<T>` result, no UXML node ever leaks out. **ISP target ≤ 7 public members per page** — mirrors §7.1 and the `BNCH-7` interface-size audit.
4. **Selector strings exist nowhere else.** Every `root.Q<Button>("load-cube-btn")` is encapsulated in its page. UXML `name` attributes are treated as a stability contract owned by the View team; renaming one is a breaking change visible in exactly one file.
5. **Event simulation uses UI Toolkit event types.** `NavigationSubmitEvent` for buttons, `ChangeEvent<T>` for text fields and dropdowns, `PointerDownEvent` / `PointerUpEvent` for list selection — dispatched via `element.SendEvent(evt)`. Driving at the `InputSystem` layer is the wrong abstraction for this test layer and is forbidden.

---

## 5. Required integration tests

The word "required" in §6.6 ST is discharged by the following minimum list. Each test follows the same shape: **construct page → drive page → assert page state**. The ViewModel is real; only services are mocked.

### 5.1 File tab — `FileTabPage` + mocked `IFitsService` / `IFileDialogService` / `IVolumeService`

| Test | What it proves |
|---|---|
| `BrowseImage_HappyPath_ShowsPathAndHeader` | `BrowseImageCommand` round-trips through the dialog mock, the FITS mock, and the View shows the resulting `ImagePath` + `HeaderText`. |
| `BrowseImage_ServiceThrows_ShowsValidationMessage_LoadButtonDisabled` | Exception in `IFitsService.OpenImageAsync` surfaces as `ValidationMessage` in the View, and `LoadCommand` stays disabled. |
| `BrowseMask_AxesMismatch_ShowsValidationMessage` | `MaskAxesMatchImage` failure is rendered to the user (validation banner visible). |
| `LoadCommand_Disabled_UntilIsLoadable` | The Load button reflects `IsLoadable` — disabled until a valid 3-D+ cube is selected. |
| `LoadCommand_WhileLoading_ButtonIsDisabled` | While `IsLoading` is true the Load button is disabled — proves the `IsLoading` → command `CanExecute` chain reaches the View. |

### 5.2 Debug tab — `DebugTabPage` + fake `ILogStream`

| Test | What it proves |
|---|---|
| `StreamEmitsEntry_AppearsInListView` | A single `LogEntry` published by `ILogStream` is appended to the ViewModel and rendered in the list. |
| `MultipleEntries_AppearInOrder` | Order is preserved end-to-end (publisher → ViewModel `_entries` → bound list). |
| `Clear_EmptiesListView` | `ClearEntries()` empties the bound list view in one frame. |
| `EntriesChanged_TriggersRebind` | The View's bind hook subscribes to `EntriesChanged` — confirms the Observer wiring is complete on the View side, not just the VM side. |

---

## 6. Worked snippet — `FileTabPage` + one test

Illustrative only — pattern, not production code. A copy lives under `refactoring-examples/sub-team-6/file-tab/skeleton/test/`.

```csharp
// Pages/FileTabPage.cs — selector strings live ONLY here.
public sealed class FileTabPage
{
    private readonly VisualElement _root;
    public FileTabPage(VisualElement root) => _root = root;

    public string?  ImagePathText      => _root.Q<Label>("image-path-label").text;
    public string?  ValidationText     => _root.Q<Label>("validation-label").text;
    public bool     IsLoadButtonEnabled => _root.Q<Button>("load-cube-btn").enabledSelf;

    public void PickImage(string path)
    {
        // The View binds a TextField named "image-path-input" to the dialog mock's return.
        var field = _root.Q<TextField>("image-path-input");
        field.value = path;
        field.SendEvent(ChangeEvent<string>.GetPooled(string.Empty, path));
    }

    public void ClickLoad()
        => _root.Q<Button>("load-cube-btn")
                .SendEvent(NavigationSubmitEvent.GetPooled());
}
```

```csharp
// FileTabIntegrationTests.cs
[UnityTest]
public IEnumerator BrowseImage_HappyPath_ShowsPathAndHeader()
{
    var fits = new Mock<IFitsService>();
    fits.Setup(s => s.OpenImageAsync("/data/cube.fits"))
        .ReturnsAsync(new FitsFileInfo { /* … 3-axis cube … */ });

    var dialog = new Mock<IFileDialogService>();
    dialog.Setup(d => d.PickFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
          .ReturnsAsync("/data/cube.fits");

    var vm   = new FileTabViewModel(fits.Object, dialog.Object, Mock.Of<IVolumeService>());
    var root = BuildPanel(vm);              // helper: loads UXML + binds vm
    var page = new FileTabPage(root);

    page.ClickBrowseImage();
    yield return null;                      // one frame for the binding to flush

    Assert.That(page.ImagePathText, Is.EqualTo("/data/cube.fits"));
    Assert.That(page.IsLoadButtonEnabled, Is.True);
}
```

Total surface area of `FileTabPage` here: 3 properties + 2 methods = **5 members**, well inside the ISP ≤ 7 budget.

---

## 7. Settle rule

UI Toolkit's binding flushes on the next layout pass. Every test must yield one frame after dispatching an event before asserting on bound state:

```csharp
page.ClickLoad();
yield return null;     // flush bindings
Assert.That(page.ValidationText, Is.Not.Empty);
```

For VM-driven async paths, prefer awaiting the ViewModel's notification surface (`EntriesChanged` for the Debug tab; a `TaskCompletionSource` hooked onto `PropertyChanged` for the File tab) over arbitrary frame counts. Polling sleeps are forbidden — they are the root cause of flake we want to design out from day one.

---

## 8. Coverage stance

| Assembly | Branch / line target | Gated in CI? |
|---|---|---|
| `iDaVIE.Client.ViewModel` | ≥ 70 % | yes (see `viewmodel-unit-tests.md` §7) |
| `iDaVIE.Client.View` | tracked | **no** |

View-layer coverage is reported but not gated. Justification: UI Toolkit panels are configuration-heavy and their behaviour is exercised by the ViewModel tests in transitive form; a strict branch target on `View` would either inflate to noise (binding boilerplate) or push us into testing UI Toolkit itself.

This matches the layered table in `docs/sub-team-6/test-strategy.md` §2 and §7.2 of the spec ("Unity-bound code tracked but not in strict target").

---

## 9. What is NOT tested here

| Concern | Why excluded | Handled by |
|---|---|---|
| ViewModel-only logic (commands, validation, `IsLoadable`) | Faster, deterministic as unit tests | `viewmodel-unit-tests.md` |
| Gateway transport (named pipe / gRPC) | I/O boundary; not in this sub-team's scope | Gateway integration test with stub server (Sub-team 1) |
| Pixel rendering, USS layout | Out of assignment scope; high flake | — |
| Render / Stats / Sources tabs | No AFTER skeleton in D4 | Pattern extends trivially if those are built later |

---

## 10. Traceability

| Item | Reference |
|---|---|
| MVVM split decision | [ADR-0001 — MVVM split](../../adrs/0001-mvvm-split.md) |
| File-tab ViewModel under test | [`refactoring-examples/sub-team-6/file-tab/skeleton/IFileTabViewModel.cs`](../../../../refactoring-examples/sub-team-6/file-tab/skeleton/IFileTabViewModel.cs) |
| Debug-tab ViewModel under test | [`refactoring-examples/sub-team-6/debug-tab/skeleton/IDebugTabViewModel.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/IDebugTabViewModel.cs) |
| ViewModel-side unit-test strategy | [`viewmodel-unit-tests.md`](viewmodel-unit-tests.md) |
| Consolidated test strategy (2–4 pp) | [`docs/sub-team-6/test-strategy.md`](../../test-strategy.md) — §5 should link here |
| Interface-size audit (ISP ≤ 7) | BNCH-7 |
| Assignment spec | §6.6 ST bullet 2 · §9.2.4 · §4.2 #4 · §7.2 testability · LO6 |

---

## 11. Definition of Done

- [ ] This document committed to `docs/sub-team-6/deliverables/D5-testing/ui-toolkit.md`
- [ ] Peer-reviewed by at least one other sub-team member
- [ ] Linked from `docs/sub-team-6/README.md`
- [ ] `docs/sub-team-6/test-strategy.md` §5 replaced with a one-line pointer to this document
- [ ] `FileTabPage` snippet committed under `refactoring-examples/sub-team-6/file-tab/skeleton/test/`
