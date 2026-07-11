# 06 — Architecture Governance

| | |
|---|---|
| **Phase** | Architecture Governance |
| **Why this phase exists** | To define the *rulebook for architecture* — the principles, forbidden practices, decision heuristics, success metrics, review gate, traceability framework, and risk register — so every architectural decision stays aligned with the approved business and engineering analysis. |
| **Owner** | Lead Architect (reviewers: Engineering Lead, Business Analyst). |

## Purpose
Establish the mandatory governance that the Software Architecture phase operates under: how
decisions are made, what they must/must not do, how they are measured, reviewed, traced, and
risk-managed.

## Scope
Governance of architecture only. It makes **no** architectural decisions, chooses **no** technology,
and produces **no** designs — those belong to phase 07.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Architecture Principles](01-architecture-principles.md) | Mandatory principles every decision must follow. |
| 02 | [Architecture Anti-Principles](02-architecture-anti-principles.md) | Explicitly forbidden practices. |
| 03 | [Decision Heuristics](03-architecture-decision-heuristics.md) | How to make and record decisions. |
| 04 | [Architecture Success Metrics](04-architecture-success-metrics.md) | Measurable approval gates. |
| 05 | [Architecture Review Checklist](05-architecture-review-checklist.md) | The gate before approval. |
| 06 | [Architecture Traceability Matrix](06-architecture-traceability-matrix.md) | Business→…→operations linkage framework. |
| 07 | [Architecture Risk Register](07-architecture-risk-register.md) | Risks arising from architectural decisions. |
| 08 | [Architecture Governance — Usage](08-architecture-governance-usage.md) | How to operate the governance set. |

## Dependencies
- **Input:** [05 Architecture Input](../05-architecture-input/README.md) and all fixed business inputs.
- **Output:** the governance framework the architecture phase must satisfy.

## Entry Criteria
- Architecture Input Report approved.

## Exit Criteria
- Governance framework approved and ready to constrain architecture decisions.

## Related Phases
- Governs the future **07 Software Architecture** and, through traceability, all later phases.

## Next Phase
→ [07 — Software Architecture](../07-software-architecture/README.md) *(future)*
