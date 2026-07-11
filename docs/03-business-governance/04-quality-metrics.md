# 22. Non-Functional Quality Metrics — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Make every non-functional requirement in [02 — SRS §2.9](../02-business-analysis/01-software-requirements.md#29-non-functional-requirements) **measurable**, by attaching a target, a measurement method, a priority, and acceptance criteria. These are **business expectations**, not implementation directives. |
| **Technology** | Neutral (targets are outcomes; no tools, tech, or infrastructure prescribed). |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [How to Read a Metric](#3-how-to-read-a-metric)
4. [Metrics](#4-metrics)
5. [Priority Summary](#5-priority-summary)
6. [Revision History](#6-revision-history)

---

## 1. Purpose & Usage

An NFR that cannot be measured cannot be verified. Each metric below maps to an NFR and states
what "good" means as a target and an acceptance test. Targets are **initial business
expectations** for the MVP and may be tuned by the Product Owner; they never change gameplay.

## 2. References
- [02 — SRS](../02-business-analysis/01-software-requirements.md) (NFR-1..14), [11 — Invariants](../02-business-analysis/10-business-invariants.md)
- [16 — Player Session](../02-business-analysis/15-player-session-reconnection.md), [21 — Constants](03-business-constants-catalog.md)

## 3. How to Read a Metric

Each entry: **Metric ID**, **Description**, **Measurement Method**, **Target Value**,
**Priority** (Must / Should / Could), **Business Reason**, **Acceptance Criteria**. Targets use
percentiles (e.g., p95 = 95th percentile) as **business** expectations, not a mandate on how to
measure internally.

## 4. Metrics

### QM-01 Availability *(NFR-4)*
- **Description:** The service is available to create/join/play rooms.
- **Measurement Method:** Ratio of successful availability checks over a rolling 30-day window.
- **Target Value:** ≥ 99.5% monthly availability.
- **Priority:** Must.
- **Business Reason:** Casual players abandon unreliable games (BO-1).
- **Acceptance Criteria:** Monthly availability meets/exceeds target; no single outage silently loses an in-progress match's state within the room's lifetime.

### QM-02 Reliability *(NFR-2, NFR-11)*
- **Description:** Matches complete without state corruption or contradictory client views.
- **Measurement Method:** Rate of matches ending correctly (win/loss/abandonment recorded) vs matches started.
- **Target Value:** ≥ 99.9% of started matches reach a correct, recorded outcome.
- **Priority:** Must.
- **Business Reason:** Fairness and trust (INV-O1..O4).
- **Acceptance Criteria:** No observed case of two clients seeing contradictory authoritative state; every completed match has exactly one recorded result.

### QM-03 Performance — Board Generation *(NFR-1)*
- **Description:** Time from valid Start to Board+Key ready for delivery.
- **Measurement Method:** Duration of the board-generation step per match start.
- **Target Value:** p95 ≤ 500 ms; p99 ≤ 1 s.
- **Priority:** Should.
- **Business Reason:** Start should feel instant.
- **Acceptance Criteria:** 95% of starts generate within target; result respects composition constants (§[21](03-business-constants-catalog.md)).

### QM-04 Performance — Room Creation *(NFR-1)*
- **Description:** Time from create request to Room Code returned.
- **Measurement Method:** Duration of room-creation handling.
- **Target Value:** p95 ≤ 300 ms.
- **Priority:** Should.
- **Business Reason:** Zero-friction onboarding (BO-2).
- **Acceptance Criteria:** 95% of creations within target; code is unique among live rooms (INV-R2).

### QM-05 Latency — Action Propagation *(NFR-1)*
- **Description:** Time from an accepted intent (clue/guess/reveal/turn change) to all participants receiving the authoritative update.
- **Measurement Method:** Elapsed time between intent acceptance and last participant's state update, per action.
- **Target Value:** p95 ≤ 300 ms; p99 ≤ 700 ms (within a region).
- **Priority:** Must.
- **Business Reason:** Real-time feel is core to the party-game experience.
- **Acceptance Criteria:** 95% of actions propagate within target; role-filtering preserved (INV-B9).

### QM-06 Response Time — Intent Acknowledgement *(NFR-1)*
- **Description:** Time to accept/reject an intent and return an outcome or Business Error.
- **Measurement Method:** Duration from intent receipt to acknowledgement.
- **Target Value:** p95 ≤ 250 ms.
- **Priority:** Should.
- **Business Reason:** Prompt feedback, including clear error messages ([13](../02-business-analysis/12-business-error-catalog.md)).
- **Acceptance Criteria:** 95% within target; every rejection returns a catalogued error code.

### QM-07 Reconnect Time *(NFR-11, NFR-4)*
- **Description:** Time for a returning Player (within grace) to be restored to their role-appropriate state.
- **Measurement Method:** Duration from reconnect request to full state restored.
- **Target Value:** p95 ≤ 2 s (well within `CONST-RECONNECT-GRACE-PERIOD`).
- **Priority:** Must.
- **Business Reason:** Mobile drops must not disrupt play (R-1).
- **Acceptance Criteria:** 95% of in-grace reconnects restore correct team/role/view (INV-P5); none leak the Key to non-Spymasters.

### QM-08 Scalability — Concurrent Rooms *(NFR-5)*
- **Description:** Number of independent rooms served concurrently without cross-room degradation.
- **Measurement Method:** Sustained concurrent live rooms while other metrics stay within target.
- **Target Value:** MVP target ≥ 1,000 concurrent rooms with QM-05 held; scalable beyond by adding capacity.
- **Priority:** Should.
- **Business Reason:** Growth without redesign (SCAL-1).
- **Acceptance Criteria:** Latency/consistency targets hold at target concurrency; rooms remain isolated (SCAL-1).

### QM-09 Concurrency — In-Room Actions *(NFR-2)*
- **Description:** Correct serialization of near-simultaneous intents in a room.
- **Measurement Method:** Rate of correctly resolved guess/clue races (first-valid-wins).
- **Target Value:** 100% deterministic resolution per [17 — Rule Precedence](../02-business-analysis/16-rule-precedence.md).
- **Priority:** Must.
- **Business Reason:** Determinism = fairness (CR-1..5).
- **Acceptance Criteria:** No double-reveal; second conflicting guess re-evaluated/rejected (INV-B7).

### QM-10 Consistency *(NFR-2)*
- **Description:** Single-source-of-truth guarantee across clients.
- **Measurement Method:** Audits comparing delivered state vs authoritative state.
- **Target Value:** 0 divergence incidents.
- **Priority:** Must.
- **Business Reason:** Trust in outcomes (INV-O1).
- **Acceptance Criteria:** No participant ever acts on stale/contradictory authoritative state affecting an outcome.

### QM-11 Maintainability *(NFR-14)*
- **Description:** Ease of change without rule impact; single codebase/gameplay.
- **Measurement Method:** Adding a new region requires changes only to dictionary content, not rules.
- **Target Value:** New region added with **zero** rule/gameplay document changes.
- **Priority:** Should.
- **Business Reason:** One product, many regions ([ADR-04/05](02-architecture-decision-records.md)).
- **Acceptance Criteria:** A new Country Dictionary goes live without editing documents 03/04/10/11.

### QM-12 Observability *(NFR-13)*
- **Description:** Sufficient business-event and result visibility to verify correctness.
- **Measurement Method:** Coverage of [Domain Events (12)](../02-business-analysis/11-domain-events-catalog.md) actually emitted for a match.
- **Target Value:** 100% of defined lifecycle events observable per match; no PII recorded.
- **Priority:** Should.
- **Business Reason:** Diagnose issues and confirm fairness without personal data (NFR-10).
- **Acceptance Criteria:** Every match's key events (start, clue, reveal, turn, finish) are observable; no personal data present.

### QM-13 Localization *(NFR-6, NFR-7)*
- **Description:** Word source is the only localized component; rules language-independent.
- **Measurement Method:** Verification that identical rule outcomes occur across dictionaries for equivalent action sequences.
- **Target Value:** 100% identical rule behaviour across all regions.
- **Priority:** Must.
- **Business Reason:** Global fairness (INV-D1).
- **Acceptance Criteria:** Swapping dictionaries changes only words; counts/flow/outcomes identical (QM cross-check with INV-D1).

### QM-14 Accessibility *(NFR-8 usability)*
- **Description:** A new Player can join and understand their role view without instruction.
- **Measurement Method:** Task-success/comprehension rate in usability checks (join → understand role).
- **Target Value:** ≥ 90% of first-time players join and correctly identify their role/actions unaided.
- **Priority:** Should.
- **Business Reason:** Casual, no-signup audience (BO-2).
- **Acceptance Criteria:** Role-appropriate view is clear; no Operative can perceive unrevealed ownership.

### QM-15 Security / Integrity *(SEC-1..8)*
- **Description:** Hidden information and action authorization are never violated.
- **Measurement Method:** Rate of unauthorized-action attempts correctly rejected; audits for Key leakage.
- **Target Value:** 100% of unauthorized intents rejected; 0 Key-leakage incidents.
- **Priority:** Must.
- **Business Reason:** The entire game depends on hidden info and turn integrity (INV-B9, INV-G4).
- **Acceptance Criteria:** No Operative receives unrevealed ownership; only authorized actors act (SEC-2/4).

### QM-16 Recovery *(NFR-11)*
- **Description:** Game state restores to the last consistent point after interruption within the room's lifetime.
- **Measurement Method:** Post-interruption audits verifying state continuity.
- **Target Value:** 100% of interruptions within a room's life recover without state loss/corruption.
- **Priority:** Must.
- **Business Reason:** Resilience expectation (NFR-4/11).
- **Acceptance Criteria:** After a transient interruption, the match resumes at the same phase with identical Board/Key/turn state.

### QM-17 Resilience — Disconnect Tolerance *(R-1, NFR-4)*
- **Description:** Matches survive transient client disconnects per grace rules.
- **Measurement Method:** Share of essential-player disconnects resolved by in-grace reconnection without abandonment.
- **Target Value:** ≥ 95% of brief disconnects (< grace) resolved with no abandonment.
- **Priority:** Should.
- **Business Reason:** Mobile networks are unreliable (R-1).
- **Acceptance Criteria:** Pause/resume behaves per [16 §7](../02-business-analysis/15-player-session-reconnection.md); abandonment only after grace.

### QM-18 Auditability *(NFR-13)*
- **Description:** Match outcomes and lifecycle are reconstructable from recorded business facts.
- **Measurement Method:** Ability to reconstruct a match's result and key transitions from its Domain Events/Result.
- **Target Value:** 100% of completed matches reconstructable (winner, reason) without PII.
- **Priority:** Should.
- **Business Reason:** Trust, dispute resolution, correctness verification.
- **Acceptance Criteria:** Recorded Game Result (INV-O4) plus events fully explain the outcome.

## 5. Priority Summary

| Priority | Metrics |
|----------|---------|
| **Must** | QM-01, QM-02, QM-05, QM-07, QM-09, QM-10, QM-13, QM-15, QM-16 |
| **Should** | QM-03, QM-04, QM-06, QM-08, QM-11, QM-12, QM-14, QM-17, QM-18 |
| **Could** | — (all identified metrics are Must/Should for the MVP) |

## 6. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial measurable metrics derived from SRS NFRs; targets are MVP business expectations. |
