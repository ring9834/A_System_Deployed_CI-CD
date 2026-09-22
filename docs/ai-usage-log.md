# AI usage log

Short, honest, written as I go. Purpose: show what AI did, what I changed, and how I verified it.

## Entries
| Date | Task | Tool | What AI produced | What I changed / rejected | How I verified |
|---|---|---|---|---|---|
| 2026-09-22 | Draft Step 1 requirements (assumptions log, backlog cut line, quality scenarios) from the RUSH brief | Claude (web chat) | Full first draft of `docs/requirements.md`: 7 assumptions (A1-A7), backlog split into Built/Deferred/Out-of-scope, 6 quality scenarios (Q1-Q6) with priority and verification method | Kept structure and most content; reviewed each assumption against the brief myself before accepting | Re-read the brief against each assumption row; checked each scenario names a test I can actually build later |
| 2026-09-22 | Add caching (Redis) to Deferred backlog and a scalability quality scenario | Claude (web chat) | I proposed the addition myself with reasoning (distributed cache, shared across instances); asked AI to add it as Q7 at priority M/M | AI pushed back on my M/M rating: separated the *architecture decision* (stateless API — cheap now, expensive to retrofit) from the *implementation* (Redis — correctly deferred), and re-rated Q7 as H/L. I accepted the re-rating because the reasoning held up | Checked the logic against my own understanding of stateless design in ASP.NET Core; confirmed the decision belongs in Step 2 as an ADR, not just a backlog note |

## Rejected or corrected suggestions
| Date | Suggestion | Why I rejected or corrected it |
|---|---|---|
| 2026-09-22 | AI's first pass on Q7 (my own initial M/M rating) | Accepted AI's counter-argument instead: priority should reflect cost of reversal (high), not implementation risk (low) — rated H/L, not M/M |

## Principles I follow
- Spec and acceptance criteria before prompting; small increments; review every diff.
- AI drafts tests, boilerplate and IaC; I verify IaC with `terraform validate`/`plan` and `helm lint`/`template`.
- I only keep code I can explain and modify without AI.
- When I disagree with AI's reasoning (or it disagrees with mine), I record which way it went and why.
