# 32. Architecture Success Metrics — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Mandatory — measurable success criteria for the architecture deliverable |
| **Purpose** | Define how the *architecture itself* (as a deliverable) is judged successful — distinct from the runtime [Quality Metrics (22)](../03-business-governance/04-quality-metrics.md), which measure the built system. These metrics gate architecture approval. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Scope](#1-purpose--scope)
2. [References](#2-references)
3. [Metric Format](#3-metric-format)
4. [Coverage & Fidelity Metrics](#4-coverage--fidelity-metrics)
5. [Fairness & Correctness Metrics](#5-fairness--correctness-metrics)
6. [Resilience Metrics](#6-resilience-metrics)
7. [Governance & Traceability Metrics](#7-governance--traceability-metrics)
8. [Quality-Attribute & Simplicity Metrics](#8-quality-attribute--simplicity-metrics)
9. [Scorecard](#9-scorecard)
10. [Revision History](#10-revision-history)

---

## 1. Purpose & Scope

These metrics answer: *"Is the architecture good enough to approve?"* They measure the
**architecture artifacts and decisions**, not the running system. Runtime behaviour targets
live in [22 Quality Metrics](../03-business-governance/04-quality-metrics.md); this document verifies the architecture
*plans to* meet them and honors the governance.

## 2. References
- [22 Quality Metrics](../03-business-governance/04-quality-metrics.md), [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md)
- [29 Principles](01-architecture-principles.md), [11 Invariants](../02-business-analysis/10-business-invariants.md), [27 §5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary)

## 3. Metric Format

Each metric: **ID · Description · Measurement · Target · Acceptance Criteria.** Targets are
binary or percentage where the analysis is enumerable (rules, invariants, P1 risks).

## 4. Coverage & Fidelity Metrics

### ASM-01 — Business rule coverage
- **Description:** Every business rule ([03](../02-business-analysis/02-business-rules.md)) has an identified enforcement point in the architecture.
- **Measurement:** Mapped rules ÷ total rules (via [34 Traceability](06-architecture-traceability-matrix.md)).
- **Target:** **100%.**
- **Acceptance:** No `BR-*` without a named responsible component/decision; rules unchanged (AP-02).

### ASM-02 — Invariant enforcement coverage
- **Description:** Every business invariant ([11](../02-business-analysis/10-business-invariants.md)) has a designated enforcement mechanism/boundary.
- **Measurement:** Mapped invariants ÷ total invariants.
- **Target:** **100%.**
- **Acceptance:** Each `INV-*` traceable to where it is guaranteed; none can be violated by a defined transition.

### ASM-03 — Engineering-challenge mapping
- **Description:** Every engineering challenge ([26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)) maps to an architectural response or an explicit deferral.
- **Measurement:** Mapped challenges ÷ 42.
- **Target:** **100% mapped**; **0 unaddressed P1**.
- **Acceptance:** No P1 (RPN band) challenge is unresolved; deferrals justified (e.g., Future Optimization). [27 §5.1/5.3]

### ASM-04 — Requirement traceability
- **Description:** Architecture decisions trace back to business/engineering drivers.
- **Measurement:** Decisions with complete traceability links ÷ total decisions.
- **Target:** **100%.**
- **Acceptance:** Every architecture ADR links to a driver and forward to verification ([34](06-architecture-traceability-matrix.md)).

## 5. Fairness & Correctness Metrics

### ASM-05 — No hidden-information leakage (by design)
- **Description:** No architectural delivery path can send unrevealed ownership to a non-Spymaster.
- **Measurement:** Review of all delivery/projection paths (initial, delta, reconnect, rematch, telemetry) against INV-B9.
- **Target:** **Zero** leak-capable paths.
- **Acceptance:** Every path demonstrably role-filtered at a boundary (AP-05/AP-11, AAP-03). **Non-waivable.**

### ASM-06 — Deterministic outcomes (by design)
- **Description:** Conflict resolution and terminal ordering follow the fixed precedence deterministically.
- **Measurement:** Design conforms to [17 Rule Precedence](../02-business-analysis/16-rule-precedence.md) for all conflict scenarios (RP-1..12).
- **Target:** **100%** of scenarios deterministic.
- **Acceptance:** First-valid-wins and precedence encoded; no timing-dependent outcomes (AP-06, AAP-14).

### ASM-07 — Atomic, valid state (by design)
- **Description:** All state mutations are atomic and pass only through valid transitions.
- **Measurement:** Design review against [08 State Machines](../02-business-analysis/07-state-machines.md) and ENG-ST-01/02.
- **Target:** **No** partial-update or undefined-transition path.
- **Acceptance:** Every mutation has a defined commit boundary and invariant check (AP-08/AP-09).

## 6. Resilience Metrics

### ASM-08 — Recoverable room state (by design)
- **Description:** In-progress matches can recover to a consistent point within the room lifetime.
- **Measurement:** Design addresses ENG-RE-01/ENG-ST-04 with a stated recovery boundary.
- **Target:** **100%** of terminal/critical operations recoverable without replaying terminal effects.
- **Acceptance:** Commit-then-broadcast and idempotent recovery defined (QM-16 alignment).

### ASM-09 — Reconnection preserves gameplay (by design)
- **Description:** Reconnect restores role-appropriate state and resumes paused phases within grace.
- **Measurement:** Design addresses ENG-RT-03 and INV-P5; single active connection (INV-P4).
- **Target:** **100%** of roles/phases have a defined restore path; **zero** leak on restore.
- **Acceptance:** Snapshot filtering and pause-resume specified (AP-05/AP-11).

## 7. Governance & Traceability Metrics

### ASM-10 — Every architectural decision documented
- **Description:** No significant decision is implicit.
- **Measurement:** Decisions recorded as ADRs ÷ decisions identified in review.
- **Target:** **100%.**
- **Acceptance:** Each has context, options, rejection rationale, drivers, consequences ([31 §8](03-architecture-decision-heuristics.md#8-recording-a-decision)).

### ASM-11 — Principle compliance
- **Description:** No design element violates a mandatory principle or triggers an anti-principle without a recorded exception.
- **Measurement:** Review findings against [29](01-architecture-principles.md)/[30](02-architecture-anti-principles.md).
- **Target:** **Zero** unwaived violations; **zero** exceptions for non-waivable items (AAP-02/03/11).
- **Acceptance:** Clean [Review Checklist (33)](05-architecture-review-checklist.md).

### ASM-12 — Technology neutrality of the business core
- **Description:** The rules/adjudication core is expressible independently of transport/storage/UI/language.
- **Measurement:** Review for infrastructure/language leakage into the core (AP-14, ADR-12/13).
- **Target:** **Zero** infrastructure dependencies in the rules core.
- **Acceptance:** Core describable and testable without transport or storage.

## 8. Quality-Attribute & Simplicity Metrics

### ASM-13 — Every quality attribute addressed
- **Description:** Each prioritized quality attribute ([28 §4](../05-architecture-input/01-architecture-input-report.md#4-quality-attribute-priorities)) has an explicit architectural stance.
- **Measurement:** Attributes with a stated approach ÷ total.
- **Target:** **100%.**
- **Acceptance:** Precedence order honored where attributes conflict.

### ASM-14 — No unjustified complexity
- **Description:** Every component/abstraction is justified by a fixed requirement or top quality attribute.
- **Measurement:** Review each element for justification (AP-12, AAP-05/12).
- **Target:** **Zero** unjustified elements.
- **Acceptance:** No speculative generality; deferrable scale complexity is deferred.

### ASM-15 — Future-auth seam present, not built
- **Description:** An additive identity seam exists so authentication can attach later without rule changes.
- **Measurement:** Design shows the seam (AUTH-1..5) and does **not** implement auth.
- **Target:** Seam present; auth **not** built.
- **Acceptance:** Adding auth later requires no change to rules/workflows (AP-13, AAP-12).

## 9. Scorecard

Architecture is **approved** only when all of the following hold:

| Gate | Metric(s) | Threshold |
|------|-----------|-----------|
| **Fidelity** | ASM-01, ASM-02, ASM-12 | 100% / 100% / zero leakage of infra into core |
| **Risk** | ASM-03 | 100% mapped, **0 unaddressed P1** |
| **Fairness (non-waivable)** | ASM-05, ASM-06 | Zero leak paths; fully deterministic |
| **Integrity** | ASM-07, ASM-08, ASM-09 | Atomic/valid; recoverable; leak-free reconnect |
| **Traceability** | ASM-04, ASM-10 | 100% traced & documented |
| **Governance** | ASM-11, ASM-13 | Zero unwaived violations; all QAs addressed |
| **Simplicity/Evolution** | ASM-14, ASM-15 | No unjustified complexity; seam present, auth not built |

> Any red on a **non-waivable** gate (ASM-05, ASM-06, ASM-11 for AAP-02/03/11) blocks approval
> outright, regardless of other scores.

## 10. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial architecture success metrics and approval scorecard. |
