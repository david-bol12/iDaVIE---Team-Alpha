# Architecturally Significant Requirements (ASRs)

## 1. Purpose

This document defines the Architecturally Significant Requirements (ASRs) and Non-Functional Requirements (NFRs) for the iDaVIE kernel and plug-in system, owned by Sub-team Apaties I. Requirements are derived from ISO/IEC 25010:2023 maintainability sub-characteristics and translated into concrete, testable statements that drive architecture decisions and CI enforcement.

## 2. Architecturally Significant Requirements (ASRs)

Each ASR maps directly to one ISO/IEC 25010:2023 maintainability sub-characteristic. All ASRs are enforceable — each has a defined verification method and is tracked in the CI pipeline from Day 2.

### 2.1 Modularity

> **Modularity** — Degree to which a change to one component has minimal impact on others

Currently lack of modularity is a serious issue within the codebase likely created due to years of mounting technical debt. Architecture is tightly coupled with many god-classes. This makes changes risky and expensive: a modification to one component frequently forces changes (and recompilation) across many others, increases the chance of introducing defects, and makes the system difficult to analyse, modify and test. The requirements below directly target this problem by enforcing loose coupling, interface-based boundaries, and the elimination of dependency cycles.

| Requirement | Verified by |
|---|---|
| No circular dependencies shall exist between any two top-level components. Any circular dependencies currently in the codebase should be replaced with dependency inversion. | NDepend CQLinq rule on every PR — fail = merge blocked |
| A change to one plug-in (e.g. FitsReader) shall require zero recompilation of any other plug-in. This ensures true modularity allowing teams to build plugins independently of each other. | Manual verification + CI build isolation test |
| CBO must be ≤14 for domain classes and ≤25 for orchestrators | Understand CK metrics snapshot at Day 2 and Day 13 |
| Dependency cycles at namespace and assembly level must be 0. All server side code must follow the strict Domain / Application / Infrastructure / Plug-in architecture. | NDepend cycle detection — fail = merge blocked |
| All cross-component calls shall be made through an interface. No component shall reference the concrete implementation type of another top-level component. | NDepend CQLinq rule asserting that methods on a public component boundary are only invoked via interface types — fail = merge blocked |

### 2.2 Analysability

> **Analysability** — Effectiveness of assessing change impact and identifying parts to modify

The current code base is plagued with analysability issues majorly due to poor modularisation and a lack of documentation and comments. Currently, due to classes exceeding their scope, their naming is not a clear indication of their capabilities.

| Requirement | Verified by |
|---|---|
| Cyclomatic complexity per method must not exceed 10 as outlined by IEEE. | SonarQube complexity gate on every PR |
| Classes exceeding 200 LOC, and Methods exceeding 30 LOC are marked for manual review. Requiring manual review of lengthy classes and methods aims to ensure they remain within scope and don't lower readability. | SonarQube LOC rule — fail = merge blocked |
| Every public API boundary must have documented contract. This creates a layer of abstraction and independence between the iDaVIE code base and the users of its API. | Manual Verification |
| Code duplication must not exceed 5% across domain classes — As listed by SWEBOK: To avoid the problem of code clones, developers should encapsulate reusable code fragments into well-structured libraries or components. | SonarQube duplication report |

### 2.3 Modifiability

> **Modifiability** — Degree to which a product can be modified without introducing defects

The tight coupling between classes and Unity dependencies make modifying the codebase difficult and dangerous. Because logic is bound to MonoBehaviour lifecycles, resolved through scene lookups (FindObjectOfType, GameObject.Find), and concentrated in 1,400–1,900-line God objects, a single change ripples unpredictably across modules. With no automated tests to catch regressions, every edit carries a high, unverifiable risk of silently breaking existing behaviour.

| Requirement | Verified by |
|---|---|
| Replacing Unity version requires changes only in the Infrastructure layer — zero Domain or Application changes. This enables easier migration to Unity 6 and future Unity versions. Separating Domain and Application logic also makes migrating to other game engines easier if required. | Architecture fitness function in CI (ArchUnit-style) |
| A new C/C++ plug-in can be added without modifying existing kernel code — SOLID Open-Closed Principle | Design review + Test suite |
| Maintain CodeScene Health Score of 8-10 (Healthy). Ensures low friction code changes. Code is consistently easy to read and modify. | CodeScene dashboard |
| Propagation cost (DV8) must decrease by ≥20%. High propagation cost can have a large impact on modifiability as small changes can cause large ripples across codebase due to tight coupling. | DV8 sprint boundary snapshot |

### 2.4 Testability

> **Testability** — Effectiveness of establishing test criteria and performing tests

Currently the lack of automated testing is a serious issue within the codebase, compounded by an architecture that actively resists verification. With no unit or integration tests and no CI test pipeline, the bulk of the logic is bound to Unity's MonoBehaviour lifecycle, resolved through scene lookups and global singletons, and dependent on an unmockable native plugin layer — leaving few seams at which behaviour can be exercised in isolation. This makes every change unsafe to make: defects go undetected until runtime, regressions slip in silently, and developers must manually re-verify the application after each modification. The requirements below directly target this problem by mandating test coverage, extracting pure logic from engine and hardware dependencies, and introducing interface-based seams that allow components to be tested in isolation.

| Requirement | Verified by |
|---|---|
| Every domain class must have zero direct UnityEngine imports. Creating this separation allows domain code to be tested without requiring a full Unity build. This removes a lot of testing overhead by not requiring the Unity Editor or PlayMode runtime to spin up for every test. Tests can run as plain .NET unit tests against the domain logic directly, so they execute in milliseconds instead of waiting on a full engine load. This also makes tests more deterministic and makes producing test harnesses easier. | NDepend layer rule — fail = merge blocked |
| Branch coverage ≥70% on all domain and application layer classes. Ensures high coverage on areas that hold the most logic. | SonarQube coverage gate |
| Every public interface must be mockable with no Unity runtime present. Again removes testing overhead by not requiring a Unity Runtime present in testing. | Edit-mode unit test suite in CI |
| Interface size must not exceed 7 public members (ISP compliance) | NDepend interface size rule |

## 3. Kernel Non-Functional Requirements (NFRs)

The following NFRs apply specifically to the kernel boundary and plug-in host layer. They govern runtime behaviour, operational resilience, and ABI contract stability.

| NFR | Requirement | Measured by |
|---|---|---|
| Load time | Kernel initialises in under 3 seconds on target hardware | BenchmarkManager timing log |
| Plug-in isolation | A crash in one plug-in must not bring down the kernel or other plug-ins | Contract test suite — fault injection |
| Hot-reload | A plug-in can be unloaded and reloaded without restarting the application | Manual test + CI scenario script |
| Observability | Every plug-in call is logged with timing and error codes via structured logging | Log output verification in CI |
| ABI stability | No breaking ABI change within a major version — semantic versioning enforced | NDepend ABI versioning rules |
| Platform portability | No direct kernel32 calls — NativePluginLoader must be cross-platform | CI build matrix (Windows + Linux) |

## 4. Stakeholder Expectations

### Medium-term

- Switch rendering between emission and absorption
- Visualise particle / sparse multiparameter datasets (e.g. simulations)
- Scripting API for smooth camera fly-through videos
- Movable projection plane showing a 2D cross-section in a separate window
- Separate the visualisation tool from the analysis tools (plugin-based)

### Long-term

- Integrate a Python console into the application
  - Spec created by Team 6
- Add visualisation modes such as isocontours
- Multiplayer / spectator mode (controller drives, others follow)
- Integrate a VO system for retrieving images from catalogues in other wavelengths
- Visualise multiple cubes at once, or a time-series
- Save application state / workspace (as in CARTA)
  - Fully implemented by Team 7

## 5. Requirements Traceability

All ASRs, NFRs and Stakeholder expectations listed above trace directly to the following sources:

- ISO/IEC 25010:2023 — Modularity, Analysability, Modifiability, Testability sub-characteristics
- iDaVIE Assessment Specification — Section 4.2 Mandatory Architectural Constraints
- iDaVIE Assessment Specification — Section 6.1 Architecture and Micro-kernel Core
- iDaVIE codebase Day 2 baseline — identified god-classes, singletons, and Unity lifecycle pollution
- iDaVIE Future plans docs — Stakeholder Expectations
- SWEBOK Guide V4.0
- Modern Software Engineering — David Farley
