# ADR-000 — Architecture Vocabulary & Canonical Definitions

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Numbering note** | Numbered "000" as the foundational reference, but authored **after** [ADR-001](ADR-001-overall-architecture-style.md): it captures and standardizes the terminology that emerged during Discovery and the first decision. |
| **Nature** | This ADR makes **no new architectural decision** and introduces **no new business concept**. It fixes vocabulary. It must not contradict [Business Analysis](../../02-business-analysis/README.md), [Architecture Discovery](../README.md), [Architecture Governance](../../06-architecture-governance/README.md), or [ADR-001](ADR-001-overall-architecture-style.md). |

---

## 1. Purpose

### Why architectural vocabulary matters
Architects, developers, reviewers, and testers must mean the *same thing* by the *same word*. This
document is the **single authoritative source for architectural terminology** across the project.
It is **not** the [Business Glossary](../../03-business-governance/01-business-glossary.md) (which
defines *product/gameplay* terms) and **not** an implementation guide; it defines the **architectural
language** used by every future ADR, design, implementation guideline, and code review.

### Why ambiguous terminology is an architectural risk
Ambiguous words cause divergent designs: if "state" means both "the authoritative room aggregate"
and "a UI flag," two teams build incompatible things. Vocabulary drift is how the anti-principles
([duplicated rules](../../06-architecture-governance/02-architecture-anti-principles.md),
[rules-in-transport](../../06-architecture-governance/02-architecture-anti-principles.md)) creep in
unnoticed. Fixing language early is the cheapest defect prevention available.

### How to use this document
- Every future architecture document **must** use these terms as defined.
- Reviews check terminology against ADR-000 ([Review Checklist A3](../../06-architecture-governance/05-architecture-review-checklist.md)).
- **No architectural document may introduce new terminology without first updating ADR-000** (§10).
- Where a term already existed with variants, the **Canonical Definition** (§3) reconciles them; the
  variants are listed in [Synonyms & Forbidden Terms](#6-synonyms--forbidden-terms).

---

## 2. Vocabulary Categories

Terms are grouped for navigation only; a term belongs to exactly one canonical definition regardless
of category.

| # | Category | Covers |
|---|----------|--------|
| A | **Core Architecture** | Style, module, responsibility, boundary, authority, ownership, service kinds, aggregate. |
| B | **Gameplay Architecture** | Room entity, rules core, room/game/board state, delivery boundary. |
| C | **State Management** | State, custody, snapshot, projection, state transition, commit. |
| D | **Consistency** | Consistency, strong/eventual, consistency boundary. |
| E | **Concurrency** | Coordination, serialization, single writer, reader, isolation, room isolation, determinism, atomicity. |
| F | **Communication** | Intent, command, query, domain event, system event, publisher/consumer. |
| G | **Recovery** | Recovery, snapshot (recovery sense), replay-safety/idempotency. |
| H | **Ownership & Access** | Owner, authority vs responsibility, reader, role filtering, hidden information. |
| I | **Quality & Governance** | Quality attribute, architectural driver, trade-off, principle, constraint, assumption, risk, debt, fitness function. |
| J | **Evolution & Deployment** | Evolution, deployment unit/topology (conceptual), future-auth seam. |

---

## 3. Canonical Definitions

Each entry: **Definition · Purpose · Context · Related Documents · Related ADRs · Common
Misunderstandings · Implementation Notes (only if needed) · Example Usage.** All definitions are
technology-, framework-, and language-independent.

### A — Core Architecture

#### Architecture Style
- **Definition:** The macro organizing pattern of the system — how responsibilities, state, and
  authority are structured at the highest level.
- **Purpose:** Provides the frame within which all other decisions must fit.
- **Context:** Cluely's style is fixed by [ADR-001](ADR-001-overall-architecture-style.md).
- **Related Documents:** [Architecture Overview](../01-architecture-overview.md). **Related ADRs:** ADR-001.
- **Common Misunderstandings:** Not a technology stack; not a deployment topology.
- **Example Usage:** "The style (ADR-001) requires a single writer per room."

#### Module
- **Definition:** A cohesive internal unit of the system with a single area of responsibility and an
  explicit boundary; the smallest unit reviewed for cohesion/coupling.
- **Purpose:** Localizes change and enables testability.
- **Context:** The modular monolith of ADR-001 is composed of modules (rules core, delivery boundary, etc.).
- **Related Documents:** [Responsibility Boundaries](../04-responsibility-boundaries.md). **Related ADRs:** ADR-001.
- **Common Misunderstandings:** A module is not a deployable service; not a source folder.
- **Example Usage:** "The dictionary provider is a module, not a separate service (in the MVP)."

#### Responsibility
- **Definition:** An accountability the system must fulfil — *what* it is responsible for, not *how*.
- **Purpose:** The atomic unit of architectural decomposition.
- **Context:** Catalogued as R-01…R-17.
- **Related Documents:** [System Responsibilities](../03-system-responsibilities.md).
- **Common Misunderstandings:** A responsibility is not a class, service, or component.
- **Example Usage:** "Adjudication (R-04) is the responsibility of the rules core."

#### Boundary
- **Definition:** An explicit line across which responsibilities, information, or authority are
  separated, and at which guarantees are enforced.
- **Purpose:** Where invariants are protected (e.g., role filtering at the delivery boundary).
- **Context:** Isolation boundaries and cohesion clusters in Discovery.
- **Related Documents:** [Responsibility Boundaries](../04-responsibility-boundaries.md).
- **Common Misunderstandings:** A boundary is conceptual; it is not necessarily a network hop.
- **Example Usage:** "Role filtering happens **at** the delivery boundary."

#### Authority
- **Definition:** The single component entitled to **decide and adjudicate** outcomes for a given
  scope (per room, the rules core within the room entity).
- **Purpose:** Guarantees one source of truth and deterministic outcomes.
- **Context:** Server-authority ([AP-03](../../06-architecture-governance/01-architecture-principles.md)); single writer per room ([ADR-001](ADR-001-overall-architecture-style.md)).
- **Common Misunderstandings:** Authority ≠ Responsibility (authority *decides*; a responsibility may
  merely *carry out or hold*). Authority ≠ "controller" (see forbidden terms).
- **Example Usage:** "The room entity is the authority for its match's outcomes."

#### Ownership
- **Definition:** The exclusive right and accountability to **modify** a given piece of state; the
  owner is the sole writer.
- **Purpose:** Enforces single-writer discipline and prevents corruption.
- **Context:** State ownership table S-01…S-10.
- **Related Documents:** [State Ownership](../05-state-ownership.md).
- **Common Misunderstandings:** Ownership ≠ Access. Owning state (may modify) is distinct from being a
  reader (may observe).
- **Example Usage:** "The rules core owns board reveal flags; delivery only reads them."

#### Application Service
- **Definition:** A thin coordinator that receives an intent, invokes validation/authorization, and
  hands the validated intent to the authority; it **does not adjudicate rules**.
- **Purpose:** Keeps orchestration separate from decision-making.
- **Context:** Framed by [ADR-001 §13](ADR-001-overall-architecture-style.md#13-impact-on-future-architecture).
- **Common Misunderstandings:** Not the place for game rules ([AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Example Usage:** "The submit-guess application service validates then delegates to the room entity."

#### Domain Service
- **Definition:** A stateless, pure unit of domain logic (notably the rules core) free of transport,
  storage, UI, and natural language.
- **Purpose:** Makes rules portable and testable; enforces one gameplay worldwide.
- **Context:** [AP-14](../../06-architecture-governance/01-architecture-principles.md), [INV-D1](../../02-business-analysis/10-business-invariants.md).
- **Common Misunderstandings:** Not an application service (which orchestrates); not infrastructure.
- **Example Usage:** "Clue structural validation lives in a domain service."

#### Aggregate
- **Definition:** The consistency unit of related state that changes together under a single authority
  and whose invariants must always hold as a whole.
- **Purpose:** Defines the atomic boundary of mutation and consistency.
- **Context:** The **Room/Match** is Cluely's primary aggregate ([ADR-001 §13](ADR-001-overall-architecture-style.md#13-impact-on-future-architecture)).
- **Related Documents:** [Domain Model](../../02-business-analysis/06-domain-model.md), [Consistency Boundaries](../06-consistency-boundaries.md).
- **Common Misunderstandings:** Not a database table; a business consistency unit.
- **Example Usage:** "Board, turn, and counts are inside the Room/Match aggregate."

### B — Gameplay Architecture

#### Room Entity
- **Definition:** The single logical authority that **owns one live room's state for the room's
  lifetime** and processes that room's intents **one at a time** (single writer).
- **Purpose:** Makes per-room determinism, atomicity, and precedence structural.
- **Context:** The organizing element of [ADR-001](ADR-001-overall-architecture-style.md).
- **Related Documents:** [State Ownership](../05-state-ownership.md), [Consistency Boundaries](../06-consistency-boundaries.md).
- **Common Misunderstandings:** Not a thread, process, actor library, or class — it is an
  architectural role (a single-writer authority scoped to a room). Its realization is deferred to
  ADR-003/ADR-007.
- **Example Usage:** "Two simultaneous guesses are serialized by the room entity."

#### Rules Core
- **Definition:** The pure, language-neutral domain service that validates and resolves clues/guesses,
  updates counts, and evaluates terminal conditions per precedence — the canonical source of outcomes.
- **Purpose:** Correctness, fairness, determinism, testability, one gameplay worldwide.
- **Context:** R-04; [AP-14](../../06-architecture-governance/01-architecture-principles.md).
- **Related Documents:** [Business Rules](../../02-business-analysis/02-business-rules.md), [Rule Precedence](../../02-business-analysis/16-rule-precedence.md).
- **Common Misunderstandings:** Not the transport/delivery; not aware of language or storage.
- **Example Usage:** "Only the rules core decides whether a guess ends the turn."

#### Room State
- **Definition:** The authoritative state of a room: membership, host, code, capacity, room lifecycle
  status, and (when a match is active) the Game State.
- **Purpose:** The top-level aggregate owned by the room entity.
- **Context:** S-01. **Related Documents:** [State Ownership](../05-state-ownership.md), [State Machines](../../02-business-analysis/07-state-machines.md).
- **Common Misunderstandings:** Room State *contains* Game State; they are not synonyms.
- **Example Usage:** "Host migration changes Room State, not Game State."

#### Game State
- **Definition:** The authoritative state of an active match: status, active team, turn, and Board
  State, plus the recorded result at completion.
- **Purpose:** The match-level consistency scope inside Room State.
- **Context:** S-02/04. **Related Documents:** [Domain Model](../../02-business-analysis/06-domain-model.md).
- **Common Misunderstandings:** Distinct from Room State (which also covers lobby/membership).
- **Example Usage:** "A finished Game State never resumes ([INV-G7](../../02-business-analysis/10-business-invariants.md))."

#### Board State
- **Definition:** The 25 word cards, their immutable ownership (the key), and their reveal flags.
- **Purpose:** The core hidden-information state; the key is a server secret until reveal.
- **Context:** S-03; [INV-B2/B5/B7/B9](../../02-business-analysis/10-business-invariants.md).
- **Common Misunderstandings:** Ownership (the key) is not the same as revealed state; only reveal
  flags change during play.
- **Example Usage:** "Board State ownership is immutable after generation."

#### Delivery Boundary
- **Definition:** The single conceptual boundary that **transports** authoritative state/events to
  participants and **filters** them by role; it never adjudicates.
- **Purpose:** The one auditable place that guarantees no hidden-information leak.
- **Context:** R-11; [QS-01](../09-quality-attribute-scenarios.md); [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md).
- **Common Misunderstandings:** Not a rules location; not a decision-maker.
- **Example Usage:** "Operative-facing payloads are produced at the delivery boundary."

### C — State Management

#### State
- **Definition:** Authoritative, persistent-for-the-room-lifetime information the system holds and
  mutates through defined transitions.
- **Purpose:** The thing ownership, consistency, and recovery are about.
- **Context:** S-01…S-10.
- **Common Misunderstandings:** "State" always means *authoritative* state here — not a transient UI
  flag or a client cache.
- **Example Usage:** "Only the owner may modify state."

#### State Custody
- **Definition:** The responsibility of **holding** authoritative state and supporting atomic commit
  and recovery reads — without adjudicating it.
- **Purpose:** Separates *holding* state from *deciding* outcomes.
- **Context:** R-14. **Related ADRs:** ADR-001 (custody seam), future ADR-005.
- **Common Misunderstandings:** Custody ≠ authority; the custodian never changes semantics.
- **Example Usage:** "State custody commits the new state before delivery broadcasts it."

#### State Transition
- **Definition:** A defined, permitted change from one state to another; undefined transitions are
  rejected by default.
- **Purpose:** Keeps state legal and predictable.
- **Context:** [State Machines](../../02-business-analysis/07-state-machines.md); [AP-09](../../06-architecture-governance/01-architecture-principles.md).
- **Common Misunderstandings:** Not any mutation — only whitelisted ones.
- **Example Usage:** "AwaitingClue → AwaitingGuess is a valid state transition."

#### Commit
- **Definition:** The atomic point at which a state change becomes the new authoritative truth; only
  after commit may it be broadcast ("commit-then-broadcast").
- **Purpose:** Prevents observing partial/half-applied state.
- **Context:** [Interactions I-08](../08-interaction-discovery.md).
- **Common Misunderstandings:** Not a database term here; a conceptual atomic boundary.
- **Example Usage:** "Reveal, counts, and turn change commit together, then broadcast."

#### Snapshot
- **Definition:** A consistent capture of authoritative state at a commit point, used for (a) recovery
  and (b) delivering current state on (re)connection.
- **Purpose:** Enables recovery and resynchronization.
- **Context:** [Interactions I-07/I-09](../08-interaction-discovery.md); future ADR-005.
- **Common Misunderstandings:** A snapshot for delivery is **role-filtered**; a recovery snapshot is
  the full authoritative state.
- **Example Usage:** "On reconnect, the player receives a role-filtered snapshot."

#### Projection
- **Definition:** A **read-only, role-appropriate view** derived from authoritative state for a
  specific audience (e.g., a Spymaster projection includes the key; an Operative projection never does).
- **Purpose:** The mechanism by which one truth yields different, safe views per role.
- **Context:** [Command & Query Discovery](../07-command-query-discovery.md); [INV-B9](../../02-business-analysis/10-business-invariants.md).
- **Common Misunderstandings:** A projection is derived, never authoritative; it must never contain
  data the audience may not see.
- **Example Usage:** "The Operative projection omits unrevealed ownership."

### D — Consistency

#### Consistency
- **Definition:** The property that observers never see contradictory authoritative state affecting an
  outcome.
- **Purpose:** Underpins fairness and correctness.
- **Common Misunderstandings:** Consistency ≠ Synchronization (see §8).
- **Example Usage:** "In-room game state requires consistency."

#### Strong Consistency
- **Definition:** All observers see the same authoritative value immediately after a commit; required
  for outcome-bearing state.
- **Purpose:** Guarantees correct, non-contradictory play.
- **Context:** [Consistency Boundaries](../06-consistency-boundaries.md) CB-01…CB-10.
- **Example Usage:** "Guess resolution is strongly consistent per room."

#### Eventual Consistency
- **Definition:** Observers converge to the same value over time; acceptable only for non-outcome
  signals (presence display, metrics).
- **Purpose:** Permits cheap, lag-tolerant signals where correctness is unaffected.
- **Context:** [Consistency Boundaries §"where eventual is acceptable"](../06-consistency-boundaries.md).
- **Common Misunderstandings:** Never acceptable for reveals/turns/results.
- **Example Usage:** "A 'connecting…' indicator may be eventually consistent."

#### Consistency Boundary
- **Definition:** A scope within which a defined consistency level is guaranteed (e.g., strong within a
  room; none across rooms).
- **Purpose:** Makes explicit where guarantees hold.
- **Context:** [Consistency Boundaries](../06-consistency-boundaries.md).
- **Example Usage:** "There is no consistency boundary spanning two rooms."

### E — Concurrency

#### Coordination
- **Definition:** The means by which concurrent intents to the same scope are ordered so outcomes are
  deterministic.
- **Purpose:** Prevents races on shared state.
- **Context:** Deferred mechanism to future ADR-003; the *requirement* is single-writer per room.
- **Common Misunderstandings:** Coordination is a requirement here, not a specific technology.
- **Example Usage:** "Per-room coordination guarantees first-valid-wins."

#### Serialization
- **Definition:** Processing a scope's intents strictly one at a time, in a defined order.
- **Purpose:** The concrete shape of per-room coordination in ADR-001.
- **Context:** [ADR-001](ADR-001-overall-architecture-style.md); CB-01.
- **Common Misunderstandings:** "Serialization" here means *ordering of processing*, **not** data
  encoding/marshalling (that meaning is forbidden — §6).
- **Example Usage:** "The room entity serializes guess processing."

#### Single Writer
- **Definition:** The rule that exactly one authority may modify a given state scope, eliminating write
  races by construction.
- **Purpose:** The core concurrency guarantee of ADR-001.
- **Context:** [State Ownership](../05-state-ownership.md); [ADR-001](ADR-001-overall-architecture-style.md).
- **Example Usage:** "Single writer per room makes CB-01/02/03 structural."

#### Reader
- **Definition:** Any responsibility that may **observe** state (possibly via a projection) but never
  modify it.
- **Purpose:** Distinguishes access from ownership.
- **Common Misunderstandings:** Delivery, connectivity, and observability are readers, never writers.
- **Example Usage:** "The delivery boundary is a reader of Game State."

#### Isolation
- **Definition:** The property that one scope's processing/state cannot interfere with another's.
- **Purpose:** Enables safe concurrency and scaling.
- **Example Usage:** "Turn resolution is isolated within its room."

#### Room Isolation
- **Definition:** The specific rule that rooms share **no mutable state** and do not depend on one
  another.
- **Purpose:** Concurrency safety and scale-by-room-count.
- **Context:** [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md); SCAL-1.
- **Example Usage:** "Room isolation lets rooms distribute across nodes later."

#### Determinism
- **Definition:** The same inputs and state always yield the same outcome, per the fixed precedence.
- **Purpose:** Provable fairness and reproducibility.
- **Context:** [Rule Precedence](../../02-business-analysis/16-rule-precedence.md); [AP-06](../../06-architecture-governance/01-architecture-principles.md).
- **Example Usage:** "Determinism is validated by property-based testing."

#### Atomicity
- **Definition:** A change applies fully or not at all; no partial/observable intermediate state.
- **Purpose:** Prevents corruption from interrupted multi-field mutations.
- **Context:** ENG-ST-02; [AP-08](../../06-architecture-governance/01-architecture-principles.md).
- **Example Usage:** "Reveal + count + turn update is atomic."

### F — Communication

#### Intent
- **Definition:** A participant's request to change state or act, submitted to the authority for
  validation and adjudication. **The canonical term** for an inbound action request.
- **Purpose:** Emphasizes that clients *propose*; the server *decides*.
- **Context:** [Command & Query Discovery](../07-command-query-discovery.md); [AP-03](../../06-architecture-governance/01-architecture-principles.md).
- **Common Misunderstandings:** An intent is not guaranteed to succeed; it is not a "request/response"
  contract (see forbidden terms).
- **Example Usage:** "The submit-clue intent is rejected if out of turn."

#### Command
- **Definition:** An intent that, when accepted, **changes** authoritative state (and typically emits
  events).
- **Purpose:** Classifies state-changing operations.
- **Context:** [Command & Query Discovery](../07-command-query-discovery.md).
- **Common Misunderstandings:** A command is a *classification* of an intent, not a separate transport.
- **Example Usage:** "Submit Guess is a command."

#### Query
- **Definition:** A read of current (role-filtered) state that changes nothing.
- **Purpose:** Classifies read operations; results are projections.
- **Context:** [Command & Query Discovery](../07-command-query-discovery.md).
- **Example Usage:** "Get Game State is a query returning a projection."

#### Domain Event
- **Definition:** A past-tense **business** fact emitted after a committed state change (e.g.,
  CardRevealed).
- **Purpose:** The observable record of what happened; drives delivery, results, observability.
- **Context:** [Domain Events Catalog](../../02-business-analysis/11-domain-events-catalog.md).
- **Common Misunderstandings:** A domain event is business-level, not an infrastructure/log message.
- **Example Usage:** "GameFinished is a domain event."

#### System Event
- **Definition:** A non-business, operational signal internal to the system (e.g., connectivity change,
  timer fire) that may trigger responsibilities but is not part of the business record.
- **Purpose:** Separates operational signals from business facts.
- **Context:** Connectivity/presence, expiry timers.
- **Common Misunderstandings:** Distinct from a domain event; system events must never carry
  business/hidden data to clients.
- **Example Usage:** "A disconnect is a system event that may pause play."

#### Publisher / Consumer
- **Definition:** **Publisher** = a responsibility that emits events after commit; **Consumer** = a
  responsibility that reacts to them.
- **Purpose:** Names the roles in event flow without implying a specific mechanism.
- **Context:** [Domain Events Catalog](../../02-business-analysis/11-domain-events-catalog.md).
- **Example Usage:** "The rules core is the publisher of CardRevealed; delivery is a consumer."

### G — Recovery

#### Recovery
- **Definition:** Restoring a room/match to its last consistent (committed) state after interruption,
  within the room's lifetime, without replaying terminal effects.
- **Purpose:** Reliability guarantee for in-progress matches.
- **Context:** ENG-RE-01; [QS-06](../09-quality-attribute-scenarios.md); future ADR-005.
- **Common Misunderstandings:** Recovery is room-lifetime scoped, **not** long-term durability.
- **Example Usage:** "Recovery restores to the last commit, once."

#### Idempotency (Replay-Safety)
- **Definition:** Applying the same intent/effect more than once has the same result as applying it
  once.
- **Purpose:** Makes retries, duplicates, and recovery safe.
- **Context:** CR-4; ENG-GP-02/FP-03.
- **Common Misunderstandings:** Idempotency is required for all state-changing intents.
- **Example Usage:** "A replayed guess is an idempotent no-op or rejection."

### H — Ownership & Access

#### Owner
- **Definition:** The single responsibility accountable for a state scope and its only writer.
- **Related:** [Ownership](#ownership). **Example Usage:** "The room entity is the owner of Game State."

#### Role Filtering
- **Definition:** Producing per-role projections at the delivery boundary so each participant receives
  only what their role permits.
- **Purpose:** The concrete mechanism protecting hidden information.
- **Context:** [Delivery Boundary](#delivery-boundary); [INV-B9](../../02-business-analysis/10-business-invariants.md).
- **Example Usage:** "Role filtering strips ownership from Operative projections."

#### Hidden Information
- **Definition:** State that must not be disclosed to certain roles until a defined event — chiefly
  unrevealed card ownership (the key), disclosed to Spymasters only.
- **Purpose:** The game's core secret; leaking it is existential failure.
- **Context:** [INV-B9](../../02-business-analysis/10-business-invariants.md); ENG-FP-01.
- **Example Usage:** "Hidden information never reaches an Operative on any path."

### I — Quality & Governance

#### Quality Attribute
- **Definition:** A measurable non-functional property (correctness, fairness, consistency,
  performance, etc.) the architecture must exhibit.
- **Context:** [Quality Metrics](../../03-business-governance/04-quality-metrics.md), [Quality Scenarios](../09-quality-attribute-scenarios.md).
- **Example Usage:** "Recoverability is a quality attribute with scenario QS-06."

#### Architectural Driver
- **Definition:** A force (business or engineering) that shapes architectural decisions.
- **Context:** [Architectural Drivers](../02-architectural-drivers.md).
- **Common Misunderstandings:** A driver is a *force*, not a decision.
- **Example Usage:** "Gameplay fairness is the top architectural driver."

#### Trade-off
- **Definition:** An explicit acceptance of losing some quality/benefit to gain another, with reasoning.
- **Context:** [Architecture Input §8](../../05-architecture-input/01-architecture-input-report.md#8-major-trade-offs); ADR-001 §7.
- **Example Usage:** "ADR-001 trades stateless interchangeability for determinism."

#### Architecture Decision
- **Definition:** A recorded, binding choice that resolves an architectural question (an ADR).
- **Context:** [Heuristics §8](../../06-architecture-governance/03-architecture-decision-heuristics.md).
- **Example Usage:** "ADR-001 is an architecture decision."

#### Architecture Principle
- **Definition:** A mandatory rule every decision must follow (`AP-*`).
- **Context:** [Architecture Principles](../../06-architecture-governance/01-architecture-principles.md).
- **Example Usage:** "AP-05 Fairness Before Optimization."

#### Architecture Constraint
- **Definition:** A non-negotiable condition imposed by a prior phase that bounds decisions.
- **Context:** [ADR-001 §4](ADR-001-overall-architecture-style.md#4-constraints-mandatory-from-prior-phases).
- **Common Misunderstandings:** A constraint is given, not chosen; distinct from a principle (a rule
  of practice) and an assumption (a belief).
- **Example Usage:** "Room isolation is a constraint from governance."

#### Architecture Assumption
- **Definition:** A stated belief taken as true for a decision, which would change the decision if
  false.
- **Purpose:** Separates facts from beliefs (see §8).
- **Common Misunderstandings:** An assumption must be labeled as such, never presented as fact.
- **Example Usage:** "Assumption: MVP concurrent-room load fits a single node."

#### Architecture Risk
- **Definition:** A possible future condition arising from an architectural choice that could harm a
  quality attribute.
- **Context:** [Architecture Risk Register](../../06-architecture-governance/07-architecture-risk-register.md); ADR §10.
- **Example Usage:** "In-process state loss is an architecture risk (mitigated by recovery)."

#### Architecture Debt
- **Definition:** A deliberately deferred or knowingly-suboptimal architectural choice that will cost
  more to change later, tracked for repayment.
- **Purpose:** Makes deferral explicit and revisitable.
- **Common Misunderstandings:** Debt is *acknowledged and tracked*, not accidental mess.
- **Example Usage:** "Deferring distribution is tracked as bounded architecture debt."

#### Architecture Fitness Function
- **Definition:** An objective, repeatable check that confirms the architecture still satisfies a
  required property as it evolves (e.g., "no delivery path exposes unrevealed ownership").
- **Purpose:** Guards invariants over time.
- **Context:** [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md); ADR §11 validation.
- **Common Misunderstandings:** A fitness function is a *check*, not a test case per se; it expresses a
  property to hold continuously.
- **Example Usage:** "A fitness function asserts determinism across interleavings."

### J — Evolution & Deployment

#### Evolution
- **Definition:** Planned, additive change to the architecture over time without violating fixed
  business/governance (e.g., distributing room entities, extracting services, adding auth).
- **Context:** [AP-13](../../06-architecture-governance/01-architecture-principles.md); [Roadmap](../../03-business-governance/06-product-roadmap.md); ADR-001 §9.
- **Common Misunderstandings:** Evolution is additive; it must not change the rules core or business.
- **Example Usage:** "Evolving toward distributed room entities is a contained change."

#### Deployment Unit / Topology (conceptual)
- **Definition:** **Deployment unit** = an independently deployable grouping (MVP: one, per ADR-001);
  **topology** = the conceptual arrangement of units and where room entities run.
- **Purpose:** Names deployment concepts without choosing infrastructure.
- **Common Misunderstandings:** These are conceptual here; concrete deployment is a later phase.
- **Example Usage:** "MVP topology is a single deployment unit with room affinity."

#### Future-Auth Seam
- **Definition:** The isolated point (identity) where durable authentication can attach later without
  changing rules or workflows.
- **Context:** [AP-13](../../06-architecture-governance/01-architecture-principles.md); [SRS §2.14](../../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations).
- **Example Usage:** "Accounts attach at the future-auth seam."

*(Business terms — Room, Match, Team, Spymaster, Operative, Clue, Guess, Assassin, Dictionary,
Session, Presence, Host, Reconnect Token — are defined canonically in the
[Business Glossary](../../03-business-governance/01-business-glossary.md) and are **not** redefined
here. Where this ADR uses them architecturally, it defers to that glossary.)*

> **Session** and **Presence** clarification (architectural use, deferring to the business glossary):
> **Session** = a player's live, transient participation and identity in a room (owned by
> Connectivity & Identity); **Presence** = the current connectivity status derived from the session
> (connected/disconnected/grace). Session is the *what*; presence is the *observable status*.

---

## 4. Mandatory Terms — Coverage Check

All mandated terms are defined above or explicitly deferred to the Business Glossary:

Architecture Style ✓ · Module ✓ · Responsibility ✓ · Boundary ✓ · Authority ✓ · Ownership ✓ ·
Room Entity ✓ · Room State ✓ · Game State ✓ · Board State ✓ · Session (✓ arch note + glossary) ·
Presence (✓ arch note + glossary) · Intent ✓ · Command ✓ · Query ✓ · Domain Event ✓ ·
System Event ✓ · State Transition ✓ · Consistency ✓ · Strong Consistency ✓ · Eventual Consistency ✓ ·
Consistency Boundary ✓ · Coordination ✓ · Serialization ✓ · Single Writer ✓ · Reader ✓ ·
State Custody ✓ · Recovery ✓ · Snapshot ✓ · Projection ✓ · Role Filtering ✓ · Hidden Information ✓ ·
Determinism ✓ · Atomicity ✓ · Isolation ✓ · Room Isolation ✓ · Rules Core ✓ · Delivery Boundary ✓ ·
Application Service ✓ · Domain Service ✓ · Aggregate ✓ · Invariant (↓) · Validation (↓) ·
Quality Attribute ✓ · Architectural Driver ✓ · Trade-off ✓ · Architecture Decision ✓ ·
Architecture Principle ✓ · Architecture Constraint ✓ · Architecture Assumption ✓ · Architecture Risk ✓ ·
Architecture Debt ✓ · Architecture Fitness Function ✓ · Evolution ✓.

- **Invariant** — a condition that must always hold; canonical set in [Business Invariants](../../02-business-analysis/10-business-invariants.md) and defined generically in the [standards](../../_meta/01-documentation-standards.md). Architecturally: a property the design must never allow to be violated.
- **Validation** — the gate that checks an intent against role/team/phase/state before any effect, producing a catalogued rejection; canonical in [Validation Rules](../../02-business-analysis/09-validation-rules.md). Architecturally distinct from **adjudication** (deciding an outcome) — validation *admits or rejects*; the rules core *resolves*.

---

## 5. Relationships

Technology-neutral conceptual chains (no C4, no components chosen).

### 5.1 The intent lifecycle
```
Intent → Validation → (Authority: Rules Core adjudicates) → State Transition → Commit → Domain Event → Delivery (role-filtered Projection) → participants
```

### 5.2 State containment
```
Room Entity
  owns → Room State
             contains → Game State
                            contains → Board State (words + key + reveal flags)
                                        Turn (phase, active team, active clue, allowance)
```

### 5.3 Ownership vs access
```
Owner (single writer)  ── may modify ──►  State
Reader (delivery, connectivity, observability) ── may only observe (via Projection) ──►  State
Hidden Information ── disclosed only via Role Filtering at the Delivery Boundary
```

### 5.4 Consistency scope
```
Strong Consistency  ── holds within ──►  a Room (Consistency Boundary = room)
No consistency relationship  ── across ──►  different Rooms (Room Isolation)
Eventual Consistency  ── acceptable only for ──►  non-outcome signals (presence display, metrics)
```

### 5.5 Governance chain
```
Architectural Driver → Architecture Principle / Constraint → Architecture Decision (ADR) → Consequence + Risk → Fitness Function (validates) → Evolution
```

---

## 6. Synonyms & Forbidden Terms

| Use (canonical) | Instead of (deprecated/forbidden) | Why |
|-----------------|-----------------------------------|-----|
| **Intent** | Request, message, call | "Request" implies a request/response contract and client entitlement; clients *propose*, the authority *decides*. |
| **Room Entity** | Game thread, game loop, game object, session object | Names an implementation mechanism, not the architectural single-writer authority; conflates concurrency tech with role. |
| **Authority** | Controller, manager, engine (bare) | "Controller/manager" are vague and imply frameworks; "engine" alone is ambiguous (use **Rules Core** for the pure decision logic). |
| **Rules Core** | Game logic, business logic layer, engine | Precise, pure, language-neutral decision service; avoids vague "logic layer". |
| **Delivery Boundary** | Gateway, socket layer, transport (as adjudicator) | Delivery only transports + filters; must never be described as deciding. |
| **Ownership / Owner** | "the component that has the data" | Ownership = exclusive write right; be explicit. |
| **Reader** | "consumer of state" (when meaning read access) | Reserve **Consumer** for event reaction; **Reader** for state observation. |
| **Serialization (of intents)** | (never use "serialization" for data encoding) | Ambiguous with marshalling; for data encoding use "encoding"/"marshalling" (a later, implementation concern). |
| **Projection** | View model, DTO, payload (as authoritative) | A projection is a derived, role-safe read; never authoritative. |
| **Domain Event** | Log, notification (as business record) | Business fact vs operational/log message. |
| **System Event** | Domain event (for operational signals) | Keep business facts and operational signals distinct. |
| **State Custody** | Repository, database, store (as decision-maker) | Custody holds; it never adjudicates; storage tech is deferred. |
| **Recovery** | Persistence, durability (as long-term) | Recovery is room-lifetime; not long-term storage. |
| **Consistency** | Synchronization | Different concepts (see §8). |
| **Adjudication / Resolve** | Validation (when deciding outcomes) | Validation admits/rejects; the Rules Core adjudicates outcomes. |

**Terms that should never be used** in architecture documents (they pre-bias technology/patterns
without a decision): specific framework/library names, specific datastore/protocol names, and pattern
names asserted as chosen (e.g., "the actor system", "the event bus") unless an ADR has explicitly
decided them. Document alternatives, don't assume them ([AAP-12](../../06-architecture-governance/02-architecture-anti-principles.md)).

---

## 7. Naming Rules (for future ADRs & architecture documents)

Use these suffixes/roles consistently:

| Suffix / role | Use it for | Do not use it for |
|---------------|-----------|-------------------|
| **State** | Authoritative, owned information (Room State, Game State, Board State) | Transient UI flags, client caches. |
| **Session** | A player's transient live participation/identity in a room | Any long-lived/account concept. |
| **Context** | A bounded area of the domain (Play, Room & Lobby, Content, Delivery, Connectivity & Identity) | A component or service instance. |
| **Service** | A stateless unit: **Application Service** (orchestrates) or **Domain Service** (pure logic) | A stateful owner (use **Entity/Authority**). |
| **Boundary** | A line where a guarantee is enforced (Delivery Boundary, Consistency Boundary) | A general component. |
| **Coordinator** | A responsibility that orders concurrent work within a scope | A rules decision-maker (that is the Rules Core/Authority). |
| **Provider** | A read-mostly supplier of external-ish content (Dictionary Provider) | A state owner or decision-maker. |
| **Manager** | **Avoid** where possible; if unavoidable, only for lifecycle/connectivity bookkeeping (e.g., Connection Management) | Anything that decides outcomes. |
| **Resolver** | A pure function that computes an outcome from inputs (within the Rules Core) | Anything with side effects/state. |
| **Dispatcher** | A router of intents to the owning authority | A decision-maker. |
| **Publisher / Consumer** | Emitter / reactor of events | State access (use Reader/Owner). |

General rules: prefer **role names** (Authority, Owner, Reader) over **mechanism names** (thread,
queue, bus); prefer **precise** over **generic** ("Rules Core" over "engine"); never bake a
technology or an undecided pattern into a name.

---

## 8. Architectural Language Guidelines

Write to make distinctions unmistakable. Each guideline with an example:

1. **Distinguish facts from assumptions.** State facts plainly; label beliefs.
   - *Fact:* "The single writer serializes guesses (ADR-001)." *Assumption:* "Assumption: MVP load fits one node."
2. **Distinguish ownership from access.** Owning = may modify; access = may read.
   - "Delivery **reads** Game State; it does not **own** it."
3. **Distinguish command from event.** A command is a requested change; an event is a committed fact.
   - "Submit Guess (command) may result in CardRevealed (domain event)."
4. **Distinguish domain event from system event.** Business fact vs operational signal.
   - "CardRevealed is a domain event; a disconnect is a system event."
5. **Distinguish consistency from synchronization.** Consistency = observers don't see contradictory
   truth; synchronization = timing/coordination of activities. A system can coordinate (synchronize)
   without guaranteeing consistency, and vice versa.
   - "We require strong **consistency** for reveals, achieved by serialized **coordination**."
6. **Distinguish authority from responsibility.** Authority decides outcomes; a responsibility may only
   hold/transport/signal.
   - "State custody is a **responsibility**; the Rules Core is the **authority**."
7. **Distinguish validation from adjudication.** Validation admits/rejects an intent; adjudication
   resolves the outcome.
   - "Validation rejects an out-of-turn guess before the Rules Core would adjudicate it."
8. **Distinguish projection from state.** A projection is derived and role-safe; state is authoritative.
   - "Send the Operative **projection**, never the raw Board **State**."
9. **Name the consistency scope.** Always say *within a room* or *across rooms*.
10. **Prefer role over mechanism.** Say "the authority for the room," not "the thread/actor."

---

## 9. Traceability

| Term group | Originating Business Docs | Discovery Docs | ADR References | Governance References | Future Architecture Docs |
|-----------|---------------------------|----------------|----------------|-----------------------|--------------------------|
| Core Architecture (style, module, authority, ownership, services, aggregate) | [Domain Model](../../02-business-analysis/06-domain-model.md) | [01](../01-architecture-overview.md), [03](../03-system-responsibilities.md), [04](../04-responsibility-boundaries.md) | ADR-001 | [AP-03/07/14/16](../../06-architecture-governance/01-architecture-principles.md) | ADR-002, ADR-003, future design |
| Gameplay Architecture (room entity, rules core, states, delivery boundary) | [Business Rules](../../02-business-analysis/02-business-rules.md), [Invariants](../../02-business-analysis/10-business-invariants.md) | [03](../03-system-responsibilities.md), [05](../05-state-ownership.md) | ADR-001 | [AP-05/11](../../06-architecture-governance/01-architecture-principles.md) | ADR-002, ADR-006 |
| State Management (state, custody, snapshot, projection, transition, commit) | [State Machines](../../02-business-analysis/07-state-machines.md) | [05](../05-state-ownership.md), [08](../08-interaction-discovery.md) | ADR-001 | [AP-07/08/09](../../06-architecture-governance/01-architecture-principles.md) | ADR-002, ADR-005, ADR-010 |
| Consistency (strong/eventual, boundary) | — | [06](../06-consistency-boundaries.md) | ADR-001 | [AP-06](../../06-architecture-governance/01-architecture-principles.md) | ADR-003, ADR-004 |
| Concurrency (coordination, serialization, single writer, isolation, determinism, atomicity) | [Rule Precedence](../../02-business-analysis/16-rule-precedence.md) | [06](../06-consistency-boundaries.md), [08](../08-interaction-discovery.md) | ADR-001 | [AP-06/18](../../06-architecture-governance/01-architecture-principles.md), [AAP-08/14](../../06-architecture-governance/02-architecture-anti-principles.md) | ADR-003, ADR-007 |
| Communication (intent, command, query, events, publisher/consumer) | [Domain Events](../../02-business-analysis/11-domain-events-catalog.md) | [07](../07-command-query-discovery.md), [08](../08-interaction-discovery.md) | ADR-001 | [AP-03](../../06-architecture-governance/01-architecture-principles.md) | ADR-004, ADR-010 |
| Recovery (recovery, idempotency, snapshot) | — | [08](../08-interaction-discovery.md) | ADR-001 | — | ADR-005 |
| Ownership & Access (owner, role filtering, hidden information) | [Invariants INV-B9](../../02-business-analysis/10-business-invariants.md) | [05](../05-state-ownership.md), [09](../09-quality-attribute-scenarios.md) | ADR-001 | [AP-05/11](../../06-architecture-governance/01-architecture-principles.md), [AAP-03](../../06-architecture-governance/02-architecture-anti-principles.md) | ADR-006 |
| Quality & Governance (driver, trade-off, principle, constraint, assumption, risk, debt, fitness fn) | [Quality Metrics](../../03-business-governance/04-quality-metrics.md) | [02](../02-architectural-drivers.md), [09](../09-quality-attribute-scenarios.md) | ADR-001 | [Governance set 29–35](../../06-architecture-governance/README.md) | all future ADRs |
| Evolution & Deployment (evolution, deployment unit, future-auth seam) | [Roadmap](../../03-business-governance/06-product-roadmap.md) | [10](../10-architecture-readiness-review.md) | ADR-001 | [AP-13](../../06-architecture-governance/01-architecture-principles.md) | ADR-007, future evolution |

---

## 10. Governance

- **Ownership:** The **Lead Software Architect** owns ADR-000 (reviewer: Engineering Lead; Business
  Analyst for business-term boundaries).
- **Adding a term:** Propose via an update to ADR-000 (new §3 entry with all fields, §6 synonym note,
  §9 traceability). A term is not "official" until merged here. **No architecture document may
  introduce new terminology without first updating ADR-000.**
- **Modifying a definition:** Treated as a **MAJOR** documentation change ([standards §7](../../_meta/01-documentation-standards.md));
  requires review, a Revision History entry, and a downstream check of documents using the term
  (via [Dependency Graph](../../_meta/03-dependency-graph.md) / [Traceability](../../06-architecture-governance/06-architecture-traceability-matrix.md)).
- **Deprecating a term:** Move it to §6 (forbidden/deprecated) with the preferred replacement and the
  reason; retain for history; update inbound usages in the same change.
- **How ADRs reference this document:** Every ADR and architecture/design/implementation document
  should link ADR-000 as a normative reference and use its terms verbatim. Reviews validate this
  ([Review Checklist A3](../../06-architecture-governance/05-architecture-review-checklist.md)).
- **Relationship to the Business Glossary:** For **product/gameplay** terms, the
  [Business Glossary](../../03-business-governance/01-business-glossary.md) is authoritative and ADR-000
  defers to it; for **architectural** terms, ADR-000 is authoritative. Neither redefines the other.

---

## 11. Final Review

A self-review against the quality requirements:

| Check | Result |
|-------|--------|
| Duplicate concepts | Reconciled — e.g., "engine/logic layer" → **Rules Core**; "controller/manager" → **Authority**; state layers explicitly nested (Room ⊃ Game ⊃ Board). |
| Conflicting terminology | Resolved — Session vs Presence, Validation vs Adjudication, Domain vs System Event, Consistency vs Synchronization, Ownership vs Access all disambiguated (§8). |
| Ambiguous wording | "Serialization" pinned to *intent ordering* (encoding meaning forbidden); "snapshot" split into delivery (role-filtered) vs recovery (full). |
| Missing definitions | All mandatory terms covered (§4); business terms deferred to the glossary, not omitted. |
| Overlap with Business Glossary | Avoided — product terms deferred; only architectural senses added. |
| Contradiction with ADR-001 / Discovery / Governance | None — definitions restate, never redefine, those sources. |

**Recommended improvements (advisory, non-blocking):**
1. When ADR-002 (authoritative game state) lands, cross-link its precise state model back into the
   Room/Game/Board State entries.
2. As each future ADR decides a mechanism (coordination, delivery, recovery), add an *Implementation
   Notes* line to the relevant term pointing at that ADR (keeping definitions themselves neutral).
3. Consider a short one-page "cheat sheet" extract for code reviews (derived from §6–§8), maintained
   as a view of this ADR, not a fork.

**Conclusion:** ADR-000 is complete, internally consistent, technology-neutral, and ready to serve as
the **mandatory terminology reference** for every future ADR, architecture, design, implementation,
and review artifact.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial canonical architecture vocabulary; reconciles terms from Discovery and ADR-001; no new decisions. |
