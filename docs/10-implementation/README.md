# 10 — Implementation

| | |
|---|---|
| **Phase** | Implementation |
| **Why this phase exists** | To document how the system is built: developer setup, coding standards, and module guides — the reference for contributors writing code. |
| **Owner** | Engineering Lead. |

## Purpose
Developer-facing documentation supporting construction of the designed system.

## Scope
Implementation guidance and standards; not architecture/design decisions, not business rules.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Simplicity Principles](simplicity-principles.md) | Mandatory simplicity-focused coding philosophy. |
| 02 | [Refactoring Policy](refactoring-policy.md) | Policy for controlled, reviewable refactoring. |
| 03 | [Implementation Planning Standard](implementation-planning-standard.md) | Mandatory planning process before writing code. |
| 04 | [Implementation Decision Log](implementation-decision-log.md) | Log of implementation-level decisions (not ADRs). |
| 05 | [Global Engineering Implementation Standard](global-engineering-implementation-standard.md) | Mandatory engineering workflow for every implementation phase. |
| 06 | [Definition of Done](definition-of-done.md) | Mandatory criteria for completion of any implementation task. |
| 07 | [Technical Debt Policy](technical-debt-policy.md) | Policy for managing and documenting technical debt. |
| 08 | [Backend Release Candidate Report](backend-release-candidate-report.md) | RC1 production-readiness, security, performance, and accepted-debt assessment. |
| 09 | [Backend RC1 Release Notes](backend-rc1-release-notes.md) | Deployment-facing summary of RC1 changes and compatibility. |

## Dependencies
- **Input:** [09 Technical Design](../09-technical-design/README.md).
- **Output:** built system + guides consumed by Testing and Operations.

## Entry / Exit Criteria
- **Entry:** designs approved. **Exit:** implementation guides current; code traceable to designs.

## Related Phases & Next
- Follows **09**; precedes **11 Testing**.
→ [11 — Testing](../11-testing/README.md)
