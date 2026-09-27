# Requirements: assumptions, backlog, quality scenarios

## 1. Assumptions log
The brief leaves several things open. Each is answered and justified here so the reasoning
is written down before implementation, not invented afterwards.

| # | Question | Assumption | Reason |
|---|---|---|---|
| A1 | What is "location"? | Free-text address string + optional lat/lng; geocoding deferred | Matches the gardener use case without requiring a maps API for MVP |
| A2 | Does "scheduled" carry a date/time? | Yes: `ScheduledFor` (nullable, required when state = scheduled) | Needed for "plan each day's services" to be meaningful |
| A3 | Legal state transitions? | `todo -> scheduled -> done`, plus `scheduled -> todo` (unschedule). No transition out of `done`. | Simplest model that supports the domain story; defensible live |
| A4 | Single-user or multi-user? | Multi-user from day one; every todo has `OwnerId` | Cheap now, expensive to retrofit; creates a real security scenario (Q1) |
| A5 | Mobile or web UI? | Web (Next.js) | Matches chosen stack; brief allows either |
| A6 | Is a full identity provider in scope? | No — full OIDC/IdP integration is out of scope for the assessment; some lighter form of authentication is required (per A4, multi-user) and the mechanism is decided in [ADR-0007](adr/0007-authentication-mechanism.md), not assumed here | Standing up or integrating a real IdP is disproportionate setup cost for this time box; which mechanism to use instead is an architecture decision, not a requirements assumption |
| A7 | Offline support? | Out of scope | Not implied by the brief; adds complexity with no stated benefit |
| A8 | Target browser support? | Modern evergreen browsers only (current Chrome/Safari/Firefox/Edge, desktop and mobile), no legacy/IE11 support | Not specified by the brief; avoids silent scope creep into cross-browser polyfill work |

## 2. Backlog with a cut line
Reasoned prioritisation of an intentionally oversized brief.

### Built (MVP)
- Create / list / update / delete todo (title, state, location) — brief's explicit CRUD core
- State machine enforcing legal transitions (A3) — smallest thing that shows real domain logic
- Owner-scoped access control (A4, A6; mechanism decided in [ADR-0007](adr/0007-authentication-mechanism.md)) — highest-value, lowest-cost security story
- Pagination + filter by state on list endpoint — realistic API behaviour
- Deployed to Kubernetes via Helm, provisioned by Terraform — infra requirement, end to end
- CI pipeline: build, test, lint, scan — core evidence for "production-ready"

### Deferred (documented, would do next)
- Geocoding / map view of location — enhancement, not required by the brief
- Full third-party identity provider — a lighter, self-issued mechanism (ADR-0007) proves the pattern at lower cost
- Observability beyond basic health/logs (metrics, tracing) — valuable, low interview signal
- HPA / autoscaling — real concern, low signal at todo-app scale
- Distributed caching (e.g. Redis) for frequently-requested, relatively stable data — valuable once traffic/instances grow; only pays off once the API is already stateless and running across multiple instances, so it's a pure addition later rather than a prerequisite now
- Idempotency key on `POST /todos` — a network retry could currently create a duplicate todo; low severity for a personal todo list, deferred until it's a demonstrated real annoyance (see ADR-0022)

### Out of scope
- Offline support (A7)
- Multi-tenant organisations, notifications, recurring todos — beyond "track todos with a location"

## 3. Quality attribute scenarios
Each scenario has a measurable target and a named verification, so "production-ready" is
checkable rather than asserted. Priority = business importance / technical risk (H/M/L).
High/High rows drive the Step 2 architecture decisions (ADD-lite).

| # | Attribute | Scenario | Priority | Verified by |
|---|---|---|---|---|
| Q1 | Security | User A requests/modifies user B's todo via a guessed ID | H/H | Integration test: expect 404 (not 403, to avoid leaking existence) |
| Q2 | Correctness | Client attempts an illegal state transition | H/H | Unit test on domain state machine; API returns 409 ProblemDetails |
| Q3 | Performance | List 1,000+ todos, paginated, filtered by state | H/M | Integration test: p95 < 200ms locally; index on (OwnerId, State) |
| Q4 | Availability | Rolling deploy while API receives traffic | M/M | Manual drill (Step 6): `helm upgrade --atomic` + smoke test, zero failed requests |
| Q5 | Modifiability | Add a new field end to end (migration -> DTO -> UI) | M/L | Timed manual drill (Step 7), target < 30 min, no AI |
| Q6 | Data integrity | Two clients update the same todo concurrently | M/M | Integration test: second write gets 409 via concurrency token (xmin) |
| Q7 | Scalability | API runs as N replicas behind a load balancer; any instance can serve any request | H/L | ADR: stateless API, no in-process session/cache; integration test hits the same endpoint via two instances (or a mid-test restart) and gets an identical result. Priority is H (cheap now, costly to retrofit) even though risk of getting it wrong is L (near-default for ASP.NET Core). Caching implementation itself (Redis) is deferred — see backlog. |
| Q8 | Testability | Domain state machine (Q2) is unit-testable in isolation — no HTTP host, no database | H/L | Already satisfied by ADR-0003's layering; `TodoStateMachineTests` runs in milliseconds |
| Q9 | Testability | API integration tests run against the real database engine, not a stand-in, catching genuine query/schema issues (Q1, Q6) | H/M | ADR-0016: Testcontainers PostgreSQL, run in CI (Docker available natively on `ubuntu-latest`) |
| Q10 | Testability | Frontend components are testable in isolation without a real backend running | M/L | ADR-0016: mock `fetch` directly per test; MSW deferred until a real trigger is hit |
| Q11 | Testability | Time-dependent logic (`ScheduledFor`, A2) is testable without waiting on or awkwardly mocking the wall clock | M/L | ADR-0016: narrow `IClock` port, the one deliberate exception to "no repository/port layer" |
| Q12 | Portability | A gardener accesses and updates their todos from a phone browser while on-site with a customer | H/M | ADR-0017: responsive, mobile-first web UI; manual verification at common breakpoints; no native app built |
| Q13 | Observability | Given only logs/metrics/traces (no debugger attached), determine whether a specific request succeeded, how long it took, and which user made it | H/L | ADR-0018: structured logs + correlation ID (free via ASP.NET Core `Activity`/`TraceIdentifier`) + OpenTelemetry-based metrics/traces, vendor-neutral; backend/exporter choice deferred |

## Traceability (sample rows)
| Requirement | Quality scenario | ADR (Step 2) | Test | Pipeline gate |
|---|---|---|---|---|
| Owner isolation | Q1 | ADR: Auth approach | `OwnerIsolationTests` | Integration job |
| No illegal transitions | Q2 | ADR: Domain layering | `TodoStateMachineTests` | Unit job |
| Concurrent update safety | Q6 | ADR: Concurrency strategy | `ConcurrentUpdateTests` | Integration job |
| Horizontal scalability | Q7 | ADR: Stateless API / no in-process session | `StatelessApiTests` | Integration job |