# 02 — Business Analysis

| | |
|---|---|
| **Phase** | Business Analysis |
| **Why this phase exists** | To specify *what* the system must do and *what must always be true* — the complete, unambiguous business behaviour that faithfully reproduces Codenames. This is the authoritative business source of truth. |
| **Owner** | Business Analyst (reviewers: Product Owner, Engineering Lead). |

## Purpose
Define requirements, rules, domain model, states, workflows, validations, invariants, events,
errors, and the sub-specifications (dictionary, lobby/room, session/reconnection, rule precedence),
plus the consistency validation that proves it all coheres.

## Scope
All business behaviour and rules. Technology-neutral. No architecture or implementation.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Software Requirements (SRS)](01-software-requirements.md) | IEEE-style functional & non-functional requirements + logical component view. |
| 02 | [Business Rules](02-business-rules.md) | Every rule, validation, and state transition. |
| 03 | [Functional Requirements](03-functional-requirements.md) | Each feature: flows, pre/postconditions, failures. |
| 04 | [User Stories](04-user-stories.md) | Stories per actor with acceptance criteria. |
| 05 | [Use Cases](05-use-cases.md) | Detailed use cases with alternative/exception flows. |
| 06 | [Domain Model](06-domain-model.md) | Entities, responsibilities, relationships, constraints. |
| 07 | [State Machines](07-state-machines.md) | Lifecycle states/transitions for every stateful entity. |
| 08 | [Business Workflows](08-business-workflows.md) | End-to-end process flows. |
| 09 | [Validation Rules](09-validation-rules.md) | Every validation, reason, and business outcome. |
| 10 | [Business Invariants](10-business-invariants.md) | Conditions that must always hold. |
| 11 | [Domain Events Catalog](11-domain-events-catalog.md) | Business events: trigger, publisher, consumers, payload. |
| 12 | [Business Error Catalog](12-business-error-catalog.md) | Business error codes, causes, messages, recovery. |
| 13 | [Dictionary Management](13-dictionary-management.md) | Regional dictionary lifecycle, versioning, validation. |
| 14 | [Lobby & Room Lifecycle](14-lobby-room-lifecycle.md) | Pre/between-match behaviour, host migration, rematch, expiry. |
| 15 | [Player Session & Reconnection](15-player-session-reconnection.md) | Temporary identity, disconnect/reconnect, grace, pause, abandonment. |
| 16 | [Rule Precedence](16-rule-precedence.md) | Deterministic ordering when rules coincide. |
| 17 | [Consistency Validation Report](17-consistency-validation-report.md) | Findings across the business set (advisory). |

## Dependencies
- **Input:** [BRD](../01-product-discovery/01-business-requirements.md).
- **Output:** the authoritative business behaviour consumed by Governance, Engineering, and
  Architecture.

## Entry Criteria
- BRD approved.

## Exit Criteria
- All business behaviour specified, internally consistent (per the Consistency Report), and approved.

## Related Phases
- Consolidated by **03 Business Governance**; risk-analyzed by **04 Engineering Analysis**;
  constrains all architecture.

## Next Phase
→ [03 — Business Governance](../03-business-governance/README.md)
