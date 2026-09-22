# 0012: Client-side rendering (CSR) via Next.js for authenticated views; SSR not used

## Context
ADR-0002 established that the JWT lives in an httpOnly cookie, and the browser calls the API
directly, same-origin via ingress path routing. Nearly all app content is behind auth — todos
are per-owner (A4/Q1), so there is no public content to index for SEO, removing SSR's usual
justification.

## Options
1. **Server-side rendering (SSR)** — Next.js server renders pages per request, including
   authenticated ones.
2. **Client-side rendering (CSR)** — Next.js serves a client-rendered app; the browser calls
   the API directly with the cookie attached.
3. **Static export, no Next.js server at runtime** — closest to option 2, further simplified.

## Decision
Option 2. All authenticated views are `'use client'`/CSR. SSR is not used for anything behind
auth. (A future genuinely-public page, e.g. a marketing/login page, could use SSR/SSG as a
narrow exception — not the default.)

## Rationale
- **SSR (1)** rejected: to render an authenticated page server-side, the Next.js server itself
  would need to read the httpOnly cookie and make an authenticated call to the API on every
  request — turning the frontend server into a second auth-aware component. That directly
  complicates ADR-0002, which was designed around the browser being the only thing holding
  and sending the cookie.
- **CSR (2)** chosen: matches ADR-0002 exactly — browser holds the cookie, browser calls the
  API same-origin. No second place validating or forwarding auth.
- SSR would have offered SEO/first-paint benefits that don't apply here, since there's no
  public content to optimise for.

## Consequences
- Next.js is used mainly for its tooling (file-based routing, TypeScript config, code
  splitting, a clean production build/Docker output for Step 6) rather than its rendering
  model — a real but narrower justification than "we use Next.js for SSR." Plain Vite + React
  Router would have been an equally defensible alternative; Next.js was chosen partly because
  the brief's footnote names it explicitly.
- If a genuinely public page is added later, it can use SSR/SSG in isolation without
  reworking the authenticated app's CSR model.
