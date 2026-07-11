# 36. Architecture Governance Framework — Usage Summary

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Framework summary — how to operate the architecture governance (docs 29–35) |
| **Purpose** | Explain how the governance documents are used during (and after) the Software Architecture phase: mandatory references, how reviews run, how changes are approved, and how traceability is preserved end-to-end. This is the operating manual for the "constitution of architecture." |
| **Technology** | Neutral. |

## Table of Contents
1. [The Governance Set at a Glance](#1-the-governance-set-at-a-glance)
2. [How the Documents Are Used During Architecture](#2-how-the-documents-are-used-during-architecture)
3. [Mandatory References for Architects](#3-mandatory-references-for-architects)
4. [How Architecture Reviews Are Performed](#4-how-architecture-reviews-are-performed)
5. [How Architectural Changes Are Approved](#5-how-architectural-changes-are-approved)
6. [Preserving Traceability Across the Lifecycle](#6-preserving-traceability-across-the-lifecycle)
7. [Guarantee Statement](#7-guarantee-statement)
8. [Revision History](#8-revision-history)

---

## 1. The Governance Set at a Glance

| Doc | Role in governance | Use it to… |
|-----|--------------------|-----------|
| [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) | The briefing (input) | Understand drivers, constraints, fixed vs open, success criteria. |
| [29 Principles](01-architecture-principles.md) | What every design **must** do | Justify decisions; the mandatory rulebook. |
| [30 Anti-Principles](02-architecture-anti-principles.md) | What designs **must not** do | Red-flag detection in review. |
| [31 Decision Heuristics](03-architecture-decision-heuristics.md) | **How** to decide & record | Make and document decisions consistently. |
| [32 Success Metrics](04-architecture-success-metrics.md) | **Measurable** approval gates | Judge whether the architecture is good enough. |
| [33 Review Checklist](05-architecture-review-checklist.md) | The **gate** | Run the review; produce Pass/Fail/Waived. |
| [34 Traceability Matrix](06-architecture-traceability-matrix.md) | The **linkage** | Keep business→…→ops connected. |
| [35 Risk Register](07-architecture-risk-register.md) | Solution-practice **risks** | Catch over-engineering, wrong boundaries, leaks. |

Together they ensure every architectural decision stays aligned with the approved business
([00–25](../_meta/00-canonical-constants-and-index.md)) and engineering analysis ([26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md)).

## 2. How the Documents Are Used During Architecture

The normal working loop for the architecture team:

1. **Read the briefing** — start from [28](../05-architecture-input/01-architecture-input-report.md): what must be solved, what's fixed, what's open.
2. **Pick an open question** — from [28 §9](../05-architecture-input/01-architecture-input-report.md#9-open-architectural-questions) / a P1 challenge in [27 §5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary).
3. **Decide with heuristics** — apply [31](03-architecture-decision-heuristics.md)'s decision loop; check against [29](01-architecture-principles.md) and [30](02-architecture-anti-principles.md).
4. **Record the decision** — as an architecture ADR ([31 §8](03-architecture-decision-heuristics.md#8-recording-a-decision)); add its links to [34](06-architecture-traceability-matrix.md).
5. **Scan for risk** — check the new decision against [35](07-architecture-risk-register.md); add any new risk.
6. **Self-check against metrics** — is progress toward the [32](04-architecture-success-metrics.md) gates?
7. **Review** — when a coherent architecture (or change) is ready, run [33](05-architecture-review-checklist.md).

## 3. Mandatory References for Architects

Before designing anything, every architect must have read and must continuously honor:

- **Fixed inputs (never change):** [03 Business Rules](../02-business-analysis/02-business-rules.md), [11 Invariants](../02-business-analysis/10-business-invariants.md), [17 Rule Precedence](../02-business-analysis/16-rule-precedence.md), [19 Glossary](../03-business-governance/01-business-glossary.md), [21 Constants](../03-business-governance/03-business-constants-catalog.md), [20 Business ADRs](../03-business-governance/02-architecture-decision-records.md).
- **Problem framing:** [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md).
- **Governance (this framework):** [29](01-architecture-principles.md), [30](02-architecture-anti-principles.md), [31](03-architecture-decision-heuristics.md), [32](04-architecture-success-metrics.md), [33](05-architecture-review-checklist.md), [34](06-architecture-traceability-matrix.md), [35](07-architecture-risk-register.md).

> The **non-waivable** anchors: rules immutable ([AP-02](01-architecture-principles.md), AAP-11),
> fairness/no-leak ([AP-05/AP-11](01-architecture-principles.md), AAP-03), server authority
> (AAP-02), determinism ([AP-06](01-architecture-principles.md), AAP-14).

## 4. How Architecture Reviews Are Performed

1. **Entry condition:** a documented architecture (or change) with ADRs and updated [traceability (34)](06-architecture-traceability-matrix.md).
2. **Run the checklist:** work through [33](05-architecture-review-checklist.md) sections A–G; mark each Pass / Fail / Waived.
3. **Score the metrics:** complete the [32 §9 scorecard](04-architecture-success-metrics.md#9-scorecard).
4. **Review the risks:** re-score [35](07-architecture-risk-register.md); confirm no open Critical/Non-waivable risk.
5. **Panel:** Lead Architect + BA (fidelity) + Engineering Lead (risk) + Product Owner (scope) sign off ([33 Sign-Off](05-architecture-review-checklist.md#sign-off)).
6. **Outcome:** **Approve** (all non-waivable Pass, scorecard green), **Revise** (fixable findings), or **Reject** (fundamental conflict).
7. **Record:** findings, waivers (with rationale + expiry), and decisions are archived and linked in [34](06-architecture-traceability-matrix.md).

**Non-waivable failures that block approval outright:** any rule change (A1/AAP-11), any
hidden-info leak path (B1/AAP-03), any client-trusted authority (B2/AAP-02), any
non-deterministic outcome (C4/AAP-14), or any unaddressed **P1** engineering challenge (G3).

## 5. How Architectural Changes Are Approved

Changes after initial approval follow the same governance, scaled to impact:

| Change size | Process |
|-------------|---------|
| **Minor** (no principle/rule/boundary impact) | New/updated ADR + traceability update; lightweight review against [33](05-architecture-review-checklist.md) relevant items. |
| **Significant** (affects boundaries, state ownership, concurrency, delivery) | Full [33](05-architecture-review-checklist.md) + [32](04-architecture-success-metrics.md) re-score + [35](07-architecture-risk-register.md) re-scan; panel sign-off. |
| **Touches a fixed decision** ([28 §10](../05-architecture-input/01-architecture-input-report.md#10-fixed-decisions)) | **Not an architecture decision** — escalate to Product/BA; architecture cannot change fixed items (AAP-11). |
| **Needs a business/parameter clarification** ([28 §9](../05-architecture-input/01-architecture-input-report.md#9-open-architectural-questions)) | Route to BA/PO; proceed once the parameter is set; record as a dependency. |

Every change: update the ADR status (supersede, don't erase), propagate links ([34 LR-6](06-architecture-traceability-matrix.md)), and re-run the affected checklist items.

## 6. Preserving Traceability Across the Lifecycle

Traceability is maintained continuously via [34](06-architecture-traceability-matrix.md), one column per phase:

```
Business Analysis → Engineering Analysis → Software Architecture → Technical Design → Implementation → Testing → Operations
     (anchors)          (challenges)          (decisions)            (design)          (work items)     (suites)     (signals)
```

- **Business & Engineering Analysis** — the fixed anchors (rules, invariants, drivers, challenges); already complete.
- **Software Architecture** — populate the *Architecture Decision* column; each decision carries a [§6 record (34)](06-architecture-traceability-matrix.md#6-per-decision-traceability-record-template).
- **Technical Design** — extend decisions into design elements; link design→decision.
- **Implementation** — fill the *Implementation* column with work-item references; keep links current as code changes.
- **Testing** — fill the *Testing* column; each suite references the driver/decision it verifies ([22 QM-*](../03-business-governance/04-quality-metrics.md)).
- **Operations** — fill the *Operations* column; each runtime signal/runbook references what it confirms ([12 Events](../02-business-analysis/11-domain-events-catalog.md)/[22](../03-business-governance/04-quality-metrics.md)).

**Health rules** (checked every review): no orphan decisions (LR-4), no unaddressed drivers
(LR-5), 100% rule/invariant/P1 coverage (LR-3), change propagation on any edit (LR-6). Owners:
Lead Architect during architecture; each phase lead thereafter.

## 7. Guarantee Statement

Operated as described, this framework guarantees that **every architectural decision**:

- implements the approved business **exactly** (rules/invariants/precedence immutable);
- protects **fairness and hidden information** without exception;
- is **deterministic, correct, and recoverable** by design;
- is the **simplest** option meeting the MVP and top quality attributes, with **additive seams** for future evolution;
- is **documented, traceable, and reviewed**; and
- is checked against known **solution-practice risks**.

It is the official bridge from the approved Business and Engineering analysis into Software
Architecture — and the standing governance that keeps Technical Design, Implementation, Testing,
and Operations aligned with them.

## 8. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial governance framework usage summary tying documents 29–35 into an operating model. |
