# ADR-0002: Client–server transport — JSON-RPC over named pipes (local), gRPC (future remote)

- **Status:** proposed
- **Date:** 2026-05-19
- **Authors:** _(Sub-team 6 TL — fill in)_
- **Backlog:** ARCH-2
- **Supersedes:** —

## Context

Section 6.6 explicitly names the transport: "JSON-RPC over named pipes for local mode; gRPC for future remote streaming." We document the rationale here so the panel can probe it.

## Decision

- **Local mode (Day 1 of the refactored system):** JSON-RPC 2.0 over named pipes. One pipe per session.
- **Remote mode (post-MVP):** gRPC over HTTP/2 with the same `IServiceGateway` interface surface on the client.

The `IServiceGateway` interface is transport-agnostic — the gateway implementation is chosen by the composition root at startup.

## Consequences

- Local-mode latency is good; named pipes are first-class on Windows.
- JSON-RPC is debuggable by tail + eye, lowering the cost of cross-sub-team integration in Sprint 2.
- gRPC adoption later requires a proto schema generated from the same service surface — needs coordination with Sub-team 1.

## Alternatives considered

- **gRPC for local mode too** — Heavier ceremony; harder for first-year cohort to debug; brings Protobuf as a dependency on Day 1.
- **REST over loopback HTTP** — Adds an HTTP server in-process; less efficient; weaker contract.
- **In-process method calls** — Defeats the client–server separation (§4.1).

## References

- §4.1 architectural style.
- §6.6 sub-team work package brief.
- Coordination dependency on Sub-team 1 (Architecture/Micro-kernel) — see DEPS-1.
