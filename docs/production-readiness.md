# Production-readiness checklist

**Working definition:** a team could deploy this repeatedly, know when it breaks, roll it back,
trust it against baseline threats, and change it confidently.

**Status:** `todo` | `partial` | `done` | `deferred`. Every row needs evidence (a file, test, CI run or screenshot),
and every `partial` or `deferred` row needs a one-line reason. Update this file in the same PR that changes a row.

| Dimension | Item | Status | Evidence | Notes / reason |
|---|---|---|---|---|
| Correctness | State-machine unit tests (legal and illegal transitions) | todo | | |
| Correctness | API integration tests against real PostgreSQL | todo | | |
| Correctness | Frontend component tests (forms, loading/empty/error) | todo | | |
| Security | JWT validation configured (issuer, audience, expiry) | todo | | |
| Security | Owner isolation: user A cannot read/modify user B's todo (tested) | todo | | |
| Security | Server-side input validation | todo | | |
| Security | No secrets in git (secret scanning on) | todo | | |
| Security | Containers non-root, read-only root filesystem | todo | | |
| Security | Dependency + image vulnerability scanning in CI | todo | | |
| Security | TLS at ingress | todo | | |
| Reliability | Liveness / readiness / startup probes | todo | | |
| Reliability | >=2 replicas + PodDisruptionBudget | todo | | |
| Reliability | Resource requests and limits | todo | | |
| Reliability | Graceful shutdown | todo | | |
| Reliability | Backward-compatible migrations | todo | | |
| Observability | Structured logs with correlation ID | todo | | |
| Observability | Health endpoints | todo | | |
| Observability | Metrics / traces (OpenTelemetry) | todo | | |
| Deployability | CI on every PR (build, test, lint, scan) | todo | | |
| Deployability | Images tagged by commit SHA | todo | | |
| Deployability | Deploy with `helm upgrade --atomic` + smoke test | todo | | |
| Deployability | Rollback drill performed | todo | | |
| Deployability | Terraform remote state, no long-lived cloud secrets in CI (OIDC) | todo | | |
| Deployability | Static asset cache-control headers/CDN | todo | | |
| Data | Managed PostgreSQL with backups | todo | | |
| Data | Migrations tested; indexes match query patterns | todo | | |
| Maintainability | OpenAPI contract in repo; client types generated | todo | | |
| Maintainability | Lint / format enforced in CI | todo | | |
| Maintainability | ADRs for significant decisions | todo | | |
| Operability | README quickstart; cold start productive in < 5 min | todo | | |
| Operability | One-command local stack | todo | | |
| Operability | Runbook (crash loop, DB unreachable, bad deploy) | todo | | |
| Observability | Structured logs with correlation ID | done | | free via Activity/TraceIdentifier, no custom work |
| Observability | Metrics / traces (OpenTelemetry) | partial | instrumentation present once built, no backend/dashboard | backend choice deferred per ADR-0018, trigger: real dashboarding/alerting need |

## Definition of Done (per change)
- [ ] Acceptance criteria met
- [ ] Tests added/updated; CI green
- [ ] Lint / format clean; no new high or critical vulnerabilities
- [ ] OpenAPI updated if the API changed
- [ ] Migration is backward-compatible if the schema changed
- [ ] Deployed to dev and smoke-tested (once the pipeline exists)
- [ ] This checklist / ADRs updated if a row or decision changed

## What I would do next
(Fill in at the end: honest gaps and follow-ups.)
