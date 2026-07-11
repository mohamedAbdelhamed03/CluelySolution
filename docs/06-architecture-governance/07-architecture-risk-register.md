# 35. Architecture Risk Register — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Living register — risks arising from *architectural decisions* |
| **Purpose** | Track risks introduced by the *way the system is designed* (as opposed to the engineering challenges of the domain, which live in [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)). These are the failure modes of architecture practice — over-engineering, wrong boundaries, leaky abstractions, etc. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Distinction from Doc 26](#1-purpose--distinction-from-doc-26)
2. [References](#2-references)
3. [Scoring & Register Format](#3-scoring--register-format)
4. [The Register](#4-the-register)
5. [Review Cadence & Ownership](#5-review-cadence--ownership)
6. [Revision History](#6-revision-history)

---

## 1. Purpose & Distinction from Doc 26

[26 Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) covers risks of the
**problem domain** (concurrency, hidden info, reconnection…). This register covers risks of the
**solution practice** — mistakes an architecture team can make while solving those challenges.
Both feed reviews; this one is owned by the architecture governance process and is revisited
every review.

## 2. References
- [29 Principles](01-architecture-principles.md), [30 Anti-Principles](02-architecture-anti-principles.md)
- [33 Review Checklist](05-architecture-review-checklist.md), [34 Traceability](06-architecture-traceability-matrix.md)

## 3. Scoring & Register Format

- **Likelihood:** Rare / Occasional / Frequent. **Impact:** Low / Medium / High / Critical.
- Each risk: **ID · Description · Root cause · Likelihood · Impact · Detection methods ·
  Mitigation strategies · Review frequency · Related principles/anti-principles.**
- Mitigations are **process/guidance**, never architecture decisions (this is governance).

## 4. The Register

### ARISK-01 — Incorrect bounded contexts / responsibility split
- **Description:** Components are carved along the wrong seams (e.g., rules split across delivery and engine).
- **Root cause:** Designing structure before understanding responsibilities; premature decomposition.
- **Likelihood:** Occasional · **Impact:** High.
- **Detection:** Responsibility map review ([33 F3](05-architecture-review-checklist.md)); rules appearing in transport (AAP-09); duplicated logic (AAP-01).
- **Mitigation:** Derive boundaries from responsibilities/invariants first; keep the rules core cohesive and transport-free; review against AP-16/17.
- **Review frequency:** Every architecture review.

### ARISK-02 — Over-engineering / speculative generality
- **Description:** Building for unbuilt phases (auth, ranking, matchmaking) or generalizing a fixed game.
- **Root cause:** Designing for hypothetical requirements; fear of future change.
- **Likelihood:** Frequent · **Impact:** High (cost/delay/complexity).
- **Detection:** Orphan decisions with no backward driver ([34 LR-4](06-architecture-traceability-matrix.md)); elements without justification ([33 F2](05-architecture-review-checklist.md), ASM-14).
- **Mitigation:** Enforce AP-12/AAP-12; provide only additive seams (AP-13); require a driver link for every element.
- **Review frequency:** Every review; spot-check at design sign-off.

### ARISK-03 — Premature optimization
- **Description:** Optimizations/scaling machinery added without measured need, harming clarity or correctness.
- **Root cause:** Optimizing before targets exist; assuming scale.
- **Likelihood:** Occasional · **Impact:** Medium–High.
- **Detection:** Optimizations without a metric target ([32](04-architecture-success-metrics.md)); shared caches near hidden info (leak risk, AAP-03/06).
- **Mitigation:** Optimize only against measured [22 QM-*](../03-business-governance/04-quality-metrics.md) targets; defer ENG-SC-* (Future Optimization); keep footprints bounded, not optimized.
- **Review frequency:** Every review.

### ARISK-04 — Large aggregates / oversized state units
- **Description:** State grouped too coarsely, creating contention or forcing non-atomic updates.
- **Root cause:** Ignoring the natural room/match/turn granularity.
- **Likelihood:** Occasional · **Impact:** High (concurrency/atomicity).
- **Detection:** Wide locks; partial-update risk ([33 C2](05-architecture-review-checklist.md), ENG-ST-02); cross-room coupling (AAP-08).
- **Mitigation:** Align state units with domain ([07](../02-business-analysis/06-domain-model.md), [08](../02-business-analysis/07-state-machines.md)); room-scoped ownership (AP-18).
- **Review frequency:** Every review of state-owning decisions.

### ARISK-05 — Leaky abstractions
- **Description:** Boundaries expose internals (storage/transport/representation), coupling consumers to them.
- **Root cause:** Convenience; unclear contracts.
- **Likelihood:** Occasional · **Impact:** Medium–High (coupling, possible info exposure).
- **Detection:** Boundary contracts revealing internals ([33 F3](05-architecture-review-checklist.md), AAP-10); ownership data crossing a non-Spymaster boundary (AAP-03).
- **Mitigation:** Contracts express intent/outcome only; enforce guarantees at boundaries (AP-17).
- **Review frequency:** Every review.

### ARISK-06 — Tight coupling / hidden dependencies
- **Description:** Components depend on each other's internals or on undocumented assumptions.
- **Root cause:** Shortcuts; missing boundaries.
- **Likelihood:** Occasional · **Impact:** High (fragility).
- **Detection:** Ripple-change analysis; dependency review ([33 F3](05-architecture-review-checklist.md), AAP-07).
- **Mitigation:** Depend on stable boundaries; make dependencies explicit; AP-15.
- **Review frequency:** Every review.

### ARISK-07 — Insufficient observability
- **Description:** The design can't be diagnosed or verified in production; or observability accidentally carries PII.
- **Root cause:** Observability treated as an afterthought.
- **Likelihood:** Occasional · **Impact:** Medium–High.
- **Detection:** No event/metric plan ([33 F6](05-architecture-review-checklist.md), AP-19); telemetry review for PII/ownership leakage (AAP-03).
- **Mitigation:** Design observability in (PII-free) from [12 Events](../02-business-analysis/11-domain-events-catalog.md)/[22](../03-business-governance/04-quality-metrics.md); scrub sensitive data.
- **Review frequency:** Every review.

### ARISK-08 — Poor scalability assumptions
- **Description:** Assuming a scale (too high or too low) that misguides the design.
- **Root cause:** Guessing load; ignoring room-isolation model.
- **Likelihood:** Occasional · **Impact:** Medium.
- **Detection:** Scaling claims without basis ([33 F5](05-architecture-review-checklist.md)); cross-room shared state (AAP-08).
- **Mitigation:** Treat scale as bounded-now/deferred (ENG-SC-*); keep rooms isolated; revisit with real data.
- **Review frequency:** Architecture review + when load data arrives.

### ARISK-09 — Hidden dependencies on timing/ordering
- **Description:** Correctness accidentally depends on message timing not governed by precedence.
- **Root cause:** Not applying deterministic resolution everywhere.
- **Likelihood:** Occasional · **Impact:** Critical (fairness).
- **Detection:** Outcome varies by interleaving ([33 C4](05-architecture-review-checklist.md), AAP-14); non-idempotent effects.
- **Mitigation:** Apply [17 precedence](../02-business-analysis/16-rule-precedence.md) and idempotency uniformly; AP-06.
- **Review frequency:** Every review of concurrency/real-time decisions.

### ARISK-10 — Incorrect state ownership
- **Description:** Authoritative state owned in the wrong place (e.g., client-influenced) or duplicated.
- **Root cause:** Unclear source-of-truth; trusting clients.
- **Likelihood:** Occasional · **Impact:** Critical (correctness/fairness).
- **Detection:** Multiple truths ([33 C3](05-architecture-review-checklist.md), AP-07); client-derived authority (AAP-02).
- **Mitigation:** One authoritative state per room, server-owned (AP-03/07); never trust clients.
- **Review frequency:** Every review.

### ARISK-11 — Weak boundary definitions (fairness leakage)
- **Description:** Boundaries don't clearly enforce the "no unrevealed ownership to non-Spymaster" guarantee.
- **Root cause:** Filtering not localized to a boundary; done ad hoc.
- **Likelihood:** Occasional · **Impact:** Critical (existential).
- **Detection:** No single enforcing boundary for projections ([33 B1](05-architecture-review-checklist.md), ASM-05); leak-capable paths.
- **Mitigation:** Make role-filtering a boundary guarantee, audited across all paths; AP-17/AP-05. **Non-waivable.**
- **Review frequency:** Every review; mandatory before approval.

### ARISK-12 — Architecture pressure to change gameplay
- **Description:** Design difficulty tempts altering a rule/count/timing for convenience.
- **Root cause:** Treating rules as negotiable.
- **Likelihood:** Occasional · **Impact:** Critical (product identity).
- **Detection:** Proposed rule/precedence tweaks ([33 A1](05-architecture-review-checklist.md), AAP-11).
- **Mitigation:** Rules/invariants/precedence are immutable inputs; escalate friction to BA, never self-resolve; AP-02. **Non-waivable.**
- **Review frequency:** Every review.

### ARISK-13 — Traceability decay
- **Description:** Links between drivers, decisions, tests, and ops rot as the project evolves.
- **Root cause:** Not maintaining the matrix through phases.
- **Likelihood:** Frequent · **Impact:** Medium (governance blind spots).
- **Detection:** Orphan decisions / unaddressed drivers ([34 LR-4/5](06-architecture-traceability-matrix.md), ASM-04/10).
- **Mitigation:** Maintain [34](06-architecture-traceability-matrix.md) every phase; change propagation (LR-6); review each cadence.
- **Review frequency:** Every review + phase transitions.

### ARISK-14 — Undocumented decisions
- **Description:** Significant choices made implicitly, losing rationale.
- **Root cause:** Skipping ADR recording under time pressure.
- **Likelihood:** Frequent · **Impact:** Medium.
- **Detection:** Decisions without records ([33 G1](05-architecture-review-checklist.md), ASM-10).
- **Mitigation:** Mandatory ADR recording ([31 §8](03-architecture-decision-heuristics.md#8-recording-a-decision)); no undocumented decision passes review.
- **Review frequency:** Every review.

## 5. Review Cadence & Ownership

- **Owner:** Lead Software Architect (register), with Engineering Lead and BA as reviewers.
- **Cadence:** Reviewed at **every architecture review**, at **design sign-off**, and at each
  **phase transition**; risks re-scored when the design changes.
- **Escalation:** Any **Critical/Non-waivable** risk (ARISK-09/10/11/12) that is open **blocks
  architecture approval** ([33 Sign-Off](05-architecture-review-checklist.md#sign-off)).
- **New risks:** Added as discovered; each new architecture ADR is scanned for new risks before
  acceptance.

## 6. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial architecture risk register (solution-practice risks). |
