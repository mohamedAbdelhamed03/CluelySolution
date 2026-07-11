# ADR-003 — Per-Room Coordination Model

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Governs** | How all work targeting a single room is coordinated so that correctness, determinism, and fairness are structural. |
| **Complies with** | [ADR-000 Vocabulary](ADR-000-architecture-vocabulary.md) (terms used verbatim) and [ADR-001 Overall Architecture Style](ADR-001-overall-architecture-style.md) (single-writer room entity). Does **not** redefine business rules or contradict ADR-001. |
| **Scope note** | Defines the **coordination model** only. No transport, networking, APIs, persistence, databases, frameworks, deployment, or scaling technology is chosen here. Related mechanisms are deferred to their own ADRs (real-time → ADR-004, recovery → ADR-005, visibility → ADR-006, isolation/distribution → ADR-007). |

---

## 1. Executive Summary

All work targeting a single room is coordinated by a **Serialized Single Writer**: each live room has
**exactly one Authority** (the **Room Entity** of [ADR-001](ADR-001-overall-architecture-style.md))
that admits and processes that room's **Intents strictly one at a time**, in a deterministic order,
against the room's authoritative state. Every state-changing Intent flows through the same conceptual
pipeline — **Validation → Admission → Ordering → Adjudication → State Transition → Commit → Event
Publication → Role Filtering → Delivery** — and no state mutation may bypass it.

Because only one writer ever touches a room's state and it processes intents sequentially,
**race conditions cannot occur within a room by construction**: simultaneous guesses, joins, or
reconnects are ordered and each is re-evaluated against the latest committed state (first-valid-wins).
This makes the [consistency boundaries](../06-consistency-boundaries.md) (CB-01…CB-10), the
[rule precedence](../../02-business-analysis/16-rule-precedence.md), and the
[business invariants](../../02-business-analysis/10-business-invariants.md) enforceable *structurally*
rather than by external locking or distributed transactions.

Rooms are fully **isolated**: there is no cross-room coordination (ADR-000 *Room Isolation*). The model
is the **simplest** coordination that guarantees the dominant drivers (fairness, correctness,
determinism) and is chosen to **evolve** — a serialized single writer can later be distributed as a
per-room owner across nodes ([ADR-007], [ADR-001 A5 evolution](ADR-001-overall-architecture-style.md#5-candidate-architectures))
without changing the model or the rules core.

> One-line statement: **one Authority per room, one Intent at a time, deterministic order,
> commit-then-broadcast, no bypass, no cross-room coordination.**

---

## 2. Problem Statement

### Why coordination exists
A room receives concurrent Intents from multiple participants over unreliable networks (two Operatives
guessing at once, a join racing a leave, a reconnect arriving mid-processing). The authoritative room
state (S-01…S-04) is a single [aggregate](ADR-000-architecture-vocabulary.md) whose invariants must
hold as a whole. Without a defined coordination model, concurrent writers corrupt it.

### What fails without it
- **Double-reveal / mis-counted guesses** — two guesses resolve against stale state (ENG-GP-01).
- **Two active clues / two active turns** — overlapping mutations (violating [INV-G2/G3](../../02-business-analysis/10-business-invariants.md)).
- **Missed or duplicated terminal detection** — a win/assassin evaluated against a partially-updated
  board (violating [INV-O2](../../02-business-analysis/10-business-invariants.md)).
- **Non-deterministic outcomes** — results depend on arrival timing, not the fixed
  [precedence](../../02-business-analysis/16-rule-precedence.md) (violating [AP-06](../../06-architecture-governance/01-architecture-principles.md)).
- **Hidden-information leaks** under concurrent delivery (ENG-FP-01).

### Which business rules depend on it
Guess resolution ([BR-GV/CG/IG/NC/OPP/ASN](../../02-business-analysis/02-business-rules.md)), clue
rules ([BR-CL](../../02-business-analysis/02-business-rules.md)), turn rules
([BR-TO/TE](../../02-business-analysis/02-business-rules.md)), win/loss
([BR-WIN/LOSE/ASN](../../02-business-analysis/02-business-rules.md)), host singularity
([BR-HM](../../02-business-analysis/02-business-rules.md)), and the entire
[Rule Precedence](../../02-business-analysis/16-rule-precedence.md) all assume a single, ordered point
of adjudication.

### Which engineering challenges require it
[ENG-GP-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) (simultaneous
guesses), ENG-GP-05 (turn-transition atomicity), ENG-ST-01/02 (valid/atomic state), ENG-CO-01/02/04
(concurrent joins/leaves/reconnects), ENG-FP-03 (replay), ENG-RE-01/02 (recovery) — the P1 cluster in
[Enrichment §5.1](../../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary).

### Why it is foundational
State ownership, recovery, real-time delivery, and command/query strategy all assume *how* a room's
work is ordered. Deciding coordination first makes those later ADRs coherent; deciding them first would
risk incompatible assumptions.

---

## 3. Coordination Requirements

Every requirement traces to prior documentation.

| # | Requirement | Traces to |
|---|-------------|-----------|
| CR-1 | **Deterministic execution** — same intents + state → same outcome, per precedence. | [Rule Precedence](../../02-business-analysis/16-rule-precedence.md), [AP-06](../../06-architecture-governance/01-architecture-principles.md), [QS-02](../09-quality-attribute-scenarios.md) |
| CR-2 | **Atomic state transitions** — reveal+counts+turn+terminal all-or-nothing. | [CB-01/02/03](../06-consistency-boundaries.md), ENG-ST-02 |
| CR-3 | **Single authoritative outcome per intent** — at most one committed effect. | [INV-O1](../../02-business-analysis/10-business-invariants.md), CR-4 (idempotency) |
| CR-4 | **Single writer / one authority per room.** | [State Ownership §cross-cutting](../05-state-ownership.md), [ADR-001](ADR-001-overall-architecture-style.md) |
| CR-5 | **Room isolation** — no cross-room coordination/shared mutable state. | [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md), SCAL-1 |
| CR-6 | **Fairness** — hidden information never leaks during concurrent processing. | [INV-B9](../../02-business-analysis/10-business-invariants.md), [QS-01](../09-quality-attribute-scenarios.md) |
| CR-7 | **Correctness** — only valid state transitions; no bypass. | [State Machines](../../02-business-analysis/07-state-machines.md), [AP-09](../../06-architecture-governance/01-architecture-principles.md) |
| CR-8 | **Recoverability** — coordination supports recovery to last commit without replaying terminals. | ENG-RE-01, [QS-06](../09-quality-attribute-scenarios.md) |
| CR-9 | **Replay safety (idempotency)** — duplicates/retries have single effect. | CR-4 governance, ENG-GP-02/FP-03 |
| CR-10 | **Graceful disconnect handling** — pause/abandon decided against consistent state. | [Session & Reconnection](../../02-business-analysis/15-player-session-reconnection.md), [CB-08](../06-consistency-boundaries.md) |
| CR-11 | **Future scalability** — model extends to distributed per-room ownership without change. | [ADR-001 evolution](ADR-001-overall-architecture-style.md#9-consequences), [ADR-007] (future) |
| CR-12 | **Commit-then-broadcast** — no observation of partial state. | [Interactions I-08](../08-interaction-discovery.md) |
| CR-13 | **Ordering across coincident events** — match-end preempts turn preempts migration preempts expiry. | [Rule Precedence §3](../../02-business-analysis/16-rule-precedence.md) |

---

## 4. Candidate Coordination Models

Eight models were considered. None is dismissed without engineering justification. All are evaluated at
the **per-room** scope required by ADR-001.

### M1 — Serialized Single Writer (RECOMMENDED)
- **Core idea:** One Authority per room owns the room's state and processes its Intents **strictly one
  at a time** from an ordered admission point; each Intent is validated and adjudicated against the
  latest committed state; effects commit atomically before broadcast.
- **Advantages:** Race conditions impossible within a room *by construction*; determinism and
  precedence are trivial to enforce (a single ordered stream); atomicity is natural (one writer, one
  commit); simplest reasoning and testing; strongest fairness posture (one adjudication + one delivery
  boundary).
- **Disadvantages:** Per-room throughput is bounded by sequential processing (acceptable — rooms are
  tiny: ≤ ~20 players, one 25-card board); requires **room→authority routing** and a **recovery**
  strategy for the owner (deferred to ADR-007/ADR-005).
- **Complexity:** Low.
- **Failure modes:** Loss of the owner mid-processing → handled by recovery (commit-then-broadcast +
  idempotent replay of the *in-flight* intent only).
- **Scalability:** By room count; rooms distribute across owners naturally (isolation).
- **Operational complexity:** Low (MVP).
- **Determinism:** Highest — single ordered stream.
- **Recoverability:** High — last commit is the recovery point.
- **Fairness:** Highest.
- **Testing difficulty:** Lowest — deterministic, replayable single stream.
- **Evolution potential:** High — becomes a distributed per-room owner without model change.

### M2 — Optimistic Concurrency (version/compare-and-set on room state)
- **Core idea:** Multiple workers may attempt an Intent concurrently; each reads a version, computes a
  new state, and commits only if the version is unchanged; conflicts retry.
- **Advantages:** Higher parallelism when conflicts are rare; no long-held locks; stateless workers
  possible.
- **Disadvantages:** The hot path (guess resolution) is **highly contended per room** (all guesses
  touch the same aggregate), so conflicts are the *common* case, not the rare one → retry storms and
  latency spikes; retries make **precedence/ordering nondeterministic** unless an ordering layer is
  added (which reintroduces serialization); harder to reason about.
- **Complexity:** Medium–high (retry + conflict semantics).
- **Failure modes:** Livelock under contention; subtle ordering bugs; partial application if commit
  isn't atomic.
- **Scalability:** Good only under low contention — not our profile within a room.
- **Determinism:** Weak without an added ordering layer.
- **Recoverability:** Good (versioned state) but complicated by in-flight retries.
- **Fairness:** Adequate if ordering is added — but then it approximates M1 with extra machinery.
- **Testing difficulty:** High (nondeterministic interleavings).
- **Evolution:** Fine, but the per-room contention makes it a poor fit.

### M3 — Lock-Based Coordination (mutual exclusion per room)
- **Core idea:** Acquire a per-room lock; whoever holds it mutates state; release after commit.
- **Advantages:** Simple mental model; strong mutual exclusion; effectively serializes writers.
- **Disadvantages:** A correctly-scoped per-room lock **is** serialization — but via a shared lock
  primitive that adds failure modes (deadlock, lock loss, fencing, lease expiry) and, in a distributed
  setting, a distributed-lock dependency; ordering fairness (who gets the lock next) is not inherently
  deterministic.
- **Complexity:** Medium (correct locking is subtle; distributed locking more so).
- **Failure modes:** Deadlock, stuck locks on holder death, split-brain on distributed locks.
- **Scalability:** Fine per room; distributed locks add operational risk.
- **Determinism:** Depends on lock-acquisition ordering (not guaranteed).
- **Recoverability:** Complicated by orphaned locks.
- **Fairness:** Not inherent (lock acquisition order).
- **Testing difficulty:** Medium–high (timing-dependent).
- **Evolution:** Distributed locking is a known operational hazard.
- **Verdict:** Achieves the same mutual exclusion as M1 but via a heavier, failure-prone primitive; M1
  gets the guarantee without a shared lock.

### M4 — Compare-and-Swap (CAS) on state (a primitive, not a full model)
- **Core idea:** Lock-free single-word/state CAS to publish new state.
- **Advantages:** Lock-free; low overhead for tiny updates.
- **Disadvantages:** Insufficient for **multi-field aggregate** transitions (reveal+counts+turn+terminal)
  which are not a single word; degenerates into optimistic concurrency (M2) with the same contention and
  ordering problems for real transitions; ABA hazards.
- **Complexity:** Deceptively high for aggregates.
- **Determinism / Fairness / Testing:** Same weaknesses as M2.
- **Verdict:** A building block, not a coordination model for a multi-field aggregate; folded into M2.

### M5 — Event-Sourced Coordination (append-only intent/event log per room)
- **Core idea:** Coordinate by appending to a per-room ordered log; state is the fold of events; a
  single appender enforces order.
- **Advantages:** Natural ordering and audit trail; excellent recovery/replay; append point *is* a
  serialization point.
- **Disadvantages:** The **coordination guarantee still comes from the single ordered appender** (i.e.,
  M1 underneath); adds event-store semantics and snapshotting complexity that the MVP does not require
  ([AP-12](../../06-architecture-governance/01-architecture-principles.md), [AAP-05](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Complexity:** Medium–high.
- **Determinism:** High (ordered log).
- **Recoverability:** Excellent — a genuine strength.
- **Fairness / Testing:** Good.
- **Evolution:** Strong.
- **Verdict:** A viable *realization* of the recovery/state aspects, but as a coordination *model* it
  is M1 (single ordered writer) plus a persistence choice — that persistence choice belongs to ADR-005,
  not here. Not selected as the coordination model, but explicitly compatible with M1.

### M6 — Distributed Coordination (consensus/quorum across nodes per room)
- **Core idea:** Multiple nodes co-own a room and agree via consensus.
- **Advantages:** Fault tolerance; no single owner.
- **Disadvantages:** Massive complexity/latency for a **single-room, low-value-per-message** workload;
  consensus per guess is disproportionate; premature ([AAP-12](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Complexity / Operational risk:** Very high.
- **Determinism:** Achievable but at high cost.
- **Verdict:** Unjustified for room-scale coordination; rejected as premature and disproportionate.

### M7 — Queue-Based Coordination (per-room ordered intake feeding one processor)
- **Core idea:** Each room has an ordered intake; a single processor consumes it sequentially.
- **Advantages:** Explicit ordering + back-pressure; decouples arrival from processing; **is a natural
  realization of M1** (the queue provides admission/ordering, the single consumer provides the single
  writer).
- **Disadvantages:** If more than one consumer drains a room's queue, the single-writer guarantee is
  lost → must enforce exactly-one-consumer-per-room (which is M1's routing requirement).
- **Complexity:** Low–medium.
- **Determinism / Fairness / Recovery / Testing:** Same strengths as M1 when single-consumer.
- **Verdict:** Not a distinct model but the **recommended realization shape** of M1's Admission +
  Ordering stages; adopted as an implementation-neutral description, not a technology.

### M8 — Transactional Coordination (each intent an ACID transaction on a shared store)
- **Core idea:** Wrap each Intent in a transaction against a strongly-consistent store; the store
  serializes conflicting transactions.
- **Advantages:** Familiar; atomicity/consistency delegated to the store; good durability.
- **Disadvantages:** Relocates coordination into infrastructure on the **hot path** (a store round-trip
  + lock/transaction per guess), raising latency and making determinism/ordering dependent on store
  semantics; this is the A2 style already weighed and set aside in
  [ADR-001](ADR-001-overall-architecture-style.md#5-candidate-architectures); couples correctness to a
  critical external store.
- **Complexity / Operational risk:** Medium–high (the store becomes critical).
- **Determinism:** Depends on store isolation level; ordering not inherently deterministic.
- **Recoverability:** Strong (durable) — a strength.
- **Verdict:** Viable but heavier and slower on the critical path than M1; contradicts ADR-001's
  in-process authority. The *recovery* benefits inform ADR-005, not the coordination model.

---

## 5. Engineering Evaluation Matrix

Scores **5 = strongest, 1 = weakest**; for **Complexity** and **Operational Risk**, 5 = *lowest*
(best). M4 is folded into M2; M7 is M1's realization; both shown for completeness.

| Criterion | **M1 Serialized SW** | M2 Optimistic | M3 Lock-based | M5 Event-sourced | M6 Distributed | M8 Transactional |
|-----------|:--------------------:|:-------------:|:-------------:|:----------------:|:--------------:|:----------------:|
| Correctness | **5** | 3 | 4 | 5 | 4 | 4 |
| Determinism | **5** | 2 | 3 | 5 | 4 | 3 |
| Consistency (in-room) | **5** | 3 | 4 | 5 | 4 | 4 |
| Concurrency Safety | **5** | 3 | 4 | 5 | 4 | 4 |
| Recovery | 4 | 3 | 3 | **5** | 4 | 5 |
| Scalability | 4 | 4 | 3 | 4 | **5** | 4 |
| Maintainability | **5** | 3 | 3 | 3 | 2 | 3 |
| Complexity (5=lowest) | **5** | 3 | 3 | 3 | 1 | 2 |
| Operational Risk (5=lowest) | **5** | 3 | 2 | 3 | 1 | 2 |
| Debuggability | **5** | 2 | 3 | 4 | 2 | 3 |
| Testability | **5** | 2 | 3 | 4 | 2 | 3 |
| Observability | 4 | 3 | 3 | **5** | 3 | 4 |
| Future Distribution | 4 | 4 | 3 | 4 | **5** | 4 |
| Developer Productivity | **5** | 3 | 3 | 3 | 2 | 3 |
| **Fit for Cluely (room scope)** | **Highest** | Low–Med | Medium | Med (as recovery aid) | Premature | Med (heavier) |

**Reasoning highlights:**
- **M1** dominates the *dominant* criteria (correctness, determinism, consistency, concurrency safety,
  testability) because a single ordered writer makes them structural; it loses a point on Recovery vs
  M5/M8 (which externalize state) and on raw Scalability vs M6 — both acceptable given room-scale and
  the fact that M1 distributes by room.
- **M2/M4** suffer because the per-room hot path is **highly contended** (every guess touches one
  aggregate) → optimistic retries are the common case and ordering becomes nondeterministic without
  re-adding serialization.
- **M3** achieves exclusion but via a failure-prone shared primitive (deadlock/lock-loss), and lock
  ordering isn't deterministic.
- **M5/M8** have genuine **recovery** strengths that will inform **ADR-005**, but as *coordination
  models* they reduce to "single ordered writer + a persistence choice."
- **M6** is disproportionate for single-room coordination.

---

## 6. Trade-off Analysis

**What is gained (choosing M1):**
- Correctness, determinism, atomicity, and precedence become **structural guarantees**, not obligations
  on external infrastructure — the single ordered writer eliminates the entire class of intra-room race
  bugs.
- **Lowest complexity and highest testability/debuggability** — a room's behavior is a deterministic
  function of an ordered intent stream, ideal for property-based and replay testing.
- **Strongest fairness posture** — one adjudication point and (with ADR-006) one delivery boundary.

**What is sacrificed / deferred:**
- **Per-room throughput** is sequential. Accepted: rooms are tiny and human-paced; the sequential path
  per room is far below any human-perceivable limit, and parallelism *across* rooms is unbounded.
- **Owner recovery and room→owner routing** are new responsibilities (deferred to ADR-005/ADR-007). M1
  deliberately does not solve durability here.

**Operational implications:** MVP is one in-process authority per room (per ADR-001); operations are
simple. The main new operational duty is ensuring **exactly one active authority per room** (routing).

**Failure implications:** Owner loss mid-intent is the key failure; M1 pairs with **commit-then-broadcast**
so at most the *in-flight* intent needs idempotent re-processing on recovery; already-committed effects
are never replayed.

**Scaling implications:** Scale = more rooms across more owners; because rooms are isolated (CR-5), this
is horizontal by nature. No cross-room coordination is ever introduced.

**Recovery implications:** The last commit is the recovery point; M5/M8 techniques (event log / durable
transactions) remain available to ADR-005 *underneath* M1 without changing the model.

**Developer implications:** Developers reason about one ordered stream per room — the simplest possible
concurrency model — and never write cross-room locking.

**Testing implications:** Determinism enables exhaustive interleaving/property tests on the pure rules
core and replay-based recovery tests.

**Future migration implications:** Moving to distributed per-room owners (ADR-007) or an event-sourced
store (ADR-005) are **additive** changes around an unchanged coordination model and rules core.

---

## 7. Final Decision

**Adopt M1 — Serialized Single Writer per room**, realized conceptually as a per-room **ordered
admission (queue-shaped, M7) feeding exactly one Authority (the Room Entity)** that processes Intents
one at a time and commits atomically before broadcast.

**Why it best satisfies Cluely:** the product's value is fair, correct, deterministic play; M1 makes
those properties *structural* at the room scope, at the lowest complexity, with the best
testability/debuggability — directly serving the top architectural drivers
([02 Drivers](../02-architectural-drivers.md)) and the P1 engineering risks.

**Why the alternatives were rejected:**
- **M2/M4 (optimistic/CAS):** the per-room hot path is contended, so optimism degrades into retry
  storms and nondeterministic ordering; re-adding ordering just rebuilds M1 with more moving parts.
- **M3 (locks):** equivalent exclusion but via a failure-prone shared primitive with non-deterministic
  acquisition order; M1 gets the guarantee without a lock.
- **M5 (event-sourced) / M8 (transactional):** their strengths are *recovery/durability*, which belong
  to **ADR-005**; as coordination models they reduce to "single ordered writer + persistence." M1 is
  compatible with either underneath.
- **M6 (distributed consensus):** disproportionate and premature for single-room coordination.

**Why it aligns with ADR-001:** ADR-001 already mandates a single-writer Room Entity; M1 is the
coordination discipline that *realizes* that mandate — the two are consistent by design.

**Why it satisfies the drivers & protects invariants:** a single ordered writer directly enforces
[CB-01/02/03](../06-consistency-boundaries.md), the [precedence ladder](../../02-business-analysis/16-rule-precedence.md),
and invariants [INV-G2/G3](../../02-business-analysis/10-business-invariants.md) (one turn/clue),
[INV-O1/O2](../../02-business-analysis/10-business-invariants.md) (single, post-reveal outcome), and —
combined with the delivery boundary (ADR-006) — [INV-B9](../../02-business-analysis/10-business-invariants.md)
(no leak).

---

## 8. Coordination Lifecycle

The conceptual lifecycle of a single room Intent (no APIs, no code, no technology). Each stage is a
**responsibility handoff**, not a component.

```
Intent → Validation → Admission → Ordering → Adjudication → State Transition → Commit
        → Event Publication → Role Filtering → Delivery → Completion
```

| Stage | What happens | Guarantees / notes |
|-------|--------------|--------------------|
| **Intent** | A participant proposes an action (ADR-000 *Intent*). | Clients propose; the Authority decides. |
| **Validation** | Gate against role/team/phase/state; catalogued rejection on failure ([R-10](../03-system-responsibilities.md), [Validation Rules](../../02-business-analysis/09-validation-rules.md)). | Precedes any effect; deny-by-default; no mutation on reject. |
| **Admission** | The Intent enters the room's single ordered intake (M7 shape); non-admissible intents are rejected here (e.g., room closed). | One intake per room; back-pressure possible. |
| **Ordering** | The intake imposes a **deterministic order** on admitted Intents. | Establishes the single stream that makes determinism structural. |
| **Adjudication** | The single Authority (Rules Core within the Room Entity) resolves the Intent against the **latest committed** state, applying [precedence](../../02-business-analysis/16-rule-precedence.md). | Single writer; re-validates against current state (first-valid-wins). |
| **State Transition** | The resulting change is computed as a valid transition ([State Machines](../../02-business-analysis/07-state-machines.md)). | Only whitelisted transitions; multi-field change assembled as one unit. |
| **Commit** | The change becomes the new authoritative truth **atomically**. | Atomicity (CR-2); the recovery point (CR-8). |
| **Event Publication** | Domain events are emitted **after** commit. | Commit-then-broadcast (CR-12); business facts only. |
| **Role Filtering** | Per-role projections are produced at the delivery boundary (ADR-006). | Hidden information stripped for non-Spymasters (CR-6). |
| **Delivery** | Role-filtered state/events reach participants. | Delivery never mutates state (ADR-000 *Reader*). |
| **Completion** | The Intent is acknowledged as applied-once (or rejected). | At most one committed outcome (CR-3); idempotent on replay (CR-9). |

Only **Adjudication → Commit** touches authoritative state, and only the single Authority performs it.
Everything before is admission/validation; everything after is read-only propagation.

---

## 9. Failure Analysis

For each failure: **Expected behavior · Required guarantees · Recovery expectation · Business impact.**

| Failure | Expected behavior | Required guarantees | Recovery | Business impact if mishandled |
|---------|-------------------|---------------------|----------|-------------------------------|
| **Simultaneous guesses** | Ordered by Admission; first adjudicated, second re-validated against new state (rejected if card now revealed / turn ended). | Serialization; first-valid-wins ([RP-12](../../02-business-analysis/16-rule-precedence.md)). | N/A (no partial state). | Double-reveal, wrong counts (critical). |
| **Duplicate submission** | Idempotent — second is a no-op or catalogued rejection (bound to state/turn). | Replay safety (CR-9). | N/A. | Skipped turn / re-reveal. |
| **Reconnect during processing** | Reconnect is itself an ordered Intent; it cannot interleave a mutation; snapshot taken at a commit boundary. | Single-writer ordering; commit-consistent snapshot ([CB-08](../06-consistency-boundaries.md)). | Player resumes at last commit. | Leaked/partial view (critical if mishandled). |
| **Timeout (stalled adjudication)** | The in-flight Intent fails cleanly with no partial application; the stream is not corrupted. | Atomic commit (all-or-nothing). | Next Intent proceeds from last commit. | Half-applied state (critical). |
| **Network interruption (client)** | Intent may be lost pre-admission (never applied) or acknowledged post-commit; client resyncs to latest via delivery. | Commit-then-broadcast; idempotency. | Client re-reads current state. | Divergent client view. |
| **Player disconnect** | Presence system event; if essential to the active phase, coordination **pauses** admission of play Intents for that room ([BR-DC-3/4](../../02-business-analysis/02-business-rules.md)); else continues. | Pause decided against consistent state (CR-10). | Resume on reconnect within grace. | Unfair stall or premature loss. |
| **Host disconnect** | Control-only; coordination of match Intents is unaffected; host migration is a separate ordered Intent after grace ([RP-9](../../02-business-analysis/16-rule-precedence.md)). | Migration never mutates match state; exactly one host ([INV-R1](../../02-business-analysis/10-business-invariants.md)). | Deterministic successor. | Zero/two hosts; interrupted match. |
| **Late arrival** | An Intent that arrives after the state moved on is re-validated and typically rejected (stale). | Re-validation against latest committed state. | Client resyncs. | Acting on stale state. |
| **Replay (malicious or accidental)** | Rejected/no-op via idempotency + state/turn binding. | Replay safety; bound-to-state Intents. | N/A. | Unfair re-trigger. |
| **Partial failure (mid multi-field change)** | Nothing commits; state remains at last commit. | Atomicity. | Retry the Intent from last commit. | Corrupt aggregate. |
| **Unexpected interruption (owner loss)** | On recovery, restore last committed state; re-process only the *in-flight* Intent idempotently; never replay committed terminals. | Commit-then-broadcast + idempotent recovery (CR-8) — realization deferred to ADR-005. | Room resumes at last commit. | Lost/duplicated match, double result (critical). |

---

## 10. Concurrency Analysis

How M1 handles each concurrency case (all resolve because a room has one ordered writer):

| Case | Handling under M1 |
|------|-------------------|
| **Concurrent guesses** | Admitted to one ordered stream; adjudicated sequentially; each re-validated against latest state; first-valid-wins ([CB-01](../06-consistency-boundaries.md)). |
| **Concurrent joins** | Ordered; capacity + nickname checked atomically per admission; over-capacity/duplicate rejected ([CB-05](../06-consistency-boundaries.md)). |
| **Concurrent reconnects (same identity)** | Ordered; newest supersedes prior connection; single active connection enforced ([INV-P4](../../02-business-analysis/10-business-invariants.md)). |
| **Concurrent room updates (team/role/dictionary)** | Lobby-phase Intents ordered; one Spymaster enforced; setup lock at start rejects in-flight setup ([CB-06](../06-consistency-boundaries.md)). |
| **Concurrent state reads** | Reads are role-filtered projections of **committed** state; they never block or mutate; they observe only commit boundaries (never partial state). |
| **Concurrent deliveries** | Delivery is read-only fan-out of committed events, per-room ordered; it cannot interleave with a mutation because it runs after commit (CR-12). |
| **Concurrent cleanup/expiry** | Expiry is an ordered Intent/decision re-checked atomically at fire; result recorded before close; activity aborts expiry ([CB-10](../06-consistency-boundaries.md), [RP-11](../../02-business-analysis/16-rule-precedence.md)). |
| **Across rooms** | No coordination — rooms are isolated (CR-5); concurrency across rooms is unbounded and independent. |

---

## 11. Architectural Invariants (introduced/affirmed by this ADR)

These are **architectural** invariants (distinct from business invariants). They are objectively
checkable (see §16).

- **AI-COORD-1:** Exactly **one Authority coordinates a room** at any time.
- **AI-COORD-2:** Exactly **one writer** modifies a room's authoritative state (single writer).
- **AI-COORD-3:** **No state mutation bypasses coordination** (Validation → Admission → Adjudication →
  Commit is the only mutation path).
- **AI-COORD-4:** **Delivery never changes state** (readers only).
- **AI-COORD-5:** **Ordering within a room is deterministic**; outcomes obey the fixed precedence.
- **AI-COORD-6:** **Cross-room coordination is forbidden**; no shared mutable state between rooms.
- **AI-COORD-7:** **Every state-changing Intent produces at most one committed outcome** (idempotent).
- **AI-COORD-8:** **Commit precedes broadcast**; no partial state is ever observable.
- **AI-COORD-9:** **Every committed mutation yields a consistent, invariant-satisfying state** (atomic
  transition validated before/at commit).
- **AI-COORD-10:** **Every mutation is traceable** to the Intent that caused it and the events it
  produced.

---

## 12. Assumptions

Facts are stated as facts; beliefs are labeled assumptions with confidence and impact-if-false.

| # | Assumption | Confidence | If violated |
|---|-----------|-----------|-------------|
| AS-1 | Per-room workload is **low and human-paced** (≤ ~20 players, one board, sporadic actions), so sequential per-room processing is far below any perceivable latency limit. | High | If a single room needed very high throughput (not a Codenames trait), sequential processing could bottleneck → revisit ordering/parallelism *within* the room (unlikely). |
| AS-2 | **Rooms are independent** (no rule couples two rooms). *(Fact, from [INV/room isolation](../../02-business-analysis/10-business-invariants.md) and business scope — treated as fact, not assumption.)* | Fact | — |
| AS-3 | A room's authoritative state **fits in one owner's working set** for its lifetime. | High | If state grew huge (it cannot for a 25-card game), custody would need partitioning (not applicable). |
| AS-4 | It is acceptable for a room to be served by **one active owner at a time** (with recovery on loss). | High | If continuous availability during owner failover were required at zero interruption, a distributed model (M6/ADR-007) would be needed → revisit. |
| AS-5 | **Recovery to the last commit within the room lifetime** is a sufficient durability guarantee (no long-term persistence needed for MVP). | High | If long-term durability/audit became a requirement, ADR-005 would adopt event-sourced/transactional persistence *under* M1. |
| AS-6 | Ordering can be made **deterministic at admission** (a total order per room exists). | High | If truly simultaneous admission couldn't be totally ordered, a deterministic tie-break rule is required (already implied by first-valid-wins). |

---

## 13. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| Two active authorities for one room (routing/failover bug) → double writer | Architecture (critical) | Enforce exactly-one-active-owner via room→owner routing (ADR-007); fitness function AI-COORD-1/2; fence on ownership handoff. |
| In-flight Intent lost or double-applied on owner loss | Recovery | Commit-then-broadcast + idempotent re-processing of only the in-flight Intent (ADR-005); never replay committed terminals. |
| Per-room sequential path perceived as a bottleneck | Scalability | Rooms are tiny (AS-1); scale by room count; measure against [QS-08](../09-quality-attribute-scenarios.md); only revisit if data shows a real limit. |
| Delivery or another responsibility mutating state | Correctness/Fairness | AI-COORD-3/4 fitness functions; review against [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md). |
| Nondeterministic tie-break for truly simultaneous admission | Determinism | Define a deterministic admission tie-break (e.g., stable arrival sequence); property-test interleavings ([QS-02](../09-quality-attribute-scenarios.md)). |
| Coordination logic leaking into transport (ordering done client-side) | Correctness | Ordering is server-side at Admission only; clients never order outcomes ([AP-03](../../06-architecture-governance/01-architecture-principles.md)). |
| Pause/abandon decided on stale presence | Operational | Decide against committed state at the moment of the ordered decision (CR-10, [CB-08](../06-consistency-boundaries.md)). |
| Future distribution introduces cross-room coupling | Evolution | AI-COORD-6 is permanent; distribution keeps per-room ownership, never cross-room coordination. |

---

## 14. Validation Strategy

(How the decision is verified later — not implementation.)

- **Concurrency simulation / property-based testing:** generate many interleavings of admitted Intents;
  assert a single deterministic outcome per the [precedence ladder](../../02-business-analysis/16-rule-precedence.md) ([QS-02](../09-quality-attribute-scenarios.md)).
- **Determinism testing:** replay the same ordered Intent stream repeatedly → identical committed state
  and events (validates AI-COORD-5).
- **Stress testing:** high per-room Intent rates to confirm sequential processing stays within
  [QS-08](../09-quality-attribute-scenarios.md) latency targets and no state corruption occurs.
- **Recovery testing / chaos testing:** kill the owner mid-Intent; confirm restore to last commit, at
  most-once re-application of the in-flight Intent, and no replayed terminal ([QS-06](../09-quality-attribute-scenarios.md)).
- **Security testing:** confirm no hidden information leaks during concurrent processing/delivery
  ([QS-01](../09-quality-attribute-scenarios.md)).
- **Formal reasoning:** argue single-writer serial execution ⇒ linearizable per-room history ⇒ invariants
  preserved (a lightweight model/argument, not code).
- **Architecture review & fitness functions (§16):** verify AI-COORD-1…10 against
  [Review Checklist](../../06-architecture-governance/05-architecture-review-checklist.md) and
  [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md).

---

## 15. Impact on Future ADRs

| ADR / Phase | How ADR-003 constrains it |
|-------------|---------------------------|
| **ADR-002 Authoritative Game State** | The state model is the aggregate mutated by the single writer at Commit; must be shaped for atomic transition and a clean recovery point. |
| **ADR-004 Real-Time Communication** | Must respect commit-then-broadcast and per-room ordered delivery; delivery is read-only; clients never order outcomes. |
| **ADR-005 State Recovery** | Recovery point = last commit; may realize custody via event-sourced (M5) or transactional (M8) means **under** M1; must re-apply only the in-flight Intent idempotently. |
| **ADR-006 Role-Based Visibility** | Role filtering occurs post-commit at the delivery boundary; must not require reading uncommitted/partial state. |
| **ADR-007 Room Isolation / Distribution** | Must guarantee exactly-one-active-authority per room during routing/failover; preserve AI-COORD-1/2/6; distribution keeps per-room single ownership. |
| **ADR-008 Dictionary Architecture** | Board generation runs within the coordinated Start Intent; dictionary provision remains a read-only input (no coordination influence). |
| **ADR-009 Session & Reconnection** | Reconnect/join/leave are ordered Intents; single-active-connection enforced by coordination order. |
| **ADR-010 Command/Query Strategy** | Commands enter the single ordered writer; queries read committed projections; any read-model must be built from committed events, never bypass coordination. |
| **Software Architecture / Technical Design** | The room's mutation path is fixed; components map to lifecycle stages (§8); the aggregate is the consistency unit. |
| **Implementation** | Must not introduce a second writer, cross-room lock, or client-side ordering; all mutations go through the pipeline. |
| **Testing** | Determinism/replay/recovery test strategy is set by §14; fitness functions §16 are mandatory checks. |
| **Operations** | Must monitor one-active-owner-per-room and recovery behavior; per-room ordering is the mental model for incident analysis. |

---

## 16. Architecture Fitness Functions

Objectively verifiable rules derived from the invariants (§11):

- **FF-1:** For every room at any instant, **exactly one active Authority** exists (no two owners).
- **FF-2:** **No mutation path exists that bypasses** Validation → Admission → Adjudication → Commit.
- **FF-3:** **No delivery/read component ever writes** authoritative state.
- **FF-4:** **No cross-room synchronization or shared mutable state** exists.
- **FF-5:** Under any interleaving of concurrent Intents, the committed history is **equivalent to some
  serial order** consistent with the precedence ladder (linearizable per room).
- **FF-6:** Every state-changing Intent yields **at most one committed outcome** (idempotent under
  replay/retry).
- **FF-7:** **No hidden information** appears in any non-Spymaster projection during or after concurrent
  processing.
- **FF-8:** Every broadcast event corresponds to an **already-committed** state (no pre-commit
  emission).

Each maps to a validation method in §14 and to
[Success Metrics ASM-05/06/07](../../06-architecture-governance/04-architecture-success-metrics.md).

---

## 17. Decision Confidence

**Confidence: High.**

Rationale: the coordination model is *entailed* by frozen upstream decisions — ADR-001 already mandates
a single-writer Room Entity, the business requires deterministic precedence, and the workload is
room-scale and human-paced. M1 is the minimal model that satisfies all coordination requirements
(§3) and every serious alternative either reduces to M1 with extra machinery (M3/M5/M7/M8) or is
ill-suited to a contended single aggregate (M2/M4) or premature (M6).

**Conditions that would require revisiting this ADR:**
- **Massive per-room concurrency increase** (a future mode with very high per-room action rates) that
  a sequential path cannot serve within latency targets.
- **Zero-interruption availability** requirement during owner failover (would push toward distributed
  co-ownership / consensus, M6, via ADR-007).
- **Multi-region / cross-region coordination** of a single room.
- **Persistent matchmaking or replay infrastructure** that changes durability/audit requirements enough
  to reshape custody (would still likely keep M1 with an event-sourced store under it — ADR-005).
- **Cross-room interactions** (a business change) — currently forbidden; would violate AI-COORD-6 and
  require reopening at the business level first.

**Review trigger:** revisit at the earliest of — a validated per-room latency breach under stress
([QS-08](../09-quality-attribute-scenarios.md)), a new availability SLA requiring hot failover, or any
roadmap phase introducing cross-room/multi-region behavior.

---

## 18. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | Guess/clue/turn/win/loss/host: [BR-GV/CG/IG/NC/OPP/ASN, BR-CL, BR-TO/TE, BR-WIN/LOSE/ASN, BR-HM, BR-DC](../../02-business-analysis/02-business-rules.md). |
| **Business Invariants** | [INV-G2/G3](../../02-business-analysis/10-business-invariants.md) (one turn/clue), [INV-O1/O2](../../02-business-analysis/10-business-invariants.md) (single/post-reveal outcome), [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-R1](../../02-business-analysis/10-business-invariants.md) (one host), [INV-P4](../../02-business-analysis/10-business-invariants.md) (one connection). |
| **Rule Precedence** | [16 Rule Precedence](../../02-business-analysis/16-rule-precedence.md) (the order the single writer enforces). |
| **Engineering Challenges** | [ENG-GP-01/05](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), ENG-ST-01/02, ENG-CO-01/02/04, ENG-FP-03, ENG-RE-01/02; priorities [Enrichment §5](../../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary). |
| **Architectural Drivers** | [02 Drivers](../02-architectural-drivers.md): fairness, correctness, determinism, consistency, recoverability, isolation. |
| **Responsibility Boundaries** | [04 Boundaries](../04-responsibility-boundaries.md): C1 Rules & Play; delivery≠adjudication; room≠room. |
| **State Ownership** | [05 State Ownership](../05-state-ownership.md): single writer; S-01…S-04 owned by Rules & Play. |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md): CB-01/02/03/05/06/08/10. |
| **Quality Attribute Scenarios** | [09 QS](../09-quality-attribute-scenarios.md): QS-01/02/03/06/08. |
| **Interaction Discovery** | [08 Interactions](../08-interaction-discovery.md): I-01 (guess), I-08 (commit-then-broadcast), I-09 (recovery). |
| **ADR-000** | Uses *Authority, Room Entity, Intent, Command, Serialization, Single Writer, Commit, Projection, Role Filtering, Atomicity, Determinism, Room Isolation* as defined. |
| **ADR-001** | Realizes the single-writer Room Entity mandate; consistent with the modular-monolith, evolvable style. |
| **Governance** | Principles [AP-03/04/05/06/07/08/09/18](../../06-architecture-governance/01-architecture-principles.md); Anti-principles [AAP-02/04/05/08/09/12/14](../../06-architecture-governance/02-architecture-anti-principles.md); [Heuristics](../../06-architecture-governance/03-architecture-decision-heuristics.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## Architecture Review Verdict

- **Coordination model:** **APPROVED** — *Serialized Single Writer per room* (M1), realized as per-room
  ordered admission feeding exactly one Authority, with commit-then-broadcast and no bypass.
- **Alignment:** Consistent with ADR-001 and ADR-000; preserves all business rules, invariants, and
  precedence; satisfies the coordination requirements (§3) and the non-waivable fairness/determinism
  scenarios ([QS-01/02/03](../09-quality-attribute-scenarios.md)).
- **Remaining architectural risks:** (1) guaranteeing **exactly one active Authority per room** across
  routing/failover — owned by **ADR-007**; (2) **in-flight-Intent recovery** semantics — owned by
  **ADR-005**. Both are tracked in the [Architecture Risk Register](../../06-architecture-governance/07-architecture-risk-register.md)
  and constrained (not solved) here.
- **Assumptions requiring future validation:** AS-1 (per-room load stays human-paced) and AS-4
  (one-active-owner-with-recovery is acceptable) — validate via stress and recovery testing (§14).
- **Readiness to proceed to ADR-002 (Authoritative Game State):** **READY.** ADR-003 fixes *how* a
  room's work is coordinated; ADR-002 can now define *what* state that single writer owns and mutates,
  shaped for atomic transition and clean recovery, within this model.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: Serialized Single Writer per-room coordination (M1); alternatives evaluated; fitness functions and verdict recorded. |
