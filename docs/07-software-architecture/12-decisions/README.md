# 07.12 — Architecture Decision Records (ADRs)

| | |
|---|---|
| **Purpose** | Hold the **architecture** decision records produced during the Software Architecture phase — the permanent, traceable answers to the open architectural questions from [Discovery](../10-architecture-readiness-review.md#2-unresolved-questions-for-the-architecture-design-work). |
| **Owner** | Lead Architect. |
| **Naming exception** | Files use the `ADR-NNN-topic.md` convention (an accepted industry standard) rather than the folder-local `NN-` numbering, so each ADR's stable identifier is visible in the filename. This is the documented exception permitted by the [Documentation Standards](../../_meta/01-documentation-standards.md). |

## Relationship to other documents
- These are **architecture** ADRs — distinct from the **business/product** ADRs in
  [03 Business Governance](../../03-business-governance/02-architecture-decision-records.md).
- Every ADR here is made **under** the [Architecture Governance](../../06-architecture-governance/README.md)
  (principles, anti-principles, heuristics, success metrics, review checklist) and **must not**
  contradict the frozen business analysis.

## Index
| ADR | Topic | Status |
|-----|-------|--------|
| [ADR-000](ADR-000-architecture-vocabulary.md) | Architecture Vocabulary & Canonical Definitions | Accepted |
| [ADR-001](ADR-001-overall-architecture-style.md) | Overall Architecture Style | Accepted |
| [ADR-002](ADR-002-authoritative-game-state.md) | Authoritative Game State | Accepted |
| [ADR-003](ADR-003-per-room-coordination-model.md) | Per-Room Coordination Model | Accepted |
| [ADR-004](ADR-004-real-time-communication-delivery.md) | Real-Time Communication & Delivery Architecture | Accepted |
| [ADR-005](ADR-005-state-recovery-resilience.md) | State Recovery & Resilience Architecture | Accepted |
| [ADR-006](ADR-006-role-based-information-visibility.md) | Role-Based Information Visibility & Projection Architecture | Accepted |
| [ADR-007](ADR-007-room-isolation-distribution.md) | Room Isolation & Distribution Architecture | Accepted |
| [ADR-008](ADR-008-dictionary-content-architecture.md) | Dictionary & Content Architecture | Accepted |
| [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md) | Participant Lifecycle, Presence & Session Continuity | Accepted |
| [ADR-010](ADR-010-command-query-strategy.md) | Command / Query Strategy Architecture | Accepted |

> **ADR-000** is the mandatory terminology reference for every ADR and architecture/design/
> implementation document. Though numbered 000, it was authored after ADR-001 (it standardizes the
> vocabulary that emerged during discovery and the first decision). No future architecture document
> may introduce new terminology without first updating ADR-000.

*(Future ADRs — e.g., authoritative game state, per-room coordination, real-time communication,
state recovery, role-based visibility, room isolation, dictionary architecture, session &
reconnection, command/query strategy — will be added here as they are decided.)*

## Dependencies
- **Input:** [Architecture Discovery (01–11)](../README.md), [Architecture Governance](../../06-architecture-governance/README.md).
- **Output:** binding decisions that future design, technical design, and implementation must follow.
