# 0003: Api -> Application -> Domain -> Infrastructure layering

## Context
Q2 requires the todo state machine (legal/illegal transitions) to be correctly enforced and
fast to test. Controllers alone don't give a place for that logic to live independent of
HTTP or persistence concerns.

## Options
1. Fat controllers: transitions/validation logic inline in controller actions.
2. Layered: `Api` (controllers, thin) -> `Application` (use cases) -> `Domain` (entities, state
   machine, invariants) -> `Infrastructure` (EF Core, Npgsql — implements ports defined by
   Domain/Application).

## Decision
Option 2. `Domain` has no framework dependencies (no `Microsoft.AspNetCore.*`, no EF Core
types). Controllers map HTTP <-> DTOs and delegate to `Application`.

## Consequences
- The state machine (Q2) is testable as plain C# unit tests, no web host or database needed —
  fast and directly demonstrable live in the interview.
- Controllers stay thin and easy to read/modify under time pressure (a stated interview
  requirement: implement live changes without AI).
- Slightly more ceremony than fat controllers for trivial CRUD; accepted because Q2 is a
  named high-priority scenario, not a hypothetical.

## Quality scenario(s) addressed
Q2 (correctness of state transitions); indirectly supports the "understand the code well
enough to change it live" requirement.
