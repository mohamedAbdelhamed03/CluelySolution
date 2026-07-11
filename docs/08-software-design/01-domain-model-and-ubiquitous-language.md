# Cluely — Domain Model & Ubiquitous Language

| | |
|---|---|
| **Document** | 08.01 — Domain Model & Ubiquitous Language |
| **Phase** | Software Design (first document) |
| **Version** | 1.1 |
| **Status** | Approved — canonical domain model |
| **Technology** | **Neutral.** No database, ORM, framework, API, protocol, or language is chosen or implied. This is a *conceptual* model. |
| **Purpose** | Transform the approved architecture (Discovery 01–11, [ADR-000…ADR-010](../07-software-architecture/12-decisions/README.md)) into a precise, technology-neutral **domain model** that every future design and implementation must conform to — the single source of truth for terminology, aggregates, entities, value objects, domain services, events, invariants, ownership, and lifecycle. |
| **Owner** | Lead Architect / Senior Engineers. |
| **Consumes (does not redefine)** | [Business Rules](../02-business-analysis/02-business-rules.md), [Business Invariants](../02-business-analysis/10-business-invariants.md), [Domain Events](../02-business-analysis/11-domain-events-catalog.md), [SRS](../02-business-analysis/01-software-requirements.md), [Architecture Discovery](../07-software-architecture/README.md), [ADR-000…ADR-010](../07-software-architecture/12-decisions/README.md). |

> **Reading contract.** This document **consumes** the frozen business and architecture layers; it
> never redefines them. Where a term, rule, invariant, or event already has a canonical home, this
> document **cites** it (`BR-*`, `INV-*`, `EVT-*`, `S-*`, `R-*`, `ADR-*`, `AI-*`) rather than
> restating it. Terminology follows [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md)
> **verbatim**, including its forbidden-terms list.

---

## Table of Contents
- [Domain Overview & Orientation](#domain-overview--orientation) *(read this first)*
1. [Ubiquitous Language](#1-ubiquitous-language)
2. [Bounded Context Identification](#2-bounded-context-identification)
3. [Aggregate Discovery](#3-aggregate-discovery)
4. [Entity Discovery](#4-entity-discovery)
5. [Value Object Discovery](#5-value-object-discovery)
6. [Domain Services](#6-domain-services)
7. [Domain Events](#7-domain-events)
8. [Aggregate Relationships](#8-aggregate-relationships)
9. [Ownership Matrix](#9-ownership-matrix)
10. [Lifecycle Models](#10-lifecycle-models)
11. [Business Invariants](#11-business-invariants)
12. [Domain Policies](#12-domain-policies)
13. [Domain Constraints](#13-domain-constraints)
14. [Domain State Model](#14-domain-state-model)
15. [Aggregate Boundary Validation](#15-aggregate-boundary-validation)
16. [Domain Smells](#16-domain-smells)
17. [Future Evolution](#17-future-evolution)
18. [Traceability](#18-traceability)
19. [Architecture Compliance Review](#19-architecture-compliance-review)
20. [Domain Readiness Review](#20-domain-readiness-review)
- **Appendices (quick reference):** [A. Classification Matrix](#appendix-a--domain-classification-matrix) · [B. Dependency Rules](#appendix-b--domain-object-dependency-rules) · [C. Not Part of the Domain Model](#appendix-c--not-part-of-the-domain-model) · [D. Aggregate Summary Cards](#appendix-d--aggregate-summary-cards) · [E. Aggregate Size Rationale](#appendix-e--aggregate-size-rationale) · [F. Future Extension Map](#appendix-f--future-extension-map) · [G. Domain Design Principles](#appendix-g--domain-design-principles) · [H. Executive Summary](#appendix-h--executive-summary)

---

## Domain Overview & Orientation

*Additive reader's aid (v1.1). Changes nothing in the model below — it summarizes it. For any
conflict, sections 1–20 and the cited `BR-*/INV-*/EVT-*/ADR-*` IDs are authoritative.*

### Conceptual ownership at a glance
Only conceptual ownership is shown — **no** databases, APIs, or technologies.
```text
Cluely Domain (two aggregates)

Room / Match Aggregate  ── root: Room
    ├── Participants        (Host is an assignment on one Participant)
    ├── Match               (Game State — a scope of Room, not a separate aggregate)
    ├── Board
    │     └── Cards         (each: Word + immutable ownership [the Key] + reveal flag)
    ├── Turn                (exactly one current)
    └── Dictionary Reference ──► (by ID only)

Dictionary Version Aggregate  ── root: Dictionary Version   [immutable, versioned]
    └── Words

Readers (own no authoritative state): Delivery → reads Room → role-filtered Projections
Signalers: Connectivity → system events → Room updates derived Presence
```

### Developer reading order
For onboarding, read in this order: **1** Ubiquitous Language → **3** Aggregate Discovery →
**4** Entity Discovery → **5** Value Objects → **6** Domain Services → **7** Domain Events →
**11** Business Invariants → **12** Domain Policies → **10** Lifecycle Models → **20** Readiness
Review. Use [Appendix A](#appendix-a--domain-classification-matrix) as the fastest "what is this
concept?" lookup and [Appendix D](#appendix-d--aggregate-summary-cards) for per-aggregate cards.

---

## 1. Ubiquitous Language

The official vocabulary developers, architects, testers, and product owners **must** use. Product
terms defer to the [Business Glossary](../03-business-governance/01-business-glossary.md);
architectural terms defer to [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md).
This section fixes their **domain-model** meaning and, critically, records the *synonyms that must
not be used* so the model, code, and tests read the same.

Each entry: **Definition · Business meaning · Architectural meaning · Common mistakes · Related terms
· Forbidden synonyms.**

### 1.1 Structural & lifecycle terms

#### Room
- **Definition:** A private, code-joined space that holds a set of Participants and, at most, one
  Match at a time.
- **Business meaning:** Where friends gather to play ([BR-RC](../02-business-analysis/02-business-rules.md), [BR-JR](../02-business-analysis/02-business-rules.md)).
- **Architectural meaning:** The **aggregate root** of the primary aggregate and the scope of the
  single-writer [Authority](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#authority) ([ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md), [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)).
- **Common mistakes:** Treating a Room as a mere data record; a Room *owns behavior and invariants*.
- **Related:** Match, Participant, Host, Room Entity, RoomCode.
- **Forbidden synonyms:** *session object*, *game thread*, *lobby record* (see ADR-000 §6).

#### Match
- **Definition:** One playthrough of the game inside a Room, from start to a terminal result.
- **Business meaning:** "A game" ([BR-GS](../02-business-analysis/02-business-rules.md)).
- **Architectural meaning:** The **Game State** sub-scope owned inside the Room aggregate (S-02/04);
  not a separate aggregate.
- **Common mistakes:** Modeling Match as its own aggregate — it shares the Room's consistency
  boundary and single writer.
- **Related:** Room, Board, Turn, GameResult, Game Phase.
- **Forbidden synonyms:** *game session*, *game instance* (as a separate lifecycle owner).

#### Board
- **Definition:** The fixed set of 25 Cards laid out for a Match.
- **Business meaning:** The 5×5 grid of words ([BR-BG](../02-business-analysis/02-business-rules.md); `25` cards, [INV-B1](../02-business-analysis/10-business-invariants.md)).
- **Architectural meaning:** **Board State** (S-03), owned by the Room aggregate; contains the *key*
  (immutable ownership) and mutable reveal flags.
- **Common mistakes:** Assuming the Board is regenerable mid-Match — it is generated once and fixed
  ([INV-B2](../02-business-analysis/10-business-invariants.md), [INV-B5](../02-business-analysis/10-business-invariants.md)).
- **Related:** Card, Key, CardPosition, RevealFlag.
- **Forbidden synonyms:** *grid model*, *board DTO*.

#### Card
- **Definition:** One of the 25 positioned words on the Board, with an immutable owning category and
  a reveal flag.
- **Business meaning:** A word a Spymaster clues toward and an Operative guesses.
- **Architectural meaning:** An **Entity** *within* Board State; identified by CardPosition; never
  referenced from outside the Room aggregate.
- **Common mistakes:** Exposing a Card's ownership to Operatives before reveal ([INV-B9](../02-business-analysis/10-business-invariants.md)).
- **Related:** CardPosition, CardOwnership (Key), RevealFlag, Word.
- **Forbidden synonyms:** *tile record*, *cell entity*.

#### Dictionary / Dictionary Version
- **Definition:** A **Dictionary** is a country-scoped body of playable words; a **Dictionary
  Version** is one immutable, published snapshot of it.
- **Business meaning:** The localized word source ([BR-DC](../02-business-analysis/02-business-rules.md)); only the word library is localized.
- **Architectural meaning:** The **second aggregate** (root = Dictionary Version), immutable and
  versioned ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md), [INV-D1](../02-business-analysis/10-business-invariants.md)); a Room references a *pinned* Dictionary Version **by ID only**.
- **Common mistakes:** Mutating a published version; sharing a live Dictionary object into a Room.
- **Related:** DictionaryReference, RegionCode, ContentVersion, Word.
- **Forbidden synonyms:** *word table*, *content DB*.

### 1.2 People & roles

#### Participant
- **Definition:** A person present in a Room for the Room's lifetime, holding a Team and Role once a
  Match is set up.
- **Business meaning:** A player in the room ([BR-JR](../02-business-analysis/02-business-rules.md), [BR-TA](../02-business-analysis/02-business-rules.md)).
- **Architectural meaning:** An **Entity** owned by the Room aggregate; identified by ParticipantId.
- **Common mistakes:** Conflating Participant (in-room membership) with a durable account — there are
  no accounts (future-auth seam only).
- **Related:** Player, Host, Team, Role, Presence, Session.
- **Forbidden synonyms:** *user*, *account* (reserved for the future Identity context).

#### Player
- **Definition:** A Participant while actively taking part in a Match.
- **Business meaning:** Everyday word for a Participant in a game.
- **Architectural meaning:** A *role framing* of Participant during a Match; **not** a separate
  entity.
- **Common mistakes:** Introducing a `Player` entity distinct from `Participant`.
- **Related:** Participant, Role, Team.
- **Forbidden synonyms:** *gamer*, *member record*.

#### Host
- **Definition:** The single Participant with room-management authority (start match, remove
  participant, etc.).
- **Business meaning:** The room's organizer ([BR-HOST](../02-business-analysis/02-business-rules.md), [BR-HM](../02-business-analysis/02-business-rules.md)).
- **Architectural meaning:** A **Host Assignment** — a role attribute of exactly one Participant
  ([INV-P*](../02-business-analysis/10-business-invariants.md), [INV-R*](../02-business-analysis/10-business-invariants.md)); reassigned by Host Transfer.
- **Common mistakes:** Modeling Host as an entity rather than an assignment; allowing two hosts.
- **Related:** Host Assignment, Ownership Transfer, Participant.
- **Forbidden synonyms:** *admin*, *owner* (of the room, in the DDD-ownership sense).

#### Team / Role
- **Definition:** **Team** = the two competing sides (the two colors). **Role** = Spymaster or
  Operative within a Team.
- **Business meaning:** [BR-TA](../02-business-analysis/02-business-rules.md), [BR-ASN](../02-business-analysis/02-business-rules.md); role distribution rules.
- **Architectural meaning:** Both are **Value Objects** describing a Participant's placement; not
  entities (a Team has no identity beyond its color within a Match).
- **Common mistakes:** Creating a `Team` entity with its own lifecycle; a Team is a partition of
  Participants + Color, not an aggregate.
- **Related:** Color, RoleAssignment, Participant.
- **Forbidden synonyms:** *squad*, *group entity*.

### 1.3 Play terms

#### Turn / Game Phase / Move
- **Definition:** **Turn** = a Team's period of play (clue then guesses). **Game Phase** = the
  Match's current stage (e.g., AwaitingClue, AwaitingGuess, Finished). **Move** = an accepted,
  state-changing play act (a Clue or a Guess).
- **Business meaning:** [BR-TO](../02-business-analysis/02-business-rules.md), [BR-TE](../02-business-analysis/02-business-rules.md); one active Turn at a time.
- **Architectural meaning:** The **Turn** is an Entity within Game State; Game Phase is derived state
  ([State Machines](../02-business-analysis/07-state-machines.md)); "Move" is realized as a **Command** ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)).
- **Common mistakes:** Two current Turns; treating a rejected intent as a Move.
- **Related:** Clue, Guess, GuessAllowance, Command.
- **Forbidden synonyms:** *round object* (Round is a business grouping, distinct — see glossary), *action record*.

#### Clue / Guess
- **Definition:** A **Clue** is a Spymaster's one word + a number. A **Guess** is an Operative's
  selection of a Card.
- **Business meaning:** [BR-CL](../02-business-analysis/02-business-rules.md) (clue validity), [BR-GV](../02-business-analysis/02-business-rules.md) (guess validity); guesses allowed = number + 1.
- **Architectural meaning:** Both are **Commands** (state-changing intents) whose outcomes are
  adjudicated by the Rules Core; the *recorded* Clue is a Value Object held by the Turn.
- **Common mistakes:** Letting the client decide a Guess's outcome (reveal/turn end) — only the
  Rules Core adjudicates.
- **Related:** Turn, GuessAllowance, CardRevealed, Move.
- **Forbidden synonyms:** *hint* (for Clue, in code), *pick record*.

### 1.4 Behavior, ownership & delivery terms
*(These defer to [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md) and are used here with the same meaning.)*

| Term | Domain-model use | Forbidden synonyms |
|------|------------------|--------------------|
| **Authority** | The Room aggregate as the sole decider/adjudicator for its Match. | controller, manager, engine (bare) |
| **Presence** | Derived connectivity status of a Participant (connected/disconnected/grace) — a Value Object computed from the Session ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)). | online-flag, heartbeat record |
| **Projection** | A read-only, role-filtered view of authoritative state ([ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)); never authoritative. | view model, DTO (as truth), payload |
| **Command** | A state-changing intent routed to the Authority ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). | request, message, call |
| **Query** | A read of role-filtered state that changes nothing ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). | fetch, getter (as authoritative op) |
| **Recovery** | Restoring a Room/Match to its last committed state within its lifetime ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)). | persistence, durability (long-term) |
| **Visibility** | The rule set governing which Role may observe which state ([ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)). | permissions (in the auth sense) |
| **Delivery** | Transport + role-filtering of committed state/events to Participants ([ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md)); never adjudicates. | gateway, socket layer (as decider) |
| **Aggregate / Entity / Value Object / Domain Event** | Standard DDD building blocks as defined in §§3–7, aligned to ADR-000. | table, row, struct, log message |

---

## 2. Bounded Context Identification

Contexts follow the boundaries named in [ADR-000 §7](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#7-naming-rules-for-future-adrs--architecture-documents)
and the [Responsibility Boundaries](../07-software-architecture/04-responsibility-boundaries.md). A
**bounded context** is a boundary within which a term has one precise meaning and one model. The
boundary exists to keep the hidden-information core (Play) small, pure, and independently correct.

| Context | Kind | Purpose |
|---------|------|---------|
| **Play (Gameplay)** | Core | Adjudicate a Match: board, turns, clues, guesses, reveals, terminal outcome. |
| **Room & Lobby** | Core | Room lifecycle, membership, host, team/role setup, match start. |
| **Content (Dictionary)** | Supporting | Provide immutable, versioned, country-scoped word sets. |
| **Delivery** | Supporting | Transport + role-filter committed state/events to Participants. |
| **Connectivity & Identity** | Supporting | Sessions, reconnection, and derived Presence. |
| **Administration** | Supporting (future-leaning) | Operational control of dictionaries/rooms; no gameplay authority. |
| **Analytics** | Generic (future) | Observe committed events for metrics; never influences play. |
| **Future: Identity** | Future | Durable accounts at the future-auth seam. |
| **Future: Matchmaking** | Future | Public pairing into rooms. |
| **Future: Tournament** | Future | Multi-match competitive structures over rooms. |

For each active context:

### 2.1 Play (Gameplay) — Core
- **Responsibilities:** Board generation, clue/guess validation & adjudication, reveal, turn
  progression, terminal evaluation (R-03/R-04 in [System Responsibilities](../07-software-architecture/03-system-responsibilities.md)).
- **Owned concepts:** Match/Game State, Board, Card, Turn, Clue (recorded), Guess (adjudicated),
  GameResult, the Key.
- **Public interface:** Accepts Commands (SubmitClue, SubmitGuess, EndTurn) from the Room context;
  emits Domain Events ([EVT-14…EVT-21](../02-business-analysis/11-domain-events-catalog.md)).
- **Dependencies:** Content (pinned Dictionary Version, by ID) for words at board generation only.
- **Forbidden dependencies:** Delivery, Connectivity, natural language, storage, transport
  ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md), [INV-D1](../02-business-analysis/10-business-invariants.md)). The Play model must be pure (Rules Core).
- **Why the boundary exists:** Purity and determinism make gameplay provably fair and testable, and
  keep the secret (the Key) in one small, auditable place.
- **Future evolution:** Bots/AI players submit the *same* Commands; ranked play adds context around
  it, never inside it.

### 2.2 Room & Lobby — Core
- **Responsibilities:** Create/join/leave, capacity, host assignment & transfer, team/role
  assignment, dictionary selection, match start ([BR-RC/JR/TA/ASN/HOST/HM/DC/GS](../02-business-analysis/02-business-rules.md)).
- **Owned concepts:** Room State, Participant, Host Assignment, Team/Role assignments, DictionaryReference (pinned).
- **Public interface:** Commands (CreateRoom, JoinRoom, LeaveRoom, TransferHost, AssignTeam/Role,
  SelectDictionary, StartMatch); emits [EVT-1…EVT-13](../02-business-analysis/11-domain-events-catalog.md).
- **Dependencies:** Content (to validate/pin a Dictionary Version); Play (to begin a Match).
- **Forbidden dependencies:** Direct manipulation of Board/Card/Key (that is Play's); Delivery internals.
- **Why the boundary exists:** Lobby churn (joins/leaves/host changes) has different consistency and
  frequency characteristics than adjudication, but **shares the Room's single writer** — so it is a
  *context within the same aggregate*, not a separate aggregate (see §15).
- **Future evolution:** Matchmaking creates rooms via the same Commands; spectators join as a Role.

### 2.3 Content (Dictionary) — Supporting
- **Responsibilities:** Publish immutable, country-scoped Dictionary Versions; answer "give me the
  pinned version V" ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md), [INV-D1/D2/D3](../02-business-analysis/10-business-invariants.md)).
- **Owned concepts:** Dictionary, Dictionary Version, Word, RegionCode, ContentVersion.
- **Public interface:** Queries only (resolve a DictionaryReference to words); **no** gameplay
  Commands.
- **Dependencies:** None on Play/Room (it is upstream, read-only to them).
- **Forbidden dependencies:** Any dependency on a Room or Match; it must not know who is playing.
- **Why the boundary exists:** Content changes on an editorial cadence, must be immutable once
  pinned, and must be swappable per region without touching gameplay.
- **Future evolution:** Custom/premium dictionaries add versions; the pin-by-ID contract is unchanged.

### 2.4 Delivery — Supporting
- **Responsibilities:** Transport committed state/events; **role-filter** into Projections
  ([ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md), [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)).
- **Owned concepts:** None authoritative — it holds *derived* Projections only.
- **Public interface:** Consumes Domain Events + snapshots; produces per-Role Projections; answers Queries.
- **Forbidden dependencies:** Adjudication, state ownership ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md), [INV-B9](../02-business-analysis/10-business-invariants.md)).
- **Why the boundary exists:** One auditable place guarantees no hidden-information leak.

### 2.5 Connectivity & Identity — Supporting
- **Responsibilities:** Sessions, reconnect tokens, derive Presence ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md), [BR-RX](../02-business-analysis/02-business-rules.md)).
- **Owned concepts:** Session, ReconnectToken; **Presence is derived**.
- **Public interface:** Emits system events (connect/disconnect); Room consumes them to update
  authoritative presence status.
- **Forbidden dependencies:** Gameplay adjudication; must never carry hidden information.
- **Why the boundary exists:** Transient connection concerns must not pollute the pure Play model and
  are the seam where durable Identity later attaches.

---

## 3. Aggregate Discovery

Applying DDD (an **aggregate** is the smallest set of objects that must stay transactionally
consistent under one authority, cf. [ADR-000 "Aggregate"](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#aggregate)),
Cluely has exactly **two** aggregates in the MVP core. Aggregates are **not** forced: Team, Turn,
Card, Participant, and Presence are consistency-coupled to a Room and therefore live *inside* it, not
beside it.

| Aggregate | Root | Rationale |
|-----------|------|-----------|
| **Room / Match** | Room | Single-writer, one consistency boundary per room ([ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md), [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)); all in-match invariants must hold together. |
| **Dictionary Version** | Dictionary Version | Immutable, versioned, referenced by ID only; different lifecycle and authority ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| *(future) Administration* | — | Deferred; operational, not gameplay. |
| *(future) Tournament* | — | Deferred; structures over rooms, references by ID. |
| *(future) Identity* | — | Deferred to the future-auth seam. |

### 3.1 Room / Match aggregate
- **Purpose:** Be the sole authority and consistency unit for one live room and its (at most one)
  active Match.
- **Aggregate root:** **Room** (identity: RoomId; discoverable by RoomCode).
- **Owned entities:** Participant, Board, Card, Turn (see §4).
- **Owned value objects:** RoomCode, Team, RoleAssignment, HostAssignment, Clue (recorded),
  GuessAllowance, CardPosition, CardOwnership (Key element), RevealFlag, RemainingCounts, GameResult,
  GamePhase, Presence, Version, DictionaryReference (see §5).
- **Owned domain events:** [EVT-1…EVT-9, EVT-11…EVT-21](../02-business-analysis/11-domain-events-catalog.md) and presence events EVT-22/23 (consumed→reflected).
- **Owned invariants:** all `INV-B*`, `INV-G*`, `INV-T*`, `INV-P*`, `INV-R*`, `INV-O*` (see §11).
- **Consistency boundary:** strong, **within the room** ([CB-01…CB-10](../07-software-architecture/06-consistency-boundaries.md)); none across rooms ([Room Isolation](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)).
- **Transaction boundary:** one Command commits atomically ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md); commit-then-broadcast); each commit advances the room's monotonic **Version**.
- **Lifecycle:** §10.1 (Created → Active → (Match: Setup → Playing → Finished) → Closed/Expired).
- **External references:** **DictionaryReference by ID only** to a Dictionary Version — never a
  navigational link, never ownership.
- **Dependencies:** Content (resolve pinned words at board generation); consumes Connectivity system
  events.
- **Forbidden references:** No reference from inside a Room to another Room; no Delivery/Connectivity
  objects held as owned state; no live Dictionary object.

### 3.2 Dictionary Version aggregate
- **Purpose:** Provide an immutable, versioned, country-scoped word set.
- **Aggregate root:** **Dictionary Version** (identity: RegionCode + ContentVersion).
- **Owned entities:** none required (Words are Value Objects within it).
- **Owned value objects:** Word, RegionCode, ContentVersion, word-count metadata.
- **Owned domain events:** publication events (a version becomes available) — see EVT-10 selection is
  a *Room* event; *publication* is a Content event (catalogued under Content, [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)).
- **Owned invariants:** immutability after publish, minimum word count ≥ 25 ([INV-D1/D2/D3](../02-business-analysis/10-business-invariants.md)).
- **Consistency boundary:** each version is internally consistent and **frozen**; no cross-version
  consistency needed.
- **Transaction boundary:** publication is atomic; thereafter read-only.
- **Lifecycle:** §10.4 (Draft → Published(immutable) → Deprecated; never Modified).
- **External references:** none to Rooms (upstream only).
- **Forbidden references:** must not reference or depend on any Room, Match, or Participant.

---

## 4. Entity Discovery

An **entity** has identity and a lifecycle. Below, only **Room** and **Dictionary Version** are
aggregate roots; all other entities are **internal** to the Room aggregate and are never referenced
from outside it.

### 4.1 Room *(aggregate root)*
- **Identity:** RoomId (stable); RoomCode (human-facing lookup, unique among active rooms).
- **Responsibilities:** Hold membership, host, dictionary pin, and the active Match; enforce all
  in-room invariants; serialize Commands.
- **Lifecycle:** §10.1.
- **Mutable state:** membership set, host assignment, team/role assignments, room phase, active Match,
  Version.
- **Immutable state:** RoomId, creation metadata, (once pinned) DictionaryReference for the current
  Match.
- **Relationships:** contains Participants, one Board/Turn while a Match runs; references a Dictionary
  Version by ID.
- **Ownership:** owner of Room State (S-01) and, transitively, all in-room state.
- **Business rules:** [BR-RC/JR/LR/HM/HOST/TA/ASN/DC/GS](../02-business-analysis/02-business-rules.md).
- **Architectural constraints:** single writer ([ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)); recoverable to last commit ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)).

### 4.2 Participant *(entity within Room)*
- **Identity:** ParticipantId (unique within the Room for its lifetime).
- **Responsibilities:** Represent one person's membership, team/role, host flag, and presence.
- **Lifecycle:** §10.2 (Joining → Active → Disconnected(grace) → Left/Removed).
- **Mutable state:** Team, RoleAssignment, HostAssignment flag, Presence, nickname.
- **Immutable state:** ParticipantId, join metadata.
- **Relationships:** belongs to exactly one Room; may hold a Session (Connectivity context) by ID.
- **Ownership:** owned by Room aggregate; **no external entity may reference a Participant except by
  ParticipantId** and only within the room's projections.
- **Business rules:** [BR-JR/LR/TA/ASN/HM](../02-business-analysis/02-business-rules.md); [INV-P*](../02-business-analysis/10-business-invariants.md).
- **Architectural constraints:** presence is derived from Session ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)).

### 4.3 Board *(entity within Room)*
- **Identity:** implicit — one Board per Match (BoardId scoped to the Match).
- **Responsibilities:** Hold the 25 Cards, the Key, and reveal state.
- **Lifecycle:** §10.8 (Generated(once) → Revealing → Frozen at terminal).
- **Mutable state:** per-Card reveal flags; RemainingCounts (derived/maintained).
- **Immutable state:** the 25 words, their positions, and their ownership (**the Key**) — fixed at
  generation ([INV-B2/B5](../02-business-analysis/10-business-invariants.md)).
- **Relationships:** contains 25 Cards; belongs to the active Match.
- **Ownership:** Board State (S-03), owned by Room aggregate; the Key is a server secret ([INV-B9](../02-business-analysis/10-business-invariants.md)).
- **Business rules:** [BR-BG](../02-business-analysis/02-business-rules.md); 9/8/7/1 split ([INV-B2](../02-business-analysis/10-business-invariants.md)).

### 4.4 Card *(entity within Board)*
- **Identity:** CardPosition (0–24) within its Board.
- **Responsibilities:** Carry one Word, its immutable CardOwnership, and its RevealFlag.
- **Lifecycle:** Unrevealed → Revealed (one-way).
- **Mutable state:** RevealFlag only.
- **Immutable state:** Word, CardPosition, CardOwnership.
- **Relationships:** part of exactly one Board.
- **Business rules:** reveal on guess ([BR-GV](../02-business-analysis/02-business-rules.md)); ownership hidden until revealed ([INV-B9](../02-business-analysis/10-business-invariants.md)).

### 4.5 Turn *(entity within Match)*
- **Identity:** TurnId / sequence within the Match (exactly one *current* Turn, [INV-T1](../02-business-analysis/10-business-invariants.md)).
- **Responsibilities:** Track the active Team, current Clue, GuessAllowance, and guesses used.
- **Lifecycle:** §10.9 (Started → AwaitingClue → AwaitingGuess → Ended).
- **Mutable state:** active Team, recorded Clue, remaining GuessAllowance, phase.
- **Immutable state:** TurnId, ordering.
- **Relationships:** belongs to the Match; produces Guess adjudications and reveals.
- **Business rules:** [BR-TO/TE/CL/GV](../02-business-analysis/02-business-rules.md); guesses = number + 1; [INV-T*](../02-business-analysis/10-business-invariants.md).

> **Not entities (deliberately):** **Clue** and **Guess** have no independent lifecycle — a Clue is a
> recorded **Value Object** on the Turn; a Guess is a transient **Command** whose *effects* (reveal,
> counts, turn end) are the persisted facts. **Team** is a Value-Object partition (Color + members),
> not an entity. **Host** is an **assignment** (a flag on one Participant), not an entity. **Match**
> is the Game-State scope of the Room, not a separate-identity entity. **Presence** is derived. See
> §15/§16 for why promoting any of these to an entity/aggregate would be a smell.

### 4.6 Dictionary Version *(aggregate root — Content context)*
- **Identity:** RegionCode + ContentVersion.
- **Responsibilities:** Provide an immutable set of Words for board generation.
- **Lifecycle:** §10.4.
- **Mutable state:** none after publish.
- **Immutable state:** all Words, counts, metadata.
- **Relationships:** referenced by Rooms **by ID only**.
- **Business rules:** [BR-DC](../02-business-analysis/02-business-rules.md); [INV-D1/D2/D3](../02-business-analysis/10-business-invariants.md); ≥ 25 words.

---

## 5. Value Object Discovery

A **Value Object (VO)** has no identity, is immutable, and is compared by value. Each below qualifies
because it is defined wholly by its attributes and is safely copyable/replaceable.

| VO | Belongs to | Why it is a VO (not an entity) | Traceability |
|----|-----------|--------------------------------|--------------|
| **RoomId** | Room | Opaque identity value; immutable. | S-01 |
| **RoomCode** | Room | A short code; two equal codes are interchangeable; immutable for the room's life. | [BR-RC](../02-business-analysis/02-business-rules.md) |
| **ParticipantId** | Participant | Identity value; no behavior/state of its own. | [INV-P*](../02-business-analysis/10-business-invariants.md) |
| **Team / Color** | Participant, Turn | A side is fully defined by its color; no independent lifecycle. | [BR-TA](../02-business-analysis/02-business-rules.md) |
| **RoleAssignment** | Participant | (Team, Role) pair; immutable once set for a Match. | [BR-ASN](../02-business-analysis/02-business-rules.md) |
| **HostAssignment** | Room/Participant | "who is host" is a value (a ParticipantId) reassigned atomically. | [BR-HM](../02-business-analysis/02-business-rules.md) |
| **CardPosition / Coordinates** | Card | Grid location (0–24 or row/col); pure value. | [INV-B1](../02-business-analysis/10-business-invariants.md) |
| **CardOwnership (Key element)** | Card | A category (starting/second/neutral/assassin); immutable. | [INV-B2](../02-business-analysis/10-business-invariants.md) |
| **RevealFlag** | Card | Boolean-like state value. | [BR-GV](../02-business-analysis/02-business-rules.md) |
| **Clue** | Turn | (word, number) recorded value; immutable once submitted. | [BR-CL](../02-business-analysis/02-business-rules.md) |
| **GuessAllowance** | Turn | number + 1, a computed value. | [BR-GV](../02-business-analysis/02-business-rules.md) |
| **RemainingCounts / Score** | Board/Match | Counts of unrevealed agents per team; derived value. | [INV-G*](../02-business-analysis/10-business-invariants.md) |
| **GamePhase** | Match | Enumerated stage value. | [State Machines](../02-business-analysis/07-state-machines.md) |
| **GameResult** | Match | (winner, reason) value fixed at terminal; immutable. | [BR-WIN/LOSE/TIE](../02-business-analysis/02-business-rules.md), [INV-G7](../02-business-analysis/10-business-invariants.md) |
| **Version** | Room | Monotonic integer; a pure value used for ordering/recovery. | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| **DictionaryReference** | Room | (RegionCode, ContentVersion) pin — a value pointing at a Dictionary Version by ID. | [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) |
| **RegionCode / ContentVersion** | Dictionary Version | Locale + version values. | [INV-D2](../02-business-analysis/10-business-invariants.md) |
| **Word** | Dictionary Version / Card | A word string value; interchangeable if equal. | [BR-DC](../02-business-analysis/02-business-rules.md) |
| **Presence** | Participant | Derived status value (connected/disconnected/grace). | [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md) |
| **VisibilityScope** | Projection | The set of state a Role may see; a value describing a filter. | [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| **ReconnectToken** | Session | An opaque credential value. | [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md), [BR-RX](../02-business-analysis/02-business-rules.md) |

**Not made VOs (to avoid over-modeling):** transient transport payloads, timers, and connection
handles are infrastructure, not domain values. **Primitive-obsession guard:** RoomCode, ParticipantId,
Version, RegionCode, ContentVersion, and CardPosition are modeled as VOs (not bare strings/ints) to
prevent mixing them up — see §16.

---

## 6. Domain Services

A **domain service** holds domain behavior that does not naturally belong to a single Entity/VO,
typically because it spans several or is a pure computation. All are **stateless** and (for Play)
**pure** (Rules Core, [AP-14](../06-architecture-governance/01-architecture-principles.md)).

| Domain Service | Why it is a service (not on an entity) | Owner / context | Traceability |
|----------------|----------------------------------------|-----------------|--------------|
| **Board Generation** | Combines a Dictionary Version + the 9/8/7/1 split into a new Board; spans Content + Board and needs randomness policy. | Play | [BR-BG](../02-business-analysis/02-business-rules.md), [INV-B1/B2](../02-business-analysis/10-business-invariants.md) |
| **Turn / Guess Evaluation (Adjudication)** | Decides a Guess's outcome (reveal, count change, turn end, terminal) from Board + Turn together; the canonical Rules-Core computation. | Play | [BR-GV/TE/TO](../02-business-analysis/02-business-rules.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md) |
| **Clue Validation** | Structural validity of a Clue independent of any one entity's state. | Play | [BR-CL](../02-business-analysis/02-business-rules.md) |
| **Victory / Terminal Evaluation** | Determines win/lose/tie across Board + Team counts per precedence; not owned by one entity. | Play | [BR-WIN/LOSE/TIE](../02-business-analysis/02-business-rules.md), [INV-G*](../02-business-analysis/10-business-invariants.md) |
| **Dictionary Selection** | Resolves a requested locale to a concrete Dictionary Version to pin. | Room ↔ Content | [BR-DC](../02-business-analysis/02-business-rules.md), [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) |
| **Host Transfer (Ownership Transfer)** | Moves the Host assignment deterministically when the host leaves. | Room | [BR-HM](../02-business-analysis/02-business-rules.md), [INV-P*](../02-business-analysis/10-business-invariants.md) |
| **Recovery Validation** | Rebuilds/validates a Room from its last snapshot + committed tail; spans the whole aggregate. | Room ↔ Recovery | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| **Projection Generation** | Produces a Role-appropriate view of committed state. | Delivery | [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| **Visibility Evaluation** | Decides, per Role, which fields are included (whitelist-by-inclusion). | Delivery | [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md), [INV-B9](../02-business-analysis/10-business-invariants.md) |
| **Distribution / Ownership-Fencing Validation** | Ensures exactly one Authority instance owns a room (monotonic epoch) when rooms distribute. | Room ↔ Distribution | [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) |

> **Purity boundary:** Play services (Board Generation, Adjudication, Clue/Terminal Evaluation) are
> pure and language/transport/storage-free ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md)). Projection/Visibility/Recovery/Distribution
> services are *not* part of the Rules Core and must never adjudicate.

---

## 7. Domain Events

The canonical catalog lives in [Domain Events (EVT-1…EVT-25)](../02-business-analysis/11-domain-events-catalog.md);
this section maps each to publisher/consumers/ordering/idempotency **without redefining** it. A
**Domain Event** is a past-tense business fact emitted **after commit** ([ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#domain-event)).

| Event | Publisher (Authority) | Primary consumers | Ordering | Idempotency | Importance |
|-------|----------------------|-------------------|----------|-------------|-----------|
| [EVT-1 RoomCreated](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: RoomId | High |
| [EVT-2 PlayerJoined](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,ParticipantId) | High |
| [EVT-3 PlayerLeft](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery, Host Transfer | per-room | key: (RoomId,ParticipantId,seq) | High |
| [EVT-4 RoomExpired](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery, cleanup | per-room, terminal | key: RoomId | High |
| [EVT-5 HostTransferred](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,seq) | High |
| [EVT-6 PlayerRemovedByHost](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,ParticipantId,seq) | Medium |
| [EVT-7 RoomClosed](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery, cleanup | per-room, terminal | key: RoomId | High |
| [EVT-8 TeamChanged](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,ParticipantId,seq) | Medium |
| [EVT-9 RoleChanged](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,ParticipantId,seq) | Medium |
| [EVT-10 DictionarySelected](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,seq) | Medium |
| [EVT-11 GameStarted](../02-business-analysis/11-domain-events-catalog.md) | Room | Play, Delivery | per-room | key: (RoomId,MatchId) | High |
| [EVT-12 BoardGenerated](../02-business-analysis/11-domain-events-catalog.md) | Play (Room) | Delivery (role-filtered) | after EVT-11 | key: (MatchId) | **Critical** (carries Key — Spymaster-only) |
| [EVT-13 StartingTeamSelected](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | after EVT-12 | key: (MatchId) | High |
| [EVT-14 TurnStarted](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | per-match | key: (MatchId,TurnId) | High |
| [EVT-15 RoundStarted](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | per-match | key: (MatchId,RoundId) | Medium |
| [EVT-16 ClueSubmitted](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | after TurnStarted | key: (TurnId,seq) | High |
| [EVT-17 GuessSubmitted](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | after ClueSubmitted | key: (TurnId,seq) | High |
| [EVT-18 CardRevealed](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | after GuessSubmitted | key: (MatchId,CardPosition) | **Critical** (mutates hidden info) |
| [EVT-19 TurnEnded](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | per-match | key: (MatchId,TurnId) | High |
| [EVT-20 RoundFinished](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery | per-match | key: (MatchId,RoundId) | Medium |
| [EVT-21 GameFinished](../02-business-analysis/11-domain-events-catalog.md) | Play | Delivery, Analytics | terminal | key: (MatchId) | **Critical** (terminal, once) |
| [EVT-22 PlayerDisconnected](../02-business-analysis/11-domain-events-catalog.md) | Connectivity→Room | Delivery, presence | per-room | key: (SessionId,seq) | Medium |
| [EVT-23 PlayerReconnected](../02-business-analysis/11-domain-events-catalog.md) | Connectivity→Room | Delivery, presence, Recovery | per-room | key: (SessionId,seq) | Medium |
| [EVT-24/25 GamePaused / GameResumed](../02-business-analysis/11-domain-events-catalog.md) | Room | Delivery | per-room | key: (RoomId,seq) | Medium |

**Cross-cutting rules:** every event is emitted **only after** its state change commits
(commit-then-broadcast, [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)); ordering is **total per room** and never assumed across
rooms ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)); consumers must treat events **idempotently** by the keys above ([ADR-000 Idempotency](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#idempotency-replay-safety)); events carrying hidden
information (EVT-12, EVT-18) are **role-filtered at Delivery** ([ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md), [INV-B9](../02-business-analysis/10-business-invariants.md)).

> Terminology note: the *architectural* term for the read view is **Projection**; "ProjectionGenerated"
> and "VisibilityUpdated" from the prompt are **not** business Domain Events — projection is a Delivery
> activity over committed events, not a new business fact. They are therefore **not** added to the
> EVT catalog (avoiding duplicate/invented events); Delivery may treat them as internal system events.

---

## 8. Aggregate Relationships

```
        pins (by ID only, one-way)
Room ─────────────────────────────►  Dictionary Version
(aggregate)   DictionaryReference       (aggregate, immutable)
   │ contains (owned)
   ├── Participant*        (never referenced from outside except by ParticipantId, inside projections)
   ├── Board ── Card*      (Card never referenced outside the Room)
   └── Turn                (one current)

Room ─X─ Room              (no reference between rooms — Room Isolation)
Dictionary Version ─X─ Room (Content never references a Room — upstream only)
Delivery ─reads─► Room     (derives Projections; owns nothing authoritative)
Connectivity ─signals─► Room (system events; Room updates authoritative presence)
```

- **Which aggregates reference others:** Room → Dictionary Version, **by ID only**, one-way, and only
  the *pinned* version for the current Match.
- **Which never reference each other:** Room ↔ Room (Room Isolation, [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)); Dictionary Version → anything
  downstream.
- **Who owns whom:** Room owns Participant/Board/Card/Turn; Dictionary Version owns its Words.
- **ID-only references:** Room→Dictionary Version; any external mention of a Participant/Card is by ID
  inside a Projection, never a live object handle.
- **Navigational references:** only *within* an aggregate (Room→Participant, Board→Card, Match→Turn).
- **Forbidden relationships:** cross-room references; Content→Room; Delivery/Connectivity owning
  authoritative state; a Room holding a live Dictionary object instead of a reference.

---

## 9. Ownership Matrix

"Owner" = the single writer accountable for the artifact ([ADR-000 Ownership](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#ownership)).

| Artifact | Owner | State ref | Notes |
|----------|-------|-----------|-------|
| Room State | Room aggregate | S-01 | Top-level; single writer per room. |
| Participant / membership | Room aggregate | S-05 | Team/role/host are attributes here. |
| Host Assignment | Room aggregate | S-05 | Exactly one; reassigned by Host Transfer. |
| Game State (Match) | Room aggregate | S-02/04 | Sub-scope of Room State. |
| Board State + Key | Room aggregate (Play) | S-03 | Key is a server secret; immutable. |
| Card reveal flags | Room aggregate (Play) | S-03 | Only reveal changes during play. |
| Turn / phase | Room aggregate (Play) | S-04 | Exactly one current Turn. |
| DictionaryReference (pin) | Room aggregate | S-01 | Reference by ID to Content. |
| Dictionary Version + Words | Dictionary Version aggregate | — | Immutable after publish ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| Version (monotonic) | Room aggregate | — | Advances on each commit ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)). |
| Session / ReconnectToken | Connectivity & Identity | S-06 | Transient; Room consumes signals. |
| Presence (status) | Room aggregate (derived) | S-06 | Derived from Session; reflected in state. |
| Projection | Delivery layer | — | **Derived, never authoritative** ([ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)). |
| Delivery/transport | Delivery layer | — | Reader only; owns no game state. |
| Snapshot / committed tail | Recovery (custody) | — | Holds; never adjudicates ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)). |

**Rule:** every artifact has **exactly one** owner; no artifact is co-owned. Ownership disputes are
resolved in favor of the Room aggregate for all in-room state and Content for all word data.

---

## 10. Lifecycle Models

Conceptual state models (no technology). Authoritative machines live in
[State Machines](../02-business-analysis/07-state-machines.md); these summarize them for the model.

### 10.1 Room
```
Created ──► Active(Lobby) ──► MatchInProgress ──► Active(Lobby, post-match) ──► Closed
     └────────────────────────────────────────────────► Expired
```
Guards: capacity/host rules ([BR-JR/HOST](../02-business-analysis/02-business-rules.md)); a Room holds ≤ 1 Match at a time.

### 10.2 Participant
```
Joining ──► Active ──► Disconnected(grace) ──► Reconnected(→Active)
   │           │                └──────────────► Left / Removed (grace expired or explicit)
   └───────────┴──► Left / Removed
```
Guards: [BR-JR/LR](../02-business-analysis/02-business-rules.md), reconnection [BR-RX](../02-business-analysis/02-business-rules.md)/[ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md).

### 10.3 Match (Game)
```
Setup(team/role/dictionary) ──► BoardGenerated ──► Playing(Turns) ──► Finished(terminal, immutable)
```
Guard: a Finished Match never resumes ([INV-G7](../02-business-analysis/10-business-invariants.md)).

### 10.4 Dictionary Version
```
Draft ──► Published (immutable) ──► Deprecated
```
No "Modified" transition exists ([INV-D1](../02-business-analysis/10-business-invariants.md), [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)).

### 10.5 Recovery
```
Interrupted ──► LoadSnapshot ──► ReplayCommittedTail ──► Validated ──► Resumed(at last commit, once)
```
No terminal effect is replayed ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)).

### 10.6 Presence (derived)
```
Connected ──► Disconnected ──► Grace ──► (Reconnected→Connected | Expired→Left)
```
Derived from Session ([ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)).

### 10.7 Host
```
Assigned ──► (holder leaves) ──► Transferred(deterministic) ──► Assigned(new)
```
Exactly one host at all times ([BR-HM](../02-business-analysis/02-business-rules.md)).

### 10.8 Board
```
Generated(once) ──► Revealing(reveal flags flip) ──► Frozen(at terminal)
```
Key immutable throughout ([INV-B2/B5](../02-business-analysis/10-business-invariants.md)).

### 10.9 Turn
```
Started ──► AwaitingClue ──► AwaitingGuess ──► Ended ──► (next Turn | Match Finished)
```
Exactly one current Turn ([INV-T1](../02-business-analysis/10-business-invariants.md)); guesses = number + 1 ([BR-GV](../02-business-analysis/02-business-rules.md)).

---

## 11. Business Invariants

Every invariant below is **owned** by the model but **defined** in
[Business Invariants](../02-business-analysis/10-business-invariants.md) (or the cited ADR). This
section states the model-level guarantee and its origin; it does not create new invariants.

| # | Invariant (guarantee the model must never violate) | Origin |
|---|-----------------------------------------------------|--------|
| I-1 | Exactly **one Authority** per room decides outcomes. | [ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md)/[ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md) |
| I-2 | Exactly **one Host** per room. | [BR-HM](../02-business-analysis/02-business-rules.md), [INV-P*](../02-business-analysis/10-business-invariants.md) |
| I-3 | Exactly **one current Turn** per Match. | [INV-T1](../02-business-analysis/10-business-invariants.md) |
| I-4 | Exactly **one Board** per Match, generated once. | [INV-B1](../02-business-analysis/10-business-invariants.md), [BR-BG](../02-business-analysis/02-business-rules.md) |
| I-5 | **25 Cards**, split **9/8/7/1**; the Key is immutable. | [INV-B1/B2/B5](../02-business-analysis/10-business-invariants.md) |
| I-6 | **Exactly one Assassin.** | [INV-B2](../02-business-analysis/10-business-invariants.md) |
| I-7 | **Hidden information stays hidden** (Key disclosed to Spymasters only, and revealed cards to all). | [INV-B9](../02-business-analysis/10-business-invariants.md), [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| I-8 | **At most one winner / one terminal result**; a Finished Match never resumes. | [INV-G7](../02-business-analysis/10-business-invariants.md), [BR-WIN/LOSE/TIE](../02-business-analysis/02-business-rules.md) |
| I-9 | Guesses allowed in a Turn = **clue number + 1**. | [BR-GV](../02-business-analysis/02-business-rules.md), [INV-T*](../02-business-analysis/10-business-invariants.md) |
| I-10 | **One participation record** per person per room; **one Team/Role** at a time. | [INV-P*](../02-business-analysis/10-business-invariants.md), [BR-TA/ASN](../02-business-analysis/02-business-rules.md) |
| I-11 | **Exactly one pinned Dictionary Version** per Match; immutable during the Match. | [INV-D1](../02-business-analysis/10-business-invariants.md), [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) |
| I-12 | Each commit **advances the room Version monotonically**; recovery restores the last commit **once**. | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| I-13 | **No cross-room state or reference**; rooms are isolated. | [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) |
| I-14 | A Card reveal is **one-way** and atomic with its count/turn effects. | [BR-GV](../02-business-analysis/02-business-rules.md), [INV-B*](../02-business-analysis/10-business-invariants.md) |

Full set (`INV-B1…B9, G1…G8, T1…T6, P1…P5, R1…R5, O1…O4, D1…D3`) is authoritative in the
[Invariants catalog](../02-business-analysis/10-business-invariants.md); the model conforms to all.

---

## 12. Domain Policies

A **policy** is a replaceable rule of *how* a decision is made, distinct from the invariant it must
respect. Each policy has one owner and is technology-neutral.

| Policy | What it governs | Owner | Must respect |
|--------|-----------------|-------|--------------|
| **Board Generation Policy** | Word selection + placement + 9/8/7/1 assignment + randomness. | Play | [BR-BG](../02-business-analysis/02-business-rules.md), [INV-B1/B2](../02-business-analysis/10-business-invariants.md) |
| **Starting-Team Policy** | Which team gets 9 and moves first. | Play | [BR-BG](../02-business-analysis/02-business-rules.md) |
| **Turn Progression Policy** | When a Turn ends (allowance exhausted, wrong guess, pass). | Play | [BR-TO/TE/GV](../02-business-analysis/02-business-rules.md) |
| **Victory Policy** | How win/lose/tie is decided per precedence (assassin > all-agents > counts). | Play | [BR-WIN/LOSE/TIE](../02-business-analysis/02-business-rules.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md) |
| **Visibility Policy** | Which fields each Role sees (whitelist-by-inclusion). | Delivery | [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md), [INV-B9](../02-business-analysis/10-business-invariants.md) |
| **Participation Policy** | Join/leave/capacity, team balancing rules. | Room | [BR-JR/LR/TA](../02-business-analysis/02-business-rules.md) |
| **Host Transfer (Ownership) Policy** | Deterministic successor when the host leaves. | Room | [BR-HM](../02-business-analysis/02-business-rules.md) |
| **Dictionary Selection Policy** | How a locale request maps to a concrete pinned version. | Room ↔ Content | [BR-DC](../02-business-analysis/02-business-rules.md), [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) |
| **Recovery Policy** | Snapshot cadence + replay + validation to last commit. | Recovery | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| **Reconnection Policy** | Grace window, token validation, resync-on-return. | Connectivity | [BR-RX](../02-business-analysis/02-business-rules.md), [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md) |

---

## 13. Domain Constraints

Hard limits the model enforces (values are owned by the
[Constants Catalog](../03-business-governance/03-business-constants-catalog.md); cited, not restated).

| Constraint | Value / rule | Source |
|-----------|--------------|--------|
| Cards per Board | **exactly 25** (5×5) | [CONST cards](../03-business-governance/03-business-constants-catalog.md), [INV-B1](../02-business-analysis/10-business-invariants.md) |
| Ownership split | **9 / 8 / 7 / 1** (starting / second / neutral / assassin), summing to 25 | [INV-B2](../02-business-analysis/10-business-invariants.md) |
| Assassin count | **exactly 1** | [INV-B2](../02-business-analysis/10-business-invariants.md) |
| Minimum players to start | **4** (2 teams × 2) | [Constants](../03-business-governance/03-business-constants-catalog.md), [BR-GS](../02-business-analysis/02-business-rules.md) |
| Maximum participants | room capacity (`ROOM_MAX_PLAYERS`) | [Constants](../03-business-governance/03-business-constants-catalog.md) |
| Roles per team | ≥ 1 Spymaster, ≥ 1 Operative | [BR-ASN](../02-business-analysis/02-business-rules.md) |
| Teams | exactly 2 | [BR-TA](../02-business-analysis/02-business-rules.md) |
| Clue | one word + a non-negative number | [BR-CL](../02-business-analysis/02-business-rules.md) |
| Guess allowance | clue number + 1 | [BR-GV](../02-business-analysis/02-business-rules.md) |
| Dictionary minimum words | **≥ 25** (hard floor) | [Constants](../03-business-governance/03-business-constants-catalog.md), [INV-D3](../02-business-analysis/10-business-invariants.md) |
| Pinned dictionary versions per Match | exactly 1, immutable | [INV-D1](../02-business-analysis/10-business-invariants.md) |
| Version monotonicity | strictly increasing per commit | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| Cross-room references | none | [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) |

---

## 14. Domain State Model

For each aggregate, classifying state by nature clarifies what must be persisted, recovered, or
recomputed.

### 14.1 Room / Match aggregate
| Class | Examples | Why |
|-------|----------|-----|
| **Authoritative** | membership, host, team/role, Board+Key, reveal flags, current Turn, phase, GameResult, Version, DictionaryReference | The truth; single-writer owned. |
| **Derived** | Presence, GamePhase (from machine), whose-turn indicators | Computed from authoritative state/session. |
| **Computed** | RemainingCounts, GuessAllowance, victory condition | Functions of Board/Turn; may be recomputed. |
| **Cached** | last broadcast Projection per Role | Delivery-side; rebuildable from authoritative state. |
| **Transient** | in-flight Command being validated, timers | Exists only during processing. |
| **Recoverable** | everything authoritative (via snapshot + committed tail) | Must survive interruption within room lifetime ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)). |
| **Discardable** | caches, transient timers, derived Presence detail | Rebuilt on recovery; not persisted as truth. |

### 14.2 Dictionary Version aggregate
| Class | Examples | Why |
|-------|----------|-----|
| **Authoritative** | Words, RegionCode, ContentVersion | The published, immutable content. |
| **Derived/Computed** | word count, index | Functions of the word set. |
| **Cached** | loaded word set in a running node | Rebuildable from the published version. |
| **Recoverable** | the published version (by ID) | Immutable; trivially reloadable. |
| **Discardable** | in-memory caches | Non-authoritative. |

---

## 15. Aggregate Boundary Validation

Deliberately attacking the boundaries (the prompt's stress test). The recurring pressure is to
**split the Room**; the recurring answer is that the architecture makes Room a single-writer
consistency unit, so splitting it would create distributed transactions the architecture explicitly
forbids.

| Challenge | Verdict | Reasoning |
|-----------|---------|-----------|
| *Can Board/Turn be their own aggregate?* | **No** | Reveal + count + turn-end + terminal must commit **atomically** ([INV-T*/B*/G*](../02-business-analysis/10-business-invariants.md)); separate aggregates ⇒ cross-aggregate transaction, violating [ADR-001/003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md). |
| *Can Participant/Lobby be its own aggregate?* | **No** | Host, team/role, and match-start invariants ([INV-P*/R*](../02-business-analysis/10-business-invariants.md)) couple membership to the Match; same single writer. |
| *Can Presence be its own aggregate?* | **No (it is derived)** | Presence is computed from Session; making it authoritative-separate would duplicate ownership. |
| *Does the Room violate SRP?* | **No** | Its single responsibility is *"be the authority for one room's consistency."* The sub-contexts (Play, Lobby) are cohesive facets of that one responsibility, not separate responsibilities. |
| *Does keeping Dictionary inside Room reduce coupling?* | **No — separate it** | Dictionary has a different lifecycle (immutable, editorial) and authority; embedding it would couple gameplay to content changes. Reference **by ID** ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| *Can any invariant be broken by the current boundaries?* | **No** | Every in-match invariant lies **within** the Room boundary, so a single atomic commit preserves them all. |
| *Should two aggregates merge (Room + Dictionary)?* | **No** | Different lifecycle, authority, and consistency needs; merging would make content edits contend with gameplay. |
| *Should the Room split to scale?* | **No — distribute whole rooms** | Scale is by **room count** across nodes with ownership fencing ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)), not by splitting a room. |

**On the "large aggregate" concern:** the Room is intentionally the *largest* consistency unit the
domain allows, because the game's fairness invariants are inherently whole-room and whole-match. Its
size is bounded by one room (small, finite: ≤ capacity participants, 25 cards, one turn), so it does
**not** grow unboundedly — the classic justification against large aggregates (unbounded collections,
contention) does not apply. See §16 (God Aggregate).

---

## 16. Domain Smells

Proactively identifying and mitigating future modeling problems.

| Smell | Present? | Mitigation |
|-------|----------|------------|
| **God Aggregate** | *Risk, addressed* | The Room is large but **bounded** (one room, 25 cards, one turn, ≤ capacity). Internal **contexts** (Play vs Lobby) keep it cohesive; only the *consistency* boundary is shared, not the code responsibilities. Contention is bounded because a single room's command rate is human-paced. |
| **Anemic Model** | *Avoided* | Behavior (adjudication, victory, host transfer, board generation) lives in domain services + the Room, not in a service layer over dumb data ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md)). |
| **Duplicate Ownership** | *Avoided* | §9 gives every artifact exactly one owner; Presence is *derived*, not co-owned. |
| **Circular References** | *Avoided* | References are one-way (Room→Dictionary by ID); no Room↔Room, no Content→Room. |
| **Leaky Boundaries** | *Guarded* | The Key never leaves Play except via role-filtered Projections at Delivery ([INV-B9](../02-business-analysis/10-business-invariants.md), [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)). |
| **Hidden Coupling** | *Guarded* | Content is upstream/immutable; Delivery/Connectivity are readers/signalers, not owners. |
| **Mutable Value Objects** | *Avoided* | All VOs in §5 are immutable; state changes replace values (e.g., a new Version, a flipped RevealFlag on the Card entity, not on a VO). |
| **Overloaded Entities** | *Watch* | Turn could accrete responsibilities; keep adjudication in the Rules-Core service, leaving Turn as state. |
| **Primitive Obsession** | *Avoided* | RoomCode, ParticipantId, Version, CardPosition, RegionCode/ContentVersion are VOs, not bare primitives (§5). |
| **Feature Envy / rules in transport** | *Guarded* | No rules in Delivery/Command carriers ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md), [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). |

---

## 17. Future Evolution

The model supports each roadmap capability **additively**, without changing the two aggregate
boundaries (per the [Roadmap](../03-business-governance/06-product-roadmap.md) and [AP-13](../06-architecture-governance/01-architecture-principles.md)).

| Future capability | How the model absorbs it (no boundary change) |
|-------------------|-----------------------------------------------|
| **Authentication / Accounts** | New **Identity** aggregate at the future-auth seam; a Participant gains an optional `accountId` VO. Room/Match unchanged. |
| **Spectators** | A new **Role** (VO) with a Visibility Policy that sees no Key; no new aggregate. |
| **Bots / AI Players** | A Participant whose Commands are machine-generated; identical Command contract ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). |
| **Ranked / Competitive** | A **Match Result** consumer (Analytics) + external ranking context referencing MatchId; play unchanged. |
| **Custom / Premium Dictionaries** | New Dictionary Versions (immutable) + entitlement in selection policy; pin-by-ID contract unchanged ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| **Teams / Friends / Organizations** | New contexts referencing ParticipantId/AccountId by ID; no in-room coupling. |
| **Tournaments** | New **Tournament** aggregate structuring many Matches by RoomId/MatchId (by ID only). |
| **Enterprise Edition** | Administration context gains scoping; core aggregates untouched. |

**Guarantee:** every future capability attaches **by ID** and **outside** the Room aggregate, or as a
new **Role/VO** inside it — never by splitting or merging the two core aggregates.

---

## 18. Traceability

Every concept traces to its origin. (Representative — the full ID sets are authoritative in the cited
catalogs.)

| Model element | Business | Architecture / ADR |
|---------------|----------|--------------------|
| Room / Match aggregate | [BR-RC/JR/GS](../02-business-analysis/02-business-rules.md), [INV-R*/G*](../02-business-analysis/10-business-invariants.md) | [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md), [ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md), [ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md), [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md) |
| Board / Card / Key | [BR-BG](../02-business-analysis/02-business-rules.md), [INV-B1/B2/B5/B9](../02-business-analysis/10-business-invariants.md) | [ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md), [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| Turn / Clue / Guess | [BR-CL/GV/TO/TE](../02-business-analysis/02-business-rules.md), [INV-T*](../02-business-analysis/10-business-invariants.md) | [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md), [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md) |
| Participant / Host / Team / Role | [BR-JR/LR/TA/ASN/HM/HOST](../02-business-analysis/02-business-rules.md), [INV-P*](../02-business-analysis/10-business-invariants.md) | [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md) |
| Presence / Session / Reconnect | [BR-RX](../02-business-analysis/02-business-rules.md) | [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md) |
| Dictionary Version / Word | [BR-DC](../02-business-analysis/02-business-rules.md), [INV-D1/D2/D3](../02-business-analysis/10-business-invariants.md) | [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) |
| Domain Events | [EVT-1…EVT-25](../02-business-analysis/11-domain-events-catalog.md) | [ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md), [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| Projection / Visibility | [INV-B9](../02-business-analysis/10-business-invariants.md) | [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| Delivery | — | [ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md) |
| Recovery / Version | — | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md) |
| Room Isolation / Distribution | — | [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) |
| Command / Query | [Command & Query Discovery](../07-software-architecture/07-command-query-discovery.md) | [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md) |
| Ubiquitous Language | [Glossary](../03-business-governance/01-business-glossary.md) | [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md) |
| Functional scope (rooms, matches, roles, play) | [SRS](../02-business-analysis/01-software-requirements.md) (functional requirements) | [ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md) |
| Future-auth seam (accounts) | [SRS §2.14 Future Authentication](../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations) | [ADR-000 Future-Auth Seam](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md#future-auth-seam), [AP-13](../06-architecture-governance/01-architecture-principles.md) |

**On the SRS as a named source (prompt §18):** the SRS is the *functional* specification; its
requirements are elaborated into the `BR-*` rules and `INV-*` invariants cited densely throughout
this document. Thus most model elements trace to the SRS **transitively** through those IDs; the two
rows above give the direct SRS anchors (overall functional scope and the future-auth seam) so the
named source is never silently absent.

No concept in this document appears without a traceable origin above or an inline citation in its
section.

---

## 19. Architecture Compliance Review

Explicit check that the model complies with each ADR.

| ADR | Requirement | Model compliance |
|-----|-------------|------------------|
| **[ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md)** Vocabulary | Use canonical terms | §1 uses ADR-000 terms verbatim; adds no new terminology (defers to Glossary for product terms). ✅ |
| **[ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md)** Style | Modular, single-writer, pure Rules Core | Room aggregate = single writer; Play services pure. ✅ |
| **[ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md)** Authority | One authoritative state | Room owns all in-room state (§9). ✅ |
| **[ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)** Coordination | Serialized per-room commands | One current Turn; commands serialized; atomic commit (§14). ✅ |
| **[ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md)** Delivery | Transport + filter, never adjudicate | Delivery holds only Projections; owns nothing (§9). ✅ |
| **[ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)** Recovery | Snapshot + committed tail, once | Version monotonic; recovery lifecycle §10.5; recoverable-state class §14. ✅ |
| **[ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)** Visibility | Whitelist projections; Key hidden | Visibility Policy §12; EVT-12/18 role-filtered §7; INV-B9 §11. ✅ |
| **[ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)** Distribution | Isolate rooms; fence ownership | No cross-room references §8; distribute whole rooms §15. ✅ |
| **[ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)** Content | Immutable versioned, pin by ID | Dictionary Version aggregate; DictionaryReference by ID §3/§8. ✅ |
| **[ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)** Participation | Session/presence/continuity | Participant lifecycle §10.2; Presence derived §5/§9. ✅ |
| **[ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)** Command/Query | Commands to Authority; Queries as projections | Moves = Commands; reads = Projection Queries §1/§6/§7. ✅ |

**Violations found:** none. **Terminology note reconciled:** the prompt's example events
"ProjectionGenerated"/"VisibilityUpdated" are intentionally **not** added as Domain Events (they are
Delivery/system activities), preserving the single EVT catalog (§7).

---

## 20. Domain Readiness Review

**Overall assessment.** The domain model is **complete and internally consistent**. It defines two
bounded aggregates (Room/Match, Dictionary Version), a full vocabulary, entities, value objects,
domain services, the event catalog mapping, invariants, policies, constraints, ownership, and
lifecycles — every element traced to a business rule/invariant/event or an ADR, and compliant with
all of ADR-000…ADR-010.

**Confidence.** **High.** The model introduces **no new business concepts** and **no new aggregate
boundaries** beyond what the frozen architecture already implied; it makes the implied model explicit
and defensible.

**Remaining risks.**
1. *Turn overloading* (§16) — mitigated by keeping adjudication in the Rules-Core service; watch during module design.
2. *Projection cache staleness vs. authoritative state* — a Delivery concern, bounded by commit-then-broadcast; revisit in the real-time design.
3. *Distribution fencing correctness* — deferred to [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)'s epoch mechanism; a design/test focus, not a model gap.

**Open questions (for later design, not blocking).**
- Exact snapshot cadence and committed-tail retention (Technical Design, per [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)).
- Whether "Round" warrants an entity if multi-round modes are added post-MVP (currently a business grouping).
- Spectator Visibility scope specifics (when spectators are introduced).

**Design decisions deferred (correctly not made here).** Persistence/data model, API/message
contracts, transport/protocol, storage, concurrency mechanism, and serialization format — all belong
to [09 Technical Design](../09-technical-design/README.md) and later. This document chooses none.

**Readiness for Module Decomposition.** **Ready.** The two aggregates, their contexts, and the
ownership matrix give module design clean seams: a pure Play module (Rules Core), a Room/Lobby module
(the aggregate’s orchestration), a Content module, a Delivery module (projections), and a
Connectivity module — each a conforming carrier of the Commands/Queries defined by [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md).

**Recommendations.**
1. Proceed to **08.02 Module Decomposition**, deriving modules from the contexts/aggregates here.
2. Keep this document the **single source** for domain terms; route any new concept through an update
   here (and, if architectural, through [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md)) before use in code.
3. When Technical Design begins, add *implementation-notes* back-links from entities/VOs to their
   data-model designs without altering the conceptual definitions.

---

# Appendices — Quick Reference

*The appendices are **additive discoverability aids**. They restate — never redefine — the model in
sections 1–20. No aggregate boundary, entity, value object, invariant, ownership, or traceability
decision is changed here. On any discrepancy, sections 1–20 and the cited IDs govern.*

## Appendix A — Domain Classification Matrix

The single fastest "what is this concept?" lookup (derived from §§3–6).

| Concept | Classification | Defined in |
|---------|----------------|-----------|
| Room | **Aggregate Root** | §3.1, §4.1 |
| Dictionary Version | **Aggregate Root** | §3.2, §4.6 |
| Participant | Entity (in Room) | §4.2 |
| Board | Entity (in Room) | §4.3 |
| Card | Entity (in Board) | §4.4 |
| Turn | Entity (in Match) | §4.5 |
| Match | **Domain scope** of Room (not an aggregate) | §1.1, §3.1 |
| Team / Color | Value Object | §5 |
| Role / RoleAssignment | Value Object | §5 |
| Host | **Assignment** (a flag on one Participant), not an entity | §1.2, §4.5 note |
| Clue | Value Object (recorded on Turn) | §5 |
| Guess | **Command** (its effects are the persisted facts) | §1.3 |
| DictionaryReference | Value Object (by-ID pin) | §5 |
| CardOwnership (the Key) | Value Object (immutable) | §5 |
| RevealFlag / CardPosition | Value Object | §5 |
| RemainingCounts / GameResult / Version | Value Object | §5 |
| Presence | **Derived** Value Object | §5, §9 |
| Word | Value Object (in Dictionary Version) | §5 |
| Projection | Architectural concept (derived, non-authoritative) | §1.4, [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md) |
| Delivery | Architectural service (reader) | §2.4, [ADR-004](../07-software-architecture/12-decisions/ADR-004-real-time-communication-delivery.md) |
| Command / Query | Interaction classification | §1.4, [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md) |
| Board Generation / Adjudication / Victory / Recovery / Visibility | Domain Services | §6 |

## Appendix B — Domain Object Dependency Rules

Explicit allowed/forbidden dependencies (a compact restatement of §8/§9) to prevent accidental
coupling during implementation.

**Allowed:**
```text
Room               owns Participant, Board, Turn; references Dictionary Version BY ID
Participant        references nothing outside its Room
Board              owns Cards
Card               references nothing
Turn               references nothing outside its Match
Dictionary Version references nothing outside the Content context
Delivery           reads Room only (to derive Projections)
Projection         derived from committed state; references no Authority
Connectivity       signals the Room (system events); owns no game state
```
**Forbidden:** Room → Room · Content → Room/Match/Participant · Delivery/Connectivity owning
authoritative state · a Room holding a live Dictionary object (must be by-ID) · any rule/adjudication
in a Command carrier, Delivery, or Projection ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md), [INV-B9](../02-business-analysis/10-business-invariants.md)).

## Appendix C — Not Part of the Domain Model

To prevent architectural drift, the following are **intentionally excluded** from this conceptual
model. They belong to later [09 Technical Design](../09-technical-design/README.md) and
[10 Implementation](../10-implementation/README.md), and none may leak a concept back into this model.

- **Persistence/storage:** database tables, schemas, ORMs, migrations, caches (as authoritative).
- **Transport/API:** HTTP/REST/GraphQL, real-time channels/sockets, controllers, endpoints, message
  brokers.
- **Contracts/encoding:** DTOs, serialization formats, wire schemas, versioned payloads.
- **Access mechanisms:** repositories, unit-of-work, query builders.
- **Security mechanisms:** authentication, authorization, tokens *as implemented* (the future-auth
  **seam** is a domain concept; its mechanism is not).
- **Runtime/infra:** containers, orchestration, message queues, in-memory stores, load balancers.

*(Per governance, this document names **no** specific product/framework; the list above is
illustrative of the **categories** that are out of scope, not a set of chosen technologies.)*

## Appendix D — Aggregate Summary Cards

**Room / Match Aggregate**
- **Purpose:** own every consistency rule inside one room and its (≤1) active Match.
- **Root:** Room.
- **Entities:** Participant · Board · Card · Turn.
- **Value Objects:** RoomCode · Team · RoleAssignment · HostAssignment · Clue · GuessAllowance ·
  CardPosition · CardOwnership (Key) · RevealFlag · RemainingCounts · GamePhase · GameResult ·
  Version · DictionaryReference · Presence (derived).
- **External references:** Dictionary Version — **by ID only**.
- **Consistency/transaction:** strong within the room; one Command commits atomically; Version++.
- **Invariants:** `INV-B*`, `INV-G*`, `INV-T*`, `INV-P*`, `INV-R*`, `INV-O*`.
- **Traceability:** [ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md)/[002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md)/[003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)/[005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)/[006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)/[007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)/[009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)/[010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md).

**Dictionary Version Aggregate**
- **Purpose:** provide an immutable, country-scoped word set for board generation.
- **Root:** Dictionary Version (RegionCode + ContentVersion).
- **Entities:** none (Words are Value Objects).
- **Value Objects:** Word · RegionCode · ContentVersion · word-count metadata.
- **External references:** none (upstream only; never references a Room).
- **Consistency/transaction:** frozen after publish; publication is atomic.
- **Invariants:** `INV-D1/D2/D3` (immutable, versioned, ≥ 25 words).
- **Traceability:** [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md).

## Appendix E — Aggregate Size Rationale

A concise, discoverable restatement of §15/§16 for future contributors who question the Room's size:

- **Why Room is a single aggregate:** the game's fairness invariants (reveal + counts + turn +
  terminal) are inherently **whole-match** and must commit atomically; a single writer per room makes
  them structural ([ADR-001](../07-software-architecture/12-decisions/ADR-001-overall-architecture-style.md), [ADR-003](../07-software-architecture/12-decisions/ADR-003-per-room-coordination-model.md)).
- **Why splitting it would weaken consistency:** any split (Board, Turn, Lobby as separate
  aggregates) creates cross-aggregate transactions the architecture forbids, risking observable
  half-applied state.
- **Why bounded size keeps it manageable:** one room is small and finite — ≤ capacity participants,
  exactly 25 cards, one current Turn — and command rate is human-paced, so contention is bounded.
- **Why it is not a God Aggregate:** it holds **one** responsibility (be the authority for one
  room's consistency); its internal *contexts* (Play vs Lobby) keep it cohesive; it never accretes
  unbounded collections. Scale is achieved by distributing **whole rooms** ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)), not by splitting one.

## Appendix F — Future Extension Map

How each roadmap capability integrates **without changing the two aggregate boundaries** (restates §17).

| Future capability | Aggregate change |
|-------------------|------------------|
| Authentication / Accounts | **None** (new Identity aggregate at the seam; Participant gains optional `accountId` VO) |
| Public Matchmaking | **None** (creates rooms via existing Commands) |
| Spectators | **None** (new Role/VO + Visibility Policy) |
| Bots | **None** (Participant with machine-generated Commands) |
| AI Players | **None** (same Command contract) |
| Ranked / Competitive | **None** to core (external ranking references MatchId) |
| Custom / Premium Dictionaries | **None** (new immutable Dictionary Versions) |
| Regional Packs | **None** (new Dictionary Versions) |
| Teams / Friends / Organizations | **Separate context** (references by ID) |
| Tournaments | **New aggregate** (structures Matches by ID) |
| Enterprise Edition | **None** to core (Administration context scoping) |

## Appendix G — Domain Design Principles

The principles this model applies (made explicit for consistency as the system evolves):

1. **Behavior over data** — logic lives in the model/domain services, not an anemic layer ([AAP-09](../06-architecture-governance/02-architecture-anti-principles.md)).
2. **One owner for every piece of state** — no co-ownership (§9).
3. **Strong aggregate boundaries** — atomic consistency within, by-ID references across (§3, §8).
4. **Explicit invariants** — every guarantee cited to an `INV-*`/ADR (§11).
5. **Immutable Value Objects** — change replaces values, never mutates them (§5, §16).
6. **Identity only where a lifecycle exists** — otherwise a Value Object (§4/§5).
7. **Technology independence** — no storage/transport/framework in the model ([Appendix C](#appendix-c--not-part-of-the-domain-model)).
8. **Business terminology over technical** — the ubiquitous language governs ([ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md), §1).
9. **Hidden information is a first-class domain concern** — the Key is protected end to end ([INV-B9](../02-business-analysis/10-business-invariants.md), [ADR-006](../07-software-architecture/12-decisions/ADR-006-role-based-information-visibility.md)).
10. **Recovery never changes business truth** — it restores the last committed state, once ([ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)).

## Appendix H — Executive Summary

For senior architects and new contributors who need the model in one page.

- **Aggregates (2):** **Room / Match** (root: Room; the single-writer consistency unit for one room)
  and **Dictionary Version** (immutable, versioned, referenced by ID only).
- **Entities:** Participant, Board, Card, Turn — all **internal to the Room aggregate**.
- **Value Objects:** Team, Role, Clue, DictionaryReference, CardOwnership (Key), RevealFlag,
  RemainingCounts, GameResult, Version, Presence (derived), Word, and the ID types (§5).
- **Authoritative state:** membership, host, team/role, Board + Key, reveal flags, current Turn,
  phase, GameResult, Version, DictionaryReference — all owned by the Room. Words are authoritative in
  the Dictionary Version.
- **Immutable:** the Key and card positions/words, a published Dictionary Version, and a Finished
  Match's result.
- **Derived:** Presence, GamePhase, RemainingCounts, GuessAllowance, and all Projections.
- **Most important invariants:** one Authority, one Host, one current Turn, one Board of 25 cards
  split 9/8/7/1 with one Assassin, hidden information stays hidden, one terminal result (no resume),
  guesses = clue number + 1, one pinned Dictionary Version, monotonic Version with once-only recovery,
  and no cross-room state (§11).
- **Most important architectural constraints:** single writer per room, commit-then-broadcast,
  role-filtered projections at Delivery, Dictionary referenced by ID, and scale by distributing whole
  rooms (§19).
- **Verdict:** ready for **08.02 Module Decomposition**; every future design derives from this model
  rather than redefining it.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-04 | Initial canonical Domain Model & Ubiquitous Language; first document of the Software Design phase. Consumes ADR-000…ADR-010 and the frozen business analysis; introduces no new business concepts or aggregate boundaries. |
| 1.1 | 2026-07-04 | Added additive discoverability aids only — Domain Overview & Orientation (overview diagram, reading order) and Appendices A–H (classification matrix, dependency rules, "not part of the model," aggregate summary cards, size rationale, future-extension map, design principles, executive summary). No change to the model, aggregate boundaries, entities, value objects, ownership, invariants, traceability, or any ADR. |
