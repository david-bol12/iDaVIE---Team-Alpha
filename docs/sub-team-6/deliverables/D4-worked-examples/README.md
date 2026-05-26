# D4 — Worked Refactoring Examples

Two worked examples showing before/after for the Desktop GUI refactoring proposal.

All source lives under [`refactoring-examples/sub-team-6/`](../../../../refactoring-examples/sub-team-6/).

## Example 1 — File Tab

Refactoring the file-open flow from a direct native-plugin call to a ViewModel command via a service gateway.

- [Before sequence](../../../../refactoring-examples/sub-team-6/file-tab/before-sequence.md)
- [After sequence](../../../../refactoring-examples/sub-team-6/file-tab/after-sequence.md)
- [Class diagram](../../../../refactoring-examples/sub-team-6/file-tab/class-diagram.md)
- [Dependency graph](../../../../refactoring-examples/sub-team-6/file-tab/dependency-graph.md)
- [CK metrics](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md)
- [FileTabViewModel.cs](../../../../refactoring-examples/sub-team-6/file-tab/skeleton/FileTabViewModel.cs)
- [FileTabViewModelTests.cs](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs)

## Example 2 — Debug Tab

Refactoring the debug console from inline logging to an Observer of a structured logging stream.

- [Before trace](../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md)
- [After sequence](../../../../refactoring-examples/sub-team-6/debug-tab/after-sequence.md)
- [Class diagram](../../../../refactoring-examples/sub-team-6/debug-tab/class-diagram.md)
- [Dependency graph](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md)
- [CK metrics](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md)
- [DebugTabViewModel.cs](../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabViewModel.cs)
- [DebugTabTests.cs](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs)

## Metrics

CK metric deltas for both examples: [metrics.md](metrics.md)
