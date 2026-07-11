# ADR-009 — Participant Lifecycle, Presence & Session Continuity

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How does a participant exist, appear, disconnect, reconnect, leave, and cease participating while preserving architectural correctness?* |
| **Complies with** | [ADR-000](ADR-000-architecture-vocabulary.md), [ADR-001](ADR-001-overall-architecture-style.md), [ADR-002](ADR-002-authoritative-game-state.md), [ADR-003](ADR-003-per-room-coordination-model.md), [ADR-004](ADR-004-real-time-communication-delivery.md), [ADR-006](ADR-006-role-based-information-visibility.md). Builds upon; redefines none. |
| **Scope note** | Defines the **participant lifecycle / presence / session-continuity architecture** only. It chooses **no** authentication/identity provider, OAuth/JWT/cookies, transport (SignalR/WebSockets), persistence, framework, or deployment ([§18 Non-Goals](#18-non-goals)). It explicitly does **not** define authentication or authorization. |

---

## 1. Executive Summary

A **participant** is a person taking part in **one room**, represented by an **authoritative
participation record** owned by that room's **Authority** ([ADR-001/002](ADR-002-authoritative-game-state.md)).
Participation is **independent of any connection** and **independent of authentication**: a participant
**exists as long as its participation record exists**, which may **outlive** any single connection.

The chosen model is **Participant-Centric Lifecycle with Token-Based Continuity** (a hybrid): the room
holds one authoritative participation record per participant; a **transient connection** is merely the
current pipe to that participant; a **room-scoped, PII-free continuity token** (ADR-000
*Reconnect Token*) lets a returning connection **bind back to the existing participation** within a
**grace/lease window**. **Exactly one active connection** represents a participant at a time (newest
supersedes); **duplicate connections never create duplicate participation or duplicate gameplay
actions**. **Presence** (connected / disconnected-in-grace / departed) is an **authoritative** attribute
of the participation record; it is **consumed** to drive pause/host-migration/abandonment decisions and
**projected** (role-filtered, ADR-006) to others — but **presence never determines game rules**.

This separates three things the rest of the architecture depends on:
- **Participant identity ≠ user identity** — today identity is transient and room-scoped; a future
  account attaches at the **identity seam** without changing this model.
- **Participation ≠ connection** — connections are transient; participation persists across them.
- **Presence ≠ authority** — presence is data the Authority owns; it never *is* the authority and never
  mutates game state by itself.

Continuity (over reconnection) matters because mobile links drop constantly; a brief drop must **never**
end a match or compromise fairness ([ENG-RT-03/ENG-SE-02/03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md),
[QS-07](../09-quality-attribute-scenarios.md)).

> One-line statement: **one authoritative participation record per participant, connection-independent,
> continued via a room-scoped token within grace, with exactly one active connection and authoritative
> (but rules-neutral) presence — all owned by the room Authority.**

---

## 2. Problem Statement

**Why multiplayer participation is architecturally difficult.** A room must track *who is taking part*
distinctly from *who is currently connected*, keep that correct under drops/duplicates/reconnects, and
resume play without corrupting state or leaking secrets — all while remaining account-free today and
account-ready tomorrow.

**Why unreliable connectivity must be expected.** Mobile networks drop, switch, and duplicate; if
participation were tied to the physical connection, every blip would eject a player and could end a
match unfairly ([R-1](../../01-product-discovery/01-business-requirements.md#19-risks)).

**Why identity inside a room differs from user identity.** Cluely has **no accounts** today
([ADR-002](ADR-002-authoritative-game-state.md), [SRS §2.14](../../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations)).
A participant is a **transient, room-scoped** identity (a nickname + a continuity token), not a durable
user. Conflating the two would either force premature authentication or make future auth a rewrite.

**Why the architecture must tolerate interruption without compromising fairness.** Resuming after a drop
must restore the participant to their **exact role/team/view** (ADR-006) without ever exposing hidden
information or letting two devices act as one participant ([INV-B9/INV-P4](../../02-business-analysis/10-business-invariants.md)).

---

## 3. Participation Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Participation is independent of authentication** | A participant exists without any account/login. | No-auth MVP; account-ready seam ([ADR-002](ADR-002-authoritative-game-state.md), AUTH-1..5). |
| **Presence is authoritative** | Connected/disconnected/departed is owned by the Authority as part of state. | Decisions (pause/migration/abandon) must derive from consistent truth ([CB-08](../06-consistency-boundaries.md)). |
| **Connection is transient** | A connection is a disposable pipe, not identity or state. | Connections come and go; participation must not. |
| **Participation may outlive a connection** | The participation record persists across drops within grace. | Continuity over reconnection ([QS-07](../09-quality-attribute-scenarios.md)). |
| **Authority owns participation** | The room Authority is the single owner/writer of participation records. | [ADR-001/003](ADR-003-per-room-coordination-model.md); single writer. |
| **Continuity over reconnection** | The goal is to *continue the same participation*, not to create a new one on return. | Preserves role/team/view and fairness. |
| **One participant, one authoritative participation state** | Each participant has exactly one record; one active connection at a time. | [INV-P4](../../02-business-analysis/10-business-invariants.md); no dual actors. |
| **Graceful interruption** | Disconnects are tolerated for a bounded window before departure. | Resilience without stalling others. |
| **Explicit lifecycle** | Participation states/transitions are modeled, not implied. | [AP-08/09](../../06-architecture-governance/01-architecture-principles.md). |
| **Presence never determines game rules** | Rules depend on committed game state, not on who is connected. | Presence *may pause* play but never *decides outcomes*. |
| **Room isolation** | Participation is room-scoped; no cross-room participation state. | [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md). |
| **Future extensibility** | The model admits accounts, spectators, matchmaking, multi-device additively. | [AP-13](../../06-architecture-governance/01-architecture-principles.md). |

---

## 4. Candidate Lifecycle Models

Evaluated for the account-free, real-time, room-isolated setting. None dismissed without reasoning.

### LM1 — Connection-Centric Participation
- **Overview:** A participant *is* their connection; disconnect = gone.
- **Advantages:** Trivial; no continuity bookkeeping.
- **Disadvantages:** Every network blip ejects the player; cannot resume; unfair match endings;
  incompatible with mobile reality ([ENG-SE-03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)).
- **Correctness/Recoverability:** Poor recovery.
- **Future auth/spectator/matchmaking:** Poor (identity tied to socket).
- **Verdict:** **Rejected** — violates continuity and fairness requirements.

### LM2 — Session-Centric Participation
- **Overview:** A "session" object spans connections; the participant is the session.
- **Advantages:** Continuity across reconnects; a natural home for a continuity token.
- **Disadvantages:** "Session" often drifts toward auth-session semantics (cookies/JWT) — a conflation
  this ADR must avoid; if the session is transport-owned it risks a connection-centric relapse.
- **Verdict:** Viable **if** the session is an **authoritative, Authority-owned** concept (not a
  transport/auth artifact). Folded into LM3 as the "participation record."

### LM3 — Participant-Centric Lifecycle (RECOMMENDED base)
- **Overview:** The **participant** (participation record) is the first-class, Authority-owned entity;
  connections and presence are attributes of it.
- **Advantages:** Cleanest separation (participation ≠ connection ≠ presence); Authority-owned (fits
  ADR-001/003); resilient; account-ready (attach a durable identity to the record later).
- **Disadvantages:** Needs a continuity mechanism to re-bind a returning connection (→ LM4).
- **Correctness/Recoverability/Testing/Evolution:** High across the board.
- **Verdict:** **Selected base.**

### LM4 — Token-Based Continuity (RECOMMENDED mechanism)
- **Overview:** A **room-scoped, PII-free continuity token** issued at join lets a returning connection
  re-bind to the existing participation within grace.
- **Advantages:** Enables resume without accounts; simple, transport-agnostic; supersedes cleanly on a
  new connection.
- **Disadvantages:** Token loss/theft handling (mitigated: room-scoped, grace-bounded, single-active,
  invalidated on departure — [ENG-SE-02](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)).
- **Verdict:** **Selected mechanism** for LM3 continuity.

### LM5 — Lease-Based Presence (RECOMMENDED for presence/timeouts)
- **Overview:** Presence is a **lease** the Authority holds for a participant; a disconnect starts a
  grace **lease** that, if not renewed by reconnection, expires into departure.
- **Advantages:** Clean, uniform model for grace/timeout/abandonment decisions; decisions are made at the
  (coordinated) moment the lease is evaluated — consistent, not timing-fragile.
- **Disadvantages:** Requires deadline bookkeeping (already needed by ADR-002/005).
- **Verdict:** **Selected** as the presence/timeout concept (durations are Non-Goals — [§9](#9-session-continuity)).

### LM6 — Hybrid (FINAL)
- **Overview:** **LM3 (participant-centric)** + **LM4 (token continuity)** + **LM5 (lease-based
  presence)**: an Authority-owned participation record, re-bound by a room-scoped token within a
  grace lease, with exactly one active connection.
- **Verdict:** **This is the decision** — it captures continuity, single-active-connection, authoritative
  presence, and account-readiness with minimal complexity.

### Evaluation summary

| Criterion | LM1 Conn | LM2 Session | **LM3+LM4+LM5 (LM6, chosen)** |
|-----------|:--------:|:-----------:|:-----------------------------:|
| Correctness | 2 | 4 | **5** |
| Recoverability | 1 | 4 | **5** |
| Complexity (5=lowest) | 5 | 3 | **4** |
| Scalability | 3 | 4 | **4** |
| Maintainability | 2 | 3 | **5** |
| Testing | 3 | 3 | **5** |
| Operational (5=lowest) | 4 | 3 | **4** |
| Future authentication compat | 1 | 3 | **5** |
| Future spectator compat | 2 | 3 | **5** |
| Future matchmaking compat | 2 | 3 | **5** |

---

## 5. Final Lifecycle Model

**Adopt LM6 — Participant-Centric Lifecycle with Token-Based Continuity and Lease-Based Presence.** One
authoritative participation record per participant, owned by the room Authority; a transient connection
is the current pipe; a room-scoped, PII-free continuity token re-binds a returning connection to the
existing participation within a grace lease; exactly one active connection at a time (newest supersedes).

- **Why it fits Cluely:** resilient, account-free, fair, and account-ready.
- **Aligns with ADR-001/003:** participation is part of the single-writer aggregate; every lifecycle
  change is a **coordinated Intent** (no bypass).
- **Aligns with ADR-002:** the participation record (identity ref, presence, token validity, team/role)
  is **authoritative state** inside the Room aggregate.
- **Aligns with ADR-004:** connection changes drive subscribe/unsubscribe; reconnection uses
  **snapshot-then-increments** for the restored role; delivery targets the **single active connection**.
- **Aligns with ADR-006:** on reconnect the participant receives a **role-filtered projection** for their
  **restored** role; presence is projected role-safely.
- **Preserves invariants:** one participation/one connection ([INV-P4](../../02-business-analysis/10-business-invariants.md)),
  transient PII-free identity ([INV-P2](../../02-business-analysis/10-business-invariants.md)), frozen
  roles mid-match ([INV-T5](../../02-business-analysis/10-business-invariants.md)), one host ([INV-R1](../../02-business-analysis/10-business-invariants.md)).

---

## 6. Participant Lifecycle

Conceptual states (all transitions are **coordinated Intents/decisions** via ADR-003; technology-neutral).
Extends the business [Connection state machine §8.4](../../02-business-analysis/07-state-machines.md#84-player-connection-state-machine)
into an architectural participation lifecycle.

```
Invitation (future) → Join → Active ⇄ Temporary-Disconnect(grace lease) → Reconnect → Resume(Active)
   Active | Temporary-Disconnect → Leave/Departure → Removed → (Result/History archived, PII-free) → Expired
```

| State | Purpose | Allowed transitions | Forbidden transitions | Recovery expectation | Authority responsibility |
|-------|---------|---------------------|-----------------------|----------------------|--------------------------|
| **Invitation (future)** | Placeholder for a future invite/lobby-approval step. | → Join | — | N/A (future) | None today; seam only. |
| **Join** | Admit a participant; create the participation record; issue continuity token. | → Active | → Active-in-a-running-match as a *player* (late joiners wait — [BR-JR-6](../../02-business-analysis/02-business-rules.md)) | Record + token created atomically. | Validate (capacity/nickname), create record (single writer). |
| **Active** | Participant is present with an active connection; may act per role/turn. | → Temporary-Disconnect; → Leave | Two active connections (forbidden) | Steady state. | Own record; route delivery to the one active connection. |
| **Temporary-Disconnect (grace lease)** | Connection lost; participation retained; grace lease running. | → Reconnect (within grace); → Departure (grace expiry) | Acting via the lost connection; being treated as departed before grace | Re-bind on reconnect; else depart. | Start lease; pause play if essential (ADR-003); keep record. |
| **Reconnect** | A returning connection presents a valid token within grace. | → Resume (Active) | Binding to a *different* participation; creating a *new* record | Restore role/team/view. | Validate token+grace; **supersede** any residual connection; re-bind. |
| **Resume (Active)** | Continuity achieved; role-filtered snapshot delivered (ADR-004/006). | (as Active) | — | Resume paused phase. | Deliver snapshot for restored role; resume. |
| **Leave / Departure** | Explicit leave, host removal, or grace expiry. | → Removed | Remaining an actor after departure | Seat released. | Remove from team/role; invalidate token; host migration if host ([BR-HM](../../02-business-analysis/02-business-rules.md)). |
| **Removed** | No longer a participant; record retired. | → Archived/Expired | Re-acting without a fresh Join | Fresh Join required to return. | Discard transient participation; free nickname. |
| **Archived (result/history, PII-free)** | Only match **result/events** may persist (ADR-002/005), never the participation identity as PII. | → Expired | Reconstructing a live participant from history | Not a live state. | Record result PII-free ([Data Lifecycle](../../03-business-governance/05-data-lifecycle-retention.md)). |
| **Expired** | Room ended; all participation discarded. | (terminal) | Any participation activity | None. | Discard all; release code. |

---

## 7. Presence Model

- **What is Presence?** An **authoritative attribute** of the participation record indicating the
  participant's current connectivity status: **Connected**, **Disconnected (in grace)**, or **Departed**.
- **Who owns Presence?** The room **Authority** (part of the aggregate, [ADR-002 S-Presence](ADR-002-authoritative-game-state.md#6-state-inventory)).
- **How is Presence updated?** Only via **coordinated** transitions (connection established/lost,
  reconnect, grace-lease expiry) processed by the single writer (ADR-003) — never by a connection
  mutating state directly.
- **How is Presence consumed?** To drive **pause / host-migration / abandonment** decisions
  ([CB-08](../06-consistency-boundaries.md), [BR-DC/HM/RX](../../02-business-analysis/02-business-rules.md)) —
  decisions computed against **committed** presence at the coordinated moment.
- **How is Presence projected?** Role-filtered (ADR-006): others see *that* a participant is
  connected/away; **own** token/session details are never broadcast.
- **What is authoritative?** The **status** (Connected/Disconnected/Departed) and the **grace deadline**
  (as a decision input).
- **What is derived?** Display niceties ("reconnecting…"), and any "how long away" indicator — derived,
  and may be **eventually consistent** for display.
- **When is Presence stale?** A *displayed* presence may briefly lag; but every **decision** uses the
  latest **committed** presence — display staleness never affects outcomes.
- **How is Presence recovered?** After interruption/recovery (ADR-005), presence is re-established from
  connections (a disconnected participant is shown until it reconnects); it is **recomputed/re-established**,
  never restored as a stale truth.

---

## 8. Connection Ownership

| Question | Architectural answer |
|----------|----------------------|
| **Can one participant own multiple simultaneous connections?** | **No.** Exactly **one active connection** per participant at a time ([INV-P4](../../02-business-analysis/10-business-invariants.md)). |
| **Can one connection represent multiple participants?** | **No.** A connection maps to at most one participation record in a room. |
| **Can duplicate participation exist?** | **No.** One authoritative participation record per participant; a second "join" of the same continuity token **re-binds** (supersedes), it does not duplicate. |
| **Can another device replace an existing connection?** | **Yes.** A new connection presenting a valid continuity token **supersedes** the prior one (newest wins); the old connection is dropped. |
| **How are conflicts resolved?** | Deterministically by the Authority: the **newest valid** connection becomes the single active one; residual connections are severed; any in-flight Intent from a superseded connection is discarded (ADR-003 ordering). |
| **What architectural guarantees exist?** | Single active connection; no duplicate participation; no duplicate gameplay actions from parallel connections; connection changes never mutate game state (only presence, via coordination). |

---

## 9. Session Continuity

Architectural concepts only (**no durations** — those are operational parameters in the
[Business Constants Catalog](../../03-business-governance/03-business-constants-catalog.md)):

- **Grace period:** the bounded window during which a disconnected participation is retained and can be
  resumed (modeled as a **lease**, LM5).
- **Reconnect window:** synonymous with the grace lease from the participant's perspective — the interval
  in which a valid token re-binds to the existing participation.
- **Session continuity:** the property that the *same participation* continues across connection loss.
- **Participation continuity:** the persistence of role/team/identity across reconnection (the payload of
  continuity).
- **Connection replacement:** supersession of the active connection by a newer valid one (single-active).
- **Participant timeout:** grace-lease expiry → participant **Departs** (Removed).
- **Room timeout:** room-level inactivity/empty expiry ([BR-RX](../../02-business-analysis/02-business-rules.md))
  → all participations expire.
- **Administrative timeout:** a bounded window for administrative/host actions (e.g., host-migration grace)
  before the Authority acts deterministically.

All windows are **decision inputs** evaluated by the single writer at the coordinated moment; none is a
free-running side effect that mutates state on its own.

---

## 10. Failure Analysis

For each: **Expected behavior · Architectural guarantee · Recovery · Business impact · Residual risk.**

| Case | Expected | Guarantee | Recovery | Business impact | Residual risk |
|------|----------|-----------|----------|-----------------|---------------|
| **Temporary disconnect** | Mark Disconnected; retain participation; start grace lease; pause if essential. | Participation survives; presence authoritative; play pauses only when the active team can't proceed. | Reconnect re-binds; resume. | None if resumed. | Player never returns → departs at grace. |
| **Long disconnect (> grace)** | Grace expires → Departure → Removed. | Seat released; token invalidated. | Return = fresh Join → next match/lobby. | Seat freed (fair). | — |
| **Network interruption** | Same as temporary disconnect; client resyncs via snapshot on return (ADR-004). | No stale authoritative view. | Snapshot-then-increments. | Temporary lag only. | — |
| **Duplicate connection** | Newest valid connection supersedes; old dropped. | Single active connection; no duplicate actions ([INV-P4](../../02-business-analysis/10-business-invariants.md)). | Immediate. | None. | User confusion (informed via a system event). |
| **Connection replacement (device switch)** | New device with valid token becomes active; old severed. | Continuity preserved; one active. | Immediate. | Seamless. | Token misuse → see §14. |
| **Host disconnect** | Control-only; match unaffected; host-migration grace lease starts. | Play continues; exactly one host after migration ([INV-R1](../../02-business-analysis/10-business-invariants.md)). | Reconnect keeps host if within grace; else deterministic successor. | None to the match. | — |
| **Host abandonment** | After grace, deterministic successor; if none, room expires. | One-or-zero-host never; never two. | Successor becomes host. | Room continues or ends cleanly. | — |
| **Server restart** | Authoritative state recovered (ADR-005); clients re-snapshot; presence re-established from reconnections. | Participation records recovered to last commit; no duplicates. | Re-snapshot + re-bind. | Temporary disconnect. | Depends on ADR-005 realization. |
| **Room recovery** | Recover participation records at last commit; recompute presence. | No duplicate participants; finished matches don't resume. | Re-bind on reconnect. | None. | — |
| **Client restart** | New connection presents stored token → re-bind within grace. | Continuity via token. | Reconnect. | Seamless if within grace. | Lost token → fresh Join. |
| **Unexpected termination** | Treated as disconnect; grace lease governs. | Participation retained until grace. | Reconnect or depart. | None if resumed. | — |
| **Participant removal (kick, lobby)** | Host-initiated removal → Removed; token invalidated. | Clean removal; no orphaned record. | Fresh Join to return. | Player removed (host power, lobby only). | — |
| **Future authentication** | Account attaches to the participation record at the identity seam. | Continuity model unchanged. | N/A. | Additive. | Requires disciplined seam (design). |
| **Future device switching** | Same as connection replacement, possibly across authenticated devices later. | One active connection preserved. | Supersede. | Additive. | — |

---

## 11. Host Continuity

- **Who is the Host?** A **participant role** (room-control privileges), not a separate identity; exactly
  one per room ([INV-R1](../../02-business-analysis/10-business-invariants.md)). The Host is a normal
  participant with extra privileges; **Host ≠ key access** (a Host sees the key only if also a Spymaster —
  ADR-006).
- **Host responsibilities:** room-level control (setup, start, rematch, lobby removal) — a coordinated
  Intent, never a bypass ([BR-HOST](../../02-business-analysis/02-business-rules.md)).
- **Host replacement / departure / migration:** on host **leave** → immediate migration; on host
  **disconnect** → migration deferred until the host-migration grace lease expires; successor is
  **deterministic** (longest-present connected participant, [BR-HM-2](../../02-business-analysis/02-business-rules.md)).
- **Host recovery / failure:** if the host reconnects within grace, they remain host; if the Authority
  (server) recovers, host designation is part of recovered authoritative state.
- **Administrative ownership:** all administrative privileges flow from the **host role** in the
  authoritative participation state; no external/administrative actor owns room state.
- **Future dedicated / hostless rooms:** the model permits a future variant where control is system-owned
  (a "system host") — additive, since host is a role attribute, not a hardwired participant. **Out of MVP
  scope**; noted as a seam.
- **Invariant:** control migration **never mutates game/match state** (only the host attribute) — matches
  continue uninterrupted ([BR-EC-11](../../02-business-analysis/02-business-rules.md)).

---

## 12. Architectural Invariants (AI-PART-*)

- **AI-PART-1:** **Exactly one authoritative participation record** per participant, per room.
- **AI-PART-2:** **Presence never determines game rules** (it may pause play; it never decides outcomes).
- **AI-PART-3:** **Connections never own game state** and never mutate it directly.
- **AI-PART-4:** **Participation survives transient connectivity** (within grace).
- **AI-PART-5:** **The Authority owns the participant lifecycle**; all transitions are coordinated Intents.
- **AI-PART-6:** **Reconnection never bypasses the Authority**; it re-binds via a validated token+grace.
- **AI-PART-7:** **Duplicate participation never creates duplicate state or duplicate gameplay actions.**
- **AI-PART-8:** **Exactly one active connection** represents a participant at a time (newest supersedes).
- **AI-PART-9:** **Participation is room-scoped**; **no cross-room participation state** exists.
- **AI-PART-10:** **Participation identity is transient and PII-free**; a durable account may attach only
  at the identity seam without changing this model.
- **AI-PART-11:** **Roles/teams are frozen during a match**; reconnection restores the **existing** role,
  never assigns a new one mid-match.
- **AI-PART-12:** **Every lifecycle transition is coordinated and versioned** (traceable to an Intent and
  a committed version — composing with [ADR-004 versioning](ADR-004-real-time-communication-delivery.md#7-versioning-strategy)).

---

## 13. Architecture Fitness Functions (FF-PART-*)

- **FF-1:** **Exactly one participation record per participant** at any instant (no duplicates).
- **FF-2:** **No connection modifies authoritative state** (connection events only *signal*; the writer mutates).
- **FF-3:** **Reconnect always binds to the existing participation** (never creates a second record) when the token is valid and within grace.
- **FF-4:** **Duplicate/parallel connections never duplicate gameplay actions** (superseded connection's Intents are discarded).
- **FF-5:** **Presence projections derive from authoritative participation** (recompute ⇒ identical presence view).
- **FF-6:** **Participant lifecycle is deterministic** — same events/order ⇒ same lifecycle transitions.
- **FF-7:** **Participation history is traceable** (each transition ↔ its Intent + committed version).
- **FF-8:** **Every lifecycle transition is versioned** (monotonic per-room version).
- **FF-9:** **No cross-room participation artifact** exists (isolation).
- **FF-10:** **Reconnection restores the pre-disconnect role/team** (frozen mid-match) and its role-filtered projection contains **no unauthorized data** (composes with ADR-006 FF-1).

---

## 14. Security Analysis

Separating **architectural guarantees** from **future technical controls** (auth/crypto — Non-Goals).

| Threat | Architectural guarantee | Future technical control |
|--------|-------------------------|--------------------------|
| **Participant impersonation** | Re-binding requires a valid, room-scoped, **unguessable** continuity token; identity/role come from authoritative state, not client claims. | Authenticated accounts (future) strengthen assurance. |
| **Duplicate participation** | One record + supersession; a re-join with the same token re-binds, never duplicates (AI-PART-1/7). | — |
| **Connection hijacking** | Single active connection; a hijacker with a stolen token would **supersede** (evict) the victim's connection (detectable, not silent duplication); token is grace-bounded and invalidated on departure. | Encrypted transport; bound tokens; auth (future). |
| **Replay** | Lifecycle transitions are coordinated Intents bound to state/version; replays are stale/no-op (ADR-003/004). | Transport anti-replay. |
| **Session fixation (conceptual)** | Tokens are **issued by the Authority** at join (server-minted), not accepted from the client as a pre-set value; a client cannot fix a token. | Auth-grade token issuance (future). |
| **Administrative misuse / host takeover** | Host is an authoritative role; migration is deterministic and coordinated; no external actor can seize control; control never mutates game state. | Auth-based admin (future). |
| **Role escalation** | Roles come from authoritative state; frozen mid-match; claiming Spymaster is a coordinated Intent under one-Spymaster rules (AI-PART-11, [INV-T3/T5](../../02-business-analysis/10-business-invariants.md)). | — |
| **Presence manipulation** | Presence is Authority-owned; clients cannot set others' presence; a client can only *be* connected or not. | — |
| **Projection misuse via reconnect** | Reconnect delivers the **restored role's** projection only (ADR-006); no elevation. | — |

**Bottom line:** even without authentication, the architecture prevents **duplicate actors**, **silent
hijack** (supersession is visible), **client-minted identity/role**, and **cross-room leakage**. Stronger
guarantees (who holds a token) are an **auth** concern that attaches additively at the identity seam.

---

## 15. Trade-off Analysis

- **Correctness:** Maximized — participation is authoritative, single-writer, coordinated.
- **Availability:** High — brief drops don't eject players; play continues where possible.
- **Recovery:** Strong — participation records recover with the aggregate (ADR-005); reconnect re-binds.
- **Operational complexity:** Modest — grace leases + supersession; no external session/auth infra.
- **Developer experience:** Clear separation (participant / connection / presence) reduces confusion.
- **Future authentication:** Excellent — the identity seam attaches accounts without model change.
- **Future scalability:** Good — participation is per-room; distribution (ADR-007) keeps records with the room owner.
- **Future matchmaking:** Compatible — matchmaking would create/route participations into rooms; the lifecycle is unchanged.
- **Future spectators:** Compatible — a spectator is a participation with a spectator role and an
  Operative-equivalent projection (ADR-006 AI-VIS-8); the lifecycle model already accommodates it.
- **Testing:** Strong — deterministic, versioned transitions; supersession and grace are simulable.
- **Maintainability:** High — one authoritative record; no connection/state entanglement.

---

## 16. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| Continuity token loss/theft | Architecture/Security | Room-scoped, grace-bounded, single-active, invalidated on departure; supersession makes hijack visible; auth later ([ENG-SE-02](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)). |
| Two connections briefly both act (race at supersession) | Correctness | ADR-003 ordering: only one active connection's Intents are admitted; superseded Intents discarded (FF-4). |
| Presence used to drive a rule | Correctness | AI-PART-2; rules depend on committed game state; presence only pauses/decides lifecycle (review + FF). |
| Grace expiry wrongly abandons a returning player | Fairness | Decision evaluated against committed state at the coordinated moment; boundary favors in-grace reconnection ([ADR-003 §9](ADR-003-per-room-coordination-model.md#9-failure-analysis)). |
| Orphaned participation after removal | Operational | Removal invalidates token, frees nickname, discards record (FF-1); room expiry discards all. |
| Recovery duplicates participants | Recovery | Participation is part of the single aggregate; recovered as one record; no duplication (ADR-002/005). |
| Cross-room participation leakage | Isolation | AI-PART-9/FF-9; participation lives in one room's aggregate. |
| Future auth breaks continuity | Evolution | Auth attaches to the record at the seam; continuity semantics unchanged (AI-PART-10). |

---

## 17. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | A **room-scoped, PII-free continuity token** can re-bind a returning connection. *(Design established in business analysis.)* | Very High | Without it, resume would require accounts — contradicting the no-auth MVP. |
| AS-2 | **One active connection per participant** is acceptable UX (a new device supersedes the old). | High | If simultaneous multi-device *viewing* were required, add read-only mirror connections (still one *actor*) — additive. |
| AS-3 | **Grace/lease windows** suffice to cover common mobile drops. | High | Tune durations (operational parameters); if insufficient, adjust — no model change. |
| AS-4 | **Presence need not be globally durable** — it is room-lifetime and re-established on reconnect. | High | If durable presence history were needed, record events (ADR-005) — not the live model. |
| AS-5 | **Participant identity is transient and account-free today**, account-ready via the seam. | Fact (scope) | — |

---

## 18. Non-Goals

This ADR does **not** decide: **authentication, authorization, identity providers, JWT, OAuth, cookies**;
**transport (SignalR/WebSockets)**; **persistence implementation**; **encryption**; **databases**;
**frameworks**; **infrastructure**; **deployment**. It defines **only** the participant lifecycle,
presence, and session-continuity architecture.

---

## 19. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-009 |
|-------------|-------------------------------|
| **ADR-005 State Recovery & Resilience** | Participation records (identity ref, presence, token validity, role/team) are part of the recovered aggregate at last commit; recovery yields **one** record per participant; presence is re-established, not restored stale; finished matches don't resume. |
| **ADR-007 Room Isolation & Distribution** | Participation is room-scoped; distribution keeps records with the room owner; single-active-connection and single-owner preserved across routing/failover. |
| **ADR-008 Dictionary Architecture** | No dependency; participation is content-agnostic. |
| **ADR-010 Command/Query Strategy** | Join/leave/reconnect/role/team are **commands** (coordinated Intents); presence/participation are queried as **projections**; connection acks are not authoritative. |
| **Software Architecture / Technical Design** | Names the participation record + continuity token + grace-lease concepts; the **identity seam** is where future auth attaches; transport binds connections to the single-active slot. |
| **Implementation** | No connection-owned state; supersede, don't duplicate; lifecycle transitions via coordination only. |
| **Testing** | Reconnect/supersession/grace/host-migration determinism; no-duplicate-participation; presence-derivation; no cross-room state (FF-PART-*). |
| **Operations** | Monitor single-active-connection, grace/timeout behavior, host-migration correctness; presence carries no PII/secret. |
| **Future Authentication** | Attaches a durable account to the participation record at the identity seam — **no** change to lifecycle/continuity. |
| **Future Matchmaking** | Creates/routes participations into rooms; lifecycle unchanged. |
| **Future Spectator Mode** | A spectator is a participation with a spectator role + Operative-equivalent projection; lifecycle already supports it. |

---

## 20. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | [BR-JR](../../02-business-analysis/02-business-rules.md) (join), [BR-LR](../../02-business-analysis/02-business-rules.md) (leave), [BR-DC](../../02-business-analysis/02-business-rules.md) (disconnect/reconnect), [BR-HM](../../02-business-analysis/02-business-rules.md) (host migration), [BR-RX](../../02-business-analysis/02-business-rules.md) (room expiry), [BR-HOST](../../02-business-analysis/02-business-rules.md). |
| **Business Invariants** | [INV-P2](../../02-business-analysis/10-business-invariants.md) (transient PII-free identity), [INV-P4](../../02-business-analysis/10-business-invariants.md) (one connection), [INV-R1](../../02-business-analysis/10-business-invariants.md) (one host), [INV-T3/T5](../../02-business-analysis/10-business-invariants.md) (one Spymaster/frozen roles), [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak on resume). |
| **Engineering Challenges** | [ENG-RT-03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) (reconnect), ENG-CO-04 (duplicate connections), ENG-SE-01/02/03 (nickname/token/abandonment), ENG-RM-02 (mass disconnect). |
| **Quality Attribute Scenarios** | [QS-07](../09-quality-attribute-scenarios.md) (reconnect continuity), [QS-01](../09-quality-attribute-scenarios.md) (no leak), [QS-05](../09-quality-attribute-scenarios.md) (availability). |
| **State Ownership** | [05 State Ownership](../05-state-ownership.md): S-06 session/identity, S-07 presence — owned by Authority. |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md): CB-05 (join), CB-07 (host migration), CB-08 (reconnect). |
| **ADR-000** | *Session, Presence, Reconnect Token, Authority, Room Isolation, Projection, Determinism, Future-Auth Seam* used as defined. |
| **ADR-001/002/003/004/006** | Participation is authoritative aggregate state, coordinated by the single writer, delivered as role-filtered projections over the versioned delivery model. |
| **Governance** | [AP-03/06/07/08/13/18](../../06-architecture-governance/01-architecture-principles.md); [AAP-02/08/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 21. Architecture Review

- **Decision:** Participant-Centric Lifecycle with Token-Based Continuity and Lease-Based Presence
  (LM6): one Authority-owned participation record per participant; transient connections; room-scoped
  PII-free continuity token; single active connection (newest supersedes); authoritative-but-rules-neutral
  presence; deterministic host continuity.
- **Confidence:** **High** — it is the minimal model satisfying continuity, single-actor, fairness, and
  account-readiness, and it is entailed by the frozen ADRs and business invariants.
- **Remaining risks:** continuity-token security (strengthened later by auth); supersession-race
  correctness (guarded by ADR-003 ordering + FF-4); presence-never-drives-rules discipline (FF/review).
- **Open questions (delegated, non-blocking):** grace/lease **durations** (operational parameters);
  whether read-only mirror connections are ever desired (additive); spectator/matchmaking specifics
  (future modes); token issuance hardening (auth/technical design).
- **Review triggers:** introduction of authentication; multi-device simultaneous *viewing*; spectator or
  matchmaking modes; a requirement for durable presence/participation history; hostless/system-owned rooms.
- **Readiness for ADR-005:** **READY.** Participation, presence, and continuity are now defined as
  authoritative aggregate state with clear recovery expectations, so ADR-005 can specify **how** the
  aggregate (including participation records) is recovered to its last commit — knowing that participants
  are single, room-scoped, coordinated, and re-bindable by token within grace.

---

## 22. Adversarial Architecture Review — "Attempt to Break the Design"

For each: **Attack · Expected Outcome · Architectural Protection · Residual Risk · Mitigation.**

1. **Can one participant appear twice?**
   - *Expected:* No. *Protection:* one authoritative record; a re-join re-binds (AI-PART-1/7, FF-1/3).
   - *Residual:* none architecturally. *Mitigation:* —
2. **Can reconnect create a second identity?**
   - *Expected:* No. *Protection:* reconnect **binds to the existing** record via token+grace (AI-PART-6, FF-3).
   - *Residual:* invalid/expired token → **fresh** Join (a new participant, correctly). *Mitigation:* clear UX.
3. **Can a disconnected participant continue influencing gameplay?**
   - *Expected:* No. *Protection:* only the **active connection** may act; a lost connection cannot submit Intents (AI-PART-3/8).
   - *Residual:* none. *Mitigation:* —
4. **Can two devices control the same participant simultaneously?**
   - *Expected:* No. *Protection:* single active connection; newest supersedes; superseded Intents discarded (AI-PART-8, FF-4).
   - *Residual:* momentary overlap at handover. *Mitigation:* ADR-003 ordering admits only the active connection.
5. **Can a stale connection overwrite a newer one?**
   - *Expected:* No. *Protection:* supersession makes the newest connection authoritative; the old is severed; connections never write state anyway (AI-PART-3/8).
   - *Residual:* none. *Mitigation:* —
6. **Can host replacement violate authority?**
   - *Expected:* No. *Protection:* migration is a deterministic, coordinated transition; exactly one host; control never mutates game state (AI-PART-5, [INV-R1](../../02-business-analysis/10-business-invariants.md), [BR-EC-11](../../02-business-analysis/02-business-rules.md)).
   - *Residual:* none. *Mitigation:* —
7. **Can room recovery duplicate participants?**
   - *Expected:* No. *Protection:* participation is part of the single aggregate; recovered as one record each (AI-PART-1; ADR-002/005).
   - *Residual:* depends on ADR-005 idempotent recovery. *Mitigation:* ADR-005 fitness functions.
8. **Can reconnect bypass visibility rules?**
   - *Expected:* No. *Protection:* reconnect delivers the **restored role's** projection (ADR-006); role frozen mid-match (AI-PART-11, FF-10).
   - *Residual:* none. *Mitigation:* —
9. **Can participant state leak across rooms?**
   - *Expected:* No. *Protection:* participation is room-scoped; no cross-room state (AI-PART-9, FF-9).
   - *Residual:* routing misconfiguration. *Mitigation:* isolation fitness test (composes with ADR-007).
10. **Can temporary disconnect reveal hidden information?**
    - *Expected:* No. *Protection:* on return, only the role-filtered projection is delivered; disconnection changes presence, not visibility rules; the key is never in a non-Spymaster projection (ADR-006).
    - *Residual:* none. *Mitigation:* —
11. **Can future authentication invalidate continuity?**
    - *Expected:* No. *Protection:* auth attaches to the participation record at the seam; continuity semantics unchanged (AI-PART-10).
    - *Residual:* poorly-designed seam could couple auth to lifecycle. *Mitigation:* keep the seam at identity only (design discipline; this ADR mandates it).
12. **Can participant removal leave orphaned state?**
    - *Expected:* No. *Protection:* removal invalidates token, frees nickname, discards the record; room expiry discards all (FF-1).
    - *Residual:* none. *Mitigation:* —

**Conclusion:** the lifecycle architecture prevents **duplicate participation/actors**, **stale-connection
writes**, **cross-room leakage**, **visibility bypass on reconnect**, and **authority violation on host
change** — **by construction** — because participation is a single, Authority-owned, coordinated,
room-scoped record; connections are transient and never authoritative; presence is owned but
rules-neutral; and continuity is a validated, single-active re-binding. The residual exposures (continuity-
token security, seam discipline for future auth) are **future technical-control** concerns delegated to
authentication/technical design, not weaknesses of this model.

---

## Final Deliverable — Answers

- **What is a participant?** A person taking part in **one room**, represented by an **authoritative,
  Authority-owned participation record** (identity ref, role/team, presence, continuity-token validity).
- **What is authoritative participation?** The participation record inside the Room aggregate — the single
  truth of who is taking part, in what role/team, with what presence — owned and mutated only by the room
  Authority via coordinated Intents.
- **What is presence?** An authoritative attribute of that record (Connected / Disconnected-in-grace /
  Departed) that **drives** pause/migration/abandonment decisions and is **projected** role-safely, but
  **never decides game outcomes**.
- **What survives a disconnect?** The **participation record** (identity, role/team, presence=disconnected,
  token validity) for the grace window — participation outlives the connection.
- **What survives a reconnect?** The **same participation** — restored role/team/view — re-bound via the
  continuity token; the new connection becomes the single active one.
- **What does NOT survive?** The **old connection** (superseded/severed); **transient transport/UI state**;
  and, after grace expiry, the **participation itself** (Departed → Removed). Finished matches never resume.
- **How is continuity preserved?** By keeping participation **authoritative and connection-independent**,
  and re-binding a returning connection to the existing record via a **room-scoped, PII-free token within a
  grace lease**, with exactly one active connection.
- **Why is participation independent of authentication?** Identity is a **transient, room-scoped**
  participation record today; accounts are unnecessary to play and attach later at the identity seam — so
  no-auth play and future login coexist without model change.
- **Why is a connection never authoritative?** Connections are transient pipes that **only signal**
  presence and **carry** Intents/projections; they **never own or mutate** game state — the Authority does
  (ADR-001/002/003).
- **How does the architecture support future login, matchmaking, spectators, and multiple devices without
  changing the core model?** *Login* attaches a durable account to the participation record at the identity
  seam; *matchmaking* creates/routes participations into rooms; *spectators* are participations with a
  spectator role + Operative-equivalent projection; *multiple devices* resolve via single-active-connection
  supersession (with optional future read-only mirrors). Each is **additive**, leaving participation,
  presence, and continuity semantics untouched.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: participant-centric lifecycle with token-based continuity and lease-based presence; lifecycle, presence, connection-ownership, host continuity, invariants, fitness functions, security & adversarial review, verdict. |
