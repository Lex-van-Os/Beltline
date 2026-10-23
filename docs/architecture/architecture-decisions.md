# Architecture decisions

This is the index of Beltline's architecture decisions. Each significant decision is recorded as a
single immutable file under [`adr/`](adr/), using the [MADR 4.0](https://adr.github.io/madr/)
format.

## What gets an ADR here

| Tier | Definition | Where it goes |
| --- | --- | --- |
| **ADR** | Architecturally significant: affects structure, cross-cutting qualities, dependencies, interfaces, or hosting/deployment topology, and is costly to reverse. | A numbered file in [`adr/`](adr/). |
| **Development / tooling decision** | Affects how we build, test, or run the project *locally* — not the deployed system. | The relevant `README.md` (e.g. "Prerequisites"), or folded into a related ADR as a consequence. |
| **Not recorded** | Reversible, obvious, needs no rationale. | Nowhere. |

## How to add one

1. Copy [`adr/0000-madr-template.md`](adr/0000-madr-template.md) to
   `adr/NNNN-short-title.md` (next free number).
2. Fill it in. Set `status: proposed` while it's under discussion, `accepted` once decided.
3. Never edit an accepted ADR's decision. If it changes, write a new ADR and set the old one's
   status to `superseded by ADR-NNNN`.
4. Add a row to the table below, and remove the decision from the planned list if it was there.

Decisions that were made before this index existed are recorded retrospectively: they use the
date the decision was actually made and state that they were recorded retrospectively.

## Accepted / proposed

| # | Title | Status | Date |
| --- | --- | --- | --- |
| | _None yet._ | | |

## Planned (decided, not yet written)

These decisions were made during the initial design (2026-10-16 to 2026-10-21) and still need to be
recorded as ADRs.

- Separate the simulated world from the tracking system (simulation as environment, BagTracking as
  system under test)
- Track status per `BagLeg` (bag × flight), not on the `Bag`
- Use a forward-only status with full event history for out-of-order and invalid input
- Make BagTracking the single writer of bag state; manual actions arrive as commands
- Use at-least-once messaging with the NServiceBus Outbox (deduplication + outbox), committing
  before acknowledging
- Use optimistic concurrency with a `BagLeg` version
- Let the simulation own time (simulated clock via `ClockTicked`)
- Detect late bags inside BagTracking instead of in a separate detector
- Use a separate passenger read side with its own read model and live updates via SignalR
- Use NServiceBus on RabbitMQ
- Use PostgreSQL
