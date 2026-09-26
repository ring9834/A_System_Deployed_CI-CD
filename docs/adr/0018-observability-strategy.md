# 0018: Observability — instrument now with OpenTelemetry (vendor-neutral); defer backend

## Context
Q13 requires determining request success/latency/identity from logs/metrics/traces alone.
The question raised: should logs/metrics/traces be designed for now, even without selecting
a specific observability backend (Azure Monitor, Prometheus/Grafana, Application Insights,
Datadog, etc.) within this project's scope?

Observability splits into two layers with very different costs of deferring:
- **Instrumentation** (what code emits) — expensive to retrofit; every endpoint needs
  revisiting after the fact if skipped now.
- **Backend/tooling** (where data goes, dashboards, alerting) — cheap to defer, *provided*
  instrumentation used a vendor-neutral API rather than a backend-specific SDK.

## Decision
Instrument now, using .NET's native, vendor-neutral observability APIs. Defer the backend
choice, with an explicit trigger.

**Built now:**
- Structured logging (`ILogger`, structured fields — `OwnerId`, `TodoId` — never string
  interpolation), including the correlation ID on every line. ASP.NET Core generates this for
  free via `HttpContext.TraceIdentifier`/the `Activity` API — no custom correlation-ID
  mechanism needs to be built.
- Basic metrics via `System.Diagnostics.Metrics` (`Meter`/`Counter`/`Histogram`): request
  duration, count by status code, and a counter for state transitions (ties to Q2).
- Trace spans around the interesting operations (the transition endpoint, the DB call) —
  largely automatic once the OpenTelemetry SDK is wired in; ASP.NET Core and Npgsql both have
  existing OTel instrumentation packages.
- Exported to console/local OTLP collector for now — proves instrumentation works without
  committing to backend infrastructure.

**Deferred, with a trigger:**
- Choice of backend (Azure Monitor, self-hosted Prometheus/Grafana/Loki, Application
  Insights, Datadog, ...) and any dashboards/alerting rules built on top of it. Trigger: once
  real dashboards, alerting, or cross-request trace correlation are needed to debug something
  live — not assumed pre-emptively.

**Explicitly not built:** frontend error/analytics tracking (e.g. Sentry). No real production
user base exists for this assessment; adding a third-party client SDK would be unjustified
machinery, the same category of over-reach as MSW in ADR-0016.

## Rationale
Using OpenTelemetry's vendor-neutral surface rather than a backend-specific SDK (e.g. the
Application Insights SDK directly) means the backend decision genuinely stays cheap to defer
— swapping exporters later is a few lines of startup configuration, not a rewrite of
instrumentation scattered through the codebase. This is the same "cheap now, expensive to
retrofit" logic already applied to statelessness (ADR-0002/0007) and owner isolation (Q1).

## Consequences
- Upgrades the STRIDE table's Repudiation row (`design.md`) from "partial — basic logs only"
  toward a real mitigation: structured logs with `OwnerId` on every mutating request are an
  actual audit trail, not just noise.
- `production-readiness.md`: mark "Structured logs with correlation ID" and "Health
  endpoints" as achievable now; "Metrics / traces (OpenTelemetry)" as partially built
  (instrumentation present, no backend/dashboard yet) — add these rows explicitly rather than
  leaving the distinction implicit.
- No new infrastructure required for MVP — the OTel SDK and console exporter are NuGet
  packages, not a service to provision.
