# 0005: Kubernetes Secrets sourced from Terraform/Key Vault (MVP); CSI driver deferred

## Context
Secrets (DB connection string, JWT signing key) must never be committed to git and should be
manageable per-environment.

## Options
1. Kubernetes Secrets, values populated by Terraform (which itself reads from Azure Key
   Vault or a `.tfvars` file excluded from git).
2. Azure Key Vault Provider for Secrets Store CSI Driver — secrets mounted directly from Key
   Vault into pods, no Kubernetes Secret object at rest.

## Decision
Option 1 for MVP. Option 2 is recorded as a deferred backlog item, not silently dropped.

## Consequences
- Faster to set up within the time box; standard Helm/Terraform pattern.
- Secrets exist as base64-encoded Kubernetes Secret objects in etcd — weaker than option 2
  (no direct Key Vault mount, coarser rotation story). This is an explicit, documented
  trade-off, not an oversight — see `docs/production-readiness.md`.
- No secret values are ever committed to git in either case.

## Quality scenario(s) addressed
Baseline security hygiene; supports an honest "production-readiness" checklist rather than
overclaiming.
