# 0020: Environment strategy — local + one cloud "dev" environment; no separate test/staging/prod

## Context
The traditional dev/test/staging/prod split exists to solve three specific risks: isolating
individual work from teammates, verifying changes before real users see them, and protecting
real production data/traffic from untested code. Checked against what actually exists here:

| Traditional tier | Purpose | Applies to this project? |
|---|---|---|
| Dev (per-developer) | Isolate work from teammates | No — solo project |
| Test/QA (shared) | Stable target for the test suite | Superseded — Testcontainers gives a fresh, isolated PostgreSQL per CI run (ADR-0016), stronger than a persistent shared environment |
| Staging (prod-like) | Verify before real users see it | No real users exist |
| Prod (real users) | Serve real traffic with guarantees | No go-live exists in this context |

There is also concrete, already-experienced evidence duplicating environments would be
costly here specifically: this trial subscription has already shown real quota/SKU
restrictions (an AKS node SKU rejected outright; Postgres SKUs needing verification before
apply). Multiplying every resource (AKS, node pool, Postgres, Key Vault, ACR) by three or
four environments directly multiplies an already-demonstrated scarcity problem.

## Options
1. Full dev/test/staging/prod (four cloud environments).
2. Dev + staging + prod (three).
3. Local (Docker Compose) + one cloud environment ("dev").

## Decision
Option 3.

## Rationale
- Three of the four classic reasons for environment separation don't apply here (table
  above) — building the full split would be the same shape of over-engineering already
  rejected for CQRS (ADR-0015) and APIM (ADR-0009): real infrastructure cost solving a
  problem this project doesn't have.
- The one cloud environment necessarily serves two purposes at once — proving a real cloud
  deploy works, and being what's actually demonstrated live to interviewers. This is stated
  honestly here rather than dressed up as a prod-parity staging environment with real scale
  testing, which it is not.
- The single-cloud-environment risk (a bad deploy visible right when it matters) is already
  mitigated more cheaply than a duplicate cluster would mitigate it: `helm upgrade --atomic`
  auto-rolls back a failed release, the CI smoke test gates success before anything is
  considered deployed, and the local Compose stack is a complete, independent fallback if the
  cloud environment has any issue on interview day.

## Consequences
- Terraform's `var.environment` staying at `"dev"`, and the GitHub Environment named `dev`
  with its approval gate, are both already correctly scoped — no change needed, just this
  explicit reasoning recorded rather than left implicit.
- If this project ever had a genuine go-live with real users, that would be the concrete
  trigger to introduce a real staging/prod split — not assumed pre-emptively here.
