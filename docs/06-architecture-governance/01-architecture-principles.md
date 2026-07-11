# 29. Architecture Principles — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Mandatory — governance for the Software Architecture phase |
| **Purpose** | Define the mandatory principles every architectural decision must follow. These are the *rulebook* for architecture, not architecture itself. No technology, pattern, component, or diagram is chosen here. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Authority](#1-purpose--authority)
2. [References](#2-references)
3. [How Principles Are Applied](#3-how-principles-are-applied)
4. [The Principles](#4-the-principles)
5. [Precedence Among Principles](#5-precedence-among-principles)
6. [Revision History](#6-revision-history)

---

## 1. Purpose & Authority

Every architectural decision in Cluely **must** be justifiable against these principles. A
design that violates a principle is rejected unless an explicit, recorded exception is
approved (see [33 Review Checklist](05-architecture-review-checklist.md)). Principles are
mandatory; they encode the approved Business and Engineering analysis as durable constraints
on *how* the system may be designed.

## 2. References
- [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) (drivers, constraints, fixed vs open)
- [11 Business Invariants](../02-business-analysis/10-business-invariants.md), [17 Rule Precedence](../02-business-analysis/16-rule-precedence.md)
- [20 ADRs](../03-business-governance/02-architecture-decision-records.md), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md) Engineering Analysis

## 3. How Principles Are Applied

Each principle has: **ID · Statement · Description · Why it exists · Expected benefits · Risks
if violated · Related Business Drivers · Related Engineering Drivers · Examples.** During
architecture, decisions cite the principles they satisfy; reviews check compliance; the
[Traceability Matrix (34)](06-architecture-traceability-matrix.md) records the linkage.

## 4. The Principles

### AP-01 — Business Before Technology
- **Statement:** Business needs and rules drive every decision; technology is chosen to serve them, never the reverse.
- **Description:** Start from the approved business requirements and invariants; select mechanisms only after the problem is understood.
- **Why it exists:** The specs are the source of truth; technology-first designs distort the product. [ADR-13]
- **Expected benefits:** Fit-for-purpose design; no rework from tech-driven detours.
- **Risks if violated:** Over-engineering; solutions that don't map to requirements; scope creep.
- **Related Business Drivers:** Fairness, extensibility. **Related Engineering Drivers:** All (they exist to serve rules).
- **Examples:** Choosing a concurrency approach *because* determinism is required (ENG-GP-01), not because a technology offers it.

### AP-02 — Business Rules Are Immutable
- **Statement:** Architecture must implement the approved business rules exactly; it may never add, remove, or alter them.
- **Description:** Rules, counts, turn flow, win/loss, and precedence are fixed. [03, 17]
- **Why it exists:** Faithful Codenames is the product's core promise. [ADR-14]
- **Expected benefits:** Guaranteed fidelity; stable target for design.
- **Risks if violated:** A different game; loss of trust; invalidated analysis.
- **Related Business Drivers:** Faithful gameplay. **Related Engineering Drivers:** State management, validation.
- **Examples:** Architecture may choose *how* to enforce "9/8/7/1", never *whether* to.

### AP-03 — Server Is the Single Source of Truth
- **Statement:** The server holds and adjudicates all authoritative state; clients submit intents and render filtered state.
- **Description:** No client-side adjudication of rules or hidden information. [ADR-08]
- **Why it exists:** Fairness and integrity require central authority. [ENG-FP-01/02]
- **Expected benefits:** Cheating resistance; consistency; no info leakage.
- **Risks if violated:** Cheating; divergent state; leaked ownership.
- **Related Business Drivers:** Fairness, private multiplayer. **Related Engineering Drivers:** Authoritative state, hidden info.
- **Examples:** A guess is validated and resolved server-side; the client never decides outcomes.

### AP-04 — Correctness Before Performance
- **Statement:** When correctness and performance conflict, correctness wins.
- **Description:** Never trade a correct outcome for lower latency.
- **Why it exists:** An incorrect result has no value regardless of speed. [§4 QA ranking, 28]
- **Expected benefits:** Trustworthy outcomes.
- **Risks if violated:** Subtle result corruption; unfair matches.
- **Related Business Drivers:** Fairness. **Related Engineering Drivers:** State atomicity, concurrency.
- **Examples:** Serializing per-room guesses even if it caps per-room throughput.

### AP-05 — Fairness Before Optimization
- **Statement:** Hidden-information protection and determinism take precedence over any optimization.
- **Description:** No optimization may weaken filtering or determinism. [INV-B9, 17]
- **Why it exists:** Fairness is existential to the game.
- **Expected benefits:** The game remains playable and fair.
- **Risks if violated:** Leaks or non-determinism destroy the product.
- **Related Business Drivers:** Fairness. **Related Engineering Drivers:** Hidden info, determinism.
- **Examples:** Not caching a shared projection that could leak the Key across roles.

### AP-06 — Deterministic Behaviour
- **Statement:** Given the same inputs and state, the system must always produce the same outcome.
- **Description:** Conflict resolution and terminal ordering follow the fixed precedence. [17]
- **Why it exists:** Provable fairness and reproducibility. [QM-09]
- **Expected benefits:** Predictable, testable, auditable behaviour.
- **Risks if violated:** Disputed/irreproducible outcomes.
- **Related Business Drivers:** Fairness. **Related Engineering Drivers:** Concurrency, ordering.
- **Examples:** First-valid-wins for simultaneous guesses (ENG-GP-01, RP-12).

### AP-07 — Single Source of Truth (State & Definitions)
- **Statement:** Each fact — a piece of state, a term, a constant — has exactly one authoritative source.
- **Description:** No duplicated/contradictory state or definitions. [19 Glossary, 21 Constants]
- **Why it exists:** Duplication breeds inconsistency.
- **Expected benefits:** Consistency; easier maintenance.
- **Risks if violated:** Divergent copies; drift; bugs.
- **Related Business Drivers:** Maintainability. **Related Engineering Drivers:** Consistency.
- **Examples:** One authoritative game state per room; constants referenced, not re-stated.

### AP-08 — Explicit State
- **Statement:** All significant state must be explicitly modeled, never implied by scattered flags.
- **Description:** Room/Match/Turn/Connection states are first-class. [08]
- **Why it exists:** Implicit state causes illegal states and hidden bugs.
- **Expected benefits:** Clarity; enforceable invariants.
- **Risks if violated:** Ambiguous/illegal states.
- **Related Business Drivers:** Correctness. **Related Engineering Drivers:** State management.
- **Examples:** An explicit "AwaitingClue/AwaitingGuess/TurnEnded" turn state.

### AP-09 — Explicit, Validated State Transitions
- **Statement:** State changes only through defined transitions; undefined transitions are rejected by default.
- **Description:** Whitelisted transitions per [08]; default-deny. [ENG-ST-01]
- **Why it exists:** Prevents corruption from illegal moves.
- **Expected benefits:** Integrity; predictability.
- **Risks if violated:** Corrupted game state.
- **Related Business Drivers:** Correctness, fairness. **Related Engineering Drivers:** State management.
- **Examples:** Rejecting a guess submitted during the clue phase.

### AP-10 — Fail Fast & Explicitly
- **Statement:** Invalid intents are rejected immediately with a specific business reason; no partial application.
- **Description:** Validation precedes any effect; errors are catalogued. [10, 13]
- **Why it exists:** Silent/partial failures corrupt state and confuse users.
- **Expected benefits:** Clear feedback; integrity.
- **Risks if violated:** Half-applied actions; ambiguous failures.
- **Related Business Drivers:** UX, correctness. **Related Engineering Drivers:** Validation pipeline.
- **Examples:** Returning `NOT_YOUR_TURN` without mutating state.

### AP-11 — Secure by Design (Integrity)
- **Statement:** Authorize every intent against authoritative role/team/phase/state; never trust the client.
- **Description:** Segregate hidden information; gate every action. [SEC-1..8]
- **Why it exists:** The game depends on integrity and hidden info. [ENG-FP-01/02]
- **Expected benefits:** Cheating/leak resistance.
- **Risks if violated:** Cheating, leaks, corrupted play.
- **Related Business Drivers:** Fairness. **Related Engineering Drivers:** Hidden info, validation.
- **Examples:** Never sending unrevealed ownership to a non-Spymaster projection.

### AP-12 — Simple MVP First
- **Statement:** Prefer the simplest design that satisfies the MVP's fixed requirements and top quality attributes; defer complexity.
- **Description:** Avoid building for deferred phases now. [24, 27 §5.3]
- **Why it exists:** Over-engineering wastes effort and adds risk.
- **Expected benefits:** Faster, more reliable delivery.
- **Risks if violated:** Complexity, delay, fragility.
- **Related Business Drivers:** Fast delivery. **Related Engineering Drivers:** Operational simplicity.
- **Examples:** Room-lifetime recoverability, not a full durability platform (ENG-RE-01).

### AP-13 — Design for Evolution (Additive Seams)
- **Statement:** Provide seams so future capabilities (esp. authentication) attach additively, without changing core rules.
- **Description:** Isolate identity and other future concerns. [AUTH-1..5, 24]
- **Why it exists:** Protects the roadmap without premature build.
- **Expected benefits:** Cheap future evolution.
- **Risks if violated:** Costly rewrites later.
- **Related Business Drivers:** Extensibility. **Related Engineering Drivers:** Session management.
- **Examples:** Identity abstraction that a durable account can later attach to.

### AP-14 — Technology Independence (at the Business Core)
- **Statement:** The rules/adjudication core must be expressible independently of transport, storage, UI, and language.
- **Description:** Keep the engine free of infrastructure concerns. [ADR-12/13]
- **Why it exists:** Portability, testability, language-neutral fairness.
- **Expected benefits:** Reuse across clients; easy testing.
- **Risks if violated:** Lock-in; leaked infra into rules.
- **Related Business Drivers:** One gameplay worldwide. **Related Engineering Drivers:** Maintainability.
- **Examples:** Rules that don't reference any natural language (INV-D1).

### AP-15 — Loose Coupling
- **Statement:** Components depend on each other minimally and through stable boundaries.
- **Description:** Change in one area should not ripple widely.
- **Why it exists:** Maintainability and independent evolution.
- **Expected benefits:** Easier change; isolation of failures.
- **Risks if violated:** Rigid, fragile system.
- **Related Business Drivers:** Maintainability. **Related Engineering Drivers:** Room isolation.
- **Examples:** Content provider influences only words, nothing else (INV-D1).

### AP-16 — High Cohesion
- **Statement:** Each component has a single, clear responsibility.
- **Description:** Related behaviour lives together; unrelated behaviour does not.
- **Why it exists:** Clarity and testability.
- **Expected benefits:** Understandable, maintainable units.
- **Risks if violated:** Tangled responsibilities; hard testing.
- **Related Business Drivers:** Maintainability. **Related Engineering Drivers:** Validation, state.
- **Examples:** Rules adjudication separate from delivery/transport.

### AP-17 — Clear Boundaries
- **Statement:** Responsibilities and information flows across boundaries are explicit; boundaries enforce invariants (e.g., role filtering at delivery).
- **Description:** Boundaries are where guarantees are made. [SRS §2.15]
- **Why it exists:** Prevents leakage and coupling.
- **Expected benefits:** Enforceable guarantees; clarity.
- **Risks if violated:** Leaks; hidden dependencies.
- **Related Business Drivers:** Fairness. **Related Engineering Drivers:** Hidden info, boundaries.
- **Examples:** The delivery boundary guarantees no unrevealed ownership reaches Operatives.

### AP-18 — Minimal Shared Mutable State
- **Statement:** Avoid sharing mutable state, especially across rooms; rooms are isolated units.
- **Description:** Each room owns its state; no cross-room mutable coupling. [SCAL-1]
- **Why it exists:** Concurrency safety and scalability.
- **Expected benefits:** Fewer races; linear scaling by room.
- **Risks if violated:** Contention; cross-room bugs.
- **Related Business Drivers:** Scalability. **Related Engineering Drivers:** Concurrency, isolation.
- **Examples:** No global mutable structure shared between independent matches.

### AP-19 — Observability by Design
- **Statement:** The system must expose enough (PII-free) business-event and outcome visibility to verify correctness and diagnose issues.
- **Description:** Observability is planned, not bolted on. [12 Events, NFR-13]
- **Why it exists:** The risk profile demands verifiability.
- **Expected benefits:** Faster diagnosis; auditable outcomes.
- **Risks if violated:** Blind operation; undetectable defects; possible PII leakage if unplanned.
- **Related Business Drivers:** Trust. **Related Engineering Drivers:** Reliability, fairness.
- **Examples:** Emitting the defined domain events for each match without personal data.

### AP-20 — Testability by Design
- **Statement:** The architecture must make correctness, concurrency, fairness, and recovery testable.
- **Description:** Design for deterministic, isolatable verification. [27 §3]
- **Why it exists:** The hard risks are subtle; they must be testable.
- **Expected benefits:** Confidence; regression protection.
- **Risks if violated:** Undetected corruption/leaks.
- **Related Business Drivers:** Fairness, correctness. **Related Engineering Drivers:** All P1 risks.
- **Examples:** A rules core that can be exercised without transport to test outcomes deterministically.

## 5. Precedence Among Principles

When principles tension, resolve in this order (consistent with [28 §4](../05-architecture-input/01-architecture-input-report.md#4-quality-attribute-priorities)):

1. **AP-02 Business Rules Immutable** and **AP-05 Fairness** (never compromised).
2. **AP-04 Correctness** and **AP-06 Determinism**.
3. **AP-03 Server Authority**, **AP-07 Single Source of Truth**, **AP-11 Secure by Design**.
4. **AP-08/09/10 Explicit state / transitions / fail-fast**.
5. **AP-12 Simple MVP First** vs **AP-13 Design for Evolution** — bias to simplicity, but never remove additive seams.
6. Structural quality (**AP-14..AP-20**) — apply unless they conflict with a higher rank.

> Simplicity (AP-12) never overrides fairness (AP-05), correctness (AP-04), or rule fidelity
> (AP-02).

## 6. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial mandatory architecture principles. |
