# Vertical Slice Architecture

## Table of Contents

- [What Is Vertical Slice Architecture?](#what-is-vertical-slice-architecture)
- [Origin](#origin)
- [VSA vs. Layered / Clean / Onion Architecture](#vsa-vs-layered--clean--onion-architecture)
- [Core Principles](#core-principles)
- [How This Repo Applies VSA](#how-this-repo-applies-vsa)
- [Shared Code: What Belongs There](#shared-code-what-belongs-there)
- [DDD and Vertical Slice Architecture](#ddd-and-vertical-slice-architecture)
- [VSA and Clean Architecture Together](#vsa-and-clean-architecture-together)
- [Benefits](#benefits)
- [Challenges](#challenges)
- [When VSA Fits — and When It Doesn't](#when-vsa-fits--and-when-it-doesnt)
- [References](#references)

## What Is Vertical Slice Architecture?

Vertical Slice Architecture (VSA) organizes code around **business use cases (features)** instead of **technical layers**.

A traditional "N-layer" codebase groups files horizontally — every controller lives in `Controllers/`, every service in `Services/`, every repository in `Repositories/`. To understand or change one feature, you jump across every layer, touching a little bit of each folder.

VSA cuts the other way. Each use case — "create an order," "get an order by id," "cancel an order" — gets its own folder containing **everything that use case needs**: its request/response model, validation, business logic, and (if applicable) its HTTP endpoint. That folder is the "slice." Slices are stacked side by side, each one a full vertical cut through the system, from the wire to the domain — hence *vertical*, as opposed to the *horizontal* layers of a traditional architecture.

The guiding heuristic, often attributed to Jimmy Bogard: **"minimize coupling between slices, and maximize coupling in a slice."** Code that changes together lives together; code that rarely changes together is allowed to be duplicated rather than prematurely shared.

## Origin

The term **"Vertical Slice Architecture"** was coined and popularized by **Jimmy Bogard** (creator of [MediatR](https://github.com/jbogard/MediatR) and [AutoMapper](https://github.com/AutoMapper/AutoMapper)) in his December 2018 blog post *["Vertical Slice Architecture"](https://www.jimmybogard.com/vertical-slice-architecture/)*. Bogard had spent years advocating for CQRS (Command Query Responsibility Segregation) in .NET applications, and MediatR — a lightweight in-process mediator — became the natural building block for it: each command/query gets exactly one handler, and that pairing became the seed of a "slice."

VSA didn't appear out of nowhere. It formalizes and names a pattern that had been converging in the industry for a while:

- **"Package by feature, not layer"** — articulated by Dan North and others in the mid-2000s, arguing that organizing code by technical role scatters the concept of a single feature across the codebase.
- **"Screaming Architecture"** — Robert C. Martin's argument that a codebase's top-level structure should reveal what the system *does* (its use cases), not which framework or layering pattern it uses.
- **CQRS** — separating reads from writes, which naturally produces two different code paths per entity instead of one shared, layered pipeline.

Bogard's contribution was tying these ideas together into a concrete, named architecture for line-of-business apps, paired with a pragmatic implementation recipe (MediatR handlers, one per feature) that spread quickly through the .NET community via his blog, conference talks, and the ubiquity of MediatR itself.

## VSA vs. Layered / Clean / Onion Architecture

| | Layered / Clean / Onion | Vertical Slice |
|---|---|---|
| **Organized by** | Technical concern (Controllers, Services, Repositories, Domain) | Business use case (CreateOrder, CancelOrder, GetOrder) |
| **Change locality** | One feature change touches every layer | One feature change touches one folder |
| **Reuse strategy** | Shared services/base classes by default | Duplication allowed by default; extract shared code only once a real, stable pattern emerges |
| **Where coupling lives** | Across layers (interfaces bind them together) | Across slices is minimized; within a slice, coupling is fine |
| **Typical entry point** | A "fat" controller or service with many responsibilities | A small, single-purpose handler per use case |

VSA isn't necessarily opposed to Clean/Onion Architecture's *dependency rule* (domain has no outward dependencies) — it's opposed to the *folder-by-layer* convention. You can (and this repo does) still keep a shared `Domain/` and `Infrastructure/` for genuinely cross-cutting building blocks; the difference is that a feature is the primary unit of organization, not the layer.

## Core Principles

1. **One slice, one use case.** Everything a use case needs — request, validation, handler/business logic, endpoint mapping — lives in one folder.
2. **Minimize coupling *between* slices.** A slice should not reach into another slice's internals. Shared code goes into an explicitly shared, stable module (domain entities, cross-cutting infrastructure) — not into "helper" classes shared ad hoc between two features.
3. **Duplication over premature abstraction.** Two slices that look similar today are allowed to have similar-looking code. They may diverge tomorrow for reasons that have nothing to do with each other; sharing that code prematurely couples their futures together.
4. **The slice, not the layer, is the unit of change.** Adding a feature means adding a folder, not touching five existing files across five existing layers.
5. **Different triggers, same shape.** A slice doesn't have to be triggered by HTTP — a message-queue consumer, a scheduled job, or a background worker reacting to a domain event is still a vertical slice; only the trigger at the top of the slice changes.

## How This Repo Applies VSA

This repo demonstrates VSA using **ASP.NET Core Minimal APIs** and **MediatR**, split across two Bounded Contexts (`Order.Api` and `Payment.Api`), each its own process.

```
Features/
  CreateOrder/
    CreateOrderCommand.cs         ← the request
    CreateOrderCommandHandler.cs  ← the business logic
    CreateOrderValidator.cs       ← the validation rules
    CreateOrderEndpoint.cs        ← the HTTP endpoint mapping
  AddOrderItem/
  UpdateOrderStatus/
  PlaceOrder/
  CancelOrder/
  DeleteOrder/
  GetOrder/
  GetAllOrders/
  GetOrdersByCustomer/
  GetOrdersByCustomerAndStatus/
  GetOrdersByDateRange/
  HandlePaymentProcessed/         ← BackgroundService, consumes SQS — not an HTTP slice
```

Every slice implements a small `IEndpoint` interface (`Shared/IEndpoint.cs`):

```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

`Program.cs` scans the assembly at startup and calls `MapEndpoint` on every implementation it finds, so **adding a new slice never requires touching `Program.cs`** — no central route table to edit, no risk of merge conflicts between two developers adding unrelated features at the same time.

**Not every slice is an HTTP endpoint.** `HandlePaymentProcessed` (`Order.Api`) and `ProcessPayment` (`Payment.Api`) are `BackgroundService`s that long-poll an SQS queue and dispatch the same kind of MediatR command an HTTP handler would — same shape, different trigger. `Payment.Api` has no HTTP-triggered slices at all; it runs as a pure background worker.

**Where sharing does happen:** `Domain/` (entities, value objects, domain events, repository interfaces) and `Infrastructure/` (concrete repository implementations, the event bus) are shared *within* a Bounded Context, because every slice in that BC legitimately depends on the same domain model. Slices do not depend on each other.

Slices are further grouped by **Bounded Context** — `order/`, `payment/`, and (in a fuller system) `catalog/`, `customer/`, `notification/` — each ideally owned by one team with its own repository. (Payment is folded into this repo alongside Order purely for demonstration purposes.)

## Shared Code: What Belongs There

VSA's default is duplication, not sharing — but some code genuinely has to be shared, or every slice reinvents it. The question is *what kind* of code earns a place outside `Features/`, and this repo's `Domain/`, `Infrastructure/`, and `Shared/` folders draw that line in three different ways:

| Folder | What lives there | Why it's shared, not duplicated |
|---|---|---|
| `Domain/` | Aggregates/entities (`OrderAggregate`), value objects, domain events, repository **interfaces** (`IOrderRepository`) | Every slice that touches an order operates on the *same* aggregate and must respect the *same* invariants — this is the one thing slices are explicitly meant to share within a Bounded Context. |
| `Infrastructure/` | Concrete repository implementations (`DynamoDbOrderRepository`), event bus adapters | Expensive to build, doesn't vary per use case (persistence mechanics are the same whether you're creating or cancelling an order), and swapping it out should mean editing one class, not eleven. |
| `Shared/` | The `IEndpoint` self-registration mechanism, `ValidationBehavior` (a MediatR pipeline behavior), `GlobalExceptionHandler`, the `ApiResponse`/`IOperationResult` response envelope, DI registration extensions | Cross-cutting *architecture* concerns — validation, error handling, response shaping — that apply identically to every slice and have nothing to do with any one feature's business rules. |
| `Shared/Application/Dtos/` | Read-model DTOs and mappers (`OrderDto`, `OrderMapper`) | Query slices (`GetOrder`, `GetAllOrders`, `GetOrdersByCustomer`, …) all project the same aggregate into the same shape; duplicating that mapping five times buys nothing. |

A practical guideline for anything that doesn't obviously fall into one of these buckets: don't extract it the first time two slices look similar — wait for a **rule of three** (a genuinely identical third use) before generalizing. Two similar-looking validators or handlers today may diverge tomorrow for reasons specific to their own feature; sharing them prematurely re-couples slices that VSA was meant to keep independent.

The failure mode to watch for is `Shared/` (or a `Common/`/`Helpers/` folder like it) turning into a dumping ground for anything that doesn't obviously belong to one feature — at which point it *is* a horizontal layer again, just under a different name. Code review discipline, not tooling, is what keeps it from growing back into one.

## DDD and Vertical Slice Architecture

**Domain-Driven Design (DDD)** and VSA answer different questions and combine cleanly: DDD is about *how you model the business domain* (Bounded Contexts, ubiquitous language, aggregates, value objects, domain events); VSA is about *how you organize the code that operates on that model*. This repo leans on both at once:

- **Bounded Context ↔ grouping of slices.** `Order.Api` and `Payment.Api` are separate Bounded Contexts, each with its own domain model and its own set of slices. A slice never reaches across a BC boundary directly — cross-BC communication goes through domain events on SNS/SQS, not a shared repository or shared entity.
- **Aggregate ↔ the consistency boundary a slice operates within.** A slice's handler is thin on purpose: it loads (or creates) exactly one `OrderAggregate` via `IOrderRepository`, calls a domain method that enforces invariants (`PlaceOrder()`, `AddItem()`, `Cancel()`), and persists the result. The *business rule* — what makes a status transition valid, whether an item can still be added — lives on the aggregate, not in the handler.
- **Domain Events ↔ the seam between a slice and the rest of the system.** `OrderAggregate` raises events internally (`OrderCreatedEvent`, `OrderPlacedEvent`, `OrderCancelledEvent`, …) without knowing who, if anyone, is listening. The handler publishes them through `IEventBus` after persisting, which is what lets `HandlePaymentProcessed` react in a different process entirely without the `Order` slices ever referencing `Payment.Api`.
- **Repository ↔ one per aggregate, shared across every slice that needs it.** `IOrderRepository` is used by `CreateOrder`, `PlaceOrder`, `CancelOrder`, `GetOrder`, and more — one repository, many slices — because they all operate on the same aggregate, not because "data access" is a horizontal layer.

**The risk VSA introduces for DDD:** because a slice's handler has direct, easy access to persistence and infrastructure, it's tempting to skip the aggregate and put business logic straight into the handler ("just check the status here, it's faster"). That's how an anemic domain model creeps back in — VSA makes it *easy* to bypass the aggregate, it doesn't make it *wrong* to do so, so keeping "handlers orchestrate, aggregates decide" as a hard rule is a discipline the architecture doesn't enforce for you.

## VSA and Clean Architecture Together

VSA and Clean/Onion Architecture are often framed as alternatives, but they answer orthogonal questions: Clean Architecture is about **dependency direction** (the Dependency Rule — dependencies point inward, and nothing inside the domain depends on infrastructure or presentation); VSA is about **navigation and organization** (feature vs. layer). Nothing stops you from keeping the Dependency Rule *inside* every slice while dropping the "layer-first folder" convention — which is exactly what this repo does:

```
CreateOrderEndpoint          → outermost: presentation, maps HTTP → command
CreateOrderCommandHandler    → application/use-case: orchestrates, depends on abstractions only
CreateOrderValidator         → application: input rules, runs as a pipeline behavior before the handler
OrderAggregate (Domain/)     → innermost: entities/invariants, zero framework dependencies
IOrderRepository (Domain/)   → abstraction the application layer depends on (Dependency Inversion)
DynamoDbOrderRepository      → outermost: infrastructure, implements the Domain-owned interface
    (Infrastructure/)
```

The dependency arrows still point the way Clean Architecture demands: `CreateOrderCommandHandler` depends on `IOrderRepository`, never on `DynamoDbOrderRepository`; `OrderAggregate` depends on nothing outside `Domain/`. The difference from a "classic" Clean Architecture solution is purely physical: instead of four projects (`Domain`, `Application`, `Infrastructure`, `Api`) each containing a little bit of every feature, this repo keeps the same dependency direction but organizes the *application and presentation* layers by feature folder, and shares only `Domain/` and `Infrastructure/` across the whole Bounded Context. You get Clean Architecture's inward-pointing dependencies *and* VSA's one-feature-one-folder navigation, rather than choosing between them. (This blend is sometimes called "feature-folder Clean Architecture" in the .NET community; some Clean Architecture solution templates now ship a feature-folder variant for exactly this reason.)

## Benefits

- **Locality of change.** Understanding or modifying "cancel an order" means opening one folder, not tracing a call through five layers.
- **Low blast radius.** Changing one slice's internals can't silently break an unrelated slice, since they don't share code by default.
- **Parallel development.** Two developers building two different features rarely touch the same file, reducing merge conflicts — reinforced here by the self-registering `IEndpoint` pattern.
- **Easier onboarding.** A new developer can be productive by reading and copying one existing slice, without first learning the whole layered call graph.
- **Natural mapping to CQRS/event-driven design.** Every use case is already isolated, so adding messaging (queues, background workers) alongside HTTP is just another kind of slice, not an architectural detour.

## Challenges

- **Duplication can go too far.** "Prefer duplication over the wrong abstraction" is a guideline, not a law — teams that never revisit duplicated code end up with subtly-diverging copies of logic that should have been unified (e.g., validation rules for the same field, drifted across five handlers).
- **Cross-cutting concerns still need a home.** Logging, auth, transaction handling, and validation don't belong to any one slice, but also shouldn't be reimplemented per slice. VSA typically pushes these into MediatR pipeline behaviors or middleware rather than the slices themselves — but deciding what's "cross-cutting enough" to centralize is a judgment call, and getting it wrong recreates layered coupling through the back door.
- **No enforced boundary between slices.** Nothing stops a slice from reaching into another slice's handler or internals; discipline (and code review) has to substitute for a compiler-enforced rule.
- **Granularity is subjective.** Is "update order status" one slice, or is "place order" (which also updates status) a separate slice that overlaps with it? Different teams draw these lines differently, and inconsistent granularity makes a codebase harder to navigate over time.
- **Harder to see the "big picture."** Because there's no central service class listing every operation on an entity, understanding everything that can happen to an `Order` means listing folders under `Features/`, not reading one class.
- **Migration cost from legacy layered systems.** Retrofitting VSA onto an existing layered codebase usually means slicing through existing services and repositories, which is disruptive if attempted all at once — most teams introduce VSA only for new features and let it coexist with legacy code for a while.
- **Testing strategy needs adjustment.** Because a slice's handler is deliberately not decomposed into many small, independently-mockable services, teams often shift from unit-testing internal collaborators toward black-box/integration-style tests of the handler as a whole.

## When VSA Fits — and When It Doesn't

VSA earns its keep when a codebase has **many use cases whose logic genuinely differs** — an e-commerce order pipeline, a claims-processing system, anything with a long tail of commands and queries that each have their own validation and edge cases. The more those use cases diverge, the more a shared layered pipeline becomes a liability rather than a convenience.

It's a poor fit — or at least overkill — for:

- **Thin CRUD services** where every endpoint really is "validate, map, save," with no meaningful per-feature business logic. A generic repository/service pair is less code and less indirection than ten near-identical slices.
- **Small teams or small codebases**, where the coordination problem VSA solves (many developers, many features, minimizing merge conflicts and cross-feature coupling) doesn't really exist yet.
- **Systems whose core value *is* a shared, complex pipeline** (e.g., a single sophisticated pricing or tax-calculation engine used identically by every caller) — forcing that into "per slice" logic would duplicate the one thing that shouldn't be duplicated.

In practice, many teams don't choose exclusively — they apply VSA to the use-case-heavy parts of a system (order lifecycle, claims, workflow steps) and keep simpler, layered or CRUD-style code for the parts that are genuinely uniform.

## References

- Jimmy Bogard, [*Vertical Slice Architecture*](https://www.jimmybogard.com/vertical-slice-architecture/) (2018)
- Jimmy Bogard, [MediatR](https://github.com/jbogard/MediatR)
- Robert C. Martin, [*Screaming Architecture*](https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html)
- Bounded Context patterns: https://github.com/tung-le-lv/OpenMind.DDD.Patterns
