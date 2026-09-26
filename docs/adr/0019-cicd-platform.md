# 0019: CI/CD platform — GitHub Actions, not Azure Pipelines/Jenkins/GitLab CI

## Context
GitHub Actions was used from Step 0 onward without ever being compared against real
alternatives — the same gap later caught and fixed for the .NET/React stack (ADR-0006),
database engine (ADR-0008), and container platform (ADR-0014). The brief mandates Terraform,
Kubernetes, and Helm explicitly; it does not mandate a CI/CD tool — this is a genuinely open
decision, not a brief-driven constraint.

## Should a CI/CD pipeline exist at all for this project?
A prior, more fundamental question than tool choice, worth answering honestly rather than
assuming — this is a solo, time-boxed assessment project, not a long-lived team codebase, and
some of CI/CD's classic value (catching integration conflicts between multiple contributors)
doesn't apply here.

**For:** it is one of the most concrete, demonstrable answers to the brief's "production-ready"
requirement — the same category as the tests the brief names as its own example of an
unlisted-but-required item. It pays for its setup cost across every subsequent commit (Step 5's
several vertical slices all benefit from automatic build/test/deploy verification, rather than
each being manually re-checked). It is itself interview evidence — a green run, a rollback
drill — rather than something to describe from memory. It is also a concrete instance of
catching AI-introduced errors automatically, not just a claimed practice.

**Against, stated honestly:** the setup cost was real and non-trivial — OIDC federated
credential configuration, two incorrect Azure VM SKUs discovered only at `apply` time, a
job-dependency skip-cascade bug, an environment-variable scoping bug — hours that did not go
toward the domain/feature code actually graded live. `apply` remains manual by design (see
`infra-runbook.md`), so this automates the application deploy, not the full infrastructure
lifecycle — a partial, not complete, "CI/CD" story, worth stating precisely rather than
overclaiming. And its value during the interview itself is mostly already-banked evidence,
not something actively exercised live under time pressure, since a real deploy cycle takes
minutes an interview session may not have room for.

**Decision:** build it. The setup cost is sunk; the marginal cost of using it going forward is
near zero, while the marginal benefit (verification on every remaining commit, plus standing
evidence) compounds. Had time been more constrained, the defensible alternative would have
been tests-only CI with manual, runbook-documented deploys — not a worse answer, a different
point on the same cost/benefit curve, and worth being able to articulate as a considered
trade-off rather than defending CI/CD as a reflexive default.

## Options (given a pipeline is being built — which platform)
1. **GitHub Actions** — native to GitHub, where the repository already lives.
2. **Azure Pipelines (Azure DevOps)** — Microsoft's own CI/CD; a plausible "natural fit" given
   the deployment target is Azure.
3. **Jenkins** — self-hosted, maximal flexibility via plugins.
4. **GitLab CI** — comparable capability to GitHub Actions.
5. **CircleCI / other third-party SaaS CI.**

## Decision
Option 1: GitHub Actions.

## Rationale
- **Not a brief requirement** — unlike Terraform/Kubernetes/Helm, this was always a free
  choice, and is recorded here rather than left implicit.
- **The real differentiator is avoiding a second platform for no corresponding benefit**, not
  a feature comparison. Azure Pipelines (2) is the only alternative with a genuine specific
  advantage (native Azure Resource Manager integration) — but it requires a separate Azure
  DevOps organization/account, a separate place for secrets and environment approvals to
  live, and a GitHub<->Azure DevOps service connection to bridge the two. GitHub Actions
  already reaches Azure directly via OIDC federation, with no such bridge needed — solving a
  problem Azure Pipelines exists to solve, that doesn't exist here.
- **Jenkins (3)** rejected: self-hosting a CI server means standing up, patching, and
  operating a new piece of infrastructure, on top of everything already provisioned —
  disproportionate operational cost for a single-repo project, and the same over-engineering
  shape as every other rejected option in this project (ADR-0001, 0009, 0015).
- **GitLab CI (4) / other SaaS (5)** rejected: the repository is on GitHub; either option
  requires mirroring/migrating the repo or bridging platforms, for no capability GitHub
  Actions lacks.
- **Empirical evidence, not just a priori reasoning** (unusual for an ADR in this project —
  most reason about a decision not yet built): by the time this ADR was written, GitHub
  Actions had already been used successfully to build .NET and Node in one workflow, push
  Docker images to ACR via OIDC (no stored secret), run Terraform with native OIDC auth
  (path-gated to skip non-infra changes), lint/dry-run the Helm chart, and deploy to AKS
  behind a `dev` environment approval gate with a smoke test. This is a stronger form of
  justification than a hypothetical feature comparison — the tool has already demonstrated it
  handles every job this project actually needs.

## Consequences
- No migration cost, since this simply documents a decision already exercised in practice.
- If a future need arose that GitHub Actions genuinely couldn't serve (e.g. Windows-specific
  build agents at a scale GitHub-hosted runners don't support well), that would be a concrete,
  evidence-based trigger to revisit — not assumed pre-emptively.
