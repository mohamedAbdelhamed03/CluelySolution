# 07.04 — Responsibility Boundaries (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Analyze which responsibilities (R-01…R-17) belong together and which must stay isolated: cohesion, coupling, ownership boundaries, and forbidden direct dependencies. Guides — does not decide — future component boundaries. |
| **Scope** | Cohesion/coupling analysis. No components, technologies, or patterns chosen. |
| **Inputs** | [System Responsibilities](03-system-responsibilities.md), drivers, invariants. |
| **Outputs** | Candidate ownership groupings and hard isolation rules for the architecture to honor. |
| **Dependencies** | [Anti-Principles](../06-architecture-governance/02-architecture-anti-principles.md) (esp. AAP-08/09). |
| **Cross References** | [State Ownership](05-state-ownership.md), [Interactions](08-interaction-discovery.md). |
| **Related Business Documents** | [Domain Model](../02-business-analysis/06-domain-model.md), [Invariants](../02-business-analysis/10-business-invariants.md). |
| **Related Engineering Documents** | [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md). |

---

## 1. Cohesion clusters (responsibilities that naturally belong together)

These are **candidate** groupings by strong cohesion; the architecture may realize them differently.

| Cluster | Responsibilities | Why cohesive (shared purpose/state) |
|---------|------------------|-------------------------------------|
| **C1 — Rules & Play** | R-04 Rules Engine, R-07 Turn Mgmt, R-08 Clue, R-09 Guess, R-05 Board Gen | All mutate/decide the one authoritative match state deterministically; splitting them risks non-atomic outcomes and duplicated rule logic. |
| **C2 — Room & Lobby** | R-01 Room Mgmt, R-02 Lobby/Setup, R-16 Cleanup | All own room-level membership/config/lifecycle; share room state. |
| **C3 — Connectivity & Identity** | R-12 Connection/Presence, R-13 Session/Identity | Both concern a player's live participation and continuity. |
| **C4 — Gatekeeping** | R-10 Validation & Authorization | Cross-cuts, but is a single cohesive accountability that must precede all effects. |
| **C5 — Delivery** | R-11 Notification/Delivery | A single boundary that filters and transports; cohesive around visibility. |
| **C6 — Custody & Recovery** | R-14 State Store, R-15 Recovery | Both concern holding and restoring authoritative state. |
| **C7 — Content** | R-06 Dictionary Provision | Read-mostly content, cohesive and independent. |
| **C8 — Observability** | R-17 Observability | Cross-cutting, cohesive around signals. |

## 2. Cohesion strength

| Strength | Examples | Note |
|----------|----------|------|
| **Strong** | Rules Engine ↔ Turn/Clue/Guess (C1); Room ↔ Lobby (C2) | Share the same state and must change it atomically. |
| **Weak** | Dictionary ↔ Board Gen | One-directional at match start only; otherwise independent. |
| **Weak** | Observability ↔ everything | Consumes events; must not influence outcomes. |

## 3. Shared responsibilities (need explicit ownership)

| Concern | Touched by | Boundary rule |
|---------|-----------|---------------|
| **Authoritative state mutation** | Rules & Play (C1) writes; others read | Only C1 (adjudication) may change match/turn state; everyone else reads via custody (C6). |
| **Role-filtered visibility** | Delivery (C5) enforces; Rules (C1) produces | Filtering is a **delivery boundary** guarantee; adjudication produces role-tagged data. |
| **Validation** | Gatekeeping (C4) for all intents | Every intent passes C4 before any effect; no cluster self-authorizes ad hoc. |
| **Presence effects on play** | Connectivity (C3) signals; Rules/Turn (C1) reacts | C3 signals pause/abandonment; C1 decides play effects — C3 never mutates match state. |

## 4. Hard isolation rules (must never directly depend / never co-mingle)

| Rule | Why |
|------|-----|
| **Delivery (C5) must never adjudicate rules** | Rules leaking into transport → duplication, non-portability, untestable (AAP-09). |
| **Dictionary (C7) must never influence rules/outcomes** | Content affects words only ([INV-D1](../02-business-analysis/10-business-invariants.md)); any rule dependency breaks one-gameplay. |
| **Rooms must not share mutable state** | Cross-room coupling breaks isolation/scale (AAP-08); each room's state is independent. |
| **Operative-facing delivery must never carry unrevealed ownership** | Existential fairness ([INV-B9](../02-business-analysis/10-business-invariants.md)); the filtering boundary is absolute. |
| **Observability (C8) must never affect outcomes** | Signals are side-effect-free; must not carry PII/ownership. |
| **Connectivity (C3) must not change match state** | Presence is not adjudication; only signals to C1. |
| **Validation (C4) must not be bypassable** | Any unvalidated path can corrupt state (AAP-04). |

## 5. Ownership vs isolation summary

- **Ownership boundaries (who is accountable):** match/turn state → Rules & Play; room/membership →
  Room & Lobby; connectivity/identity → Connectivity & Identity; visibility → Delivery; custody →
  Store & Recovery; content → Dictionary.
- **Isolation boundaries (must not cross):** rules↔transport, content↔rules, room↔room,
  ownership↔non-Spymaster delivery, observability↔outcomes.

## 6. Coupling watch-list (for the architecture to manage)

- Rules & Play (C1) ↔ State Store (C6): tight by necessity (atomic commit); keep the *contract*
  narrow and the store non-adjudicating.
- Connectivity (C3) ↔ Rules & Play (C1): event-driven only (pause/resume/abandon); avoid direct
  state coupling.
- Delivery (C5) ↔ everything: unavoidable fan-in/out; keep it a pure boundary (filter + transport).

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial responsibility-boundary analysis. |
