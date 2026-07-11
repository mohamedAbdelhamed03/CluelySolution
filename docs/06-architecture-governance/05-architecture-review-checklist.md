# 33. Architecture Review Checklist — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Mandatory — must be completed before architecture approval |
| **Purpose** | The gate every architecture (or significant architectural change) passes before approval. Each item has a purpose, the evidence expected, review questions, and pass criteria. Ties directly to [29 Principles](01-architecture-principles.md), [30 Anti-Principles](02-architecture-anti-principles.md), and [32 Success Metrics](04-architecture-success-metrics.md). |
| **Technology** | Neutral. |

## Table of Contents
1. [How to Use This Checklist](#1-how-to-use-this-checklist)
2. [References](#2-references)
3. [A. Business Fidelity](#a-business-fidelity)
4. [B. Fairness & Security](#b-fairness--security)
5. [C. Correctness & State](#c-correctness--state)
6. [D. Concurrency & Real-Time](#d-concurrency--real-time)
7. [E. Resilience & Recovery](#e-resilience--recovery)
8. [F. Quality Attributes & Simplicity](#f-quality-attributes--simplicity)
9. [G. Governance & Traceability](#g-governance--traceability)
10. [Sign-Off](#sign-off)
11. [Revision History](#revision-history)

---

## 1. How to Use This Checklist

Every item is **Pass / Fail / Waived (with recorded exception)**. Items marked
**Non-waivable** cannot be waived — a Fail blocks approval. A review is complete when all items
are Pass or justifiably Waived and the [Success Metrics scorecard (32 §9)](04-architecture-success-metrics.md#9-scorecard)
is green. Findings feed the [Risk Register (35)](07-architecture-risk-register.md).

## 2. References
- [29 Principles](01-architecture-principles.md), [30 Anti-Principles](02-architecture-anti-principles.md), [32 Metrics](04-architecture-success-metrics.md)
- [11 Invariants](../02-business-analysis/10-business-invariants.md), [17 Precedence](../02-business-analysis/16-rule-precedence.md), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md)

---

## A. Business Fidelity

### A1 — Business rules respected
- **Purpose:** Ensure faithful Codenames; no rule added/removed/changed.
- **Expected evidence:** Rule→enforcement mapping ([34](06-architecture-traceability-matrix.md)); ASM-01.
- **Review questions:** Is every `BR-*` enforced somewhere? Any rule altered for design convenience (AAP-11)?
- **Pass criteria:** 100% rules mapped; zero rule changes. **Non-waivable.**

### A2 — Business invariants enforced
- **Purpose:** Guarantee the always-true conditions.
- **Expected evidence:** Invariant→boundary mapping; ASM-02.
- **Review questions:** Where is each `INV-*` guaranteed? Can any defined transition violate one?
- **Pass criteria:** 100% invariants enforced; none violable. **Non-waivable.**

### A3 — Business constants & terminology single-sourced
- **Purpose:** No duplicated/contradictory values or terms.
- **Expected evidence:** References to [21 Constants](../03-business-governance/03-business-constants-catalog.md) and [19 Glossary](../03-business-governance/01-business-glossary.md).
- **Review questions:** Are numbers referenced, not restated? Terminology consistent?
- **Pass criteria:** No duplicated constants; consistent terms (AP-07).

### A4 — No conflicting assumptions
- **Purpose:** Architecture doesn't assume something contrary to the analysis.
- **Expected evidence:** Assumptions list checked against [01](../01-product-discovery/01-business-requirements.md)/[28](../05-architecture-input/01-architecture-input-report.md).
- **Review questions:** Does any assumption contradict a fixed decision ([28 §10](../05-architecture-input/01-architecture-input-report.md#10-fixed-decisions))?
- **Pass criteria:** Zero conflicting assumptions.

## B. Fairness & Security

### B1 — Hidden information never leaks
- **Purpose:** Protect the game's core secret.
- **Expected evidence:** All delivery paths shown role-filtered; ASM-05.
- **Review questions:** Can any path (initial/delta/reconnect/rematch/telemetry) carry unrevealed ownership to a non-Spymaster (AAP-03)?
- **Pass criteria:** Zero leak-capable paths. **Non-waivable.**

### B2 — Server authority / no client trust
- **Purpose:** Prevent cheating and client-side adjudication.
- **Expected evidence:** Every intent authorized/adjudicated server-side; ASM-11.
- **Review questions:** Is any authority derived from client-declared role/team/host (AAP-02)?
- **Pass criteria:** No client-trusted decisions. **Non-waivable.**

### B3 — Security/integrity considered
- **Purpose:** Authorization, room access, reconnection identity, abuse limits.
- **Expected evidence:** Coverage of [SEC-1..8](../02-business-analysis/01-software-requirements.md#212-security-considerations).
- **Review questions:** Are all intents authorized by state? Tokens opaque/room-scoped? Abuse limits acknowledged?
- **Pass criteria:** All security considerations addressed or explicitly deferred with rationale.

## C. Correctness & State

### C1 — Explicit state & valid transitions
- **Purpose:** No implicit/illegal states.
- **Expected evidence:** State model aligned to [08](../02-business-analysis/07-state-machines.md); default-deny; ASM-07.
- **Review questions:** Is all significant state explicit (AP-08)? Are undefined transitions rejected (AP-09)?
- **Pass criteria:** Explicit states; whitelisted transitions.

### C2 — Atomic mutations / no partial updates
- **Purpose:** Prevent corruption from half-applied changes.
- **Expected evidence:** Commit boundaries; invariant checks pre-commit (ENG-ST-02).
- **Review questions:** Are multi-field changes atomic? Is state ever broadcast half-applied?
- **Pass criteria:** All-or-nothing mutations; commit-then-broadcast.

### C3 — Single authoritative game state
- **Purpose:** One truth per room.
- **Expected evidence:** State ownership defined; no duplication (AP-07).
- **Review questions:** Is there exactly one authoritative state per room? Any duplicate/derived-as-truth copies?
- **Pass criteria:** One source of truth per room.

### C4 — Deterministic outcomes / precedence honored
- **Purpose:** Provable fairness under conflict.
- **Expected evidence:** Conformance to [17](../02-business-analysis/16-rule-precedence.md); ASM-06.
- **Review questions:** Are all conflict scenarios (RP-1..12) resolved deterministically? First-valid-wins?
- **Pass criteria:** Fully deterministic. **Non-waivable.**

## D. Concurrency & Real-Time

### D1 — Concurrency addressed
- **Purpose:** Safe simultaneous actions.
- **Expected evidence:** Handling for ENG-GP-01, ENG-CO-01/02/04.
- **Review questions:** How are simultaneous guesses/joins/leaves/host actions coordinated? Any shared mutable cross-room state (AAP-08)?
- **Pass criteria:** Deterministic coordination; no cross-room mutable sharing.

### D2 — Real-time communication considered
- **Purpose:** Tolerate loss/dup/reorder/slow clients.
- **Expected evidence:** Handling for ENG-RT-01/02/03; ordering/resync approach identified.
- **Review questions:** How is exactly-once effect achieved? How do clients detect they're behind?
- **Pass criteria:** Loss/dup/reorder tolerated; idempotent intents.

### D3 — Duplicate/replay resistance
- **Purpose:** Retries/replays must not corrupt state.
- **Expected evidence:** Idempotency + state/turn binding (ENG-GP-02, ENG-FP-03).
- **Review questions:** Do duplicate commands become no-ops? Are intents bound to state/turn?
- **Pass criteria:** Duplicates/replays safe.

## E. Resilience & Recovery

### E1 — Recovery considered
- **Purpose:** Survive crashes/interruptions within room life.
- **Expected evidence:** Recovery boundary defined (ENG-RE-01/02, ENG-ST-04); ASM-08.
- **Review questions:** What is the recovery point? Are terminal effects never replayed?
- **Pass criteria:** Recoverable to last consistent state; idempotent recovery.

### E2 — Reconnection preserves gameplay
- **Purpose:** Brief drops don't end matches or leak info.
- **Expected evidence:** Role-filtered snapshot + pause resume + single active connection (ENG-RT-03, INV-P4/P5); ASM-09.
- **Review questions:** Is the restored view correct per role? Is the paused phase resumed?
- **Pass criteria:** Correct, leak-free, resumable reconnection.

### E3 — Room lifecycle & expiry safe
- **Purpose:** No lost live rooms; clean reclaim.
- **Expected evidence:** Race-safe expiry; host migration; result-before-expiry (ENG-CO-05, ENG-RM-01/02).
- **Review questions:** Can a live room be wrongly closed? Is exactly one Host guaranteed after migration?
- **Pass criteria:** Safe lifecycle; INV-R1 preserved.

## F. Quality Attributes & Simplicity

### F1 — Quality attributes considered (with precedence)
- **Purpose:** Each prioritized attribute has a stance.
- **Expected evidence:** Attribute→approach map ([28 §4](../05-architecture-input/01-architecture-input-report.md#4-quality-attribute-priorities)); ASM-13.
- **Review questions:** Is the precedence order honored on conflicts?
- **Pass criteria:** All attributes addressed; precedence respected.

### F2 — No unnecessary complexity
- **Purpose:** Avoid over-engineering.
- **Expected evidence:** Justification per element; ASM-14.
- **Review questions:** Is each abstraction justified by a fixed requirement/top QA? Any speculative generality (AAP-05/12)?
- **Pass criteria:** Zero unjustified elements.

### F3 — No duplicated responsibilities; clear boundaries; high cohesion / loose coupling
- **Purpose:** Maintainable structure.
- **Expected evidence:** Responsibility map; boundary definitions (AP-15/16/17).
- **Review questions:** Any duplicated responsibility? Do boundaries enforce guarantees (e.g., filtering)?
- **Pass criteria:** Single-responsibility units; explicit boundaries.

### F4 — Technology neutrality maintained (core)
- **Purpose:** Portable, testable rules core.
- **Expected evidence:** Core free of infra/language (AP-14); ASM-12.
- **Review questions:** Any transport/storage/language leaked into rules?
- **Pass criteria:** Neutral core. **Non-waivable** for the rules core.

### F5 — Scalable evolution (bounded now)
- **Purpose:** Grow by room count without redesign; don't pre-scale.
- **Expected evidence:** Bounded per-room footprint; deferrals justified (ENG-SC-*).
- **Review questions:** Is per-room state bounded? Is scale complexity appropriately deferred?
- **Pass criteria:** Bounded footprint; deferrals recorded.

### F6 — Observability & testability considered
- **Purpose:** Verifiable, diagnosable system.
- **Expected evidence:** Event visibility plan (PII-free); testable seams (AP-19/20).
- **Review questions:** Can correctness/concurrency/fairness/recovery be tested? Are events observable without PII?
- **Pass criteria:** Observability and testability designed in.

## G. Governance & Traceability

### G1 — Every decision documented
- **Purpose:** No implicit decisions.
- **Expected evidence:** Architecture ADRs ([31 §8](03-architecture-decision-heuristics.md#8-recording-a-decision)); ASM-10.
- **Review questions:** Is each significant decision recorded with options/rationale/drivers?
- **Pass criteria:** 100% documented.

### G2 — Traceability maintained
- **Purpose:** Business→…→verification linkage intact.
- **Expected evidence:** Updated [Traceability Matrix (34)](06-architecture-traceability-matrix.md); ASM-04.
- **Review questions:** Does each decision link back to a driver and forward to verification?
- **Pass criteria:** Complete traceability.

### G3 — Engineering challenges addressed
- **Purpose:** No unresolved P1 risks.
- **Expected evidence:** Challenge→response map; ASM-03.
- **Review questions:** Are all P1 challenges resolved? Deferrals justified?
- **Pass criteria:** 0 unaddressed P1; all mapped. **Non-waivable for P1.**

### G4 — Principle compliance / no anti-principles
- **Purpose:** Governance adherence.
- **Expected evidence:** Findings vs [29](01-architecture-principles.md)/[30](02-architecture-anti-principles.md); ASM-11.
- **Review questions:** Any principle violated or anti-principle present without a recorded, waivable exception?
- **Pass criteria:** Zero unwaived violations; non-waivable items clean.

## Sign-Off

| Role | Name | Decision (Approve / Revise / Reject) | Date |
|------|------|--------------------------------------|------|
| Lead Software Architect | | | |
| Business Analyst (fidelity) | | | |
| Engineering Lead (risk) | | | |
| Product Owner (scope) | | | |

**Approval requires:** all Non-waivable items Pass, the [Success-Metrics scorecard (32 §9)](04-architecture-success-metrics.md#9-scorecard) green, and every waiver recorded with rationale and expiry.

## Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial architecture review checklist. |
