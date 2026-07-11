# 07.03 — System Responsibilities (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Identify every major responsibility the system must fulfil, defined by *what it is accountable for* — not how it is built. Each has purpose, inputs, outputs, dependencies, quality attributes, and constraints. |
| **Scope** | Responsibility definition only. No components, technologies, or implementation. A "responsibility" is an accountability, not a module. |
| **Inputs** | Business behaviour (rules, workflows, state machines), engineering challenges, drivers. |
| **Outputs** | The responsibility catalog consumed by boundary, ownership, and interaction analysis. |
| **Dependencies** | [Domain Model](../02-business-analysis/06-domain-model.md), [Workflows](../02-business-analysis/08-business-workflows.md), [State Machines](../02-business-analysis/07-state-machines.md). |
| **Cross References** | [Boundaries](04-responsibility-boundaries.md), [State Ownership](05-state-ownership.md), [Interactions](08-interaction-discovery.md). |
| **Related Business Documents** | [Business Rules](../02-business-analysis/02-business-rules.md), [Domain Events](../02-business-analysis/11-domain-events-catalog.md), [Validation Rules](../02-business-analysis/09-validation-rules.md), [Dictionary Management](../02-business-analysis/13-dictionary-management.md), [Lobby & Room Lifecycle](../02-business-analysis/14-lobby-room-lifecycle.md), [Session & Reconnection](../02-business-analysis/15-player-session-reconnection.md). |
| **Related Engineering Documents** | [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md). |

---

## Responsibility catalog (R-01 … R-16)

> These are accountabilities. Whether any two share an owner is analyzed in
> [Responsibility Boundaries](04-responsibility-boundaries.md); it is **not** decided here.

### R-01 Room Management
- **Purpose:** Own room existence: creation, unique room code, membership, host designation, capacity.
- **Inputs:** Create/join/leave intents; connection signals.
- **Outputs:** Room membership state; room lifecycle events (created, joined, left, host transferred, expired).
- **Business dependencies:** [Room rules BR-RC/JR/LR/HM](../02-business-analysis/02-business-rules.md), [Lobby & Room Lifecycle](../02-business-analysis/14-lobby-room-lifecycle.md).
- **Engineering dependencies:** Concurrent joins/leaves/host races (ENG-CO-01/02), capacity/nickname atomicity.
- **Quality attributes:** Consistency (one host), reliability, isolation.
- **Constraints:** Exactly one host ([INV-R1](../02-business-analysis/10-business-invariants.md)); unique live code; capacity bound.

### R-02 Lobby / Setup
- **Purpose:** Manage pre-match configuration: team selection, role claim, dictionary selection, readiness, start validation.
- **Inputs:** Team/role/dictionary intents; start intent.
- **Outputs:** Valid match configuration; setup events (team/role/dictionary changed).
- **Business dependencies:** [Team/Role rules BR-TA/RO/GS](../02-business-analysis/02-business-rules.md).
- **Engineering dependencies:** Boundary races at start (ENG-GP-10), simultaneous role claims.
- **Quality attributes:** Correctness (valid composition), consistency.
- **Constraints:** One Spymaster/team ([INV-T3](../02-business-analysis/10-business-invariants.md)); setup locked at start.

### R-03 Match Lifecycle
- **Purpose:** Own a match's existence from start to terminal (win/loss/abandonment) and rematch transitions.
- **Inputs:** Validated setup; terminal conditions from the rules engine; abandonment signals.
- **Outputs:** Match status; Game Result; match events (started, finished).
- **Business dependencies:** [Game start/end BR-GS/GE](../02-business-analysis/02-business-rules.md), [Game state machine](../02-business-analysis/07-state-machines.md#82-game-match-state-machine).
- **Engineering dependencies:** Rematch reset completeness (ENG-GP-09), abandonment handling.
- **Quality attributes:** Correctness, recoverability.
- **Constraints:** One active match/room; finished cannot resume ([INV-G7](../02-business-analysis/10-business-invariants.md)); one recorded result.

### R-04 Rules Engine (Adjudication)
- **Purpose:** The authoritative decision-maker: validate and resolve clues/guesses, update counts, evaluate terminal conditions per precedence. Language-independent.
- **Inputs:** Validated clue/guess intents + current authoritative state.
- **Outputs:** Reveals, count updates, turn/terminal decisions; the canonical outcome.
- **Business dependencies:** [Business Rules](../02-business-analysis/02-business-rules.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md), [Invariants](../02-business-analysis/10-business-invariants.md).
- **Engineering dependencies:** Determinism (ENG-GP-01/03/04), atomicity (ENG-ST-02).
- **Quality attributes:** Correctness, fairness, determinism, testability.
- **Constraints:** No transport/UI/language dependence; deterministic; the only source of outcomes.

### R-05 Board Generation
- **Purpose:** Produce the immutable 25-card board, key (9/8/7/1), and starting team at match start.
- **Inputs:** Selected dictionary version; randomness.
- **Outputs:** Board + key (delivered to Spymasters only); starting team.
- **Business dependencies:** [Board rules BR-BG](../02-business-analysis/02-business-rules.md), [Constants](../03-business-governance/03-business-constants-catalog.md).
- **Engineering dependencies:** Unbiased randomness, distinctness, seed protection (ENG-GP-08).
- **Quality attributes:** Fairness, correctness, security (seed).
- **Constraints:** 25 distinct words; exact partition; immutable for the match.

### R-06 Dictionary Provision
- **Purpose:** Supply versioned regional word sets for board generation; pin a version per match.
- **Inputs:** Region selection; content catalog.
- **Outputs:** A resolved, pinned dictionary version (words only).
- **Business dependencies:** [Dictionary Management](../02-business-analysis/13-dictionary-management.md), [INV-D1/D3](../02-business-analysis/10-business-invariants.md).
- **Engineering dependencies:** Version pinning, retention while in use (ENG-DC-02).
- **Quality attributes:** Localizability, maintainability.
- **Constraints:** Affects words only — never rules; immutable versions; ≥25 words.

### R-07 Turn Management
- **Purpose:** Own turn/phase progression (clue → guess → end), active-team pointer, guess allowance.
- **Inputs:** Rules-engine outcomes; end-turn intents.
- **Outputs:** Turn/phase state; turn events (started, ended).
- **Business dependencies:** [Turn rules BR-TO/TE](../02-business-analysis/02-business-rules.md), [Turn state machine](../02-business-analysis/07-state-machines.md#83-turn-state-machine).
- **Engineering dependencies:** Transition atomicity (ENG-GP-05).
- **Quality attributes:** Correctness, consistency.
- **Constraints:** One active turn; one active clue; strict alternation.

### R-08 Clue Processing
- **Purpose:** Accept and structurally validate a Spymaster's clue; establish the active clue and guess allowance.
- **Inputs:** Clue intent (active team's Spymaster).
- **Outputs:** Active clue; clue event.
- **Business dependencies:** [Clue rules BR-CL](../02-business-analysis/02-business-rules.md), [Validation V-CLUE](../02-business-analysis/09-validation-rules.md).
- **Engineering dependencies:** Language-neutral normalization (ENG-GP-06).
- **Quality attributes:** Fairness, correctness, language-neutrality.
- **Constraints:** Structural rules only (semantics are social); one clue/turn.

### R-09 Guess Processing
- **Purpose:** Accept and resolve a guess: reveal, update counts, drive turn/terminal outcome, serialized.
- **Inputs:** Guess intent (active team's Operative).
- **Outputs:** Reveal + outcome; guess/reveal events.
- **Business dependencies:** [Guess rules BR-GV/CG/IG/NC/OPP/ASN](../02-business-analysis/02-business-rules.md).
- **Engineering dependencies:** Simultaneous guesses, first-valid-wins (ENG-GP-01), idempotency.
- **Quality attributes:** Correctness, fairness, determinism.
- **Constraints:** Unrevealed target; within allowance; serialized resolution.

### R-10 Validation & Authorization
- **Purpose:** Gate every intent against role/team/phase/state before any effect; produce catalogued rejections.
- **Inputs:** Any intent + authoritative state + actor identity/role.
- **Outputs:** Accept/reject with a business error code.
- **Business dependencies:** [Validation Rules](../02-business-analysis/09-validation-rules.md), [Error Catalog](../02-business-analysis/12-business-error-catalog.md), [SEC-1..8](../02-business-analysis/01-software-requirements.md#212-security-considerations).
- **Engineering dependencies:** No-bypass, no client trust (ENG-FP-02/04).
- **Quality attributes:** Security, correctness, fairness.
- **Constraints:** Precedes every effect; deny-by-default; never trusts the client.

### R-11 Notification / Delivery (role-filtered)
- **Purpose:** Distribute authoritative state changes to a room's participants with correct per-role visibility; receive intents.
- **Inputs:** Committed state changes/events; inbound intents.
- **Outputs:** Role-filtered state/events to each participant.
- **Business dependencies:** [Domain Events](../02-business-analysis/11-domain-events-catalog.md), role visibility ([INV-B9](../02-business-analysis/10-business-invariants.md)).
- **Engineering dependencies:** Loss/dup/reorder tolerance, fan-out (ENG-RT-01, ENG-SC-03), leak prevention (ENG-FP-01).
- **Quality attributes:** Fairness (no leak), real-time performance, consistency.
- **Constraints:** Transports/filters only — never adjudicates; must never send unrevealed ownership to non-Spymasters.

### R-12 Connection / Presence Management
- **Purpose:** Track per-player connectivity; drive disconnect/reconnect, single-active-connection, grace timers; signal presence changes.
- **Inputs:** Connect/disconnect signals; reconnect intents.
- **Outputs:** Connection state; presence events; pause/migration/abandonment triggers.
- **Business dependencies:** [Session & Reconnection](../02-business-analysis/15-player-session-reconnection.md), [Connection state machine](../02-business-analysis/07-state-machines.md#84-player-connection-state-machine).
- **Engineering dependencies:** Duplicate connections (ENG-CO-04), host-disconnect timing (ENG-RT-05).
- **Quality attributes:** Reliability, fairness (one actor), availability.
- **Constraints:** One active connection/identity; disconnect ≠ immediate removal.

### R-13 Session / Identity
- **Purpose:** Issue and manage transient, room-scoped, PII-free identity + reconnect tokens; the future-auth seam.
- **Inputs:** Join; reconnect.
- **Outputs:** Identity/token; validity decisions.
- **Business dependencies:** [Temporary identity BR-JR-7, INV-P2](../02-business-analysis/10-business-invariants.md), [Future auth AUTH-1..5](../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations).
- **Engineering dependencies:** Token loss/theft/expiry (ENG-SE-02).
- **Quality attributes:** Security, extensibility, privacy.
- **Constraints:** Transient, room-scoped, PII-free; auth attaches additively here.

### R-14 State & Session Store (authoritative state custody)
- **Purpose:** Hold the authoritative room/match/turn/connection state for the room lifetime; support atomic commit and recovery reads.
- **Inputs:** Committed state from the rules engine and lifecycle owners.
- **Outputs:** Current authoritative state; recovery snapshots.
- **Business dependencies:** [Game State](../02-business-analysis/06-domain-model.md), [Data Lifecycle](../03-business-governance/05-data-lifecycle-retention.md).
- **Engineering dependencies:** Atomicity, recovery boundary (ENG-ST-02/04, ENG-RE-01).
- **Quality attributes:** Consistency, recoverability.
- **Constraints:** One authoritative state per room; storage only — never adjudicates.

### R-15 State Recovery
- **Purpose:** Restore a room/match to its last consistent state after interruption, without replaying terminal effects.
- **Inputs:** Recovery snapshots; interruption signals.
- **Outputs:** Restored authoritative state; resumed play.
- **Business dependencies:** [Recovery NFR-11](../02-business-analysis/01-software-requirements.md#29-non-functional-requirements), [QM-16](../03-business-governance/04-quality-metrics.md).
- **Engineering dependencies:** Crash/partial-op recovery (ENG-RE-01/02).
- **Quality attributes:** Recoverability, correctness.
- **Constraints:** Room-lifetime scope; idempotent; no double terminal effects.

### R-16 Room Cleanup / Expiry
- **Purpose:** Reclaim empty/idle/abandoned rooms race-safely; release codes; record abandonment.
- **Inputs:** Inactivity/emptiness signals; abandonment.
- **Outputs:** Expired/closed rooms; expiry events; released codes.
- **Business dependencies:** [Expiry rules BR-RX](../02-business-analysis/02-business-rules.md).
- **Engineering dependencies:** Expiry-vs-activity races (ENG-CO-05), timers at scale (ENG-SC-02).
- **Quality attributes:** Reliability, scalability, cost.
- **Constraints:** Inactivity-based (reset by activity); result recorded before expiry.

### R-17 Observability (supporting)
- **Purpose:** Expose PII-free business-event and outcome visibility for verification and diagnosis.
- **Inputs:** Domain events; results; lifecycle signals.
- **Outputs:** Observable signals/metrics (no PII, no ownership).
- **Business dependencies:** [Domain Events](../02-business-analysis/11-domain-events-catalog.md), [NFR-13](../02-business-analysis/01-software-requirements.md#29-non-functional-requirements).
- **Engineering dependencies:** Telemetry leak avoidance (ENG-FP-01).
- **Quality attributes:** Observability, auditability, security.
- **Constraints:** PII-free; never carries unrevealed ownership.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial system responsibilities catalog (R-01…R-17). |
