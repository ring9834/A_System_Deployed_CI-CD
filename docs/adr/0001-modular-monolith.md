# 0001: Modular monolith, not microservices

## Context
The brief asks for a todo CRUD API. The full domain (todos, state, location) is small and has
no part that needs independent scaling, independent deployment cadence, or a different tech
stack from the rest.

## Options
1. **Microservices** — split by resource/bounded context, each independently deployable.
2. **Modular monolith** — one deployable API with clear internal module/layer boundaries.
3. **Unstructured single project** — no enforced boundaries.

## Decision
Modular monolith (option 2): one ASP.NET Core project with enforced layering
(`Api -> Application -> Domain -> Infrastructure`, see ADR-0003), one deployable, one database.

## Consequences
- Simpler deployment, one Helm release, no cross-service network calls or distributed transactions.
- No independent scaling of parts — acceptable, since nothing in this domain needs it.
- If a genuine need for independent scaling emerges later (unlikely at this scope), the
  layering already in place makes future extraction easier than it would be from an
  unstructured project.
- Directly addresses the brief's instruction not to over-engineer relative to the actual problem.

## Quality scenario(s) addressed
Right-sizing / maintainability (supports fast iteration within the time box).
