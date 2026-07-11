# 05 — Architecture Input

| | |
|---|---|
| **Phase** | Architecture Input |
| **Why this phase exists** | To provide the single briefing that bridges all analysis into the architecture phase — what must be solved, what is fixed, what is open, and what defines architectural success — so architects start with full, prioritized context. |
| **Owner** | Lead Architect (reviewers: Engineering Lead, Business Analyst). |

## Purpose
Summarize and prioritize the approved business and engineering analysis into drivers, constraints,
fixed vs open decisions, quality-attribute priorities, trade-offs, open questions, success criteria,
and a readiness assessment.

## Scope
A briefing/bridge. It summarizes and cross-references; it makes **no** architectural decisions and
chooses **no** technology.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Architecture Input Report](01-architecture-input-report.md) | The first document every architect reads: drivers, constraints, fixed/open decisions, decision matrix, success criteria, readiness. |

## Dependencies
- **Input:** [02 Business Analysis](../02-business-analysis/README.md), [03 Business Governance](../03-business-governance/README.md),
  [04 Engineering Analysis](../04-engineering-analysis/README.md).
- **Output:** a prioritized briefing consumed by architecture governance and the architecture phase.

## Entry Criteria
- Business, governance, and engineering analysis approved.

## Exit Criteria
- Report approved; fixed vs open decisions explicit; readiness recommendation given.

## Related Phases
- Directly precedes **06 Architecture Governance** and the future **07 Software Architecture**.

## Next Phase
→ [06 — Architecture Governance](../06-architecture-governance/README.md)
