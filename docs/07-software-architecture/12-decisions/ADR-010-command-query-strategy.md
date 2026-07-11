# ADR-010 — Command / Query Strategy Architecture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-04 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How does every actor interact with Cluely while preserving authoritative ownership, deterministic gameplay, consistency boundaries, recovery guarantees, room isolation, and hidden-information integrity?* |
| **Complies with** | [ADR-000](ADR-000-architecture-vocabulary.md) … [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md) (all frozen). Extends the [Command & Query Discovery](../07-command-query-discovery.md); redefines none. |
| **Scope note** | Defines the **interaction model** only. It chooses **no** REST/HTTP/GraphQL/SignalR/gRPC/ASP.NET/controllers/MediatR/CQRS-library/API/DTO/serialization/broker/framework ([§20 Non-Goals](#20-non-goals)). "Command/Query separation" here is an **architectural stance**, not a product/library called "CQRS". |

---

## 1. Executive Summary

Every interaction with Cluely is either a **Command** or a **Query**, and the two are **fundamentally
different**:

- A **Command** is an **Intent** ([ADR-000](ADR-000-architecture-vocabulary.md)) to *change* a room's
  authoritative state. It is admitted, validated, **adjudicated by the room's single Authority**
  ([ADR-003](ADR-003-per-room-coordination-model.md)), and **committed exactly once**; its result is
  observed **only** via delivered domain events/projections ([ADR-004](ADR-004-real-time-communication-delivery.md)),
  never by "reading back" from the writer.
- A **Query** is a request to *observe* truth. It returns a **read-only, role-filtered projection**
  ([ADR-006](ADR-006-role-based-information-visibility.md)) **derived** from committed state
  ([ADR-002](ADR-002-authoritative-game-state.md)). A Query **never** changes state, never coordinates,
  never becomes authoritative, and **gameplay never depends on it**.

**Why separating them is correctness, not convenience:** gameplay determinism, consistency, and
hidden-information integrity all rest on a single fact — **only the Authority may change state, and it
does so through one ordered, committed path**. If reads could mutate, or writes could bypass the
Authority, or a projection could be treated as truth, the guarantees of ADR-002/003/006 would collapse.
So the interaction model makes the asymmetry **structural**: **the Authority accepts only Commands;
Queries only observe projections; the two never cross.**

**Why the Authority only accepts Commands:** it is the single writer ([ADR-001/003](ADR-003-per-room-coordination-model.md));
Commands are the *only* way to request a change, and every change is one **decision → commit → version
increment** ([ADR-004 versioning](ADR-004-real-time-communication-delivery.md#7-versioning-strategy)).
**Why Queries are never authoritative:** they are **derived, disposable projections**; promoting one to
truth would create a second source of truth and a leak vector — forbidden by
[ADR-002 AI-STATE-8](ADR-002-authoritative-game-state.md#14-architectural-invariants-introducedaffirmed)
and [ADR-006](ADR-006-role-based-information-visibility.md).

The chosen model is a **Command/Query-Separated, Authority-Owned, Intent-Based Interaction Model**:
Commands flow to the **owning** Authority (routed by [ADR-007](ADR-007-room-isolation-distribution.md)),
are validated then adjudicated then committed under [ADR-003](ADR-003-per-room-coordination-model.md);
Queries are answered as **role-filtered projections** of committed state, delivered read-only. This
model is **transport-agnostic**: any future API, protocol, client, bot, or agent is merely a **carrier**
of Commands and Queries and must **conform to** this model rather than redefine it.

> One-line statement: **Commands express intent and change truth (Authority-only, ordered,
> committed-once); Queries observe truth as role-filtered projections (read-only, disposable, never
> authoritative); the two never cross, and gameplay depends only on Commands.**

---

## 2. Problem Statement

Multiplayer systems fail when the read/write boundary is blurred:

- **Reads mutate state** → hidden coupling, races, and non-reproducible behavior; a "query" that changes
  something is a disguised, unordered write ([ENG-ST-01/02](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)).
- **Writes bypass authority** → two writers, non-determinism, split-brain; the single-writer guarantee of
  [ADR-003](ADR-003-per-room-coordination-model.md) is void.
- **Clients issue competing state** → if clients could assert state (not just *intend* it), a modified
  client could inject a false board/key ([AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Projections become truth** → drift and leaks; a cached/read view treated as canonical breaks
  [ADR-002](ADR-002-authoritative-game-state.md)/[ADR-006](ADR-006-role-based-information-visibility.md).
- **Queries influence gameplay** → outcomes become dependent on read timing/freshness — non-deterministic
  and unfair.

**Command/Query separation is architectural, not a framework choice.** It is not "use a CQRS library";
it is a **stance** that the *ability to change truth* is exclusive to the Authority via Commands, and
*observation* is a separate, powerless act. No framework can grant this; only the model can — and every
prior ADR already assumes it (Authority owns state, commit-then-broadcast, projections derived,
recovery blocks writes). This ADR names it explicitly so every future interaction conforms.

---

## 3. Architectural Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Commands Express Intent** | A Command *requests* a change; it does not assert state. | Clients propose; Authority decides ([AP-03](../../06-architecture-governance/01-architecture-principles.md)). |
| **Authority Decides** | Only the room Authority adjudicates a Command. | Single writer ([ADR-003](ADR-003-per-room-coordination-model.md)). |
| **Queries Observe Truth** | A Query returns a projection of committed state. | Read/observe only ([ADR-006](ADR-006-role-based-information-visibility.md)). |
| **Commands Change State** | State changes **only** via committed Commands. | One mutation path ([ADR-002](ADR-002-authoritative-game-state.md)). |
| **Queries Never Change State** | A Query has no side effects on truth. | No disguised writes. |
| **One Command → One Decision** | Each admitted Command yields at most one committed decision. | Idempotency ([ADR-003 CR-3](ADR-003-per-room-coordination-model.md#3-coordination-requirements)). |
| **Queries Never Coordinate** | Queries never enter the ordered write path. | They cannot race/order state. |
| **Commands Are Ordered** | Commands are serialized per room and version-stamped at commit. | Determinism ([ADR-003/004](ADR-004-real-time-communication-delivery.md)). |
| **Queries Are Disposable** | Query results (projections) are transient, non-authoritative. | Never a second truth. |
| **Truth Before Projection** | Projections derive from **committed** truth only. | Commit-then-project. |
| **Projection Before Delivery** | Only projected (filtered) views are delivered. | [ADR-006](ADR-006-role-based-information-visibility.md). |
| **Visibility Before Query** | Role filtering is applied before any Query result leaves. | No leak ([INV-B9](../../02-business-analysis/10-business-invariants.md)). |
| **Consistency Before Freshness** | Correct, consistent reads beat maximally-fresh reads. | Fairness > freshness. |
| **Validation Before Admission** | A Command is validated before the Authority admits/adjudicates it. | [ADR-003](ADR-003-per-room-coordination-model.md) gate. |
| **Recovery Before Commands** | No Command is admitted while a room is recovering. | [ADR-005](ADR-005-state-recovery-resilience.md) gate. |

---

## 4. Candidate Interaction Models

Evaluated for the authoritative, deterministic, hidden-information, room-isolated setting. None dismissed
without reasoning.

### IM1 — CRUD (clients create/read/update/delete state)
- **Overview:** Clients directly manipulate state records.
- **Correctness/Determinism:** **Fails** — clients asserting/updating state bypasses the Authority; no
  single-writer, no adjudication, no ordering; leak-prone.
- **Verdict:** **Disqualified** — contradicts [ADR-002/003](ADR-003-per-room-coordination-model.md), [AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md).

### IM2 — RPC (remote procedure calls that read *and* write freely)
- **Overview:** Callers invoke procedures that may mutate and return state.
- **Disadvantages:** Mixes read and write; a call that both mutates and "reads back" invites treating the
  return as truth and blurs ordering; no inherent single-writer discipline.
- **Verdict:** As a *transport tactic* it is fine (a Command or Query can be *carried* by an RPC), but as
  an **interaction model** it lacks the write/read separation and authority discipline → rejected as the
  model; permitted only as a carrier under CQS (IM4).

### IM3 — Traditional Service Layer (services with mixed read/write methods on shared state)
- **Overview:** A service layer exposes methods that read and write.
- **Disadvantages:** Encourages reads that touch write paths and writes that return state; tends toward
  shared mutable state and rule-in-service sprawl ([AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Verdict:** Rejected as the model; a thin **application layer** may *carry* Commands/Queries but must
  not merge them.

### IM4 — Command/Query Separation (CQS) — Authority-owned Commands, projection Queries (RECOMMENDED)
- **Overview:** Writes are **Commands** to the Authority (ordered, committed-once); reads are **Queries**
  returning role-filtered projections; the two are separate and never cross.
- **Advantages:** Perfect fit for the frozen ADRs — single writer, commit-then-broadcast, derived
  projections, recovery gating; deterministic, testable, leak-safe.
- **Verdict:** **Selected** — it is the *stance* the whole architecture already assumes.

### IM5 — Event-Sourcing Style (interaction as append of events; reads fold events)
- **Overview:** Commands append events; queries fold events/read models.
- **Advantages:** Natural ordering/versioning; strong for recovery/audit.
- **Disadvantages:** As an *interaction* model it still needs the Authority to admit/decide (i.e., CQS
  underneath); the event-store is a **persistence/recovery** concern (ADR-005), not the interaction model.
- **Verdict:** **Compatible substrate**, not the interaction model itself — folded under IM4 (Commands are
  admitted/decided by the Authority; whether the commit is recorded as events is ADR-005).

### IM6 — Actor Messaging (send messages to a room actor)
- **Overview:** Interactions are messages to the room's single-writer actor.
- **Advantages:** Matches the single-writer room entity ([ADR-001](ADR-001-overall-architecture-style.md)); messages to one mailbox = ordered Commands.
- **Disadvantages:** A *realization* of "route Commands to the owning Authority"; says nothing about the
  read side by itself.
- **Verdict:** A natural **realization** of IM4's Command path (a Command is a message to the room
  Authority); the read side is still projection Queries. Folded under IM4.

### IM7 — Hybrid (FINAL): CQS with Authority-owned Commands + projection Queries, realized as messages to the owning room Authority
- **Overview:** IM4 (the stance) + IM6 (Commands as messages to the owning Authority) + IM5-compatible
  recording (ADR-005) + projection Queries (ADR-006).
- **Verdict:** **This is the decision.**

### IM8 — Shared State (actors read/write a shared store directly)
- **Overview:** Interactions go through shared mutable state.
- **Disadvantages:** **Fails** — cross-actor/room coupling, races, no single writer ([AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Verdict:** **Disqualified.**

### Evaluation summary

| Criterion | IM1 CRUD | IM2 RPC | IM3 Service | **IM4/6/7 CQS (chosen)** | IM8 Shared |
|-----------|:--------:|:-------:|:-----------:|:------------------------:|:----------:|
| Correctness | 1 | 3 | 3 | **5** | 1 |
| Determinism | 1 | 2 | 3 | **5** | 1 |
| Consistency | 2 | 3 | 3 | **5** | 1 |
| Recovery | 2 | 3 | 3 | **5** | 2 |
| Scalability | 3 | 3 | 3 | **5** | 2 |
| Isolation | 2 | 3 | 3 | **5** | 1 |
| Testing | 2 | 3 | 3 | **5** | 1 |
| Evolution | 2 | 3 | 3 | **5** | 1 |
| Developer Experience | 4 | 4 | 4 | **4** | 2 |

---

## 5. Final Interaction Model

**Adopt IM7 — Command/Query Separation with Authority-owned Commands and projection Queries**, realized as
**Commands routed to the owning room Authority** (ADR-007) and **Queries answered as role-filtered
projections** of committed state (ADR-006).

- **Why it fits Cluely:** it is the explicit naming of the interaction discipline every frozen ADR already
  depends on — single writer, ordered/committed Commands, derived projections, recovery gating, room
  isolation, no leak.
- **Why the Authority owns Commands:** it is the single writer; Commands are the only change path; each is
  one decision → commit → version increment ([ADR-003/004](ADR-004-real-time-communication-delivery.md)).
- **Why Queries observe projections:** truth is the aggregate ([ADR-002](ADR-002-authoritative-game-state.md));
  participants observe **role-filtered projections** of it ([ADR-006](ADR-006-role-based-information-visibility.md)),
  never the raw aggregate or a competing copy.
- **Why gameplay never depends on queries:** outcomes are decided by Commands against committed state;
  Queries only *observe* — a stale/slow/absent Query can never change what happened.
- **Why projections never become authoritative:** they are derived and disposable; promoting one would
  create a second truth and a leak path — forbidden.

---

## 6. Command Architecture

| Question | Answer |
|----------|--------|
| **What is a Command?** | An **Intent** to change a room's authoritative state (e.g., Submit Clue/Guess, Join, Leave, Start, Select Team/Role/Region, Reconnect, End Turn). |
| **Who creates Commands?** | Any actor (participant/host/system/future bot/AI) — but creation is only a *proposal*. |
| **Who owns Commands?** | The **room's Authority** owns admission/adjudication/commit of a Command; the actor owns only the *intent* to submit it. |
| **Who validates Commands?** | The **validation gate** ([ADR-003 R-10](../03-system-responsibilities.md)) checks role/team/phase/state/version/ownership **before** the Authority adjudicates (§10). |
| **Who executes (adjudicates) Commands?** | The **single Authority** for the target room ([ADR-003](ADR-003-per-room-coordination-model.md)). |
| **Who commits Commands?** | The Authority, **atomically**, at the ADR-003 Commit stage, incrementing the per-room version. |
| **Who rejects Commands?** | The validation gate (structural/authorization) or the Authority (state-dependent), returning a catalogued [business error](../../02-business-analysis/12-business-error-catalog.md). |
| **Who observes Commands?** | Everyone — via delivered **domain events/projections** ([ADR-004/006](ADR-006-role-based-information-visibility.md)); **no** actor reads a Command's result back from the writer. |

**Differentiating the stages (a Command is not a single act):**
- **Intent** — the actor's proposal (unauthoritative).
- **Validation** — admit/reject against role/team/phase/state/version/ownership (no mutation on reject).
- **Decision (adjudication)** — the Authority resolves the outcome per rules/precedence ([ADR-003](ADR-003-per-room-coordination-model.md), [Rule Precedence](../../02-business-analysis/16-rule-precedence.md)).
- **Commit** — the outcome becomes truth atomically (version++).
- **Publication** — domain events/projections are emitted **after** commit (commit-then-broadcast).

---

## 7. Query Architecture

| Question | Answer |
|----------|--------|
| **What is a Query?** | A request to **observe** current truth for a room, answered as a **role-filtered projection**. |
| **What is queried?** | Committed room/game state, **as a projection** (Spymaster view, Operative view, lobby view, result view) — never the raw aggregate. |
| **Who owns projections?** | The **Projection Policy** ([ADR-006](ADR-006-role-based-information-visibility.md)) produces them from committed state; the **Delivery boundary** ([ADR-004](ADR-004-real-time-communication-delivery.md)) carries them; **no one owns them as truth**. |
| **What is query freshness?** | A projection reflects **some committed version**; the freshest is the latest committed version at answer time. |
| **What is eventual freshness?** | A Query may briefly reflect a slightly older committed version (a client catches up via versioned delivery); it **always** reflects *a* committed version, never partial/uncommitted state. |
| **What can Queries never do?** | Change state, coordinate, order, become authoritative, carry hidden information to the wrong role, or influence gameplay. |
| **Role-filtered projections** | Every Query result is produced **by inclusion** for the actor's authoritative (role, team) ([ADR-006](ADR-006-role-based-information-visibility.md)); the **key** appears only in the Spymaster projection. |

---

## 8. Command Lifecycle

Technology-neutral; composes ADR-003/004/005/006/007.

```
Intent → Admission (routed to the OWNING Authority; blocked if recovering) → Validation
   → Authority (single writer) → Business Decision (rules/precedence) → Commit (atomic, version++)
   → Projection Update (recompute role projections) → Delivery (role-filtered, version-tagged) → Observation
```

| Stage | What happens |
|-------|--------------|
| **Intent** | An actor proposes a Command. |
| **Admission** | The Command is **routed to the room's current owning Authority** ([ADR-007](ADR-007-room-isolation-distribution.md)); **rejected/blocked if the room is recovering** ([ADR-005](ADR-005-state-recovery-resilience.md)); enters the room's single ordered intake ([ADR-003](ADR-003-per-room-coordination-model.md)). |
| **Validation** | Role/team/phase/state/version/ownership checks; catalogued rejection on failure, **no mutation** (§10). |
| **Authority** | The single writer takes the Command from the ordered intake. |
| **Business Decision** | The rules core adjudicates per rules/precedence against committed state (first-valid-wins). |
| **Commit** | The outcome is applied atomically; **per-room version increments** ([ADR-004](ADR-004-real-time-communication-delivery.md)). |
| **Projection Update** | Role-filtered projections are recomputed from the new committed state ([ADR-006](ADR-006-role-based-information-visibility.md)). |
| **Delivery** | Version-tagged, role-filtered projections/events are pushed to participants ([ADR-004](ADR-004-real-time-communication-delivery.md)). |
| **Observation** | Actors observe the result via delivery; **never** by reading back from the Authority. |

Only **Authority → Commit** mutates truth; everything before is admission/validation, everything after is
read-only propagation.

---

## 9. Query Lifecycle

```
Consumer → Projection Selection (for the consumer's authoritative role/team) → Visibility Filter (inclusion)
   → Projection Read (from committed state at latest version) → Delivery (read-only) → Discard
```

| Stage | What happens |
|-------|--------------|
| **Consumer** | An actor requests to observe. |
| **Projection Selection** | The projection for the actor's **authoritative** (role, team) is selected ([ADR-006](ADR-006-role-based-information-visibility.md)). |
| **Visibility Filter** | The projection is assembled **by inclusion** (whitelist) — the key only for Spymasters. |
| **Projection Read** | The projection reflects committed state at the latest committed version. |
| **Delivery** | The read-only projection is delivered ([ADR-004](ADR-004-real-time-communication-delivery.md)). |
| **Discard** | The result is **disposable** — the consumer renders it; it is **never** written back or promoted to truth. |

**Why Queries never persist truth:** a Query is an *observation*; its result is a derived projection of an
already-committed version. Persisting it as truth would create a **second source of truth** and a leak
path — forbidden ([ADR-002 AI-STATE-8](ADR-002-authoritative-game-state.md#14-architectural-invariants-introducedaffirmed), AI-VIS-1). The **only** truth is the Authority's aggregate.

---

## 10. Validation Architecture

Validation is layered; each layer has an owner. All layers precede the Authority's decision.

| Layer | What it checks | Owner |
|-------|----------------|-------|
| **Business validation** | Rule legality of the intent (structural clue rules, guess legality, turn/phase) — [Validation Rules](../../02-business-analysis/09-validation-rules.md), [Business Rules](../../02-business-analysis/02-business-rules.md). | Validation gate + rules core ([ADR-003 R-10/R-04](../03-system-responsibilities.md)). |
| **Architectural validation** | Command well-formedness; targets a valid room; not during recovery. | Admission/validation gate. |
| **Ownership validation** | The Command reaches the **current owning Authority** (correct room, current epoch/fence). | Routing + fence ([ADR-007](ADR-007-room-isolation-distribution.md)). |
| **Participation validation** | The actor is a current participant with the claimed **authoritative** role/team (not client-declared). | Participation ([ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)). |
| **Version validation** | The Command/observation is consistent with the current committed version (stale/duplicate handled). | Coordination/versioning ([ADR-003/004](ADR-004-real-time-communication-delivery.md)). |
| **Visibility validation** | A Query result contains only role-permitted data (no key to non-Spymasters). | Projection Policy ([ADR-006](ADR-006-role-based-information-visibility.md)). |
| **Recovery validation** | No Command is admitted while recovering; recovered state validated before resume. | Recovery ([ADR-005](ADR-005-state-recovery-resilience.md)). |

**Order:** architectural/ownership/participation/recovery gating → business validation → adjudication.
Nothing mutates until adjudication+commit; a rejection at any layer returns a catalogued error and
**changes no state**.

---

## 11. Ordering Guarantees

| Aspect | Guarantee |
|--------|-----------|
| **Command ordering** | **Total order per room** via the single ordered intake ([ADR-003](ADR-003-per-room-coordination-model.md)); no cross-room ordering (isolation). |
| **Version ordering** | Each committed Command **increments the monotonic per-room version** ([ADR-004 §7](ADR-004-real-time-communication-delivery.md#7-versioning-strategy)). |
| **Projection ordering** | Projections inherit the commit's version; presented in version order. |
| **Delivery ordering** | Applied by version (apply-newest-only), not by arrival ([ADR-004](ADR-004-real-time-communication-delivery.md)). |
| **Observation ordering** | Consumers observe in version order; a Query returns a coherent single-version projection. |
| **Concurrent commands** | Serialized per room; first-valid-wins; later re-validated against new state ([ADR-003 §8](ADR-003-per-room-coordination-model.md#8-coordination-lifecycle)). |
| **Duplicate commands** | Idempotent — a duplicate yields **no additional decision** (one decision per Command). |
| **Late commands** | Re-validated against current committed state/version; stale ones rejected. |
| **Replay** | Idempotent + version/turn-bound → no-op/reject; observes only already-committed data. |
| **Recovery** | Commands are **blocked** during recovery; resume after validated completion ([ADR-005](ADR-005-state-recovery-resilience.md)). |
| **Migration** | Commands route to the **new owner**; the fence rejects stale-owner processing; version preserved ([ADR-007](ADR-007-room-isolation-distribution.md)). |

---

## 12. Consistency Model

| Aspect | Guarantee |
|--------|-----------|
| **Strong consistency** | For **Commands/authoritative state** within a room — serialized, atomic, committed ([CB-01…CB-10](../06-consistency-boundaries.md)). |
| **Eventual consistency** | For **Query/projection freshness** — a consumer may briefly lag a version, then catch up; always reflecting *a* committed version. |
| **Projection freshness** | A projection reflects the committed version at answer time (or a client's applied version); never partial/uncommitted. |
| **Read-after-write** | An actor's own committed Command is observed via delivery of the resulting (versioned) projection — **not** by an immediate synchronous read-back from the writer; correctness never depends on read-after-write timing. |
| **Recovery consistency** | Post-recovery, consumers re-snapshot at the recovered version (ADR-004/005); no stale projection is authoritative. |
| **Visibility consistency** | Every read is role-filtered; the key never reaches a non-Spymaster ([ADR-006](ADR-006-role-based-information-visibility.md)). |
| **Distribution consistency** | Per-room strong consistency holds under distribution (single fenced owner); no cross-room consistency ([ADR-007](ADR-007-room-isolation-distribution.md)). |
| **Content consistency** | The pinned dictionary version is immutable for the match; content reads are read-only ([ADR-008](ADR-008-dictionary-content-architecture.md)). |
| **Participant consistency** | Participation/role/team are authoritative; queried as projections; commands change them only via coordination ([ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)). |

**Consistency Before Freshness:** where they conflict, a correct, consistent projection (possibly a
version behind) beats a fresher-but-inconsistent one.

---

## 13. Responsibility Matrix

✓ = participates in that side; **W** = performs authoritative *writes*; **R** = performs *reads/projections*.

| Responsibility | Commands | Queries |
|----------------|:--------:|:-------:|
| **Authority (Room Entity)** | ✓ **W** (admit, adjudicate, commit) | — (never answers queries directly) |
| **Validation Gate** | ✓ (validate/admit-or-reject) | ✓ (authorize a query's role/scope) |
| **Rules Core (Domain Service)** | ✓ (decide outcomes) | — |
| **Projection Policy** | — | ✓ **R** (produce role-filtered projections) |
| **Delivery Boundary** | — (carries Command intents inbound; never executes) | ✓ **R** (carry projections/events outbound) |
| **State Custody / Recovery** | ✓ (record commits; gate commands during recovery) | — (recovery does not answer queries; blocks them until complete) |
| **Content / Dictionary Provider** | — (region *selection* is a Command handled by Authority) | ✓ **R** (read-only content by pinned version) |
| **Participant / Session** | ✓ (join/leave/reconnect/role/team as Commands) | ✓ (presence/participation as projections) |
| **Routing / Ownership (Distribution)** | ✓ (route Command to the owning Authority) | ✓ (route Query to the owner/projection source) |
| **Actors (participant/host/system/bot/AI/client)** | ✓ (propose Commands) | ✓ (issue Queries) |

**Rule:** exactly one responsibility (**Authority**) performs authoritative writes; reads are produced by
the **Projection Policy** and carried by **Delivery**; nothing else writes truth.

---

## 14. Architectural Invariants (AI-CQ-*)

- **AI-CQ-1:** **Queries never mutate truth.**
- **AI-CQ-2:** **Commands never bypass the Authority** — the only write path is admit→validate→adjudicate→commit.
- **AI-CQ-3:** **Each Command is committed at most once** (one decision per Command; idempotent).
- **AI-CQ-4:** **Queries never own state** — results are derived, disposable projections.
- **AI-CQ-5:** **The Authority never answers queries directly** — reads come from projections of committed state (no read-back from the writer).
- **AI-CQ-6:** **A projection never becomes authoritative.**
- **AI-CQ-7:** **Delivery never executes Commands** — it carries intents inbound and projections outbound; it adjudicates nothing.
- **AI-CQ-8:** **Recovery blocks Command admission** (and Query answering) until validated completion.
- **AI-CQ-9:** **Content queries never affect gameplay** — reading content changes no state and no outcome.
- **AI-CQ-10:** **Room ownership defines Command ownership** — a Command is adjudicated only by the room's current fenced Authority.
- **AI-CQ-11:** **Every committed Command increments the per-room version.**
- **AI-CQ-12:** **Gameplay depends only on Commands** — no outcome depends on a Query's presence, freshness, or timing.
- **AI-CQ-13:** **A Query result carries only role-permitted information** (no key to non-Spymasters).
- **AI-CQ-14:** **Commands are ordered per room; Queries are unordered observations** that never enter the write path.

---

## 15. Architecture Fitness Functions (FF-CQ-*)

- **FF-1:** **Every Command reaches exactly one Authority** (the room's current owner) or is rejected/re-routed.
- **FF-2:** **Every committed Command increments the room version** (monotonic).
- **FF-3:** **Queries never change versions** (a Query leaves the version unchanged).
- **FF-4:** **Every projection reflects committed truth** (recompute equality; composes with ADR-006 FF-2).
- **FF-5:** **Recovery rejects Commands** (no admission while recovering; composes with ADR-005 §12).
- **FF-6:** **Duplicate Commands never duplicate decisions** (idempotency).
- **FF-7:** **Visibility never leaks** — no Query/Command-result to a non-Spymaster contains the key (composes with ADR-006 FF-1).
- **FF-8:** **Queries remain read-only** (no state mutation observable from any Query).
- **FF-9:** **No projection becomes authoritative** (no code path reads a projection as truth).
- **FF-10:** **No Command is adjudicated by a non-owning/stale-epoch Authority** (composes with ADR-007 FF-3/7).
- **FF-11:** **Gameplay outcome is independent of Query behavior** (removing/lagging Queries changes no committed result).

Map to [Success Metrics ASM-02/05/06/07](../../06-architecture-governance/04-architecture-success-metrics.md),
[QS-01/02/03/08](../09-quality-attribute-scenarios.md).

---

## 16. Security Analysis

Separating **architectural guarantees** from **future technical controls** (auth/crypto/transport — Non-Goals).

| Threat | Architectural guarantee | Future technical control |
|--------|-------------------------|--------------------------|
| **Command spoofing** | A Command is only an *intent*; the Authority adjudicates against **authoritative** role/team/ownership, never client claims (AI-CQ-2/10). | AuthN/AuthZ (future). |
| **Query abuse** | Queries are read-only, role-filtered, and cannot change state or gameplay (AI-CQ-1/9/12). | Rate limiting (ops). |
| **Projection tampering** | Projections are server-produced; a client-supplied "projection" is never truth (AI-CQ-6). | Integrity/signing (future). |
| **Replay attacks** | Idempotent, version/turn-bound Commands; replays are no-op/stale (AI-CQ-3, FF-6). | Anti-replay transport. |
| **Duplicate submission** | One decision per Command (AI-CQ-3, FF-6). | Transport dedup. |
| **Version confusion** | Monotonic per-room versions; stale/duplicate handled (AI-CQ-11, ADR-004). | — |
| **Visibility bypass** | Every result is role-filtered by inclusion (AI-CQ-13; ADR-006). | Encryption in transit (future). |
| **Unauthorized commands** | Validation checks authoritative role/team/phase/ownership before adjudication (§10). | AuthZ (future). |
| **Unauthorized queries** | A Query returns only the actor's role projection; requesting "as Spymaster" without the role yields an Operative projection (no key). | Access control on subscription (future). |
| **Future authentication** | Attaches at the identity seam; the interaction model is unchanged (Commands/Queries still authoritative-owned/projected). | AuthN (future). |

**Bottom line:** the model guarantees **no client can change truth except by an adjudicated Command**,
**no Query can mutate or leak**, and **no projection is truth** — by construction. Residuals (auth,
signing, crypto, rate limits) are named **future technical controls**.

---

## 17. Trade-off Analysis

- **Correctness:** Maximized — one write path, ordered/committed; reads powerless over truth.
- **Consistency:** Strong for Commands; eventual (bounded) for Query freshness — the right split.
- **Latency:** Commands incur the ordered/commit path (bounded, per small room); Queries are cheap
  projection reads; **read-after-write is via delivery**, not synchronous read-back (a deliberate,
  correctness-preserving choice).
- **Scalability:** Excellent — Commands scale per room (single owner); Queries scale as cacheable
  projections; isolation (ADR-007).
- **Developer experience:** Clear mental model — "to change truth, send a Command to the Authority; to
  observe, read a projection." No ambiguous read/write methods.
- **Testing:** Strong — Commands are deterministic and version-checkable; Queries are pure projections;
  invariants/fitness functions are enumerable.
- **Recovery:** Clean — recovery blocks Commands and re-snapshots Queries (ADR-005).
- **Distribution:** Clean — Commands route to the owner; Queries read projections anywhere; fence enforces
  single writer (ADR-007).
- **Maintainability:** High — read and write concerns are separated and cohesive ([AP-16](../../06-architecture-governance/01-architecture-principles.md)).
- **Future evolution:** Any transport/API/client is just a **carrier** of Commands/Queries; the model
  absorbs REST/SignalR/gRPC/GraphQL/bots/AI without change.

---

## 18. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| A "query" path that mutates (disguised write) | Architecture (critical) | AI-CQ-1/8; FF-8; review against [AAP-04](../../06-architecture-governance/02-architecture-anti-principles.md); all writes are Commands to the Authority. |
| A projection treated as truth (read-model-as-source) | Architecture/Security | AI-CQ-6/9; FF-9; projections derived on demand from committed state. |
| Read-after-write confusion (clients expecting sync read-back) | Consistency/DX | Results observed via delivery; document the model; correctness never depends on sync read-back. |
| Command reaches a non-owner/stale Authority | Ordering/Consistency | Routing + fence (ADR-007); FF-1/10; stale rejected + re-routed. |
| Duplicate/late Commands double-apply | Correctness | Idempotency + version/turn binding (AI-CQ-3); FF-6. |
| Query freshness becomes a business dependency | Correctness (subtle) | AI-CQ-12/FF-11; gameplay decided by Commands only; freshness never a rule input. |
| Hidden info leaks via a query/projection | Security (existential) | AI-CQ-13; FF-7; ADR-006 inclusion filtering. |
| Command admitted during recovery | Recovery | AI-CQ-8; FF-5; admission gated (ADR-005). |
| Rule logic leaking into delivery/query layer | Maintainability/Correctness | Delivery/queries carry/observe only; adjudication is the rules core (AI-CQ-5/7; [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md)). |

---

## 19. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | **Every interaction cleanly classifies as a Command or a Query** (writes vs observations). *(Established in [Command & Query Discovery](../07-command-query-discovery.md).)* | Very High | A "mixed" operation is modeled as a Command whose *result* is observed via delivery — never a read-back write. |
| AS-2 | **Results are observed via delivery**, not synchronous read-back. *(Follows commit-then-broadcast.)* | Very High | If a synchronous ack is desired, it may confirm *admission/commit* but must not return authoritative state as a competing truth. |
| AS-3 | **Actors cannot assert authoritative state** — only propose Commands. *(Server authority, [AP-03](../../06-architecture-governance/01-architecture-principles.md).)* | Fact | — |
| AS-4 | **Projections are cheap to (re)compute** from committed state. *(Small rooms.)* | Very High | If costly, cache role-keyed projections (still non-authoritative). |
| AS-5 | **Command volume per room is human-paced** so the ordered path is not a bottleneck. | High | If not, revisit per-room throughput (unlikely for Codenames). |

---

## 20. Non-Goals

This ADR does **not** define: **REST, HTTP, GraphQL, controllers, frameworks, libraries, DTOs,
serialization, message brokers, or implementation**. It defines **only** the interaction model
(Command/Query separation, ownership, lifecycles, validation, ordering, consistency, contracts). Those
belong to **Technical Design**.

---

## 21. Impact on Software Design

| Area | Constraint imposed by ADR-010 |
|------|-------------------------------|
| **Module boundaries** | Separate **Command handling** (to the Authority) from **Query/projection** (read side); never merge read/write on shared mutable state. |
| **Application layer** | A thin layer that **carries** Commands to the owning Authority and **serves** Queries as projections; it **never adjudicates** (AI-CQ-7). |
| **Domain layer** | The **rules core** decides Commands; it is pure and transport-free ([AP-14](../../06-architecture-governance/01-architecture-principles.md)); it does not answer queries. |
| **Infrastructure layer** | Transports/persistence are **carriers/custodians**; they neither adjudicate Commands nor own projections as truth. |
| **Public interfaces** | Any interface exposes **Commands (intents)** and **Queries (projection reads)** — not CRUD on state. |
| **Repositories** | Hold/record committed state (custody); reads produce projections; no repository is a second source of truth. |
| **Event publishing** | Events are published **after commit** (commit-then-broadcast); they are observations, not commands. |
| **Projection building** | Owned by the Projection Policy from committed state; role-filtered; disposable. |
| **API design (future)** | APIs are **carriers**: endpoints map to Commands or Queries; they must not offer state-assertion (CRUD) or read-back-as-truth. |
| **Testing** | Command determinism/idempotency/version tests; Query read-only/role-filter tests; recovery-gating; no-leak; no-projection-as-truth (FF-CQ-*). |
| **Technical ADRs** | Choose transports/frameworks/serialization as **conforming carriers**; none may redefine the interaction model. |

---

## 22. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | Clue/guess/turn/join/leave/start as intents: [BR-CL, BR-GV, BR-TO/TE, BR-JR, BR-LR, BR-GS](../../02-business-analysis/02-business-rules.md); [Error Catalog](../../02-business-analysis/12-business-error-catalog.md). |
| **Business Invariants** | [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-G2/G7](../../02-business-analysis/10-business-invariants.md), [INV-O1](../../02-business-analysis/10-business-invariants.md). |
| **Architecture Discovery** | [Command & Query Discovery](../07-command-query-discovery.md), [Interactions](../08-interaction-discovery.md), [Responsibilities R-04/R-10/R-11](../03-system-responsibilities.md), [Consistency Boundaries](../06-consistency-boundaries.md), [State Ownership](../05-state-ownership.md). |
| **Engineering Challenges** | [ENG-GP-01/02](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), ENG-ST-01/02, ENG-FP-02/03/04, ENG-RT-01/02. |
| **Quality Attribute Scenarios** | [QS-01/02/03](../09-quality-attribute-scenarios.md), [QS-08](../09-quality-attribute-scenarios.md), [QS-11](../09-quality-attribute-scenarios.md). |
| **ADR-002** | Commands mutate the aggregate at commit; Queries read projections of it. |
| **ADR-003** | Commands are serialized/adjudicated by the single writer. |
| **ADR-004** | Results observed via versioned, role-filtered delivery. |
| **ADR-005** | Recovery blocks Command admission; Queries re-snapshot post-recovery. |
| **ADR-006** | Queries return role-filtered projections; no leak. |
| **ADR-007** | Commands route to the owning fenced Authority; per-room isolation. |
| **ADR-008** | Region selection is a Command; content reads are read-only Queries; content never affects gameplay. |
| **ADR-009** | Join/leave/reconnect/role/team are Commands; presence/participation are Queries. |
| **Governance** | [AP-03/04/06/07/08/14/16](../../06-architecture-governance/01-architecture-principles.md); [AAP-02/04/08/09](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 23. Architecture Review

- **Decision:** **Command/Query Separation with Authority-owned Commands and projection Queries**, realized
  as Commands routed to the owning room Authority (adjudicated/committed/versioned) and Queries answered as
  role-filtered projections of committed state; the two never cross; gameplay depends only on Commands.
- **Confidence:** **High** — it is the explicit naming of the discipline every frozen ADR already assumes;
  alternatives (CRUD/shared-state) are disqualified, others are carriers/substrates under CQS.
- **Remaining risks:** disguised-write "queries" and projection-as-truth regressions (guarded by
  AI-CQ/FF-CQ + review); read-after-write UX expectations (documented — observe via delivery); routing to
  the correct owner (ADR-007 fence).
- **Open questions (delegated, non-blocking):** transport/API/serialization realization; synchronous
  admission-ack shape (may confirm commit, never return competing truth); read-model caching specifics
  (technical design).
- **Review triggers:** any interface offering state-assertion (CRUD) or read-back-as-truth; a requirement
  making gameplay depend on query freshness; introduction of bots/AI actors (they are just Command/Query
  carriers — must conform); authentication (attaches at the seam, model unchanged).
- **Readiness for Software Design:** **READY.** With the interaction model fixed, Software Design can define
  module/layer boundaries, interfaces, and (later) transports/APIs **as conforming carriers** of Commands
  and Queries — completing the ADR-000…ADR-010 foundation.

---

## 24. Adversarial Architecture Review — "Attempt to Break the Interaction Model"

**Attack · Expected Outcome · Architectural Protection · Residual Risk · Mitigation.**

1. **Can a Query change state?**
   - *Expected:* No. *Protection:* Queries are read-only projections (AI-CQ-1; FF-8). *Residual:* disguised-write regression. *Mitigation:* review + FF-8.
2. **Can a Command bypass Authority?**
   - *Expected:* No. *Protection:* the only write path is admit→validate→adjudicate→commit (AI-CQ-2). *Residual:* none. *Mitigation:* —
3. **Can Delivery execute Commands?**
   - *Expected:* No. *Protection:* Delivery carries intents inbound and projections outbound; it never adjudicates (AI-CQ-7; [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md)). *Residual:* rule-leak into delivery. *Mitigation:* review; FF.
4. **Can Projection become Truth?**
   - *Expected:* No. *Protection:* projections are derived/disposable; no read-as-truth path (AI-CQ-6; FF-9). *Residual:* client cache misuse. *Mitigation:* clients re-snapshot; server truth only.
5. **Can Recovery admit Commands?**
   - *Expected:* No. *Protection:* admission gated during recovery (AI-CQ-8; FF-5; ADR-005). *Residual:* none. *Mitigation:* —
6. **Can two Authorities execute the same Command?**
   - *Expected:* No. *Protection:* single fenced owner per room; stale-epoch rejected (AI-CQ-10; ADR-007). *Residual:* fence bug. *Mitigation:* fence realization (technical design).
7. **Can Commands execute out of order?**
   - *Expected:* No. *Protection:* total per-room order via single intake (AI-CQ-14; ADR-003). *Residual:* none. *Mitigation:* —
8. **Can stale Queries overwrite new state?**
   - *Expected:* No. *Protection:* Queries never write; apply-newest-only on the client (AI-CQ-1; ADR-004). *Residual:* none. *Mitigation:* —
9. **Can hidden information leak through Queries?**
   - *Expected:* No. *Protection:* role-filtered by inclusion; key only in Spymaster projection (AI-CQ-13; FF-7; ADR-006). *Residual:* transport interception of a Spymaster read. *Mitigation:* encryption (future).
10. **Can Content Queries affect gameplay?**
    - *Expected:* No. *Protection:* content reads are read-only; content never changes state/outcome (AI-CQ-9; [ADR-008](ADR-008-dictionary-content-architecture.md)). *Residual:* none. *Mitigation:* —
11. **Can migration duplicate Commands?**
    - *Expected:* No. *Protection:* idempotent + version/turn-bound; routed to the new owner; fence rejects the old (AI-CQ-3; FF-6; ADR-007). *Residual:* none. *Mitigation:* —
12. **Can replay change outcomes?**
    - *Expected:* No. *Protection:* idempotent, version/turn-bound Commands; replays no-op/reject (AI-CQ-3). *Residual:* none. *Mitigation:* —
13. **Can cached projections become authoritative?**
    - *Expected:* No. *Protection:* caches are role-keyed, versioned, non-authoritative (AI-CQ-6; ADR-004/006). *Residual:* mis-keyed cache. *Mitigation:* FF-7; role+version keys.
14. **Can clients infer hidden information through timing or metadata?**
    - *Expected:* No. *Protection:* projections are shape/size data-independent for unrevealed cards (ADR-006 FF-9); outcomes public only post-commit; no query-timing oracle for the key. *Residual:* side-channel research. *Mitigation:* constant-shape projections; monitoring.
15. **Can duplicate Commands create duplicate business decisions?**
    - *Expected:* No. *Protection:* one decision per Command; idempotency (AI-CQ-3; FF-6). *Residual:* none. *Mitigation:* —
16. **Can Query freshness become a business dependency?**
    - *Expected:* No. *Protection:* gameplay depends only on Commands; no rule reads a Query's freshness (AI-CQ-12; FF-11). *Residual:* a future feature coupling to freshness. *Mitigation:* invariant + review reject such coupling.

**Conclusion:** the interaction model **cannot let a read change truth, a write bypass the Authority, a
projection become authoritative, a Command execute out of order or twice, a Query leak the key, content
affect gameplay, or freshness become a rule** — **by construction** — because **Commands are the sole,
ordered, committed-once write path owned by the room Authority**, **Queries are read-only, role-filtered,
disposable projections of committed truth**, and **the two never cross**. The only genuine residuals —
**transport encryption/authn, fence realization, side-channel hardening, and client cache discipline** —
are **future technical/operational controls**, delegated and named, not weaknesses of the model.

---

## Final Deliverable — Answers

- **What is a Command?** An **Intent** to change a room's authoritative state, adjudicated and committed
  **exactly once** by the room's single Authority (ordered, version-stamped).
- **What is a Query?** A request to **observe** truth, answered as a **read-only, role-filtered projection**
  of committed state — disposable and never authoritative.
- **Why are they fundamentally different?** A Command *changes* truth through the single ordered write path;
  a Query *observes* truth with no power to change it. One is a decision; the other is a view.
- **Why does only the Authority execute Commands?** It is the single writer; centralizing decisions is what
  makes gameplay deterministic, atomic, and fair (ADR-002/003).
- **Why are Queries never authoritative?** They are derived, disposable projections; promoting one to truth
  would create a second source of truth and a leak path — forbidden.
- **Why do Projections exist?** To let one authoritative truth be observed **safely and per-role** — the key
  for Spymasters, never for Operatives (ADR-006).
- **Why does Delivery never execute Commands?** Delivery is a Reader/carrier; adjudication belongs to the
  Authority; mixing them would duplicate rules and risk races/leaks (AI-CQ-7).
- **Why does Recovery block Commands?** Because a Command adjudicated against partial/uncommitted state could
  corrupt outcomes; recovery-before-commands guarantees correctness over speed (ADR-005).
- **Why does room ownership define command ownership?** A Command may be adjudicated only by the room's
  **current fenced Authority**; ownership (ADR-007) determines who may decide — routing follows it.
- **Why do Queries never affect gameplay?** Outcomes are decided by Commands against committed state; a
  stale, slow, or absent Query can never change what happened (AI-CQ-12).
- **How does the architecture support future REST, SignalR, gRPC, GraphQL, background workers, bots, AI
  players, and mobile/web/console clients without changing the model?** All of them are **carriers**: they
  submit **Commands** (intents) to the owning Authority and read **Queries** (projections) — nothing more.
  A bot or AI player is just another actor proposing Commands and observing projections (subject to the same
  validation/visibility); a REST/gRPC/GraphQL endpoint or a SignalR channel is just a transport for Commands
  and Queries. Because the model is defined in terms of **intents, adjudication, commits, versions, and
  role-filtered projections** — all transport-agnostic — any technology that carries those conforms; choosing
  or swapping one is a Technical-Design change **beneath an unchanged interaction model**.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-04 | Initial decision: Command/Query Separation with Authority-owned Commands and projection Queries; lifecycles, validation, ordering, consistency, responsibility matrix, invariants, fitness functions, security & adversarial review, verdict. |
