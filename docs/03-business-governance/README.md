# 03 — Business Governance

| | |
|---|---|
| **Phase** | Business Governance (product foundation) |
| **Why this phase exists** | To consolidate the business into single-source authorities (terminology, constants), record the *why* behind product decisions, make quality measurable, define data lifecycle, and set the roadmap — elevating the business from "complete" to "enterprise-grade". |
| **Owner** | Business Analyst / Product Owner (dictionaries: Content team). |

## Purpose
Provide the canonical glossary and constants, the business/product decision records, measurable
quality expectations, the data lifecycle/retention policy, the roadmap (current vs future), and the
governance validation that confirms these complement the business set.

## Scope
Business-level governance and foundations. Technology-neutral. Not architecture governance (that is
phase 06).

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Business Glossary](01-business-glossary.md) | Canonical, authoritative terminology. |
| 02 | [Architecture Decision Records (Business)](02-architecture-decision-records.md) | The *why* behind fixed product/architecture decisions. |
| 03 | [Business Constants Catalog](03-business-constants-catalog.md) | Single authoritative source for numeric values. |
| 04 | [Quality Metrics](04-quality-metrics.md) | Measurable non-functional targets. |
| 05 | [Data Lifecycle & Retention](05-data-lifecycle-retention.md) | Business lifecycle of every object (PII-free). |
| 06 | [Product Roadmap](06-product-roadmap.md) | Current vs future scope; deferred evolution. |
| 07 | [Governance Validation Summary](07-governance-validation-summary.md) | Confirms governance complements the business set. |

## Dependencies
- **Input:** [02 Business Analysis](../02-business-analysis/README.md).
- **Output:** canonical terminology/constants and governance consumed by every later phase.

## Entry Criteria
- Business Analysis approved.

## Exit Criteria
- Glossary and Constants established as single sources; roadmap and quality metrics approved;
  governance validation clean.

## Related Phases
- Referenced by **all** phases (terminology/constants); feeds **04 Engineering Analysis** and
  architecture.

## Next Phase
→ [04 — Engineering Analysis](../04-engineering-analysis/README.md)
