# Reading Paths by Role — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Guidance |
| **Purpose** | Recommend, per role, which documents to read and in what order, so each contributor reaches productivity quickly without reading all 37+ documents. Paths reference the reorganized SDLC folders. |

## How to use
Start at the [Documentation Portal](../README.md). Then follow your role's path below. "→" means
"read next". Optional/deep-dive items are marked *(as needed)*.

Legend of locations: `PD`=01-product-discovery, `BA`=02-business-analysis,
`BG`=03-business-governance, `EA`=04-engineering-analysis, `AI`=05-architecture-input,
`AG`=06-architecture-governance.

---

## Product Manager
Goal: understand vision, scope, and roadmap.
1. [Portal](../README.md) → 2. [BRD](../01-product-discovery/01-business-requirements.md)
→ 3. [Product Roadmap](../03-business-governance/06-product-roadmap.md)
→ 4. [Glossary](../03-business-governance/01-business-glossary.md)
→ 5. [Business Rules](../02-business-analysis/02-business-rules.md) *(as needed for scope calls)*
→ 6. [Governance Validation Summary](../03-business-governance/07-governance-validation-summary.md).

## Business Analyst
Goal: own the business truth end-to-end.
1. [BRD](../01-product-discovery/01-business-requirements.md)
→ 2. [SRS](../02-business-analysis/01-software-requirements.md)
→ 3. [Business Rules](../02-business-analysis/02-business-rules.md)
→ 4. [Functional Requirements](../02-business-analysis/03-functional-requirements.md)
→ 5. [Use Cases](../02-business-analysis/05-use-cases.md) & [User Stories](../02-business-analysis/04-user-stories.md)
→ 6. [Domain Model](../02-business-analysis/06-domain-model.md) → [State Machines](../02-business-analysis/07-state-machines.md) → [Workflows](../02-business-analysis/08-business-workflows.md)
→ 7. [Validation Rules](../02-business-analysis/09-validation-rules.md), [Invariants](../02-business-analysis/10-business-invariants.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md)
→ 8. [Glossary](../03-business-governance/01-business-glossary.md) & [Constants](../03-business-governance/03-business-constants-catalog.md)
→ 9. [Consistency Report](../02-business-analysis/17-consistency-validation-report.md) & [Governance Summary](../03-business-governance/07-governance-validation-summary.md).

## Software Architect
Goal: design within the governance. **Read the Architecture Input first.**
1. [Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) *(the briefing)*
→ 2. [Business Rules](../02-business-analysis/02-business-rules.md), [Invariants](../02-business-analysis/10-business-invariants.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md) *(fixed inputs)*
→ 3. [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) + [Enrichment](../04-engineering-analysis/02-engineering-challenges-enrichment.md) *(priorities)*
→ 4. [Architecture Principles](../06-architecture-governance/01-architecture-principles.md) & [Anti-Principles](../06-architecture-governance/02-architecture-anti-principles.md)
→ 5. [Decision Heuristics](../06-architecture-governance/03-architecture-decision-heuristics.md)
→ 6. [Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md), [Review Checklist](../06-architecture-governance/05-architecture-review-checklist.md), [Traceability](../06-architecture-governance/06-architecture-traceability-matrix.md), [Risk Register](../06-architecture-governance/07-architecture-risk-register.md)
→ 7. [Governance Usage](../06-architecture-governance/08-architecture-governance-usage.md)
→ 8. [Domain Model](../02-business-analysis/06-domain-model.md) & [State Machines](../02-business-analysis/07-state-machines.md) *(structure)*
→ 9. Then produce/consume the decisions: [ADR-000…ADR-010](../07-software-architecture/12-decisions/README.md) → the [Software-Design Domain Model & Ubiquitous Language](../08-software-design/01-domain-model-and-ubiquitous-language.md)
→ 10. [Constants](../03-business-governance/03-business-constants-catalog.md), [Data Lifecycle](../03-business-governance/05-data-lifecycle-retention.md), [Quality Metrics](../03-business-governance/04-quality-metrics.md).

## Backend Developer
Goal: implement the authoritative rules correctly.
1. [Portal](../README.md) → 2. [Business Rules](../02-business-analysis/02-business-rules.md)
→ 3. [State Machines](../02-business-analysis/07-state-machines.md) & [Workflows](../02-business-analysis/08-business-workflows.md)
→ 4. [Validation Rules](../02-business-analysis/09-validation-rules.md), [Error Catalog](../02-business-analysis/12-business-error-catalog.md), [Domain Events](../02-business-analysis/11-domain-events-catalog.md)
→ 5. [Invariants](../02-business-analysis/10-business-invariants.md) & [Rule Precedence](../02-business-analysis/16-rule-precedence.md)
→ 6. [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) (esp. P1 in [Enrichment §5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary))
→ 7. [Constants](../03-business-governance/03-business-constants-catalog.md), [Session & Reconnection](../02-business-analysis/15-player-session-reconnection.md), [Lobby & Room Lifecycle](../02-business-analysis/14-lobby-room-lifecycle.md)
→ 8. [Architecture Decisions (ADR-000…ADR-010)](../07-software-architecture/12-decisions/README.md) and the [Domain Model & Ubiquitous Language](../08-software-design/01-domain-model-and-ubiquitous-language.md); Technical Design *(when produced)*.

## Frontend / Mobile Developer
Goal: build a client that renders authoritative, role-filtered state.
1. [Portal](../README.md) → 2. [Functional Requirements](../02-business-analysis/03-functional-requirements.md) & [User Stories](../02-business-analysis/04-user-stories.md)
→ 3. [Use Cases](../02-business-analysis/05-use-cases.md) → 4. [State Machines](../02-business-analysis/07-state-machines.md) (turn/room phases)
→ 5. [Domain Events](../02-business-analysis/11-domain-events-catalog.md) & [Error Catalog](../02-business-analysis/12-business-error-catalog.md) (what to display)
→ 6. [Session & Reconnection](../02-business-analysis/15-player-session-reconnection.md) (reconnect UX)
→ 7. hidden-information constraint in [Invariants](../02-business-analysis/10-business-invariants.md#inv-b9--unrevealed-ownership-is-never-disclosed-to-operatives) *(never render unrevealed ownership)*.

## QA Engineer
Goal: verify correctness, fairness, concurrency, recovery.
1. [Portal](../README.md) → 2. [Business Rules](../02-business-analysis/02-business-rules.md) & [Validation Rules](../02-business-analysis/09-validation-rules.md)
→ 3. [Invariants](../02-business-analysis/10-business-invariants.md) & [Rule Precedence](../02-business-analysis/16-rule-precedence.md) (the oracle for correctness)
→ 4. [Use Cases](../02-business-analysis/05-use-cases.md) & [Workflows](../02-business-analysis/08-business-workflows.md) (scenarios)
→ 5. [Error Catalog](../02-business-analysis/12-business-error-catalog.md) (expected rejections)
→ 6. [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) + [Enrichment](../04-engineering-analysis/02-engineering-challenges-enrichment.md) (testing considerations & validation scenarios per challenge)
→ 7. [Quality Metrics](../03-business-governance/04-quality-metrics.md) (measurable targets).

## DevOps / Operations Engineer
Goal: run and observe the system.
1. [Portal](../README.md) → 2. [Quality Metrics](../03-business-governance/04-quality-metrics.md) (availability, latency, recovery targets)
→ 3. [Data Lifecycle & Retention](../03-business-governance/05-data-lifecycle-retention.md)
→ 4. [Domain Events](../02-business-analysis/11-domain-events-catalog.md) (observability signals, PII-free)
→ 5. scalability items in [Engineering Challenges §15](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md#15-scalability-risks)
→ 6. Deployment & Operations folders *(when produced)*.

## Technical Writer
Goal: maintain the documentation system.
1. [Portal](../README.md) → 2. [Documentation Standards](01-documentation-standards.md)
→ 3. [Glossary](../03-business-governance/01-business-glossary.md) (canonical terminology)
→ 4. [Dependency Graph](03-dependency-graph.md) & [Documentation Governance](04-documentation-governance.md)
→ 5. [Migration & Structure Record](06-migration-plan-and-record.md) (how the system is organized).

## New Team Member (any role)
Goal: orient in under an hour.
1. [Portal](../README.md) → 2. [BRD](../01-product-discovery/01-business-requirements.md) (the product)
→ 3. [Glossary](../03-business-governance/01-business-glossary.md) (the language)
→ 4. [Business Rules](../02-business-analysis/02-business-rules.md) (the game)
→ 5. [Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) (where the project is)
→ 6. then branch to your role's path above.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial role-based reading paths for the reorganized structure. |
