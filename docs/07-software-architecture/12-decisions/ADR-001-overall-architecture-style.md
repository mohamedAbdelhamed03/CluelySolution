# ADR-001 — Overall Architecture Style

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Governs** | The macro architectural style of Cluely; every subsequent ADR and design document must fit within it. |
| **Made under** | [Architecture Governance](../../06-architecture-governance/README.md) — [Principles](../../06-architecture-governance/01-architecture-principles.md), [Anti-Principles](../../06-architecture-governance/02-architecture-anti-principles.md), [Heuristics](../../06-architecture-governance/03-architecture-decision-heuristics.md). |
| **Scope note** | Sets the **style** only. It intentionally defers the *mechanisms* to their own ADRs: per-room coordination, real-time communication, state recovery, role-based visibility, room isolation, dictionary architecture, session/reconnection, command/query. No technology, framework, C4 diagram, API, schema, or code is chosen here. |

---

## 1. Decision Summary

Cluely will be built as a **server-authoritative, room-isolated, stateful application organized
around a single-writer "room entity" per active room**, deployed initially as a **modular
monolith** with a strict internal separation between (a) a **language-neutral rules core**, (b) a
**room/session lifecycle layer**, and (c) a **role-filtering delivery boundary**.

Each live room is owned by exactly one logical authority ("room entity") that **processes that
room's intents one at a time** and holds its authoritative state for the room's lifetime. Rooms
share **no mutable state**. The rules core is pure and infrastructure-free; the delivery boundary is
the only place role-based visibility is enforced; state custody supports atomic commit and
room-lifetime recovery.

The style is deliberately chosen to be **the simplest style that guarantees fairness, determinism,
correctness, and recoverability for the MVP**, while being **evolvable** — the same room-entity and
rules-core boundaries allow later **distribution of room entities across nodes** and **selective
extraction of services** (e.g., dictionary, delivery) **without changing the rules core or the
business**. This preserves [AP-12 Simple MVP First](../../06-architecture-governance/01-architecture-principles.md)
and [AP-13 Design for Evolution](../../06-architecture-governance/01-architecture-principles.md)
simultaneously.

> This ADR chooses a **style and its invariants**, not a deployment topology or technology. "Modular
> monolith" and "stateful room entity" are style properties (single deployable unit; per-room
> single-writer stateful ownership), not product/vendor choices.

---

## 2. Context

### 2.1 The architectural problem
Cluely is a real-time, hidden-information, room-isolated multiplayer game
([Discovery Overview](../01-architecture-overview.md)). The macro style must make it *structurally
easy* to guarantee the hard properties and *structurally hard* to violate them:

- **Determinism & correctness** under concurrent actions (simultaneous guesses, joins, host changes).
- **Hidden-information integrity** — unrevealed ownership must never reach a non-Spymaster on any path.
- **Atomic, valid state** — multi-field mutations all-or-nothing.
- **Recoverability** to a consistent point within a room's lifetime.
- **Room isolation** — the unit of state and scale is the room.
- **Language-neutral rules** — one gameplay worldwide; only the dictionary is localized.

The style is the *first* architectural decision because the placement of state ownership,
concurrency, and the rules/transport boundary determines whether all later decisions can satisfy the
frozen constraints.

### 2.2 Why this decision is required now
Every subsequent ADR (coordination, real-time, recovery, visibility, isolation, sessions,
command/query) and every design document assumes a macro style. Choosing coordination or recovery
before the style would risk incoherence. [Discovery Readiness](../10-architecture-readiness-review.md#2-unresolved-questions-for-the-architecture-design-work)
lists these as open questions that all sit *inside* a style.

### 2.3 What happens if the decision is not made
Without a fixed style, teams would make incompatible local assumptions (e.g., some responsibilities
adjudicating in the delivery layer, some rooms sharing state), producing the exact anti-principles
the governance forbids ([AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md),
[AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md)) and making fairness and
determinism unprovable.

### 2.4 Documents that require this decision
- [Architecture Discovery 01–11](../README.md) — especially [Responsibilities](../03-system-responsibilities.md),
  [Boundaries](../04-responsibility-boundaries.md), [State Ownership](../05-state-ownership.md),
  [Consistency Boundaries](../06-consistency-boundaries.md), [Interactions](../08-interaction-discovery.md),
  [Quality Scenarios](../09-quality-attribute-scenarios.md), [Handoff](../11-analysis-to-architecture-handoff.md).
- [Architecture Input Report](../../05-architecture-input/01-architecture-input-report.md) (drivers, fixed vs open).
- [Engineering Challenges](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) + [Enrichment](../../04-engineering-analysis/02-engineering-challenges-enrichment.md).

---

## 3. Forces

| Force | Impact on the style |
|-------|---------------------|
| **Gameplay fairness (hidden info + determinism)** | The dominant force. Favors a single authority per room and a single role-filtering boundary; disqualifies any client-authoritative style. |
| **Correctness** | Favors co-locating the rules core with the state it mutates so mutations are atomic; disfavors spreading rule decisions across services. |
| **Determinism / concurrency** | Strongly favors a **single-writer per room** (serialized processing) so simultaneous actions resolve identically. |
| **Consistency (in-room, strong)** | Favors state ownership co-located with the writer; disfavors styles that push per-action consistency into a shared external store. |
| **Recoverability** | Favors a clear custody boundary and commit-then-broadcast; achievable in several styles but simplest when state ownership is explicit. |
| **Scalability (by room count)** | Favors room isolation (no cross-room mutable state) so scaling = more rooms/nodes; distribution should be *possible* but not *required* for MVP. |
| **Maintainability (one gameplay worldwide)** | Favors a pure, language-neutral rules core reused across clients; disfavors rules leaking into transport/services. |
| **Security / integrity (no accounts)** | Favors server authority and a single authorization+visibility boundary. |
| **Performance / latency (real-time feel)** | Favors low-overhead in-process handling per room over cross-service/network hops per action. |
| **Operational complexity** | Favors the fewest moving parts for MVP (monolith) over many services. |
| **Future extensibility** | Favors boundaries that permit later distribution and service extraction without touching the rules core. |
| **Testability** | Favors a rules core exercisable without transport, and per-room determinism that is reproducible. |

---

## 4. Constraints (mandatory, from prior phases)

| Constraint | Source |
|------------|--------|
| **Server is the single source of truth**; clients submit intents only. | [ADR-08 (business)](../../03-business-governance/02-architecture-decision-records.md), [AP-03](../../06-architecture-governance/01-architecture-principles.md) |
| **Business rules, invariants, and precedence are immutable inputs.** | [Business Rules](../../02-business-analysis/02-business-rules.md), [Invariants](../../02-business-analysis/10-business-invariants.md), [Rule Precedence](../../02-business-analysis/16-rule-precedence.md), [AP-02](../../06-architecture-governance/01-architecture-principles.md) |
| **Single writer / one authoritative state per room; atomic, valid transitions.** | [State Ownership §cross-cutting](../05-state-ownership.md), [CB-01/02/03](../06-consistency-boundaries.md), [AP-07/08/09](../../06-architecture-governance/01-architecture-principles.md) |
| **Rooms must not share mutable state.** | [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md), [Boundaries §4](../04-responsibility-boundaries.md) |
| **Rules must not live in transport/delivery; dictionary must not influence rules.** | [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md), [INV-D1](../../02-business-analysis/10-business-invariants.md) |
| **Unrevealed ownership never reaches a non-Spymaster (any path).** | [INV-B9](../../02-business-analysis/10-business-invariants.md), [QS-01](../09-quality-attribute-scenarios.md) |
| **Deterministic outcomes / precedence.** | [Rule Precedence](../../02-business-analysis/16-rule-precedence.md), [AP-06](../../06-architecture-governance/01-architecture-principles.md) |
| **Room-lifetime recoverability (not long-term durability).** | [ENG-RE-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), [Data Lifecycle](../../03-business-governance/05-data-lifecycle-retention.md) |
| **Language-neutral rules core; localization is data-only.** | [AP-14](../../06-architecture-governance/01-architecture-principles.md), [INV-D1](../../02-business-analysis/10-business-invariants.md) |
| **Additive future-auth seam; no auth in MVP.** | [AP-13](../../06-architecture-governance/01-architecture-principles.md), [SRS §2.14](../../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations) |
| **Simplest option meeting the top quality attributes; no speculative generality.** | [AP-12](../../06-architecture-governance/01-architecture-principles.md), [AAP-05/12](../../06-architecture-governance/02-architecture-anti-principles.md) |

---

## 5. Candidate Architectures

Five viable styles were considered. None is dismissed without justification.

### A1 — Client-authoritative / peer-to-peer
- **Overview:** Clients hold and adjudicate game state; peers or a thin relay coordinate.
- **Advantages:** Minimal server cost; low central latency.
- **Disadvantages:** Cannot protect hidden information; cannot prevent cheating; no single truth.
- **Operational complexity:** Low server-side, but unmanageable trust/moderation.
- **Performance:** Good locally.
- **Scalability:** Superficially high (offloaded), but irrelevant given disqualification.
- **Correctness:** Fails — no authority.
- **Failure modes:** Divergent state; trivial key leakage/cheating.
- **Maintainability:** Poor (rules duplicated on clients).
- **Testability:** Poor (non-deterministic across peers).
- **Recovery:** No authoritative point to recover to.
- **Future extensibility:** Blocks auth/fairness features.
- **Verdict:** **Disqualified** — violates [AP-03](../../06-architecture-governance/01-architecture-principles.md),
  [AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md), [INV-B9](../../02-business-analysis/10-business-invariants.md).
  Included only for completeness.

### A2 — Stateless service tier + shared external state store
- **Overview:** A horizontally-scaled stateless tier handles each intent by loading room state from a
  shared store, applying the rule, and writing back; real-time delivery reads the store/stream.
- **Advantages:** Familiar stateless scaling; instances are interchangeable; recovery via the store.
- **Disadvantages:** **Per-room serialization and strong per-action consistency must be delegated to
  the store** (locks/transactions), turning the hottest, most latency-sensitive path (guess
  resolution, [CB-01](../06-consistency-boundaries.md)) into a distributed-consistency problem on
  every action; higher per-action latency (load/lock/write); risk of contention and subtle
  race/ordering bugs; the rules core is pure but the *coordination* is externalized.
- **Operational complexity:** Medium–high (a strongly-consistent store becomes critical infra).
- **Performance:** Lower for real-time — network + store round-trip per action.
- **Scalability:** High for stateless tier; **the store becomes the scaling bottleneck** for hot rooms.
- **Correctness:** Achievable but **depends entirely** on correct external locking/transactions.
- **Failure modes:** Lock contention, partial writes if not transactional, store outages stall all rooms.
- **Maintainability:** Medium (coordination logic spread between tier and store).
- **Testability:** Determinism is harder — depends on store semantics; needs infra to test.
- **Recovery:** Good (state externalized) — a genuine strength.
- **Future extensibility:** Good horizontally; but couples correctness to store guarantees.
- **Verdict:** Viable but **pushes the crux (per-room determinism/atomicity) into infrastructure** and
  raises real-time latency — heavier than the MVP needs ([AP-12](../../06-architecture-governance/01-architecture-principles.md)).

### A3 — Modular monolith with server-authoritative, in-process per-room single-writer entities (RECOMMENDED)
- **Overview:** One deployable service. Each live room is a **single-writer "room entity"** that owns
  its authoritative state (for the room's lifetime) and processes that room's intents **one at a
  time**. Internally, strict modules separate the **pure rules core**, the **room/session lifecycle**,
  the **role-filtering delivery boundary**, **state custody/recovery**, and **dictionary provision**.
  Rooms share no mutable state (room-affinity).
- **Advantages:** Per-room **determinism and atomicity are structural** (single writer → serialized
  processing → [CB-01/02/03](../06-consistency-boundaries.md) hold by construction, not by external
  locks); lowest per-action latency (in-process); fewest moving parts for MVP; the pure rules core is
  trivially testable without transport; the delivery boundary is the single, auditable place for
  role filtering ([QS-01](../09-quality-attribute-scenarios.md)); room isolation is natural.
- **Disadvantages:** A stateful, room-affinity service needs **room→owner routing** and, for
  multi-node, sticky ownership (a coordination concern deferred to ADR-003/007); recovery of in-process
  state needs an explicit strategy (deferred to ADR-005); a single deployable can be a scaling unit if
  not designed for horizontal replication of rooms.
- **Operational complexity:** Low for MVP (one service); moderate later when distributing room entities.
- **Performance:** Best for real-time (no per-action network/store hop).
- **Scalability:** By room count; scales by replicating stateless-of-each-other room entities across
  nodes once routing is added — **isolation makes this natural** (SCAL-1).
- **Correctness:** Highest — single writer + co-located rules core + atomic commit.
- **Failure modes:** Node loss affects the rooms it owns → mitigated by the recovery ADR and
  room-lifetime scope; routing errors → mitigated by ownership discipline.
- **Maintainability:** High — one codebase, clear internal boundaries, pure rules core (one gameplay).
- **Testability:** Highest — deterministic per-room core testable in isolation ([AP-20](../../06-architecture-governance/01-architecture-principles.md)).
- **Recovery:** Requires an explicit strategy (ADR-005), scoped to room lifetime — acceptable per constraints.
- **Future extensibility:** High — the **same** room-entity and rules-core boundaries permit later
  distribution (A5) and selective service extraction (A4) **without changing the rules core**.
- **Verdict:** **Recommended** — best satisfies the dominant forces (fairness, determinism,
  correctness) at the lowest MVP complexity, while preserving evolution.

### A4 — Microservices decomposed by responsibility
- **Overview:** Separate services for rooms, rules engine, delivery, dictionary, session — communicating
  over the network.
- **Advantages:** Independent scaling/deployment per responsibility; strong team autonomy at scale.
- **Disadvantages:** The **rules engine and the state it mutates are split across a network**, making
  per-action atomicity and determinism a **distributed-transaction/ordering problem** on the hot path;
  many failure modes; high operational cost; strong temptation to leak rules into delivery
  ([AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md)); **speculative for an
  MVP** ([AAP-12](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Operational complexity:** High (service mesh, cross-service consistency, observability overhead).
- **Performance:** Lower on the critical path (network hops per action).
- **Scalability:** High per-service, but the *coupled* correctness path gains little from splitting.
- **Correctness:** Harder — distributed consistency for guess resolution.
- **Failure modes:** Partial failures, cross-service races, cascading outages.
- **Maintainability:** Medium — clear service seams but distributed reasoning is costly.
- **Testability:** Harder — requires multi-service orchestration to test outcomes.
- **Recovery:** Complex (per-service + cross-service consistency).
- **Future extensibility:** High, but at premature cost now.
- **Verdict:** Viable at large scale, **over-engineered for the MVP**; A3 can *evolve into* selective
  microservices later where it genuinely helps (e.g., dictionary, delivery).

### A5 — Distributed stateful actor/entity-per-room platform (from day one)
- **Overview:** Room entities as first-class distributed actors spread across a cluster with built-in
  placement, supervision, and rebalancing.
- **Advantages:** Single-writer determinism **and** horizontal distribution/resilience out of the box;
  strong isolation; good recovery if the platform provides persistence/supervision.
- **Disadvantages:** Significant **upfront platform complexity** (placement, rebalancing, cluster
  membership) not needed at MVP scale ([AAP-05/12](../../06-architecture-governance/02-architecture-anti-principles.md));
  steeper operational and cognitive cost.
- **Operational complexity:** High from the start (clustered stateful platform).
- **Performance:** Excellent per room; cross-node routing adds some overhead.
- **Scalability:** Very high — the natural end-state for this domain.
- **Correctness:** High — same single-writer guarantee as A3.
- **Failure modes:** Split-brain/placement issues if the platform is misused.
- **Maintainability:** Medium — powerful but complex.
- **Testability:** High for the entity model; cluster behavior needs more.
- **Recovery:** Strong if the platform supports it.
- **Future extensibility:** Excellent — this is likely the **destination** as scale grows.
- **Verdict:** Excellent long-term target; **premature now**. A3 adopts A5's *concurrency shape*
  (single-writer room entity) without A5's *distribution machinery*, so migrating A3 → A5 later is a
  contained change that does not touch the rules core.

---

## 6. Comparison Matrix

Scores: **5 = strongest, 1 = weakest** for that criterion (for *Complexity* and *Operational Cost*,
5 = *lowest* complexity/cost = best). Reasoning follows each row group.

| Criterion | A1 P2P | A2 Stateless+Store | **A3 Monolith + room entities** | A4 Microservices | A5 Distributed actors |
|-----------|:------:|:------------------:|:------------------------------:|:----------------:|:---------------------:|
| Correctness | 1 | 3 | **5** | 3 | 5 |
| Determinism | 1 | 3 | **5** | 3 | 5 |
| Consistency (in-room) | 1 | 3 | **5** | 3 | 5 |
| Recoverability | 1 | 4 | **4** | 3 | 5 |
| Scalability | 3 | 4 | **4** | 5 | 5 |
| Maintainability | 1 | 3 | **5** | 3 | 4 |
| Complexity (5=lowest) | 3 | 3 | **5** | 2 | 2 |
| Security/Integrity | 1 | 4 | **5** | 4 | 5 |
| Observability | 2 | 3 | **4** | 3 | 4 |
| Operational Cost (5=lowest) | 3 | 3 | **5** | 2 | 2 |
| Developer Experience | 2 | 3 | **5** | 3 | 3 |
| Future Evolution | 1 | 4 | **4** | 5 | 5 |
| **Weighted fit for Cluely MVP** | ✗ | Medium | **Highest** | Low (premature) | Medium (premature) |

**Reasoning for the scores:**
- **Correctness / Determinism / Consistency:** A3 and A5 score 5 because a **single writer per room**
  makes serialized, atomic, precedence-ordered resolution *structural*. A2/A4 score 3 because they
  achieve it only by delegating to external locking/transactions or distributed coordination on the hot
  path. A1 scores 1 (no authority).
- **Recoverability:** A2/A5 score highest (state externalized/platform-managed); A3 scores 4 because it
  needs an explicit recovery strategy (ADR-005) but only to room-lifetime scope; A4 lower (cross-service
  recovery).
- **Scalability:** A4/A5 score 5 (built for it); A3/A2 score 4 (A3 scales by room count once routing is
  added — isolation makes it natural); A1 irrelevant.
- **Complexity / Operational Cost / DX:** A3 scores best — one deployable, one codebase, pure core. A4/A5
  score low (mesh/cluster machinery) — premature for MVP. A2 medium (a critical strongly-consistent store).
- **Security:** A3/A5 score 5 (single authority + single visibility boundary); A2/A4 score 4 (authority
  intact but more surfaces); A1 fails.
- **Future evolution:** A4/A5 score 5; A3 scores 4 because it is explicitly designed to evolve toward
  both without rules-core changes.

---

## 7. Trade-off Analysis

**What is gained by A3:**
- Fairness/determinism/correctness become **structural guarantees**, not infrastructure obligations —
  the single-writer room entity makes [CB-01/02/03](../06-consistency-boundaries.md) hold by
  construction and the pure rules core makes outcomes reproducible and testable.
- **Lowest MVP complexity and best real-time latency** (no per-action network/store hop).
- A **single, auditable role-filtering boundary** — the strongest posture for the existential
  no-leak requirement ([QS-01](../09-quality-attribute-scenarios.md)).

**What is sacrificed / deferred:**
- **Out-of-the-box horizontal distribution** (A5) and **independent per-responsibility scaling** (A4)
  are not present initially; A3 must add **room→owner routing** and a **recovery strategy** to scale
  and survive node loss. These are deferred to ADR-003 (coordination), ADR-005 (recovery), ADR-007
  (room isolation) — a deliberate, governed deferral, not an omission.
- A stateful, room-affinity service is **less "interchangeable"** than a stateless tier (A2); this is an
  accepted cost given the determinism/latency benefits.

**Long-term consequences:**
- The room-entity + pure-rules-core boundaries are the **stable seams** for future growth: distributing
  room entities (→ A5) or extracting services like dictionary/delivery (→ A4) becomes a contained change
  that **does not touch the rules core or the business** ([AP-13](../../06-architecture-governance/01-architecture-principles.md)).

**Operational consequences:**
- MVP operations are simple (one service). The main operational responsibilities introduced are
  **room ownership/routing** and **recovery**, addressed by later ADRs.

**Performance consequences:**
- Best-in-class for the real-time critical path now; distribution later adds bounded cross-node routing
  overhead only when scale requires it.

**Future migration cost:**
- Low-to-moderate and **localized**: because coordination is already single-writer-per-room and the
  rules core is pure, migrating to a distributed actor platform (A5) or carving out services (A4) is an
  infrastructure/hosting change around unchanged cores — the opposite of a rewrite.

---

## 8. Final Decision

**Adopt A3 — a server-authoritative modular monolith organized around single-writer per-room
entities, with a pure language-neutral rules core, a single role-filtering delivery boundary, and an
explicit state-custody/recovery seam; designed to evolve toward distributed room entities (A5) and
selective service extraction (A4) without changing the rules core.**

**Why chosen:** It is the **only** candidate that makes the dominant forces — fairness, determinism,
correctness, in-room consistency — *structural* while also being the **simplest and lowest-latency**
option for the MVP ([AP-04](../../06-architecture-governance/01-architecture-principles.md) correctness
> performance, [AP-05](../../06-architecture-governance/01-architecture-principles.md) fairness >
optimization, [AP-12](../../06-architecture-governance/01-architecture-principles.md) simple first). A
single writer per room removes the hardest class of concurrency bugs *by construction* rather than
relying on external coordination; a pure rules core maximizes testability and preserves one gameplay
worldwide; a single delivery boundary gives the strongest no-leak posture.

**Why the others were rejected:**
- **A1 (P2P):** disqualified — violates server authority and hidden-information integrity.
- **A2 (stateless + store):** viable but relocates the crux (per-room determinism/atomicity) into
  infrastructure and raises real-time latency; heavier than the MVP requires.
- **A4 (microservices):** splits the coupled correctness path across a network — premature, higher
  cost, and tempts rule leakage; A3 can evolve into targeted services later where justified.
- **A5 (distributed actors):** the likely long-term destination, but its cluster machinery is
  speculative complexity now; A3 already adopts its single-writer concurrency shape, making later
  migration contained.

**Why it best satisfies Cluely:** Cluely's value is fair, correct, responsive play in isolated rooms;
A3 aligns the architecture's *structure* with exactly those guarantees at minimum cost, and keeps the
door open for scale.

---

## 9. Consequences

**Positive:**
- Per-room determinism, atomicity, and precedence hold **by construction** (single writer).
- Lowest MVP operational footprint and best real-time latency.
- One auditable role-filtering boundary → strongest no-leak posture.
- Pure, testable, language-neutral rules core → one gameplay worldwide, high testability.
- Clear seams for future distribution/extraction without business change.

**Negative / accepted limitations:**
- The service is **stateful and room-affine** → requires room→owner routing and cannot treat instances
  as fully interchangeable (accepted; addressed by ADR-003/007).
- **Recovery of in-process room state** needs an explicit strategy (accepted; ADR-005), scoped to
  room lifetime only.
- Not horizontally distributed on day one → a single node's capacity bounds concurrent rooms until
  distribution is added (accepted for MVP scale; [ENG-SC-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)).

**New responsibilities introduced:**
- **Room ownership/routing** (which authority owns a given room).
- **State custody + recovery** for room entities.
- Discipline that the **delivery boundary and dictionary never adjudicate** and rooms never share
  mutable state (enforced in review).

**Future work enabled:**
- Distribute room entities across nodes (→ A5) and extract services (dictionary, delivery → A4) without
  touching the rules core; attach authentication at the identity seam additively.

**Future work made harder:**
- A wholesale move to a **stateless** model (A2) would be a larger change, since A3 intentionally keeps
  authoritative room state in-process for determinism/latency. This is an accepted, deliberate bias.

---

## 10. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| In-process room state lost on node/process failure | Recovery | Explicit recovery strategy (ADR-005) scoped to room lifetime; commit-then-broadcast; idempotent recovery ([ENG-RE-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)). |
| Single-node capacity bounds concurrent rooms | Scalability | Room isolation makes horizontal replication of room entities natural; add routing/distribution when load warrants (ADR-007), not before ([AP-12](../../06-architecture-governance/01-architecture-principles.md)). |
| Rules logic drifting into the delivery/lifecycle layers | Maintainability / Correctness | Enforce the pure-core boundary in [Review Checklist F4/A1](../../06-architecture-governance/05-architecture-review-checklist.md); [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md). |
| Accidental cross-room shared mutable state | Correctness / Scalability | Room isolation is a hard rule ([AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md)); reviewed. |
| Role-filtering bypass in a delivery path | Security (existential) | Single delivery boundary; negative testing across all paths ([QS-01](../09-quality-attribute-scenarios.md), [ASM-05](../../06-architecture-governance/04-architecture-success-metrics.md)). |
| Monolith becoming a "big ball of mud" | Maintainability | Strict internal module boundaries; each is independently testable; boundaries reviewed against [Boundaries §4](../04-responsibility-boundaries.md). |
| Recovery/routing added late, forcing rework | Operational | These are explicitly scheduled as ADR-003/005/007 *before* scale is needed; the style is chosen to make them additive. |
| Testing per-room determinism under load | Testing | Property-based + concurrency tests on the pure core; deterministic replay (see §11). |

---

## 11. Validation

This decision is verifiable by:
- **Architecture review** against [Review Checklist](../../06-architecture-governance/05-architecture-review-checklist.md)
  and [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md) (esp. ASM-05/06/07/12).
- **Concurrency testing / property-based testing** of the rules core: many interleavings of simultaneous
  intents must yield the single deterministic outcome required by [Rule Precedence](../../02-business-analysis/16-rule-precedence.md) ([QS-02](../09-quality-attribute-scenarios.md)).
- **Simulation** of full matches (including [QS-03](../09-quality-attribute-scenarios.md) terminal cases)
  driven directly against the transport-free core.
- **Recovery testing / chaos testing:** kill the owner mid-operation and confirm recovery to the last
  consistent state with no replayed terminal effects ([QS-06](../09-quality-attribute-scenarios.md)).
- **Security testing:** exhaustive negative testing that **no** delivery path exposes unrevealed
  ownership to a non-Spymaster ([QS-01](../09-quality-attribute-scenarios.md)).
- **Load testing:** confirm bounded per-room footprint and that scaling by room count holds
  ([QS-10](../09-quality-attribute-scenarios.md)); measure the real-time propagation target ([QS-08](../09-quality-attribute-scenarios.md)).
- **Migration probe (future):** demonstrate a room entity can be relocated/distributed without changing
  the rules core (validates the evolution claim).

---

## 12. Traceability

| Dimension | References |
|-----------|-----------|
| **Architectural Drivers** | [02 Drivers](../02-architectural-drivers.md): fairness, correctness, determinism, real-time, recoverability, security, isolation, maintainability, extensibility. |
| **Business Rules** | [Business Rules](../../02-business-analysis/02-business-rules.md) (server-adjudicated), [BR-CO-4/BR-GV/BR-CL](../../02-business-analysis/02-business-rules.md). |
| **Invariants** | [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-G2/G3](../../02-business-analysis/10-business-invariants.md) (one turn/clue), [INV-R1](../../02-business-analysis/10-business-invariants.md) (one host), [INV-D1](../../02-business-analysis/10-business-invariants.md) (dictionary=words only). |
| **Rule Precedence** | [16 Rule Precedence](../../02-business-analysis/16-rule-precedence.md) (deterministic resolution the single writer enforces). |
| **Engineering Challenges** | [ENG-GP-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) (simultaneous guesses), ENG-ST-02 (atomicity), ENG-FP-01 (leak), ENG-RE-01 (recovery), ENG-SC-01 (footprint), ENG-CO-* (concurrency). |
| **Quality Scenarios** | [QS-01/02/03/06/08/10](../09-quality-attribute-scenarios.md). |
| **Responsibility Boundaries** | [04 Boundaries](../04-responsibility-boundaries.md): C1 Rules & Play cluster; isolation rules (rules≠transport, dictionary≠rules, room≠room). |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md): CB-01/02/03/09 (strong, per-room). |
| **State Ownership** | [05 State Ownership](../05-state-ownership.md): single writer; S-02/03/04 owned by Rules & Play. |
| **Interaction Discovery** | [08 Interactions](../08-interaction-discovery.md): I-01 (guess), I-08 (commit-then-broadcast), I-09 (recovery). |
| **Architecture Governance** | Principles [AP-02/03/04/05/06/07/08/12/13/14/18/20](../../06-architecture-governance/01-architecture-principles.md); Anti-principles [AAP-02/05/08/09/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Heuristics](../../06-architecture-governance/03-architecture-decision-heuristics.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 13. Impact on Future Architecture

| Area | Impact |
|------|--------|
| **C4 diagrams (future)** | The Container level is a single service (MVP) containing components: rules core, room lifecycle, delivery boundary, custody/recovery, dictionary provider. Component boundaries follow the [cohesion clusters](../04-responsibility-boundaries.md). |
| **Bounded contexts** | Natural contexts: **Play** (rules/board/turn/clue/guess), **Room & Lobby**, **Connectivity & Identity**, **Content/Dictionary**, **Delivery**. The Play context is the single-writer heart. |
| **Aggregates** | The **Room/Match** is the primary consistency aggregate owned by the room entity; the board+key and turn are within it. This ADR fixes that the aggregate is mutated by a single writer. |
| **Application services** | Thin intent-handlers that validate/authorize then hand to the room entity; they never adjudicate. |
| **Domain services** | The rules core is a pure domain service, transport/storage/language-free. |
| **Persistence** | Deferred to ADR-005; must be room-lifetime recovery, commit-then-broadcast, idempotent — **not** an external per-action consistency dependency. |
| **Real-time delivery** | Deferred to ADR-004; must sit at the single role-filtering boundary and never adjudicate; commit-then-broadcast; ordering/resync. |
| **Security** | Single authorization + visibility boundary; no client trust; identity seam additive. |
| **Observability** | Emitted at the room-entity/commit boundary (PII-free); one place to instrument outcomes/events. |
| **Deployment** | MVP: one deployable, room-affinity. Evolution: room→owner routing enables multi-node; later distribution (A5) / service extraction (A4). |
| **Future ADRs** | This ADR frames and constrains **ADR-002** (authoritative game state — inside the room entity), **ADR-003** (per-room coordination — realizes single-writer), **ADR-004** (real-time), **ADR-005** (recovery), **ADR-006** (role-based visibility — the delivery boundary), **ADR-007** (room isolation/routing/distribution), **ADR-008** (dictionary), **ADR-009** (session/reconnection), **ADR-010** (command/query). Each must fit within A3. |

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: A3 — server-authoritative modular monolith with single-writer per-room entities, evolvable to distributed entities/services without changing the rules core. |
