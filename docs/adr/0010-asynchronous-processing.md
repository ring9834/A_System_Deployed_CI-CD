# 0010: No asynchronous messaging for MVP (distinct from async/await, which is used throughout)

## Context
"Asynchronous" covers two unrelated things, worth separating explicitly:
1. `async`/`await` for I/O-bound code (DB calls, HTTP calls) — a coding practice.
2. Asynchronous *messaging* (a queue decoupling request handling from background work) — an
   architectural decision.

## Decision
1. Use `async`/`await` throughout the API for all I/O — not an ADR-level decision, it's the
   ASP.NET Core baseline regardless of scale, and always correct here.
2. No message queue (e.g. Azure Service Bus) for MVP.

## Rationale
Every current operation (create/read/update/delete a todo, change state) is a fast,
synchronous, single-resource round trip. A queue would decouple request handling from work
that doesn't currently exist — no long-running processing, no external slow integrations, no
fan-out notifications.

## Trigger for revisiting
If geocoding (currently a deferred backlog item — see `requirements.md`) is built, that
introduces an external, potentially slow, potentially unreliable call. At that point, moving
geocoding off the request path onto a queue becomes genuinely justified — not before.

## Consequences
- Simpler system: no broker to provision, operate, or explain.
- The trigger above is written down so this isn't a silent gap if the interview asks "what
  about async?" — the answer is "considered, correctly not needed yet, here's what would
  change that."
