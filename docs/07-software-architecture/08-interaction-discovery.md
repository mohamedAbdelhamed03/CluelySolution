# 07.08 — Interaction Discovery (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Describe how responsibilities interact for the key flows: initiator, receiver, purpose, expected outcome, state changes, events, failure cases, retry, ordering, and timeout considerations. Technology-neutral. |
| **Scope** | Interaction analysis between responsibilities (R-01…R-17). No protocols, APIs, message formats, or technology. |
| **Inputs** | [Workflows](../02-business-analysis/08-business-workflows.md), [Domain Events](../02-business-analysis/11-domain-events-catalog.md), [Responsibilities](03-system-responsibilities.md). |
| **Outputs** | Interaction requirements (ordering, retry, timeout) for the architecture to satisfy. |
| **Dependencies** | [Consistency Boundaries](06-consistency-boundaries.md), [State Ownership](05-state-ownership.md). |
| **Cross References** | [Command & Query Discovery](07-command-query-discovery.md), [Quality Attribute Scenarios](09-quality-attribute-scenarios.md). |
| **Related Business Documents** | [Business Rules](../02-business-analysis/02-business-rules.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md). |
| **Related Engineering Documents** | ENG-RT-01/03, ENG-GP-01, ENG-CO-04, ENG-RE-01. |

---

## Interaction shape (responsibility-level, technology-neutral)

```
Player intent → [R-11 Delivery: receive] → [R-10 Validation/Authorization]
   → [R-04 Rules & Play cluster: adjudicate atomically] ↔ [R-14 State Store: commit]
   → emit events → [R-11 Delivery: role-filtered broadcast] → participants
Presence signals → [R-12 Connection] → (pause/migration/abandon) → [R-04 / R-01]
```

Below, each interaction is `Initiator → Receiver`.

### I-01 Submit Guess (the critical path)
- **Initiator → Receiver:** Player (via R-11) → R-10 → R-09/R-04.
- **Purpose:** Reveal a card and resolve its outcome.
- **Expected outcome:** One atomic reveal + count/turn/terminal update; events broadcast role-filtered.
- **State changes:** S-03 reveal, S-02/S-04 counts/turn (possibly terminal).
- **Business events:** GuessSubmitted, CardRevealed, (TurnEnded | GameFinished).
- **Failure cases:** Not active operative/phase, revealed card, over limit → catalogued rejection; no state change.
- **Retry:** Idempotent — a replayed guess is a no-op/reject (bound to state version).
- **Ordering:** **Strict serialization per room**; first-valid-wins; terminal preempts turn (RP-1..12).
- **Timeout:** If the resolving path stalls, the intent must fail cleanly without partial application.

### I-02 Submit Clue
- **Initiator → Receiver:** Spymaster (via R-11) → R-10 → R-08/R-07.
- **Purpose:** Establish the active clue and guess allowance.
- **Outcome:** Active clue; phase → guessing; broadcast.
- **State changes:** S-04 (clue, allowance, phase).
- **Events:** ClueSubmitted.
- **Failure:** Not active Spymaster/phase, multi-word/board-word/invalid number → rejection.
- **Retry:** Idempotent; one clue per turn (a second is rejected).
- **Ordering:** Before any guess in the turn; single active clue.
- **Timeout:** If the active Spymaster is disconnected, the clue phase pauses (see I-06).

### I-03 Start Match
- **Initiator → Receiver:** Host (via R-11) → R-10 → R-02 → R-05/R-04 → R-14.
- **Purpose:** Generate board+key, lock setup, open first turn.
- **Outcome:** Match In-Progress; role-filtered board delivered (key to Spymasters only).
- **State changes:** S-02/03/04/05 (created/locked).
- **Events:** GameStarted, BoardGenerated, StartingTeamSelected, TurnStarted.
- **Failure:** Invalid composition/dictionary → rejection; no match created.
- **Retry:** Idempotent — a duplicate start after generation is rejected.
- **Ordering:** Composite, single commit boundary (validate→generate→lock→open).
- **Timeout:** Re-validate at commit if membership changed mid-operation.

### I-04 Join Room
- **Initiator → Receiver:** Player (via R-11) → R-10 → R-01 (+ R-13 token).
- **Purpose:** Admit a player with a unique nickname within capacity.
- **Outcome:** Member added; identity issued; membership broadcast.
- **State changes:** S-01, S-06.
- **Events:** PlayerJoined.
- **Failure:** Invalid/expired code, full, duplicate nickname, match-in-progress → rejection/waiting.
- **Retry:** Safe with same nickname (idempotent admit by identity).
- **Ordering:** Atomic capacity+nickname admit; concurrent joins serialized per room.
- **Timeout:** Transient failures retryable; no partial membership.

### I-05 Leave / Host Migration
- **Initiator → Receiver:** Player (or R-12 on grace-expiry) → R-01.
- **Purpose:** Remove a player; if host, reassign deterministically.
- **Outcome:** Membership updated; exactly one host or room expired.
- **State changes:** S-01.
- **Events:** PlayerLeft, HostTransferred | RoomExpired.
- **Failure:** None to the leaver; must never leave zero/two hosts.
- **Retry:** Idempotent.
- **Ordering:** Recompute successor against post-leave membership; control-only (never match state).
- **Timeout:** Host disconnect defers migration until grace expiry.

### I-06 Disconnect → Pause / Abandon
- **Initiator → Receiver:** Connectivity signal → R-12 → (R-04/R-07 for pause; R-03 for abandon).
- **Purpose:** Tolerate drops; pause only when the active team can't proceed; abandon only after grace.
- **Outcome:** Player marked disconnected; phase paused if essential; abandonment if grace expires and unplayable.
- **State changes:** S-07 (presence); S-04 pause overlay; possibly S-02 terminal (abandon).
- **Events:** PlayerDisconnected, GamePaused | GameFinished(abandoned).
- **Failure:** Match end preempts pause/abandon (RP-7).
- **Retry:** N/A (signal-driven); presence may be eventually consistent, decisions consistent.
- **Ordering:** Match-end > pause/abandon > host-migration (precedence).
- **Timeout:** Grace timers govern pause→abandon and host migration.

### I-07 Reconnect
- **Initiator → Receiver:** Returning player (via R-11) → R-10 → R-13 → R-12 → R-14 (snapshot) → R-11.
- **Purpose:** Resume exact team/role/view within grace; supersede any prior connection.
- **Outcome:** Single active connection; role-filtered snapshot delivered; paused phase resumed.
- **State changes:** S-06/S-07; no match-state change.
- **Events:** PlayerReconnected, (GameResumed).
- **Failure:** Invalid token/expired grace/closed room → fresh join or failure.
- **Retry:** Idempotent; newest connection wins.
- **Ordering:** Supersede-then-snapshot; resume after snapshot.
- **Timeout:** Bounded by grace; snapshot delivery has a responsiveness target (QM-07).

### I-08 State Change → Role-Filtered Broadcast
- **Initiator → Receiver:** R-04/R-03/R-07 (committed change) → R-11 → participants.
- **Purpose:** Propagate authoritative state with per-role visibility.
- **Outcome:** Each participant receives a consistent, role-appropriate update.
- **State changes:** None (delivery reads committed state).
- **Events:** the corresponding domain event(s).
- **Failure:** Lost/dup/out-of-order delivery → clients resync to latest (versioned).
- **Retry:** Idempotent application on clients (apply newer only).
- **Ordering:** Broadcast **after** commit (commit-then-broadcast); per-room event order preserved.
- **Timeout:** Slow clients reconcile via resync; never block the authority.

### I-09 Recovery After Interruption
- **Initiator → Receiver:** Interruption signal → R-15 ↔ R-14.
- **Purpose:** Restore room/match to last consistent state without replaying terminal effects.
- **Outcome:** Authoritative state restored; play resumes; clients resync.
- **State changes:** Restores S-01..S-10 to last commit.
- **Events:** (implementation-defined recovery signal); no duplicate terminal events.
- **Failure:** If unrecoverable, room ends/abandons cleanly.
- **Retry:** Idempotent recovery.
- **Ordering:** Recover to last committed atomic state only.
- **Timeout:** Bounded by room lifetime.

### I-10 Room Expiry
- **Initiator → Receiver:** Inactivity/empty signal → R-16 → R-01/R-14.
- **Purpose:** Reclaim room; release code; record result first if mid-match abandonment.
- **Outcome:** Room closed; code released; participants notified.
- **State changes:** S-01 closed; S-10 result if applicable.
- **Events:** RoomExpired, RoomClosed.
- **Failure:** Activity during expiry → abort expiry (re-check).
- **Retry:** Idempotent close.
- **Ordering:** Result-record before expiry (RP-11); atomic re-check at fire.
- **Timeout:** Idle timer reset by activity; timing may be coarse.

## Cross-cutting interaction requirements (for the architecture)
- **Commit-then-broadcast** everywhere (I-08): never emit unbefore committed state.
- **Per-room strict ordering** for outcome-bearing writes (I-01/02/03).
- **Idempotency** for all state-changing intents (retry/replay safe).
- **Precedence** governs coincident events (I-06 vs I-01) per [Rule Precedence](../02-business-analysis/16-rule-precedence.md).
- **Timeouts fail cleanly** — no partial application; recovery restores consistency.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial interaction discovery (I-01…I-10). |
