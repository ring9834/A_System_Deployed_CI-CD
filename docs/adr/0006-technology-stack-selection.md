# 0006: Technology stack selection (within brief-mandated options)

> **Related ADRs:** this ADR decides the coarse framework family (React web vs mobile). For
> the finer-grained rendering strategy within React web (CSR vs SSR, and why Next.js
> specifically), see [ADR-0012](0012-frontend-rendering-strategy.md).

## Context
The brief fixes a set of *allowed* options, not a single stack: frontend is React, React
Native, or Flutter; backend is .NET, Java, Node.js, Python, or Rails; cloud is AWS or Azure.
This ADR records the actual choice and why, so it isn't silently assumed.

## Options and decision
| Layer | Options (per brief) | Chosen | Why |
|---|---|---|---|
| Frontend | React (web), React Native, Flutter | React web (Next.js) | A web SPA is sufficient to demonstrate CRUD + state-machine UI; mobile (RN/Flutter) adds platform-specific build/deploy concerns (app store signing, device testing) with no corresponding requirement in the brief — same reasoning as rejecting microservices in ADR-0001: no problem here needs the extra surface |
| Backend | .NET, Java, Node.js, Python, Rails | .NET/C#, controllers | Existing proficiency (relevant given the live no-AI change requirement); strong typed EF Core + PostgreSQL support via Npgsql; brief explicitly allows candidate's own stack when not backend-specialised |
| Cloud | AWS, Azure | Azure | Arbitrary within the allowed set; AKS/Terraform/Helm patterns used here transfer near-identically to EKS if needed later — see ADR-0008 for why PostgreSQL was still chosen to avoid Azure-specific lock-in at the data layer |

## Consequences
- These choices are personal-preference-legitimate per the brief, but are now written down and
  defensible rather than assumed.
- Because Postgres (not Azure SQL) and REST (not a proprietary Azure API style) were chosen,
  moving the API and data layer to AWS later would require re-writing Terraform modules,
  not the application code.
