# 0007: Authentication mechanism — JWT (self-issued), not Basic Auth or server-side sessions

## Context
Q1 (owner isolation) requires reliably knowing which user is making each request. Q7
(stateless API, any replica serves any request) constrains *how* that identity can be
established. A6 (requirements.md) records that full IdP integration is out of scope but
deliberately leaves the mechanism itself undecided — this ADR is where that mechanism is
actually chosen, against real alternatives.

## Options
1. **Basic Auth** — credentials sent on every request.
2. **Server-side session** — session ID in a cookie, session state held by the server.
3. **JWT (self-contained bearer token)** — signed token carries claims (user ID, expiry);
   server validates the signature only, no server-side lookup.
4. **OAuth2/OIDC via a real identity provider** (Entra ID, Auth0, etc.).

## Decision
Option 3: JWT, self-issued by the API for this assessment (per A6). Transport of the token
is a separate decision — see ADR-0002 (httpOnly cookie, same-origin via ingress).

## Rationale
- **Basic Auth (1)** rejected: no expiry, no claims to carry `OwnerId`, and repeatedly
  transmitting credentials is a weaker baseline than a scoped, expiring token.
- **Server-side session (2)** rejected: directly conflicts with Q7 unless backed by a
  distributed session store — which would reintroduce exactly the Redis dependency already
  correctly deferred in the backlog, just to support auth rather than performance.
- **JWT (3)** chosen because it satisfies Q7 and Q1 *simultaneously and for free*: no
  server-side state to synchronize across replicas (Q7), and the token's `OwnerId` claim is
  exactly what every query needs to filter by (Q1).
- **OIDC (4)** is the stronger real-world answer and is the deferred backlog item — standing
  up or integrating a real IdP is disproportionate setup cost against this time box, but the
  JWT-based design here doesn't preclude swapping in a real IdP's tokens later (the API
  already validates signature + claims, not passwords).

## Consequences
- Self-issued JWTs mean the API also owns a minimal `/auth/login` (or a seeded dev user) —
  scoped down, not a full identity system.
- If the assessment/interviewer asks "why not OAuth," the answer is cost-vs-scope, with the
  design already compatible with a future swap.
