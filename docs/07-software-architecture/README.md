# 07 — Software Architecture

| | |
|---|---|
| **Phase** | Software Architecture |
| **Status** | In progress — **Discovery/Decomposition complete (01–11)**; design decisions next. |
| **Why this phase exists** | To make the actual architectural decisions — how the system is structured to enforce the business, under the governance of phase 06. This is where technology and design choices are first made. It begins with architectural *discovery* (what the architecture must solve) before any design. |
| **Owner** | Lead Architect. |

## Purpose
Produce the system architecture: architectural discovery/decomposition, then component/context
structure, state-ownership realization, concurrency and real-time approach, session/recovery design,
and the architecture decision records — all satisfying the
[Architecture Governance](../06-architecture-governance/README.md).

## Scope
Architecture-level discovery and design/decisions. Detailed component-internal design belongs to
[09 Technical Design](../09-technical-design/README.md).

## Documents — Discovery / Decomposition (complete)
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Architecture Overview](01-architecture-overview.md) | What kind of system, why challenging, qualities. |
| 02 | [Architectural Drivers](02-architectural-drivers.md) | Ranked drivers and their architectural impact. |
| 03 | [System Responsibilities](03-system-responsibilities.md) | Every responsibility (R-01…R-17). |
| 04 | [Responsibility Boundaries](04-responsibility-boundaries.md) | Cohesion, coupling, isolation rules. |
| 05 | [State Ownership Analysis](05-state-ownership.md) | Who owns/reads/modifies each state (S-01…S-10). |
| 06 | [Consistency Boundaries](06-consistency-boundaries.md) | Where strong consistency is required (CB-01…CB-10). |
| 07 | [Command & Query Discovery](07-command-query-discovery.md) | Command/query/mixed classification. |
| 08 | [Interaction Discovery](08-interaction-discovery.md) | Responsibility interactions (I-01…I-10). |
| 09 | [Quality Attribute Scenarios](09-quality-attribute-scenarios.md) | Measurable scenarios (QS-01…QS-14). |
| 10 | [Architecture Readiness Review](10-architecture-readiness-review.md) | Discovery completeness + open design questions. |
| 11 | [Analysis → Architecture Handoff](11-analysis-to-architecture-handoff.md) | Final synthesis: what the architecture must solve. |

## Documents — Design Decisions (in progress)
Architecture ADRs (distinct from the *business* ADRs in [03 Governance](../03-business-governance/02-architecture-decision-records.md))
live in [`12-decisions/`](12-decisions/README.md):
| ADR | Topic | Status |
|-----|-------|--------|
| [ADR-001](12-decisions/ADR-001-overall-architecture-style.md) | Overall Architecture Style | Accepted |

Further designs will address the P1 focus areas identified in the
[handoff](11-analysis-to-architecture-handoff.md#recommended-focus-areas-for-software-architecture).
*(Introduce topic sub-folders once this phase exceeds ~25 documents — see [Scalability Assessment](../_meta/05-scalability-assessment.md).)*

## Dependencies
- **Input:** [05 Architecture Input](../05-architecture-input/README.md), [06 Architecture Governance](../06-architecture-governance/README.md), and all fixed business inputs.
- **Output:** approved architecture + decisions consumed by Software Design.

## Entry Criteria
- Architecture Input approved; governance framework in place.

## Exit Criteria
- Architecture passes the [Review Checklist](../06-architecture-governance/05-architecture-review-checklist.md)
  and [Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md); decisions recorded and traced.

## Related Phases & Next
- Governed by **06**; precedes **08 Software Design**.
→ [08 — Software Design](../08-software-design/README.md)
