# 0016: Testability strategy — no generic repository layer, Testcontainers for integration
# tests, real auth in tests, one narrow IClock port, deferred frontend mocking library

## Context
Q8-Q11 (requirements.md) require: fast isolated domain tests, integration tests against the
real database engine, testable time-dependent logic, and testable frontend components.
Several real alternatives exist for each; this ADR records the choices and why, applying the
same right-sizing test used throughout (ADR-0001, 0009, 0011, 0013, 0015): does the
abstraction solve a problem that actually exists here, or add indirection for its own sake.

## Decisions

### 1. No generic repository/port layer over EF Core
**Options:** (a) wrap `DbContext` in `IRepository<T>`-style interfaces for mockability, or
(b) inject `DbContext` directly into `Application`/`Infrastructure`, relying on layering
(ADR-0003) for the testability that actually matters.
**Decision:** (b). `DbContext` already implements Unit-of-Work/Repository; a generic wrapper
adds indirection without adding real isolation, since the logic worth unit-testing (the state
machine) already lives in `Domain` with zero EF Core dependency. A repository layer here would
re-solve a problem ADR-0003 already solved more cheaply.

### 2. Testcontainers (real PostgreSQL) for integration tests, not SQLite in-memory
**Options:** (a) EF Core's `UseInMemoryDatabase`, (b) SQLite in-memory, (c) Testcontainers
spinning up a real, ephemeral PostgreSQL per test run, (d) a shared long-lived dev database.
**Decision:** (c). (a) and (b) both risk tests passing against behavior that diverges from
real PostgreSQL — concretely, `xmin`-based optimistic concurrency (ADR-0004, Q6) doesn't exist
in SQLite at all, so Q6 literally cannot be honestly tested against it. (d) risks state leaking
between test runs and doesn't work well in parallel CI. Testcontainers gives a real, isolated,
disposable Postgres instance per run — GitHub-hosted `ubuntu-latest` runners have Docker
available natively, so this works in CI with no extra setup.

### 3. Auth in tests uses the real `/auth/login` endpoint — no test-only bypass
**Decision:** integration tests authenticate as real seeded test users through the actual
login endpoint (ADR-0007), not a special "skip auth" test mode. A bypass would mean Q1's
owner-isolation tests never actually exercise the thing they claim to verify.

### 4. One narrow port: `IClock`
**Options:** (a) call `DateTime.UtcNow` directly wherever needed, (b) a single `IClock`
interface (`DateTimeOffset UtcNow { get; }`) injected where `ScheduledFor`/overdue logic lives.
**Decision:** (b), narrowly. This is deliberately not a general pattern — it's the one place
untestable wall-clock dependence would otherwise force tests to sleep or become flaky (Q11).
No other ports are added under this justification; this is the exception, not a precedent for
wrapping everything.

### 5. Frontend: mock `fetch` directly; defer MSW (Mock Service Worker)
**Options:** (a) hand-mock `fetch` per test (`vi.spyOn`/equivalent), (b) adopt MSW for
realistic network-layer interception.
**Decision:** (a) for now, with (b) as a deferred, triggered upgrade — same pattern as
ADR-0011 (caching) and ADR-0015 (CQRS). Trigger: once enough test files need consistent,
shared request-mocking that hand-rolled mocks start actively hurting maintainability, not
assumed pre-emptively for a component set this small.

## Consequences
- Step 5 code should inject `DbContext` directly (no repository interfaces to build first).
- Step 5 needs one small `IClock`/`SystemClock` pair, nothing more speculative.
- The integration test project needs a Testcontainers PostgreSQL fixture, reused across the
  test run (not spun up per-test, for speed) — a real CI-time-budget decision, worth
  monitoring once the suite grows.
- No new frontend testing library added yet; add MSW only when the deferred trigger is hit.
