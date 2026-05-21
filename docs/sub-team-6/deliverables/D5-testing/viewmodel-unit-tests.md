# TEST-1 — ViewModel Unit-Test Strategy


---

## 1. Goal

Establish a repeatable, Unity-free unit-test strategy for all ViewModel classes produced by the MVVM split (ADR-01). Prove that business logic is testable in isolation — no Unity Editor, no scene, no native plug-ins — and achieve **≥ 70 % branch and line coverage** on domain code.

---

## 2. Scope

| In scope | Out of scope |
|---|---|
| All classes under the proposed `ViewModel/` namespace | `View/` layer (Unity Test Framework handles that — TEST-2) |
| `ServiceGateway` client stub | Server-side code (other sub-teams) |
| `IServiceGateway`, `ILogStream`, `IFileTabViewModel`, `IDebugTabViewModel` interface contracts | Unity MonoBehaviours / UI Toolkit wiring |

---

## 3. Framework stack

| Tool | Version | Role |
|---|---|---|
| **NUnit 3** | ≥ 3.14 | Test runner and assertion library |
| **Moq 4** | ≥ 4.20 | Interface mocking |
| **.NET 7 SDK** (standalone) | match Unity 2021 Mono runtime | Build + run tests outside Unity |
| **Coverlet** | latest | Branch + line coverage collection |
| **ReportGenerator** | latest | HTML + Cobertura reports for CI |

Tests live in a plain **.NET class library project** (`DesktopClient.Tests/`) that references only:
- `DesktopClient.ViewModels` (our new project, no `UnityEngine` dependency)
- `NUnit`, `Moq`, `Coverlet` NuGet packages

Unity is never imported. This is the hard guarantee that enforces the anti-corruption layer.

---

## 4. Dependency isolation rules

1. **No `UnityEngine` in `ViewModel/`** — any ViewModel that `using UnityEngine` fails the build (`<Nullable>enable</Nullable>` + `<WarningsAsErrors>` in the `.csproj`).
2. **All external services behind interfaces** — `IServiceGateway`, `ILogStream`, `IDialogService`, `IPanel`. Moq mocks these.
3. **No `FindObjectOfType` / `MonoBehaviour` in ViewModel** — enforced by the standalone project: these types don't exist.
4. **Static clock / randomness injected** — `ISystemClock` interface so time-dependent logic is deterministic.

---

## 5. Test categories and naming convention

```
[Category("ViewModel")]
public class FileTabViewModelTests
{
    // Arrange / Act / Assert structure.
    // Method name: MethodUnderTest_Scenario_ExpectedBehaviour
    [Test]
    public void OpenCube_HappyPath_CallsGatewayWithExpectedArgs() { ... }
    [Test]
    public void OpenCube_GatewayThrows_SetsErrorMessageProperty() { ... }
    [Test]
    public void OpenCube_WhileLoading_CommandIsIgnoredAndNoDoubleCall() { ... }
}
```

Categories:
- `ViewModel` — pure ViewModel logic
- `Gateway` — `ServiceGateway` client stub with mock transport
- `Integration` — ViewModel + Gateway together (no Unity still)

---

## 6. Mock patterns

### 6.1 Happy path — `IServiceGateway`

This pattern tests the normal success case: the gateway succeeds, the ViewModel updates its state correctly, and it delegates to the gateway exactly once with the right arguments.

`IServiceGateway` is the interface that separates ViewModel logic from the actual transport layer (named pipe / gRPC). By mocking it with Moq, the test controls what the gateway returns without needing a running server. `ReturnsAsync` makes the mock complete successfully with a known `CubeMetadata` value. After the command executes, the test uses `Verify` to assert the gateway was called with the exact file path — catching bugs where the ViewModel mangles or ignores the input — and then asserts that `IsLoading` is `false` and `ErrorMessage` is `null`, proving the ViewModel cleaned up its busy state and did not surface a spurious error.

```csharp
var gateway = new Mock<IServiceGateway>();
gateway.Setup(g => g.LoadCubeAsync(It.IsAny<string>(), CancellationToken.None))
       .ReturnsAsync(new CubeMetadata { AxisCount = 3 });

var vm = new FileTabViewModel(gateway.Object);
await vm.OpenCubeCommand.ExecuteAsync("/data/cube.fits");

gateway.Verify(g => g.LoadCubeAsync("/data/cube.fits", CancellationToken.None), Times.Once);
Assert.That(vm.IsLoading, Is.False);
Assert.That(vm.ErrorMessage, Is.Null);
```

### 6.2 Error path — gateway throws

This pattern tests fault handling: when the gateway raises an exception (network timeout, bad file path, server error), the ViewModel must catch it, surface a human-readable message through `ErrorMessage`, and leave `IsLoading` as `false` so the UI does not get stuck in a spinning state.

The mock is configured with `ThrowsAsync` so the awaited call throws `GatewayException` instead of returning a value. The ViewModel is expected to catch this internally — the test itself should not throw — and map the exception message to the `ErrorMessage` property. This verifies that error handling lives in the ViewModel rather than leaking into the View or going unhandled.

```csharp
gateway.Setup(g => g.LoadCubeAsync(...)).ThrowsAsync(new GatewayException("timeout"));

await vm.OpenCubeCommand.ExecuteAsync("/bad/path.fits");

Assert.That(vm.ErrorMessage, Is.EqualTo("timeout"));
Assert.That(vm.IsLoading, Is.False);
```

### 6.3 Observer test — `ILogStream` (Debug tab)

This pattern tests the Debug tab's role as a **passive observer** of the application's logging infrastructure. `DebugTabViewModel` subscribes to `ILogStream.OnLogEntry` and appends each entry to a bindable `Entries` collection. It must not poll or own the log source — it simply reacts to events.

Moq's `Raise` method fires the event directly on the mock, simulating the log infrastructure emitting a warning. The test then asserts that the ViewModel's `Entries` collection contains exactly one item with the correct message. This confirms that the subscription is wired up, that the entry is appended (not replaced), and that the ViewModel does not filter or silently drop the event. Running this without Unity proves the Observer wiring is pure C# with no scene or engine dependency.

```csharp
var logStream = new Mock<ILogStream>();
var vm = new DebugTabViewModel(logStream.Object);

logStream.Raise(l => l.OnLogEntry += null,
    new LogEntry { Level = LogLevel.Warning, Message = "VR init slow" });

Assert.That(vm.Entries, Has.Count.EqualTo(1));
Assert.That(vm.Entries[0].Message, Is.EqualTo("VR init slow"));
```

---

## 7. Coverage targets and measurement

| Namespace | Branch target | Line target | Measured by |
|---|---|---|---|
| `ViewModel/` | ≥ 70 % | ≥ 70 % | Coverlet + CI gate |
| `ServiceGateway/` client | ≥ 70 % | ≥ 70 % | Coverlet + CI gate |
| `View/` (Unity) | tracked | tracked | Unity Test Framework, not gated |

Run locally:

```
dotnet test DesktopClient.Tests/ --collect:"XPlat Code Coverage"
reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

CI (Quality Guild pipeline) will fail the build if either gate is missed on `main`.

---

## 8. What is NOT unit-tested here

| Concern | Why excluded | Handled by |
|---|---|---|
| UI Toolkit bindings | Requires Unity runtime | TEST-2 (page-object pattern) |
| Named-pipe transport | I/O boundary | Gateway integration test with test server stub |
| Unity MonoBehaviour lifecycle | Requires scene | Manual smoke test list |

---

## 9. Traceability

| Item | Reference |
|---|---|
| MVVM split decision | ADR-01 ([adrs/0001-mvvm-split.md](../adrs/0001-mvvm-split.md)) |
| Interface contracts | ARCH-8 |
| Full test strategy document | TEST-4 ([test-strategy.md](../test-strategy.md)) |
| Worked test spec for `OpenCubeCommand` | TEST-3 |
| CK thresholds this strategy defends | §7.1 — RFC ≤ 50, LCOM ≤ 0.5 on ViewModel classes |
| Assignment spec reference | §9.2.4, §6.6 ST, LO6 |

---

## 10. Definition of Done checklist

- [ ] This doc committed to `docs/sub-team-6/deliverables/TEST-1.md`
- [ ] Peer-reviewed by at least one other sub-team member
- [ ] Linked from `docs/sub-team-6/README.md`
- [ ] `DesktopClient.Tests/` project skeleton exists under `refactoring-examples/sub-team-6/` with at least three passing stub tests
- [ ] Coverage report generated and screenshot committed alongside (or CI badge added)
- [ ] TEST-4 (full test strategy doc) updated to reference this strategy
