# Production Readiness — RUSH Todo Planner

Status as of the close of Step 6 (production hardening) plus the full real-deployment
debugging session that followed. Each row reflects what was actually verified against the
real Azure/AKS deployment, not just what was coded.

## Security

| Item | Status | Evidence |
|---|---|---|
| Owner isolation (Q1) | ✅ Done | `OwnerIsolationTests` — cross-owner access returns 404, never 403 |
| Self-issued JWT, httpOnly cookie transport | ✅ Done | ADR-0007/0002; `AuthController.cs` |
| JWT claim mapping (`sub`) read correctly | ✅ Done | `MapInboundClaims = false` fix, Slice 4 |
| Session cookie `Secure` flag correctness | ✅ Done | Fixed twice: once for local/test HTTP (Slice 4), once for the real "Staging" AKS tier with no TLS yet (found live in production testing — see AI usage log) |
| Password hashing | ✅ Done | ASP.NET Core `IPasswordHasher<User>` (PBKDF2), no third-party dependency |
| Key Vault RBAC-only access (no shared keys) | ✅ Done | `enable_rbac_authorization = true`; found disabled by default, a real gap closed this session |
| Terraform backend uses AAD auth, not storage account keys | ✅ Done | `use_azuread_auth = true` — avoids ever needing broad `listKeys` permission |
| Real secrets in Key Vault, not committed anywhere | ✅ Done | `db-connection-string`, `jwt-signing-key` — verified present via `az keyvault secret list` |
| CI/CD image scanning (Trivy) | ✅ Done | Wired in, with a documented `.trivyignore` for non-runtime-reachable base-image (npm/yarn) CVEs |
| Static analysis (CodeQL) | ✅ Done | Runs via GitHub's own "Advanced" setup workflow (`codeql.yml`), not a hand-rolled job |
| Ingress TLS | ❌ Not done | Plain HTTP only (`nip.io`); known, deliberate gap for this dev tier — the reason the `Secure` cookie logic had to become environment-aware rather than a blunt `!IsDevelopment()` check |
| Database firewall — least-privilege scoping | ⚠️ Partial | Currently "Allow Azure services" (broad, not IP-scoped) — pragmatic for AKS-without-VNet-integration; a private-endpoint/VNet-integrated setup would be the stricter alternative, deliberately deferred |
| Key Vault CSI driver (direct pod-level secret mounting) | ❌ Deferred | ADR-0005's originally-deferred item; current approach (Terraform → K8s Secret → Helm `envFrom`) is a reasonable interim, not the CSI-driver end state |
| RBAC granted per-identity, not shared/dynamic | ✅ Done | Fixed this session — a single `data.azurerm_client_config.current.object_id`-based grant was flip-flopping between the local user and CI identity, causing destroy/recreate churn and, briefly, CI having zero Key Vault access |

## Reliability

| Item | Status | Evidence |
|---|---|---|
| ≥2 replicas + PodDisruptionBudget | ✅ Done | Both `api` and `web`, `minAvailable: 1` |
| Liveness vs readiness split | ✅ Done | `/health/live` (no dependency check) vs `/health/ready` (real DB connectivity check via `AddDbContextCheck`) |
| Rolling updates are genuinely zero-downtime | ✅ Verified | Watched a real `deploy-dev` rollout keep both services available throughout |
| Automatic rollback on failed deploy (`--atomic`/`--rollback-on-failure`) | ✅ Verified live | Confirmed twice — once during a deliberately-broken readiness scenario, once when the migration Job hung and the whole release correctly rolled back rather than leaving a half-applied state |
| Real rollback drill, with captured evidence | ❌ Not yet run | Procedure documented (`ci-hardening-additions.md`); not yet personally executed with saved logs |
| Database schema migrations run automatically, safely | ✅ Done | Real gap found and closed this session — a `helm.sh/hook: pre-upgrade` Job, using the existing runtime image with a `--migrate` mode (no SDK image needed) |
| Postgres network reachability from AKS | ✅ Done | Firewall rule added — found as a genuine, previously-invisible gap: the server shipped with zero firewall rules by default |
| Stale Terraform state locks | ⚠️ Known operational risk | Encountered multiple times this session (interrupted CI runs); `terraform force-unlock` is the documented recovery, not yet automated |

## Observability

| Item | Status | Evidence |
|---|---|---|
| OpenTelemetry tracing/metrics/logging | ✅ Done | Console exporter; backend/dashboard choice explicitly deferred (ADR-0018) |
| Structured logs | ✅ Done | Built-in .NET logging + OpenTelemetry |
| No raw database errors leaked to clients | ❌ Real gap found | `TodoExceptionHandler`'s fallback currently returns a raw Npgsql/Postgres exception message to the browser (seen live during the pre-migration `42P01` error) — needs a generic 500 response with the real detail logged server-side only |

## CI/CD

| Item | Status | Evidence |
|---|---|---|
| PR-time gates (build+test, lint, Terraform plan, Helm validation) | ✅ Done | `dorny/paths-filter`-gated so infra checks only run when infra actually changes |
| Helm chart validated fully offline in CI | ✅ Done | Switched from `kubectl --dry-run=client` (which still needs a live cluster for API discovery — cannot work in CI at all) to `kubeconform` |
| OIDC federated auth (no long-lived cloud credentials in CI) | ✅ Done | Three separate trust subjects needed and added: `ref:refs/heads/main`, `pull_request`, `environment:dev` |
| Terraform `apply` only on merge to `main`, never on PR | ✅ Done | `deploy-dev` gated to `push` |
| Third-party GitHub Action versions verified against real advisories | ✅ Done, the hard way | `aquasecurity/trivy-action` — the originally-drafted version fell inside a range confirmed compromised in a real supply-chain attack; caught before merging |

## Frontend / UX

| Item | Status | Evidence |
|---|---|---|
| Mobile-first responsive layout | ✅ Done | Verified via manual device-emulation pass |
| Loading/empty/error states | ✅ Done | Across `TodoForm`, `TodoList`, `AuthGate` |
| Logout | ✅ Done | Added in Slice 5 as a real, previously-missing gap |
| Ingress correctly routes ALL paths, not just API + homepage | ✅ Done, found live | A `rewrite-target` annotation applied to a capture-group-free web path silently broke every static asset (JS/CSS/fonts) — invisible until the first real browser test of the deployed frontend |

## Known, deliberate scope limitations (not gaps — documented trade-offs)

- No blue/green or canary deploys (ADR-0021) — no real traffic to justify the cost/complexity
- Single AKS node, Free control-plane tier (ADR-0014) — cost-conscious for a personal assessment project
- No Redis/caching layer (ADR-0011) — not triggered by any real load
- CQRS explicitly rejected in favor of a staged read-scaling ladder (ADR-0015)
