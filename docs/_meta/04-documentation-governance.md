# Documentation Governance — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Recommendations |
| **Purpose** | Recommend how the documentation system is governed as it grows: ownership, review cadence, quality gates, and how structural integrity (links, numbering, traceability) is preserved. Complements the [Documentation Standards](01-documentation-standards.md). |

## 1. Ownership model

| Area | Accountable owner | Reviewer(s) |
|------|-------------------|-------------|
| Product Discovery | Product Owner | Business Analyst |
| Business Analysis | Business Analyst | Product Owner, Engineering Lead |
| Business Governance | Business Analyst / Product Owner | Content team (dictionaries) |
| Engineering Analysis | Engineering Lead | Lead Architect |
| Architecture Input & Governance | Lead Architect | Engineering Lead, Business Analyst |
| Software Architecture → Operations *(future)* | Respective phase lead | Cross-functional per phase |
| `_meta` (this system) | Documentation / Tech-Writing | Lead Architect |

Each document names (or inherits from its folder) an accountable owner who approves changes.

## 2. Review cadence & quality gates

- **On authoring/change:** the [review & approval workflow](01-documentation-standards.md#9-review--approval-workflow) applies.
- **At phase gates:** before a phase is declared complete, its folder README **Exit Criteria** must
  be met and (for architecture) the [Architecture Review Checklist](../06-architecture-governance/05-architecture-review-checklist.md)
  and [Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md) must pass.
- **Periodic health check (recommended monthly / at each milestone):**
  - Link integrity (no broken internal links).
  - Numbering integrity (contiguous local numbering; no duplicate `NN`).
  - Every folder has a current README.
  - Glossary/Constants remain the single source (no duplicated definitions/values).
  - Traceability matrix has no orphans/gaps ([LR-4/LR-5](../06-architecture-governance/06-architecture-traceability-matrix.md#4-linking-rules)).

## 3. Structural-integrity rules

- **Single source of truth:** definitions live once (Glossary), numbers live once (Constants);
  everything else links. Duplicates are a governance defect.
- **Move = move + relink + validate:** any relocation updates all inbound links and is validated
  (the method recorded in the [Migration Record](06-migration-plan-and-record.md)).
- **IDs are immutable:** `BR-/INV-/EVT-/ENG-/AP-/ASM-…` are never reused or renumbered.
- **Fixed inputs are immutable downstream:** gameplay, rules, invariants, constants change only at
  their source via the Business Analyst, with downstream traceability updated.

## 4. Change impact & propagation

- Use the [Dependency Graph](03-dependency-graph.md) to identify what a change affects.
- A change to a **high-impact** document triggers review of all dependents.
- Cross-phase changes update the [Traceability Matrix](../06-architecture-governance/06-architecture-traceability-matrix.md)
  and, where relevant, the [Architecture Risk Register](../06-architecture-governance/07-architecture-risk-register.md).

## 5. Approval workflow (summary)

```
Draft → In Review → Approved/Mandatory
                     │
        (supersede) → old marked Superseded, successor linked
```
Governance/architecture documents additionally clear their checklist before Approved.

## 6. Deprecation & archival

- Follow the [Deprecation Policy](01-documentation-standards.md#12-deprecation-policy): mark, link a
  successor, retain for history; optionally move to a phase-local `archive/` after a cool-down.
- Never delete a document that others link to without updating those links in the same change.

## 7. Tooling recommendations (non-mandatory)

- A link-checker run in the periodic health check (the reorganization used a scripted validator
  that confirmed 0 broken internal links).
- A simple numbering/README presence check per folder.
- These are process aids; the standards themselves are tool-agnostic.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial documentation-governance recommendations. |
