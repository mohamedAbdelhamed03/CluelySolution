# 30. Architecture Anti-Principles — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Mandatory — forbidden practices for the Software Architecture phase |
| **Purpose** | Define practices that are **explicitly forbidden**. Any design exhibiting an anti-principle is rejected in review. Complements [29 Architecture Principles](01-architecture-principles.md). |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [The Anti-Principles](#3-the-anti-principles)
4. [Detection at Review Time](#4-detection-at-review-time)
5. [Revision History](#5-revision-history)

---

## 1. Purpose & Usage

Where [29](01-architecture-principles.md) says what architecture **must** do, this document
says what it **must not** do. Each anti-principle has: **Statement · Why it is dangerous ·
Common symptoms · Consequences · Prevention strategies.** Reviewers use these as red flags
(see [33 Review Checklist](05-architecture-review-checklist.md)).

## 2. References
- [29 Architecture Principles](01-architecture-principles.md), [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md)
- [11 Invariants](../02-business-analysis/10-business-invariants.md), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md)

## 3. The Anti-Principles

### AAP-01 — Do NOT duplicate business rules
- **Why dangerous:** Multiple copies of a rule drift out of sync; the game becomes inconsistent. Violates [AP-02/AP-07].
- **Common symptoms:** The same rule enforced in several places; client and server both "deciding" outcomes.
- **Consequences:** Contradictory behaviour; bugs that appear only on some paths.
- **Prevention:** One authoritative rules core; all paths defer to it; reference, don't re-implement.

### AAP-02 — Do NOT trust the client
- **Why dangerous:** Clients can be modified; trusting them enables cheating and leaks. Violates [AP-03/AP-11].
- **Common symptoms:** Authorization based on client-declared role/team/host; outcomes computed client-side.
- **Consequences:** Forged intents, corrupted state, unfair play. [ENG-FP-02]
- **Prevention:** Authorize/adjudicate every intent server-side against authoritative state.

### AAP-03 — Do NOT expose hidden information
- **Why dangerous:** Any leak of unrevealed ownership destroys the game — the single existential failure. Violates [AP-05/AP-11].
- **Common symptoms:** Full board (with ownership) sent to all; client-side hiding; leaks via reconnect/rematch/telemetry.
- **Consequences:** Game rendered unfair/pointless. [ENG-FP-01, INV-B9]
- **Prevention:** Ownership never leaves the server for non-Spymaster projections; audit every delivery path; scrub telemetry.

### AAP-04 — Do NOT bypass validation
- **Why dangerous:** Any unvalidated path can corrupt state or break fairness. Violates [AP-09/AP-10].
- **Common symptoms:** "Fast paths" that skip checks; effects applied before validation.
- **Consequences:** Illegal states; out-of-turn actions; corruption. [ENG-ST-01]
- **Prevention:** A single mandatory validation/authorization step precedes every effect; no exceptions.

### AAP-05 — Do NOT introduce unnecessary complexity
- **Why dangerous:** Complexity adds risk, cost, and defects without value. Violates [AP-12].
- **Common symptoms:** Abstractions with one user; layers with no purpose; generalized frameworks for a fixed game.
- **Consequences:** Slower delivery; fragile system; harder testing.
- **Prevention:** Justify each element against a fixed requirement or top quality attribute; delete the unjustified.

### AAP-06 — Do NOT optimize prematurely
- **Why dangerous:** Optimizing before need harms clarity and may compromise correctness/fairness. Violates [AP-04/AP-12].
- **Common symptoms:** Micro-optimizations, caches, or scaling machinery with no measured need.
- **Consequences:** Complexity, subtle bugs, possible leaks (e.g., shared caches). [ENG-SC-* are Future Optimization]
- **Prevention:** Optimize only against a measured target ([32 Metrics](04-architecture-success-metrics.md)); keep footprints bounded without pre-scaling.

### AAP-07 — Do NOT create tight coupling
- **Why dangerous:** Ripple changes and fragility; hard to evolve. Violates [AP-15].
- **Common symptoms:** Components reaching into each other's internals; shared implementation details.
- **Consequences:** Rigid system; risky changes.
- **Prevention:** Depend on stable boundaries; hide internals; minimize dependencies.

### AAP-08 — Do NOT share mutable state between rooms
- **Why dangerous:** Cross-room mutable state creates races and blocks scaling. Violates [AP-18].
- **Common symptoms:** Global mutable structures touched by multiple rooms.
- **Consequences:** Concurrency bugs; contention; non-isolation. [ENG-CO-*, SCAL-1]
- **Prevention:** Each room owns its state; no cross-room mutable coupling.

### AAP-09 — Do NOT embed business logic in transport/delivery layers
- **Why dangerous:** Rules leak out of the core; duplicated/inconsistent enforcement; hard to test. Violates [AP-14/AP-16].
- **Common symptoms:** Turn/clue/guess decisions made inside the real-time delivery layer.
- **Consequences:** Non-portable, untestable rules; drift.
- **Prevention:** Delivery transports and filters only; it never adjudicates. [SRS §2.15 boundary]

### AAP-10 — Do NOT leak implementation details across boundaries
- **Why dangerous:** Consumers couple to internals; boundaries stop protecting invariants. Violates [AP-17].
- **Common symptoms:** Internal representations exposed; boundary contracts revealing storage/transport specifics.
- **Consequences:** Coupling; fragility; accidental information exposure.
- **Prevention:** Boundaries expose intent/outcome, not internals; keep contracts minimal and stable.

### AAP-11 — Do NOT allow architecture to influence gameplay
- **Why dangerous:** Architecture must serve the rules, never bend them for convenience. Violates [AP-01/AP-02].
- **Common symptoms:** "We can simplify if we change this rule/count/flow"; altering timing/precedence to ease design.
- **Consequences:** A different, non-faithful game; invalidated analysis. [ADR-14]
- **Prevention:** Treat rules/invariants/precedence as immutable inputs; escalate any perceived rule friction to the BA, never self-resolve.

### AAP-12 — Do NOT design for hypothetical requirements
- **Why dangerous:** Speculative generality adds cost/risk for unbuilt features. Violates [AP-12].
- **Common symptoms:** Building auth/ranking/matchmaking machinery now; configurable rule engines for one fixed game.
- **Consequences:** Wasted effort; complexity; delay.
- **Prevention:** Build for the MVP; provide only *additive seams* (AP-13) for known future phases, not implementations. [24 Roadmap]

### AAP-13 — Do NOT create illegal or implicit state
- **Why dangerous:** Implicit/unconstrained state permits illegal combinations (two clues, two Hosts). Violates [AP-08/AP-09].
- **Common symptoms:** Booleans/flags standing in for real state; no explicit state model.
- **Consequences:** Corruption; invariant violations. [ENG-ST-01/02]
- **Prevention:** Model states explicitly ([08]); enforce invariants ([11]); default-deny undefined transitions.

### AAP-14 — Do NOT make non-deterministic outcome decisions
- **Why dangerous:** Non-determinism breaks fairness and reproducibility. Violates [AP-06].
- **Common symptoms:** Outcome depends on arrival timing/ordering not governed by the precedence rules.
- **Consequences:** Disputed, irreproducible results. [17, ENG-GP-01]
- **Prevention:** Serialize/resolve conflicts by the fixed precedence; make effects idempotent.

## 4. Detection at Review Time

Each anti-principle maps to a red-flag question in the [Review Checklist (33)](05-architecture-review-checklist.md).
A design triggering any red flag must either remove the anti-principle or obtain a **recorded,
justified exception** (documented as an ADR-style decision with rationale and expiry). Fairness
and rule-fidelity anti-principles (AAP-02, AAP-03, AAP-11) are **non-waivable**.

## 5. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial forbidden-practices set. |
