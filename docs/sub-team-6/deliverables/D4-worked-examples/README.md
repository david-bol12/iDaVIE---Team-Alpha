# D4 — Worked Refactoring Examples

Two worked examples showing before/after for the Desktop GUI refactoring proposal.

## Example 1 — File Tab

Refactoring the file-open flow from a direct native-plugin call to a ViewModel command via a service gateway.

- [Scope & motivation](ex1-file-tab/file-tab-info-docs/file-tab-scope.md)
- [Code walkthrough](ex1-file-tab/file-tab-code.md)
- [Before class diagram](ex1-file-tab/file-tab-design/before-class-diagram.puml)
- [Before dependency graph](ex1-file-tab/file-tab-design/before-dependency-graph.puml)
- [Before DSM](ex1-file-tab/file-tab-design/before-dsm.md)
- [Before sequence diagram](ex1-file-tab/file-tab-design/before-sequence.puml)
- [After class diagram](ex1-file-tab/file-tab-design/after-class-diagram.puml)
- [After dependency graph](ex1-file-tab/file-tab-design/after-dependency-graph.puml)
- [After DSM](ex1-file-tab/file-tab-design/after-dsm.md)

## Example 2 — Debug Tab

Refactoring the debug console from inline logging to an Observer of a structured logging stream.

- [Code walkthrough](ex2-debug-tab/debug-tab-code.md)
- [Before class diagram](ex2-debug-tab/debug-tab-design/before-class-diagram.puml)
- [Before dependency graph](ex2-debug-tab/debug-tab-design/before-dependency-graph.puml)
- [Before DSM](ex2-debug-tab/debug-tab-design/before-dsm.md)
- [Before sequence diagram](ex2-debug-tab/debug-tab-design/before-sequence.puml)
- [After class diagram](ex2-debug-tab/debug-tab-design/after-class-diagram.puml)
- [After dependency graph](ex2-debug-tab/debug-tab-design/after-dependency-graph.puml)
- [After DSM](ex2-debug-tab/debug-tab-design/after-dsm.md)

## Metrics

CK metric deltas for both examples: [metrics.md](metrics.md)
