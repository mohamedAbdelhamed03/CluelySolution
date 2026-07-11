# Cluely — Module Decomposition

| | |
|---|---|
| **Document** | 08.02 — Module Decomposition |
| **Phase** | Software Design (second document) |
| **Version** | 1.1 |
| **Status** | Approved — canonical logical module decomposition |
| **Technology** | **Neutral.** No language, framework, API, protocol, datastore, project/folder/namespace, or deployment topology is chosen or implied. Modules are **logical** units. |
| **Purpose** | Transform the approved [Domain Model (08.01)](01-domain-model-and-ubiquitous-language.md) into a modular software design: the logical modules, their responsibilities, ownership, dependencies, communication rules, contracts, lifecycle, concurrency ownership, and extension points — preserving every architectural decision. |
| **Owner** | Lead Architect / Senior Engineers. |
| **Consumes (does not redefine)** | [Domain Model (08.01)](01-domain-model-and-ubiquitous-language.md), [Architecture Discovery](../07-software-architecture/README.md) (esp. [System Responsibilities R-01…R-17](../07-software-architecture/03-system-responsibilities.md)), [ADR-000…ADR-010](../07-software-architecture/12-decisions/README.md), [Business Rules](../02-business-analysis/02-business-rules.md), [Business Invariants](../02-business-analysis/10-business-invariants.md), [SRS](../02-business-analysis/01-software-requirements.md). |

> **Reading contract.** This document **consumes** the frozen domain model and architecture; it never
> redefines them. Every module traces to a [bounded context (08.01 §2)](01-domain-model-and-ubiquitous-language.md#2-bounded-context-identification)
> and one or more [System Responsibilities (R-01…R-17)](../07-software-architecture/03-system-responsibilities.md).
> The module set is the [08.01 §20](01-domain-model-and-ubiquitous-language.md#20-domain-readiness-review)
> five-module seam, reconciled explicitly (§2). Terminology follows [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md) verbatim.

---

## Table of Contents
1. [Module Design Philosophy](#1-module-design-philosophy)
2. [Module Identification](#2-module-identification)
3. [Module Responsibilities](#3-module-responsibilities)
4. [Module Boundaries](#4-module-boundaries)
5. [Public Interfaces (logical)](#5-public-interfaces-logical)
6. [Dependency Rules](#6-dependency-rules)
7. [Dependency Graph](#7-dependency-graph)
8. [Communication Model](#8-communication-model)
9. [State Ownership](#9-state-ownership)
10. [Cross-Module Contracts](#10-cross-module-contracts)
11. [Module Lifecycle](#11-module-lifecycle)
12. [Concurrency Ownership](#12-concurrency-ownership)
13. [Extension Points](#13-extension-points)
14. [Module Smell Analysis](#14-module-smell-analysis)
15. [Architecture Compliance Review](#15-architecture-compliance-review)
16. [Readiness for C4](#16-readiness-for-c4)
17. [Adversarial Architecture Review](#17-adversarial-architecture-review)
18. [Module Readiness Review](#18-module-readiness-review)

---

## 1. Module Design Philosophy

### Why the software is divided into modules
Cluely's correctness rests on a small number of hard guarantees — a single authority per room,
determinism, hidden information, recoverability. A **module** is a cohesive logical unit that owns one
area of responsibility behind an explicit boundary, so that each guarantee lives in exactly one place
that can be reasoned about, tested, and evolved independently. Decomposition exists to **localize
change** and to make the architecture's boundaries visible in the software structure.

### A module is **not** a layer
Layers stack technical concerns (transport → application → domain → data). A module is a **vertical
slice of responsibility** (e.g., Gameplay), not a horizontal tier. A single module may participate at
several conceptual layers; layering is an *internal* concern of a module and is out of scope here.

### A module is **not** a service
A module is a logical partition, **not** an independently deployed process. Whether modules run in one
deployment unit or many is a deployment-topology decision explicitly deferred ([ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md) sets a
modular-monolith MVP; [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) allows later distribution **by room**). Nothing here chooses services.

### A module is **not** a project / folder / namespace
Project structure, folders, and namespaces are implementation packaging decisions ([09 Technical
Design](../09-technical-design/README.md)+). This document defines *logical* modules only.

### Why modules exist — and how this preserves the architecture
Each module is the software home of a [bounded context (08.01 §2)](01-domain-model-and-ubiquitous-language.md#2-bounded-context-identification)
and a cluster of [responsibilities (R-01…R-17)](../07-software-architecture/03-system-responsibilities.md).
The decomposition preserves the architecture because:

- **Aggregate boundaries are untouched.** Modules are **code-cohesion units, not consistency
  boundaries.** The single [Room/Match aggregate (08.01 §3.1)](01-domain-model-and-ubiquitous-language.md#31-room--match-aggregate)
  is operated on by **two** modules — **Room & Lobby** (owns and mutates the state; the Authority)
  and **Gameplay** (a *pure* Rules Core that **computes** decisions and owns **no** state). Splitting
  the *code* into two modules does **not** split the *aggregate*: there is still one writer, one
  consistency boundary, one atomic commit ([ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md)/[ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)). This is the central invariant of the
  decomposition and is defended throughout (§4, §12, §17).
- **Authority, Recovery, Visibility, Delivery, Distribution** each map to exactly one owning module,
  so no guarantee is duplicated or diffused.
- **Purity is structural.** Gameplay depends on nothing and holds no state, so "one gameplay
  worldwide" and determinism are enforceable ([AP-14](../06-architecture-governance/01-architecture-principles.md), [AAP-09](../06-architecture-governance/02-architecture-anti-principles.md)).

---

## 2. Module Identification

The prompt's 12 candidates are **validated against** the frozen model — not blindly accepted. The
authoritative anchor is the [08.01 §20](01-domain-model-and-ubiquitous-language.md#20-domain-readiness-review)
five-module seam plus the [§2 contexts](01-domain-model-and-ubiquitous-language.md#2-bounded-context-identification).

### 2.1 Accepted modules (MVP)
| Module | Realizes context (08.01 §2) | Responsibilities | Why it exists |
|--------|------------------------------|------------------|---------------|
| **M1 · Room & Lobby** | Room & Lobby (core) | R-01, R-02, R-03 (orchestration), R-16 | The **Authority**: owns and mutates the Room/Match aggregate; serializes Commands. |
| **M2 · Gameplay (Rules Core)** | Play (core) | R-04, R-05, R-07, R-08, R-09, R-10 (rule validation/adjudication) | The **pure decision engine**; computes outcomes, owns no state. |
| **M3 · Content (Dictionary)** | Content (supporting) | R-06 | Immutable, versioned, country-scoped words; the second aggregate. |
| **M4 · Delivery** | Delivery (supporting) | R-11 | Transports committed state/events and **role-filters** them into Projections (Projection & Visibility fold in — see 2.2). |
| **M5 · Connectivity & Identity** | Connectivity & Identity (supporting) | R-12, R-13 | Sessions, reconnection, and **derived Presence** signals. |
| **M6 · Recovery & State Custody** | (custody seam of Room, [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)) | R-14, R-15 | Holds snapshots + committed tail and validates recovery; **holds, never adjudicates**. |

### 2.2 Rejected, merged, or deferred candidates (with reasons)
| Candidate | Decision | Reason |
|-----------|----------|--------|
| **Projection** | **Merge → Delivery (M4)** | [08.01 §6](01-domain-model-and-ubiquitous-language.md#6-domain-services) makes *Projection Generation* and *Visibility Evaluation* **Delivery-owned services**. A separate module would split one cohesive responsibility (produce role-safe views at the delivery boundary) and create chatty coupling. |
| **Presence** | **Merge → Connectivity & Identity (M5)** | Presence is a **derived Value Object** ([08.01 §5/§9](01-domain-model-and-ubiquitous-language.md#5-value-object-discovery)), not an owner of state. Its source (Session) lives in the Connectivity context; a "Presence module" would own nothing authoritative. |
| **Recovery** | **Accept as distinct (M6)** | Kept separate from Room because [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) separates **custody** (R-14, holds state) from **authority** (Room, decides). Merging would blur that seam and let recovery drift toward adjudication. |
| **Administration** | **Defer (supporting/future)** | Operational control (dictionary publication, room ops); named, **not designed** here. No gameplay authority. |
| **Analytics / Observability** | **Supporting (R-17), minimal** | Consumes committed events read-only; never influences play. Named as a downstream consumer, not designed. |
| **Future Identity** | **Defer (future)** | Durable accounts attach at the future-auth seam ([ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#future-auth-seam), [AP-13](../06-architecture-governance/01-architecture-principles.md)); no MVP module. |
| **Future Matchmaking** | **Defer (future)** | Creates rooms via existing Commands; no restructuring (§13). |
| **Future Tournament** | **Defer (future)** | A future aggregate structuring Matches by ID ([08.01 §17](01-domain-model-and-ubiquitous-language.md#17-future-evolution)). |

> **Coverage check:** R-01…R-17 are each owned by exactly one MVP module (R-17 by the supporting
> Observability consumer), with **one deliberate split**: **R-10 "Validation & Authorization"** is
> divided by kind — **authorization** (role / turn / participation admission) is owned by **M1** (the
> Authority *gates admission* before any effect), while **rule-validation & adjudication** is owned by
> **M2** (the Rules Core *decides the outcome*). This is a clean separation of two distinct
> sub-responsibilities, not co-ownership of one: M1 answers "*may this actor act now?*"; M2 answers
> "*given that it may, what is the result?*" No other responsibility is split, unowned, or co-owned.

---

## 3. Module Responsibilities

For each MVP module. **Owned** items are pulled from [08.01 §6/§9](01-domain-model-and-ubiquitous-language.md#9-ownership-matrix)
and the ADRs — not re-derived.

### M1 · Room & Lobby *(the Authority)*
- **Purpose:** Be the single-writer authority and consistency unit for one room and its ≤1 Match.
- **Responsibilities:** Room lifecycle (create/join/leave/close/expire), host assignment & transfer,
  team/role assignment, dictionary pin, match start/finish orchestration, **authorization** (R-10:
  role/turn/participation admission — the *gate* before any effect), **Command serialization**,
  atomic **commit** + **Version++**, emit committed Domain Events (R-01/02/03/10-authz/16).
- **Owned concepts / domain objects:** Room (aggregate root), Participant, Board, Card, Turn — i.e.,
  the whole [Room/Match aggregate (08.01 §3.1)](01-domain-model-and-ubiquitous-language.md#31-room--match-aggregate).
- **Owned domain services:** Host Transfer (Ownership Transfer), Dictionary Selection (with M3),
  Participation policy enforcement.
- **Owned events:** [EVT-1…EVT-11](../02-business-analysis/11-domain-events-catalog.md), EVT-24/25; reflects EVT-22/23 into state.
- **Owned policies:** Participation, Host Transfer, Turn Progression *invocation* (decision computed by M2), Dictionary Selection.
- **Owned invariants:** `INV-P*`, `INV-R*`, `INV-O*`, `INV-G*` (as the committer), one-Host/one-Authority.
- **Owned state:** Room State (S-01), Game State (S-02/04), Board State (S-03), Version, DictionaryReference.
- **Owned Commands:** CreateRoom, JoinRoom, LeaveRoom, TransferHost, RemoveParticipant, AssignTeam, AssignRole, SelectDictionary, StartMatch (see §5).
- **Owned Queries:** (delegates read views to M4; may answer membership/room-status projections).
- **External interfaces:** invokes M2 (compute decision), M3 (resolve words), M6 (persist/commit + recover), M4 (publish committed events), consumes M5 signals.
- **Consumers:** all Commands terminate here.
- **Dependencies:** M2, M3, M6, M4 (publish), M5 (consume).
- **Forbidden responsibilities:** rule adjudication (M2 only), transport/serialization (M4), word storage (M3), connection handling (M5), snapshot mechanics (M6).
- **Future evolution:** gains new Commands (e.g., spectator join) without new aggregates.

### M2 · Gameplay *(Rules Core — pure)*
- **Purpose:** Compute deterministic outcomes from supplied state; the canonical source of rules.
- **Responsibilities:** Board generation, clue validation, guess adjudication, turn evaluation,
  victory/terminal evaluation (R-04/05/07/08/09; R-10 rule validation).
- **Owned concepts:** none as state — it operates on inputs handed in by M1.
- **Owned domain services:** Board Generation, Clue Validation, Turn/Guess Evaluation (Adjudication),
  Victory/Terminal Evaluation ([08.01 §6](01-domain-model-and-ubiquitous-language.md#6-domain-services)).
- **Owned events:** produces the *facts* (reveal/turn-end/finished) that M1 commits and emits as
  [EVT-12…EVT-21](../02-business-analysis/11-domain-events-catalog.md); Gameplay itself emits **no** external events.
- **Owned policies:** Board Generation, Starting-Team, Turn Progression, Victory policies.
- **Owned invariants:** `INV-B*`, `INV-T*`, `INV-G*` (as the *decider*; M1 is the *committer*).
- **Owned state:** **none** (pure).
- **Owned Commands:** none directly — it exposes pure operations invoked by M1 (SubmitClue/SubmitGuess/EndTurn *decisions*).
- **External interfaces:** pure functions taking state + intent, returning a decision/outcome.
- **Dependencies:** **none** (no transport, storage, language, or Content object — words are passed in).
- **Forbidden responsibilities:** owning/mutating state, connections, delivery, persistence, natural language, randomness sourced from outside its policy input.
- **Future evolution:** new modes add rules here without touching other modules; bots/AI submit the *same* intents through M1.

### M3 · Content (Dictionary)
- **Purpose:** Provide immutable, versioned, country-scoped word sets.
- **Responsibilities:** Publish Dictionary Versions; resolve a DictionaryReference/locale to words (R-06).
- **Owned concepts:** Dictionary Version (aggregate root), Word.
- **Owned domain services:** Dictionary resolution (with M1's selection policy).
- **Owned events:** version publication (Content-scoped, [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)).
- **Owned invariants:** `INV-D1/D2/D3` (immutable, versioned, ≥ 25 words).
- **Owned state:** Dictionary Versions + Words (authoritative, immutable after publish).
- **Owned Queries:** ResolveVersion, ResolveWords (see §5).
- **Dependencies:** **none downstream** (upstream, read-only to M1).
- **Forbidden responsibilities:** knowing about rooms/matches/participants; any dependency on M1/M2.
- **Future evolution:** custom/premium/regional dictionaries add versions; pin-by-ID contract unchanged.

### M4 · Delivery *(incl. Projection & Visibility)*
- **Purpose:** Transport committed state/events to participants and **role-filter** into Projections.
- **Responsibilities:** consume committed Domain Events + snapshots; generate per-Role Projections;
  answer Queries; never adjudicate (R-11).
- **Owned domain services:** Projection Generation, Visibility Evaluation ([08.01 §6](01-domain-model-and-ubiquitous-language.md#6-domain-services), [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)).
- **Owned policies:** Visibility Policy (whitelist-by-inclusion).
- **Owned invariants:** upholds `INV-B9` (no hidden-info leak) at the delivery boundary.
- **Owned state:** **none authoritative** — only derived, rebuildable Projection caches.
- **Owned Queries:** GetProjection(role), GetSnapshot(role) (see §5).
- **Dependencies:** consumes M1's outbound committed-event/snapshot **contract** (not M1 internals).
- **Forbidden responsibilities:** adjudication, state ownership, deciding outcomes, holding the Key as truth.
- **Future evolution:** new Roles (spectator) add Visibility rules only.

### M5 · Connectivity & Identity *(Presence/Session)*
- **Purpose:** Manage transient sessions and reconnection; derive Presence.
- **Responsibilities:** session lifecycle, reconnect-token validation, connect/disconnect signals (R-12/13).
- **Owned concepts:** Session, ReconnectToken; **Presence is derived**.
- **Owned events:** system events (connect/disconnect), surfaced as [EVT-22/23](../02-business-analysis/11-domain-events-catalog.md) for M1 to reflect.
- **Owned invariants:** session/token integrity ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md), [BR-RX](../02-business-analysis/02-business-rules.md)).
- **Owned state:** Session/ReconnectToken (S-06, transient).
- **Dependencies:** signals M1; depends on no gameplay module.
- **Forbidden responsibilities:** deciding gameplay, owning game state, carrying hidden information.
- **Future evolution:** the **future-auth seam** — durable Identity attaches here without changing M1/M2.

### M6 · Recovery & State Custody
- **Purpose:** Hold authoritative state durably enough to restore a room to its last commit, once.
- **Responsibilities:** snapshot capture at commit, committed-tail retention, recovery validation & replay (R-14/15).
- **Owned domain services:** Recovery Validation ([08.01 §6](01-domain-model-and-ubiquitous-language.md#6-domain-services)).
- **Owned policies:** Recovery Policy (snapshot cadence, replay, validate to last commit).
- **Owned invariants:** monotonic Version; restore last commit **once**; no terminal-effect replay ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)).
- **Owned state:** snapshots + committed tail (custody of Room's authoritative state; **holds, never owns semantics**).
- **Dependencies:** invoked by M1 at commit and on recovery; depends on no other module.
- **Forbidden responsibilities:** adjudication, changing business truth, role filtering.
- **Future evolution:** distribution adds ownership-fencing coordination ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)) without changing the custody contract.

---

## 4. Module Boundaries

Explicit *owns* / *must never own* per module.

| Module | Owns | Must never own |
|--------|------|----------------|
| **M1 Room & Lobby** | Room/Match aggregate state, membership, host, version, commit, serialization | Rule adjudication · transport/encoding · word storage · connections · snapshot mechanics |
| **M2 Gameplay** | Rules, board generation, turn/guess/victory evaluation (**pure**) | **Any state** · connections · delivery · persistence · authentication · language/localization |
| **M3 Content** | Dictionary Versions, Words, resolution | Rooms/matches/participants · gameplay rules · delivery |
| **M4 Delivery** | Projection generation, visibility filtering, transport of committed data | Adjudication · authoritative state · the Key as truth · deciding outcomes |
| **M5 Connectivity & Identity** | Sessions, reconnect tokens, presence signals | Gameplay decisions · game state · hidden information |
| **M6 Recovery & Custody** | Snapshots, committed tail, recovery validation | Adjudication · business-rule changes · role filtering · authority over outcomes |

**Universal boundary rule:** only **M1 writes authoritative game state.** Every other module either
computes (M2), supplies (M3), reads/filters (M4), signals (M5), or holds/restores (M6).

---

## 5. Public Interfaces (logical)

Logical capabilities only — **no** HTTP/REST/real-time/RPC. These are *carriers-agnostic* Commands
and Queries per [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md); any future transport merely carries them.

| Module | Exposes (Commands — state-changing) | Exposes (Queries — read-only) |
|--------|-------------------------------------|-------------------------------|
| **M1 Room & Lobby** | CreateRoom · JoinRoom · LeaveRoom · TransferHost · RemoveParticipant · AssignTeam · AssignRole · SelectDictionary · StartMatch · SubmitClue* · SubmitGuess* · EndTurn* | RoomStatus · MembershipView (delegates role-filtered views to M4) |
| **M2 Gameplay** | *(pure operations invoked by M1)* GenerateBoard · ValidateClue · AdjudicateGuess · EvaluateTurn · EvaluateVictory | *(none — stateless)* |
| **M3 Content** | PublishVersion *(Admin/future)* | ResolveVersion · ResolveWords |
| **M4 Delivery** | *(none — never changes state)* | GetProjection(role) · GetSnapshot(role) |
| **M5 Connectivity** | OpenSession · Reconnect · CloseSession | PresenceView |
| **M6 Recovery & Custody** | Commit(snapshotable) · Recover | GetLastCommit *(internal to M1)* |

\* SubmitClue/SubmitGuess/EndTurn are **Commands received by M1** (the Authority); M1 invokes M2 to
compute the outcome, then commits. The *play intent* enters at M1, not M2 — preserving single-writer
Authority ([ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)/[ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)).

---

## 6. Dependency Rules

| Module | May depend on | Must never depend on | Why |
|--------|---------------|----------------------|-----|
| **M1 Room & Lobby** | **M2 (compute), M3 (resolve), M6 (commit/recover)** | — | The composition root/orchestrator. It *publishes* to M4 and *consumes signals* from M5, but those are one-way **flows**, not dependencies: **M4→M1 and M5→M1** are the dependency edges (M1 keeps working if M4/M5 are down). See the `Allowed` line below and [08.04 §6](04-c4-container-diagram.md#6-container-dependency-rules). |
| **M2 Gameplay** | **nothing** | M1, M3, M4, M5, M6 | Purity: any dependency would let transport/state/content leak into rules ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md)). |
| **M3 Content** | **nothing** | M1, M2, M4, M5, M6 | Upstream, read-only; must not know who plays ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| **M4 Delivery** | M1 outbound **contract** (committed events/snapshots) | M2, M3, M6 internals; **must not** be depended on by M1 for decisions | Reader only; consuming a contract (not internals) prevents a cycle. |
| **M5 Connectivity** | M1 inbound signal **contract** | M2, M4, M6 | Signals only; must not decide gameplay. |
| **M6 Recovery & Custody** | **nothing** (invoked by M1) | M2, M3, M4, M5 | Holds state; must not adjudicate or filter. |

**No cycles.** M2, M3, M6 are **leaf** modules (depend on nothing). M1 is the **core**. M4/M5
communicate with M1 via **one-way contracts** (M1→M4 publish; M5→M1 signal), so the graph is a DAG.

---

## 7. Dependency Graph

Allowed dependencies (arrows point *toward the depended-upon* capability). Directions not shown are
**forbidden**.

```text
                         ┌───────────────► M2 Gameplay (Rules Core) ── leaf (pure, no deps)
                         │
                         ├───────────────► M3 Content (Dictionary) ── leaf (upstream, no deps)
                         │
   Commands ──► M1 Room & Lobby (CORE, Authority)
                         │
                         ├───────────────► M6 Recovery & State Custody ── leaf (holds; no deps)
                         │
                         │   commit-then-broadcast (one-way outbound contract)
                         └───────────────► M4 Delivery ──► role-filtered Projections ──► participants

   M5 Connectivity ──(one-way signal contract: connect/disconnect)──► M1
```

- **Allowed:** M1 → {M2, M3, M6}; M1 ⇒ M4 (publish committed events); M5 ⇒ M1 (signals).
- **Forbidden directions:** M2 → anything · M3 → anything downstream · M4 → M1 (for decisions) ·
  M4 authoritative · M5 → gameplay · any Module → Module cycle · any cross-room edge ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)).
- **Dependency depth:** ≤ 2 (M1 → leaf). **Critical module:** M1. **Leaf modules:** M2, M3, M6.
  **Boundary modules:** M4 (out), M5 (in).

---

## 8. Communication Model

| Aspect | Rule |
|--------|------|
| **Commands** | Enter at **M1** only (the Authority), are **serialized** per room, validated, adjudicated via M2, committed atomically, then broadcast ([ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)/[ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). |
| **Queries** | Answered as **read-only Projections** by M4 (or simple status by M1); never mutate ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). |
| **Direct communication** | M1 → M2 (synchronous, in-scope, pure call returning a decision); M1 → M3 (resolve words); M1 → M6 (commit/recover). No other direct calls. |
| **Indirect communication (events)** | M1 emits committed **Domain Events** **after commit**; M4 and Observability **consume** them; M6 persists the tail. Consumers are decoupled from M1's internals. |
| **System events** | M5 emits connect/disconnect **system events**; M1 consumes them to update **derived** presence. |
| **State sharing** | **None.** Modules share **no mutable state.** State is passed as immutable inputs (M1→M2) or exposed as derived Projections (M4). |
| **Cross-module reads** | Allowed only as **Projections/contracts** (M4 reads committed state via contract), never as live handles into M1's aggregate. |
| **Cross-module writes** | **Forbidden.** Only **M1** writes authoritative game state; M3 writes only its own immutable versions; M6 writes only custody copies of M1-committed state. |
| **Ownership transfer** | Host transfer is *within* M1; distribution ownership-fencing (epoch) coordinates *which node's* M1 owns a room ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)) — never two writers. |
| **Reference sharing** | By **ID only** across boundaries (e.g., DictionaryReference to M3; ParticipantId in Projections). No navigational cross-module object graphs. |
| **Forbidden styles** | Shared mutable state, cross-module transactions, call-backs from M2/M3 into M1, M4 writing state, chatty per-field reads into the aggregate. |

---

## 9. State Ownership

Pulled from [08.01 §9/§14](01-domain-model-and-ubiquitous-language.md#9-ownership-matrix); one owner each.

| Module | Authoritative | Derived | Shared | Transient | Recoverable | Read-only | External refs |
|--------|---------------|---------|--------|-----------|-------------|-----------|---------------|
| **M1** | Room/Match, Board+Key, Turn, membership, host, Version | GamePhase, whose-turn | **none** | in-flight Command | all authoritative (via M6) | — | Dictionary Version (by ID) |
| **M2** | **none** | (computes) | none | working values during a call | none | its policy inputs | — |
| **M3** | Dictionary Versions, Words | word counts/index | none | load buffers | published versions | — | — |
| **M4** | **none** | Projections | none | broadcast buffers | none (rebuildable) | committed state (via contract) | — |
| **M5** | Session, ReconnectToken | Presence | none | connection handles | (token as needed) | — | ParticipantId (by ID) |
| **M6** | custody copies (snapshot + tail) | — | none | replay buffers | the room's state | — | RoomId (by ID) |

**Rule:** no **Shared** authoritative state exists in any row — the architecture forbids it
([AAP-08/14](../06-architecture-governance/02-architecture-anti-principles.md)).

---

## 10. Cross-Module Contracts

Logical contracts (no implementation). Each: purpose · owner · consumer · inputs · outputs ·
pre/postconditions · invariants · versioning · failure.

### C1 — Compute Decision (M1 → M2)
- **Purpose:** Given current state + a play intent, compute the deterministic outcome.
- **Owner:** M2. **Consumer:** M1.
- **Inputs:** immutable snapshot of relevant state + intent (clue/guess/end-turn).
- **Outputs:** decision (accept/reject + effects: reveals, count/turn changes, terminal).
- **Preconditions:** intent already role/turn-validated by M1; state consistent.
- **Postconditions:** M2 mutates nothing; M1 applies effects atomically.
- **Invariants:** determinism ([AP-06](../06-architecture-governance/01-architecture-principles.md)); `INV-B*/T*/G*` respected in the decision.
- **Versioning:** rule changes are new M2 behavior, versioned with the rules core; no wire schema.
- **Failure:** M2 returns a catalogued rejection ([Error Catalog](../02-business-analysis/12-business-error-catalog.md)); never throws business truth away.

### C2 — Resolve Words (M1 → M3)
- **Purpose:** Resolve a pinned DictionaryReference to the word set for board generation.
- **Owner:** M3. **Consumer:** M1 (then passed to M2).
- **Inputs:** DictionaryReference (RegionCode + ContentVersion).
- **Outputs:** immutable word set (≥ 25).
- **Pre/Post:** version exists & is published; result is immutable.
- **Invariants:** `INV-D1/D3`; pin-by-ID ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)).
- **Failure:** unknown/insufficient version → rejection before match start.

### C3 — Commit & Custody (M1 → M6)
- **Purpose:** Persist a committed state change (snapshot + tail) for recovery.
- **Owner:** M6. **Consumer:** M1.
- **Inputs:** committed state delta + new Version.
- **Outputs:** durability acknowledgement.
- **Pre/Post:** commit is atomic; Version strictly increases.
- **Invariants:** monotonic Version; recover-once ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)).
- **Failure:** custody failure blocks broadcast (commit-then-broadcast preserved).

### C4 — Publish Committed Events (M1 → M4)
- **Purpose:** Deliver committed state/events for role-filtered projection.
- **Owner:** M1 (producer contract). **Consumer:** M4.
- **Inputs:** committed Domain Events + current snapshot reference.
- **Outputs:** none back to M1 (one-way).
- **Pre/Post:** emitted **only after commit**; ordering total per room.
- **Invariants:** `INV-B9` enforced by M4 filtering; no adjudication in M4.
- **Failure:** delivery failure never rolls back committed truth; M4 re-derives on reconnect.

### C5 — Presence Signal (M5 → M1)
- **Purpose:** Inform M1 of connectivity changes to update derived presence.
- **Owner:** M5. **Consumer:** M1.
- **Inputs:** session connect/disconnect + ParticipantId.
- **Outputs:** none (fire-and-reflect); M1 may emit EVT-22/23.
- **Invariants:** presence is derived, never authoritative gameplay ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)).
- **Failure:** lost signal degrades presence display only (eventual), never outcomes.

---

## 11. Module Lifecycle

Active (●) / passive-or-consuming (○) / inactive (—) per stage. "Ownership transitions" note where
authority/custody hands off.

| Stage | M1 Room | M2 Gameplay | M3 Content | M4 Delivery | M5 Connectivity | M6 Recovery |
|-------|---------|-------------|------------|-------------|-----------------|-------------|
| **Room Creation** | ● creates Room, becomes Authority | — | — | ○ broadcasts RoomCreated | ○ opens sessions | ● begins custody |
| **Lobby** | ● membership/host/teams | — | ○ (dictionary browse) | ○ lobby projections | ● sessions/presence | ○ custody |
| **Game Setup** | ● pins dictionary, starts match | ● generates Board (pure) | ● resolves words | ○ role-filtered board (Key→Spymaster) | ○ | ● snapshot at start |
| **Gameplay** | ● serializes/commits moves | ● adjudicates each move | — | ● per-move projections | ○ presence | ● commit each move |
| **Recovery** | ○ requests restore, resumes Authority | — | — | ○ resends snapshot on resume | ○ | ● validates + replays to last commit |
| **Reconnect** | ● re-attaches participant | — | — | ● resync role-filtered snapshot | ● validates token | ○ |
| **Delivery** | ● produces committed events | — | — | ● transports + filters | ○ | ○ |
| **Game Finish** | ● commits terminal result | ● evaluates victory | — | ● broadcasts GameFinished | ○ | ● final snapshot |
| **Room Close** | ● closes/expires, releases Authority | — | — | ○ broadcasts closed | ● closes sessions | ● retires custody |

**Ownership transitions:** Authority is held by exactly one M1 instance per room throughout; on
recovery/distribution it is re-acquired under **ownership fencing** (monotonic epoch, [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)) so
two M1 instances never write concurrently.

---

## 12. Concurrency Ownership

Pulled verbatim from the ADRs — this document assigns, it does not invent.

| Concern | Owner | Source |
|---------|-------|--------|
| **Serializes Commands** | M1 (single writer per room) | [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md) |
| **Consistency** | M1 (strong, within room) | [ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md), [CB-01…CB-10](../07-software-architecture/06-consistency-boundaries.md) |
| **Transactions (atomic commit)** | M1 (with M6 custody) | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| **Ordering** | M1 (total order per room); M4 preserves it downstream | [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)/[ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md) |
| **Versioning** | M1 (monotonic Version++) | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| **Recovery** | M6 (custody + replay); M1 resumes Authority | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| **Visibility** | M4 (role filtering at delivery boundary) | [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| **Delivery** | M4 (transport + filter, never adjudicate) | [ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md) |
| **Distribution (ownership fencing)** | M1 under epoch coordination; rooms isolated | [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) |

**Key guarantee:** there is exactly **one writer (M1) per room**; M2 is pure (no concurrency), M3 is
immutable (no write contention), M4/M5 are readers/signalers, M6 is custody. Concurrency correctness
is therefore **structural**, not lock-managed at the module level.

---

## 13. Extension Points

Each future capability attaches **without restructuring existing modules** (restates [08.01 §17](01-domain-model-and-ubiquitous-language.md#17-future-evolution)).

| Capability | How it attaches | Modules changed |
|-----------|-----------------|-----------------|
| **Authentication** | New Identity module at M5's future-auth seam; Participant gains optional accountId | +Identity; M1/M2 unchanged |
| **Spectators** | New Role + Visibility rule in M4; join Command in M1 | M4 (policy), M1 (command) |
| **Bots / AI Players** | Emit the **same** Commands to M1; no privileged path | none (new client only) |
| **Custom / Premium Dictionaries** | New immutable versions + entitlement in selection | M3 (data), M1 (policy) |
| **Ranked Mode / Achievements** | Observability/Analytics consumes committed events | +consumer; core unchanged |
| **Friends / Organizations** | New context referencing IDs | new module(s); core unchanged |
| **Tournaments** | New aggregate/module structuring Matches by ID | +Tournament; core unchanged |

**Guarantee:** extensions add modules or Roles/policies; they never split M1's aggregate, never make
M4 authoritative, and never bypass M1's Authority or M2's rules.

---

## 14. Module Smell Analysis

| Smell | Present? | Mitigation |
|-------|----------|------------|
| **God Module** | *Watch — M1* | M1 is the Authority but delegates *decisions* to M2, *content* to M3, *delivery* to M4, *custody* to M6 — it orchestrates, it doesn't do everything. Its scope is one bounded room (bounded, [08.01 §16](01-domain-model-and-ubiquitous-language.md#16-domain-smells)). |
| **Circular Dependencies** | *Avoided* | DAG (§7): leaves M2/M3/M6 depend on nothing; M4/M5 use one-way contracts. |
| **Hidden Coupling** | *Guarded* | No shared mutable state; references by ID; no "utility" module (see §17). |
| **Shared Mutable State** | *Avoided* | Only M1 writes; others receive immutable inputs or derived views (§8/§9). |
| **Leaky Abstractions** | *Guarded* | The Key never leaves M2/M1 except via M4 role-filtering ([INV-B9](../02-business-analysis/10-business-invariants.md)). |
| **Duplicate Responsibilities** | *Avoided* | R-01…R-17 each owned once (§2), with R-10 **split by kind** (authorization→M1 gate, rule-validation/adjudication→M2), not co-owned; adjudication is only ever in M2. |
| **Chatty Modules** | *Guarded* | M1↔M2 is one call per move returning a whole decision; M4 consumes batched committed events, not per-field reads. |
| **Boundary Violations** | *Guarded* | §4 owns/never-owns table + §6 dependency rules; enforced by fitness checks (§16). |
| **Temporal Coupling** | *Watch* | Commit-then-broadcast fixes order (M6 commit → M4 publish); documented in C3/C4 so sequence isn't implicit. |

---

## 15. Architecture Compliance Review

| ADR | Requirement | Module-design compliance |
|-----|-------------|--------------------------|
| **[ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md)** | Canonical vocabulary | Module names use context/role terms; no new terminology. ✅ |
| **[ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md)** | Modular, single-writer, pure Rules Core | M1 single writer; M2 pure; modules ≠ services. ✅ |
| **[ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md)** | One authoritative state | Only M1 writes (§4/§9). ✅ |
| **[ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)** | Serialized per-room commands | M1 serializes; §12. ✅ |
| **[ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md)** | Transport + filter, never adjudicate | M4 owns delivery, no adjudication (§4). ✅ |
| **[ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)** | Snapshot + tail, once | M6 custody/recovery; C3; §12. ✅ |
| **[ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)** | Whitelist projections; Key hidden | M4 Projection/Visibility; INV-B9 (§4/§14). ✅ |
| **[ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)** | Isolate rooms; fence ownership | No cross-room edges (§7); epoch fencing (§11/§12). ✅ |
| **[ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)** | Immutable versioned, pin by ID | M3 upstream, by-ID (C2). ✅ |
| **[ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)** | Session/presence/continuity | M5 owns sessions; presence derived (§3/§9). ✅ |
| **[ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)** | Commands to Authority; Queries as projections | Commands→M1; Queries→M4 (§5/§8). ✅ |

**Violations found:** none.

---

## 16. Readiness for C4

The decomposition is complete enough to produce, **without redefining module responsibilities**:

- **C4 L1 (System Context):** the system + external actors (players/host, future auth) — actors and
  the one system boundary are implied by §2/§11.
- **C4 L2 (Container View):** *when* a deployment topology is chosen, containers group these modules;
  the modular-monolith MVP ([ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md)) maps all six modules into one unit, and [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)
  shows the distribute-by-room evolution — a *visualization* of §7, not a new decision.
- **Aggregate Design:** refines M1's Room/Match and M3's Dictionary Version internals (from [08.01 §3/§4](01-domain-model-and-ubiquitous-language.md#3-aggregate-discovery)).
- **Application-Layer Design:** organizes behavior *within* modules (M1 orchestration, M2 pure ops).
- **Technical Design:** maps modules onto concrete technologies (deferred; §Constraints).

Each downstream artifact **visualizes or refines** these modules; none may redraw the boundaries.

---

## 17. Adversarial Architecture Review

Each scenario: **Attack · Expected Failure · Architectural Protection · Residual Risk · Mitigation.**

| # | Attack | Expected failure | Architectural protection | Residual risk | Mitigation |
|---|--------|------------------|--------------------------|---------------|------------|
| 1 | A module takes ownership of another's state | Duplicate/ambiguous ownership | §4/§9 single-owner rule; only M1 writes | A module reads a live handle | Cross-module refs by **ID/contract** only (§8); fitness check |
| 2 | Gameplay depends on Delivery | Rules coupled to transport | M2 has **zero** dependencies (§6) | A dev adds a call M2→M4 | Purity fitness check ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md)); M2 takes only value inputs |
| 3 | Delivery becomes authoritative | Two sources of truth | M4 owns **no** authoritative state (§4/§9) | Cached projection treated as truth | Projections labeled derived; recovery re-derives from M1 |
| 4 | Recovery modifies business rules | Truth changed on restore | M6 **holds, never adjudicates** (§3/§12) | Replay reorders effects | Deterministic replay to last commit, once ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)) |
| 5 | Dictionary depends on Room | Content coupled to gameplay | M3 has **no** downstream deps (§6, [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)) | Admin tooling couples them | Keep publication in Content/Admin, resolve by ID only |
| 6 | Presence decides gameplay | Connectivity affects outcomes | M5 signals only; presence **derived** (§3/§12) | Pause logic leaks into rules | Pause is a Room state, not an M2 rule ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)) |
| 7 | Circular dependency introduced | Build/reasoning cycle | DAG with leaf modules (§7) | New feature adds a back-edge | Dependency fitness check; contracts, not calls |
| 8 | Cross-module transaction | Distributed transaction | One aggregate, one writer (M1) commits atomically | Custody + commit split | C3 makes commit atomic before broadcast |
| 9 | Shared mutable state appears | Race/corruption | No shared state (§8/§9) | A "cache" shared across modules | Only M4 caches, privately, rebuildable |
| 10 | Hidden coupling via a "utility" module | Silent god-dependency | **No utility module exists**; shared concerns are contracts | A helper grows responsibilities | Reject utility modules in review; keep leaves pure |
| 11 | Future auth forces restructuring | Boundary churn | Auth attaches at M5 seam (§13, [AP-13](../06-architecture-governance/01-architecture-principles.md)) | Identity leaks into M1 | accountId is an optional VO; M1/M2 untouched |
| 12 | AI players bypass Gameplay | Unfair/unvalidated moves | AI emits the **same** Commands to M1 (§13) | A privileged AI path is added | No module exposes M2 directly; all moves via M1 |
| 13 | Multiple modules own one invariant | Diffused guarantee | Each invariant owned once (§3/§9) | Decider vs committer confusion | M2 decides, M1 commits — documented per invariant |
| 14 | Cross-room coordination appears | Room isolation broken | No cross-room edges (§7, [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)) | Shared cache keyed globally | Per-room custody/state; isolation fitness check |
| 15 | Boundaries change due to implementation choice | Model drift | Modules are logical; tech is deferred (§1/§Constraints) | Framework tempts a merge | Technical Design maps *onto* modules; may not redraw them |

---

## 18. Module Readiness Review

**Overall assessment.** The decomposition is **complete and architecture-preserving**. Six MVP
modules (M1 Room & Lobby, M2 Gameplay, M3 Content, M4 Delivery, M5 Connectivity & Identity, M6
Recovery & State Custody) cover R-01…R-17, map 1:1 onto the [08.01 §2 contexts](01-domain-model-and-ubiquitous-language.md#2-bounded-context-identification)
and the [§20 seam](01-domain-model-and-ubiquitous-language.md#20-domain-readiness-review), and honor
every ADR (§15).

**Strengths.** Single-writer Authority isolated in M1; a genuinely **pure** M2; a clean by-ID
Content boundary; delivery/visibility consolidated with no authority; custody separated from
adjudication; a cycle-free dependency DAG; extension points that add modules rather than reshape them.

**Remaining risks.** (1) M1 breadth — watch for god-module drift (mitigated by delegation, §14).
(2) Temporal coupling in commit-then-broadcast (documented in C3/C4). (3) Distribution fencing
correctness — an [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) design/test focus, not a module-boundary gap.

**Open questions (non-blocking).** Whether Observability is worth a first-class module before ranked
features; snapshot cadence/retention (Technical Design, [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)); whether M1 orchestration should
be internally sub-modularized (an *internal* concern, not a boundary change).

**Deferred decisions.** All technology, deployment topology, project/folder/namespace structure, and
persistence/transport mechanisms — deferred to [09 Technical Design](../09-technical-design/README.md)+.

**Implementation impact.** Teams can own modules independently: M2 (rules) and M3 (content) are
leaf, independently testable; M1 integrates; M4/M5/M6 develop against one-way contracts (§10).

**Confidence level.** **High.** No new business concepts, no aggregate changes, no ADR conflicts.

**Recommendation.** Approve and proceed to **C4 views** (which visualize these modules) and
**Aggregate/Application-Layer design** (which refine within them). No later document may redefine
these boundaries.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-04 | Initial canonical Module Decomposition. Six MVP modules reconciled to the 08.01 §2 contexts and §20 seam (Projection→Delivery, Presence→Connectivity, Recovery kept distinct); responsibilities, boundaries, logical interfaces, dependency rules/graph, communication, state ownership, contracts, lifecycle, concurrency ownership, extension points, smell + adversarial reviews, and full ADR-000…ADR-010 compliance. Technology-neutral; no aggregate or ADR changes. |
| 1.1 | 2026-07-04 | Clarification only (no boundary change): §6 M1 "may depend on" row corrected to list **M2/M3/M6** as the true dependencies; M4 (publish) and M5 (signal) are one-way **flows**, and the dependency edges run **M4→M1 / M5→M1** (M1 functions if they are down). Aligns the table with the already-correct `Allowed` line and [08.04 §6](04-c4-container-diagram.md#6-container-dependency-rules). |
