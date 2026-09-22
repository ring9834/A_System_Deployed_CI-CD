# 0013: TanStack Query for server state; React state/Context for UI state; no Redux

## Context
Q5 (modifiability — add a field end-to-end, timed, without AI) is directly shaped by how
state is organised: the fewer moving parts to touch for a change, the faster and more
explainable that drill is.

App state splits into two kinds:
- **Server state** — the todos themselves: needs fetching, caching, staleness handling,
  invalidation after writes, loading/error states.
- **UI state** — which modal is open, the active filter: local, small, short-lived.

## Options
1. **Redux (or another global store)** for all state, server and UI alike.
2. **TanStack Query for server state** (fetching/caching/invalidation) **+ `useState`/Context
   for UI state**.

## Decision
Option 2.

## Rationale
- Server state isn't really "application state" — it's a cache of the API's data, with its
  own well-defined problems (staleness, refetch, optimistic updates) that TanStack Query
  solves directly, without hand-written reducers or action types.
- Remaining UI state (open/closed modal, selected filter) is small and local enough that
  React's built-in state is sufficient; introducing Redux for it would add boilerplate
  (actions, reducers, store wiring) disproportionate to the actual state complexity — the
  same "machinery exceeding the problem" pattern rejected in ADR-0001 (microservices) and
  ADR-0009 (APIM).
- Lower risk to reverse than most ADRs here — noted explicitly: this is recorded for
  defensibility and consistency (Q5), not because a wrong call would be costly.

## Consequences
- Front-end data caching (an earlier open question) is resolved by this decision: TanStack
  Query's in-memory cache *is* the front-end caching strategy. No separate front-end caching
  ADR is needed.
- Static asset caching (cache-control headers, CDN) is a distinct, deployment-layer concern —
  tracked in `production-readiness.md` under Deployability, addressed in Step 6, not here.
