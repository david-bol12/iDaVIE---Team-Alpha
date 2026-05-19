# Sub-team 6 — Test Strategy (2–4 pp)

**Status:** stub — TEST-4 in the backlog. Final by Day 10 (Fri 29 May 2026).

## 1. Scope

Testability strategy for the Desktop GUI and Client Shell, evidencing LO6.

## 2. Layered test approach

| Layer | Test type | Framework | Unity required? | Coverage target |
|---|---|---|---|---|
| ViewModel (pure C#) | Unit | NUnit + Moq | No | ≥ 70 % branch + line |
| Service Gateway client | Unit (with mock transport) | NUnit + Moq | No | ≥ 70 % |
| View (UI Toolkit) | Integration (page-object) | Unity Test Framework | Yes | tracked, not gated |
| End-to-end | Smoke (manual at first) | n/a | Yes | smoke list, not %-gated |

Spec reference: §7.2 testability metric family; §6.6 ST.

## 3. Dependency isolation

- Every Unity API call inside the View layer wrapped behind a method we control.
- ViewModel sees only its own interfaces (`IServiceGateway`, `ILogStream`, …).
- No `FindObjectOfType` inside the ViewModel ever.

## 4. Mocking strategy

- Moq for interface-based mocks.
- `IServiceGateway` mocked to drive ViewModel command tests (TEST-3).
- `ILogStream` mocked to drive Debug tab Observer tests.

## 5. UI Toolkit page-object pattern (TEST-2)

_(Spec the pattern: one Page-Object class per visual panel, exposes intent-named queries (`ClickOpenCubeButton()`, `EnterPath(string)`), wraps `UIDocument` query selectors.)_

## 6. Worked test specification: `OpenCubeCommand` (TEST-3)

```csharp
[Test]
public void OpenCube_HappyPath_CallsGatewayWithExpectedArgs() { ... }
[Test]
public void OpenCube_GatewayThrows_ShowsErrorState() { ... }
[Test]
public void OpenCube_WhileLoading_IsIgnored() { ... }
```

## 7. Interface-size audit (BNCH-7)

ISP target ≤ 7 public members per interface. Audit table covers `IServiceGateway`, `IFileTabViewModel`, `IDebugTabViewModel`, `ILogStream`, `IPanel`.

## 8. Mocking-difficulty count (BNCH-6)

Before vs projected count of static / Unity API calls per class — demonstrates testability improvement.

## 9. CI integration

Quality Guild owns the team CI/CD pipeline (T6). Our coverage reports feed it.
