# 0011: No caching for MVP; Redis is the deferred answer once triggered

> **Related ADRs:** this ADR covers backend/API-layer (Redis) caching only. Front-end data
> caching is resolved separately by TanStack Query — see
> [ADR-0013](0013-frontend-state-management.md); no additional front-end caching decision is
> needed.

## Context
Formalizes the caching item already in `requirements.md`'s Deferred backlog and Q7, with an
explicit trigger so "deferred" doesn't read as "forgotten."

## Decision
No cache for MVP. If/when introduced, use a distributed cache (Redis) — never an in-process
cache — because the API is stateless and horizontally replicated (ADR-0002/ADR-0007); an
in-process cache would give different replicas inconsistent views of the same data.

## Rationale
Caching is justified once the todo-list endpoint is demonstrably a hot, read-heavy path
relative to write volume. That isn't assumed true at this scale — asserting it without
evidence would be premature optimisation, and caching has a real cost (invalidation
correctness, another moving part to explain live in the interview).

## Trigger for revisiting
Sustained read:write ratio and/or p95 latency on the list endpoint (Q3) approaching its
target under realistic load — evidence-based, not assumed.

## Consequences
- Q3 (pagination performance) is met first through indexing (ADR already covers this via
  the `(OwnerId, State)` index), not caching — the cheaper fix first.
