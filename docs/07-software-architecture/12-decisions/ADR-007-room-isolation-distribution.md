# ADR-007 — Room Isolation & Distribution Architecture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How does Cluely execute thousands of independent rooms while guaranteeing that every room has exactly one Authority, remains completely isolated, and can move between runtime nodes without changing the architectural model?* |
| **Complies with** | [ADR-000](ADR-000-architecture-vocabulary.md), [ADR-001](ADR-001-overall-architecture-style.md), [ADR-002](ADR-002-authoritative-game-state.md), [ADR-003](ADR-003-per-room-coordination-model.md), [ADR-004](ADR-004-real-time-communication-delivery.md), [ADR-005](ADR-005-state-recovery-resilience.md), [ADR-006](ADR-006-role-based-information-visibility.md), [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md). Extends; redefines none. |
| **Scope note** | Defines the **distribution architecture** only — not infrastructure. It chooses **no** cloud provider, container/orchestrator (Kubernetes/Docker), actor runtime (Orleans/Dapr), Redis, Azure/AWS, load balancer, message broker, networking, framework, or deployment ([§20 Non-Goals](#20-non-goals)). |

---

## 1. Executive Summary

Cluely distributes by **making the room the unit of distribution** and **ownership — not placement — the
thing that is authoritative**. Each live room has **exactly one Authority** (the Room Entity of
[ADR-001](ADR-001-overall-architecture-style.md)) that owns and single-writes its
[aggregate](ADR-002-authoritative-game-state.md); rooms **share no authoritative state**
([ADR-002](ADR-002-authoritative-game-state.md), [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md)).
The architecture runs **thousands of independent rooms** by placing each room's Authority on **some
runtime node**, routing that room's traffic to its **current owner**, and — when needed — **moving
ownership** of a room from one node to another via an **atomic, fenced ownership transfer**.

The chosen model is **Owned, Fenced, Movable Room Instances with Deterministic Ownership Resolution**
(a hybrid of *actor-based rooms* + *dynamic placement* + *fencing*): a room is an **owned instance**
addressable by a stable **room identity**; a **placement/ownership directory** resolves *which node
currently owns* a room; a **monotonic ownership fence (epoch)** guarantees that **at most one Authority
may act on a room at a time**, so even under partitions/migrations a stale owner is **rejected** — no
split-brain, no two winners. **Runtime nodes are stateless-of-truth**: they *host* room instances but
hold no cross-room authoritative state; a room can therefore **move between nodes** without changing any
ADR — because **we move ownership, never truth**, and truth (the committed aggregate) is restored via
[ADR-005 checkpoint recovery](ADR-005-state-recovery-resilience.md) at the new owner.

**Why isolation is fundamental:** it is the property that makes correctness, fault-containment, and
scale simultaneously achievable — a room's determinism (ADR-003), recovery (ADR-005), and secrecy
(ADR-006) are all defined *per room*, so isolation lets each room be reasoned about, failed, recovered,
and scaled **independently**. **Why distribution is architectural, not infrastructural:** *where* a room
runs is an infrastructure choice, but *that exactly one Authority owns it*, *how ownership moves*, and
*how stale owners are fenced* are **correctness properties** that no load balancer or orchestrator can
provide by itself. **Why scale must preserve correctness:** a faster or larger deployment that allowed
two Authorities, cross-room coupling, or a version rewind would destroy fairness — so **correctness and
single-Authority win over distribution speed**. **Why ownership matters more than placement:** placement
can change freely (a room may run anywhere); ownership must be **singular and fenced** at all times —
routing follows ownership, never defines it.

> One-line statement: **the room is the distribution unit; exactly one fenced Authority owns each room;
> nodes are stateless-of-truth hosts; move ownership (never truth) atomically with a monotonic fence;
> route to the current owner; recover per room.**

---

## 2. Problem Statement

**Why thousands of simultaneous rooms are hard.** Each room is a small, strongly-consistent,
single-writer unit; running many at once raises **scalability** (spread load across nodes),
**fault isolation** (one node/room failure must not harm others), **ownership** (who is the single
writer *now*), **routing** (get a room's traffic to its current owner), **migration** (move a room
without corruption), **split-brain** (never two owners), and **coordination** (preserve per-room
determinism) — all **at once**.

**Why infrastructure alone cannot solve this.** Load balancers, orchestrators, and brokers can place
processes and route packets, but they **do not know** that a room must have exactly one writer, that a
stale owner must be rejected, that a migration must be atomic and truth-preserving, or that versions must
never rewind. Those are **domain correctness invariants** ([INV-*](../../02-business-analysis/10-business-invariants.md),
[ADR-003/005](ADR-005-state-recovery-resilience.md)). Infrastructure is the *substrate*; this ADR defines
the *contract* the substrate must satisfy — otherwise a routing race or a dead-node takeover could create
two Authorities and two winners ([ENG-CO-02/05, ENG-RE-01, ENG-SC-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)).

---

## 3. Distribution Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Room First** | The room is the unit of ownership, isolation, distribution, and recovery. | Everything (ADR-002/003/005) is defined per room. |
| **Single Authority** | Exactly one Authority owns a room at any instant. | Determinism/correctness ([ADR-003](ADR-003-per-room-coordination-model.md)). |
| **Single Writer** | Only that Authority mutates the room aggregate. | No races ([ADR-002](ADR-002-authoritative-game-state.md)). |
| **Isolation Before Scale** | Rooms share no authoritative state; scale by count, not by coupling. | Fault-containment + horizontal scale ([AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md)). |
| **Ownership Before Location** | *Who owns* a room is authoritative; *where* it runs is not. | Placement can change; ownership must be singular. |
| **Move Ownership, Never Truth** | Migration transfers **ownership**; truth (committed state) is restored, not shipped as a second copy that could diverge. | Prevents divergent writers; composes with recovery. |
| **Rooms Never Share State** | No cross-room authoritative read/write. | Isolation invariant. |
| **Failure Containment** | A room/node failure is contained to the rooms it owned. | Blast-radius control. |
| **No Cross-Room Dependencies** | A room's progress never depends on another room. | Independence/scale. |
| **Deterministic Routing** | Given ownership state, routing to the owner is deterministic and convergent. | Correct delivery of commands. |
| **Room Mobility** | A room can move between nodes without changing the model. | Elasticity/rebalancing/failover. |
| **Stateless-of-Truth Runtime Nodes** | Nodes host room instances but hold no cross-room authoritative state. | Nodes are interchangeable hosts. |
| **Horizontal Scalability** | Add nodes → host more rooms; no shared bottleneck. | Growth ([SCAL-1](../../02-business-analysis/01-software-requirements.md#213-scalability-considerations)). |
| **One Recovery Per Room** | A room recovers as one unit, once, under one owner (ADR-005). | Recovery correctness. |
| **Fencing** | A monotonic ownership epoch rejects stale owners. | Split-brain prevention. |

---

## 4. Candidate Distribution Models

Evaluated for many small, strongly-consistent, single-writer rooms. None dismissed without reasoning.

### DM1 — Single Process (all rooms in one process)
- **Overview:** One process hosts every room.
- **Correctness/Isolation:** Correct per room, but **no fault isolation** (process death loses all rooms) and **no horizontal scale**.
- **Scalability:** Capped by one process/node.
- **Verdict:** The **MVP starting point** (matches [ADR-001](ADR-001-overall-architecture-style.md) modular monolith), but **not** the distribution model; it is the degenerate 1-node case of the chosen model.

### DM2 — Thread Per Room
- **Overview:** A dedicated thread per room within a process.
- **Advantages:** Simple per-room single-writer within one process.
- **Disadvantages:** A *concurrency* tactic, **not** a distribution model; bounded to one node; thread-count limits; says nothing about cross-node ownership/migration.
- **Verdict:** An **implementation option** for realizing single-writer *within* a node (subordinate to ADR-003), not the distribution architecture.

### DM3 — Shared Global State
- **Overview:** Rooms coordinate through shared mutable global state.
- **Correctness/Isolation:** **Fails** — cross-room coupling reintroduces races and destroys isolation ([AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Verdict:** **Disqualified.**

### DM4 — Sharded Rooms (partition rooms across nodes by a shard key)
- **Overview:** Rooms are partitioned across nodes (e.g., by room identity) — each shard owns a subset.
- **Advantages:** Simple horizontal scale; clear ownership per shard; natural isolation.
- **Disadvantages:** Static sharding complicates rebalancing/hot-room handling and failover unless combined with dynamic placement + fencing.
- **Verdict:** A viable **placement strategy** *under* the chosen model (a room maps to an owner); on its own, needs dynamic ownership transfer + fencing → folded into DM10.

### DM5 — Actor-Based Rooms (each room is an addressable, single-writer instance)
- **Overview:** A room is a first-class **owned instance** with a single mailbox/writer, addressable by identity, placeable on any node.
- **Advantages:** Perfect fit for single-Authority/single-writer (ADR-001/003); natural per-room isolation, mobility, and lifecycle; the concurrency shape ADR-001 already adopted.
- **Disadvantages:** Requires an **ownership/placement directory** and **fencing** to guarantee single-owner across nodes (added here).
- **Verdict:** **Selected base** — the room-instance model; completed by DM8 (placement) + DM11 (fencing).

### DM6 — Process Per Room
- **Overview:** One OS process per room.
- **Advantages:** Strong isolation.
- **Disadvantages:** Very heavy for thousands of tiny, short-lived rooms; process overhead dominates; a *technology* granularity choice, not required by the architecture.
- **Verdict:** An **implementation option**; the architecture needs an *owned instance*, not necessarily an OS process.

### DM7 — Node Affinity (a room prefers/sticks to a node)
- **Overview:** A room is affined to a node (sticky ownership).
- **Advantages:** Cache locality; stable routing.
- **Disadvantages:** Alone, it doesn't handle failover/rebalance; affinity is a **placement heuristic** under the model.
- **Verdict:** Folded into DM8 placement.

### DM8 — Dynamic Placement (owner chosen/changed at runtime)
- **Overview:** A room's owning node is chosen at creation and may change (rebalance/failover) via a placement/ownership directory.
- **Advantages:** Elasticity, hot-room handling, failover; mobility.
- **Disadvantages:** Requires atomic transfer + fencing to avoid two owners.
- **Verdict:** **Selected** as the placement/ownership-resolution mechanism.

### DM9 — Consistent Hashing (map room identity → node)
- **Overview:** Hash room identity to a node ring for placement.
- **Advantages:** Even, low-coordination placement; smooth membership changes.
- **Disadvantages:** A **placement algorithm**, not a full model; must still enforce single-owner + fencing during ring changes.
- **Verdict:** A viable **placement algorithm** under DM8; not the architecture by itself.

### DM10/DM11 — Hybrid: Owned, Fenced, Movable Room Instances with Deterministic Ownership Resolution (FINAL)
- **Overview:** **DM5 (owned room instances)** + **DM8 (dynamic placement, optionally DM9 hashing/DM4 sharding as the algorithm)** + **fencing (monotonic ownership epoch)** + **deterministic routing to the current owner** + **per-room recovery (ADR-005)**.
- **Advantages:** Single fenced Authority per room; full isolation; mobility without model change; failover/rebalance/hot-room handling; scales by room count; recovery composes cleanly.
- **Verdict:** **This is the decision.**

### Evaluation summary

| Criterion | DM1 Single | DM3 Shared | DM4 Shard | **DM5+DM8+Fence (chosen)** | DM6 ProcPerRoom |
|-----------|:----------:|:----------:|:---------:|:--------------------------:|:---------------:|
| Correctness | 5 | 1 | 4 | **5** | 5 |
| Scalability | 1 | 3 | 4 | **5** | 4 |
| Recovery | 4 | 2 | 4 | **5** | 4 |
| Complexity (5=lowest) | 5 | 3 | 4 | **3** | 2 |
| Isolation | 4 | 1 | 5 | **5** | 5 |
| Developer Experience | 5 | 2 | 4 | **4** | 3 |
| Testing | 5 | 2 | 4 | **4** | 3 |
| Migration | 1 | 2 | 3 | **5** | 2 |
| Future growth | 1 | 2 | 4 | **5** | 3 |

---

## 5. Final Distribution Model

**Adopt DM10/DM11 — Owned, Fenced, Movable Room Instances with Deterministic Ownership Resolution.** A
room is an **owned instance** addressable by stable **room identity**; a **placement/ownership directory**
records the room's **current owning node** and a **monotonic ownership epoch (fence)**; traffic **routes to
the current owner**; ownership can **move atomically** (rebalance/failover) with the fence rejecting any
stale owner; each room **recovers per ADR-005** at its owner. The MVP runs as a **single node** (DM1 as the
1-node case); the same model scales to many nodes with **no ADR change**.

- **Why it fits Cluely:** it makes single-Authority, isolation, and mobility **structural** while scaling
  by room count — exactly the shape ADR-001 chose (single-writer room entity) extended across nodes.
- **Why it preserves every prior ADR:**
  - [ADR-001](ADR-001-overall-architecture-style.md): the owned room instance **is** the Room Entity; monolith today, distributable tomorrow — unchanged.
  - [ADR-002](ADR-002-authoritative-game-state.md): one aggregate per room; nodes hold no cross-room truth.
  - [ADR-003](ADR-003-per-room-coordination-model.md): single fenced owner ⇒ single serialized writer preserved.
  - [ADR-004](ADR-004-real-time-communication-delivery.md): per-room versioning is unchanged; clients re-snapshot after migration/recovery.
  - [ADR-005](ADR-005-state-recovery-resilience.md): per-room, single-owner, deterministic recovery — now with fencing to guarantee the single owner across nodes.
  - [ADR-006](ADR-006-role-based-information-visibility.md): projections are per-room and recomputed; migration never exposes the key.
  - [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md): participation is room-scoped; it moves with the room; continuity tokens re-bind at the new owner.
- **Why it scales:** add nodes → host more room instances; no shared mutable bottleneck; isolation makes
  it linear in rooms.
- **Why it simplifies recovery:** recovery is already per-room and single-owner (ADR-005); fencing supplies
  the missing cross-node single-owner guarantee, so recovery composes without change.
- **Why it preserves room independence:** rooms never share authoritative state; a room's owner, version,
  recovery, and secrecy are entirely its own.

---

## 6. Distribution Unit

The **distribution unit is the Room** (its aggregate + Authority + participation records). Per artifact:

| Artifact | Distributed? | Movable? | Replicated? | Reconstructed? | Owner |
|----------|:------------:|:--------:|:-----------:|:--------------:|-------|
| **Room Aggregate** (authoritative state) | Yes (one per room, on its owner) | **Yes** (with ownership) | **No** (single authoritative copy) | **Yes** (via ADR-005 recovery at new owner) | Room Authority |
| **Authority** (Room Entity) | Yes (one per room) | **Yes** (ownership transfer) | **No** (exactly one) | Re-established on migration/recovery | The runtime hosting the owner |
| **Participation Records** | With the room | **Yes** (move with the room) | No | Recovered with the aggregate | Room Authority |
| **Dictionary** | Shared, read-only content | N/A (not owned per room) | **May be replicated/cached** (read-only) | Re-referenced by pinned version | Content provider (ADR-008) |
| **Projection** | Derived per room, per role | N/A (recomputed) | No (recomputed) | **Recomputed** at owner/delivery | Delivery boundary (ADR-006) |
| **Delivery** (subscriptions/streams) | Per room, at the owner | Re-established on move | No | Re-established | Delivery boundary (ADR-004) |
| **Recovery** | Per room | N/A | No | The per-room recovery operation | Room Authority (ADR-005) |
| **Commands (Intents)** | Routed to the owner | Follow the room | No | N/A | Admitted by the Authority (ADR-003) |
| **Queries** | Served by the owner (projections) | Follow the room | No | N/A | Delivery/owner |
| **Connections** | At the current owner | Re-established on move (reconnect) | No | Re-established | Connection mgmt (ADR-009) |

**Rule:** authoritative artifacts have **exactly one** location (the owner) and **move with ownership**;
read-only content (dictionary) may be replicated; derived artifacts (projections) are **recomputed**, never
shipped as truth.

---

## 7. Room Ownership Model

- **What is ownership?** The exclusive right and accountability to be a room's **single Authority** — the
  sole admitter/adjudicator/writer of its aggregate ([ADR-003](ADR-003-per-room-coordination-model.md)).
- **Who owns a room?** Exactly **one** runtime-hosted Authority instance at any instant.
- **How is ownership established?** At room creation, the placement mechanism (§8) assigns an owner and an
  initial **ownership epoch**; the directory records `(room → owner, epoch)`.
- **Can ownership change?** **Yes** — for rebalancing, hot-room relief, or failover.
- **When?** On explicit rebalance decisions, on owner/node failure (via recovery), or on graceful drain.
- **How?** Via an **atomic, fenced ownership transfer** (§10): the epoch is **monotonically incremented**;
  the new owner acts only under the new epoch; the old owner is **fenced out**.
- **Who authorizes it?** The **ownership/placement authority** (the directory + transfer protocol) — an
  architectural role, not a specific technology; it authorizes *placement*, never *game outcomes*.
- **What guarantees exist?** At most one Authority may **act** on a room at a time (fencing); ownership
  changes are atomic and epoch-ordered; routing follows ownership.
- **Can ownership be shared?** **Never.** Shared ownership would mean two writers — forbidden
  ([AI-DIST-1](#14-architectural-invariants-ai-dist-)).

---

## 8. Runtime Placement

- **How rooms are placed:** each room is assigned an owning node by a **placement decision** (e.g.,
  least-loaded, affinity, or consistent-hashing of room identity — the *algorithm* is an implementation
  choice under this model).
- **How new rooms are assigned:** at creation, the placement mechanism picks an owner and records
  `(room → owner, epoch=initial)`.
- **How placement decisions are made:** by the placement authority using load/affinity/health signals
  (architecturally: a decision that yields exactly one owner; the heuristic is deferred).
- **How placement changes:** via ownership transfer (§10) for rebalance/failover/drain.
- **What is stable:** the **room identity** (never changes) and the **invariant of single ownership**.
- **What is transient:** the **owning node** (placement) and the physical location — these may change
  freely; **ownership (with its epoch) is authoritative, placement is not** (Ownership Before Location).

---

## 9. Routing Model

- **How does traffic reach the correct room?** By resolving the room's **current owner** from the
  placement/ownership directory and routing the room's Intents/connections there.
- **Who resolves routing?** The **routing role** consults the ownership directory (the source of truth for
  *current owner + epoch*); routing **follows** ownership, it **never defines** it.
- **Can routing change?** Yes — after migration/failover, the directory reflects the new owner and routing
  converges to it.
- **Can routing be cached?** Yes, as a **non-authoritative** hint keyed by ownership epoch; a cache pointing
  at a stale owner is **corrected by fencing** (the stale owner rejects the request; the client/router
  re-resolves).
- **What happens after migration?** Routing re-resolves to the new owner; in-flight requests to the old
  owner are **fenced/rejected** and retried at the new owner (idempotent — ADR-003/004).
- **What happens after recovery?** Same — the recovered owner is recorded in the directory; clients
  **re-snapshot** at the recovered version (ADR-004/005).
- **How is stale routing handled?** A request arriving at a **non-owner or stale-epoch** owner is
  **rejected** (fencing, §11); the caller re-resolves and retries; because Intents are idempotent and
  versioned, no double effect occurs.

---

## 10. Ownership Transfer

Conceptual lifecycle (atomic, fenced; technology-neutral):

```
Current Authority (epoch N) → Freeze (quiesce: stop admission/delivery for the room)
   → Validate (state consistent at a committed version) → Transfer Ownership (directory: owner=new, epoch=N+1)
   → Verify (new owner restores/holds state at committed version; single owner) → Resume (route to new owner; clients re-snapshot)
```

| Aspect | Definition |
|--------|-----------|
| **Preconditions** | The room is at a **committed version**; exactly one current owner (epoch N); a healthy target for the new owner. |
| **Validation** | State is consistent (last committed version); no uncommitted work is treated as committed (ADR-003/005). |
| **Consistency** | The transfer is **atomic**: the directory advances to `(new owner, epoch N+1)` as a single step; there is never a window where two epochs are both "current". |
| **Fence** | Epoch **monotonically increments**; the old owner (epoch N) can no longer act on the room (§11). |
| **Failure** | If transfer fails mid-way, **no new owner is acknowledged**; the room stays owned by the current owner (epoch N) **or** is recovered per ADR-005 under a single new owner — never two. |
| **Rollback** | Because ownership advances only when the new owner is verified, a failed transfer **rolls back to the single prior owner** (or triggers recovery) — atomicity guarantees no dual ownership. |
| **Forbidden transitions** | Two owners "current" simultaneously; a lower epoch resuming after a higher epoch exists; transferring uncommitted state as committed; changing game state during transfer (migration moves **ownership**, not truth — the aggregate's committed content is preserved). |

**Key property:** migration **freezes → validates → transfers ownership (epoch++) → verifies → resumes**;
it **never mutates game state** and **never yields two acting Authorities**.

---

## 11. Fencing Strategy

- **What is fencing?** A **monotonic ownership epoch** attached to a room's ownership: every authoritative
  action/commit for a room is associated with the **current epoch**, and any action from a **lower/stale
  epoch** is **rejected**.
- **Why necessary?** Under partitions, slow nodes, or migration, an **old owner might believe it still
  owns** the room. Without fencing, it could write → two Authorities → split-brain → two winners. Fencing
  makes the stale owner's writes **provably invalid**.
- **How does it prevent split-brain?** Only **one epoch is current**; ownership transfer increments it;
  the directory records the current epoch. Any writer must present the **current** epoch to act; a stale
  writer (old epoch) is **fenced out** — its commits/deliveries are refused. Thus **at most one Authority
  can act** even if two believe they own the room.
- **How is stale ownership rejected?** At the **commit/admission boundary** (ADR-003) and the **delivery
  boundary** (ADR-004): actions/commits/artifacts carrying a non-current epoch are refused; the stale owner
  learns it is fenced and stands down.
- **How does it compose with recovery?** Recovery (ADR-005) re-establishes **one** owner **and** advances
  the epoch, so a pre-failure owner that "comes back" is at a **stale epoch** and cannot act — recovery and
  fencing together guarantee the single-owner invariant across failure/migration.

*(The epoch is an architectural concept — a monotonic ownership token. Its concrete realization —
lease, term number, generation counter — is an implementation detail deferred to technical design.)*

---

## 12. Failure Domains

| Domain | Expected behavior | Isolation guarantee | Recovery expectation | Residual risk |
|--------|-------------------|---------------------|----------------------|---------------|
| **Room failure** | That room's Authority recovers (ADR-005). | Room-scoped; others unaffected. | Checkpoint recovery at the (same/new) owner. | Durable-record freshness. |
| **Node failure** | Rooms it owned are re-owned elsewhere (new epoch) and recovered per room. | Only that node's rooms affected. | Per-room recovery under new single owner. | Transient unavailability of those rooms. |
| **Application failure** | All rooms recover per room on restart/relocation. | Rooms independent. | Per-room recovery (eager/lazy). | Recovery storms → lazy recovery. |
| **Ownership failure** (owner unreachable) | Directory reassigns ownership (epoch++); old owner fenced. | Single owner preserved. | New owner recovers the room. | Reassign-before-truly-dead → old owner fenced (safe). |
| **Migration failure** | Atomic transfer rolls back / recovers under one owner. | No dual ownership. | Room stays/recovers with one owner. | Extra delay. |
| **Routing failure** (stale/wrong route) | Non-owner/stale-epoch rejects; caller re-resolves; retries idempotently. | No wrong-room writes. | Convergent routing. | Temporary mis-route → rejected, not applied. |
| **Network partition** | At most one epoch is current; a partitioned old owner is fenced; only the side holding current ownership may act. | No split-brain. | Recovery/reassignment on the surviving side. | Minority-side rooms unavailable until healed. |
| **Future cluster failure** | Per-room re-ownership + fencing + recovery across surviving nodes. | Room isolation + single owner. | Per-room recovery. | Depends on directory/fence availability (technical design). |

---

## 13. Scalability Model

- **How it scales:** by **room count** — add nodes to host more room instances; no shared mutable state, so
  no central bottleneck (linear in rooms, [SCAL-1](../../02-business-analysis/01-software-requirements.md#213-scalability-considerations)).
- **Scaling limits:** bounded by the **placement/ownership directory** and routing capacity (a read-mostly,
  small-per-room concern) and per-node room capacity; each room is tiny and short-lived, so per-node density
  is high.
- **Independent scaling:** rooms scale independently; delivery, recovery, and secrecy are per room; content
  (dictionary) scales as read-mostly, replicable data (ADR-008).
- **Hot rooms:** a room's load is bounded by its small size (≤ ~20 players); "hot" is mild — but placement
  can move a heavy room to a less-loaded node (ownership transfer).
- **Cold/idle rooms:** reclaimed by expiry ([ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)/[BR-RX](../../02-business-analysis/02-business-rules.md)); ownership released.
- **Burst creation:** new rooms are placed across nodes by the placement mechanism; no global lock (creation
  is per-room).
- **Future geo-distribution:** rooms are self-contained; a room can be owned near its participants; **no
  cross-room, cross-region coordination** is introduced (a room is single-region-owned at a time).
- **Future dedicated room servers:** a room instance can be hosted on a dedicated server — same model
  (ownership + fence).
- **Future autoscaling:** scale nodes up/down; drain moves ownership; the model is unchanged (the *how* of
  autoscaling is Non-Goals).

---

## 14. Architectural Invariants (AI-DIST-*)

- **AI-DIST-1:** **Exactly one Authority owns a room** at any instant (never zero-acting-and-two, never two).
- **AI-DIST-2:** **Exactly one writer** exists per room (the owner).
- **AI-DIST-3:** **Rooms never share authoritative state**; no cross-room authoritative read/write.
- **AI-DIST-4:** **Recovery remains room-scoped** and single-owner (composes with ADR-005).
- **AI-DIST-5:** **Migration never changes game state** — it moves ownership, preserving the committed aggregate.
- **AI-DIST-6:** **Routing never changes authority** — it follows ownership; it never confers or defines it.
- **AI-DIST-7:** **Distribution never changes versions** — per-room versions are preserved across placement/migration/recovery.
- **AI-DIST-8:** **Ownership transfer is atomic and fenced** (monotonic epoch); no dual-current-epoch window.
- **AI-DIST-9:** **No room executes on two Authorities simultaneously** — a stale-epoch owner is fenced out.
- **AI-DIST-10:** **Cross-room communication never affects gameplay** — rooms are independent.
- **AI-DIST-11:** **Placement is transient; ownership (with epoch) is authoritative** — location can change without changing truth.
- **AI-DIST-12:** **Migration/failover never changes participant identity** or role/team (composes with ADR-009).
- **AI-DIST-13:** **Only the current-epoch owner may commit or publish** for a room (fence at commit/delivery boundaries).

---

## 15. Architecture Fitness Functions (FF-DIST-*)

- **FF-1:** **Every room has exactly one owner** (directory shows one current `(owner, epoch)` per room).
- **FF-2:** **Every command reaches the owning Authority** (or is rejected/re-resolved), never a non-owner.
- **FF-3:** **No room exists simultaneously on two nodes as acting owner** (stale-epoch actions rejected).
- **FF-4:** **Migration preserves the room version** (post-migration version == pre-migration committed version).
- **FF-5:** **Recovery remains deterministic after migration** (same durable record ⇒ same restored state, on any node).
- **FF-6:** **No cross-room data access** occurs (isolation check).
- **FF-7:** **No split-brain:** two owners never both commit for a room (epoch monotonicity + fence).
- **FF-8:** **Routing converges** to the current owner after any placement change.
- **FF-9:** **Ownership is traceable** (every commit associated with `(room, epoch, owner)`).
- **FF-10:** **Distribution is deterministic** — placement/migration never alters outcomes, versions, or projections.
- **FF-11:** **No non-Spymaster artifact carries the key during/after migration** (composes with ADR-006 FF-1).

Map to [Success Metrics ASM-02/05/06/07](../../06-architecture-governance/04-architecture-success-metrics.md),
[QS-10](../09-quality-attribute-scenarios.md), [QS-06](../09-quality-attribute-scenarios.md).

---

## 16. Security Analysis

Separating **architectural guarantees** from **future technical controls** (infra/crypto/auth — Non-Goals).

| Threat | Architectural guarantee | Future technical control |
|--------|-------------------------|--------------------------|
| **Split-brain** | Monotonic epoch fence: at most one current owner may act; stale owner rejected (AI-DIST-8/9/13). | Lease/consensus realization (technical design). |
| **Ownership spoofing** | Ownership is recorded/authorized by the placement authority; a node cannot self-declare ownership and act without the current epoch (fenced). | Authenticated node membership. |
| **Routing manipulation** | Routing follows the ownership directory; a mis-routed request to a non-owner/stale-epoch is **rejected** (AI-DIST-6, FF-2/3). | Signed/authenticated routing. |
| **Unauthorized migration** | Ownership transfer is an authorized, atomic, fenced operation; a rogue transfer without the fence cannot make a second owner act. | Access-controlled placement ops. |
| **Replay** | Intents/commits are idempotent and versioned + epoch-bound; replays are stale/no-op (ADR-003/004). | Anti-replay transport. |
| **Duplicate ownership** | Forbidden by AI-DIST-1/8; fence rejects the second. | — |
| **Cross-room leakage** | No cross-room authoritative access (AI-DIST-3/10, FF-6); per-room projections (ADR-006). | — |
| **Distributed denial (flooding)** | Isolation contains impact to targeted rooms; per-room bounds. | Rate limiting/quotas (ops). |
| **Future authentication** | Attaches at the identity seam; distribution model unchanged. | AuthN/AuthZ (future). |
| **Future encryption** | Distribution is agnostic to at-rest/in-transit crypto. | Crypto (technical design/ops). |

**Bottom line:** the architecture guarantees **no two Authorities act on a room** and **no cross-room
leakage**, even under partitions/migration — via the **monotonic ownership fence** and **room isolation**.
Residuals (fence realization, node authentication, crypto) are named **future technical controls**.

---

## 17. Trade-off Analysis

- **Correctness:** Maximized — single fenced owner preserves determinism/consistency across nodes.
- **Scalability:** Excellent — linear by room count; no shared bottleneck.
- **Availability:** A room is briefly unavailable during migration/failover recovery; other rooms stay up.
- **Latency:** Steady-state latency is local to the owner; migration adds a bounded pause; routing adds a lookup (cacheable).
- **Recovery:** Composes cleanly (ADR-005) — fencing supplies the cross-node single-owner guarantee.
- **Operational complexity:** Moderate — a placement/ownership directory + fence + routing; deferred to MVP (single node) and grown later.
- **Migration:** First-class (freeze→transfer→verify→resume) — enables rebalance/failover/drain.
- **Developer experience:** Clear — "the room is owned; route to the owner; ownership moves atomically."
- **Testing:** Strong — fencing/migration/recovery are simulable; determinism-after-migration is checkable.
- **Maintainability:** High — nodes are interchangeable hosts; truth lives in the room aggregate.
- **Future evolution:** The model absorbs orchestration, actor runtimes, geo-distribution, and dedicated servers as **placement/fence realizations** without changing the architecture.

---

## 18. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| Two owners act (fence gap) | Architecture (critical) | Monotonic epoch enforced at commit/delivery boundaries (AI-DIST-13); realization via lease/term (technical design); FF-7. |
| Ownership directory unavailable | Operational | Directory is read-mostly and small; make it highly-available (technical design); rooms already owned keep operating until a change is needed. |
| Migration loses the tail | Recovery | Migrate only at a committed version; recover via ADR-005; move ownership, not uncommitted work. |
| Recovery storms on node loss | Scalability | Lazy/on-access recovery; spread re-ownership. |
| Hot-room imbalance | Scalability | Placement can move a room (ownership transfer); rooms are small anyway. |
| Cross-room coupling creeps in | Isolation | AI-DIST-3/10; FF-6; review against [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md). |
| Stale routing causes errors | Operational | Fencing rejects stale-owner writes; routing re-resolves; idempotent retries (FF-2/8). |
| Geo/multi-region complexity | Evolution | A room is single-region-owned; no cross-region room coordination introduced; revisit for multi-region as a future ADR. |
| Testing distributed edge cases | Testing | Simulate partitions/migrations/failover; assert single-owner, version-preservation, no-leak (FF-DIST-*). |

---

## 19. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | A **placement/ownership directory** and a **monotonic ownership fence** can be realized. *(Deferred to technical design; the model requires them for multi-node.)* | High | Without a fence, multi-node is unsafe (split-brain) → stay single-node (DM1) until available. |
| AS-2 | **Rooms are independent** (no cross-room rule). *(Fact from business scope/isolation.)* | Fact | — |
| AS-3 | **A room fits on one node** and a single owner suffices at a time. | Very High | If a room needed multi-node compute (not for a 25-card game), the model would need rethinking — not applicable. |
| AS-4 | **Recovery + fence** together guarantee single acting owner across failure/migration. | High | If the fence can't be made reliable, dual-owner risk returns → restrict to single node. |
| AS-5 | **Placement can change** without changing truth (move ownership, recover state). | Very High | If truth had to be shipped live (not recovered), divergence risk rises — the model deliberately avoids this. |
| AS-6 | **MVP runs single-node** (DM1) and grows to multi-node later without ADR change. | Fact (design intent) | — |

---

## 20. Non-Goals

This ADR does **not** decide: **cloud provider, containers, VMs, Kubernetes, Docker, Redis, Azure, AWS,
Orleans, Dapr, load balancers, networking, infrastructure, frameworks, deployment, observability
implementation, or autoscaling implementation**. It defines **only** the distribution architecture
(unit, ownership, placement, routing, transfer, fencing, invariants). Those platforms must **conform to**
this model (Technical Design).

---

## 21. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-007 |
|-------------|-------------------------------|
| **ADR-008 Dictionary Architecture** | The dictionary is **shared, read-only, replicable content** — **not** a per-room owned artifact; it must never couple rooms or influence ownership; the pinned version travels with the room aggregate. |
| **ADR-010 Command/Query Strategy** | Commands route to the **owning** Authority (current epoch); queries are served by the owner from committed projections; both must tolerate re-resolution after migration. |
| **Software Design / C4 Diagrams** | Introduce the **room instance / owner**, **placement-ownership directory**, **routing**, and **fence** as logical components; nodes are stateless-of-truth hosts. |
| **Deployment Design** | Any topology (single node → cluster → multi-region) must preserve single fenced ownership and room isolation. |
| **Persistence Design** | The durable record (ADR-005) must be reachable by whichever node owns the room after migration; persistence must not become cross-room shared mutable truth. |
| **Technical Design** | Choose the concrete placement algorithm, directory, fence (lease/term), routing, and (optional) actor runtime — all conforming to AI-DIST-*. |
| **Testing** | Partition/migration/failover simulations; single-owner, version-preservation, no-leak, routing-convergence (FF-DIST-*). |
| **Operations** | Monitor ownership uniqueness, fence/epoch health, migration success, routing convergence, per-room recovery. |
| **Future Matchmaking** | Creates/places rooms; unchanged ownership/isolation model. |
| **Future Multi-Region** | A room is single-region-owned at a time; no cross-region room coordination; a future ADR governs region placement. |

---

## 22. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | [BR-RC](../../02-business-analysis/02-business-rules.md) (room creation/code), [BR-RX](../../02-business-analysis/02-business-rules.md) (expiry), [BR-HM](../../02-business-analysis/02-business-rules.md) (host — a room-role, distinct from node ownership). |
| **Business Invariants** | [INV-R1](../../02-business-analysis/10-business-invariants.md) (one host), [INV-G2/G7](../../02-business-analysis/10-business-invariants.md), [INV-O1](../../02-business-analysis/10-business-invariants.md) (single result), [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-P4](../../02-business-analysis/10-business-invariants.md) (one connection). |
| **Engineering Challenges** | [ENG-SC-01/02/03/04](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), ENG-CO-02/05, ENG-RE-01, ENG-RM-02. |
| **Quality Attribute Scenarios** | [QS-10](../09-quality-attribute-scenarios.md) (concurrent rooms/scale), [QS-06](../09-quality-attribute-scenarios.md) (recovery), [QS-01](../09-quality-attribute-scenarios.md) (no leak), [QS-05/08](../09-quality-attribute-scenarios.md). |
| **State Ownership** | [05 State Ownership](../05-state-ownership.md) (single owner per state; no cross-room). |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md) (strong within a room; **none across rooms**). |
| **ADR-000** | *Room Isolation, Authority, Single Writer, Aggregate, Determinism, Room Entity, Deployment Unit/Topology* used as defined. |
| **ADR-001/002/003/004/005/006/009** | Owned room instance = Room Entity; one aggregate; single serialized writer; per-room versioning; per-room recovery; per-room projections; room-scoped participation — all preserved across nodes. |
| **Governance** | [AP-03/04/06/07/12/18](../../06-architecture-governance/01-architecture-principles.md); [AAP-05/08/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 23. Architecture Review

- **Decision:** **Owned, Fenced, Movable Room Instances with Deterministic Ownership Resolution** — the
  room is the distribution unit; exactly one fenced Authority owns each room; nodes are stateless-of-truth
  hosts; ownership (with a monotonic epoch) is authoritative while placement is transient; ownership moves
  atomically (freeze→transfer→verify→resume) with fencing preventing split-brain; routing follows the
  owner; recovery is per-room. MVP = single node; scales to many nodes with no ADR change.
- **Confidence:** **High** — it is the minimal extension of ADR-001/003 (single-writer room entity) across
  nodes that preserves determinism, isolation, recovery, and secrecy; alternatives are disqualified
  (shared state), bounded to one node (single process/thread), or mere placement algorithms.
- **Remaining risks:** **fence realization reliability** (the crux — technical design); ownership-directory
  availability; recovery storms — all mitigated and delegated.
- **Open questions (delegated, non-blocking):** concrete placement algorithm (hashing/least-loaded), fence
  realization (lease/term/generation), directory technology, routing/cache specifics, and multi-region
  placement (future ADR) — all **implementation/technology**, not architecture.
- **Review triggers:** moving beyond single-node (fence becomes mandatory); multi-region requirements;
  dedicated room servers; extreme per-room load (not expected); a business change introducing cross-room
  interaction (would violate AI-DIST-3/10 — reopen at business level first).
- **Readiness for ADR-008:** **READY.** Distribution fixes that the **room aggregate** is the movable,
  owned unit and that content must be **shared read-only** and non-coupling — exactly the constraints
  **ADR-008 (Dictionary Architecture)** must satisfy (versioned, replicable, per-room-pinned content that
  never couples rooms or influences ownership).

---

## 24. Adversarial Architecture Review — "Attempt to Break the Distribution Model"

**Attack · Expected Outcome · Architectural Protection · Residual Risk · Mitigation.**

1. **Can two Authorities own one room?**
   - *Expected:* No. *Protection:* AI-DIST-1/8; monotonic epoch — only one current owner may act. *Residual:* fence realization bug. *Mitigation:* reliable lease/term (technical design); FF-7.
2. **Can routing send commands to the wrong room?**
   - *Expected:* No. *Protection:* routing resolves by room identity → current owner; a non-owner rejects (FF-2). *Residual:* misconfig. *Mitigation:* fencing + re-resolution; FF-8.
3. **Can migration duplicate gameplay?**
   - *Expected:* No. *Protection:* migrate only at a committed version; move ownership, not uncommitted work; committed events fold once (ADR-005 AI-REC-13; AI-DIST-5). *Residual:* none. *Mitigation:* —
4. **Can ownership transfer lose state?**
   - *Expected:* No. *Protection:* transfer preserves the committed aggregate; the new owner restores/holds it at the committed version; recovery covers node loss (AI-DIST-5/7). *Residual:* durable-record loss. *Mitigation:* persistence durability (technical design).
5. **Can recovery restore to the wrong node?**
   - *Expected:* Harmless. *Protection:* recovery restores the **room aggregate** deterministically on **whichever** node now owns it; node identity is irrelevant to truth (AI-DIST-11; ADR-005 FF-3). *Residual:* none. *Mitigation:* —
6. **Can stale routing reach an old owner?**
   - *Expected:* Rejected. *Protection:* the old owner is at a stale epoch → fenced; it refuses the request (AI-DIST-9/13). *Residual:* brief mis-route. *Mitigation:* re-resolve + idempotent retry.
7. **Can two recoveries execute after migration?**
   - *Expected:* No. *Protection:* recovery is single-owner and serialized under the current epoch (ADR-005 AI-REC-10 + AI-DIST-8). *Residual:* none. *Mitigation:* —
8. **Can rooms read each other's state?**
   - *Expected:* No. *Protection:* no cross-room authoritative access (AI-DIST-3/10, FF-6). *Residual:* shared-infra contention (operational). *Mitigation:* isolation + resource bounds.
9. **Can a node failure affect unrelated rooms?**
   - *Expected:* No. *Protection:* only that node's owned rooms are affected; others independent (failure containment, AI-DIST-3). *Residual:* shared-directory dependency. *Mitigation:* HA directory (technical design).
10. **Can dictionary updates break room isolation?**
    - *Expected:* No. *Protection:* the dictionary is shared read-only content pinned per match; content never couples rooms or influences ownership (§21; [INV-D1/D3](../../02-business-analysis/10-business-invariants.md)). *Residual:* none. *Mitigation:* ADR-008 conforms.
11. **Can routing change game versions?**
    - *Expected:* No. *Protection:* versions are per-room and preserved across placement/migration (AI-DIST-7, FF-4); routing carries no authority. *Residual:* none. *Mitigation:* —
12. **Can migration reveal hidden information?**
    - *Expected:* No. *Protection:* migration moves ownership of the aggregate (key stays server-side); projections are recomputed and re-filtered at the new owner (AI-DIST-5; ADR-006 FF-11). *Residual:* transport interception. *Mitigation:* encryption (future).
13. **Can ownership exist without fencing?**
    - *Expected:* No (multi-node). *Protection:* ownership carries an epoch; acting requires the current epoch (AI-DIST-8/13). *Residual:* single-node MVP has trivial fence. *Mitigation:* enforce fence before enabling multi-node.
14. **Can split-brain create two winners?**
    - *Expected:* No. *Protection:* only the current-epoch owner may commit; a partitioned stale owner is fenced → cannot produce a competing result (AI-DIST-9/13, FF-7); one aggregate, one result ([INV-O1](../../02-business-analysis/10-business-invariants.md)). *Residual:* fence failure. *Mitigation:* reliable fence realization; correctness-over-availability (minority side unavailable, not divergent).
15. **Can room migration change participant identity?**
    - *Expected:* No. *Protection:* participation records move with the room; one-per-participant; re-bind by token; role/team frozen (AI-DIST-12; ADR-009). *Residual:* none. *Mitigation:* —

**Conclusion:** the distribution model **cannot create two acting Authorities, cannot lose or diverge
truth, cannot change versions/outcomes, cannot leak the key, and cannot couple or cross rooms** — **by
construction** — because ownership is **singular and fenced by a monotonic epoch**, **truth is moved by
ownership transfer + per-room recovery (never shipped as a second live copy)**, **routing follows
ownership without defining it**, and **rooms share no authoritative state**. The only genuine residuals —
**fence realization reliability**, **ownership-directory availability**, and **transport crypto** — are
**future technical/operational controls**, explicitly delegated and named, not weaknesses of the
architecture. Under partition, the model **chooses correctness over availability** (the minority side is
unavailable, never divergent).

---

## Final Deliverable — Answers

- **What is the distribution unit?** The **Room** (its aggregate + Authority + participation records).
- **What owns a room?** Exactly **one Authority** (the Room Entity), hosted on some runtime node, holding
  the room's **current ownership epoch**.
- **Why can a room have only one Authority?** Because a single writer is what makes coordination
  deterministic and state atomic (ADR-003/002); two writers = races, split-brain, two winners — forbidden.
- **What is movable?** **Ownership** of a room (and, with it, the room's authoritative work), plus its
  participation records and delivery/connections (re-established) — via atomic, fenced transfer.
- **What is never movable (as a second live copy)?** **Truth** — the authoritative aggregate is never shipped
  as a concurrent second copy; it is **preserved through ownership transfer and restored via recovery**, so
  only one authoritative copy ever exists.
- **How are rooms isolated?** They share **no authoritative state**, have **no cross-room dependencies**, and
  fail/recover/scale independently (AI-DIST-3/10).
- **How is ownership transferred?** **Freeze → Validate → Transfer (epoch++) → Verify → Resume**, atomically,
  with the old owner fenced out.
- **How is split-brain prevented?** A **monotonic ownership epoch (fence)**: only the current-epoch owner may
  commit/publish; a stale owner is rejected — so at most one Authority ever acts.
- **Why does routing never define ownership?** Routing merely **resolves and follows** the current owner from
  the directory; ownership is established by placement/transfer and enforced by the fence — routing carries no
  authority (AI-DIST-6).
- **Why does migration never change authoritative state?** Migration moves **ownership**, not truth; the
  committed aggregate is preserved and restored at the new owner (AI-DIST-5); versions are unchanged (AI-DIST-7).
- **How does recovery compose with distribution?** Recovery (ADR-005) is already per-room and single-owner;
  the **fence** supplies the cross-node single-owner guarantee, so a recovered room is owned by exactly one
  current-epoch Authority — recovery and distribution reinforce each other.
- **How does this support future Kubernetes, Orleans, Dapr, cloud, and multi-region without changing the
  model?** Those are **realizations** of *placement, hosting, fence, and routing* — the model requires only
  "one fenced owner per room, movable, isolated, per-room recovery." Any platform that provides single-owner
  fencing + a placement/ownership directory + routing conforms; choosing or swapping one is a Technical-Design
  change beneath an unchanged architecture. (Multi-region adds a placement policy — a future ADR — not a model change.)

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: Owned, Fenced, Movable Room Instances with Deterministic Ownership Resolution; ownership/placement/routing/transfer/fencing model, invariants, fitness functions, security & adversarial review, verdict. |
