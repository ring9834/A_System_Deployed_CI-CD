# 0009: Plain Kubernetes ingress for MVP; Application Gateway deferred; APIM rejected

## Context
ADR-0002 requires path-based routing (`/` -> web, `/api/*` -> API) under one host. Azure
offers several layers that could provide this: a bare ingress controller, Application
Gateway, or full API Management (APIM).

## Options
1. **Plain Kubernetes ingress** (nginx or a Gateway API implementation) — path routing, TLS
   termination.
2. **Azure Application Gateway** — L7 load balancer with WAF, can front AKS.
3. **Azure API Management (APIM)** — rate limiting, subscription keys, developer portal,
   versioning, analytics.

## Decision
Option 1 for MVP. Option 2 recorded as a reasonable **deferred** production upgrade. Option 3
is **rejected**, not deferred.

## Rationale
- **Ingress (1)** is exactly what ADR-0002's routing needs — nothing more.
- **Application Gateway (2)** adds real production value (WAF/DDoS protection at the edge)
  and is a legitimate next step; deferred because it isn't required to demonstrate the
  architecture within the time box.
- **APIM (3)** solves problems for an API with external, third-party API consumers needing
  subscription keys, a developer portal, or usage analytics. This project has exactly one
  first-party frontend calling its own backend. Adding APIM here would add cost and
  operational surface with no corresponding requirement — the API-layer version of the
  over-engineering rejected in ADR-0001 and ADR-0008. This is a considered rejection, not a
  time-boxing deferral, and worth stating as such if asked.

## Consequences
- Ingress config (Step 6) is small: two path rules, one TLS cert.
- If real external API consumers are ever added, revisit APIM at that point — not before.
