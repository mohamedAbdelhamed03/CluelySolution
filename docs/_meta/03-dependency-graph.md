# Documentation Dependency Graph — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Reference |
| **Purpose** | Show how documents depend on one another — both **logical dependency** (what must exist/be true first) and **recommended reading order** — so contributors understand impact of change and newcomers read in a sensible sequence. |

## 1. Lifecycle-level dependency (phases)

```
Product Discovery
      │
      ▼
Business Analysis ──────────────┐
      │                         │ (governance consolidates BA)
      ▼                         ▼
Engineering Analysis      Business Governance
      │                         │
      └───────────┬─────────────┘
                  ▼
          Architecture Input
                  │
                  ▼
        Architecture Governance
                  │
                  ▼
        [Software Architecture]
                  ▼
        [Software Design]
                  ▼
        [Technical Design]
                  ▼
        [Implementation] → [Testing] → [Deployment] → [Operations]
                  ▼
        [Future Evolution]
```

Each phase **depends on** the phases above it: it consumes their approved outputs as fixed inputs.

## 2. Document-level dependency (core chain)

Logical dependency (A → B means "B builds on / must not contradict A"):

```
BRD (why)
  → SRS (what)
     → Business Rules (rules)
        → {Functional Requirements, User Stories, Use Cases}
        → Domain Model → State Machines → Workflows
        → Validation Rules → Business Invariants → Rule Precedence
        → Domain Events → Business Error Catalog
        → Dictionary Management, Lobby/Room Lifecycle, Player Session/Reconnection
           → Consistency Validation Report (validates the above)
              → Business Glossary, Business ADRs, Constants, Quality Metrics,
                Data Lifecycle, Product Roadmap → Governance Validation Summary
                 → Engineering Challenges → Engineering Enrichment
                    → Architecture Input Report
                       → Architecture Principles → Anti-Principles → Decision Heuristics
                          → Success Metrics, Review Checklist, Traceability, Risk Register
                             → Architecture Governance Usage
                                → [Software Architecture] → [Software Design] → [Technical Design]
                                   → [Implementation] → [Testing] → [Deployment] → [Operations]
```

## 3. Key "many depend on this" documents (high-impact — change with care)

| Document | Depended on by | Why |
|----------|----------------|-----|
| [Business Rules](../02-business-analysis/02-business-rules.md) | Nearly everything downstream | The authoritative ruleset. |
| [Business Invariants](../02-business-analysis/10-business-invariants.md) | Rules consumers, engineering, architecture governance | The always-true guarantees. |
| [Rule Precedence](../02-business-analysis/16-rule-precedence.md) | Engineering, architecture, testing | Determinism source. |
| [Glossary](../03-business-governance/01-business-glossary.md) | All documents | Canonical terminology. |
| [Constants Catalog](../03-business-governance/03-business-constants-catalog.md) | All documents citing numbers | Single numeric source. |
| [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) + [Enrichment](../04-engineering-analysis/02-engineering-challenges-enrichment.md) | Architecture Input & Governance | Risk & priority basis. |
| [Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) | Architecture Governance & future architecture | The briefing. |
| [Architecture Decisions (ADR-000…ADR-010)](../07-software-architecture/12-decisions/README.md) | Software Design & all later phases | Binding architecture; ADR-000 is the terminology reference. |
| [Domain Model & Ubiquitous Language](../08-software-design/01-domain-model-and-ubiquitous-language.md) | Module decomposition, technical design, implementation, testing | Canonical domain concepts & aggregate boundaries; later design derives from it. |

> A change to any high-impact document triggers a downstream review of everything that depends on
> it (see [Documentation Governance](04-documentation-governance.md) and
> [Change Management](01-documentation-standards.md#13-change-management)).

## 4. Cross-cutting references (not strict dependencies)

- The **Glossary** and **Constants Catalog** are referenced by (almost) every document but are
  *leaf authorities* — they depend on little and are depended on by much.
- The **Consistency Validation Report** and **Governance Validation Summary** depend on the whole
  business set (they validate it) but nothing depends on them except audit/trust.
- The **Traceability Matrix** links across all phases and is updated by each phase (see its
  [maintenance section](../06-architecture-governance/06-architecture-traceability-matrix.md#7-maintaining-traceability-through-the-lifecycle)).

## 5. Recommended reading order vs logical dependency

- **Reading order** (for humans) is captured per role in [Reading Paths](02-reading-paths.md) and
  by folder numbering (local, in intended reading sequence).
- **Logical dependency** (for impact analysis) is the graph in §2–§3.
- They mostly align; where they differ, follow **reading paths** to learn and the **dependency
  graph** to assess change impact.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial dependency graph for the reorganized documentation. |
