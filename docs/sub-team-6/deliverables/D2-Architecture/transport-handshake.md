# Transport-contract handshake status — Sub-team 1 ↔ Sub-team 6 (ADR-0002)

> **Audit item F15.** Records the *real* status of Sub-team 1's agreement to our
> JSON-RPC wire contract (ADR-0002 — `IServiceGateway` seam, method catalogue,
> length-prefixed framing). Written so the claim on `T5-pitch/pitch-spine.md`
> Slide 2.4 and `qa-practice.md` Q6.3 / Q6.8 is backed by a fact, not a guess, and
> so we never assert a sign-off that did not occur.

## Status (as of 2026-05-28, Day 9)

**Informal verbal agreement — not minuted, scope not pinned. Formal sign-off still owed.**

- **Sean Lynch (Sub-team 1 / Architecture)** gave a verbal nod to the client–server
  transport *direction*: JSON-RPC 2.0 over named pipes for local mode, with
  `IServiceGateway` as the transport-agnostic seam.
- The exact **date/venue was not minuted**, and the **precise scope** of what was
  agreed was **not recorded**. In particular it is **not** established that Sub-team 1
  ratified the full JSON-RPC **method catalogue** (`session.hello`, `file.open`,
  `file.close`, `dataset.getAxes`, `log.subscribe`, `log.emit`, `progress.update`),
  the framing, or the error model. **That catalogue was drafted unilaterally by
  Sub-team 6.**

## What this means for the pitch

- **ADR-0002 stays at `Status: proposed`.** We have an informal verbal nod, not a
  documented ratification — so we do not upgrade it to `accepted`.
- We treat the wire contract as **proposed; formal Sub-team-1 sign-off owed**, tracked
  on the team-wide integration risk register
  (`docs/team-alpha/integration-risk-register.md`) as:
  - **R01** — service-gateway contract not frozen — **Open**.
  - **DEPS-1** — Sub-team 1 ratifies or replaces the `IServiceGateway` stub — **Open**.
- **Mitigation (unchanged, cheap):** our seam to Sub-team 1 is a single adapter
  (`JsonRpcServiceGateway`). If their ratified shape differs from our draft catalogue,
  we rewrite the adapter, **not** the ViewModels.

## Owed action (not ours to close)

The integration risk register moved to **Sub-team 1's Integration Lead** at Day 2 EOD
(2026-05-20), so R01 / DEPS-1 status edits go through them. Owed: a **minuted
ratification** of the method catalogue + framing (or a documented replacement), after
which ADR-0002 can move `proposed → accepted`. Until then the panel-facing position is
honest: *direction verbally agreed, wire spec proposed and unsigned.*
