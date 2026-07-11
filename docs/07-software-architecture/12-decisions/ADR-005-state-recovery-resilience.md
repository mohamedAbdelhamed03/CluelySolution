# ADR-005 — State Recovery & Resilience Architecture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How does Cluely recover authoritative room state after interruption while preserving correctness, determinism, fairness, and hidden-information integrity?* |
| **Complies with** | [ADR-000](ADR-000-architecture-vocabulary.md), [ADR-001](ADR-001-overall-architecture-style.md), [ADR-002](ADR-002-authoritative-game-state.md), [ADR-003](ADR-003-per-room-coordination-model.md), [ADR-004](ADR-004-real-time-communication-delivery.md), [ADR-006](ADR-006-role-based-information-visibility.md), [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md). Builds upon; redefines none. |
| **Scope note** | Defines the **recovery architecture** only. It chooses **no** database, storage engine, snapshot/event-store implementation, Redis/SQL, file system, cloud storage, replication, infrastructure, framework, backup, or disaster-recovery product ([§19 Non-Goals](#19-non-goals)). |

---

## 1. Executive Summary

Cluely recovers by **restoring each room's authoritative aggregate to its last committed state**, then
**deterministically recomputing everything derived** (projections, presence display, derived scores) and
**re-binding participants** — before any delivery, participation action, gameplay, or new command is
allowed. The **recovery unit is the room** ([ADR-002](ADR-002-authoritative-game-state.md)); rooms recover
**independently** (isolation). The **Authority recovers**: a room's single Authority (Room Entity) is
re-established as the sole owner/writer, and it restores state from a **durable record of committed
state** (a checkpoint and/or the ordered committed-event history) — the *record* is deferred to
technical design; the *architectural model* is fixed here.

The chosen model is **Authoritative Checkpoint Recovery with bounded committed-history catch-up** (a
hybrid of *authoritative snapshot* + *committed-event replay*): restore the latest **committed
checkpoint version**, apply any **committed** events after it, and **stop at the last commit** —
**never** re-adjudicate, **never** replay the single **in-flight (uncommitted) Intent** as if committed
(it is either idempotently re-processed once as a *new* admission or dropped), and **never** resurrect a
finished match. Because [ADR-003](ADR-003-per-room-coordination-model.md) is **commit-then-broadcast**,
every version any participant ever saw corresponds to a committed checkpoint — so recovery can always
land on a state that is **consistent with what was already observed**.

**Recovery is an architectural concern** (not merely operational) because *what* is restored, *in what
order*, and *what guarantees hold during restoration* determine whether fairness, determinism, and
hidden-information integrity survive a failure. **Recovery differs from persistence**: persistence is
*where/how* committed state is durably recorded (technical design); recovery is *the model by which the
Authority is re-established and truth is restored*. **Correctness beats speed**: a fast recovery that
changed an outcome, duplicated a participant, or leaked the key would be worthless — so gameplay,
delivery, and commands stay **suspended until recovery completes and validates**.

> One-line statement: **restore the room aggregate to its last committed version, recompute all derived
> data, re-bind participants, validate — then, and only then, resume delivery and gameplay; never
> re-adjudicate, never resurrect finished matches, never leak.**

---

## 2. Problem Statement

**Why failures are inevitable.** Processes crash, nodes restart, power fails, networks partition.
[ADR-001](ADR-001-overall-architecture-style.md) keeps a room's authoritative state in the Authority's
working set for the room's lifetime; any interruption risks that state. Resilience must be designed, not
hoped for ([ENG-RE-01/02](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md),
[QS-06](../09-quality-attribute-scenarios.md)).

**Why multiplayer recovery is difficult.** Many participants hold *views* of a room that they saw at some
version; recovery must land on a state **consistent with what was already delivered** (never "un-reveal"
a card or change a shown turn), while restoring the single Authority and re-binding participants — all
without races or duplicates.

**Why recovery must preserve fairness.** Recovery must not change **who won**, **whose turn it was**,
**which cards were revealed**, or **who the Spymaster is** — otherwise the outcome becomes arbitrary
([INV-O1/O4](../../02-business-analysis/10-business-invariants.md), [Rule Precedence](../../02-business-analysis/16-rule-precedence.md)).

**Why recovery must preserve hidden information.** The key must remain secret through and after recovery;
projections are **recomputed** and re-filtered ([ADR-006](ADR-006-role-based-information-visibility.md)),
never restored from a stale participant-facing view ([INV-B9](../../02-business-analysis/10-business-invariants.md)).

**Why deterministic recovery matters.** Given the same durable record, recovery must always produce the
**same** restored state and the **same** derived projections — otherwise recovery itself becomes a source
of nondeterminism, defeating [AP-06](../../06-architecture-governance/01-architecture-principles.md).

---

## 3. Recovery Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Authority Recovers** | Recovery re-establishes the room's **single Authority** as the sole owner/writer, then restores state. | [ADR-001/003](ADR-003-per-room-coordination-model.md); one writer. |
| **Recovery Restores Truth** | Recovery reproduces the **last committed authoritative state**, nothing more. | Single source of truth ([ADR-002](ADR-002-authoritative-game-state.md)). |
| **Recovery Never Invents State** | It restores what was committed; it never fabricates cards, turns, results, or participants. | Correctness/fairness. |
| **Recovery Never Replays Business Decisions** | Committed outcomes are **restored**, not **re-adjudicated**; the rules core does not re-decide the past. | Determinism; no double effects. |
| **Recovery Preserves Versions** | The restored state carries its **committed per-room version**; recovery never rewinds below or invents above what was delivered. | [ADR-004 versioning](ADR-004-real-time-communication-delivery.md#7-versioning-strategy). |
| **Recovery Preserves Identity** | Participation records restore as **the same** participants (one each), re-bindable by continuity token. | [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md). |
| **Recovery Is Deterministic** | Same durable record ⇒ same restored state ⇒ same projections. | Reproducibility/testing. |
| **Recovery Is Idempotent** | Re-running recovery yields the same result; a re-applied committed event has no extra effect. | Safe retries. |
| **Recovery Is Verifiable** | Recovery **validates** invariants before completing (§10). | Trust; abort on corruption. |
| **Recovery Before Delivery** | No projection is delivered until recovery completes and validates. | No stale/partial exposure. |
| **Recovery Before Participation** | No participant re-binds/acts until recovery completes. | No action on partial truth. |
| **Recovery Before Gameplay** | No adjudication resumes until recovery completes. | Correctness over speed. |
| **Recovery Before New Commands** | No new Intent is admitted until recovery completes. | ADR-003 admission gated. |

---

## 4. Candidate Recovery Models

Evaluated for the per-room, in-Authority-working-set model of ADR-001, with room-lifetime durability.
None dismissed without reasoning.

### RM1 — Cold Restart (no recovery; lose in-progress rooms)
- **Overview:** On failure, discard live rooms; players start over.
- **Correctness:** Trivially "correct" (nothing restored) but **fails resilience** ([QS-06](../09-quality-attribute-scenarios.md)) — in-progress matches are lost.
- **Complexity/Latency:** Lowest/instant.
- **Verdict:** **Rejected** as the model — violates the room-lifetime recoverability expectation
  ([NFR-11](../../02-business-analysis/01-software-requirements.md#29-non-functional-requirements)). (Acceptable only as a last-resort fallback when a room is unrecoverable/corrupt — §10.)

### RM2 — Stateless Restart (rebuild state from clients)
- **Overview:** Trust clients to resubmit their view to rebuild room state.
- **Correctness/Fairness:** **Fails** — clients are untrusted ([AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md)); they could inject a false board/key → cheating; contradicts server authority.
- **Verdict:** **Disqualified.**

### RM3 — Authoritative Snapshot Recovery (restore latest committed snapshot)
- **Overview:** Periodically/at-commit capture an authoritative snapshot; on failure restore the latest.
- **Advantages:** Simple, fast restore; deterministic; server-owned truth.
- **Disadvantages:** If snapshots are not taken **every** commit, the tail (commits after the last
  snapshot) is lost unless combined with committed-event catch-up (→ RM7).
- **Correctness/Determinism:** High (restores committed truth).
- **Verdict:** **Selected component** — the base; completed by RM4/RM7 for the tail.

### RM4 — Event Replay (replay committed events onto a base)
- **Overview:** Fold the ordered **committed** events onto a base (empty or a snapshot) to reach the last committed state.
- **Advantages:** Exact tail recovery; natural per-room ordering (ADR-003/004 versions); auditable.
- **Disadvantages:** Full-from-empty replay can be costly if history is long (bounded here: a single
  short match); must fold **committed** events only and **stop at the last commit**.
- **Correctness/Determinism:** High (deterministic fold).
- **Verdict:** **Selected component** — used for **bounded catch-up** from the latest snapshot to the last
  committed version. **Not** a re-adjudication (folding recorded outcomes ≠ re-deciding them).

### RM5 — Hybrid Recovery (snapshot + committed-event catch-up) = **Checkpoint Recovery**
- **Overview:** Restore the latest **checkpoint** (snapshot at a committed version), then **fold committed
  events** after it up to the last committed version.
- **Advantages:** Fast (snapshot) **and** exact (tail catch-up); bounded work (short matches); deterministic;
  idempotent.
- **Verdict:** **This is the recommended model** (a.k.a. Checkpoint Recovery, RM6).

### RM6 — Checkpoint Recovery
- Same as RM5 by another name; "checkpoint" = a snapshot bound to a committed version. **Adopted.**

### RM7 — Incremental Recovery
- **Overview:** Recover only what changed since a known-good point (delta).
- **Advantages:** Efficient for large state.
- **Disadvantages:** Room state is **small and bounded** (25 cards, ≤ ~20 players) — incremental machinery
  is unnecessary complexity ([AP-12](../../06-architecture-governance/01-architecture-principles.md)).
- **Verdict:** Folded into RM5's catch-up (the "increment" is the committed-event tail); not a separate model.

### RM8 — Lazy Recovery (restore on first access)
- **Overview:** Defer a room's recovery until someone interacts with it.
- **Advantages:** Spreads load; avoids recovering dead rooms.
- **Disadvantages:** Complicates "recovery-before-everything" gating; risks serving/deciding before
  recovery; acceptable **only** if the first access **triggers and awaits** full recovery before any
  effect.
- **Verdict:** Permissible as an **optimization** of *when* RM5 runs (on demand), provided the
  recovery-before-delivery/participation/gameplay/commands invariants hold. Not a different model.

### RM9 — Warm Recovery (standby holds recent state)
- **Overview:** A standby continuously holds recent committed state for fast takeover.
- **Advantages:** Low recovery latency; good for future HA.
- **Disadvantages:** Introduces replication/standby infrastructure (technology — out of scope) and
  split-brain risk if two Authorities could both act.
- **Verdict:** A **future** availability enhancement compatible with RM5 (the standby restores the same
  checkpoint+tail); **not** required for MVP; must preserve single-Authority ([§15 split-brain](#15-security-analysis)).

### Evaluation summary

| Criterion | RM1 Cold | RM2 Stateless | RM3 Snapshot | RM4 Replay | **RM5/6 Checkpoint (chosen)** | RM9 Warm |
|-----------|:--------:|:-------------:|:------------:|:----------:|:-----------------------------:|:--------:|
| Correctness | 2 | 1 | 4 | 5 | **5** | 5 |
| Recoverability | 1 | 1 | 4 | 5 | **5** | 5 |
| Complexity (5=lowest) | 5 | 4 | 4 | 3 | **3** | 2 |
| Latency (restore) | 5 | 4 | 5 | 3 | **4** | 5 |
| Determinism | 5 | 1 | 5 | 5 | **5** | 5 |
| Maintainability | 5 | 2 | 4 | 4 | **4** | 3 |
| Operational (5=lowest) | 5 | 4 | 4 | 3 | **3** | 2 |
| Testing | 5 | 2 | 4 | 4 | **5** | 3 |
| Future scalability | 3 | 2 | 4 | 4 | **5** | 5 |
| Future distribution | 2 | 1 | 4 | 4 | **5** | 5 |
| Future persistence | 2 | 1 | 4 | 5 | **5** | 4 |

---

## 5. Final Recovery Model

**Adopt RM5/RM6 — Authoritative Checkpoint Recovery:** restore the latest **committed checkpoint** (a
snapshot bound to a committed per-room version) for the room, **fold the committed-event tail** up to the
**last committed version**, **recompute all derived data and projections**, **re-bind participants**, and
**validate** — before resuming delivery, participation, gameplay, or command admission. Recovery may run
eagerly or **lazily on first access** (RM8-as-optimization) provided the gating invariants hold; a
**warm standby** (RM9) is a compatible future availability enhancement.

- **Why it fits Cluely:** rooms are small/bounded and short-lived, so checkpoint + short tail is fast and
  exact; recovery is deterministic and idempotent; correctness is never traded for speed.
- **Why it aligns with every prior ADR:**
  - [ADR-001](ADR-001-overall-architecture-style.md): the Authority is re-established as the single owner.
  - [ADR-002](ADR-002-authoritative-game-state.md): the **room aggregate** is the recovery unit; projections are recomputed, never restored as truth.
  - [ADR-003](ADR-003-per-room-coordination-model.md): commit-then-broadcast means every observed version is a committed checkpoint; the in-flight Intent is handled per its rules (re-admitted once or dropped), never replayed as committed.
  - [ADR-004](ADR-004-real-time-communication-delivery.md): after recovery, clients **re-snapshot** at the recovered version; delivery never replays terminals.
  - [ADR-006](ADR-006-role-based-information-visibility.md): projections are re-filtered from recovered state — the key never appears in a non-Spymaster projection.
  - [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md): participation records restore as one-each and re-bind by continuity token; presence is re-established.
- **Preserves business invariants, participant continuity, visibility, and committed versions** by
  construction (§13).

---

## 6. Recovery Unit

The **recovery unit is the Room aggregate** ([ADR-002](ADR-002-authoritative-game-state.md#12-recovery-model));
rooms recover independently. Ownership of every item is the room **Authority** unless noted.

| Item | Recovered / Reconstructed / Recomputed / Discarded | Notes | Owner |
|------|----------------------------------------------------|-------|-------|
| **Room Aggregate** | **Recovered** (to last committed version) | The unit; identity/code, lifecycle, membership, host, dictionary pin. | Authority |
| **Participation Records** | **Recovered** (one per participant) | Identity ref, role/team, token validity; re-bound on reconnect ([ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)). | Authority |
| **Current Match / Game State** | **Recovered** (if InProgress) or terminal preserved | A **Finished** match stays finished ([INV-G7](../../02-business-analysis/10-business-invariants.md)). | Authority |
| **Board (words, key, reveal flags, starting team)** | **Recovered** (immutable parts as-is; reveal flags as committed) | Key restored server-side only. | Authority |
| **Roles / Teams** | **Recovered** (frozen mid-match) | Same Spymaster/Operative as committed ([INV-T5](../../02-business-analysis/10-business-invariants.md)). | Authority |
| **Version (per-room)** | **Recovered / Preserved** | The restored state carries its committed version. | Authority |
| **Result** | **Recovered** (immutable if written) | Never changed ([INV-O4](../../02-business-analysis/10-business-invariants.md)). | Authority |
| **Presence** | **Recomputed / Re-established** | Reconstructed from reconnections; not restored as stale truth. | Authority |
| **Host** | **Recovered** (designation) | Exactly one ([INV-R1](../../02-business-analysis/10-business-invariants.md)); migration only if the host is gone post-recovery. | Authority |
| **Administrative Metadata (pending actions)** | **Recovered** (committed ones) | Uncommitted admin actions are not resurrected. | Authority |
| **Derived data (remaining-agents, "win now?")** | **Recomputed** | From recovered board/turn ([ADR-002 §7](ADR-002-authoritative-game-state.md#7-state-classification)). | Authority (computed) |
| **Projections / Projection Cache** | **Recomputed / Discarded** | Never restored as truth; re-derived by ADR-006. | Delivery boundary (derived) |
| **Delivery State (subscriptions, in-flight artifacts)** | **Discarded / Re-established** | Clients re-snapshot; delivery holds no truth ([ADR-004](ADR-004-real-time-communication-delivery.md)). | Delivery boundary |
| **Connections** | **Discarded** (transient) | Re-established on reconnect; superseded singletons. | Connection mgmt |
| **In-flight (uncommitted) Intent** | **Not restored as committed** | Either idempotently re-admitted **once** as a new Intent or dropped; never applied twice. | Authority |

**What is NOT recovered:** transient transport/connection state, uncommitted work, projection caches, and
any client-side/UI state. **What is reconstructed/recomputed:** presence and all derived data/projections.
**What is discarded:** connections, delivery buffers, uncommitted Intents.

---

## 7. Failure Domains

| Domain | Failure impact | Recovery owner | Recovery strategy | Isolation guarantee |
|--------|----------------|----------------|-------------------|---------------------|
| **Participant** | One participant's view/connection lost. | Connection/Participation ([ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)) | Reconnect + re-snapshot; grace lease. | Affects only that participant. |
| **Connection** | Transport pipe lost. | Delivery ([ADR-004](ADR-004-real-time-communication-delivery.md)) | Client resync/snapshot; supersession. | No state impact (connections own no state). |
| **Room** | One room's Authority working set at risk. | The room's **Authority** | RM5 checkpoint recovery of that room aggregate. | Other rooms unaffected (isolation). |
| **Authority (Room Entity)** | The single writer for a room is interrupted. | Re-established Authority | Re-establish single owner, then RM5 restore. | Per-room; single owner guaranteed. |
| **Process** | All rooms on the process at risk. | Each room's Authority (on restart) | Per-room RM5 recovery (eager or lazy). | Rooms recover independently. |
| **Node** | All rooms on a node at risk. | Per-room Authority (post-restart/relocation) | Per-room RM5; routing re-establishes single owner (composes with ADR-007). | Per-room isolation preserved. |
| **Application** | Whole service interruption. | Per-room recovery on restart | RM5 per room; clients re-snapshot. | Rooms independent. |
| **Future Cluster** | Node/partition failures across a cluster. | Per-room owner + routing/HA (ADR-007/RM9) | Relocate room ownership; RM5 restore; single-owner enforcement. | Room isolation + single-owner invariant. |

**Key isolation guarantee:** recovery is **room-scoped**; recovering one room **never** reads, writes, or
affects another (AI-REC-8/§13).

---

## 8. Recovery Lifecycle

Technology-neutral; **all effects gated** until completion/validation.

```
Failure Detection → Recovery Start (single-owner re-established, room quiesced)
   → State Validation (durable record integrity) → State Restoration (checkpoint + committed tail)
   → State Verification (invariants hold; version consistent) → Projection Regeneration (ADR-006)
   → Participant Rebinding (ADR-009; presence re-established) → Snapshot Synchronization (clients re-snapshot, ADR-004)
   → Resume Delivery → Resume Gameplay (command admission re-opened, ADR-003)
```

| Stage | What happens |
|-------|--------------|
| **Failure Detection** | The interruption is detected; the affected room(s) are marked *Recovering*. |
| **Recovery Start** | Exactly **one** Authority is (re-)established for the room; the room is **quiesced** (no admission, no delivery). |
| **State Validation** | The durable record (checkpoint + committed tail) is checked for integrity/consistency (version continuity). |
| **State Restoration** | Restore the latest committed checkpoint; **fold committed events** to the last committed version (RM5). |
| **State Verification** | Assert business + architectural invariants on the restored aggregate (§10/§13). |
| **Projection Regeneration** | Recompute derived data and role-filtered projections (ADR-006) — never restore stale ones. |
| **Participant Rebinding** | Restore participation records (one each); re-establish presence; allow token re-binding (ADR-009). |
| **Snapshot Synchronization** | Clients receive a fresh role snapshot at the recovered version (ADR-004). |
| **Resume Delivery** | Delivery re-opens for the room. |
| **Resume Gameplay** | Command admission (ADR-003) re-opens; play continues from the restored state. |

Ordering is strict: **restore → verify → project → rebind → sync → deliver → play.** Nothing later begins
before the earlier stage completes.

---

## 9. Recovery Consistency

| Aspect | Guarantee |
|--------|-----------|
| **Consistency during recovery** | The room is **quiesced** — no partial state is delivered or acted upon while restoring. |
| **Version preservation** | The restored state carries its **committed per-room version**; recovery never rewinds below a delivered version nor invents a higher one ([ADR-004 §7](ADR-004-real-time-communication-delivery.md#7-versioning-strategy)). |
| **Recovery ordering** | Committed events are folded **in version order** (deterministic); the strict lifecycle ordering (§8) holds. |
| **Recovery atomicity** | Recovery **completes or aborts**; a room is either fully recovered-and-validated or not resumed (no half-recovered play). |
| **Visibility consistency** | Projections are **recomputed** and re-filtered; a non-Spymaster never receives the key through recovery (ADR-006). |
| **Participation consistency** | One participation record per participant; roles/teams as committed; re-bind by token (ADR-009). |
| **Delivery consistency** | Clients **re-snapshot** at the recovered version; duplicate/late artifacts remain harmless (ADR-004 apply-newest). |
| **Command admission** | **Closed** during recovery; **re-opened** only after completion — no Intent is adjudicated against partial state. |
| **Recovery completion** | Declared only after **verification passes**; only then do delivery/participation/gameplay/commands resume. |

**Consistency-with-observers property:** because commit-then-broadcast means every delivered version is a
committed checkpoint, the recovered state is always **≥** and **consistent with** any version a
participant already saw — recovery never contradicts observed history (no "un-reveal", no changed turn).

---

## 10. Recovery Validation

- **How does the architecture know recovery succeeded?** The restored aggregate **passes verification**: all
  **business invariants** ([INV-*](../../02-business-analysis/10-business-invariants.md)) and **architectural
  invariants** (§13) hold; the version is consistent (monotonic, not below a delivered version); board
  composition (9/8/7/1), one Spymaster/team, one host, one participation-per-participant, and result
  immutability all check out.
- **What conditions must hold?** Single Authority established; durable record integrity verified; restored
  version ≥ last-delivered; derived data recomputes consistently (e.g., remaining-agents == count of
  unrevealed team agents); projections reproduce without the key for non-Spymasters.
- **What invalidates recovery?** Failed integrity of the durable record; invariant violation on the
  restored state; version inconsistency (e.g., a version below what participants already saw);
  irreconcilable/corrupt state.
- **When must recovery abort?** When verification **fails** and the state **cannot** be reconciled to a
  valid committed version — recovery **must not** resume play on an invalid or invented state.
- **What is the fallback?** **Graceful termination** of the affected room: record the situation
  (abandonment, [BR-RX-3/BR-GE-5](../../02-business-analysis/02-business-rules.md)), notify participants,
  and **do not resume** — i.e., a controlled **Cold-Restart of that room only** (RM1 as last resort),
  never a silent, incorrect recovery. Other rooms are unaffected.

> **Correctness over availability:** aborting to a clean, honest failure is preferred to resuming on a
> state that could change outcomes or leak information.

---

## 11. Recovery Failure Analysis

**Expected · Guarantee · Business impact · Residual risk · Mitigation** for each.

| Case | Expected | Guarantee | Business impact | Residual risk | Mitigation |
|------|----------|-----------|-----------------|---------------|------------|
| **Server crash** | Per-room RM5 recovery on restart; clients re-snapshot. | Rooms restore to last committed version; no re-adjudication. | Temporary interruption. | Depends on durable-record freshness. | Frequent checkpoints/committed-tail (technical design). |
| **Room crash** | That room recovers (RM5); others unaffected. | Room-scoped; isolation. | One room paused briefly. | — | — |
| **Application restart** | All rooms recover per-room (eager/lazy). | Independent per-room recovery. | Brief global interruption. | Recovery storms. | Lazy/on-access recovery; bounded per-room cost. |
| **Authority interruption** | Re-establish single Authority, then restore. | One writer; no dual authority. | Pause until restored. | Two owners if routing buggy. | Single-owner enforcement (ADR-007); fencing. |
| **Power loss** | Same as server crash. | Restore to last committed version. | Interruption. | Loss of un-persisted tail. | Commit durability (technical design). |
| **Network partition** | Far-side clients lag/re-snapshot on rejoin; single Authority still owns the room (no second writer). | No split-truth (single owner). | Temporary staleness for partitioned clients. | Split-brain **if** two owners emerged. | Single-owner invariant + fencing (ADR-007). |
| **Mass disconnect** | Room retained through grace ([ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)); state intact; participants re-bind. | Participation survives; state unchanged. | Possible abandonment if none return. | — | Grace leases; abandonment rules. |
| **Corrupted state (durable record)** | Verification fails → **abort** → graceful room termination. | Never resume on invalid state (§10). | That room ends (recorded). | Data loss for that room. | Redundant/validated records (technical design). |
| **Partial recovery** | Not completed → **not resumed** (atomic completion). | No half-recovered play. | Delay or termination. | — | Atomicity + verification gate. |
| **Duplicate recovery** | Second recovery attempt is idempotent/serialized; only one proceeds. | No two recoveries for a room at once (AI-REC-10). | None. | Concurrent recovery bug. | Recovery is a single-owner, serialized operation. |
| **Interrupted recovery** | Re-run from the durable record; idempotent. | Same result; no double effects. | Extra delay. | Repeated failures. | Idempotent recovery; bounded retries → fallback. |
| **Recovery timeout** | If recovery cannot complete in a bounded time, **abort → fallback** (graceful termination). | No indefinite limbo. | That room ends. | — | Timeout → controlled fallback (§10). |
| **Future distributed failure** | Per-room owner relocated; RM5 restore; single-owner enforced. | Room isolation + single owner. | Brief per-room pause. | Cluster split-brain. | ADR-007 ownership/fencing; optional warm standby (RM9). |

---

## 12. Graceful Degradation

- **What may temporarily disappear (while recovering a room):** the ability to **submit clues/guesses**,
  **receive live updates**, **join**, or **act** in that room — all **suspended** until recovery completes.
- **What must never disappear:** **correctness, fairness, and hidden-information integrity** — these are
  **never** relaxed to speed recovery; a card's ownership never leaks, an outcome never changes.
- **What happens while recovery runs:** the room is **quiesced**; participants are **notified** they are
  reconnecting/recovering; **command admission is closed**; **delivery is paused** (clients await a fresh
  snapshot); **gameplay does not advance**.
- **Who waits:** all participants of the **recovering room** wait; participants of **other rooms are
  unaffected** (isolation).
- **Who is notified:** the recovering room's participants (a "recovering/reconnecting" system signal —
  ADR-009 presence/notification), carrying **no** secret data.
- **Can new participants join?** **Not until recovery completes** for that room (admission closed);
  attempts wait or are told the room is recovering.
- **Can commands execute?** **No** — command admission is closed until recovery completes (ADR-003 gate).
- **Can gameplay continue?** **No** — gameplay resumes only after restore → verify → project → rebind →
  sync (§8). **Correctness over speed** (recovery-before-gameplay).

---

## 13. Architectural Invariants (AI-REC-*)

- **AI-REC-1:** **Recovery never invents state** — it restores only committed truth.
- **AI-REC-2:** **Recovery never changes committed versions** — the restored version equals the last committed one.
- **AI-REC-3:** **Recovery never changes winners/outcomes** — results are immutable and preserved.
- **AI-REC-4:** **Recovery never changes hidden information** — the key is restored server-side only; projections are recomputed and re-filtered.
- **AI-REC-5:** **Recovery never changes participant identity** — one participation record per participant, same role/team.
- **AI-REC-6:** **Recovery never bypasses the Authority** — the single writer performs restoration; nothing else mutates state.
- **AI-REC-7:** **Recovery completes (and validates) before gameplay, delivery, participation, or new commands resume.**
- **AI-REC-8:** **Recovery is room-scoped** — recovering one room never affects another.
- **AI-REC-9:** **Recovery never creates duplicate participants or duplicate deliveries.**
- **AI-REC-10:** **A room never recovers twice simultaneously** — recovery is single-owner and serialized.
- **AI-REC-11:** **Recovery never modifies business history** — committed events/results are read, not rewritten.
- **AI-REC-12:** **Recovery never changes authoritative ownership** — exactly one Authority owns the room after recovery.
- **AI-REC-13:** **Recovery never re-adjudicates the past** — committed outcomes are restored, not re-decided; the in-flight Intent is re-admitted once or dropped, never applied twice.
- **AI-REC-14:** **Finished matches never resume** ([INV-G7](../../02-business-analysis/10-business-invariants.md)).
- **AI-REC-15:** **Recovery is atomic** — a room fully recovers-and-validates or is not resumed (fallback).

---

## 14. Architecture Fitness Functions (FF-REC-*)

- **FF-1:** **Recovered version == last committed version** (no rewind/invention).
- **FF-2:** **Recovered state reproduces identical projections** to what those versions would produce (recompute equality; composes with ADR-006 FF-2).
- **FF-3:** **Recovery is deterministic** — same durable record ⇒ same restored state.
- **FF-4:** **Recovery is idempotent** — re-running yields the same result; re-applied committed events add no effect.
- **FF-5:** **Recovery never duplicates gameplay actions** (a committed reveal/turn appears once).
- **FF-6:** **Recovered participants retain their role/team** (frozen mid-match).
- **FF-7:** **Finished games never resume** after recovery.
- **FF-8:** **A recovered room passes invariant validation** before resuming (business + architectural).
- **FF-9:** **Every recovery is traceable** (which checkpoint + tail + version produced the restored state).
- **FF-10:** **No room recovers twice simultaneously** (single-owner, serialized).
- **FF-11:** **No non-Spymaster projection contains the key at any point during/after recovery** (composes with ADR-006 FF-1).
- **FF-12:** **No cross-room read/write occurs during a room's recovery** (isolation).

Map to [Success Metrics ASM-02/05/06/07](../../06-architecture-governance/04-architecture-success-metrics.md),
[QS-06](../09-quality-attribute-scenarios.md).

---

## 15. Security Analysis

Separating **architectural guarantees** from **future technical controls** (persistence/crypto — Non-Goals).

| Threat | Architectural guarantee | Future technical control |
|--------|-------------------------|--------------------------|
| **Recovery spoofing** | Recovery reads the **server-owned durable record**, never client-supplied state (contrast RM2, disqualified); the Authority alone restores. | Integrity-protected durable record (technical design). |
| **Replay attacks** | Committed events are folded **once, in order, up to the last commit**; re-folding is idempotent (FF-4); the in-flight Intent is never applied twice (AI-REC-13). | Anti-replay on the durable record. |
| **Corrupted recovery** | Verification (§10) **aborts** on invariant violation → graceful termination; never resume on invalid state. | Redundant/checksummed records. |
| **Unauthorized recovery** | Recovery is an internal Authority operation, not a client action; clients cannot trigger arbitrary state restoration. | Access-controlled recovery ops (ops). |
| **Projection leakage during recovery** | Delivery is **paused** until recovery completes; projections are **recomputed** and re-filtered; the key never enters a non-Spymaster projection (FF-11). | Encryption in transit (future). |
| **Identity confusion** | Participation restores one-per-participant; re-bind by continuity token; roles frozen (AI-REC-5/9). | Auth-grade identity (future). |
| **Duplicate authority** | Exactly one Authority is (re-)established before restore (AI-REC-12); single-owner enforced. | Fencing/leases in distribution (ADR-007). |
| **Split-brain (conceptual)** | The single-owner invariant forbids two Authorities acting on one room; under partition, at most one owner may act; the architecture **never** merges divergent writers. | Fencing tokens/consensus (ADR-007/RM9). |
| **Future authentication** | Auth attaches at the identity seam; recovery model unchanged. | AuthN/AuthZ (future). |
| **Future encryption** | Recovery is agnostic to at-rest/in-transit encryption; adding it doesn't change the model. | Crypto (technical design/ops). |

**Bottom line:** recovery **cannot be driven by clients**, **cannot change outcomes or leak the key**, and
**cannot create two authorities that both act** — by construction. Residual exposures (durable-record
integrity, distribution fencing, crypto) are **future technical controls**, delegated and named.

---

## 16. Trade-off Analysis

- **Correctness:** Maximized — restore committed truth, verify, never re-adjudicate; abort rather than resume-wrong.
- **Availability:** A room is briefly unavailable during recovery (accepted); other rooms stay up (isolation); warm standby (future) reduces this.
- **Recovery latency:** Checkpoint restore is fast; committed-tail fold is bounded (short matches). Prioritized **below** correctness.
- **Complexity:** Moderate — checkpoint + tail + verification. Justified; simpler than warm/distributed HA, which is deferred.
- **Memory:** Bounded per-room aggregate; the durable record is out-of-scope tech but conceptually small (short matches).
- **Testing:** Strong — determinism + idempotency make recovery reproducible; chaos/kill tests are well-defined (§14).
- **Operational impact:** Recovery is per-room and automatic; recovery storms mitigated by lazy/on-access recovery.
- **Developer experience:** Clear model — "restore committed, recompute derived, verify, resume."
- **Future scalability/clustering:** Per-room, single-owner recovery maps cleanly onto distributed ownership (ADR-007) and warm standby (RM9) without model change.
- **Future persistence:** The model names *what* is durable (committed checkpoints + committed events) and *what* is recomputed; **any** persistence technology that can store/read these conforms — the model is unchanged.

---

## 17. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| Durable-record staleness → tail loss on crash | Recovery | Frequent checkpoints / committed-tail durability (technical design); RM5 bounds loss to un-persisted commits. |
| Two Authorities act on one room (split-brain) | Architecture/Consistency (critical) | Single-owner invariant (AI-REC-12); fencing/leases in distribution (ADR-007); never merge writers. |
| Recovery resumes on corrupt/invalid state | Correctness (critical) | Verification gate (§10); abort → graceful termination; correctness over availability. |
| Recovery storms on mass restart | Operational/Scalability | Lazy/on-access recovery; bounded per-room cost; isolation. |
| Re-adjudication or double-applied Intent | Correctness | AI-REC-13; fold committed outcomes only; in-flight Intent re-admitted once/dropped; FF-5. |
| Stale projection resurfaces as truth | Security | Projections recomputed, never restored (AI-REC-4/FF-11); delivery paused until complete. |
| Cross-room interference during recovery | Isolation | AI-REC-8/FF-12; room-scoped recovery. |
| Recovery never completes (limbo) | Operational | Timeout → fallback (§10/§11); bounded retries. |
| Testing can't cover all crash points | Testing | Property-based + chaos (kill at every lifecycle stage §8); determinism/idempotency assertions. |

---

## 18. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | **Committed state can be durably recorded** (checkpoint and/or committed events) for the room lifetime. *(Deferred to technical design, but the model requires it.)* | High | If nothing durable exists, only cold restart is possible (RM1 fallback) — reduced resilience; the model still holds (degrades to fallback). |
| AS-2 | **Every observed version corresponds to a committed checkpoint** (commit-then-broadcast, ADR-003/004). | Fact | — |
| AS-3 | **Room state is small/bounded**, so checkpoint restore + short tail is fast. | Very High | If large (not for a 25-card game), incremental recovery (RM7) would matter — not applicable. |
| AS-4 | **A single Authority per room can be re-established** before restore (routing/ownership). | High | If not guaranteed, split-brain risk → must add fencing (ADR-007) before distribution. |
| AS-5 | **Recovery-before-everything gating** is acceptable UX (brief pause over incorrect resume). | Very High | If instant availability were required over correctness, warm standby (RM9) reduces the pause — but correctness gating remains. |
| AS-6 | **Aborting to graceful termination** is acceptable for unrecoverable/corrupt rooms. | High | If a room must never be lost, stronger durability/redundancy is needed (technical design) — model unchanged. |

---

## 19. Non-Goals

This ADR does **not** decide: **database technology, storage engine, snapshot/event-store implementation,
Redis, SQL, replication, infrastructure, cloud, frameworks, deployment, backup strategy, or disaster
recovery products**. It defines **only** the recovery architecture (unit, lifecycle, guarantees,
invariants). The above belong to **Technical Design and Operations**, which must **conform to** this model.

---

## 20. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-005 |
|-------------|-------------------------------|
| **ADR-007 Room Isolation & Distribution** | Must guarantee **exactly one Authority per room** during routing/failover (fencing/leases); per-room recovery relocates ownership without cross-room effect; warm standby (RM9) is the compatible HA path. |
| **ADR-008 Dictionary Architecture** | The pinned dictionary version is recovered with the aggregate; dictionary content is external and re-referenced, not part of recovery. |
| **ADR-010 Command/Query Strategy** | Commands are **inadmissible** during recovery; queries served only after recovery completes (fresh projections); no read-back of partial state. |
| **Software Architecture / Technical Design** | Names a **State Custody** responsibility that durably records committed checkpoints + committed events and supports restore — **any** technology conforming to this model; must preserve commit-then-broadcast and versioning. |
| **Implementation** | Restore committed truth only; recompute derived/projections; verify before resume; idempotent recovery; no re-adjudication; no client-driven recovery. |
| **Testing** | Chaos/kill-at-every-stage, determinism/idempotency, no-double-actions, finished-never-resume, no-key-leak-during-recovery, single-recovery, room-isolation (FF-REC-*). |
| **Operations** | Automatic per-room recovery; monitor recovery success/latency, single-owner, abort/fallback rates; ensure recovery signals carry no secret. |
| **Future Clustering / Dedicated Servers** | Per-room single-owner recovery + isolation maps directly; add fencing + optional warm standby without changing the model. |

---

## 21. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | [BR-GE](../../02-business-analysis/02-business-rules.md) (game end/finality), [BR-RX-3/BR-GE-5](../../02-business-analysis/02-business-rules.md) (abandonment fallback), [BR-DC](../../02-business-analysis/02-business-rules.md) (disconnect), [BR-HM](../../02-business-analysis/02-business-rules.md) (host). |
| **Business Invariants** | [INV-G7](../../02-business-analysis/10-business-invariants.md) (finished never resumes), [INV-O1/O4](../../02-business-analysis/10-business-invariants.md) (single/immutable result), [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-R1](../../02-business-analysis/10-business-invariants.md) (one host), [INV-T5](../../02-business-analysis/10-business-invariants.md) (frozen roles), [INV-P4](../../02-business-analysis/10-business-invariants.md) (one connection), [INV-D3](../../02-business-analysis/10-business-invariants.md) (dictionary pin). |
| **Engineering Challenges** | [ENG-RE-01/02](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), ENG-ST-04, ENG-RM-02, ENG-FP-01. |
| **Quality Attribute Scenarios** | [QS-06](../09-quality-attribute-scenarios.md) (recovery), [QS-01](../09-quality-attribute-scenarios.md) (no leak), [QS-02/03](../09-quality-attribute-scenarios.md) (determinism/terminal), [QS-05](../09-quality-attribute-scenarios.md) (availability). |
| **State Ownership** | [05 State Ownership](../05-state-ownership.md) (Authority owns; store holds, never adjudicates). |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md) (CB-03 completion, CB-08 reconnect). |
| **ADR-000** | *State Custody, Recovery, Snapshot, Aggregate, Authority, Projection, Determinism, Idempotency, Room Isolation, Commit* used as defined. |
| **ADR-001/002/003/004/006/009** | Authority re-established (001/003); aggregate is the unit (002); commit-then-broadcast + versions (003/004); projections recomputed & filtered (006); participation restored & re-bound (009). |
| **Governance** | [AP-03/04/05/06/07/08/12/14/18](../../06-architecture-governance/01-architecture-principles.md); [AAP-02/05/08/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 22. Architecture Review

- **Decision:** **Authoritative Checkpoint Recovery** — restore the room aggregate to its last committed
  checkpoint + committed-event tail, recompute derived data/projections, re-bind participants, **verify**,
  then resume delivery/participation/gameplay/commands; abort to graceful room termination on
  unrecoverable/corrupt state; per-room, single-owner, deterministic, idempotent.
- **Confidence:** **High** — entailed by ADR-002 (aggregate/recovery unit) and ADR-003/004
  (commit-then-broadcast + versions) and the bounded, short-lived nature of rooms; alternatives are
  disqualified (stateless), too lossy (cold), or premature (warm/distributed HA).
- **Remaining risks:** durable-record freshness (tail loss) — technical design; **split-brain/single-owner**
  under distribution — **ADR-007** (fencing); recovery storms — lazy recovery.
- **Open questions (delegated, non-blocking):** checkpoint cadence vs event-tail length; durable-record
  technology and integrity (technical design); warm-standby adoption (future HA); recovery timeout values
  (operational parameters).
- **Review triggers:** distribution/clustering (single-owner fencing becomes mandatory); a requirement for
  near-zero recovery pause (warm standby); long-lived or large room state (not expected); durable
  match/audit history requirements (adopt committed-event store under this model).
- **Readiness for ADR-007:** **READY.** Recovery is defined as per-room, single-owner, and isolated, with a
  clear single-Authority requirement — exactly the property **ADR-007 (Room Isolation & Distribution)**
  must guarantee across nodes (routing, ownership, fencing) without changing this recovery model.

---

## 23. Adversarial Architecture Review — "Attempt to Break the Recovery Model"

**Attack · Expected Outcome · Architectural Protection · Residual Risk · Mitigation.**

1. **Can recovery resurrect a finished game?**
   - *Expected:* No. *Protection:* AI-REC-14/FF-7; a Finished result is immutable and preserved ([INV-G7/O4](../../02-business-analysis/10-business-invariants.md)); recovery restores the terminal state, it does not reopen it. *Residual:* none. *Mitigation:* —
2. **Can recovery duplicate participants?**
   - *Expected:* No. *Protection:* participation is part of the single aggregate; one record each (AI-REC-9, FF-6; ADR-009). *Residual:* none. *Mitigation:* —
3. **Can recovery change committed versions?**
   - *Expected:* No. *Protection:* AI-REC-2/FF-1; restored version == last committed. *Residual:* durable-record inconsistency. *Mitigation:* verification (§10) aborts on mismatch.
4. **Can recovery reveal hidden information?**
   - *Expected:* No. *Protection:* projections recomputed & re-filtered; delivery paused until complete; key server-only (AI-REC-4/FF-11; ADR-006). *Residual:* interception of a Spymaster stream post-resume. *Mitigation:* encryption in transit (future).
5. **Can recovery assign a different Spymaster?**
   - *Expected:* No. *Protection:* roles restored as committed, frozen mid-match (AI-REC-5/FF-6; [INV-T5](../../02-business-analysis/10-business-invariants.md)). *Residual:* none. *Mitigation:* —
6. **Can recovery change the board?**
   - *Expected:* No. *Protection:* board words/key/starting-team immutable; reveal flags restored as committed (AI-REC-1/3). *Residual:* none. *Mitigation:* —
7. **Can recovery create two Authorities?**
   - *Expected:* No. *Protection:* exactly one Authority re-established before restore (AI-REC-12). *Residual:* split-brain under partition/distribution. *Mitigation:* single-owner fencing (ADR-007); never merge writers.
8. **Can recovery replay gameplay twice?**
   - *Expected:* No. *Protection:* committed outcomes are **restored**, not re-adjudicated; committed events fold **once**; in-flight Intent re-admitted once/dropped (AI-REC-13/FF-5). *Residual:* none. *Mitigation:* —
9. **Can commands execute before recovery finishes?**
   - *Expected:* No. *Protection:* command admission is **closed** until recovery completes (AI-REC-7; ADR-003 gate). *Residual:* none. *Mitigation:* —
10. **Can delivery resume before projections exist?**
    - *Expected:* No. *Protection:* strict lifecycle (§8): projection regeneration precedes resume-delivery (AI-REC-7). *Residual:* none. *Mitigation:* —
11. **Can reconnect succeed before recovery completes?**
    - *Expected:* No. *Protection:* participation rebinding is a recovery stage; the room is quiesced until complete; reconnect awaits a fresh snapshot (AI-REC-7; ADR-009/004). *Residual:* none. *Mitigation:* —
12. **Can stale projections survive recovery?**
    - *Expected:* No. *Protection:* projections are recomputed, never restored; caches discarded (AI-REC-4; ADR-006). *Residual:* client cache misuse. *Mitigation:* clients re-snapshot; server projections authoritative-derived only.
13. **Can two recoveries execute simultaneously?**
    - *Expected:* No. *Protection:* recovery is single-owner and serialized (AI-REC-10/FF-10). *Residual:* routing bug. *Mitigation:* single-owner enforcement (ADR-007).
14. **Can room recovery affect another room?**
    - *Expected:* No. *Protection:* room-scoped recovery; no cross-room read/write (AI-REC-8/FF-12). *Residual:* shared-resource contention (operational). *Mitigation:* isolation; per-room resource bounds.
15. **Can recovery violate room isolation?**
    - *Expected:* No. *Protection:* the recovery unit is one room aggregate; isolation invariant (AI-REC-8). *Residual:* misconfiguration. *Mitigation:* isolation fitness test (FF-12).

**Conclusion:** the recovery model **cannot change outcomes, cannot leak hidden information, cannot
resume finished matches, cannot duplicate participants/actions, cannot resume before completion, and
cannot cross room boundaries** — **by construction** — because recovery **restores committed truth
under a single re-established Authority, recomputes all derived views, verifies invariants, and gates
all activity until complete**, aborting to an honest failure rather than resuming on an invalid state.
The only genuine residuals — **durable-record integrity/freshness**, **split-brain under future
distribution**, and **transport encryption** — are **future technical/operational controls**, explicitly
delegated (technical design, ADR-007, ops) and named, not weaknesses of the architecture.

---

## Final Deliverable — Answers

- **What is the recovery unit?** The **Room aggregate** (per room), recovered independently.
- **What survives a crash?** The **last committed authoritative state** — room identity/lifecycle,
  membership, **participation records** (one per participant), **roles/teams**, **board (words, key, reveal
  flags, starting team)**, **turn/clue/allowance**, **host**, **pinned dictionary version**, **result (if
  written)**, and the **per-room version** — via a durable record of committed state.
- **What is intentionally lost?** Transient **connections**, **delivery buffers/subscriptions**, **projection
  caches**, **uncommitted (in-flight) work**, and client/UI state.
- **What is reconstructed?** **Participation re-binding** and **presence** (re-established from reconnections).
- **What is recomputed?** All **derived data** (remaining-agents, "win now?") and all **role-filtered
  projections** (ADR-006) — never restored as truth.
- **What guarantees recovery correctness?** Restoring **committed truth only** (no invention, no
  re-adjudication), **verification** of business + architectural invariants before resuming, **determinism**
  and **idempotency**, and **abort-to-fallback** on invalid state.
- **How is participant continuity preserved?** Participation records restore one-per-participant with their
  role/team; a returning connection **re-binds** via the room-scoped continuity token within grace
  (ADR-009); presence is re-established.
- **How is version continuity preserved?** The restored state carries its **committed per-room version**;
  recovery never rewinds below a delivered version nor invents a higher one; clients **re-snapshot** at the
  recovered version (ADR-004).
- **Why can recovery never change game outcomes?** Results/board/turns are **committed and immutable**;
  recovery **restores**, it never **re-decides**; finished matches never resume (AI-REC-3/13/14).
- **Why can recovery never leak hidden information?** Projections are **recomputed and re-filtered** from
  restored state; delivery is **paused** until complete; the key is restored **server-side only** and never
  enters a non-Spymaster projection (AI-REC-4, ADR-006).
- **Why can gameplay never resume before recovery completes?** Command admission, delivery, participation,
  and gameplay are **gated** behind restore → verify → project → rebind → sync; **correctness over speed**
  (AI-REC-7).
- **How does this support future persistence technologies without changing the model?** The model names
  **what** must be durable (committed checkpoints + committed events) and **what** is recomputed; **any**
  storage technology that can record and read those — under a **State Custody** responsibility that never
  adjudicates — conforms. Persistence is *where/how*; recovery is *the model* — and the model is fixed here.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: Authoritative Checkpoint Recovery (snapshot + committed-tail), room-scoped, single-owner, deterministic, idempotent, verify-before-resume, abort-to-fallback; lifecycle, guarantees, invariants, fitness functions, security & adversarial review, verdict. |
