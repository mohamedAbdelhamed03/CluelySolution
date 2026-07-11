# 07.01 — Architecture Overview (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery (bridges Analysis → Software Architecture) |
| **Purpose** | Introduce the Software Architecture phase: what kind of system Cluely is, why it is architecturally challenging, its major responsibilities and qualities, and why architecture matters here. Synthesis only — it does not design or choose technology. |
| **Scope** | Architectural discovery/decomposition. No diagrams (C4), technologies, patterns (except as documented alternatives elsewhere), APIs, schemas, or code. |
| **Inputs** | Approved [Architecture Input Report](../05-architecture-input/01-architecture-input-report.md); business analysis; engineering analysis; architecture governance. |
| **Outputs** | A shared understanding that frames the rest of this phase (documents 02–11). |
| **Dependencies** | All approved analysis (00–06 phases). |
| **Cross References** | [Architectural Drivers](02-architectural-drivers.md), [System Responsibilities](03-system-responsibilities.md). |
| **Related Business Documents** | [BRD](../01-product-discovery/01-business-requirements.md), [Business Rules](../02-business-analysis/02-business-rules.md), [Invariants](../02-business-analysis/10-business-invariants.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md). |
| **Related Engineering Documents** | [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), [Enrichment](../04-engineering-analysis/02-engineering-challenges-enrichment.md). |

---

## 1. What kind of system Cluely is

Cluely is a **real-time, server-authoritative, hidden-information, room-isolated multiplayer
system**. Concretely:

- **Real-time:** participants act and observe with near-immediate feedback.
- **Server-authoritative:** one authority owns all game state and adjudicates every rule; clients
  submit intents and render filtered state ([ADR-08](../03-business-governance/02-architecture-decision-records.md)).
- **Hidden-information:** card ownership is secret to Operatives; leaking it destroys the game
  ([INV-B9](../02-business-analysis/10-business-invariants.md#inv-b9--unrevealed-ownership-is-never-disclosed-to-operatives)).
- **Room-isolated:** the unit of state and scale is the room; rooms share no mutable state.
- **No-account / transient identity:** players exist only for a room's lifetime, with an additive
  seam for future authentication.

The *rules* themselves are small and fully specified. The architecture's difficulty lies almost
entirely in **enforcing those rules correctly under concurrency, unreliable networks, and
reconnection, while never leaking hidden information**.

## 2. Why it is architecturally challenging

| Challenge area | Essence | Source |
|----------------|---------|--------|
| **Deterministic concurrency** | Simultaneous actions (guesses, joins, host changes) must resolve identically every time. | [Rule Precedence](../02-business-analysis/16-rule-precedence.md), ENG-GP-01 |
| **Atomic, valid state** | Multi-field mutations (reveal → counts → terminal → turn) must be all-or-nothing; illegal states forbidden. | ENG-ST-01/02 |
| **Hidden-information integrity** | Ownership must never reach a non-Spymaster on any path (initial, delta, reconnect, rematch, telemetry). | ENG-FP-01 |
| **Real-time reliability** | Lost/duplicated/out-of-order messages and slow clients must not desync or double-apply. | ENG-RT-01 |
| **Reconnection continuity** | Brief drops must not end matches; role-appropriate state restored without leaks. | ENG-RT-03 |
| **Recoverability** | Crashes/interruptions must recover to a consistent point within the room lifetime. | ENG-RE-01 |
| **Room lifecycle & scale** | Many small isolated rooms; race-safe expiry/host-migration; bounded footprint. | ENG-CO-05, ENG-SC-* |

These are the classic failure modes of real-time multiplayer platforms, made stricter by the
hidden-information requirement.

## 3. Major architectural responsibilities (preview)

Discovered in detail in [System Responsibilities](03-system-responsibilities.md). At a glance:
**Room Management, Lobby/Setup, Match Lifecycle, Rules Engine (adjudication), Board Generation,
Dictionary Provision, Turn Management, Clue Processing, Guess Processing, Validation &
Authorization, State & Session Store, Connection/Presence Management, Notification/Delivery
(role-filtered), State Recovery, Room Cleanup/Expiry, Observability.**

## 4. Major system qualities

Ordered by architectural precedence (from [Architecture Input §4](../05-architecture-input/01-architecture-input-report.md#4-quality-attribute-priorities)):
**Correctness → Gameplay Fairness → Consistency → Reliability/Recoverability → Security → Performance
→ Availability → Testability → Maintainability → Extensibility → Observability → Scalability →
Operational Simplicity.** When these conflict, higher-ranked wins — this ordering is the tie-breaker
the architecture must encode.

## 5. Why architecture matters for this product

The business is simple to *state* and hard to *guarantee*. The value of the product is entirely in
**fair, correct, responsive** play; a single architectural mistake — a leaked key, a non-deterministic
outcome, a corrupt state after a race — destroys that value regardless of features. Architecture is
therefore about **guaranteeing invariants under adversarial conditions** (concurrency, faults,
modified clients), not about feature richness. This discovery phase exists to make those guarantees
explicit — *what the architecture must be capable of solving* — before any *how* is chosen.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial architecture overview (discovery). |
