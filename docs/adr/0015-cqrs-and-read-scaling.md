# 0015: CQRS not adopted; staged read-scaling ladder with explicit triggers instead

## Context
Raised: as the number of gardeners (users) grows significantly or spikes, should read and
write paths be separated via CQRS to scale independently? Evaluated against the same
right-sizing principle already applied in ADR-0001 (no microservices), ADR-0009 (no APIM),
ADR-0011 (no cache yet), and ADR-0013 (no Redux) — is this the cheapest mechanism that
solves the actual problem, not just a plausible-sounding pattern.

## What CQRS solves, and what it costs
CQRS earns its cost when reads and writes have **structurally different shapes** — e.g.
writes are simple single-aggregate updates, but reads need denormalized, cross-aggregate, or
reporting-style views the write model can't serve well. The cost is real: a projection
mechanism to keep a separate read model in sync (often event-driven), eventual consistency to
reason about and explain, and a second model to build and maintain.

## Does this project's read/write shape need it?
No, even at higher scale. Each gardener only ever reads their own todos, filtered by state,
paginated (Q3) — a uniform, simple shape on both sides. More gardeners means more instances
of the *same* simple query pattern, not a *different* query shape. There is no reporting
dashboard, cross-user search, or analytics view anywhere in the brief that would need a read
model structurally different from the write model. CQRS's actual trigger — read/write shape
divergence — is not present.

## Decision
Reject full CQRS for the foreseeable scope. Instead, a staged scaling ladder, each stage
triggered by evidence, not assumption:

| Stage | Addresses | Mechanism | Trigger |
|---|---|---|---|
| 0 (built) | More concurrent users | Stateless API scales horizontally (ADR-0002/0007), composite index (design.md) | N/A — already in place |
| 1 (deferred, ADR-0011) | Read-heavy hot paths | Redis cache | Sustained high read:write ratio / p95 approaching target on the list endpoint |
| 2 (deferred, this ADR) | DB read throughput becomes the bottleneck even with caching | PostgreSQL read replica(s); route list/get queries to a replica, writes stay on the primary — physical read/write separation, same schema | Cache hit rate high but DB read load still saturating the primary |
| 3 (deferred, this ADR) | Read *shape* structurally diverges from write shape | Full CQRS: separate read model, possibly separate store, projections | A genuine reporting/analytics/cross-user-search requirement actually appears — not before |

## Rationale
- Stage 2 (read replica) captures most of CQRS's practical benefit for this domain — reads
  scaled independently of writes — via physical separation (same schema, different
  connection) rather than logical separation (a second model plus sync mechanism). Far
  cheaper to build, explain, and get right under interview time pressure.
- Stage 3 is not "never" — it's honestly deferred with a concrete, checkable trigger
  (structural read/write divergence), the same discipline as ADR-0011's caching trigger.

## Consequences
- No new infrastructure or model is built now.
- If asked "how would this scale to many more users," the answer is the ladder above, not a
  single "we'd add CQRS" — demonstrates evidence-based scaling judgment rather than reaching
  for a familiar pattern.
