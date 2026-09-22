# 0002: Stateless API; JWT delivered via httpOnly cookie, same-origin via ingress

> **Related ADRs:** this ADR covers *transport* only — how the already-chosen JWT mechanism
> physically travels between browser and API. For *why JWT* (vs Basic Auth or server-side
> sessions) in the first place, see [ADR-0007](0007-authentication-mechanism.md). For how the
> frontend consumes this (CSR, no SSR), see [ADR-0012](0012-frontend-rendering-strategy.md).

## Context
Q7 (scalability) requires any API replica to serve any request with no server-held session
state. Q1 (owner isolation) requires the SPA to reliably send credentials the API can validate
per-request. The brief's stack is Next.js (browser SPA) talking to a separate .NET API.

## Options
1. **Bearer token in browser storage** (localStorage/sessionStorage), sent as
   `Authorization: Bearer <token>`.
2. **JWT in an httpOnly, Secure, SameSite=Lax cookie**, set by the API on login; SPA and API
   served same-origin via ingress path-based routing (`/` -> web, `/api/*` -> API).
3. **Next.js as a full backend-for-frontend**, holding its own session and proxying to the API.

## Decision
Option 2. JWT lives in an httpOnly cookie set by the API. Ingress routes `/api/*` to the
backend and `/` to the frontend under one host, so the cookie is same-origin and no CORS
configuration is needed for the browser-to-API path.

## Consequences
- The token is not readable by JavaScript, which mitigates XSS token theft — a stronger
  default than option 1.
- The API still holds **no session state**: it validates the JWT fresh on every request: the
  cookie is only a transport mechanism, and statelessness (Q7) is preserved. Any replica can
  validate any request.
- State-changing requests need CSRF protection (SameSite=Lax covers most cases; a
  double-submit or anti-forgery token can be added if needed).
- Avoids building a full BFF (option 3), which is more machinery than this scope justifies.
- Ingress must route both `/` and `/api/*` under one host — a small, explicit decision made
  now rather than discovered late in Step 6.

## Quality scenario(s) addressed
Q1 (owner isolation depends on reliable credential delivery), Q7 (stateless, horizontally
scalable API).
