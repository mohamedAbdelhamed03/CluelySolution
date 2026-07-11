# 26. Engineering Challenges & Risk Analysis — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Analysis only — **no architecture, technology, or code decisions** |
| **Purpose** | Identify every significant engineering challenge, implementation risk, and edge case a team will face building Cluely, so future architecture is informed by them. This is a **feasibility/risk study**, not a design. It changes no business rule and introduces no gameplay. |
| **Technology** | Neutral (no language, framework, protocol, or infrastructure is assumed or recommended). |

## Table of Contents
1. [Purpose & Method](#1-purpose--method)
2. [References](#2-references)
3. [Severity & Likelihood Scales](#3-severity--likelihood-scales)
4. [Challenge Format](#4-challenge-format)
5. [Challenge Index](#5-challenge-index)
6. [Gameplay Risks](#6-gameplay-risks)
7. [State Management Risks](#7-state-management-risks)
8. [Concurrency Risks](#8-concurrency-risks)
9. [Real-Time Risks](#9-real-time-risks)
10. [Fair-Play Risks](#10-fair-play-risks)
11. [Dictionary Risks](#11-dictionary-risks)
12. [Session Risks](#12-session-risks)
13. [Room Risks](#13-room-risks)
14. [Reliability Risks](#14-reliability-risks)
15. [Scalability Risks](#15-scalability-risks)
16. [Cross-Cutting Open Questions](#16-cross-cutting-open-questions)
17. [Revision History](#17-revision-history)

---

## 1. Purpose & Method

Cluely is a real-time, server-authoritative, hidden-information multiplayer game. That class
of system fails in characteristic ways: races, lost/duplicated/out-of-order messages,
partial state, reconnection ambiguity, and information leakage. Each challenge below is
analyzed with *why it exists, what goes wrong, which rules it touches, candidate approaches
with trade-offs, common mistakes, forgotten edge cases, a recommended practice, and open
questions*. Recommendations are **engineering guidance**, not architecture selection.

## 2. References
- Rules & invariants: [03](../02-business-analysis/02-business-rules.md), [11](../02-business-analysis/10-business-invariants.md), [17 Rule Precedence](../02-business-analysis/16-rule-precedence.md)
- Behaviour: [08 State Machines](../02-business-analysis/07-state-machines.md), [09 Workflows](../02-business-analysis/08-business-workflows.md), [12 Events](../02-business-analysis/11-domain-events-catalog.md), [13 Errors](../02-business-analysis/12-business-error-catalog.md)
- Sessions/rooms/dictionaries: [14](../02-business-analysis/13-dictionary-management.md), [15](../02-business-analysis/14-lobby-room-lifecycle.md), [16](../02-business-analysis/15-player-session-reconnection.md)
- Quality & constants: [21 Constants](../03-business-governance/03-business-constants-catalog.md), [22 Quality Metrics](../03-business-governance/04-quality-metrics.md)

## 3. Severity & Likelihood Scales

**Severity:** *Critical* (corrupts outcome/fairness or loses a match), *High* (disrupts a
match/room), *Medium* (degrades experience), *Low* (cosmetic).
**Likelihood:** *Frequent*, *Occasional*, *Rare* — under real multiplayer conditions.

## 4. Challenge Format

Each entry includes: **ID · Title · Category · Description · Business Impact · Engineering
Impact · Severity · Likelihood · Related Rules · Possible Solutions · Advantages ·
Disadvantages · Trade-offs · Common Mistakes · Edge Cases · Best Practices · Open Questions ·
References.** To keep the document readable, Advantages/Disadvantages/Trade-offs are given per
solution inline.

## 5. Challenge Index

| ID | Title | Category | Severity | Likelihood |
|----|-------|----------|----------|-----------|
| ENG-GP-01 | Simultaneous guesses / double-reveal | Gameplay | Critical | Frequent |
| ENG-GP-02 | Double-click & duplicate intent submission | Gameplay | High | Frequent |
| ENG-GP-03 | Win/last-card detection ordering | Gameplay | Critical | Occasional |
| ENG-GP-04 | Assassin precedence & terminal ordering | Gameplay | Critical | Occasional |
| ENG-GP-05 | Turn-transition atomicity | Gameplay | High | Frequent |
| ENG-GP-06 | Structural clue validation vs semantics | Gameplay | Medium | Frequent |
| ENG-GP-07 | Guess-limit accounting (N+1, 0/∞) | Gameplay | High | Frequent |
| ENG-GP-08 | Board generation randomness & fairness | Gameplay | High | Rare |
| ENG-GP-09 | Rematch reset completeness | Gameplay | High | Occasional |
| ENG-GP-10 | Team/role changes at lobby↔match boundary | Gameplay | High | Occasional |
| ENG-ST-01 | Illegal/invalid state transitions | State | Critical | Occasional |
| ENG-ST-02 | Partial state updates / non-atomic mutations | State | Critical | Occasional |
| ENG-ST-03 | State synchronization & role-filtered views | State | High | Frequent |
| ENG-ST-04 | State recovery after interruption | State | High | Occasional |
| ENG-CO-01 | Concurrent joins & capacity/nickname races | Concurrency | High | Frequent |
| ENG-CO-02 | Concurrent leaves & host migration races | Concurrency | High | Occasional |
| ENG-CO-03 | Simultaneous host actions | Concurrency | Medium | Occasional |
| ENG-CO-04 | Simultaneous reconnects / duplicate connections | Concurrency | High | Frequent |
| ENG-CO-05 | Expiry racing with live activity | Concurrency | High | Occasional |
| ENG-RT-01 | Lost / duplicated / out-of-order messages | Real-time | High | Frequent |
| ENG-RT-02 | Delayed events & slow clients | Real-time | Medium | Frequent |
| ENG-RT-03 | Reconnection & state resynchronization | Real-time | High | Frequent |
| ENG-RT-04 | Multiple tabs / devices per identity | Real-time | High | Occasional |
| ENG-RT-05 | Host disconnection mid-critical-phase | Real-time | High | Occasional |
| ENG-FP-01 | Hidden-card / Key leakage | Fair-play | Critical | Occasional |
| ENG-FP-02 | Client manipulation & forged intents | Fair-play | Critical | Occasional |
| ENG-FP-03 | Replay / duplicate command attacks | Fair-play | High | Occasional |
| ENG-FP-04 | Out-of-turn / wrong-role action attempts | Fair-play | High | Frequent |
| ENG-DC-01 | Dictionary quality/duplicates/offensive words | Dictionary | Medium | Occasional |
| ENG-DC-02 | Version mismatch & updates during matches | Dictionary | High | Occasional |
| ENG-DC-03 | Insufficient / unsupported dictionaries | Dictionary | High | Rare |
| ENG-SE-01 | Nickname collisions & identity ambiguity | Session | Medium | Frequent |
| ENG-SE-02 | Reconnect-token loss / theft / expiry | Session | High | Occasional |
| ENG-SE-03 | Player abandonment & grace tuning | Session | High | Frequent |
| ENG-RM-01 | Empty/idle room cleanup | Room | Medium | Frequent |
| ENG-RM-02 | Everyone disconnects simultaneously | Room | High | Occasional |
| ENG-RM-03 | Rematch failure paths | Room | Medium | Occasional |
| ENG-RE-01 | Crash / unexpected shutdown mid-operation | Reliability | Critical | Occasional |
| ENG-RE-02 | Interrupted / partial multi-step operations | Reliability | High | Occasional |
| ENG-SC-01 | Per-room memory & connection growth | Scalability | High | Occasional |
| ENG-SC-02 | Timer/expiry management at scale | Scalability | Medium | Occasional |
| ENG-SC-03 | Broadcast fan-out & hot rooms | Scalability | Medium | Occasional |
| ENG-SC-04 | Resource exhaustion & cleanup backpressure | Scalability | High | Occasional |

---

## 6. Gameplay Risks

### ENG-GP-01 — Simultaneous guesses / double-reveal
- **Category:** Gameplay / Concurrency boundary.
- **Description:** Two Operatives on the active team tap different (or the same) unrevealed cards within milliseconds. Both intents arrive before either reveal is committed.
- **Business Impact:** Could reveal two cards for one clue-guess slot, mis-decrement agent counts, or skip a turn-ending reveal — corrupting the outcome.
- **Engineering Impact:** Requires strict serialization and re-evaluation of the second intent against post-first state.
- **Severity:** Critical · **Likelihood:** Frequent.
- **Related Rules:** BR-GV-8, BR-EC-13, INV-B7, INV-G6; precedence [RP-12](../02-business-analysis/16-rule-precedence.md).
- **Possible Solutions:**
  1. *Single-writer serialization per room* — process one intent at a time for a room. **Adv:** simplest correctness; matches "first valid wins". **Disadv:** per-room throughput ceiling (acceptable — rooms are small). **Trade-off:** simplicity over parallelism.
  2. *Optimistic concurrency with version/state token* — reject a guess whose expected state no longer holds. **Adv:** tolerates parallel processing. **Disadv:** more client round-trips and retry logic; more edge cases.
  3. *Card-level lock* — lock the targeted card. **Adv:** fine-grained. **Disadv:** doesn't protect cross-card invariants (turn/agent counts); easy to under-lock.
- **Common Mistakes:** Checking "card unrevealed" then revealing in two steps without atomicity; treating the second guess as automatically valid; deduplicating by card only (misses two *different* cards guessed at once when the first should end the turn).
- **Edge Cases:** First guess is wrong (turn should end) but the second guess already queued as if the turn continued; both guesses target the same card; guess arrives exactly as a win is being computed.
- **Best Practices:** Serialize per room; make "validate → reveal → update counts → check terminal → maybe end turn" a single atomic step; re-validate every queued intent against fresh state.
- **Open Questions:** What feedback should a rejected second guess show (silently dropped vs explicit `CARD_ALREADY_REVEALED`)? Is there a maximum queue depth per turn?
- **References:** [17](../02-business-analysis/16-rule-precedence.md), [03 §3.12](../02-business-analysis/02-business-rules.md).

### ENG-GP-02 — Double-click & duplicate intent submission
- **Category:** Gameplay.
- **Description:** A user taps "submit clue"/"guess"/"end turn"/"start" twice, or the client retries on a slow response, producing duplicate intents.
- **Business Impact:** Duplicate clue submission, double turn-end (skipping the opponent), double match-start.
- **Engineering Impact:** Needs idempotency for state-changing intents.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-CL-7 (one clue), BR-TE, INV-G3, CR-4 (idempotent effects).
- **Possible Solutions:**
  1. *Idempotency key per intent* — same key applied once. **Adv:** robust against retries. **Disadv:** clients must generate/echo keys; server must remember recent keys.
  2. *State-guarded intents* — reject if the phase already advanced (e.g., clue already exists). **Adv:** no extra bookkeeping. **Disadv:** a legitimate fast second action could be misread; race with ENG-GP-01.
  3. *Client-side debounce only* — **Adv:** trivial. **Disadv:** insufficient alone (network retries bypass it).
- **Common Mistakes:** Relying solely on client debounce; making "end turn" non-idempotent; treating retried start as a second match.
- **Edge Cases:** Retry arrives after the turn already passed to the opponent; duplicate start after board already generated.
- **Best Practices:** Combine idempotency keys with state guards; every terminal/transition effect applied at most once (CR-4).
- **Open Questions:** How long must recent idempotency keys be retained per room?
- **References:** [12](../02-business-analysis/11-domain-events-catalog.md), [13](../02-business-analysis/12-business-error-catalog.md).

### ENG-GP-03 — Win / last-card detection ordering
- **Category:** Gameplay.
- **Description:** The moment a team's final agent is revealed, the match must end immediately — even if the reveal was made by the *other* team, or coincides with reaching a guess limit.
- **Business Impact:** Missing/late detection lets play continue past a win, corrupting the result.
- **Engineering Impact:** Terminal checks must run after *every* reveal, before any turn-continuation logic.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** BR-WIN-1, BR-OPP-4, BR-EC-1, INV-O2; precedence ranks 1–2 ([17 §4](../02-business-analysis/16-rule-precedence.md)).
- **Possible Solutions:**
  1. *Post-reveal terminal evaluation as part of the atomic step* — **Adv:** guarantees ordering. **Disadv:** must include the opponent's-count case. 
  2. *Event-driven recheck* — recompute on every CardRevealed event. **Adv:** decoupled. **Disadv:** risk of ordering gaps if events are async.
- **Common Mistakes:** Only checking the *active* team's win; checking win after turn-continuation; forgetting the "you revealed the opponent's last card → opponent wins" case (BR-EC-1).
- **Edge Cases:** Active team reveals opponent's last agent; a correct guess is simultaneously the winning card and the guess-limit boundary; win coincides with an essential disconnect (match end must win, ENG-RT-05).
- **Best Practices:** Evaluate *both* teams' completion after each reveal, before continuation/turn-end; treat win as preempting all lower-rank effects.
- **Open Questions:** Should the full Key be revealed to all on win (allowed by BR-GE-2) — always, or configurable?
- **References:** [17 §4–5](../02-business-analysis/16-rule-precedence.md), [03 §3.16/3.18](../02-business-analysis/02-business-rules.md).

### ENG-GP-04 — Assassin precedence & terminal ordering
- **Category:** Gameplay.
- **Description:** Revealing the assassin must end the match instantly with the guessing team losing, overriding any other pending condition.
- **Business Impact:** Wrong winner if assassin isn't given top precedence.
- **Engineering Impact:** The reveal-resolution order must check assassin before counts/continuation.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** BR-ASN-*, INV-O3, RP-1 ([17 §4](../02-business-analysis/16-rule-precedence.md)).
- **Possible Solutions:** Single ordered resolution function (reveal → assassin? → counts → win? → classify). **Adv:** deterministic. **Disadv:** must be the *only* path that resolves guesses (no bypass).
- **Common Mistakes:** Updating counts before assassin check; letting a queued guess process after an assassin already ended the match.
- **Edge Cases:** Assassin guessed as the "bonus" (N+1) guess; assassin guessed when the guessing team was one card from winning.
- **Best Practices:** Assassin is rank 1; once terminal, reject all further play intents (INV-G7).
- **Open Questions:** None material — rule is unambiguous; the risk is implementation discipline.
- **References:** [17](../02-business-analysis/16-rule-precedence.md).

### ENG-GP-05 — Turn-transition atomicity
- **Category:** Gameplay / State.
- **Description:** Ending a turn must clear the active clue, reset the guess allowance, flip the active team, and open the opponent's clue phase — all together.
- **Business Impact:** A half-applied transition can leave two active clues, a stale guess count, or an ambiguous active team.
- **Engineering Impact:** Transition must be atomic and idempotent.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-TE-*, BR-TO-2, INV-G2/G3, Turn machine [§8.3](../02-business-analysis/07-state-machines.md).
- **Possible Solutions:** Atomic transition applying all sub-changes together; guard against re-entry. **Adv:** consistency. **Disadv:** care needed where a turn-end coincides with match-end (match-end wins).
- **Common Mistakes:** Clearing the clue but not resetting guess count; flipping active team before terminal check.
- **Edge Cases:** Voluntary end-turn arriving at the same time as a guess resolves the turn; end-turn after match already ended.
- **Best Practices:** Model the turn as an explicit state machine; forbid transitions not defined in [§8.3](../02-business-analysis/07-state-machines.md).
- **Open Questions:** Should there be a per-turn transition sequence number for observability?
- **References:** [08 §8.3](../02-business-analysis/07-state-machines.md).

### ENG-GP-06 — Structural clue validation vs semantics
- **Category:** Gameplay.
- **Description:** The system validates only clue *structure* (one word, valid number, not an unrevealed board word); *meaning/fairness* is social. Defining "one word" and "matches a board word" is subtler than it looks.
- **Business Impact:** Over-strict validation blocks legal clues; under-strict allows cheating-looking clues; inconsistent normalization differs by region.
- **Engineering Impact:** Needs language-independent normalization (trim, case-fold) without language-specific rules ([ADR-12](../03-business-governance/02-architecture-decision-records.md)).
- **Severity:** Medium · **Likelihood:** Frequent.
- **Related Rules:** BR-CL-2/3/4/8, ADR-15, INV-D1.
- **Possible Solutions:**
  1. *Whitespace/token-based single-word check + case-insensitive compare to unrevealed words.* **Adv:** language-neutral. **Disadv:** cannot catch morphological variants (by design — that's social).
  2. *Allow only comparison to unrevealed words, permit revealed words (BR-CL-8).* **Adv:** faithful. **Disadv:** must recompute the "unrevealed set" each clue.
- **Common Mistakes:** Blocking clues equal to *revealed* words (should be allowed); applying language-specific stemming (violates INV-D1); disallowing hyphenated inputs without a defined policy.
- **Edge Cases:** Clue with leading/trailing spaces; different Unicode representations of the same characters; numbers-as-words; a board word becomes revealed mid-turn changing legality of a future clue.
- **Best Practices:** Define one neutral normalization (trim + case-fold + Unicode normalization) applied identically to clue and board words; document what is *not* enforced (semantics).
- **Open Questions:** Exact normalization form for multi-script dictionaries? Policy on hyphens/apostrophes/compound tokens per region (content-team owned)?
- **References:** [03 §3.11](../02-business-analysis/02-business-rules.md), [14](../02-business-analysis/13-dictionary-management.md).

### ENG-GP-07 — Guess-limit accounting (N+1, 0/∞)
- **Category:** Gameplay.
- **Description:** Guess allowance is N+1 for N≥1, and unbounded (min 1) for 0/unlimited. Off-by-one errors are classic here.
- **Business Impact:** Too few/many guesses changes fairness and outcomes.
- **Engineering Impact:** Careful counting including the "bonus" guess and the mandatory first guess.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-GV-6/7, BR-EC-2/3/4, INV-G5/G6, RULE-MAX-GUESSES ([21 §6](../03-business-governance/03-business-constants-catalog.md)).
- **Possible Solutions:** Represent allowance as a small computed policy (N≥1 → N+1; 0/∞ → unbounded) with a per-turn counter. **Adv:** centralizes the rule. **Disadv:** must handle unbounded distinctly from a large number.
- **Common Mistakes:** Modeling unlimited as a huge integer (edge bugs); forgetting the mandatory minimum-one guess; letting the bonus guess be skipped or double-counted.
- **Edge Cases:** Clue 0 then immediate wrong guess (turn ends after one); using the bonus guess to recover a prior clue's word; unlimited clue when almost all cards are revealed.
- **Best Practices:** Encode allowance as an explicit value type {bounded(n) | unbounded}; enforce min-one at end-turn.
- **Open Questions:** UI expectation for showing remaining guesses when unbounded?
- **References:** [21 §6](../03-business-governance/03-business-constants-catalog.md), [03 §3.12](../02-business-analysis/02-business-rules.md).

### ENG-GP-08 — Board generation randomness & fairness
- **Category:** Gameplay.
- **Description:** Board words, ownership assignment, and starting team must be uniformly random and unpredictable; bias or predictability harms fairness.
- **Business Impact:** Predictable/repeated boards, biased starting team, or leaked layout undermines fairness.
- **Engineering Impact:** Needs a well-distributed, unpredictable selection; must draw 25 *distinct* words and partition 9/8/7/1 without bias.
- **Severity:** High · **Likelihood:** Rare (but high impact).
- **Related Rules:** BR-BG-2/4/5, INV-B2/B6, [ADR-14](../03-business-governance/02-architecture-decision-records.md).
- **Possible Solutions:**
  1. *Uniform shuffle of the word pool + uniform ownership assignment.* **Adv:** simple, unbiased if the shuffle is unbiased. **Disadv:** must ensure the shuffle itself is unbiased (naive swaps bias).
  2. *Seeded generation for reproducibility/audit.* **Adv:** debuggable/auditable (QM-18). **Disadv:** seed must be unpredictable to clients and never leak (ties to ENG-FP-01).
- **Common Mistakes:** Biased shuffles; reusing a poor randomness source; letting the seed or full layout reach clients; duplicate words slipping through.
- **Edge Cases:** Dictionary exactly at 25 words (no spare); repeated boards across quick rematches; starting-team bias over many matches.
- **Best Practices:** Use an unbiased shuffle; verify distinctness (INV-B6) and the 9/8/7/1 partition (INV-B2); keep any seed server-only.
- **Open Questions:** Is auditable/seeded generation desired for dispute resolution, and how is the seed protected?
- **References:** [03 §3.8](../02-business-analysis/02-business-rules.md), [22 QM-18](../03-business-governance/04-quality-metrics.md).

### ENG-GP-09 — Rematch reset completeness
- **Category:** Gameplay / State.
- **Description:** Starting a new match must fully reset board, key, turn, clue, guess counts, and results, while preserving membership and permitted new setup.
- **Business Impact:** Leftover state from the prior match (stale reveals, old clue, old counts) corrupts the new one.
- **Engineering Impact:** Requires a clean, complete reset boundary between matches.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-GE-4, RF-3 ([15 §9](../02-business-analysis/14-lobby-room-lifecycle.md)), INV-D3 (new version resolves at start).
- **Possible Solutions:** Construct a fresh match object rather than mutating the old one. **Adv:** avoids residue. **Disadv:** must carefully carry over only membership/new setup.
- **Common Mistakes:** Reusing the previous board object; not clearing revealed flags; carrying the previous active team/clue; not re-resolving the dictionary version.
- **Edge Cases:** Rematch after abandonment; rematch when a player left (composition now invalid); dictionary changed between matches; rapid successive rematches.
- **Best Practices:** Treat each Match as immutable-per-instance; re-validate composition before start (RF-4).
- **Open Questions:** Should team/role assignments persist by default into a rematch or reset to unassigned?
- **References:** [15 §9](../02-business-analysis/14-lobby-room-lifecycle.md).

### ENG-GP-10 — Team/role changes at the lobby↔match boundary
- **Category:** Gameplay / State.
- **Description:** Team/role changes are allowed only outside an active match; the boundary (start/finish) is where races and stale UI cause illegal changes.
- **Business Impact:** A change applied as the match starts could corrupt the key/turn assumptions or create two Spymasters.
- **Engineering Impact:** Requires a hard lock at start and clear boundary semantics.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-TA-4, BR-RO-5, INV-T3/T5, V-TEAM-1/V-ROLE-2.
- **Possible Solutions:** Lock setup atomically at start; reject setup intents in-match with a specific error. **Adv:** clear. **Disadv:** must handle intents in flight at the exact transition.
- **Common Mistakes:** Validating "not in match" then starting before the change applies; allowing a Spymaster claim to land during start.
- **Edge Cases:** Player switches team exactly as Host starts; two players claim Spymaster as the match begins; role change during Post-Match→Lobby transition.
- **Best Practices:** Single serialized room writer (see ENG-CO-*) makes the boundary unambiguous; re-validate at the moment of start.
- **Open Questions:** Should in-flight setup intents at start be rejected or deferred to the next lobby?
- **References:** [15 §8](../02-business-analysis/14-lobby-room-lifecycle.md).

---

## 7. State Management Risks

### ENG-ST-01 — Illegal / invalid state transitions
- **Category:** State.
- **Description:** Intents may request transitions not permitted by the current state (guess in clue phase, clue after finish, start when in match).
- **Business Impact:** Corrupted game state; unfair or nonsensical outcomes.
- **Engineering Impact:** Every intent must be checked against an explicit state model.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** All of [08](../02-business-analysis/07-state-machines.md); V-STATE-1/2.
- **Possible Solutions:**
  1. *Explicit state machines with a whitelist of legal transitions.* **Adv:** rejects everything undefined by default. **Disadv:** must enumerate all states/transitions (already done in [08](../02-business-analysis/07-state-machines.md)).
  2. *Ad-hoc guards per handler.* **Adv:** quick. **Disadv:** easy to miss a case; inconsistent.
- **Common Mistakes:** Implicit states scattered across flags; allowing a transition because a needed guard was forgotten.
- **Edge Cases:** Paused overlay during disconnect; abandonment vs finish; Post-Match actions.
- **Best Practices:** Implement the documented machines literally; default-deny unknown transitions.
- **Open Questions:** Where should the pause overlay live — on Turn, Match, or a separate connection layer?
- **References:** [08](../02-business-analysis/07-state-machines.md).

### ENG-ST-02 — Partial state updates / non-atomic mutations
- **Category:** State.
- **Description:** A single logical action (e.g., reveal) touches multiple fields (card flag, counts, turn, terminal check). If interrupted, state becomes inconsistent.
- **Business Impact:** Illegal states (count doesn't match revealed cards; two active clues).
- **Engineering Impact:** Multi-field mutations must be all-or-nothing.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** INV-B2/B7/G2/G3; [11](../02-business-analysis/10-business-invariants.md).
- **Possible Solutions:** Apply each action as one atomic unit; validate invariants post-apply and refuse to commit on violation. **Adv:** guarantees consistency. **Disadv:** needs a clear commit boundary.
- **Common Mistakes:** Updating counts and reveal flags in separate steps; broadcasting before the full update commits.
- **Edge Cases:** Interruption between reveal and count update; broadcast of a half-applied state.
- **Best Practices:** Compute the next full state, validate invariants, then commit and broadcast; never broadcast intermediate state.
- **Open Questions:** Should invariant checks run in production on every commit or only in test builds?
- **References:** [11](../02-business-analysis/10-business-invariants.md).

### ENG-ST-03 — State synchronization & role-filtered views
- **Category:** State / Fair-play.
- **Description:** Every participant must see a consistent, *role-appropriate* projection: Spymasters see the Key; Operatives/Waiting never see unrevealed ownership.
- **Business Impact:** Divergent views break trust; leaking ownership breaks the game.
- **Engineering Impact:** One authoritative state, many filtered projections; filtering must be enforced at the delivery boundary.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-CO-4, INV-B9/P5, NFR-3.
- **Possible Solutions:**
  1. *Server computes per-role projections; never sends the Key to non-Spymasters.* **Adv:** safe by construction. **Disadv:** must recompute projections on each change.
  2. *Send full state, filter on client.* **Adv:** simpler server. **Disadv:** catastrophic — unrevealed ownership reaches clients (violates INV-B9). **Reject.**
- **Common Mistakes:** Sending the whole board with ownership and hiding it in the UI; caching a Spymaster projection and reusing it for an Operative.
- **Edge Cases:** A player is a Spymaster in one match and Operative in the next; reconnection must restore the correct projection; role of a waiting member.
- **Best Practices:** Filter at the source; make it impossible for a non-Spymaster projection to include unrevealed ownership.
- **Open Questions:** Full or delta state on each change (see ENG-RT-01)?
- **References:** [02 §2.11](../02-business-analysis/01-software-requirements.md), [16 §7](../02-business-analysis/15-player-session-reconnection.md).

### ENG-ST-04 — State recovery after interruption
- **Category:** State / Reliability.
- **Description:** After a transient interruption, the match must resume from the last consistent point within the room's life.
- **Business Impact:** Lost/rewound progress; corrupted or duplicated reveals.
- **Engineering Impact:** Requires a recoverable authoritative state and a defined recovery point.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** NFR-11, QM-16, INV-O4.
- **Possible Solutions:** Keep authoritative state recoverable for the room's lifetime; on recovery, resend role-filtered current state. **Adv:** resilience. **Disadv:** must define what "consistent point" means and avoid replaying terminal effects.
- **Common Mistakes:** Recovering to a mid-action point; replaying a reveal or a win.
- **Edge Cases:** Interruption exactly at a terminal reveal; recovery after abandonment; recovery of the paused overlay.
- **Best Practices:** Recover to the last committed atomic state; never re-emit already-applied terminal effects (CR-4).
- **Open Questions:** How long must in-flight room state remain recoverable, and what triggers give-up?
- **References:** [22 QM-16](../03-business-governance/04-quality-metrics.md).

---

## 8. Concurrency Risks

### ENG-CO-01 — Concurrent joins & capacity/nickname races
- **Category:** Concurrency.
- **Description:** Multiple players join at once near capacity or with the same nickname.
- **Business Impact:** Over-capacity rooms or duplicate nicknames (violating INV-R5/INV-P1).
- **Engineering Impact:** Capacity and nickname-uniqueness checks must be atomic with the add.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-JR-3/5, V-CAP-1, V-NICK-3, INV-R5/P1.
- **Possible Solutions:**
  1. *Serialize membership mutations per room.* **Adv:** correct. **Disadv:** slight contention at burst joins (small rooms → fine).
  2. *Atomic compare-and-add on capacity/nickname set.* **Adv:** parallel-friendly. **Disadv:** trickier to keep both checks atomic together.
- **Common Mistakes:** Check-then-add without atomicity; case-sensitive nickname comparison; counting capacity from a stale snapshot.
- **Edge Cases:** Two identical nicknames in the same instant; join at exactly capacity; a leave and a join racing for the last slot.
- **Best Practices:** One atomic admit operation combining capacity + nickname checks; case-insensitive, trimmed comparison.
- **Open Questions:** Should nicknames freed by a leaver be immediately reusable mid-burst?
- **References:** [15 §6](../02-business-analysis/14-lobby-room-lifecycle.md).

### ENG-CO-02 — Concurrent leaves & host-migration races
- **Category:** Concurrency.
- **Description:** The Host and others leave simultaneously; migration must still yield exactly one Host (or expire the room).
- **Business Impact:** Zero or two Hosts (violating INV-R1); orphaned rooms.
- **Engineering Impact:** Migration must be atomic and deterministic under concurrent departures.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-HM-1..4, INV-R1, V-HOST-2.
- **Possible Solutions:** Serialize membership/host changes; compute the deterministic successor (longest-present connected) within the same atomic step. **Adv:** single Host guaranteed. **Disadv:** must recompute successor if that successor is also leaving.
- **Common Mistakes:** Migrating to a player who is simultaneously leaving; leaving a brief window with no Host; non-deterministic successor.
- **Edge Cases:** Host and successor leave together; all leave at once (→ expire); Host leaves during a match (control-only migration).
- **Best Practices:** Recompute successor against the *post-leave* membership; if none connected, expire (BR-HM-3).
- **Open Questions:** Tie-break when "longest-present" is ambiguous (equal join times)?
- **References:** [15 §7](../02-business-analysis/14-lobby-room-lifecycle.md).

### ENG-CO-03 — Simultaneous host actions
- **Category:** Concurrency.
- **Description:** After a migration or with a stale client, two clients briefly believe they are Host and both issue host actions (start, kick).
- **Business Impact:** Double start, conflicting config.
- **Engineering Impact:** Host authority must be validated against current authoritative state at action time.
- **Severity:** Medium · **Likelihood:** Occasional.
- **Related Rules:** BR-HOST-1, V-HOST-1, INV-R1.
- **Possible Solutions:** Authorize each host action against the *current* Host identity, not the client's belief. **Adv:** rejects stale host. **Disadv:** requires the client to handle "you are no longer host".
- **Common Mistakes:** Trusting a client "isHost" flag; not revalidating after migration.
- **Edge Cases:** Kick issued by a just-demoted Host; start issued by both old and new Host.
- **Best Practices:** Server is the sole arbiter of who is Host now; stale host actions → `NOT_ROOM_HOST`.
- **Open Questions:** How to promptly notify a demoted Host client?
- **References:** [13](../02-business-analysis/12-business-error-catalog.md).

### ENG-CO-04 — Simultaneous reconnects / duplicate connections
- **Category:** Concurrency / Real-time.
- **Description:** The same identity reconnects from two tabs/devices, or an old socket lingers while a new one opens.
- **Business Impact:** Two live connections for one player could allow double actions or dual role-views (fairness).
- **Engineering Impact:** Enforce at-most-one active connection per identity (INV-P4); newest supersedes.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** INV-P4, PS-21..24, DUPLICATE_CONNECTION.
- **Possible Solutions:** On new connection with a valid token, atomically supersede/close the prior one. **Adv:** single source of action. **Disadv:** must cleanly drop the old socket and its queued intents.
- **Common Mistakes:** Allowing both sockets to submit intents; not invalidating queued intents from the superseded socket.
- **Edge Cases:** Two reconnects within milliseconds; reconnect while the old socket is mid-intent; refresh that reopens before the old closes.
- **Best Practices:** Bind actions to the *current* active connection; discard intents from superseded connections.
- **Open Questions:** Should the superseded device get an explanatory message or a silent close?
- **References:** [16 §9](../02-business-analysis/15-player-session-reconnection.md).

### ENG-CO-05 — Expiry racing with live activity
- **Category:** Concurrency.
- **Description:** An idle-expiry fires just as activity resumes, or a match ends exactly as the room empties.
- **Business Impact:** A live room wrongly closed, or cleanup skipped.
- **Engineering Impact:** Expiry decisions must be atomic with the latest activity timestamp/membership.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-RX-1/2, RP-11, F-VAL-08 (inactivity-based).
- **Possible Solutions:** Re-check idle condition atomically at fire time; record result before expiring (rank 3 before 8). **Adv:** avoids false expiry. **Disadv:** timer bookkeeping.
- **Common Mistakes:** Expiring on elapsed wall-clock rather than inactivity; expiring before recording a just-finished result.
- **Edge Cases:** Activity arrives during the expiry step; last player leaves as the match finishes.
- **Best Practices:** Treat idle timer as reset-on-activity; order result-record before expiry ([17 §7](../02-business-analysis/16-rule-precedence.md)).
- **Open Questions:** Granularity of the idle timer vs event volume (see ENG-SC-02)?
- **References:** [17 §7](../02-business-analysis/16-rule-precedence.md).

---

## 9. Real-Time Risks

### ENG-RT-01 — Lost / duplicated / out-of-order messages
- **Category:** Real-time.
- **Description:** Under unreliable networks, state updates or intents may be lost, duplicated, or reordered.
- **Business Impact:** Clients showing wrong board/turn; duplicated actions; missed reveals.
- **Engineering Impact:** Needs ordering/versioning and a way to resync; intents must be idempotent.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** NFR-2, CR-4; consistency [22 QM-10](../03-business-governance/04-quality-metrics.md).
- **Possible Solutions:**
  1. *Monotonic state version per room + full-state resync on gap.* **Adv:** simple recovery. **Disadv:** resync payload size.
  2. *Ordered deltas with sequence numbers.* **Adv:** small messages. **Disadv:** must detect/repair gaps; more complex.
- **Common Mistakes:** Assuming ordered, exactly-once delivery; applying a stale update over a newer one; no gap detection.
- **Edge Cases:** Duplicate reveal delivered twice; an update arriving after a newer one; client that missed the terminal event.
- **Best Practices:** Version authoritative state; clients apply only newer versions; resync on detected gaps; idempotent intents.
- **Open Questions:** Full-state vs delta sync threshold; how clients detect they are behind.
- **References:** [22 QM-05/QM-10](../03-business-governance/04-quality-metrics.md).

### ENG-RT-02 — Delayed events & slow clients
- **Category:** Real-time.
- **Description:** A slow client lags behind; its user may act on stale state (e.g., guess a card already revealed).
- **Business Impact:** Frustrating rejections; perceived unfairness.
- **Engineering Impact:** Server must reject stale intents gracefully and the client must reconcile.
- **Severity:** Medium · **Likelihood:** Frequent.
- **Related Rules:** V-GUESS-3, BR-GV-2, INV-B7.
- **Possible Solutions:** Validate intents against current state and return specific errors; client reconciles to latest state. **Adv:** correctness preserved. **Disadv:** UX must handle rejections smoothly.
- **Common Mistakes:** Accepting an intent based on the client's stale view; no reconciliation after rejection.
- **Edge Cases:** Guess on a card revealed a moment ago; end-turn after the turn already passed.
- **Best Practices:** Server-authoritative validation; clear, catalogued rejections ([13](../02-business-analysis/12-business-error-catalog.md)); client auto-resyncs.
- **Open Questions:** Should the UI visibly "catch up" (animate) or snap to current state?
- **References:** [13](../02-business-analysis/12-business-error-catalog.md).

### ENG-RT-03 — Reconnection & state resynchronization
- **Category:** Real-time.
- **Description:** A returning client must receive the full, correct, role-filtered current state fast (QM-07) and resume paused phases.
- **Business Impact:** Slow/incorrect resync disrupts play; wrong projection leaks info.
- **Engineering Impact:** Needs a reliable "current state for this role" snapshot on reconnect.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-DC-2/7, INV-P5, PS-17..20, QM-07.
- **Possible Solutions:** On reconnect, validate token/grace, then send the complete role-filtered snapshot and resume pause. **Adv:** deterministic resume. **Disadv:** snapshot cost per reconnect.
- **Common Mistakes:** Sending deltas the client can't anchor; restoring the wrong role's view; not resuming the paused phase.
- **Edge Cases:** Reconnect at the grace boundary; reconnect as the match ends; Spymaster reconnect must restore the Key.
- **Best Practices:** Always full role-filtered snapshot on reconnect; then resume [§8.3](../02-business-analysis/07-state-machines.md) pause.
- **Open Questions:** Snapshot size vs frequency of reconnects at scale (ENG-SC-03).
- **References:** [16 §8](../02-business-analysis/15-player-session-reconnection.md).

### ENG-RT-04 — Multiple tabs / devices per identity
- **Category:** Real-time.
- **Description:** A user opens the game in two tabs; both may try to represent the same player.
- **Business Impact:** Double actions, dual views, confusion.
- **Engineering Impact:** Same as ENG-CO-04 — one active connection per identity.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** INV-P4, PS-21..24.
- **Possible Solutions:** Newest connection supersedes; older is disabled. **Adv:** unambiguous actor. **Disadv:** user may be surprised by the drop.
- **Common Mistakes:** Letting both tabs act; syncing them as independent players.
- **Edge Cases:** Two tabs where one is a stale Spymaster view; tab reopened after refresh.
- **Best Practices:** Enforce single active connection; clearly indicate takeover.
- **Open Questions:** Distinguish "same person, second tab" from "nickname reused by another person"? (Token disambiguates.)
- **References:** [16 §9](../02-business-analysis/15-player-session-reconnection.md).

### ENG-RT-05 — Host disconnection mid-critical-phase
- **Category:** Real-time.
- **Description:** The Host (who may also be the active Spymaster) drops during a critical phase.
- **Business Impact:** Play may pause (if essential role) while control migration is also pending.
- **Engineering Impact:** Separate *play* effects (pause) from *control* effects (migration); order per precedence.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-DC-3/8, BR-HM, RP-9, [17 §6/7](../02-business-analysis/16-rule-precedence.md).
- **Possible Solutions:** Treat role-based pause and host migration as independent, both grace-bounded; match end preempts both. **Adv:** clean separation. **Disadv:** two concurrent timers to manage.
- **Common Mistakes:** Conflating host loss with match interruption; migrating control but forgetting the Spymaster pause (or vice versa).
- **Edge Cases:** Host==active Spymaster disconnects during clue phase; host disconnects exactly at match end (match end wins).
- **Best Practices:** Independent handling; precedence [17](../02-business-analysis/16-rule-precedence.md); control migration never alters match state (BR-EC-11).
- **Open Questions:** If Host and Spymaster are the same person, should migration prefer someone who can also take a vacated role? (Out of scope — roles don't change mid-match.)
- **References:** [16 §7](../02-business-analysis/15-player-session-reconnection.md).

---

## 10. Fair-Play Risks

### ENG-FP-01 — Hidden-card / Key leakage
- **Category:** Fair-play.
- **Description:** The single most dangerous failure: an Operative obtaining unrevealed ownership by any channel (payloads, timing, reconnect snapshot, spectator/waiting view).
- **Business Impact:** Destroys the game entirely.
- **Engineering Impact:** Ownership must never leave the server for a non-Spymaster projection.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** BR-CO-4, INV-B9, BR-JR-6a, NFR-3, QM-15.
- **Possible Solutions:** Server-side role projections; the Key is included only in Spymaster projections; waiting members get no board data. **Adv:** safe by construction. **Disadv:** must audit every delivery path (initial, delta, reconnect, rematch).
- **Common Mistakes:** Sending full board to all and hiding client-side; leaking via reconnect snapshot; exposing ownership in board-generation debug/telemetry.
- **Edge Cases:** Spymaster→Operative role change between matches reusing a cached projection; waiting member during a match; observability/telemetry inadvertently carrying ownership.
- **Best Practices:** Treat ownership as a server secret until reveal; test that no Operative-facing payload ever contains it; scrub telemetry.
- **Open Questions:** How to *test* leakage comprehensively (negative testing across all delivery paths)?
- **References:** [11 INV-B9](../02-business-analysis/10-business-invariants.md), [22 QM-15](../03-business-governance/04-quality-metrics.md).

### ENG-FP-02 — Client manipulation & forged intents
- **Category:** Fair-play.
- **Description:** A modified client submits intents it shouldn't (guess out of turn, clue as Operative, reveal a specific card).
- **Business Impact:** Cheating; corrupted outcomes.
- **Engineering Impact:** Every intent authorized server-side against role/team/phase/state.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** SEC-1/4, BR-CL-1, BR-GV-1/4, INV-G4; errors NOT_SPYMASTER/NOT_YOUR_TURN.
- **Possible Solutions:** Server-authoritative authorization on every intent; never trust client-declared role/team. **Adv:** robust. **Disadv:** must centralize authorization consistently.
- **Common Mistakes:** Trusting client role flags; authorizing by UI state; missing a check on a less-common intent.
- **Edge Cases:** Forged "start" by non-Host; forged reveal of the assassin against an opponent; forged team.
- **Best Practices:** Authorize from authoritative server state only; deny by default.
- **Open Questions:** Rate/shape limits to deter automated abuse (SEC-6) — thresholds?
- **References:** [02 §2.12](../02-business-analysis/01-software-requirements.md).

### ENG-FP-03 — Replay / duplicate command attacks
- **Category:** Fair-play.
- **Description:** Re-sending a previously valid intent (a guess, an end-turn) to trigger it again.
- **Business Impact:** Double effects; skipped turns; unfair reveals.
- **Engineering Impact:** Intents must be idempotent and bound to current state/turn context.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** CR-4, INV-B7/G7; BR-EC-13.
- **Possible Solutions:** Idempotency keys + state/turn binding (an intent valid only for the current turn/phase). **Adv:** replays become no-ops or rejects. **Disadv:** bookkeeping of recent keys.
- **Common Mistakes:** Accepting any well-formed intent regardless of turn context; no dedup.
- **Edge Cases:** Replayed guess after the card is revealed (should reject); replayed end-turn in a new turn.
- **Best Practices:** Bind each intent to the specific turn/phase/state version; apply once.
- **Open Questions:** Retention window for keys per room (shared with ENG-GP-02).
- **References:** [17 §8](../02-business-analysis/16-rule-precedence.md).

### ENG-FP-04 — Out-of-turn / wrong-role action attempts
- **Category:** Fair-play.
- **Description:** Frequent, often-innocent attempts to act out of turn or role (an Operative tapping during the opponent's turn).
- **Business Impact:** Must be reliably rejected without affecting state.
- **Engineering Impact:** Cheap, consistent authorization checks on the hot path.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-TO-5, V-CLUE-1/2, V-GUESS-1/2, INV-G4.
- **Possible Solutions:** Uniform pre-checks (role, team, phase, state) before any effect. **Adv:** simple, consistent. **Disadv:** must be applied everywhere.
- **Common Mistakes:** Rejecting silently (confusing UX) or, worse, partially applying.
- **Edge Cases:** Action in the paused overlay; action during match-end resolution.
- **Best Practices:** Return specific catalogued errors; never mutate on rejection.
- **Open Questions:** Should repeated out-of-turn attempts be rate-limited?
- **References:** [13](../02-business-analysis/12-business-error-catalog.md).

---

## 11. Dictionary Risks

### ENG-DC-01 — Word quality / duplicates / offensive words
- **Category:** Dictionary.
- **Description:** A dictionary version may contain duplicates, ambiguous, or offensive words despite curation.
- **Business Impact:** Broken boards (duplicates violate INV-B6), or reputational/cultural harm.
- **Engineering Impact:** Publish-time validation and a safe correction path (new version + retire).
- **Severity:** Medium · **Likelihood:** Occasional.
- **Related Rules:** DM-V2/V3, DM-Q1..Q4, INV-B6/D2.
- **Possible Solutions:** Enforce uniqueness/min-size at publish; corrections via new immutable versions (never in-place). **Adv:** reproducibility + safe fixes. **Disadv:** operational discipline for content team.
- **Common Mistakes:** Editing a live version in place (breaks INV-D3); case/whitespace duplicates slipping through; language-specific assumptions.
- **Edge Cases:** Homographs across scripts; a word offensive in one region but fine in another (region-scoped); near-duplicates differing only by case.
- **Best Practices:** Language-neutral uniqueness (trim + case-fold + Unicode-normalize); retire+replace for corrections.
- **Open Questions:** Who signs off cultural appropriateness per region, and what is the review SLA? (Content-team owned.)
- **References:** [14 §8/§9](../02-business-analysis/13-dictionary-management.md).

### ENG-DC-02 — Version mismatch & updates during active matches
- **Category:** Dictionary.
- **Description:** A dictionary is updated while matches are running; those matches must keep their bound version.
- **Business Impact:** Changing words mid-match breaks fairness and reproducibility.
- **Engineering Impact:** Each match binds to a specific version at start; new versions apply only to new matches.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** INV-D3, DM-S2/U2, F-16 exception.
- **Possible Solutions:** Resolve and pin the version at match start; never dereference "current" mid-match. **Adv:** stable boards. **Disadv:** must retain deprecated versions in use.
- **Common Mistakes:** Reading the "active" version each time instead of the pinned one; retiring a version still bound to a live match.
- **Edge Cases:** Rematch after an update (uses new active version); retirement during a live match (must not drop the bound version).
- **Best Practices:** Pin at start; retain versions referenced by any live match (see [23](../03-business-governance/05-data-lifecycle-retention.md)).
- **Open Questions:** How long to retain deprecated versions after their last live match ends?
- **References:** [14 §10](../02-business-analysis/13-dictionary-management.md).

### ENG-DC-03 — Insufficient / unsupported dictionaries
- **Category:** Dictionary.
- **Description:** A selected region's active version has fewer than 25 usable words, or the region isn't available.
- **Business Impact:** Cannot start a fair match.
- **Engineering Impact:** Validate availability and min-size before start; clear errors.
- **Severity:** High · **Likelihood:** Rare.
- **Related Rules:** BR-GS-3, V-DICT-1/2, INV-D2; errors DICTIONARY_TOO_SMALL/NOT_FOUND.
- **Possible Solutions:** Pre-start validation with specific errors and a default fallback where appropriate. **Adv:** predictable. **Disadv:** must define fallback policy.
- **Common Mistakes:** Discovering insufficiency only during board generation (late failure); silent fallback that surprises players.
- **Edge Cases:** Exactly 25 words (no spare for variety across rematches); region removed between selection and start.
- **Best Practices:** Validate at selection and again at start; surface a clear reason.
- **Open Questions:** Minimum recommended pool size beyond the 25 hard floor for variety?
- **References:** [14 §7/§8](../02-business-analysis/13-dictionary-management.md).

---

## 12. Session Risks

### ENG-SE-01 — Nickname collisions & identity ambiguity
- **Category:** Session.
- **Description:** Nicknames are the only human identifier; collisions and near-duplicates confuse players and races (ENG-CO-01).
- **Business Impact:** Confusion about who acted; join rejections.
- **Engineering Impact:** Per-room uniqueness enforced atomically; identity tracked by token, not nickname.
- **Severity:** Medium · **Likelihood:** Frequent.
- **Related Rules:** BR-JR-3, INV-P1, V-NICK-3.
- **Possible Solutions:** Enforce case-insensitive, trimmed uniqueness; internally key players by token, display by nickname. **Adv:** stable identity under rename-like reuse. **Disadv:** must reconcile display vs internal id.
- **Common Mistakes:** Using nickname as the internal key; case-sensitive checks; not freeing a nickname on leave.
- **Edge Cases:** Whitespace/Unicode look-alikes; a leaver's nickname reused immediately; same nickname across different rooms (allowed).
- **Best Practices:** Token = identity; nickname = display, unique per room only.
- **Open Questions:** Should visually-confusable nicknames be rejected, or is exact-match uniqueness enough?
- **References:** [16 §3](../02-business-analysis/15-player-session-reconnection.md).

### ENG-SE-02 — Reconnect-token loss / theft / expiry
- **Category:** Session / Fair-play.
- **Description:** A token may be lost (rejoin as new), expire (grace passed), or be obtained by another party.
- **Business Impact:** Lost seat, or someone hijacking a seat.
- **Engineering Impact:** Tokens must be unguessable, room-scoped, single-use-per-session, and grace-bounded.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-JR-7, BR-DC-2, PS-2/3, INV-P2; errors RECONNECT_TOKEN_INVALID/WINDOW_EXPIRED.
- **Possible Solutions:** Strong, opaque, room-scoped tokens; invalidate on session end; supersede on new connection. **Adv:** limits hijack window. **Disadv:** token lifecycle management.
- **Common Mistakes:** Guessable/sequential tokens; not invalidating on removal; accepting an expired token as resumption.
- **Edge Cases:** Token reuse after grace (should be a fresh join); token presented from a second device (supersede).
- **Best Practices:** Opaque unguessable tokens; strict grace enforcement; PII-free.
- **Open Questions:** Any protection needed against a shared-device token being reused by the next person? (Room-scoped, transient — low risk; document assumption.)
- **References:** [16 §3](../02-business-analysis/15-player-session-reconnection.md).

### ENG-SE-03 — Player abandonment & grace tuning
- **Category:** Session.
- **Description:** Players leave and never return; grace periods trade off patience vs stalling the game.
- **Business Impact:** Too-long grace stalls others; too-short grace abandons matches unfairly.
- **Engineering Impact:** Tunable grace with correct pause/abandonment behaviour.
- **Severity:** High · **Likelihood:** Frequent.
- **Related Rules:** BR-DC-4/5, PS-25, CONST-RECONNECT-GRACE-PERIOD ([21 §7](../03-business-governance/03-business-constants-catalog.md)).
- **Possible Solutions:** Configurable grace within a range; abandonment only for essential, irreplaceable roles. **Adv:** balances fairness. **Disadv:** finding good defaults needs real data.
- **Common Mistakes:** Pausing for non-essential leavers; abandoning too eagerly; not communicating the wait.
- **Edge Cases:** Abandonment vs a possible last-second reconnect; multiple simultaneous essential losses.
- **Best Practices:** Pause only when the active team truly can't proceed; clear countdown; abandonment only after grace.
- **Open Questions:** Optimal default grace values from playtesting (feeds [21](../03-business-governance/03-business-constants-catalog.md))?
- **References:** [16 §11](../02-business-analysis/15-player-session-reconnection.md).

---

## 13. Room Risks

### ENG-RM-01 — Empty / idle room cleanup
- **Category:** Room.
- **Description:** Rooms left empty or idle must be reclaimed without closing live ones.
- **Business Impact:** Resource leak, or wrongful closure.
- **Engineering Impact:** Reliable, race-safe expiry keyed on inactivity/emptiness.
- **Severity:** Medium · **Likelihood:** Frequent.
- **Related Rules:** BR-RX-1/2, INV-R4; ENG-CO-05.
- **Possible Solutions:** Inactivity-reset timers + immediate empty-room close; atomic re-check at fire time. **Adv:** bounded live state. **Disadv:** timer management at scale (ENG-SC-02).
- **Common Mistakes:** Wall-clock expiry ignoring activity; leaking rooms whose last player left mid-transition.
- **Edge Cases:** Activity during the expiry step; empty exactly at match end.
- **Best Practices:** Reset-on-activity; record result before expiring.
- **Open Questions:** Grace between "empty" and "closed" to allow instant rejoin?
- **References:** [15 §11](../02-business-analysis/14-lobby-room-lifecycle.md).

### ENG-RM-02 — Everyone disconnects simultaneously
- **Category:** Room.
- **Description:** All players drop at once (e.g., shared venue Wi-Fi fails); no connected player remains.
- **Business Impact:** Room should survive briefly for reconnection, then expire.
- **Engineering Impact:** Distinguish "all disconnected but within grace" from "empty".
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-HM-3, BR-RX, PS; ENG-CO-02.
- **Possible Solutions:** Keep the room alive through the grace window for mass reconnection; expire if none return. **Adv:** tolerant of venue-wide drops. **Disadv:** holds state longer.
- **Common Mistakes:** Treating "all disconnected" as "empty" and closing immediately; losing match state before anyone can return.
- **Edge Cases:** Partial return (enough to continue vs not); Host among the returners vs not.
- **Best Practices:** Grace-bounded survival; deterministic Host re-establishment on return.
- **Open Questions:** Should mass-disconnect grace differ from single-player grace?
- **References:** [16 §12 (row 7)](../02-business-analysis/15-player-session-reconnection.md).

### ENG-RM-03 — Rematch failure paths
- **Category:** Room.
- **Description:** A rematch may fail validation (players left, invalid composition, dictionary now too small).
- **Business Impact:** Confusing dead-ends after a match.
- **Engineering Impact:** Clear re-validation and recovery to a fixable lobby state.
- **Severity:** Medium · **Likelihood:** Occasional.
- **Related Rules:** RF-4, V-START-*, ENG-GP-09.
- **Possible Solutions:** Re-validate on rematch; return to Lobby with specific guidance on what to fix. **Adv:** recoverable. **Disadv:** more states to handle.
- **Common Mistakes:** Attempting to start with stale composition; not surfacing why it failed.
- **Edge Cases:** Rematch after abandonment; dictionary retired between matches; a team lost its Spymaster.
- **Best Practices:** Always re-validate; guide the Host to the missing condition.
- **Open Questions:** Should the system suggest auto-rebalancing teams, or leave it manual (manual = faithful)?
- **References:** [15 §9](../02-business-analysis/14-lobby-room-lifecycle.md).

---

## 14. Reliability Risks

### ENG-RE-01 — Crash / unexpected shutdown mid-operation
- **Category:** Reliability.
- **Description:** The authority may stop unexpectedly mid-action, risking loss or partial application of in-flight game state.
- **Business Impact:** Lost matches or corrupted state on restart.
- **Engineering Impact:** Requires a recovery point and atomic apply so restart lands on a consistent state.
- **Severity:** Critical · **Likelihood:** Occasional.
- **Related Rules:** NFR-4/11, INV-O4, QM-16; ENG-ST-02/04.
- **Possible Solutions:**
  1. *Atomic commit + recoverable authoritative state.* **Adv:** consistent recovery. **Disadv:** must define what persists and for how long.
  2. *In-memory only, accept loss on crash.* **Adv:** simplest. **Disadv:** violates resilience expectations for in-progress matches. **Trade-off vs [23](../03-business-governance/05-data-lifecycle-retention.md) (transient) — recovery need not imply long-term storage, only room-lifetime durability.**
- **Common Mistakes:** Broadcasting before commit; assuming the process never dies; replaying terminal effects on recovery.
- **Edge Cases:** Crash exactly at a terminal reveal; crash during host migration or board generation.
- **Best Practices:** Commit-then-broadcast; idempotent recovery; never re-apply committed terminal effects.
- **Open Questions:** What durability guarantee is required for an in-progress match (room-lifetime only)?
- **References:** [22 QM-16](../03-business-governance/04-quality-metrics.md), [23](../03-business-governance/05-data-lifecycle-retention.md).

### ENG-RE-02 — Interrupted / partial multi-step operations
- **Category:** Reliability.
- **Description:** Multi-step flows (start = validate+generate+lock+broadcast; migration; expiry+cleanup) may be interrupted between steps.
- **Business Impact:** Half-started matches, half-migrated hosts, half-cleaned rooms.
- **Engineering Impact:** Each composite operation needs a defined commit boundary and safe re-entry.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-GS-4, BR-HM-4, BR-RX-4; ENG-ST-02.
- **Possible Solutions:** Make composite operations atomic or safely resumable/idempotent. **Adv:** no partial results. **Disadv:** careful design of each boundary.
- **Common Mistakes:** Locking setup but failing to generate the board; releasing a room code before cleanup completes.
- **Edge Cases:** Interruption between "board generated" and "first turn opened"; between "record result" and "enter Post-Match".
- **Best Practices:** Define one commit point per composite; re-entry detects and completes or rolls back.
- **Open Questions:** Which composite operations most need atomicity guarantees first?
- **References:** [09](../02-business-analysis/08-business-workflows.md).

---

## 15. Scalability Risks

### ENG-SC-01 — Per-room memory & connection growth
- **Category:** Scalability.
- **Description:** From 10 → 100k concurrent players, per-room state and live connections accumulate; each room is small but there are many.
- **Business Impact:** Memory/connection exhaustion degrades or drops matches.
- **Engineering Impact:** Bounded, predictable per-room footprint; many independent rooms (SCAL-1/2).
- **Severity:** High · **Likelihood:** Occasional (at growth).
- **Related Rules:** SCAL-1/2/4, QM-08.
- **Possible Solutions:** Keep per-room state minimal and bounded (one 25-card board, few players); reclaim aggressively on expiry. **Adv:** linear scaling by room count. **Disadv:** none inherent; discipline required.
- **Common Mistakes:** Accumulating unbounded history/events per room; not releasing state on expiry; keeping dead connections.
- **Edge Cases:** Many near-empty idle rooms; rooms with the maximum operatives; long-lived rooms across many rematches.
- **Best Practices:** Bound per-room state; prompt cleanup; treat rooms as isolated units.
- **Open Questions:** Expected distribution of room sizes/lifetimes to size capacity (needs product data).
- **References:** [02 §2.13](../02-business-analysis/01-software-requirements.md).

### ENG-SC-02 — Timer / expiry management at scale
- **Category:** Scalability.
- **Description:** Every room has idle/grace/migration timers; naively, 100k rooms means enormous timer churn.
- **Business Impact:** Expiry lag (leaks) or timer storms (overload).
- **Engineering Impact:** Efficient, scalable timing without per-tick scanning of all rooms.
- **Severity:** Medium · **Likelihood:** Occasional.
- **Related Rules:** BR-RX-1, BR-DC, BR-HM; ENG-CO-05.
- **Possible Solutions:** Coarse-grained/bucketed expiry checks vs precise per-room timers — trade precision for scalability. **Adv (bucketed):** cheap at scale. **Disadv:** less precise expiry timing. **Trade-off:** exactness vs cost.
- **Common Mistakes:** Scanning all rooms every tick; precise timers per room at huge counts.
- **Edge Cases:** Bursts of simultaneous expirations; grace timers firing during activity (ENG-CO-05).
- **Best Practices:** Reset-on-activity semantics; efficient expiry evaluation; atomic re-check at fire time.
- **Open Questions:** Acceptable imprecision in idle expiry (seconds? a minute?).
- **References:** [15 §11](../02-business-analysis/14-lobby-room-lifecycle.md).

### ENG-SC-03 — Broadcast fan-out & hot rooms
- **Category:** Scalability.
- **Description:** Each action fans out role-filtered updates to a room's participants; reconnect snapshots add spikes.
- **Business Impact:** Latency spikes (breaks QM-05) under load.
- **Engineering Impact:** Efficient per-room fan-out and snapshot generation, per role.
- **Severity:** Medium · **Likelihood:** Occasional.
- **Related Rules:** NFR-1/5, QM-05/08; ENG-ST-03.
- **Possible Solutions:** Compute role projections once per change and fan out; minimize snapshot frequency. **Adv:** bounded work per action. **Disadv:** projection caching must respect role boundaries (never leak).
- **Common Mistakes:** Recomputing per recipient; sending full state on every tiny change; leaking via a shared cached projection.
- **Edge Cases:** Many reconnects at once (venue Wi-Fi returns); large operative counts.
- **Best Practices:** One projection per role per change; deltas where safe; snapshots only on (re)connect.
- **Open Questions:** Full-state vs delta threshold (shared with ENG-RT-01).
- **References:** [22 QM-05](../03-business-governance/04-quality-metrics.md).

### ENG-SC-04 — Resource exhaustion & cleanup backpressure
- **Category:** Scalability.
- **Description:** Under load, abandoned rooms, dead connections, and retained keys/events can accumulate faster than cleanup.
- **Business Impact:** Gradual degradation, then failures.
- **Engineering Impact:** Cleanup must keep pace with creation; bounded retention.
- **Severity:** High · **Likelihood:** Occasional.
- **Related Rules:** BR-RX-4, [23](../03-business-governance/05-data-lifecycle-retention.md) retention; SCAL-4.
- **Possible Solutions:** Bounded retention windows (idempotency keys, events); prompt expiry; backpressure on room creation if needed. **Adv:** stable under load. **Disadv:** must choose retention windows carefully.
- **Common Mistakes:** Unbounded key/event retention; cleanup slower than creation; never releasing room codes.
- **Edge Cases:** Creation spikes; mass simultaneous expirations; long-lived rooms holding keys.
- **Best Practices:** Bound every retained set; release codes on expiry; monitor cleanup lag.
- **Open Questions:** Retention windows for idempotency keys/events at target scale (feeds [23](../03-business-governance/05-data-lifecycle-retention.md))?
- **References:** [23 §5](../03-business-governance/05-data-lifecycle-retention.md).

---

## 16. Cross-Cutting Open Questions

These recur across challenges and should be resolved early because they shape many later
decisions (but are **not** decided here):

1. **Full-state vs delta synchronization**, and how a client detects it is behind (ENG-RT-01/03, ENG-SC-03).
2. **Idempotency-key retention window** per room (ENG-GP-02, ENG-FP-03, ENG-SC-04).
3. **Durability guarantee for in-progress matches** — room-lifetime recoverability without long-term storage (ENG-RE-01, [23](../03-business-governance/05-data-lifecycle-retention.md)).
4. **Neutral normalization form** for clue/board comparison across scripts (ENG-GP-06, ENG-DC-01).
5. **Default grace values** and whether mass-disconnect grace differs (ENG-SE-03, ENG-RM-02).
6. **Auditable/seeded board generation** and seed protection (ENG-GP-08, ENG-FP-01).
7. **Expiry precision vs cost** at scale (ENG-SC-02).
8. **Deprecated dictionary-version retention** tied to live matches (ENG-DC-02, [23](../03-business-governance/05-data-lifecycle-retention.md)).

> None of the above selects a technology or architecture. They are the decisions that a future
> architecture phase must make, now surfaced with their trade-offs.

## 17. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial engineering challenges & risk analysis across gameplay, state, concurrency, real-time, fair-play, dictionary, session, room, reliability, and scalability. Analysis only; no architecture or technology decisions. |
