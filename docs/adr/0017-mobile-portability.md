# 0017: Mobile portability via responsive web design — no native/React Native/Flutter app

## Context
The brief's own domain story (a gardener planning each day's services for multiple
customers) strongly implies on-site, phone-in-hand usage — plausibly the primary real usage
pattern, even though the brief permits satisfying its requirement with either a website or a
native/cross-platform mobile app (not both). ADR-0006 already chose React web over React
Native/Flutter at the framework-selection level; this ADR addresses the follow-up question
specifically: given that choice, is mobile usage still adequately served?

## Options
1. Build a second, native/cross-platform app (React Native or Flutter) alongside the web app.
2. Responsive, mobile-first web design — one UI, works across phone/tablet/desktop.
3. A desktop-oriented web UI with no particular mobile consideration.

## Decision
Option 2.

## Rationale
Checked against what would actually justify option 1, the same test applied throughout this
project (does the extra machinery solve a problem that exists here):
- **Offline-first field work** would justify a native/PWA approach — already ruled out by A7
  (offline explicitly out of scope).
- **Native device APIs** (camera, push notifications, background location) would justify it —
  not needed; location (A1) is free-text/lat-lng, servable via the browser's Geolocation API.
- **App Store distribution** would justify it — not requested anywhere in the brief.
With none of these present, building a second UI would duplicate the web app for no served
need — the same over-engineering mistake already rejected for microservices (ADR-0001), APIM
(ADR-0009), CQRS (ADR-0015), and a repository layer (ADR-0016).

Option 3 is rejected because it ignores Q12 entirely — given the likely primary usage
pattern, a desktop-only layout would be a real usability gap, not just an incomplete nicety.

## Consequences
- UI built mobile-first: breakpoints designed from small screens up, not desktop-down.
- Touch-friendly targets, particularly on the state-transition controls (todo/scheduled/done)
  — these are the most likely to be used one-handed, on-site.
- Browser Geolocation API offered as a "use my current location" convenience when creating a
  todo, layered onto A1's existing free-text address field — no native app required for this.
- Target browsers per A8 (modern evergreen only) — no legacy/IE11 polyfill work.
- Manual verification at common breakpoints (a real phone or browser dev-tools device
  emulation) is sufficient; no dedicated responsive-design test tooling added, consistent with
  ADR-0016's minimal-testing-machinery stance.
