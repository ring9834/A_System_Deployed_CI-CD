# Architecture

## Drivers
- **Functional:** CRUD on todos; state transitions (todo/scheduled/done); owner-scoped access (A4).
- **Quality (from `requirements.md`), high-priority:** Q1 (owner isolation), Q2 (correct state transitions), Q7 (stateless API / scalability).
- **Quality, secondary:** Q3 (pagination performance), Q4 (zero-downtime deploy), Q6 (concurrency safety).
- **Hard constraints (from the brief — not my choice):** frontend is React, React Native, or Flutter; backend is .NET, Java, Node.js, Python, or Rails; API style is unspecified; cloud is AWS or Azure; Terraform, Kubernetes, Helm are fixed either way.
- **Chosen within those constraints, now justified (see ADR-0006):** React web (not RN/Flutter), .NET/C# with controllers, Azure (not AWS).
- **Decisions the brief leaves fully open, each with alternatives considered:** layering (ADR-0003), authentication mechanism (ADR-0007) and its transport (ADR-0002), database engine (ADR-0008), migrations strategy (ADR-0004), secrets (ADR-0005), API gateway/ingress layer (ADR-0009), asynchronous processing (ADR-0010), caching (ADR-0011).

## C4 — Context
```mermaid
C4Context
  Person(user, "Gardener (user)", "Creates and manages todos with a location")
  System(app, "Todo Planner", "Tracks todo items with state and location")
  System_Ext(azuredb, "Azure Database for PostgreSQL", "Managed data store")
  Rel(user, app, "Uses", "HTTPS")
  Rel(app, azuredb, "Reads/writes", "TCP/TLS")
```

## C4 — Container
```mermaid
C4Container
  Person(user, "Gardener")
  Container_Boundary(c1, "Todo Planner") {
    Container(web, "Web SPA", "Next.js, React, TypeScript", "Todo UI")
    Container(api, "API", ".NET, ASP.NET Core Controllers", "Stateless CRUD + state-machine API")
  }
  ContainerDb(db, "Database", "PostgreSQL", "Todos, owned per user")
  Rel(user, web, "HTTPS")
  Rel(web, api, "HTTPS, same-origin via ingress path routing (/api/*)", "JSON, JWT in httpOnly cookie")
  Rel(api, db, "TCP/TLS via Npgsql")
```

## ADR reading order (dependencies)
ADR numbers reflect the order decisions were made, not their logical dependency — a few
later ADRs justify assumptions made in earlier ones. Read in this order if new to the repo:
**0006 → 0012 → 0013** (stack/rendering/state) → **0008 → 0004** (database engine, then
hosting/migrations) → **0007 → 0002** (auth mechanism, then transport) → **0001, 0003, 0005,
0009, 0010, 0011** (independent). Each affected ADR carries a "Related ADRs" note pointing
the right direction.

## Key decisions (see ADRs for full reasoning)
| Decision | Resolves | ADR |
|---|---|---|
| Modular monolith, not microservices | Right-sized complexity | [0001](adr/0001-modular-monolith.md) |
| Stateless API; JWT in httpOnly cookie; same-origin via ingress | Q1, Q7 | [0002](adr/0002-stateless-api-and-auth-transport.md) |
| Api -> Application -> Domain -> Infrastructure layering | Q2 | [0003](adr/0003-domain-layering.md) |
| PostgreSQL on Azure Database Flexible Server; EF Core migrations as Helm pre-upgrade hook | Q4, Q6 | [0004](adr/0004-database-and-migrations.md) |
| Kubernetes Secrets sourced from Terraform/Key Vault (MVP); CSI driver deferred | Baseline security, honest scope | [0005](adr/0005-secrets-management.md) |
| React web + .NET + Azure chosen within brief's allowed options | Right-sized stack, defensible choice | [0006](adr/0006-technology-stack-selection.md) |
| JWT (self-issued) as auth mechanism, not Basic Auth or server-side sessions | Q1, Q7 (mechanism, distinct from transport) | [0007](adr/0007-authentication-mechanism.md) |
| PostgreSQL over Azure SQL / CosmosDB | Right-sized, portable data layer | [0008](adr/0008-database-selection.md) |
| Plain ingress for MVP; App Gateway deferred; APIM rejected | Right-sized edge layer | [0009](adr/0009-api-gateway-vs-ingress.md) |
| No message queue for MVP; async/await used throughout regardless | Right-sized, evidence-based | [0010](adr/0010-asynchronous-processing.md) |
| No caching for MVP; Redis if/when triggered | Q7 (consistent across replicas), right-sized | [0011](adr/0011-caching-strategy.md) |
| CSR via Next.js for all authenticated views; no SSR | Consistency with ADR-0002 (auth transport) | [0012](adr/0012-frontend-rendering-strategy.md) |
| TanStack Query for server state; React state/Context for UI; no Redux | Q5 (modifiability), right-sized | [0013](adr/0013-frontend-state-management.md) |

## ATAM-lite: risks, sensitivity points, trade-offs
| # | Type | Item | Detail | Disposition |
|---|---|---|---|---|
| R1 | Risk | Self-issued JWT (A6), no real IdP | Acceptable for assessment scope; a real product would need proper identity provider integration | Deferred, documented in backlog |
| R2 | Risk | Single-instance PostgreSQL (no read replica / failover configured) | Acceptable for assessment scope; production would need HA config | Deferred, documented |
| S1 | Sensitivity point | Auth transport choice (cookie vs bearer) | Changes CORS posture, XSS exposure, and ingress design all at once | Decided deliberately (ADR-0002), not left implicit |
| S2 | Sensitivity point | Layering boundary (Domain has no framework deps) | If violated, Q2 (transition tests) stop being fast, isolated unit tests | Enforced by convention + code review; could add an architecture test later |
| S3 | Sensitivity point | Security is prioritised H/H despite a "toy" domain | Location data is a customer's physical address, not abstract data; owner-isolation is cheap to build in now, expensive to retrofit once queries assume single-tenant | Built into MVP scope (Q1), not deferred |
| T1 | Trade-off | Modular monolith vs microservices | Gains: simplicity, one deploy unit, easy transactions. Costs: no independent scaling of parts (acceptable — todo API has no part that needs it) | Modular monolith chosen (ADR-0001) |
| T2 | Trade-off | Kubernetes Secrets (MVP) vs Key Vault CSI driver | Gains: less setup time now. Costs: secrets live in etcd, less rotation-friendly | Accepted for MVP; CSI driver listed as deferred, not silently skipped |
| N1 | Non-risk | Stateless API impacting horizontal scaling | ASP.NET Core with no in-process session state is close to framework default; low implementation risk | Confirmed via ADR-0002 + `StatelessApiTests` |
