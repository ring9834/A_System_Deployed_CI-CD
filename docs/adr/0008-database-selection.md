# 0008: Database — PostgreSQL, not Azure SQL/SQL Server or CosmosDB

## Context
The brief specifies no database. Data shape: todos with a title, an enum state, a location,
owned by a user — small, fixed-shape, relational.

## Options
1. **Azure SQL / SQL Server**
2. **CosmosDB** (or any document/NoSQL store)
3. **PostgreSQL**

## Decision
Option 3: PostgreSQL, hosted as Azure Database for PostgreSQL Flexible Server (see ADR-0004
for migration/hosting detail).

## Rationale
- **CosmosDB (2)** rejected: its strengths (flexible/evolving schema, global distribution,
  very high write throughput) solve problems this app doesn't have. Using it would be the
  data-layer equivalent of the microservices mistake rejected in ADR-0001 — complexity with
  no corresponding requirement.
- **Azure SQL (1)** is a reasonable option, rejected in favour of Postgres mainly for
  portability: the brief allows AWS *or* Azure, and Postgres runs identically on either
  (or on-prem), avoiding the closer Azure/licensing coupling of SQL Server.
- **PostgreSQL (3)** chosen: no per-core licensing, strong EF Core support via Npgsql, and
  provides the `xmin` system column used for optimistic concurrency in ADR-0004 (resolving
  Q6) at no extra cost.

## Consequences
- Portable to AWS (RDS for PostgreSQL) with no application-code change, only Terraform module
  changes.
