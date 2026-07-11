# 27. Engineering Challenges — Enrichment Layer

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Enrichment companion — **does not modify [26](01-engineering-challenges-risk-analysis.md)** |
| **Purpose** | Append engineering **metadata** to every challenge in [26 — Engineering Challenges & Risk Analysis](01-engineering-challenges-risk-analysis.md) so each becomes easier to prioritize, implement, validate, and trace — without changing any existing analysis, recommendation, or conclusion. |
| **Relationship** | Keyed by Challenge ID. Document 26 remains the source of truth for the analysis; this document only adds the eight metadata sections requested and a prioritization summary. No challenge is rewritten, reordered, or removed. |
| **Technology** | Neutral (no architecture, technology, framework, or pattern is *chosen*; patterns are listed for education only). |

## Table of Contents
1. [How to Read This Layer](#1-how-to-read-this-layer)
2. [Scoring Scales & RPN Formula](#2-scoring-scales--rpn-formula)
3. [Enrichment by Challenge](#3-enrichment-by-challenge)
4. [Master Priority Table](#4-master-priority-table)
5. [Architect's Focus Summary](#5-architects-focus-summary)
6. [Revision History](#6-revision-history)

---

## 1. How to Read This Layer

For each Challenge ID in [26](01-engineering-challenges-risk-analysis.md), this layer appends:

1. **Decision Deferred To** — where the final decision is finalized (not decided here).
2. **Implementation Difficulty** — Very Low…Very High, with a brief why (Severity ≠ Difficulty).
3. **Testing Considerations** — validation types, what to test, expected behaviour, failure indicators.
4. **MVP Applicability** — one bucket, with why (to avoid over-engineering).
5. **RPN** — Risk Priority Number, Priority Level, Recommended Implementation Order.
6. **Validation Scenario** — Scenario / Expected / Failure / Business Impact / Recovery.
7. **Known Industry Patterns** — all relevant proven patterns and why each is commonly used.
8. **Decision Drivers** — ranked forces behind the recommended solution.

Nothing here overrides [26](01-engineering-challenges-risk-analysis.md); where this layer names patterns, it **lists** them (per the instruction not to choose one unless clearly superior).

## 2. Scoring Scales & RPN Formula

To make the RPN reproducible, the qualitative labels already in [26](01-engineering-challenges-risk-analysis.md) map to fixed numbers:

| Factor | Mapping |
|--------|---------|
| **Severity (S)** | Low = 1 · Medium = 2 · High = 3 · Critical = 4 |
| **Likelihood (L)** | Rare = 1 · Occasional = 2 · Frequent = 3 |
| **Implementation Difficulty (D)** | Very Low = 1 · Low = 2 · Medium = 3 · High = 4 · Very High = 5 |

**RPN = S × L × D** (range 1–60).

| Priority Level | RPN band |
|----------------|----------|
| **P1 — Critical** | ≥ 30 |
| **P2 — High** | 18 – 29 |
| **P3 — Medium** | 9 – 17 |
| **P4 — Low** | ≤ 8 |

**Recommended Implementation Order** is derived primarily from RPN (descending), with
foundational/enabling challenges pulled earlier where later challenges depend on them; the
consolidated ordering is in the [Master Priority Table](#4-master-priority-table).

> Severity/Likelihood values are taken **unchanged** from [26](01-engineering-challenges-risk-analysis.md);
> only Difficulty (and therefore RPN) is newly assigned here.

---

## 3. Enrichment by Challenge

### 6. Gameplay Risks

#### ENG-GP-01 — Simultaneous guesses / double-reveal
- **Decision Deferred To:** Real-Time Communication Design; Implementation Phase.
- **Implementation Difficulty:** **High (4)** — requires per-room serialization plus an atomic "validate→reveal→count→terminal-check→turn" step and re-validation of queued intents.
- **Testing Considerations:** *Concurrency Tests* (primary), *Integration*, *E2E*. Test: burst of parallel guesses on the same/different cards. Expected: exactly one applied first, others re-evaluated. Failure indicators: two cards revealed for one slot, mis-decremented counts, skipped turn-end. Special: deterministic replay of interleavings.
- **MVP Applicability:** **Critical for MVP** — core correctness/fairness; the game is unfair without it.
- **RPN:** S4 × L3 × D4 = **48** · **P1** · Order **#1**.
- **Validation Scenario:** *Scenario:* two operatives guess within 10 ms. *Expected:* first valid reveal applied, second rejected/re-evaluated. *Failure:* both applied. *Business Impact:* corrupted outcome. *Recovery:* reconcile to authoritative state; no double effect (CR-4).
- **Known Industry Patterns:** Single Writer (serialize per room), Command Queue (order intents), Optimistic Concurrency (state-token reject), Pessimistic Concurrency (lock), State Versioning, Idempotency, Command Validation — all commonly used to make concurrent mutations deterministic.
- **Decision Drivers (ranked):** 1) Correctness, 2) Gameplay Fairness, 3) Consistency, 4) Reliability.

#### ENG-GP-02 — Double-click & duplicate intent submission
- **Decision Deferred To:** API Design; Real-Time Communication Design; Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — idempotency keys + state guards across all state-changing intents.
- **Testing Considerations:** *Integration*, *Concurrency*. Test: same intent submitted twice / retried. Expected: applied once. Failure indicators: two clues, double turn-end, double start. Special: simulate network retry, not just UI double-tap.
- **MVP Applicability:** **Required for MVP** — common real-world input; cheap to get wrong.
- **RPN:** S3 × L3 × D3 = **27** · **P2** · Order **#9**.
- **Validation Scenario:** *Scenario:* retry of "end turn". *Expected:* turn ends once. *Failure:* opponent's turn skipped. *Business Impact:* unfair turn loss. *Recovery:* idempotent no-op on replay.
- **Known Industry Patterns:** Idempotency, Command Validation, Retry Pattern (client), Message Sequencing, State Versioning — used to make retries safe.
- **Decision Drivers (ranked):** 1) Correctness, 2) User Experience, 3) Reliability.

#### ENG-GP-03 — Win / last-card detection ordering
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — ordering discipline (check both teams after each reveal, before continuation).
- **Testing Considerations:** *Unit* (resolution order), *Integration*, *E2E*. Test: winning reveal by either team; win vs guess-limit coincidence. Expected: match ends immediately, correct winner. Failure indicators: play continues past a win; wrong winner on opponent's-last-card.
- **MVP Applicability:** **Critical for MVP** — outcome correctness.
- **RPN:** S4 × L2 × D3 = **24** · **P2** · Order **#12**.
- **Validation Scenario:** *Scenario:* active team reveals opponent's last agent. *Expected:* opponent wins, match ends. *Failure:* turn continues. *Business Impact:* invalid result. *Recovery:* deterministic re-evaluation (INV-O2).
- **Known Industry Patterns:** Finite State Machine, Immutable State, Command Validation — used to make terminal detection deterministic.
- **Decision Drivers (ranked):** 1) Correctness, 2) Gameplay Fairness, 3) Consistency.

#### ENG-GP-04 — Assassin precedence & terminal ordering
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — a single ordered resolution function; rule is unambiguous.
- **Testing Considerations:** *Unit* (precedence), *Integration*. Test: assassin as bonus guess / near-win. Expected: guessing team loses immediately. Failure indicators: counts updated or other end applied first.
- **MVP Applicability:** **Critical for MVP** — instant-loss correctness.
- **RPN:** S4 × L2 × D2 = **16** · **P3** · Order **#26**.
- **Validation Scenario:** *Scenario:* assassin revealed. *Expected:* opponent wins at once. *Failure:* another outcome. *Business Impact:* wrong result. *Recovery:* terminal freeze (INV-G7).
- **Known Industry Patterns:** Finite State Machine, Command Validation, Immutable State — used to enforce absolute precedence.
- **Decision Drivers (ranked):** 1) Correctness, 2) Gameplay Fairness.

#### ENG-GP-05 — Turn-transition atomicity
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — atomic, idempotent multi-field transition.
- **Testing Considerations:** *Unit*, *Concurrency*, *Integration*. Test: end-turn coinciding with a guess resolution. Expected: single clean transition. Failure indicators: two active clues, stale guess count, ambiguous active team.
- **MVP Applicability:** **Critical for MVP** — turn integrity.
- **RPN:** S3 × L3 × D3 = **27** · **P2** · Order **#8**.
- **Validation Scenario:** *Scenario:* turn ends on neutral. *Expected:* clue cleared, allowance reset, opponent active. *Failure:* residual clue. *Business Impact:* illegal state. *Recovery:* re-apply atomic transition (INV-G3).
- **Known Industry Patterns:** Finite State Machine, Single Writer, Immutable State, Idempotency — used to keep transitions consistent.
- **Decision Drivers (ranked):** 1) Correctness, 2) Consistency, 3) Reliability.

#### ENG-GP-06 — Structural clue validation vs semantics
- **Decision Deferred To:** Implementation Phase (with content-team input on normalization policy).
- **Implementation Difficulty:** **Medium (3)** — language-neutral normalization for multi-script inputs.
- **Testing Considerations:** *Unit* (normalization), *Integration*. Test: multi-word, whitespace, case, Unicode variants, clue equal to revealed vs unrevealed word. Expected: only structural rejections. Failure indicators: legal clue blocked; revealed-word clue blocked; language-specific stemming applied.
- **MVP Applicability:** **Required for MVP** — clues are core; must be fair and neutral.
- **RPN:** S2 × L3 × D3 = **18** · **P2** · Order **#18**.
- **Validation Scenario:** *Scenario:* clue equals an unrevealed board word. *Expected:* rejected. *Failure:* accepted. *Business Impact:* trivial clue. *Recovery:* re-submit prompt.
- **Known Industry Patterns:** Command Validation, Immutable State (board word set), Server Authority — used to enforce input rules neutrally.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Correctness, 3) Consistency (language-independence).

#### ENG-GP-07 — Guess-limit accounting (N+1, 0/∞)
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — a small allowance policy + counter; risk is off-by-one.
- **Testing Considerations:** *Unit* (boundary), *Integration*. Test: N, N+1, 0, unlimited, min-one. Expected: correct allowance and mandatory first guess. Failure indicators: extra/missing guess; unlimited modeled as a finite cap.
- **MVP Applicability:** **Critical for MVP** — directly affects outcomes.
- **RPN:** S3 × L3 × D2 = **18** · **P2** · Order **#17**.
- **Validation Scenario:** *Scenario:* clue "3". *Expected:* up to 4 guesses. *Failure:* 3 or 5. *Business Impact:* unfair turn length. *Recovery:* re-derive allowance from clue.
- **Known Industry Patterns:** Finite State Machine, Command Validation, Immutable State — used for deterministic rule accounting.
- **Decision Drivers (ranked):** 1) Correctness, 2) Gameplay Fairness.

#### ENG-GP-08 — Board generation randomness & fairness
- **Decision Deferred To:** Security Design (randomness/seed protection); Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — unbiased shuffle, distinctness, protected seed.
- **Testing Considerations:** *Unit* (distribution/distinctness), *Statistical/Load* (bias over many boards), *Security* (seed non-leak). Test: many generations. Expected: uniform ownership/starting-team, 25 distinct words. Failure indicators: bias, duplicates, predictable/leaked layout.
- **MVP Applicability:** **Required for MVP** — fairness; low frequency but high impact.
- **RPN:** S3 × L1 × D3 = **9** · **P3** · Order **#27**.
- **Validation Scenario:** *Scenario:* generate 10k boards. *Expected:* even distributions, no duplicates. *Failure:* skew/dupes. *Business Impact:* systemic unfairness. *Recovery:* regenerate; audit randomness source.
- **Known Industry Patterns:** Server Authority, Immutable State, Snapshot Recovery (seed for audit) — used for fair, reproducible generation.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Security, 3) Observability (auditability).

#### ENG-GP-09 — Rematch reset completeness
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — clean fresh-match construction carrying only permitted setup.
- **Testing Considerations:** *Integration*, *E2E*. Test: rematch after win and after abandonment. Expected: fully fresh board/turn/counts. Failure indicators: residual reveals/clue/counts; stale dictionary version.
- **MVP Applicability:** **Required for MVP** — rematch is in scope.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#19**.
- **Validation Scenario:** *Scenario:* start rematch. *Expected:* new independent match. *Failure:* prior state bleeds in. *Business Impact:* corrupted new match. *Recovery:* rebuild match instance.
- **Known Industry Patterns:** Immutable State (per-match instance), Finite State Machine, Command Validation — used for clean lifecycle resets.
- **Decision Drivers (ranked):** 1) Correctness, 2) Consistency, 3) Maintainability.

#### ENG-GP-10 — Team/role changes at lobby↔match boundary
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — hard lock at start; handle in-flight intents at the boundary.
- **Testing Considerations:** *Concurrency*, *Integration*. Test: switch/claim exactly at start. Expected: rejected or deferred; setup locked. Failure indicators: two Spymasters; team change mid-match.
- **MVP Applicability:** **Required for MVP** — protects match integrity.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#20**.
- **Validation Scenario:** *Scenario:* Spymaster claim during start. *Expected:* rejected. *Failure:* second Spymaster. *Business Impact:* illegal composition. *Recovery:* re-validate at start (INV-T3/T5).
- **Known Industry Patterns:** Single Writer, Finite State Machine, Command Validation — used for boundary consistency.
- **Decision Drivers (ranked):** 1) Correctness, 2) Gameplay Fairness, 3) Consistency.

### 7. State Management Risks

#### ENG-ST-01 — Illegal / invalid state transitions
- **Decision Deferred To:** Software Architecture Phase; Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — enumerate machines and default-deny.
- **Testing Considerations:** *Unit* (transition whitelist), *Integration*. Test: every out-of-phase intent. Expected: rejected. Failure indicators: any undefined transition applied.
- **MVP Applicability:** **Critical for MVP** — prevents corruption.
- **RPN:** S4 × L2 × D3 = **24** · **P2** · Order **#11**.
- **Validation Scenario:** *Scenario:* guess in clue phase. *Expected:* rejected. *Failure:* applied. *Business Impact:* corrupted state. *Recovery:* default-deny (V-STATE-2).
- **Known Industry Patterns:** Finite State Machine, Command Validation, Server Authority — used to bound legal behaviour.
- **Decision Drivers (ranked):** 1) Correctness, 2) Consistency, 3) Maintainability.

#### ENG-ST-02 — Partial state updates / non-atomic mutations
- **Decision Deferred To:** Software Architecture Phase; Implementation Phase.
- **Implementation Difficulty:** **High (4)** — atomic multi-field commit with invariant checks.
- **Testing Considerations:** *Concurrency*, *Recovery*, *Integration*. Test: interrupt between sub-updates. Expected: all-or-nothing. Failure indicators: count≠revealed cards; two active clues; broadcast of half state.
- **MVP Applicability:** **Critical for MVP** — foundational integrity.
- **RPN:** S4 × L2 × D4 = **32** · **P1** · Order **#7**.
- **Validation Scenario:** *Scenario:* reveal interrupted mid-apply. *Expected:* no partial effect. *Failure:* inconsistent counts. *Business Impact:* illegal state. *Recovery:* commit-then-broadcast; validate invariants pre-commit.
- **Known Industry Patterns:** Immutable State, Single Writer, Compensating Actions, Snapshot Recovery, Event Sourcing — used for atomic, consistent mutations.
- **Decision Drivers (ranked):** 1) Correctness, 2) Consistency, 3) Reliability.

#### ENG-ST-03 — State synchronization & role-filtered views
- **Decision Deferred To:** Real-Time Communication Design; Security Design.
- **Implementation Difficulty:** **High (4)** — per-role projections enforced at the delivery boundary.
- **Testing Considerations:** *Security* (leak), *Integration*, *E2E*. Test: every delivery path for each role. Expected: Operatives never receive unrevealed ownership. Failure indicators: Key in a non-Spymaster payload.
- **MVP Applicability:** **Critical for MVP** — fairness depends on it.
- **RPN:** S3 × L3 × D4 = **36** · **P1** · Order **#4**.
- **Validation Scenario:** *Scenario:* Operative loads state. *Expected:* no unrevealed ownership present. *Failure:* ownership leaked. *Business Impact:* game destroyed. *Recovery:* filter at source (INV-B9).
- **Known Industry Patterns:** Server Authority, Publish/Subscribe, Immutable State, Command Validation — used for consistent, filtered delivery.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Security, 3) Consistency.

#### ENG-ST-04 — State recovery after interruption
- **Decision Deferred To:** Software Architecture Phase; Infrastructure Design.
- **Implementation Difficulty:** **High (4)** — recoverable authoritative state + defined recovery point.
- **Testing Considerations:** *Recovery*, *Chaos*, *Integration*. Test: interrupt then recover. Expected: resume at last consistent state; no replayed terminals. Failure indicators: rewound progress, duplicated reveal.
- **MVP Applicability:** **Required for MVP** — resilience expectation (room-lifetime).
- **RPN:** S3 × L2 × D4 = **24** · **P2** · Order **#13**.
- **Validation Scenario:** *Scenario:* transient interruption mid-turn. *Expected:* resume same phase/board. *Failure:* corrupted resume. *Business Impact:* lost/duplicated match state. *Recovery:* recover to last committed state (QM-16).
- **Known Industry Patterns:** Snapshot Recovery, Event Sourcing, Immutable State, Idempotency — used for consistent recovery.
- **Decision Drivers (ranked):** 1) Reliability, 2) Correctness, 3) Consistency.

### 8. Concurrency Risks

#### ENG-CO-01 — Concurrent joins & capacity/nickname races
- **Decision Deferred To:** Software Architecture Phase; Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — atomic admit combining capacity + nickname checks.
- **Testing Considerations:** *Concurrency*, *Load*, *Integration*. Test: burst joins at capacity / identical nicknames. Expected: never over capacity; never duplicate nickname. Failure indicators: N+1 members; two identical nicknames.
- **MVP Applicability:** **Required for MVP** — frequent real-world case.
- **RPN:** S3 × L3 × D3 = **27** · **P2** · Order **#10**.
- **Validation Scenario:** *Scenario:* two joins for the last slot. *Expected:* one admitted. *Failure:* both. *Business Impact:* over-capacity room. *Recovery:* atomic compare-and-add (INV-R5).
- **Known Industry Patterns:** Single Writer, Pessimistic/Optimistic Concurrency, Command Queue, Idempotency — used for safe membership mutations.
- **Decision Drivers (ranked):** 1) Correctness, 2) Consistency, 3) User Experience.

#### ENG-CO-02 — Concurrent leaves & host-migration races
- **Decision Deferred To:** Software Architecture Phase; Implementation Phase.
- **Implementation Difficulty:** **High (4)** — atomic migration against post-leave membership.
- **Testing Considerations:** *Concurrency*, *Chaos*, *Integration*. Test: Host + successor leave together; all leave. Expected: exactly one Host or room expires. Failure indicators: zero/two Hosts; orphan room.
- **MVP Applicability:** **Required for MVP** — rooms must not orphan.
- **RPN:** S3 × L2 × D4 = **24** · **P2** · Order **#14**.
- **Validation Scenario:** *Scenario:* Host and next-in-line leave simultaneously. *Expected:* deterministic single Host or expiry. *Failure:* no Host. *Business Impact:* stuck room. *Recovery:* recompute successor (INV-R1).
- **Known Industry Patterns:** Single Writer, Finite State Machine, Compensating Actions — used for deterministic ownership transfer.
- **Decision Drivers (ranked):** 1) Reliability, 2) Correctness, 3) Operational Simplicity.

#### ENG-CO-03 — Simultaneous host actions
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — authorize each action against the current Host.
- **Testing Considerations:** *Concurrency*, *Integration*. Test: stale + new Host both act. Expected: only current Host succeeds. Failure indicators: double start; conflicting config.
- **MVP Applicability:** **Recommended for MVP** — low likelihood/severity but cheap.
- **RPN:** S2 × L2 × D2 = **8** · **P4** · Order **#40**.
- **Validation Scenario:** *Scenario:* demoted Host starts match. *Expected:* rejected. *Failure:* second start. *Business Impact:* duplicate match. *Recovery:* revalidate Host (NOT_ROOM_HOST).
- **Known Industry Patterns:** Server Authority, Command Validation, State Versioning — used to reject stale authority.
- **Decision Drivers (ranked):** 1) Correctness, 2) Consistency.

#### ENG-CO-04 — Simultaneous reconnects / duplicate connections
- **Decision Deferred To:** Real-Time Communication Design; Security Design.
- **Implementation Difficulty:** **High (4)** — atomic supersede-and-close of prior connection; discard its queued intents.
- **Testing Considerations:** *Concurrency*, *Chaos*, *Integration*. Test: two reconnects ms apart; old socket mid-intent. Expected: one active connection; older discarded. Failure indicators: double actions; dual role-views.
- **MVP Applicability:** **Required for MVP** — frequent (tabs, network switches).
- **RPN:** S3 × L3 × D4 = **36** · **P1** · Order **#5**.
- **Validation Scenario:** *Scenario:* same identity, two devices. *Expected:* newest supersedes. *Failure:* both act. *Business Impact:* fairness/consistency break. *Recovery:* bind actions to current connection (INV-P4).
- **Known Industry Patterns:** Single Writer, Idempotency, State Versioning, Command Queue — used to enforce one active actor.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Consistency, 3) Reliability.

#### ENG-CO-05 — Expiry racing with live activity
- **Decision Deferred To:** Implementation Phase; Infrastructure Design.
- **Implementation Difficulty:** **Medium (3)** — atomic re-check at fire time; order result before expiry.
- **Testing Considerations:** *Concurrency*, *Integration*. Test: activity during expiry step; empty at match end. Expected: no false expiry; result recorded before close. Failure indicators: live room closed; skipped result.
- **MVP Applicability:** **Required for MVP** — avoids losing live rooms.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#21**.
- **Validation Scenario:** *Scenario:* idle timer fires as a guess arrives. *Expected:* expiry aborted. *Failure:* room closed mid-play. *Business Impact:* lost match. *Recovery:* reset-on-activity (F-VAL-08).
- **Known Industry Patterns:** Single Writer, Finite State Machine, Compensating Actions — used to serialize expiry vs activity.
- **Decision Drivers (ranked):** 1) Reliability, 2) Correctness, 3) User Experience.

### 9. Real-Time Risks

#### ENG-RT-01 — Lost / duplicated / out-of-order messages
- **Decision Deferred To:** Real-Time Communication Design.
- **Implementation Difficulty:** **High (4)** — versioning/sequencing + resync + idempotent intents.
- **Testing Considerations:** *Chaos* (drop/dup/reorder), *Integration*, *E2E*. Test: injected network faults. Expected: clients converge to authoritative state. Failure indicators: stale-over-new applied; missed terminal event.
- **MVP Applicability:** **Critical for MVP** — real networks guarantee this happens.
- **RPN:** S3 × L3 × D4 = **36** · **P1** · Order **#3**.
- **Validation Scenario:** *Scenario:* update delivered twice, out of order. *Expected:* only newest applied. *Failure:* regressed view. *Business Impact:* wrong board shown. *Recovery:* version + resync (NFR-2).
- **Known Industry Patterns:** State Versioning, Message Sequencing, Idempotency, Snapshot Recovery, Publish/Subscribe, Retry Pattern — used to tolerate unreliable transport.
- **Decision Drivers (ranked):** 1) Consistency, 2) Correctness, 3) Reliability.

#### ENG-RT-02 — Delayed events & slow clients
- **Decision Deferred To:** Real-Time Communication Design; Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — server rejects stale intents; client reconciles.
- **Testing Considerations:** *Integration*, *E2E*, *Load*. Test: artificially lagged client acts on stale state. Expected: specific rejection + resync. Failure indicators: stale intent applied; no reconciliation.
- **MVP Applicability:** **Required for MVP** — common on mobile.
- **RPN:** S2 × L3 × D3 = **18** · **P2** · Order **#22**.
- **Validation Scenario:** *Scenario:* guess a card revealed moments ago. *Expected:* rejected. *Failure:* re-revealed. *Business Impact:* confusion/unfairness. *Recovery:* client snaps to latest (V-GUESS-3).
- **Known Industry Patterns:** Server Authority, State Versioning, Command Validation, Request/Response — used to reconcile slow clients.
- **Decision Drivers (ranked):** 1) Correctness, 2) User Experience, 3) Consistency.

#### ENG-RT-03 — Reconnection & state resynchronization
- **Decision Deferred To:** Real-Time Communication Design.
- **Implementation Difficulty:** **High (4)** — reliable role-filtered snapshot + pause resume.
- **Testing Considerations:** *Recovery*, *Integration*, *E2E*, *Security*. Test: reconnect in each role/phase. Expected: correct projection restored fast (QM-07). Failure indicators: wrong role view; Key leak; pause not resumed.
- **MVP Applicability:** **Critical for MVP** — mobile drops are routine.
- **RPN:** S3 × L3 × D4 = **36** · **P1** · Order **#6**.
- **Validation Scenario:** *Scenario:* Spymaster reconnects. *Expected:* Key view restored; play resumes. *Failure:* Operative view or no Key. *Business Impact:* stalled/unfair game. *Recovery:* full role-filtered snapshot (INV-P5).
- **Known Industry Patterns:** Snapshot Recovery, State Versioning, Server Authority, Idempotency — used for reliable resume.
- **Decision Drivers (ranked):** 1) Reliability, 2) Gameplay Fairness, 3) User Experience.

#### ENG-RT-04 — Multiple tabs / devices per identity
- **Decision Deferred To:** Real-Time Communication Design; Security Design.
- **Implementation Difficulty:** **Medium (3)** — subset of ENG-CO-04 (single active connection).
- **Testing Considerations:** *Concurrency*, *Integration*. Test: two tabs same identity. Expected: newest active, older disabled. Failure indicators: both act; stale Spymaster tab active.
- **MVP Applicability:** **Required for MVP** — users open multiple tabs.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#23**.
- **Validation Scenario:** *Scenario:* second tab opened. *Expected:* takeover, first disabled. *Failure:* dual control. *Business Impact:* confusion/fairness. *Recovery:* supersede (INV-P4).
- **Known Industry Patterns:** Single Writer, State Versioning, Idempotency — used to enforce one actor.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Consistency, 3) User Experience.

#### ENG-RT-05 — Host disconnection mid-critical-phase
- **Decision Deferred To:** Real-Time Communication Design; Implementation Phase.
- **Implementation Difficulty:** **High (4)** — independent pause + migration timers; precedence with match-end.
- **Testing Considerations:** *Concurrency*, *Chaos*, *Integration*. Test: Host==Spymaster drops in clue phase; drop at match end. Expected: correct pause and/or migration; match-end preempts. Failure indicators: conflated effects; match interrupted by control loss.
- **MVP Applicability:** **Required for MVP** — plausible and disruptive.
- **RPN:** S3 × L2 × D4 = **24** · **P2** · Order **#15**.
- **Validation Scenario:** *Scenario:* Host+Spymaster disconnects during clue. *Expected:* clue pauses; control migrates after grace. *Failure:* match state altered by migration. *Business Impact:* corruption/stall. *Recovery:* independent handling (BR-EC-11).
- **Known Industry Patterns:** Finite State Machine, Single Writer, Compensating Actions, Circuit Breaker (grace gating) — used to separate concerns.
- **Decision Drivers (ranked):** 1) Reliability, 2) Correctness, 3) User Experience.

### 10. Fair-Play Risks

#### ENG-FP-01 — Hidden-card / Key leakage
- **Decision Deferred To:** Security Design; Real-Time Communication Design.
- **Implementation Difficulty:** **High (4)** — audit *every* delivery path; ownership never leaves server for non-Spymasters.
- **Testing Considerations:** *Security* (primary, negative testing), *Integration*, *E2E*. Test: inspect all payloads per role/path incl. reconnect, rematch, telemetry. Expected: zero unrevealed ownership to non-Spymasters. Failure indicators: any ownership leak.
- **MVP Applicability:** **Critical for MVP** — the single existential fairness risk.
- **RPN:** S4 × L2 × D4 = **32** · **P1** · Order **#2**.
- **Validation Scenario:** *Scenario:* audit Operative payloads. *Expected:* no unrevealed ownership. *Failure:* leak found. *Business Impact:* game destroyed. *Recovery:* filter at source; scrub telemetry (INV-B9).
- **Known Industry Patterns:** Server Authority, Immutable State, Publish/Subscribe (filtered), Command Validation — used to segregate secret information.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Security, 3) Correctness.

#### ENG-FP-02 — Client manipulation & forged intents
- **Decision Deferred To:** Security Design; Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — uniform server-side authorization on every intent.
- **Testing Considerations:** *Security*, *Integration*. Test: forged role/team/phase intents. Expected: all rejected. Failure indicators: any client-declared authority honored.
- **MVP Applicability:** **Critical for MVP** — cheating protection.
- **RPN:** S4 × L2 × D3 = **24** · **P2** · Order **#16**.
- **Validation Scenario:** *Scenario:* Operative sends a clue. *Expected:* rejected. *Failure:* accepted. *Business Impact:* corrupted play. *Recovery:* authorize from server state (SEC-1/4).
- **Known Industry Patterns:** Server Authority, Command Validation, Rate Limiting — used to prevent forged actions.
- **Decision Drivers (ranked):** 1) Security, 2) Gameplay Fairness, 3) Correctness.

#### ENG-FP-03 — Replay / duplicate command attacks
- **Decision Deferred To:** Security Design; Real-Time Communication Design.
- **Implementation Difficulty:** **Medium (3)** — idempotency + turn/phase-bound intents.
- **Testing Considerations:** *Security*, *Concurrency*. Test: replay old valid intents. Expected: no-op/reject. Failure indicators: re-triggered guess/end-turn.
- **MVP Applicability:** **Required for MVP** — abuse resistance.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#24**.
- **Validation Scenario:** *Scenario:* replay a prior guess. *Expected:* rejected (card revealed / stale turn). *Failure:* re-applied. *Business Impact:* unfair reveal. *Recovery:* bind to state version (CR-4).
- **Known Industry Patterns:** Idempotency, Message Sequencing, State Versioning, Command Validation, Rate Limiting — used to defeat replay.
- **Decision Drivers (ranked):** 1) Security, 2) Correctness, 3) Gameplay Fairness.

#### ENG-FP-04 — Out-of-turn / wrong-role action attempts
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — uniform pre-checks on the hot path.
- **Testing Considerations:** *Integration*, *Security*. Test: act out of turn/role. Expected: specific rejection, no mutation. Failure indicators: partial apply; silent confusion.
- **MVP Applicability:** **Critical for MVP** — happens constantly, must never mutate.
- **RPN:** S3 × L3 × D2 = **18** · **P2** · Order **#25**.
- **Validation Scenario:** *Scenario:* Operative guesses on opponent's turn. *Expected:* rejected. *Failure:* applied. *Business Impact:* turn integrity broken. *Recovery:* deny-and-no-mutate (INV-G4).
- **Known Industry Patterns:** Command Validation, Server Authority, Finite State Machine — used to gate actions by role/phase.
- **Decision Drivers (ranked):** 1) Correctness, 2) Gameplay Fairness, 3) User Experience.

### 11. Dictionary Risks

#### ENG-DC-01 — Word quality / duplicates / offensive words
- **Decision Deferred To:** Future Version (content governance process) + Implementation Phase (publish-time validation).
- **Implementation Difficulty:** **Medium (3)** — publish validation + retire/replace workflow.
- **Testing Considerations:** *Unit* (uniqueness/min-size), *Manual QA* (cultural review). Test: publish with dupes/small size. Expected: rejected; corrections via new version. Failure indicators: in-place edits; case/whitespace dupes.
- **MVP Applicability:** **Required for MVP** — at least one clean dictionary is needed.
- **RPN:** S2 × L2 × D3 = **12** · **P3** · Order **#28**.
- **Validation Scenario:** *Scenario:* publish version with 24 words. *Expected:* rejected. *Failure:* accepted. *Business Impact:* unplayable/broken boards. *Recovery:* fix + republish (DM-V2).
- **Known Industry Patterns:** Immutable State (versions), Command Validation — used for safe content lifecycle.
- **Decision Drivers (ranked):** 1) Correctness, 2) User Experience, 3) Maintainability.

#### ENG-DC-02 — Version mismatch & updates during active matches
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — pin version at match start; never dereference "current".
- **Testing Considerations:** *Integration*. Test: update/retire during a live match. Expected: match keeps its pinned version. Failure indicators: mid-match word change.
- **MVP Applicability:** **Required for MVP** — fairness/reproducibility.
- **RPN:** S3 × L2 × D2 = **12** · **P3** · Order **#29**.
- **Validation Scenario:** *Scenario:* dictionary updated mid-match. *Expected:* no board change. *Failure:* words change. *Business Impact:* unfair mid-match. *Recovery:* use pinned version (INV-D3).
- **Known Industry Patterns:** Immutable State, State Versioning, Snapshot Recovery — used to pin content.
- **Decision Drivers (ranked):** 1) Gameplay Fairness, 2) Consistency, 3) Correctness.

#### ENG-DC-03 — Insufficient / unsupported dictionaries
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Very Low (1)** — pre-start validation with clear errors.
- **Testing Considerations:** *Unit*, *Integration*. Test: select too-small/absent region. Expected: blocked with specific error. Failure indicators: late failure during generation.
- **MVP Applicability:** **Required for MVP** — must fail gracefully.
- **RPN:** S3 × L1 × D1 = **3** · **P4** · Order **#42**.
- **Validation Scenario:** *Scenario:* start with 24-word region. *Expected:* DICTIONARY_TOO_SMALL. *Failure:* start proceeds. *Business Impact:* broken board. *Recovery:* validate at selection + start (V-DICT-2).
- **Known Industry Patterns:** Command Validation, Server Authority — used for early, clear failure.
- **Decision Drivers (ranked):** 1) Correctness, 2) User Experience.

### 12. Session Risks

#### ENG-SE-01 — Nickname collisions & identity ambiguity
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — token = identity; nickname = per-room-unique display.
- **Testing Considerations:** *Concurrency*, *Integration*. Test: duplicate/look-alike nicknames; reuse after leave. Expected: per-room uniqueness; stable token identity. Failure indicators: nickname used as key; case-sensitive check.
- **MVP Applicability:** **Required for MVP** — only human identifier.
- **RPN:** S2 × L3 × D2 = **12** · **P3** · Order **#30**.
- **Validation Scenario:** *Scenario:* two identical nicknames. *Expected:* second rejected. *Failure:* both admitted. *Business Impact:* confusion. *Recovery:* atomic uniqueness (INV-P1).
- **Known Industry Patterns:** Immutable State (token id), Command Validation, Single Writer — used to separate identity from display.
- **Decision Drivers (ranked):** 1) Consistency, 2) User Experience, 3) Correctness.

#### ENG-SE-02 — Reconnect-token loss / theft / expiry
- **Decision Deferred To:** Security Design.
- **Implementation Difficulty:** **Medium (3)** — opaque, room-scoped, grace-bounded, invalidated tokens.
- **Testing Considerations:** *Security*, *Integration*. Test: invalid/expired/foreign token; supersede on new connection. Expected: correct accept/reject. Failure indicators: guessable/sequential tokens; expired accepted.
- **MVP Applicability:** **Required for MVP** — resume + anti-hijack.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#31**.
- **Validation Scenario:** *Scenario:* expired token presented. *Expected:* fresh join. *Failure:* old role restored. *Business Impact:* seat hijack/confusion. *Recovery:* strict grace + invalidation (PS-2/3).
- **Known Industry Patterns:** Idempotency, State Versioning, Rate Limiting, Circuit Breaker — used to secure resume flows.
- **Decision Drivers (ranked):** 1) Security, 2) Reliability, 3) User Experience.

#### ENG-SE-03 — Player abandonment & grace tuning
- **Decision Deferred To:** Implementation Phase; Performance Optimization Phase (tuning from playtest data).
- **Implementation Difficulty:** **Medium (3)** — pause/abandonment logic + tunable grace.
- **Testing Considerations:** *Integration*, *Recovery*, *Manual QA*. Test: essential vs non-essential leaver; last-second reconnect. Expected: pause only when needed; abandonment only after grace. Failure indicators: pausing for non-essential; premature abandonment.
- **MVP Applicability:** **Required for MVP** — frequent; affects everyone in the match.
- **RPN:** S3 × L3 × D3 = **27** · **P2** · Order **#7 (tie band; see table)**.
- **Validation Scenario:** *Scenario:* active Spymaster leaves. *Expected:* pause; abandon only after grace. *Failure:* instant abandon. *Business Impact:* unfair loss/stall. *Recovery:* grace-bounded (PS-25).
- **Known Industry Patterns:** Finite State Machine, Circuit Breaker (grace), Compensating Actions, Backpressure (timers) — used for resilient session handling.
- **Decision Drivers (ranked):** 1) Reliability, 2) User Experience, 3) Gameplay Fairness.

### 13. Room Risks

#### ENG-RM-01 — Empty / idle room cleanup
- **Decision Deferred To:** Infrastructure Design; Implementation Phase.
- **Implementation Difficulty:** **Medium (3)** — inactivity-reset timers + safe close; ties to ENG-SC-02.
- **Testing Considerations:** *Integration*, *Load*, *Concurrency*. Test: idle vs active; empty close. Expected: reclaim idle, keep active. Failure indicators: live room closed; leaked room.
- **MVP Applicability:** **Required for MVP** — bounded live state.
- **RPN:** S2 × L3 × D3 = **18** · **P2** · Order **#32**.
- **Validation Scenario:** *Scenario:* room idle beyond timeout. *Expected:* expired + code released. *Failure:* lingers. *Business Impact:* resource leak. *Recovery:* reset-on-activity (BR-RX-1).
- **Known Industry Patterns:** Backpressure, Finite State Machine, Single Writer — used for lifecycle cleanup.
- **Decision Drivers (ranked):** 1) Reliability, 2) Cost, 3) Operational Simplicity.

#### ENG-RM-02 — Everyone disconnects simultaneously
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **High (4)** — distinguish "all disconnected within grace" from "empty".
- **Testing Considerations:** *Chaos*, *Recovery*, *Integration*. Test: all drop then partial/full return. Expected: survive grace; expire if none return; Host re-established. Failure indicators: immediate close; lost state.
- **MVP Applicability:** **Required for MVP** — realistic venue Wi-Fi failure.
- **RPN:** S3 × L2 × D4 = **24** · **P2** · Order **#33**.
- **Validation Scenario:** *Scenario:* whole room drops. *Expected:* room held through grace. *Failure:* closed instantly. *Business Impact:* match lost. *Recovery:* grace-bounded survival (BR-HM-3).
- **Known Industry Patterns:** Circuit Breaker (grace), Snapshot Recovery, Finite State Machine — used for mass-drop tolerance.
- **Decision Drivers (ranked):** 1) Reliability, 2) User Experience, 3) Correctness.

#### ENG-RM-03 — Rematch failure paths
- **Decision Deferred To:** Implementation Phase.
- **Implementation Difficulty:** **Low (2)** — re-validate and guide back to Lobby.
- **Testing Considerations:** *Integration*, *E2E*. Test: rematch with invalid composition/retired dictionary. Expected: blocked with specific reason. Failure indicators: start with stale setup; silent dead-end.
- **MVP Applicability:** **Recommended for MVP** — smooths UX; not correctness-critical.
- **RPN:** S2 × L2 × D2 = **8** · **P4** · Order **#41**.
- **Validation Scenario:** *Scenario:* rematch missing a Spymaster. *Expected:* MATCH_CONFIGURATION_INVALID. *Failure:* starts anyway. *Business Impact:* illegal match. *Recovery:* re-validate (RF-4).
- **Known Industry Patterns:** Command Validation, Finite State Machine — used for guarded restarts.
- **Decision Drivers (ranked):** 1) Correctness, 2) User Experience.

### 14. Reliability Risks

#### ENG-RE-01 — Crash / unexpected shutdown mid-operation
- **Decision Deferred To:** Software Architecture Phase; Infrastructure Design.
- **Implementation Difficulty:** **Very High (5)** — recoverable authoritative state + atomic apply + idempotent recovery, without long-term storage.
- **Testing Considerations:** *Chaos*, *Recovery*, *Integration*. Test: kill mid-action/mid-migration/mid-generation. Expected: restart lands on last consistent state; no replayed terminals. Failure indicators: lost/duplicated match; corrupted state.
- **MVP Applicability:** **Required for MVP** — but scope to room-lifetime durability (avoid over-engineering).
- **RPN:** S4 × L2 × D5 = **40** · **P1** · Order (foundational).
- **Validation Scenario:** *Scenario:* crash at a terminal reveal. *Expected:* recover to committed state, result intact once. *Failure:* double result / lost match. *Business Impact:* trust loss. *Recovery:* commit-then-broadcast; idempotent recovery (QM-16).
- **Known Industry Patterns:** Snapshot Recovery, Event Sourcing, Immutable State, Idempotency, Compensating Actions — used for crash-consistent recovery.
- **Decision Drivers (ranked):** 1) Reliability, 2) Correctness, 3) Consistency.

#### ENG-RE-02 — Interrupted / partial multi-step operations
- **Decision Deferred To:** Software Architecture Phase; Implementation Phase.
- **Implementation Difficulty:** **High (4)** — defined commit boundary + safe re-entry per composite op.
- **Testing Considerations:** *Recovery*, *Chaos*, *Integration*. Test: interrupt start/migration/expiry mid-way. Expected: complete or roll back cleanly. Failure indicators: half-started match; code released before cleanup.
- **MVP Applicability:** **Required for MVP** — composite ops exist in the core loop.
- **RPN:** S3 × L2 × D4 = **24** · **P2** · Order **#34**.
- **Validation Scenario:** *Scenario:* interrupt between board-gen and first turn. *Expected:* atomic completion or abort. *Failure:* match half-started. *Business Impact:* stuck room. *Recovery:* single commit point (BR-GS-4).
- **Known Industry Patterns:** Compensating Actions, Snapshot Recovery, Idempotency, Event Sourcing — used for atomic composites.
- **Decision Drivers (ranked):** 1) Reliability, 2) Correctness, 3) Maintainability.

### 15. Scalability Risks

#### ENG-SC-01 — Per-room memory & connection growth
- **Decision Deferred To:** Infrastructure Design; Performance Optimization Phase.
- **Implementation Difficulty:** **Medium (3)** — bound per-room footprint; prompt reclaim.
- **Testing Considerations:** *Load*, *Stress*, *Soak*. Test: scale room count; long-lived rooms. Expected: linear, bounded per-room usage. Failure indicators: unbounded growth; unreleased state.
- **MVP Applicability:** **Future Optimization** — MVP volumes are small; keep footprint bounded but don't pre-scale.
- **RPN:** S3 × L2 × D3 = **18** · **P2** · Order **#35**.
- **Validation Scenario:** *Scenario:* many concurrent rooms. *Expected:* stable per-room memory. *Failure:* leak/growth. *Business Impact:* degradation at scale. *Recovery:* reclaim on expiry (SCAL-1/2).
- **Known Industry Patterns:** Backpressure, Immutable State, Snapshot Recovery — used to bound resource use.
- **Decision Drivers (ranked):** 1) Scalability, 2) Cost, 3) Reliability.

#### ENG-SC-02 — Timer / expiry management at scale
- **Decision Deferred To:** Infrastructure Design; Performance Optimization Phase.
- **Implementation Difficulty:** **Medium (3)** — efficient (bucketed) expiry vs precise timers.
- **Testing Considerations:** *Load*, *Stress*. Test: huge timer counts; expiry bursts. Expected: no per-tick full scans; acceptable imprecision. Failure indicators: expiry lag or timer storms.
- **MVP Applicability:** **Future Optimization** — precise timers suffice at MVP scale.
- **RPN:** S2 × L2 × D3 = **12** · **P3** · Order **#36**.
- **Validation Scenario:** *Scenario:* 100k rooms with timers. *Expected:* bounded scheduler cost. *Failure:* overload. *Business Impact:* delayed cleanup. *Recovery:* bucketed evaluation.
- **Known Industry Patterns:** Backpressure, Event Queue, Rate Limiting — used for scalable scheduling.
- **Decision Drivers (ranked):** 1) Scalability, 2) Performance, 3) Cost.

#### ENG-SC-03 — Broadcast fan-out & hot rooms
- **Decision Deferred To:** Real-Time Communication Design; Performance Optimization Phase.
- **Implementation Difficulty:** **Medium (3)** — one projection per role per change; snapshots only on connect.
- **Testing Considerations:** *Load*, *Stress*. Test: high action rate; many reconnects. Expected: bounded work/action; latency within QM-05. Failure indicators: per-recipient recompute; latency spikes; cached projection leak.
- **MVP Applicability:** **Future Optimization** — small rooms fan out cheaply at MVP scale.
- **RPN:** S2 × L2 × D3 = **12** · **P3** · Order **#37**.
- **Validation Scenario:** *Scenario:* rapid actions in a full room. *Expected:* latency within target. *Failure:* spikes. *Business Impact:* laggy play. *Recovery:* compute-once-fan-out (QM-05).
- **Known Industry Patterns:** Publish/Subscribe, Immutable State, Backpressure, State Versioning — used for efficient delivery.
- **Decision Drivers (ranked):** 1) Performance, 2) Scalability, 3) User Experience.

#### ENG-SC-04 — Resource exhaustion & cleanup backpressure
- **Decision Deferred To:** Infrastructure Design; Performance Optimization Phase.
- **Implementation Difficulty:** **High (4)** — bounded retention + cleanup keeping pace with creation.
- **Testing Considerations:** *Stress*, *Soak*, *Chaos*. Test: creation spikes; mass expiry. Expected: stable steady-state; bounded retained sets. Failure indicators: cleanup lag; growing key/event stores; codes never released.
- **MVP Applicability:** **Future Optimization** — matters at growth; keep retention bounded from day one.
- **RPN:** S3 × L2 × D4 = **24** · **P2** · Order **#38**.
- **Validation Scenario:** *Scenario:* sustained high churn. *Expected:* steady resource use. *Failure:* exhaustion. *Business Impact:* failures under load. *Recovery:* bounded retention + backpressure (SCAL-4).
- **Known Industry Patterns:** Backpressure, Rate Limiting, Event Queue, Circuit Breaker — used to protect under load.
- **Decision Drivers (ranked):** 1) Scalability, 2) Reliability, 3) Cost.

---

## 4. Master Priority Table

Sorted by RPN (desc). Order column is the **Recommended Implementation Order** (RPN-driven,
with foundational integrity items pulled forward where dependents rely on them).

| Order | ID | S | L | D | RPN | Priority | MVP Applicability | Decision Deferred To |
|------:|----|--:|--:|--:|----:|----------|-------------------|----------------------|
| 1 | ENG-GP-01 | 4 | 3 | 4 | 48 | P1 | Critical | Real-Time Comm; Impl |
| 2 | ENG-RE-01 | 4 | 2 | 5 | 40 | P1 | Required (room-life) | Architecture; Infra |
| 3 | ENG-ST-03 | 3 | 3 | 4 | 36 | P1 | Critical | Real-Time Comm; Security |
| 4 | ENG-CO-04 | 3 | 3 | 4 | 36 | P1 | Required | Real-Time Comm; Security |
| 5 | ENG-RT-01 | 3 | 3 | 4 | 36 | P1 | Critical | Real-Time Comm |
| 6 | ENG-RT-03 | 3 | 3 | 4 | 36 | P1 | Critical | Real-Time Comm |
| 7 | ENG-ST-02 | 4 | 2 | 4 | 32 | P1 | Critical | Architecture; Impl |
| 8 | ENG-FP-01 | 4 | 2 | 4 | 32 | P1 | Critical | Security; Real-Time Comm |
| 9 | ENG-GP-02 | 3 | 3 | 3 | 27 | P2 | Required | API; Real-Time Comm; Impl |
| 10 | ENG-GP-05 | 3 | 3 | 3 | 27 | P2 | Critical | Impl |
| 11 | ENG-CO-01 | 3 | 3 | 3 | 27 | P2 | Required | Architecture; Impl |
| 12 | ENG-SE-03 | 3 | 3 | 3 | 27 | P2 | Required | Impl; Performance |
| 13 | ENG-GP-03 | 4 | 2 | 3 | 24 | P2 | Critical | Impl |
| 14 | ENG-ST-01 | 4 | 2 | 3 | 24 | P2 | Critical | Architecture; Impl |
| 15 | ENG-ST-04 | 3 | 2 | 4 | 24 | P2 | Required | Architecture; Infra |
| 16 | ENG-CO-02 | 3 | 2 | 4 | 24 | P2 | Required | Architecture; Impl |
| 17 | ENG-RT-05 | 3 | 2 | 4 | 24 | P2 | Required | Real-Time Comm; Impl |
| 18 | ENG-FP-02 | 4 | 2 | 3 | 24 | P2 | Critical | Security; Impl |
| 19 | ENG-RM-02 | 3 | 2 | 4 | 24 | P2 | Required | Impl |
| 20 | ENG-RE-02 | 3 | 2 | 4 | 24 | P2 | Required | Architecture; Impl |
| 21 | ENG-SC-04 | 3 | 2 | 4 | 24 | P2 | Future Optimization | Infra; Performance |
| 22 | ENG-GP-06 | 2 | 3 | 3 | 18 | P2 | Required | Impl |
| 23 | ENG-GP-07 | 3 | 3 | 2 | 18 | P2 | Critical | Impl |
| 24 | ENG-GP-09 | 3 | 2 | 3 | 18 | P2 | Required | Impl |
| 25 | ENG-GP-10 | 3 | 2 | 3 | 18 | P2 | Required | Impl |
| 26 | ENG-CO-05 | 3 | 2 | 3 | 18 | P2 | Required | Impl; Infra |
| 27 | ENG-RT-02 | 2 | 3 | 3 | 18 | P2 | Required | Real-Time Comm; Impl |
| 28 | ENG-RT-04 | 3 | 2 | 3 | 18 | P2 | Required | Real-Time Comm; Security |
| 29 | ENG-FP-03 | 3 | 2 | 3 | 18 | P2 | Required | Security; Real-Time Comm |
| 30 | ENG-FP-04 | 3 | 3 | 2 | 18 | P2 | Critical | Impl |
| 31 | ENG-SE-02 | 3 | 2 | 3 | 18 | P2 | Required | Security |
| 32 | ENG-RM-01 | 2 | 3 | 3 | 18 | P2 | Required | Infra; Impl |
| 33 | ENG-SC-01 | 3 | 2 | 3 | 18 | P2 | Future Optimization | Infra; Performance |
| 34 | ENG-GP-04 | 4 | 2 | 2 | 16 | P3 | Critical | Impl |
| 35 | ENG-DC-01 | 2 | 2 | 3 | 12 | P3 | Required | Future Version; Impl |
| 36 | ENG-DC-02 | 3 | 2 | 2 | 12 | P3 | Required | Impl |
| 37 | ENG-SE-01 | 2 | 3 | 2 | 12 | P3 | Required | Impl |
| 38 | ENG-SC-02 | 2 | 2 | 3 | 12 | P3 | Future Optimization | Infra; Performance |
| 39 | ENG-SC-03 | 2 | 2 | 3 | 12 | P3 | Future Optimization | Real-Time Comm; Performance |
| 40 | ENG-GP-08 | 3 | 1 | 3 | 9 | P3 | Required | Security; Impl |
| 41 | ENG-CO-03 | 2 | 2 | 2 | 8 | P4 | Recommended | Impl |
| 42 | ENG-RM-03 | 2 | 2 | 2 | 8 | P4 | Recommended | Impl |
| 43 | ENG-DC-03 | 3 | 1 | 1 | 3 | P4 | Required | Impl |

> Note: ENG-GP-04 and ENG-DC-03 are high-severity but low-difficulty/likelihood, so their RPN
> is modest — they are **quick, must-do correctness wins** (do them early despite low RPN). This
> is exactly the Severity ≠ Difficulty caveat in action.

---

## 5. Architect's Focus Summary

### 5.1 Highest-priority engineering challenges (do first)
`ENG-GP-01` (simultaneous guesses), `ENG-RE-01` (crash recovery), `ENG-ST-03` (role-filtered
sync), `ENG-CO-04` (duplicate connections), `ENG-RT-01` (message reliability), `ENG-RT-03`
(reconnect resync), `ENG-ST-02` (atomic state), `ENG-FP-01` (Key leakage). These are the P1
band — the correctness/fairness/reliability backbone.

### 5.2 Most difficult engineering challenges (allocate senior effort / spikes)
`ENG-RE-01` (Very High), then the High-difficulty set: `ENG-ST-02`, `ENG-ST-03`, `ENG-ST-04`,
`ENG-CO-02`, `ENG-CO-04`, `ENG-RT-01`, `ENG-RT-03`, `ENG-RT-05`, `ENG-FP-01`, `ENG-RM-02`,
`ENG-RE-02`, `ENG-SC-04`.

### 5.3 Challenges that can be safely postponed (avoid over-engineering now)
Scalability tuning — `ENG-SC-01/02/03/04` (Future Optimization; keep footprints bounded but
don't pre-scale) — and low-RPN UX-smoothing items `ENG-CO-03`, `ENG-RM-03`. Content governance
depth (`ENG-DC-01`) can mature over time provided one clean dictionary and publish-time
validation exist for MVP.

### 5.4 Challenges that require architectural decisions (gate the Architecture phase)
`ENG-ST-01/02/04` (state model & atomicity), `ENG-CO-01/02` (serialization model),
`ENG-RE-01/02` (recovery/durability boundary), plus everything **Deferred To** Real-Time
Communication Design (`ENG-ST-03`, `ENG-CO-04`, `ENG-RT-01/03/04/05`, `ENG-SC-03`) and Security
Design (`ENG-FP-01/02/03`, `ENG-SE-02`, `ENG-RT-04`, `ENG-ST-03`).

### 5.5 Challenges that require business clarification (route to the BA/Product Owner)
Mostly the [26 §16](01-engineering-challenges-risk-analysis.md#16-cross-cutting-open-questions)
open questions: default grace values & mass-disconnect grace (`ENG-SE-03`, `ENG-RM-02`), neutral
normalization policy for multi-script dictionaries (`ENG-GP-06`, `ENG-DC-01`),
auditable/seeded generation appetite (`ENG-GP-08`), deprecated-version retention windows
(`ENG-DC-02`), and in-progress durability guarantee (`ENG-RE-01`). These are *policy/parameter*
questions, not rule changes.

### 5.6 Challenges with the greatest implementation risk (highest chance of subtle defects)
`ENG-RE-01` (recovery correctness), `ENG-ST-02`/`ENG-ST-03` (atomicity + non-leaking filtering),
`ENG-CO-04`/`ENG-RT-01`/`ENG-RT-03` (connection & message correctness under faults), and
`ENG-FP-01` (any single missed delivery path leaks the Key). These combine High/Very-High
difficulty with Critical/High severity — the classic "looks done but subtly wrong" zone; budget
concurrency/chaos/security testing accordingly.

> This summary reorders **attention**, not the source analysis. Document
> [26](01-engineering-challenges-risk-analysis.md) remains unchanged and authoritative; this
> layer only adds prioritization metadata to guide the Software Architecture phase.

## 6. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial enrichment layer: appended Decision-Deferred-To, Difficulty, Testing, MVP Applicability, RPN, Validation Scenarios, Known Patterns, and Decision Drivers to all 42 challenges; added master priority table and architect focus summary. No change to document 26. |
