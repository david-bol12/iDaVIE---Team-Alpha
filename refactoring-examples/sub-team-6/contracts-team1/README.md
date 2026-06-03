# Service-gateway contract — Desktop GUI ↔ Server kernel (Sub-team 1)

**Dependency:** Sub-team 6 (Desktop GUI) → **Sub-team 1 (Architecture / Micro-kernel)**.
**Risk:** R01 (service-gateway contract not frozen) · **Backlog:** DEPS-1, ARCH-2 · **Related ADR:** ADR-0002 (transport), ADR-009 §1 (ours).
**Assembly:** `iDaVIE.Client.Gateway` (`GatewayContracts.csproj`) — net8.0, zero UnityEngine references.

**Status:** 🟡 Sub-team 6 owns this client-side stub; **Sub-team 1 ratifies or replaces it by Day 7** (DEPS-1). Wire spec is complete in ADR-0002.

> **Companion contract:** the Desktop ↔ VR Interaction boundary (Sub-team 4) lives in a separate folder, [`../interaction-gateway`](../interaction-gateway/README.md).

---

## Why this contract exists

Every cross-process call from the desktop client to the server passes through **one transport-agnostic seam**, `IServiceGateway`. ViewModels and domain adapters depend on this interface only — never on a concrete transport — so the named-pipe implementation can be swapped for gRPC later (ADR-0002) without touching the client domain code. This is the heart of our anti-corruption layer.

## Files

| File | What it is |
|---|---|
| `IServiceGateway.cs` | **The contract.** `ConnectAsync`, `SendAsync<TResult>(method, params, ct)` for request/response, and an `OnNotification` event for server-pushed messages. |
| `JsonRpcPipeGateway.cs` | Real implementation — JSON-RPC 2.0 over a Windows named pipe, per ADR-0002 "Wire specification". |
| `LengthPrefixFraming.cs` | Byte-level encode/decode of the wire framing (`<length><LF><utf-8 json>`). |
| `JsonRpcNotification.cs` | DTO for a server-initiated notification (no `id`); carries raw `params` for lazy deserialisation. |
| `JsonRpcException.cs` | Structured error carrier; codes match ADR-0002's error model. |
| `FakeGateway.cs` | Test double. Pre-program responses by method (`SetResponse`/`SetError`), assert on `Sent`, and `EmitNotification` synchronously — no real pipe. Satisfies the Section 4.2 #4 non-negotiable. |
| `GatewayContracts.csproj` | Builds the production files as `iDaVIE.Client.Gateway` (excludes `tests/`). |
| `tests/` | NUnit suite: `FakeGatewayTests`, `LengthPrefixFramingTests` (own csproj). |

## The interface at a glance

```csharp
public interface IServiceGateway : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default);
    event Action<JsonRpcNotification>? OnNotification;
}
```

Method names are namespaced with `.` and defined in the **ADR-0002 method catalogue** (`docs/sub-team-6/.../D2-Architecture/architecture.md` §4): `session.*`, `file.*`, `dataset.*`, `log.*`, `progress.*`. The File-tab and Debug-tab worked examples consume this gateway through domain adapters (`IFitsService`, `ILogStream`).

## Open points — for Sub-team 1 to ratify

- Freeze the v0.1 method catalogue (R01 — was due Day 5).
- Confirm pipe naming, framing, and error-code ranges in ADR-0002 match the server's handler.
- Server-side implementation of the JSON-RPC handlers is Sub-team 1's responsibility; this folder is the client half only.

## Test double usage

```csharp
await using var gw = new FakeGateway();
await gw.ConnectAsync();
gw.SetResponse("file.open", new { datasetId = "ds-1" });

var result = await gw.SendAsync<OpenResult>("file.open", new { path = "m51.fits" });
Assert.That(gw.Sent.Single().Method, Is.EqualTo("file.open"));
```
