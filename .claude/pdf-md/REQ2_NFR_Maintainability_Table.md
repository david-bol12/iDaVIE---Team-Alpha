# REQ2_NFR_Maintainability_Table.pdf

_Pages: 3_


---

## Page 1

Project: Software Engineering Coursework
Task ID: REQ-2
Owner: POL
Sprint Days: 3–4
Feeds into: Requirements Document, T4 (Test Plan), LO2
This document captures the Non-Functional Requirements (NFRs) for the system, each
traced to a specific sub-characteristic of the ISO/IEC 25010 Maintainability quality
characteristic. Every NFR includes a measurable acceptance criterion to satisfy the Definition
of Done (DoD).
ISO 25010 defines five Maintainability sub-characteristics:
Sub-
Characteristic
Definition
Modularity
The system is composed of discrete components such that a change to one component has
minimal impact on others
Reusability
An asset can be used in more than one system or in building other assets
Analysability
The ease with which the impact of an intended change on the system can be assessed, or a
cause of failure diagnosed
Modifiability
The degree to which a product or system can be effectively and efficiently modified without
introducing defects
Testability
The degree of effectiveness and efficiency with which test criteria can be established and tests
performed
NFR
ID
NFR Description
ISO 25010 Sub-
Characteristic
Priority
Acceptance Criterion
NFR-
M1
Each system module must
encapsulate a single, clearly
defined responsibility
Modularity
High
No module exceeds 200 lines of code;
each module has exactly one stated
responsibility documented in its
header comment, verified during
code review
REQ-2: Non-Functional Requirements Traced to
ISO 25010 Maintainability Sub-Characteristics
Overview
NFR Table


---

## Page 2

NFR
ID
NFR Description
ISO 25010 Sub-
Characteristic
Priority
Acceptance Criterion
NFR-
M2
Changes to one module must
not require modifications to
more than one other
unrelated module
Modularity
High
Impact analysis during sprint review
confirms that any single-module
change propagates to ≤1 other
module, verified across at least 3
change scenarios
NFR-
M3
Common utility functions
(e.g., validation, formatting,
logging) must be
implemented in shared
libraries and reused across
modules
Reusability
Medium
Static analysis confirms zero
duplication of utility logic across
modules; all reusable components are
stored in a dedicated /utils or
equivalent package
NFR-
M4
A developer unfamiliar with
the codebase must be able to
identify the root cause of a
reported defect within a
defined time limit
Analysability
High
In a peer review exercise, a team
member not involved in the
component's implementation
localises the fault within 30 minutes
using only the codebase and inline
documentation
NFR-
M5
All public methods and
classes must include inline
documentation describing
their purpose, parameters,
and return values
Analysability
Medium
Documentation coverage ≥ 90% of all
public interfaces, verified by
automated documentation tool (e.g.,
Javadoc, Doxygen, or equivalent)
NFR-
M6
Implementing a new feature
of defined scope must not
require modification of more
than three existing source
files
Modifiability
High
Verified by tracking file-change diffs
in version control during at least two
feature additions in the sprint;
average files changed ≤ 3
NFR-
M7
Introducing a modification
must not cause regressions
in existing functionality
Modifiability
High
All existing automated tests pass
after any modification; zero
regression failures in the CI/CD
pipeline
NFR-
M8
All business logic functions
must be independently unit-
testable in isolation from
external dependencies
Testability
High
≥ 80% unit test code coverage
achieved across all business logic
modules, measured by an automated
coverage tool (e.g., JaCoCo,
Coverage.py); dependency injection
or mocking used where required
NFR-
M9
The system must support
automated regression
testing via a CI pipeline
Testability
High
A CI pipeline (e.g., GitHub Actions)
runs the full test suite on every
commit; test results and coverage
reports are generated automatically
and accessible to all team members
NFR-
M10
Test data must be separate
from production logic and
configurable without code
changes
Testability
Medium
All test data is stored in external
configuration files or test fixtures; no
hardcoded test values appear in
production source code, verified by
code review


---

## Page 3

ISO 25010 Sub-Characteristic
Covered By
Modularity
NFR-M1, NFR-M2
Reusability
NFR-M3
Analysability
NFR-M4, NFR-M5
Modifiability
NFR-M6, NFR-M7
Testability
NFR-M8, NFR-M9, NFR-M10
All five ISO 25010 Maintainability sub-characteristics are covered by at least one NFR.
Each acceptance criterion is measurable and verifiable — either through automated
tooling, code review, or a defined peer exercise.
This table feeds directly into:
Requirements Document — NFR section
T4 — Test plan (NFR-M8, NFR-M9, NFR-M10 directly inform test strategy and
coverage targets)
LO2 — Demonstrates ability to elicit and specify verifiable non-functional
requirements aligned to a recognised quality standard
Traceability Summary
Notes
