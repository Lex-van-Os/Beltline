---
status: "accepted"
date: 2026-10-19
decision-makers: [Lex van Os]
---

# Use a forward-only status with full event history for out-of-order and invalid input

> Recorded retrospectively on 2026-11-18. The decision was made on 2026-10-19 during the initial
> design and refined on 2026-11-14 (explicit outcomes, issue #13).

## Context and Problem Statement

Beltline tracks each `BagLeg` (one bag on one flight) through a lifecycle of statuses, driven by
facts that the simulated world reports over a message broker. Messaging is at-least-once and
messages can be delayed, so facts arrive late, more than once, or out of order. Some facts are
also invalid for the bag's current state: a scan for a bag that has already departed, or a
loading scan for a bag that is not authorized for loading.

How should the domain handle a fact that does not fit the bag's current status, without losing
information and without ever moving a bag backwards?

## Decision Drivers

* No lost events: every fact that reaches the system must leave a trace.
* Correct order: a late or repeated fact must never move a bag back to an earlier status.
* Every bag reaches a definitive final state, and final statuses never change.
* No unauthorized bag may end up on board (IATA 753): invalid transitions must be visible, not
  silently dropped.
* A missed scan must not block a bag that is physically moving forward.
* Processing must not stall: one unexpected fact must not block the messages behind it.
* Keep the implementation proportionate for v1.

## Considered Options

* Reject invalid facts (fail the message)
* Park out-of-order facts and retry them later
* Forward-only status with full event history

## Decision Outcome

Chosen option: "Forward-only status with full event history", because it is the only option that
keeps every fact (no lost events) while guaranteeing that a bag never moves backwards, and it does
so without blocking or retrying messages.

The rules:

* A fact is mapped to a target status. It is applied only when that status is later in an explicit
  happy-path order (not the enum order). Skipping forward is allowed, so a missed scan does not
  block a bag, except that `Loading` can only be reached from `LoadAuthorized`.
* Final statuses (`Departed`, `Offloaded`, `Missed`, `Missing`) never change.
* Every fact is recorded as an immutable `BagLegHistoryEntry` on the `BagLeg`, whether or not it
  changed the status.
* Each entry carries an explicit outcome and, when not applied, a reason:
  * `Applied`: the status moved forward.
  * `Ignored`: harmless and expected, e.g. an earlier or repeated fact, or a fact that is not
    supported yet.
  * `Refused`: not allowed, e.g. the bag is final or off the happy path, or it is not authorized
    for loading.
* Every reason belongs to exactly one outcome, so an entry can never contradict itself.
* Caller bugs, such as a flight that does not match the bag, are not outcomes: they throw.

### Consequences

* Good, because no fact is ever lost: ignored and refused facts remain visible to operators and for
  debugging.
* Good, because out-of-order and duplicate delivery need no special infrastructure: the status
  simply does not move back.
* Good, because security-relevant situations (an unauthorized loading scan) stand out as `Refused`
  instead of disappearing.
* Good, because the history gives passengers a timeline and operators an audit trail.
* Good, because a single unexpected fact never blocks processing of the next messages.
* Bad, because the history grows with every fact, including repeated ones. This is mitigated by
  the inbox (true duplicate messages are filtered before the domain) and by debouncing scans in the
  simulation. Aggregation can be added later if needed.
* Bad, because a fact that arrives "too early" (before an earlier one) is not applied later when
  the earlier one arrives; the bag simply moves forward with the next later fact.
* Neutral, because deciding how much history a user sees (per role) becomes a read-side concern.

### Confirmation

* Unit tests in `Beltline.BagTracking.Domain.Tests` cover each outcome and reason, that ignored and
  refused facts leave status and version unchanged but still add a history entry, and that final
  statuses never change.
* Code review: every `Apply` path in `BagLegStateMachine` appends exactly one history entry per
  affected bag, and status changes only happen through the state machine.

## Pros and Cons of the Options

### Reject invalid facts (fail the message)

The domain throws on any fact that does not fit; the message goes to retries and then an error
queue.

* Good, because it is simple and invalid input is very visible.
* Bad, because late and duplicate facts are normal with at-least-once messaging, so harmless
  messages would end up in the error queue.
* Bad, because retries cannot fix an out-of-order fact; it fails again and again.
* Bad, because the fact is not part of the bag's history.

### Park out-of-order facts and retry them later

Facts that arrive too early are stored and re-applied once the missing earlier fact arrives.

* Good, because facts are eventually applied in the right order.
* Bad, because it needs storage, timeouts and retry logic, and a missed scan (which never arrives)
  would park later facts forever unless a timeout releases them.
* Bad, because it delays the status the passenger and operator see.
* Bad, because the added complexity is out of proportion for v1.

### Forward-only status with full event history

See "Decision Outcome".

* Good, because it meets all decision drivers.
* Neutral, because the history needs storage, which the domain database provides anyway.
* Bad, because the history grows with every fact (see "Consequences").

## More Information

* Lifecycle, outcomes and reasons: [`docs/domain/bag-lifecycle.md`](../../domain/bag-lifecycle.md)
* Introduced in code: #7 (forward-only transitions), #8 (loading only from `LoadAuthorized`), #13
  (history and explicit outcomes).
* Revisit when processing time is added to history entries, or if the volume of history per bag
  becomes a problem.
