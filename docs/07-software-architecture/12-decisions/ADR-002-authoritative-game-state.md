# ADR-002 — Authoritative Game State

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *What is the complete authoritative state of a room that the single Authority owns, protects, mutates, and recovers?* |
| **Complies with** | [ADR-000 Vocabulary](ADR-000-architecture-vocabulary.md) (terms verbatim), [ADR-001 Overall Architecture Style](ADR-001-overall-architecture-style.md) (single-writer Room Entity), [ADR-003 Per-Room Coordination Model](ADR-003-per-room-coordination-model.md) (serialized single writer). Refines — does not replace — the [State Ownership Analysis](../05-state-ownership.md). Does not redefine business rules. |
| **Scope note** | Defines the **architectural state model** only. It does **not** define database schema, persistence implementation, serialization, caching, APIs, DTOs, frameworks, language, or storage engine (see [§19 Non-Goals](#19-non-goals)). |

---

## 1. Executive Summary

The **Authoritative Game State** of a room is the **single source of truth** for everything that
determines gameplay outcomes and room lifecycle for that room. It is a **central mutable aggregate**
— the **Room aggregate** — owned exclusively by that room's single **Authority** (the Room Entity of
[ADR-001](ADR-001-overall-architecture-style.md)) and mutated **only** through the coordinated pipeline
of [ADR-003](ADR-003-per-room-coordination-model.md) (Validation → Admission → Ordering → Adjudication
→ **Commit** → Publication → Role Filtering → Delivery).

The Room aggregate **contains** the room identity/lifecycle/membership/host, and — when a match is
active — the Game State, which in turn contains the Board State (words, the secret **key**, reveal
flags), the Turn (phase, active team, active clue, guess allowance), scores, and the recorded result.
Everything a participant *sees* is a **projection** — a read-only, role-filtered view **derived** from
this aggregate; **no projection is ever authoritative** and **no projection may mutate** the aggregate.

Every mutation passes through the aggregate because that is the only way to guarantee the
[consistency boundaries](../06-consistency-boundaries.md), the [rule precedence](../../02-business-analysis/16-rule-precedence.md),
and the [business invariants](../../02-business-analysis/10-business-invariants.md). The aggregate is
**recoverable to its last commit** within the room's lifetime; it is **not** long-term-durable data
(that is deferred to [ADR-005]). Transport, connections, UI, metrics, logs, and caches are explicitly
**outside** authoritative state.

> One-line statement: **one Room aggregate per room = the single source of truth; one Authority owns
> and mutates it via ADR-003; everything else is a derived, non-authoritative projection.**

---

## 2. Problem Statement

### Why authoritative state is required
[ADR-003](ADR-003-per-room-coordination-model.md) fixed *how* work is ordered; it presumes a single,
well-defined thing being mutated. Without a precisely bounded authoritative state, the single writer
has no unambiguous target, projections could drift into being "sources of truth," and recovery would
have no defined unit. The state model is the **noun** to ADR-003's **verb**.

### Why multiple sources of truth are dangerous
If two places both claim to hold "the current turn" or "which cards are revealed," they will diverge
under concurrency, network loss, or recovery — producing contradictory client views, wrong outcomes,
and — worst — **hidden-information leaks** if a projection accidentally becomes canonical and carries
the key. Multiple truths defeat determinism ([AP-06](../../06-architecture-governance/01-architecture-principles.md))
and consistency ([AP-07](../../06-architecture-governance/01-architecture-principles.md)).

### Which architectural drivers require it
Correctness, gameplay fairness (hidden information + determinism), consistency, recoverability, and
security — the top drivers ([02 Drivers](../02-architectural-drivers.md)). Each is meaningless without a
single, owned, well-bounded authoritative state.

### What failures occur without it
Double-reveals, mismatched counts, two active clues/turns, missed terminal detection, key leakage via a
"read model" treated as truth, unrecoverable or divergent state after interruption — i.e., the P1
engineering risks ([ENG-ST-01/02/03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md),
ENG-FP-01, ENG-RE-01).

---

## 3. State Philosophy

The architectural philosophy for Cluely state (technology-neutral):

- **Single Source of Truth.** Exactly one authoritative representation of a room's game-determining
  facts exists — the Room aggregate ([AP-07](../../06-architecture-governance/01-architecture-principles.md)).
- **Canonical State vs Derived Data.** The aggregate is **canonical**; anything computable from it
  (scores, "remaining agents," a Spymaster view) is **derived** and must never be stored as a competing
  truth. Where a value is cheap and unambiguous to compute, prefer **computed over stored** to avoid
  divergence; where a value is a genuine decision/outcome (a reveal, the active team), it is **stored**
  in the aggregate.
- **Mutable canonical state, with commit as the truth boundary.** Cluely's aggregate is **mutable** but
  changes only at atomic **commit** points (ADR-003). (An immutable-history/event-sourced realization is
  a *recovery/persistence* option for [ADR-005], **under** this model — see [§4](#4-candidate-state-models);
  it does not change what is authoritative.)
- **Transient Views / Read Models / Projections.** Everything a client sees is a **projection** derived
  from committed state; projections are role-filtered, disposable, and never authoritative.
- **Recovery Model.** The recovery unit is the **room** (the aggregate at its last commit); the recovery
  point is the last commit; nothing observable is reconstructed beyond that (details deferred to
  [ADR-005]).
- **Persistence ≠ Authority.** Where state is *held* or *durably stored* is independent of *who decides*
  it. Authority is the Room Entity; custody/persistence is a separate responsibility (ADR-000
  *State Custody*).

---

## 4. Candidate State Models

Five architectural approaches evaluated at the **per-room** scope mandated by ADR-001/003. None is
dismissed without reasoning.

### SM1 — Central Mutable Aggregate (RECOMMENDED)
- **Overview:** One in-memory-for-the-room, mutable **Room aggregate** owned by the single Authority;
  mutated atomically at commit via ADR-003; projections derived on demand.
- **Advantages:** Simplest, most direct mapping to the single-writer model; atomic multi-field
  transitions are natural (mutate the aggregate, commit); lowest latency on the hot path; trivial to
  reason about and test (state is a value the writer transforms); one obvious place for invariants.
- **Disadvantages:** Recovery requires an explicit strategy (the aggregate must be reconstructable to
  its last commit — deferred to ADR-005); no built-in audit history unless added.
- **Correctness:** Highest — one owner, one aggregate, atomic commit.
- **Complexity:** Lowest.
- **Recoverability:** Good with a defined checkpoint/log strategy (ADR-005); the *model* is
  recovery-friendly (a single unit).
- **Scalability:** By room count (aggregate is small and bounded); isolated.
- **Maintainability:** Highest — clear, cohesive aggregate.
- **Testing:** Easiest — deterministic transform of a value.
- **Operational complexity:** Lowest for MVP.
- **Future evolution:** High — can be persisted via snapshots or an event log *underneath* without
  changing what is authoritative.

### SM2 — Event-Sourced State (log of committed events is the source of truth)
- **Overview:** The authoritative state **is** an append-only, ordered log of committed domain events;
  current state is the fold of the log; a snapshot may accelerate reads.
- **Advantages:** Excellent recoverability and audit; the append point *is* ADR-003's ordering; natural
  replay; strong observability.
- **Disadvantages:** More machinery (event store semantics, snapshotting, versioning/upcasting) than the
  MVP needs ([AP-12](../../06-architecture-governance/01-architecture-principles.md), [AAP-05](../../06-architecture-governance/02-architecture-anti-principles.md));
  every read of "current turn" is a fold/snapshot concern; risk of leaking the key if events carry
  ownership and projections aren't carefully filtered.
- **Correctness:** High.
- **Complexity:** Medium–high.
- **Recoverability:** Excellent — its defining strength.
- **Scalability / Maintainability / Testing:** Good, but with more moving parts.
- **Future evolution:** Strong.
- **Verdict:** Its strengths are **recovery/audit** — which belong to [ADR-005] and can be adopted
  *under* SM1 without changing what is authoritative. As the *authoritative model* for the MVP it is
  heavier than required. **Not selected; explicitly compatible as a recovery realization.**

### SM3 — Snapshot + Derived Views (authoritative snapshots, views computed separately)
- **Overview:** Periodic authoritative snapshots plus separately-maintained derived views/read models.
- **Advantages:** Fast reads from prepared views; snapshots aid recovery.
- **Disadvantages:** Two things to keep consistent (snapshot vs views) → drift risk; if a view is ever
  treated as truth, invariants break; unnecessary for a tiny per-room aggregate.
- **Correctness:** Medium (drift risk).
- **Complexity:** Medium.
- **Recoverability:** Good.
- **Verdict:** Introduces view/state duality with no MVP benefit; SM1 already derives projections on
  demand from one truth. **Rejected for the MVP** (its snapshot idea is folded into recovery, ADR-005).

### SM4 — Distributed Ownership (state split/co-owned across nodes for one room)
- **Overview:** A room's state partitioned or replicated across multiple owners.
- **Advantages:** Fault tolerance; horizontal capacity for a single room.
- **Disadvantages:** Directly contradicts ADR-001/003 single-writer; reintroduces the consistency/racing
  problems those ADRs eliminated; disproportionate for a 25-card room ([AAP-08/12](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Correctness:** Hard (distributed consistency per action).
- **Complexity / Operational:** Very high.
- **Verdict:** **Rejected** — conflicts with frozen ADR-001/003; premature and disproportionate.

### SM5 — Hybrid (Central Mutable Aggregate now, event-log recovery later)
- **Overview:** SM1 as the authoritative model, with the *option* for ADR-005 to add an event log or
  periodic snapshots *underneath* for recovery/audit — without changing what is authoritative.
- **Advantages:** Best of SM1 (simplicity, correctness, latency) with a clear path to SM2's recovery
  strengths when justified; keeps authority and persistence separate.
- **Disadvantages:** Requires discipline that the recovery substrate never becomes a second source of
  truth (guarded by invariants §14).
- **Verdict:** This is effectively the recommended stance: **choose SM1 as the authoritative model and
  explicitly permit an SM2-style recovery substrate later (ADR-005).** Captured as the final decision.

---

## 5. Final State Model

**Adopt SM1 — a Central Mutable Room Aggregate as the single authoritative state — with the SM5 stance:
recovery/persistence may later be realized (ADR-005) via snapshots and/or an event log *beneath* the
aggregate, without changing what is authoritative.**

- **Why it fits Cluely:** the game state is small, bounded (one 25-card board, ≤ ~20 players), and
  changes through well-defined transitions; a single owned aggregate is the most direct, correct, and
  testable representation.
- **Why it aligns with ADR-001:** the aggregate is exactly what the single-writer Room Entity owns.
- **Why it aligns with ADR-003:** the aggregate is mutated only at the **Commit** stage of the
  coordinated pipeline; ADR-003's serialization makes aggregate mutation race-free by construction.
- **Why it protects business invariants:** one aggregate + atomic commit makes [INV-B2/B5/B7/B9,
  INV-G2/G3, INV-O1/O2, INV-R1](../../02-business-analysis/10-business-invariants.md) enforceable at a
  single point; there is no second place for them to be violated.
- **Why it satisfies quality attributes:** correctness/determinism (single truth, atomic commit),
  fairness (projections derived and filtered, never authoritative), recoverability (a single recovery
  unit), simplicity ([AP-12](../../06-architecture-governance/01-architecture-principles.md)), and
  evolvability (recovery substrate additive).

---

## 6. State Inventory

Every authoritative element of the **Room aggregate**. Owner is always the room's **Authority** (Room
Entity); mutation is always via [ADR-003](ADR-003-per-room-coordination-model.md) commit. "Visibility"
notes who may see it (via projections; the delivery boundary enforces it — ADR-006).

| Element | Purpose | Lifetime | Mutable? | Consistency | Recovery | Visibility | Depends on |
|---------|---------|----------|:--------:|-------------|----------|------------|-----------|
| **Room Identity** (room code) | Identify/join the room | Room lifetime | Immutable after creation | Strong (unique among live rooms) | Must survive | All participants | — |
| **Room Lifecycle status** (Lobby/InMatch/PostMatch/Expired) | Drive room flow | Room lifetime | Mutable (valid transitions) | Strong | Must survive | All | [State Machines](../../02-business-analysis/07-state-machines.md) |
| **Membership** (players present) | Who is in the room | Room lifetime | Mutable | Strong (capacity, [INV-R5](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Identity |
| **Host designation** | Single room control point | Room lifetime | Mutable (migration) | Strong ([INV-R1](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Membership |
| **Nickname per player** | In-room identity display/uniqueness | While present | Set on join (stable) | Strong (unique, [INV-P1](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Membership |
| **Team assignments** | Red/Blue sides | Match config → locked in match | Mutable in lobby; frozen in match | Strong at lock ([INV-T2/T5](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Membership |
| **Role assignments** (Spymaster/Operative) | Permissions & visibility | Match config → locked in match | Mutable in lobby; frozen in match | Strong (one Spymaster, [INV-T3](../../02-business-analysis/10-business-invariants.md)) | Must survive | All (role identity); key access derives from it | Team |
| **Dictionary selection (pinned version)** | Word source for the match | Chosen in lobby; pinned at start | Set at start; immutable for match | Strong pin ([INV-D3](../../02-business-analysis/10-business-invariants.md)) | Must survive | All (region); words appear on board | Content input (external) |
| **Game Lifecycle status** (NotStarted/InProgress/Finished{Won/Abandoned}) | Match flow | Match | Mutable (valid transitions) | Strong ([INV-G7](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Room lifecycle |
| **Board — words & layout** | The 25 cards | Match | Immutable after generation | Strong | Must survive | All | Dictionary version |
| **Board — key (card ownership map)** | Which card belongs to whom | Match | Immutable after generation | Strong | Must survive | **Spymasters only** ([INV-B9](../../02-business-analysis/10-business-invariants.md)) | Board words |
| **Card reveal flags** | Which cards are revealed | Match | Mutable (one-way) | Strong, atomic ([INV-B7](../../02-business-analysis/10-business-invariants.md)) | Must survive | All (revealed ownership becomes public) | Key |
| **Starting team / turn order** | Who plays first | Match | Immutable after generation | Strong | Must survive | All | Board/key |
| **Current Turn** (phase, active team) | Whose turn & phase | Match | Mutable (valid transitions) | Strong ([INV-G2](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Game lifecycle |
| **Current Clue** (word + number) for the turn | Active guidance | Current turn | Mutable (one per turn) | Strong (one active clue, [INV-G3](../../02-business-analysis/10-business-invariants.md)) | Must survive | All (once given) | Turn |
| **Guess allowance / guesses used** | Enforce guess limit | Current turn | Mutable | Strong ([INV-G6](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Clue |
| **Remaining-agents per team** | Progress toward win | Match | **Derived** (count of unrevealed team agents) — see §7 | N/A (computed) | Reconstructed from board | All | Board/reveal flags |
| **Win/Loss / Result** | Match outcome | Recorded at end | Written once, then immutable | Strong ([INV-O1/O4](../../02-business-analysis/10-business-invariants.md)) | Must survive | All | Board/turn |
| **Presence per player** (connected/disconnected/grace) | Drive pause/migration/abandon | While present | Mutable | Strong at decision; display may be eventual ([CB-08](../06-consistency-boundaries.md)) | May be reconstructed on reconnect | All (status) | Session |
| **Session/identity reference & reconnect token validity** | Resume continuity; single active connection | Session | Mutable (issue/invalidate/supersede) | Strong (one active, [INV-P4](../../02-business-analysis/10-business-invariants.md)) | Token validity must survive within grace | Owner/self only (never broadcast) | Membership |
| **Pause overlay** (active phase paused, awaited player) | Reflect essential-disconnect pause | While paused | Mutable | Strong at decision | Must survive | All (that play is paused) | Presence, Turn |
| **Authoritative timers/deadlines** (grace, idle-expiry) *if treated as authoritative* | Bound grace/expiry decisions | While relevant | Mutable | Strong at the decision moment; tick may be coarse | Deadline must be reconstructable | Owner (decisions surface as events) | Presence, Room lifecycle |
| **Pending administrative actions** (e.g., queued host kick in lobby) | In-flight lobby control | Until resolved | Mutable | Strong (ordered via ADR-003) | Should survive | Host/affected | Membership |
| **Round bookkeeping** | Pairing of turns | Match | Mutable | Strong (derived-ish) | Must survive | All | Turn |

> **Note on timers:** whether a timer's *value* is authoritative or merely an operational signal is
> refined by [ADR-005]/[ADR-009]. This ADR states only that the **deadline/decision** (e.g., "grace
> expires at commit-point T") is authoritative when it drives a state transition; the *ticking* is an
> external system concern.

---

## 7. State Classification

| Element(s) | Class | Why |
|-----------|-------|-----|
| Room identity, lifecycle, membership, host, nicknames, team/role, dictionary pin, game lifecycle, board words, key, reveal flags, starting team, turn, clue, guess allowance/used, result, presence, session/token validity, pause overlay, authoritative deadlines, pending admin actions | **Authoritative** | They are decisions/outcomes owned by the Authority; nothing else may define them. |
| Remaining-agents per team; "is it a win now?"; whose turn label; any per-role view content | **Derived / Computed** | Computable from authoritative board/turn/reveal state; storing them separately would risk drift ([§3](#3-state-philosophy)). Prefer computed. |
| Spymaster view, Operative view, lobby view, result view | **Projection** | Read-only role-filtered derivations of committed state; never authoritative. |
| Connection handles, network buffers, transport/session sockets | **Transient (outside)** | Live only with the connection; not game truth ([§8](#8-state-boundaries)). |
| Dictionary catalog/content | **External Input / Configuration** | Provided from outside; the *pinned version reference* is authoritative, the *content* is read-only input. |
| Client UI state | **Transient (outside)** | Belongs to the client; never authoritative. |
| Metrics, logs, analytics | **Derived observational (outside)** | PII-free observations of events; never authoritative, never mutate state. |
| Recovery snapshots / event log (if adopted by ADR-005) | **Persistent substrate (derived-of-authority)** | A durable *recording* of authoritative state/commits; it **serves** authority but is not a second truth. |
| Client-side or edge caches | **Cached (outside)** | Accelerate reads of projections; never authoritative; may be stale. |

---

## 8. State Boundaries

**Inside authoritative state (the Room aggregate):** everything classified *Authoritative* in §7 —
identity, lifecycle, membership/host/roles/teams, board+key+reveals, turn/clue/allowance, result,
presence-as-it-drives-decisions, session/token validity, pause overlay, authoritative deadlines,
pending admin actions.

**Outside authoritative state (must remain out):**

| Outside | Why it is not authoritative |
|---------|------------------------------|
| **Transport / connection state, network buffers** | Live with the physical connection; a player's *presence* (in-aggregate) is authoritative, but the socket/buffer is not. |
| **UI state** | Belongs to the client; the server never treats it as truth ([AP-03](../../06-architecture-governance/01-architecture-principles.md)). |
| **Analytics, logging, monitoring, metrics** | Observations derived from events; PII-free; must never influence or mutate state ([AAP §observability](../../06-architecture-governance/02-architecture-anti-principles.md)). |
| **Read models / projections** | Derived and role-filtered; disposable; never authoritative ([§13](#13-read-model-strategy)). |
| **Caches** | Performance copies of projections; may be stale; never authoritative. |
| **Dictionary content** | External read-only input; only the *pinned version reference* is inside the aggregate ([INV-D1](../../02-business-analysis/10-business-invariants.md)). |
| **Persistence/recovery substrate (ADR-005)** | Holds/records authoritative state but does not *decide* it; persistence ≠ authority ([§3](#3-state-philosophy)). |

**Rule:** if a datum determines a gameplay outcome or room-lifecycle decision, it is inside the
aggregate; if it merely displays, transports, observes, or accelerates, it is outside.

---

## 9. State Ownership

For all authoritative elements (§6), ownership is uniform under ADR-001/003:

| Question | Answer |
|----------|--------|
| **Who owns it?** | The room's single **Authority** (Room Entity). One owner per room ([ADR-001](ADR-001-overall-architecture-style.md), [AI-COORD-1/2](ADR-003-per-room-coordination-model.md#11-architectural-invariants-introducedaffirmed-by-this-adr)). |
| **Who may read it?** | Any **Reader** via a role-filtered **projection** (delivery, connectivity, observability) — never the raw aggregate for non-Spymaster-visible fields. The **key** is readable only in the Spymaster projection ([INV-B9](../../02-business-analysis/10-business-invariants.md)). |
| **Who may mutate it?** | **Only** the Authority, **only** at the ADR-003 Commit stage. |
| **Who may never mutate it?** | Delivery/transport, connectivity, observability, clients, dictionary provider, persistence substrate, projections — **all readers**. Enforced by [AI-COORD-3/4](ADR-003-per-room-coordination-model.md#11-architectural-invariants-introducedaffirmed-by-this-adr). |
| **Which ADR enforces ownership?** | [ADR-001](ADR-001-overall-architecture-style.md) (single-writer entity) + [ADR-003](ADR-003-per-room-coordination-model.md) (single ordered writer, no bypass); refined by this ADR's invariants (§14). |

This is the concrete, authoritative refinement of the [State Ownership Analysis §cross-cutting rules](../05-state-ownership.md#cross-cutting-ownership-rules).

---

## 10. State Lifetime

Conceptual lifecycle of the major states (transitions are the whitelisted ones from
[State Machines](../../02-business-analysis/07-state-machines.md); all mutations via ADR-003 commit).

**Room aggregate**
- **Creation:** on Create Room (identity/code, host, Lobby status).
- **Activation:** first player interactions; setup accumulates (teams/roles/dictionary).
- **Mutation:** throughout, only via committed transitions.
- **Completion (of a match):** result recorded; room → PostMatch.
- **Archival:** the **Result** may be retained PII-free per [Data Lifecycle](../../03-business-governance/05-data-lifecycle-retention.md); the live aggregate is not archived.
- **Deletion:** on room expiry, the live aggregate and transient state are discarded; room code released.
- **Recovery:** the aggregate is restorable to its last commit within room lifetime (ADR-005).
- **Expiration:** idle/empty/abandoned → Expired ([BR-RX](../../02-business-analysis/02-business-rules.md)).

**Game State (within the aggregate)**
- **Creation:** at Start (board+key+turn generated, status InProgress).
- **Mutation:** clue/guess/turn transitions until terminal.
- **Completion:** win/loss/abandonment → Finished; result written once (immutable).
- **Recovery:** to last commit; **finished never resumes** ([INV-G7](../../02-business-analysis/10-business-invariants.md)); terminal effects never replayed.

**Board State**
- **Creation:** at Start; words/key/starting-team immutable thereafter.
- **Mutation:** only reveal flags, one-way.
- **Deletion:** with the match/room.

**Session/Presence**
- **Creation:** on join (identity/token issued).
- **Mutation:** connect/disconnect/grace; supersede on new connection.
- **Completion/Deletion:** on leave / grace expiry / room expiry; token invalidated.
- **Recovery:** token validity within grace survives; presence re-established on reconnect.

---

## 11. Consistency Analysis

| Element | Strong? | Eventually consistent? | Immutable after creation? | Atomic update? | Coordinated mutation (ADR-003)? |
|---------|:------:|:----------------------:|:-------------------------:|:--------------:|:--------------------------------:|
| Room identity/code | ✔ | — | ✔ | — | at creation |
| Room/Game lifecycle status | ✔ | — | — | ✔ | ✔ |
| Membership / host / nicknames | ✔ | — | nickname stable per session | ✔ | ✔ |
| Team / role assignments | ✔ | — | frozen during match | ✔ | ✔ |
| Dictionary pin | ✔ | — | ✔ (for the match) | at start | ✔ |
| Board words / key / starting team | ✔ | — | ✔ | at generation | ✔ |
| Card reveal flags | ✔ | — | one-way | ✔ (with counts/turn) | ✔ |
| Current turn / clue / allowance | ✔ | — | — | ✔ | ✔ |
| Result | ✔ | — | ✔ (after write) | ✔ | ✔ |
| Presence | ✔ at decision | ✔ for display | — | ✔ at decision | decisions via ADR-003 |
| Session/token validity | ✔ | — | — | ✔ | ✔ |
| Pause overlay / authoritative deadlines | ✔ at decision | — | — | ✔ | ✔ |
| Remaining-agents / "win now?" (derived) | N/A (computed from committed state) | — | — | — | recomputed post-commit |

**Why:** all outcome- and lifecycle-determining state is **strongly consistent** and mutated
**atomically** under **coordinated** commit (per [CB-01…CB-10](../06-consistency-boundaries.md)); only
*display of presence* and *observational* data may be eventually consistent; derived values are never
independently mutated.

---

## 12. Recovery Model

(Architecture only — persistence technology is [ADR-005].)

- **Recovery boundary / unit:** the **room** — its Room aggregate as a whole. Rooms recover
  independently (isolation).
- **Recovery checkpoint:** the **last committed state** (ADR-003 commit boundary). Because ADR-003 is
  commit-then-broadcast, any broadcast state corresponds to a committed checkpoint.
- **What must survive interruption:** all **authoritative** elements needed to continue or correctly
  conclude the match/room — identity, lifecycle, membership/host/roles/teams, board+key+reveals,
  turn/clue/allowance, result (if written), session/token validity within grace, pending admin actions.
- **What may be reconstructed (not stored):** all **derived** values — remaining-agents, "win now?",
  and all **projections** (recomputed from the recovered aggregate); presence may be re-established from
  reconnections.
- **What must never be reconstructed / never survive incorrectly:** a **finished** match must not resume
  ([INV-G7](../../02-business-analysis/10-business-invariants.md)); **already-committed terminal effects
  must never be replayed**; transient transport/UI/cache state is not recovered (it is re-established).
- **Recovery assumptions:** at most the **in-flight** (uncommitted) Intent may need idempotent
  re-processing; everything committed is authoritative and replay-safe ([ADR-003 §9](ADR-003-per-room-coordination-model.md#9-failure-analysis)).
- **Non-decision:** *how* the checkpoint is captured (snapshot, event log, both) and where it is stored
  is **ADR-005**; this ADR only fixes the **unit, boundary, and what is/ isn't recoverable**.

---

## 13. Read Model Strategy

| Kind | Definition | Produced by | Consumed by | May it become authoritative? |
|------|-----------|-------------|-------------|-------------------------------|
| **Authoritative State** | The Room aggregate (single source of truth). | Mutated by the Authority at commit. | The Authority; recovery substrate records it. | It **is** authoritative. |
| **Derived State** | Values computed from the aggregate (remaining-agents, "win now?"). | Computed by the Authority/rules core when needed. | Projections. | **No.** |
| **Projections** | Role-filtered, read-only views (Spymaster/Operative/lobby/result). | Produced at the **delivery boundary** (ADR-006) from committed state. | Participants. | **No.** |
| **Client Views** | The client's rendering of a projection. | Clients. | Users. | **No.** |
| **Role-filtered Views** | The specific projection per role (key only for Spymasters). | Delivery boundary. | Participants. | **No** (and must never carry hidden info to the wrong role — [INV-B9](../../02-business-analysis/10-business-invariants.md)). |
| **Snapshots** | A consistent capture at a commit point: *recovery snapshot* (full authoritative) or *delivery snapshot* (role-filtered projection on (re)connect). | Recovery substrate (ADR-005) / delivery boundary. | Recovery / participants. | Recovery snapshot **records** authority (not a second truth); delivery snapshot is a projection — **no**. |
| **Temporary Views** | Ad-hoc computed views (e.g., lobby readiness). | Authority/delivery. | Participants. | **No.** |

**Rule (invariant, §14):** projections/derived/read models are **always** produced *from committed
authoritative state* and are **never** written back into it; a stale projection can never be promoted to
truth.

---

## 14. Architectural Invariants (introduced/affirmed)

Objectively verifiable; extend ADR-003's AI-COORD-* with state-model invariants **AI-STATE-***:

- **AI-STATE-1:** **Exactly one authoritative state (Room aggregate) per room.**
- **AI-STATE-2:** **Exactly one owner** (the Authority) may mutate it (affirms [AI-COORD-2](ADR-003-per-room-coordination-model.md#11-architectural-invariants-introducedaffirmed-by-this-adr)).
- **AI-STATE-3:** **No derived model, projection, cache, or persistence substrate mutates authoritative state.**
- **AI-STATE-4:** **Every mutation passes through the ADR-003 pipeline and commits atomically** (no bypass).
- **AI-STATE-5:** **No hidden information is stored in any non-Spymaster projection** (the key lives only in the aggregate and Spymaster projections).
- **AI-STATE-6:** **No duplicate ownership** of any state element; each element has exactly one owner.
- **AI-STATE-7:** **Every committed mutation leaves a valid state** — all business invariants hold post-commit.
- **AI-STATE-8:** **Every projection is derived solely from committed authoritative state**; none is authoritative.
- **AI-STATE-9:** **The recovery unit is the room aggregate at its last commit**; finished/terminal states never resume or replay.
- **AI-STATE-10:** **Persistence/custody never confers authority** — where state is stored is independent of who decides it.

---

## 15. Architecture Fitness Functions

Measurable checks (future architecture tests) derived from §14:

- **FF-S1:** At any instant, a room has **exactly one authoritative aggregate instance**.
- **FF-S2:** **No mutation of authoritative state occurs outside the Authority/commit path.**
- **FF-S3:** **No projection/read-model/cache performs a write** to authoritative state.
- **FF-S4:** **Recovery restores authoritative state only**; derived values/projections are recomputed, not restored as truth.
- **FF-S5:** **Every projection is reproducible from committed state** (recompute → identical projection).
- **FF-S6:** **No stale projection can be promoted to authoritative** (there is no code path that reads a projection as truth).
- **FF-S7:** **Every authoritative mutation is traceable** to its Intent and emitted events (with ADR-003).
- **FF-S8:** **No non-Spymaster projection ever contains unrevealed ownership** (the key) — negative test across all projections/snapshots.
- **FF-S9:** **Derived values equal their computation from the aggregate** (e.g., remaining-agents == count of unrevealed team agents) at every commit.

Map to [Success Metrics ASM-01/02/05/07/12](../../06-architecture-governance/04-architecture-success-metrics.md).

---

## 16. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| A projection/read model treated as a second source of truth | Architecture (critical) | AI-STATE-3/6/8; FF-S3/S6; review against [AAP-01](../../06-architecture-governance/02-architecture-anti-principles.md); derive projections on demand from committed state. |
| Key leaking via a projection/snapshot | Security (existential) | AI-STATE-5; FF-S8; key only in aggregate + Spymaster projection ([INV-B9](../../02-business-analysis/10-business-invariants.md)); negative testing across all views/snapshots. |
| Recovery restores a finished match or replays terminals | Recovery | AI-STATE-9; §12 rules; ADR-005 realizes idempotent, terminal-safe recovery. |
| Derived value drifts from source (stored instead of computed) | Correctness | Prefer computed; FF-S9 asserts equality at commit. |
| In-memory aggregate lost on owner failure | Recovery/Scaling | Recovery substrate (ADR-005) records committed checkpoints; recovery unit = room; only in-flight Intent re-applied. |
| Aggregate memory growth | Memory | Aggregate is bounded (25 cards, ≤ ~20 players); expiry reclaims; no unbounded history in-aggregate (history, if any, lives in the recovery substrate). |
| Over-engineering with event sourcing prematurely | Complexity/Evolution | SM1 now; SM2-style substrate only if ADR-005 justifies it; keep authority/persistence separate. |
| Ambiguity of "authoritative timer" | Complexity | This ADR fixes only that decision-driving deadlines are authoritative; ticking is external; refined by ADR-005/009. |
| Testing state invariants under all transitions | Testing | Property-based tests assert AI-STATE-7/9 across every whitelisted transition; deterministic replay (ADR-003). |

---

## 17. Trade-off Analysis

- **Benefits:** one unambiguous truth per room; atomic, invariant-preserving mutations; trivial
  derivation of role-safe projections; a single, simple recovery unit; lowest complexity and best
  testability; clean separation of authority from persistence and from projections.
- **Costs:** the aggregate must be explicitly recoverable (ADR-005 work); discipline required to keep
  derived/projection data from becoming truth; the in-memory-for-the-room model needs owner-failure
  recovery.
- **Accepted limitations:** no built-in audit history in the authoritative model itself (deferred to the
  optional recovery substrate); a room's authoritative state is served by one owner at a time (with
  recovery), consistent with ADR-001/003.
- **Future migration cost:** low and localized — adopting an event-log/snapshot substrate (ADR-005) or
  distributing per-room ownership (ADR-007) are additive around an unchanged authoritative model.
- **Operational impact:** minimal for MVP (bounded per-room state; expiry reclaims); recovery adds an
  operational concern owned by ADR-005.
- **Testing implications:** state becomes a deterministic value transformed by committed transitions →
  ideal for property-based and replay testing.
- **Recovery implications:** clear unit/boundary now; technology later.
- **Developer implications:** developers reason about one aggregate and derive views; they never write to
  projections or treat caches as truth.

---

## 18. Assumptions

| # | Assumption | Confidence | If it fails |
|---|-----------|-----------|-------------|
| AS-1 | A room's authoritative state is **small and bounded** (one 25-card board, ≤ ~20 players, one active turn/clue). *(Effectively a fact from business scope.)* | Very High | If state were large/unbounded (not a Codenames trait), a single mutable aggregate might need partitioning — not applicable. |
| AS-2 | **Derived values are cheap to compute** from the aggregate (e.g., remaining-agents). | Very High | If expensive, selective memoization (still derived, never a second truth) could be added without changing authority. |
| AS-3 | **Room-lifetime recoverability** suffices (no long-term durable game data required for MVP). | High | If durability/audit became required, ADR-005 adds an event-sourced/snapshot substrate under SM1. |
| AS-4 | **One active owner per room at a time** (with recovery) is acceptable. | High | If zero-interruption failover were required, distribution (ADR-007) is needed; the aggregate model still holds. |
| AS-5 | **Presence/timer *decisions*** can be made authoritative at commit even if the *tick* is external. | High | If timing needed to be authoritative continuously, ADR-005/009 must model it explicitly. |

---

## 19. Non-Goals

This ADR does **not** decide (deferred as noted):
- **Persistence implementation, storage engine, database design** → ADR-005.
- **Serialization / encoding** → later technical design.
- **Caching strategy** → later (caches are non-authoritative by rule).
- **Real-time transport / SignalR-style delivery / protocols** → ADR-004.
- **Role-filtering mechanism details** → ADR-006 (this ADR only mandates projections are derived & filtered).
- **Deployment / topology / scaling technology** → ADR-007 and deployment phase.
- **Security specifics / authentication** → future ADRs and the additive identity seam.
- **Snapshot/event-log format & cadence** → ADR-005.

---

## 20. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-002 |
|-------------|-------------------------------|
| **ADR-004 Real-Time Communication** | Delivers **projections** derived from committed aggregate state; commit-then-broadcast; never carries the key to non-Spymasters; delivery never writes state. |
| **ADR-005 State Recovery** | Recovery unit = Room aggregate at last commit; must restore **authoritative** state only, recompute derived/projections, never replay terminals; may realize a snapshot/event-log substrate **under** SM1 without becoming a second truth. |
| **ADR-006 Role-Based Visibility** | Produces role-filtered projections **from** the aggregate; the key resides only in the aggregate and Spymaster projections; enforces AI-STATE-5/8. |
| **ADR-007 Room Isolation / Distribution** | Must preserve exactly-one-authoritative-aggregate-and-owner per room during routing/failover; rooms recover independently; no shared aggregate across rooms. |
| **ADR-008 Dictionary Architecture** | Only the **pinned version reference** is inside the aggregate; dictionary content stays external read-only input; board words are immutable post-generation. |
| **ADR-009 Session & Reconnection** | Session/token validity and presence are authoritative elements; reconnection restores role-appropriate **projections** from committed state; single active connection. |
| **ADR-010 Command/Query Strategy** | Commands mutate the aggregate via ADR-003; queries read committed **projections**; any read model is derived and never authoritative. |
| **Software Architecture / Technical Design** | The Room aggregate is the primary consistency unit and the target of all mutation; components map to owner (mutates), readers (project), custody (records). |
| **Implementation** | No writes to projections/caches; no second source of truth; derive don't duplicate; all mutation through ADR-003 commit. |
| **Testing** | State-invariant property tests (AI-STATE-7/9), projection-reproducibility (FF-S5), key-leak negative tests (FF-S8), recovery-unit tests (FF-S4). |
| **Operations** | Monitor one-authoritative-aggregate-per-room; recovery restores authority only; bounded per-room footprint. |

---

## 21. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | Board/card/turn/clue/guess/win/loss/host/expiry: [BR-BG, BR-CO, BR-TO/TE, BR-CL, BR-GV…, BR-WIN/LOSE/ASN, BR-HM, BR-RX](../../02-business-analysis/02-business-rules.md). |
| **Business Invariants** | [INV-B2/B5/B7/B9](../../02-business-analysis/10-business-invariants.md) (board/key/reveal/no-leak), [INV-G2/G3/G6/G7](../../02-business-analysis/10-business-invariants.md) (turn/clue/allowance/finality), [INV-O1/O4](../../02-business-analysis/10-business-invariants.md) (single/immutable result), [INV-R1/R5/P1/P4](../../02-business-analysis/10-business-invariants.md), [INV-T2/T3/T5](../../02-business-analysis/10-business-invariants.md), [INV-D1/D3](../../02-business-analysis/10-business-invariants.md). |
| **Rule Precedence** | [16 Rule Precedence](../../02-business-analysis/16-rule-precedence.md) (terminal/turn ordering the aggregate transitions obey). |
| **State Ownership Analysis** | [05 State Ownership](../05-state-ownership.md) (S-01…S-10) — refined into this authoritative aggregate. |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md) (CB-01…CB-10). |
| **Quality Attribute Scenarios** | [09 QS](../09-quality-attribute-scenarios.md): QS-01/02/03/04/06. |
| **Engineering Challenges** | [ENG-ST-01/02/03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), ENG-FP-01, ENG-RE-01/02, ENG-DC-02. |
| **ADR-000** | Uses *Aggregate, Room/Game/Board State, Authority, Owner, Reader, Projection, Snapshot, State Custody, Recovery, Role Filtering, Hidden Information, Atomicity, Determinism, Consistency, Room Isolation* as defined. |
| **ADR-001** | The aggregate is what the single-writer Room Entity owns. |
| **ADR-003** | The aggregate is mutated only at the coordinated Commit; affirms AI-COORD-*. |
| **Governance** | Principles [AP-02/03/04/05/06/07/08/12/14](../../06-architecture-governance/01-architecture-principles.md); Anti-principles [AAP-01/03/05/08/09/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 22. Architecture Review

- **Decision:** The authoritative state is a **single Central Mutable Room Aggregate** per room (SM1),
  owned by the room's Authority and mutated only via ADR-003 commit; all views are derived,
  role-filtered projections; recovery unit is the aggregate at its last commit; persistence/audit may be
  realized under it (ADR-005) without creating a second truth (SM5 stance).
- **Confidence:** **High** — entailed by ADR-001/003 and the small, bounded, transition-based nature of
  the game; alternatives either add unjustified machinery (SM2/SM3) or contradict frozen ADRs (SM4).
- **Remaining risks:** owner-failure recovery of the in-memory aggregate (owned by **ADR-005**);
  guaranteeing single-owner during routing/failover (owned by **ADR-007**); discipline against
  projection-as-truth and key-in-projection (guarded by invariants/fitness functions, verified in
  testing).
- **Open questions (delegated, not blocking):** snapshot vs event-log substrate & cadence (ADR-005);
  exact treatment of authoritative timers (ADR-005/009); projection production placement details
  (ADR-006).
- **Future review triggers:** long-term durability/audit requirement; zero-interruption failover SLA;
  multi-region/distributed ownership; a per-room state that ceases to be small/bounded (not expected).
- **Readiness for the next ADR:** **READY.** With *how* work is coordinated (ADR-003) and *what* is
  authoritative (this ADR) fixed, the project can proceed to **ADR-004 (Real-Time Communication)** and
  **ADR-005 (State Recovery)**, both of which now have a precise state model and recovery unit to build
  on; **ADR-006 (Role-Based Visibility)** has a defined aggregate to project from.

---

## Final Deliverable — Answers

- **What is the single source of truth?** The **Room aggregate** (one per room), owned by that room's Authority.
- **What exactly is authoritative?** Room identity/lifecycle/membership/host/nicknames; team/role assignments; pinned dictionary version; game lifecycle; board words, the **key**, reveal flags, starting team; current turn/clue/guess-allowance; the recorded result; presence and session/token validity; pause overlay; decision-driving deadlines; pending admin actions ([§6](#6-state-inventory)).
- **What is merely derived?** Remaining-agents per team, "is it a win now?", and **all projections/role-filtered views** — computed from committed state, never stored as competing truth ([§7](#7-state-classification)).
- **Who owns every piece of state?** The room's single **Authority**; readers may only observe via projections; **no one else may mutate** ([§9](#9-state-ownership)).
- **What survives recovery?** The authoritative aggregate at its **last commit** (everything needed to continue/conclude correctly) ([§12](#12-recovery-model)).
- **What never survives recovery (as-is)?** Derived values and projections (recomputed), transport/UI/cache state (re-established), and — critically — a **finished match never resumes** and **committed terminal effects are never replayed**.
- **What may never mutate authoritative state?** Projections, read models, caches, delivery/transport, connectivity, observability, clients, dictionary provider, and the persistence substrate — **all readers** ([§8](#8-state-boundaries), [§14](#14-architectural-invariants-introducedaffirmed)).
- **What architectural guarantees now exist?** One source of truth per room; single-owner, atomic, invariant-preserving mutation via ADR-003; role-safe projections that can never become authoritative and never leak the key; a single, clean recovery unit; and a clear separation of **authority** from **persistence** and from **projections** — permanently, for all future architecture, design, implementation, testing, and review.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: Central Mutable Room Aggregate as the single authoritative state (SM1/SM5 stance); inventory, classification, boundaries, ownership, lifetime, consistency, recovery unit, read-model strategy, invariants, fitness functions, and verdict recorded. |
