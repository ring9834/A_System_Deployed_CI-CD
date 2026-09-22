# 0004: PostgreSQL on Azure Database Flexible Server; EF Core migrations as a Helm hook

> **Related ADRs:** this ADR assumes PostgreSQL as the engine and covers hosting/migration
> mechanics only. For *why PostgreSQL* over Azure SQL or CosmosDB, see
> [ADR-0008](0008-database-selection.md).

## Context
Q4 (zero-downtime rolling deploy) and Q6 (safe concurrent updates) both depend on how the
schema evolves and how migrations run relative to a deploy.

## Options
1. Manual migration step run by hand before deploying.
2. Migrations run automatically inside the API's startup path.
3. Migrations run as a separate Helm `pre-upgrade` hook Job, before the API Deployment rolls.

## Decision
Option 3. A dedicated Job runs `dotnet ef database update` as a Helm pre-upgrade hook.
Migrations are required to be backward-compatible (expand/contract: add nullable columns,
backfill, only remove/rename in a later release).
Concurrency uses Npgsql's `xmin` system column as an ETag/concurrency token (not
`rowversion`, which is SQL Server-specific).

## Consequences
- Migrations run exactly once per release, before new pods start, not racing multiple
  replicas trying to migrate concurrently (option 2's failure mode).
- Backward-compatibility requirement means a rolling deploy never has old-pod-vs-new-schema
  mismatches — directly satisfies Q4.
- `xmin`-based optimistic concurrency gives a real, testable 409 on conflicting writes (Q6).
- Slightly more Helm/chart complexity (a Job + hook annotations) than option 1 or 2, judged
  worth it given Q4 is an explicit scenario.

## Quality scenario(s) addressed
Q4 (availability during deploy), Q6 (concurrent update safety).
