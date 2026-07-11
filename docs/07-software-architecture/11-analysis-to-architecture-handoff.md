# 07.11 — Analysis → Architecture Handoff (Final Summary)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Official handoff from Analysis to Software Architecture (design) |
| **Purpose** | The single-page synthesis of the discovery phase. It answers **"What must the architecture be capable of solving?"** — not *how*. This is the official handoff into architecture design. |
| **Scope** | Synthesis of discovery docs 01–10. No design, technology, or patterns. |
| **Inputs** | Discovery documents [01](01-architecture-overview.md)–[10](10-architecture-readiness-review.md). |
| **Outputs** | Focus areas for the architecture design work. |
| **Dependencies** | All discovery docs; [Architecture Governance](../06-architecture-governance/README.md). |
| **Cross References** | [Architecture Input Report](../05-architecture-input/01-architecture-input-report.md). |
| **Related Business Documents** | [Invariants](../02-business-analysis/10-business-invariants.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md). |
| **Related Engineering Documents** | [Enrichment §5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary). |

---

## Top Architectural Drivers
1. **Gameplay Fairness** (hidden information + determinism) — shapes the most structure.
2. **Correctness** of outcomes.
3. **Deterministic State & Concurrency**.
4. **Real-Time Communication**.
5. **Recoverability / Reliability**.
6. **Security / Integrity** (no accounts).
*(Full ranked list: [02 Architectural Drivers](02-architectural-drivers.md).)*

## Core System Responsibilities
- **Rules & Play** (adjudication, board, turn, clue, guess) — the deterministic authority.
- **Room & Lobby** (membership, setup, cleanup).
- **Validation & Authorization** (gate before every effect).
- **Delivery** (role-filtered notification/transport).
- **Connectivity & Identity** (presence, transient identity, reconnection).
- **State Custody & Recovery** (authoritative state, restore).
- **Dictionary Provision** and **Observability** (supporting).
*(Full catalog: [03 System Responsibilities](03-system-responsibilities.md).)*

## Critical Responsibility Boundaries
- **Rules must never live in transport/delivery** (delivery filters + transports only).
- **Dictionary must never influence rules** (words only).
- **Rooms must not share mutable state** (isolation = scale).
- **Operative-facing delivery must never carry unrevealed ownership** (absolute).
- **Validation is never bypassable; connectivity/observability never adjudicate.**
*(Detail: [04 Responsibility Boundaries](04-responsibility-boundaries.md).)*

## Critical Consistency Boundaries (must be strongly consistent)
- **Guess resolution & card reveal** (atomic, serialized, first-valid-wins).
- **Turn change** and **game completion** (atomic; terminal preempts).
- **Join** (capacity+nickname), **role/team assignment & start lock**, **host migration**,
  **reconnect** (single connection), **room creation** (code uniqueness), **expiry vs activity**.
*(Detail: [06 Consistency Boundaries](06-consistency-boundaries.md); ownership: [05 State Ownership](05-state-ownership.md).)*

## Highest-Risk Responsibilities (design first, deepest)
| Responsibility | Why highest risk |
|----------------|------------------|
| **Rules & Play (R-04)** | Atomicity + determinism; a subtle bug corrupts outcomes. |
| **Delivery filtering (R-11)** | One missed path leaks the Key — existential. |
| **State Custody & Recovery (R-14/15)** | Recovery correctness is the hardest reliability problem. |
| **Connectivity & Reconnect (R-12/13)** | Dual actors / leaky resume under faults. |
*(Cross-ref: [Enrichment §5.6](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary), [Risk Register](../06-architecture-governance/07-architecture-risk-register.md).)*

## Open Architectural Questions (for design)
Q1 per-room coordination/serialization · Q2 custody + atomic commit + recovery boundary ·
Q3 full-state vs delta sync + staleness detection · Q4 leak-proof role-filtered delivery ·
Q5 single-active-connection + reconnect snapshot · Q6 room isolation & bounded scale ·
Q7 command/query separation (if any) · Q8 dictionary version pinning & retention.
*(Detail: [10 Readiness Review §2](10-architecture-readiness-review.md#2-unresolved-questions-for-the-architecture-design-work).)*
Parameter clarifications (grace defaults, normalization policy, seeded-generation appetite,
retention windows) route to BA/PO and **do not block** design.

## Recommended Focus Areas for Software Architecture
1. **Deterministic, atomic Rules & Play core** (Q1, CB-01/02/03) — the correctness/fairness backbone.
2. **Leak-proof role-filtered delivery boundary** (Q4, QS-01) — the existential fairness guarantee.
3. **Authoritative custody + recovery** (Q2, QS-06) — the reliability backbone.
4. **Reconnection + single-active-connection** (Q5, QS-07).
5. **Real-time synchronization** (Q3, QS-08).
6. **Room isolation & bounded scale** (Q6) — keep bounded now, defer scale machinery.

## The one question this handoff answers
> **"What must the architecture be capable of solving?"**
> Enforcing a small, fixed ruleset **correctly, deterministically, and without leaking hidden
> information**, in **isolated rooms**, under **concurrency, unreliable networks, disconnection, and
> interruption**, with **recoverable** state and an **additive** path to future authentication —
> all while keeping the rules core **language-neutral** and the design the **simplest** that meets
> these guarantees.

It intentionally does **not** answer *how* — that is the work the architecture design phase now
begins, governed by [Architecture Governance (06)](../06-architecture-governance/README.md).

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Official Analysis→Architecture handoff summary. |
