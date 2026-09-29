# AI Usage Log — RUSH Todo Planner

Tool used throughout: Claude (web chat). This log covers the full SDLC — requirements through
production deployment — with particular depth on the real-deployment debugging session, since
that's where the most substantial AI-assisted diagnosis and self-correction happened.

## Requirements → Design → Architecture (Steps 1–3)

**Generated:** requirements.md (assumptions, backlog, quality scenarios), architecture.md
(24 ADRs, ATAM-lite), design.md (state machine, ERD, STRIDE), openapi.yaml.

**Verified:** Every ADR's alternatives were checked against the actual constraints of the
assessment brief before accepting AI's recommendation, not accepted wholesale.

## Step 4 — Walking skeleton

**Generated:** Dockerfiles, Terraform (AKS/ACR/Postgres/Key Vault), Helm chart skeleton,
GitHub Actions CI/CD with OIDC.

**Real bugs found and fixed, not assumed correct:**
- Missing `.dockerignore` — Windows host `bin/obj` paths corrupted the Linux build context
- `.NET 10` generates `.slnx` by default; Dockerfile assumed `.sln`
- AKS node SKU rejected twice (B-series disallowed for system pools; then not in the
  subscription's allowed list) — fixed via `az postgres flexible-server list-skus`/AKS
  equivalent checks, not guessed
- CI: `azure/login` + Terraform combination was fundamentally wrong (`azurerm` provider
  rejects Service-Principal CLI sessions) — corrected to `ARM_USE_OIDC` + job-level env vars
- CI: a skip-cascade bug where `deploy-dev` was silently skipped whenever `terraform` was
  path-filtered out, even on a fully valid deploy

## Slices 0–5 — Vertical feature development

**Generated:** TDD-driven state machine, CRUD endpoints, optimistic concurrency via `xmin`,
JWT auth, frontend components with real component tests run in a sandboxed environment.

**Real bugs found through actual verification, not just code review:**
- A case-sensitivity contract violation (`ToString()` returning PascalCase against a
  lowercase-documented API) — found because the UI silently broke, not because a test caught it
- .NET `HttpClient` rejecting an unquoted `If-Match` header per RFC 7232
- A stale test assertion, correct when written, made stale by an unrelated fix landing after it
- **Four sequential, independently-diagnosed auth bugs in one slice**: a genuinely missing
  endpoint, ASP.NET Core middleware ordering breaking `[Authorize]` entirely, a cookie
  `Secure` flag silently dropped over plain HTTP, and JWT claim remapping (`MapInboundClaims`)
  — each only became visible once the previous one was fixed

## Step 6 — Production hardening

**Generated:** replica/PDB config, deepened health checks, ConfigMap/Secret split, CI security
scanning additions.

**Two things caught and corrected before they shipped, not after:**
- A self-introduced Terraform/Helm ownership conflict (both trying to manage the same K8s
  Secret) — caught and fixed before ever being applied
- A Trivy Action version recommended by AI fell inside a range confirmed compromised in a
  real, documented supply-chain attack — caught by searching the actual advisory, not assumed
  safe because the version number looked plausible

## The real-deployment debugging session (this session)

This is the single richest debugging arc in the project — roughly 40 back-and-forth turns,
resolving issues across five genuinely distinct layers, each only visible once the layer
before it was fixed:

1. **Key Vault RBAC was silently in the legacy access-policy model** — `enable_rbac_authorization`
   was never set; every earlier "successful" role grant had done nothing at all, confirmed
   via a real 403.
2. **Wrong identity granted the role** — an App ID was used where a Service Principal Object
   ID was required; RBAC role assignments for a federated-credential-only identity must
   target the SP object, not the app registration.
3. **The Terraform backend defaulted to storage-account-key auth**, requiring a broad
   `listKeys` permission — fixed structurally with `use_azuread_auth = true` rather than
   granting yet another permission.
4. **Local `terraform state list` returned empty** — traced to a real, latent mismatch in
   `versions.tf`'s backend `key` and `resource_group_name`, silently pointing at an empty
   181-byte state file while the real, populated 47KB state sat under a different key the
   whole time. Recovered by correcting both values and re-initializing — nothing was
   actually lost.
5. **A single dynamic `principal_id = data.azurerm_client_config.current.object_id`** was
   trying to represent two different identities (the local user, CI's service principal),
   causing destroy/recreate churn every time a different identity ran `apply` — split into
   two static, permanent grants.
6. **Three separate OIDC federated-credential trust subjects needed**, discovered one at a
   time as each CI job (`terraform`'s PR-triggered runs, `pull_request` events specifically,
   and `deploy-dev`'s `environment: dev`) exercised a code path the others hadn't yet.
7. **An ingress `rewrite-target` annotation, correct for the API path, silently broke every
   static asset on the web path** — invisible until the first real browser load of the
   deployed frontend, since every prior check either hit the (correctly-configured) API path
   or the homepage document (which coincidentally "worked" even when misrouted).
8. **Postgres shipped with zero firewall rules by default** — the actual root cause behind a
   `todo-api` pod stuck failing its readiness probe; confirmed via a real Npgsql connection
   timeout in the pod's own logs, not assumed.
9. **No migration mechanism had ever actually run against the real database** — ADR-0004's
   Helm pre-upgrade hook was decided at design time but never built; closed properly this
   session with a `--migrate` mode reusing the existing runtime image (no second SDK-based
   image needed) plus a real `helm.sh/hook: pre-upgrade` Job.
10. **The session cookie's `Secure` flag was correct for local/test HTTP but wrong for the
    real deployed "dev" tier** — `!IsDevelopment()` incorrectly treated a TLS-less AKS
    environment as production-equivalent; fixed by introducing a genuine `Staging`
    environment distinction and checking `IsProduction()` specifically. This is a case where
    a risk was explicitly flagged in writing twice, in earlier PR descriptions, before it
    was ever hit live — good evidence the earlier flagging was substantive, not boilerplate.

**Two AI mistakes caught and corrected within this same session, worth naming honestly:**
- A `trivy-action` version tag was missing its required `v` prefix (`0.36.0` vs `v0.36.0`),
  causing the action to fail to resolve entirely — corrected after the real CI error.
- A claim that `.trivyignore` is auto-detected by `aquasecurity/trivy-action` was wrong —
  confirmed via multiple real GitHub issues that the action requires the file path passed
  explicitly via a `trivyignores:` input; the underlying `trivy` CLI's own auto-detection
  does not extend to this action wrapper.

**Overall verification discipline maintained throughout:** every `terraform plan` was read
before applying, every RBAC/OIDC error's exact identity and permission were checked against
Azure directly (never assumed from memory), and every "should work now" claim was followed
by an actual `kubectl`/browser check rather than trusting the fix on paper.
