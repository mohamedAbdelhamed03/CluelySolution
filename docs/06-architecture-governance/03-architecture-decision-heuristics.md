# 31. Architecture Decision Heuristics — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Guidance — how to make architectural decisions (not the decisions) |
| **Purpose** | Give architects practical, repeatable guidance for making and recording decisions consistently with the approved analysis. Heuristics, not rigid rules — they help judgment, they don't replace it. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [The Decision Loop](#3-the-decision-loop)
4. [Questions to Ask (Every Decision)](#4-questions-to-ask-every-decision)
5. [Evaluation Criteria](#5-evaluation-criteria)
6. [Decision Drivers & Their Weights](#6-decision-drivers--their-weights)
7. [Tie-Break Heuristics: When Each Attribute Wins](#7-tie-break-heuristics-when-each-attribute-wins)
8. [Recording a Decision](#8-recording-a-decision)
9. [Common Decision Situations](#9-common-decision-situations)
10. [Revision History](#10-revision-history)

---

## 1. Purpose & Usage

Two architects, given the same problem and these heuristics, should reach compatible decisions
and record them the same way. Use this document while deciding; use [33 Review Checklist](05-architecture-review-checklist.md)
to validate the result. All decisions must respect [29 Principles](01-architecture-principles.md)
and avoid [30 Anti-Principles](02-architecture-anti-principles.md).

## 2. References
- [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) (§4 QA priorities, §8 trade-offs, §9 open questions)
- [29 Principles](01-architecture-principles.md), [30 Anti-Principles](02-architecture-anti-principles.md)
- [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md) Engineering Analysis

## 3. The Decision Loop

For each architectural decision, follow this loop (lightweight for small decisions, thorough
for P1 ones):

1. **Frame** — state the decision and which [open question (28 §9)](../05-architecture-input/01-architecture-input-report.md#9-open-architectural-questions) or challenge it addresses.
2. **Constrain** — list the fixed decisions, invariants, and principles that bound it.
3. **Options** — enumerate candidate approaches (business-neutral; do not pre-pick).
4. **Evaluate** — score against the criteria (§5) and drivers (§6).
5. **Decide** — choose; note why alternatives were rejected.
6. **Record** — capture as an ADR-style entry (§8) and update the [Traceability Matrix (34)](06-architecture-traceability-matrix.md).
7. **Review** — validate against [33](05-architecture-review-checklist.md).

## 4. Questions to Ask (Every Decision)

- Which **business rule/invariant** does this touch, and does the option preserve it exactly? [11, 03]
- Does it keep the **server authoritative** and **hidden information protected**? [AP-03/AP-05]
- Is the outcome **deterministic** under concurrency and network faults? [AP-06, 17]
- Does it introduce **shared mutable state** or **cross-room coupling**? [AAP-08]
- Is this the **simplest** option that meets the MVP and top quality attributes? [AP-12]
- Does it preserve **additive seams** for future auth/phases without building them now? [AP-13, AAP-12]
- Is it **testable** (correctness, concurrency, fairness, recovery)? [AP-20]
- What **fails** if this is wrong, and how would we **detect** it? [35 Risk Register]
- Does it require a **business/parameter clarification** (route to BA/PO) rather than an architecture call? [28 §9]
- Which **anti-principles** could this accidentally trigger? [30]

## 5. Evaluation Criteria

Score each option (qualitatively) on:

| Criterion | Question |
|-----------|----------|
| **Rule fidelity** | Does it implement the approved rules exactly, without altering them? |
| **Fairness/hidden-info** | Can hidden information leak on any path? Is it deterministic? |
| **Correctness/consistency** | Are mutations atomic; is there one source of truth? |
| **Reliability/recovery** | Does it recover to a consistent state within room lifetime? |
| **Simplicity** | Is it the least complex option that suffices? |
| **Testability** | Can we verify it deterministically and under faults? |
| **Evolvability** | Does it keep future seams additive without over-building? |
| **Operational fit** | Is it simple to run and observe for the MVP? |

## 6. Decision Drivers & Their Weights

Default weighting for the MVP (higher = more influence), consistent with [28 §4](../05-architecture-input/01-architecture-input-report.md#4-quality-attribute-priorities):

| Weight | Drivers |
|--------|---------|
| **Highest** | Correctness, Gameplay Fairness, Rule Fidelity |
| **High** | Consistency, Reliability/Recoverability, Security (integrity) |
| **Medium** | Testability, Performance/Latency, Availability |
| **Lower (MVP)** | Maintainability, Extensibility, Observability |
| **Lowest (MVP, keep bounded)** | Scalability, Operational Simplicity beyond MVP needs |

> "Lower/Lowest" does **not** mean ignore — it means *don't over-invest now*; keep them
> satisfied enough and cheap to grow later.

## 7. Tie-Break Heuristics: When Each Attribute Wins

Practical guidance for the common tensions in [28 §8](../05-architecture-input/01-architecture-input-report.md#8-major-trade-offs):

- **When correctness should win:** Any time an option trades a correct/fair outcome for speed, memory, or simplicity — correctness/fairness win, always. (AP-04/AP-05). *No exceptions for AAP-02/03/11.*
- **When simplicity should win:** When two options both fully satisfy rule fidelity, fairness, correctness, and reliability, choose the simpler one — even if the complex one scales better on paper (defer that). (AP-12)
- **When maintainability should win:** When a choice affects long-term change cost but not correctness/fairness — favor the more cohesive, loosely-coupled option (AP-15/16), *provided it isn't speculative generality* (AAP-05/12).
- **When scalability should win:** Only when there is a **measured or firmly-anticipated** MVP-relevant load that a simpler option cannot meet; otherwise keep footprints bounded and defer (ENG-SC-* = Future Optimization). Never adopt cross-room shared mutable state to scale (AAP-08).
- **When correctness AND performance both matter:** Meet correctness first, then optimize within that envelope against a measured target (AAP-06, [32 Metrics](04-architecture-success-metrics.md)).
- **When future extensibility should win:** When a small, cheap seam today prevents a large rewrite later (esp. authentication) — add the *seam*, not the feature (AP-13). If the "extensibility" is speculative or costly now, defer (AAP-12).
- **When consistency vs availability tension arises (in-room state):** Favor consistency/fairness for authoritative game state; degraded availability is preferable to inconsistent or leaked state (AP-05/AP-07, [28 §8](../05-architecture-input/01-architecture-input-report.md#8-major-trade-offs)).
- **When memory vs persistence tension arises:** Provide only **room-lifetime** recoverability required by the analysis; do not build long-term durability the MVP doesn't need (ENG-RE-01, AP-12).

## 8. Recording a Decision

Every non-trivial decision is recorded in the same shape as an [ADR (20)](../03-business-governance/02-architecture-decision-records.md)
(these will be *architecture* ADRs, distinct from the *business* ADRs already fixed):

- **ID · Title · Status** (Proposed / Accepted / Superseded)
- **Context** (problem, the open question/challenge it addresses)
- **Constraints honored** (rules, invariants, principles)
- **Options considered** and **why alternatives were rejected**
- **Decision** and **decision drivers** (ranked, §6)
- **Consequences** and **traceability links** ([34](06-architecture-traceability-matrix.md))
- **Detection/verification** (how we'll know it's right — ties to [32](04-architecture-success-metrics.md)/[33](05-architecture-review-checklist.md))

> Recording is mandatory: an undocumented decision fails the [Review Checklist (33)](05-architecture-review-checklist.md)
> and the [Success Metrics (32)](04-architecture-success-metrics.md).

## 9. Common Decision Situations

Illustrative *framing* only — this document does not decide them (they are open questions for
architecture):

- **State ownership & atomicity** — frame against AP-07/AP-08/AP-09; verify no partial updates (ENG-ST-02).
- **Concurrency coordination** — frame against AP-06 determinism and [17 precedence](../02-business-analysis/16-rule-precedence.md); verify first-valid-wins (ENG-GP-01).
- **Real-time sync (full vs delta)** — frame against consistency and testability; capture the open question [28 §9(7)](../05-architecture-input/01-architecture-input-report.md#9-open-architectural-questions).
- **Reconnection** — frame against fairness (no leak on resume) and reliability (ENG-RT-03).
- **Room isolation & scaling** — frame against AP-18/AAP-08; keep bounded (ENG-SC-01).
- **Dictionary loading/pinning** — frame against INV-D3 and AP-15 (content influences words only).

## 10. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial decision heuristics and recording guidance. |
