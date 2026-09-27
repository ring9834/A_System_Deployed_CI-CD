# 0022: Communication style — synchronous HTTP/REST (JSON) request/response; not gRPC/AMQP/WebSockets/async-polling

## Context
"RESTful API" was stated as a personal stack preference in this project's first message,
never formally compared against alternatives — the same gap later caught and fixed for the
.NET/React stack (ADR-0006), database engine (ADR-0008), and CI/CD platform (ADR-0019). This
ADR runs that comparison retroactively.

## Options
1. HTTP/REST with JSON.
2. gRPC (Protobuf, HTTP/2).
3. AMQP-based messaging (RabbitMQ, Azure Service Bus).
4. WebSockets.

## Decision
Option 1 (confirming the existing choice, not changing it).

## Rationale
- **gRPC (2)** rejected: its core strength is efficient, strongly-typed **service-to-service**
  communication, often across a polyglot microservice mesh. ADR-0001 chose a modular monolith
  — there is no internal service-to-service traffic to optimize. Worse, browsers cannot call
  gRPC natively; supporting it from the SPA (ADR-0012) would require gRPC-Web plus an Envoy
  proxy translating between the two, reintroducing exactly the kind of extra infrastructure
  layer already rejected elsewhere (APIM in ADR-0009, blue/green in ADR-0021).
- **AMQP (3)** rejected: this is the same question already fully analyzed under "asynchronous
  processing" in ADR-0010 and rejected for MVP, with an explicit trigger (geocoding becoming a
  real external, slow call). No new analysis needed here — cross-referenced, not repeated.
- **WebSockets (4)** rejected: no real-time or collaborative use case exists in the brief. Per
  A4, "multi-user" means each gardener has their own private todos, not multiple people
  editing a shared list simultaneously — there is nothing to push live updates about.
  Notably, a WebSocket connection is inherently sticky to one server instance, the opposite of
  the stateless-API property ADR-0002/0007 deliberately built for Q7. Supporting it properly
  across replicas would need a pub/sub backplane (typically Redis), silently reintroducing the
  exact dependency ADR-0011 correctly deferred — for a feature nothing in this project
  actually requires.
- **HTTP/REST (1)** confirmed: it is precisely suited to a browser SPA doing CRUD (the brief's
  own description of the API) against a single first-party backend — REST's core use case.
  It also preserves easy human-debuggability (`curl`, browser DevTools), which has already
  proven valuable throughout this project's own development (e.g. the Step 4 walking-skeleton
  verification sequence).

## Request/response as the interaction style (distinct from wire protocol, above)
A separate axis worth answering explicitly: synchronous request/response (client waits for
the answer) vs. an asynchronous accept-and-poll/webhook pattern (client gets `202 Accepted`
+ a task ID, retrieves the real result later) vs. publish/subscribe.

**Decision:** synchronous request/response — this is the same conclusion ADR-0010 already
reached ("every operation is a fast, single-resource round trip; no long-running work
exists"), restated here as an interaction-style choice rather than a messaging-infrastructure
choice. No new reasoning is needed; if geocoding (deferred) is later built and turns out slow,
*that specific operation* becomes the concrete trigger to introduce accept-and-poll — not
assumed pre-emptively for the whole API.

## Two related gaps this angle surfaces

**API versioning:** not previously decided. Following ADR-0009's own reasoning (no external
API consumers, one first-party frontend deployed from the same repo), no versioning scheme
(`/v1/`, header versioning) is needed. This only holds if API changes follow the same
**expand/contract discipline** ADR-0004 already established for database migrations, extended
to the API contract itself: since web and API are separate Deployments rolling independently
(Q4), a field or endpoint must be *added* in one release and only *removed* in a later one,
never both in the same deploy — otherwise a rolling update could briefly have old-web calling
new-API or vice versa against an incompatible contract.

**Idempotency of `POST /todos`:** currently unhandled — a network retry on "create todo"
could produce a duplicate. Ranked low-severity given the domain (a personal todo list, not a
payments system) and deferred, not ignored: a future fix would be a client-generated
idempotency key header the server deduplicates on. Added to the backlog's Deferred list.

## Consequences (updated)
- No change to `docs/openapi.yaml` or any existing endpoint design — this ADR documents why
  the already-built API shape is correct, rather than introducing a new one.
- If a genuine internal service boundary or real-time collaborative feature ever emerged
  (neither currently justified), gRPC or WebSockets would be the concrete trigger to revisit
  — not assumed pre-emptively here, consistent with every other deferred decision in this
  project.
