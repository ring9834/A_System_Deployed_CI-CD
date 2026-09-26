# 0023: Retry, timeout, and backoff — idempotency-aware client retries, transient-fault DB
# retry, no circuit breaker, health check depth flagged for Step 6

## Context
Retry/timeout/backoff decisions must respect idempotency (ADR-0022's gap) and optimistic
concurrency (ADR-0004, Q6) — a naive "retry on any failure" policy would be actively unsafe
for a non-idempotent create, and would misclassify a legitimate 409 conflict as a transient
fault worth retrying.

## Decisions

### Client (browser, TanStack Query)
Queries (`GET`) retry automatically with backoff — safe, no side effects.
Mutations do **not** auto-retry by default: `POST /todos` (create) is not idempotent, so an
automatic retry could produce a duplicate. `PUT`, `DELETE`, and `POST .../transition` are all
safely retryable in principle (idempotent by construction — see ADR-0022), but are left
non-retrying by default anyway, consistent with the library's own default and simplicity.
This is TanStack Query's existing default behavior — confirmed as correct here rather than
left as an unexamined default, the same treatment given to HTTP/REST itself in ADR-0022.

### API → PostgreSQL
Enable EF Core's `EnableRetryOnFailure()` transient-fault execution strategy, with an explicit,
short command timeout (not the multi-second default) given every query here is fast CRUD.
This handles a real, plausible scenario: a brief network blip or managed-server failover on
Azure Database for PostgreSQL Flexible Server — cheap to enable, genuine value.

**Critical boundary:** this retry strategy must never apply to a 409 from optimistic
concurrency (ADR-0004/Q6). A 409 means someone else already changed the row — that is an
application-level conflict to surface to the client, not a transient fault to paper over with
a retry.

### Circuit breaker
Not adopted. Circuit breakers protect against a degraded *external* downstream dependency —
per `architecture.md`'s system-boundary analysis, this application currently has zero external
dependencies; its only dependency is its own PostgreSQL, and if that is down there is nothing
to circuit-break, the API should fail fast with a clear error. Trigger for revisiting: if
geocoding (deferred) is built, a circuit breaker (e.g. via Polly) around *that specific call*
would be justified — not the API pre-emptively.

### Ingress timeouts
Default ingress-nginx proxy timeouts are left untouched. Justified directly by ADR-0022's
conclusion that every operation here is fast, synchronous request/response — there is nothing
long-running to tune a timeout around.

### Health check depth — flagged, not fixed here
The current `/health` endpoint (Step 4) checks only that the process is running, built before
the database existed. Whether the readiness probe should also verify real DB connectivity
(so a pod is marked `NotReady` and removed from routing before it fails user requests, rather
than accepting traffic it cannot serve) is a real, named gap — tracked as a Step 6 hardening
item, not resolved in this ADR.

## Consequences
- `docs/production-readiness.md`: add "EF Core transient-fault retry enabled" under
  Reliability, and "Readiness probe checks DB connectivity" as a Step 6 `todo` item under the
  same dimension.
- No new dependency required for the DB retry policy — it is built into EF Core.
