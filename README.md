# Vertical Slice Architecture

## Table of Contents

- [What Is Vertical Slice Architecture?](#what-is-vertical-slice-architecture)
- [Core Principles](#core-principles)
- [Be careful with Anemic Domain Model](#be-careful-with-anemic-domain-model)
- [Challenges](#challenges)
- [References](#references)

## What Is Vertical Slice Architecture?

Vertical Slice Architecture (VSA) organizes code around **business use cases (features)** instead of **technical layers**.

A traditional "N-layer" codebase groups files horizontally — every controller lives in `Controllers/`, every service in `Services/`, every repository in `Repositories/`. To understand or change one feature, you jump across every layer, touching a little bit of each folder.

VSA cuts the other way. Each use case — "create an order," "get an order by id," "cancel an order" — gets its own folder containing **everything that use case needs**: its request/response model, validation, business logic, and (if applicable) its HTTP endpoint. That folder is the "slice." Slices are stacked side by side, each one a full vertical cut through the system, from the wire to the domain — hence *vertical*, as opposed to the *horizontal* layers of a traditional architecture.

The guiding heuristic, often attributed to Jimmy Bogard: **"minimize coupling between slices, and maximize coupling in a slice."** Code that changes together lives together; code that rarely changes together is allowed to be duplicated rather than prematurely shared.

The term **"Vertical Slice Architecture"** was coined and popularized by **Jimmy Bogard** (creator of [MediatR](https://github.com/jbogard/MediatR) and [AutoMapper](https://github.com/AutoMapper/AutoMapper)) in his December 2018 blog post *["Vertical Slice Architecture"](https://www.jimmybogard.com/vertical-slice-architecture/)*. Bogard had spent years advocating for CQRS (Command Query Responsibility Segregation) in .NET applications, and MediatR — a lightweight in-process mediator — became the natural building block for it: each command/query gets exactly one handler, and that pairing became the seed of a "slice."

VSA didn't appear out of nowhere. It formalizes and names a pattern that had been converging in the industry for a while:

- **"Package by feature, not layer"** — articulated by Dan North and others in the mid-2000s, arguing that organizing code by technical role scatters the concept of a single feature across the codebase.
- **"Screaming Architecture"** — Robert C. Martin's argument that a codebase's top-level structure should reveal what the system *does* (its use cases), not which framework or layering pattern it uses.
- **CQRS** — separating reads from writes, which naturally produces two different code paths per entity instead of one shared, layered pipeline.

VSA isn't necessarily opposed to Clean/Onion Architecture's *dependency rule* (domain has no outward dependencies) — it's opposed to the *folder-by-layer* convention. You can (and this repo does) still keep a shared `Domain/` and `Infrastructure/` for genuinely cross-cutting building blocks; the difference is that a feature is the primary unit of organization, not the layer.

## Core Principles

1. **One slice, one use case.** Everything a use case needs — request, validation, handler/business logic, endpoint mapping — lives in one folder.
2. **Minimize coupling *between* slices.** A slice should not reach into another slice's internals. Shared code goes into an explicitly shared, stable module (domain entities, cross-cutting infrastructure) — not into "helper" classes shared ad hoc between two features.
3. **Duplication over premature abstraction.** Two slices that look similar today are allowed to have similar-looking code. They may diverge tomorrow for reasons that have nothing to do with each other; sharing that code prematurely couples their futures together.
4. **The slice, not the layer, is the unit of change.** Adding a feature means adding a folder, not touching five existing files across five existing layers.
5. **Different triggers, same shape.** A slice doesn't have to be triggered by HTTP — a message-queue consumer, a scheduled job, or a background worker reacting to a domain event is still a vertical slice; only the trigger at the top of the slice changes.

## Be careful with Anemic Domain Model

Each slice owns its request handling logic end-to-end — the request, validation, data access, and response shaping all live together in the handler. At starting point, we can use transaction script. We don't even need Repository, just DbContext. That's fine because most CRUD-ish features never need more.  

When you notice the same business rule showing up in multiple handlers, you push behavior down into the entities. The domain model is shared across slices; the handlers are not. Entities aren't duplicated per slice. What's per-slice is everything above the domain: DTOs, validators, queries, mapping.

## Challenges

- **Duplication can go too far.** "Prefer duplication over the wrong abstraction" is a guideline, not a law — teams that never revisit duplicated code end up with subtly-diverging copies of logic that should have been unified (e.g., validation rules for the same field, drifted across five handlers).
- **Cross-cutting concerns still need a home.** Logging, auth, transaction handling, and validation don't belong to any one slice, but also shouldn't be reimplemented per slice. VSA typically pushes these into MediatR pipeline behaviors or middleware rather than the slices themselves — but deciding what's "cross-cutting enough" to centralize is a judgment call, and getting it wrong recreates layered coupling through the back door.
- **No enforced boundary between slices.** Nothing stops a slice from reaching into another slice's handler or internals; discipline (and code review) has to substitute for a compiler-enforced rule.
- **Granularity is subjective.** Is "update order status" one slice, or is "place order" (which also updates status) a separate slice that overlaps with it? Different teams draw these lines differently, and inconsistent granularity makes a codebase harder to navigate over time.
- **Harder to see the "big picture."** Because there's no central service class listing every operation on an entity, understanding everything that can happen to an `Order` means listing folders under `Features/`, not reading one class.
- **Migration cost from legacy layered systems.** Retrofitting VSA onto an existing layered codebase usually means slicing through existing services and repositories, which is disruptive if attempted all at once — most teams introduce VSA only for new features and let it coexist with legacy code for a while.
- **Testing strategy needs adjustment.** Because a slice's handler is deliberately not decomposed into many small, independently-mockable services, teams often shift from unit-testing internal collaborators toward black-box/integration-style tests of the handler as a whole.
- **Tends to pull toward an anemic domain model.** A handler sits right next to persistence with everything it needs in scope, which makes it the path of least resistance to write business logic directly in the handler instead of delegating to a rich entity/aggregate — see [Be careful with Anemic Domain Model](#be-careful-with-anemic-domain-model) for why this happens and how to mitigate it.

## References

- Jimmy Bogard, [*Vertical Slice Architecture*](https://www.jimmybogard.com/vertical-slice-architecture/) (2018)
- Jimmy Bogard, [MediatR](https://github.com/jbogard/MediatR)
- Robert C. Martin, [*Screaming Architecture*](https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html)
- Bounded Context patterns: https://github.com/tung-le-lv/OpenMind.DDD.Patterns
