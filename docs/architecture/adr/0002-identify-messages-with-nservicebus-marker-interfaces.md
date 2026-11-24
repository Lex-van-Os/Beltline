---
status: "accepted"
date: 2026-11-24
decision-makers: [Lex van Os]
---

# Identify messages with the NServiceBus marker interfaces

## Context and Problem Statement

The message contracts (commands and events) live in `Beltline.Contracts`, a class library shared by
every endpoint: the simulation sends and publishes them, BagTracking handles them, and later
components follow. NServiceBus has to know which types are commands and which are events: commands
are sent to one endpoint, events are published to every subscriber, and NServiceBus validates that
each is used the right way.

The initial design (2026-10-21) chose unobtrusive conventions on namespaces
(`Beltline.Contracts.Commands`, `Beltline.Contracts.Events`), to keep the contracts free of any
NServiceBus dependency. While building the first endpoints (#16), it turned out that since
NServiceBus 8, Particular recommends marker interfaces for new contracts, and ships them in a
separate, stable package.

How should the endpoints recognise which contract types are commands and which are events?

## Decision Drivers

* The kind of a message (command or event) is visible where the message is defined.
* A mistake shows up early, not as a message that is silently not recognised at runtime.
* The contracts can be shared by every endpoint without coupling them to a version of the NServiceBus
  core package.
* As little messaging configuration as possible in each endpoint, so sender and receiver cannot
  disagree.
* Follow the vendor's current guidance where it fits.

## Considered Options

* Marker interfaces from the `NServiceBus.MessageInterfaces` package
* Unobtrusive conventions on namespaces
* Marker interfaces from the NServiceBus core package

## Decision Outcome

Chosen option: "Marker interfaces from the `NServiceBus.MessageInterfaces` package", because it makes
the kind of each message explicit in its definition, needs no configuration in the endpoints, and
does so without a dependency on the NServiceBus core package.

The rules:

* Every command implements `ICommand`, every event implements `IEvent`.
* `Beltline.Contracts` references only `NServiceBus.MessageInterfaces`, never the NServiceBus core
  package.
* The `Commands` and `Events` folders and namespaces stay, for readability. They no longer decide
  anything.
* Contracts do not reference the domain: they have their own types (for example `Checkpoint`), which
  the receiving endpoint translates.

### Consequences

* Good, because `record RegisterBag : ICommand` states what the message is; moving a type to another
  namespace cannot change or break how it is handled.
* Good, because the endpoints need no message conventions, which keeps the shared NServiceBus setup
  smaller.
* Good, because `NServiceBus.MessageInterfaces` targets `netstandard2.0` and has a stable version
  (1.0.0), so the contracts are not tied to the NServiceBus core version of any endpoint.
* Bad, because the contracts now depend on a package from Particular. Replacing NServiceBus with
  another messaging library would mean removing the interfaces from every contract. This is a
  small, mechanical change, and replacing NServiceBus is not expected.
* Neutral, because issue #16's criterion "no NServiceBus package reference" becomes "no reference to
  the NServiceBus core package".

### Confirmation

* `Beltline.Contracts.csproj` references `NServiceBus.MessageInterfaces` and no other NServiceBus
  package.
* Code review: every type in `Beltline.Contracts.Commands` implements `ICommand`, every type in
  `Beltline.Contracts.Events` implements `IEvent`, and no endpoint configures message conventions.
  This can later become an automated architecture test.

## Pros and Cons of the Options

### Marker interfaces from the `NServiceBus.MessageInterfaces` package

A package that contains only `ICommand`, `IEvent` and `IMessage`.

* Good, because the kind of message is explicit and checked by the compiler.
* Good, because no conventions have to be configured, so endpoints cannot disagree about them.
* Good, because it is Particular's recommendation for new contracts since NServiceBus 8.
* Good, because the package is stable and avoids a dependency on the core package.
* Bad, because the contracts depend on a (small) Particular package.

### Unobtrusive conventions on namespaces

Each endpoint configures that types in `.Commands` are commands and types in `.Events` are events.

* Good, because the contracts have no dependencies at all.
* Bad, because a type in the wrong namespace compiles fine and is only rejected at runtime.
* Bad, because every endpoint must configure identical conventions (mitigated by a shared setup).
* Bad, because Particular positions it for existing types that cannot implement the interfaces, not
  for new contracts.

### Marker interfaces from the NServiceBus core package

* Good, because the kind of message is explicit.
* Bad, because the contracts depend on the full core package, which couples every endpoint that
  shares them to the same NServiceBus major version.

## More Information

* Particular documentation:
  [messages, events and commands](https://docs.particular.net/nservicebus/messaging/messages-events-commands)
  and [unobtrusive mode](https://docs.particular.net/nservicebus/messaging/unobtrusive-mode).
* This replaces the namespace conventions from the initial technology design (2026-10-21), which were
  never recorded as an ADR.
* Related: the planned ADR "Use NServiceBus on RabbitMQ" (#16).
