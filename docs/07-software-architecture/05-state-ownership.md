# 07.05 — State Ownership Analysis (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Identify every important state and, for each: who owns it, who may read/modify it, who must never modify it, its consistency requirements, and the consequence of ownership violation. |
| **Scope** | State ownership analysis. No storage design, schemas, or technology. |
| **Inputs** | [Domain Model](../02-business-analysis/06-domain-model.md), [State Machines](../02-business-analysis/07-state-machines.md), [Invariants](../02-business-analysis/10-business-invariants.md), [Responsibilities](03-system-responsibilities.md). |
| **Outputs** | Ownership rules the architecture must enforce. |
| **Dependencies** | [Responsibility Boundaries](04-responsibility-boundaries.md). |
| **Cross References** | [Consistency Boundaries](06-consistency-boundaries.md). |
| **Related Business Documents** | [Data Lifecycle](../03-business-governance/05-data-lifecycle-retention.md). |
| **Related Engineering Documents** | ENG-ST-01/02/03, ENG-CO-04, ENG-FP-01. |

---

## Legend
Owner = single accountable responsibility (from [R-catalog](03-system-responsibilities.md)).
"Read" = may observe (possibly role-filtered). "Modify" = may change. "Never modify" = forbidden writers.

### S-01 Room State (membership, host, code, capacity, room status)
- **Owner:** R-01 Room Management.
- **Read:** R-02 Lobby, R-11 Delivery (to participants), R-16 Cleanup, R-12 Connection.
- **Modify:** R-01 only (via validated intents).
- **Never modify:** Rules & Play, Delivery, clients.
- **Consistency:** Strong — exactly one host; capacity/nickname atomic ([INV-R1/R5/P1](../02-business-analysis/10-business-invariants.md)).
- **If violated:** Zero/two hosts, over-capacity, duplicate nicknames → stuck/unfair rooms.

### S-02 Match/Game State (status, active team, result)
- **Owner:** R-03 Match Lifecycle (status/result) with R-04 Rules Engine as the sole mutator during play.
- **Read:** R-11 Delivery, R-07 Turn, R-17 Observability.
- **Modify:** R-04/R-03 only.
- **Never modify:** clients, Delivery, Connection.
- **Consistency:** Strong — one active match; finished never resumes; one result ([INV-G7/O1/O4](../02-business-analysis/10-business-invariants.md)).
- **If violated:** Play past a finished match; multiple/incorrect results → corrupted outcomes.

### S-03 Board & Key State (25 cards, ownership map, reveal flags)
- **Owner:** R-05 Board Generation creates it; R-04 Rules Engine mutates reveal flags only.
- **Read:** **Key** → Spymasters only (via R-11); **words/reveals** → all (via R-11).
- **Modify:** R-04 (reveal flags) only; ownership is immutable after generation.
- **Never modify:** anyone changes ownership; Operatives/clients never receive unrevealed ownership.
- **Consistency:** Strong — immutable ownership; one-way reveals; 9/8/7/1 partition ([INV-B2/B5/B7/B9](../02-business-analysis/10-business-invariants.md)).
- **If violated:** **Existential** — a leaked key or mutable ownership destroys the game.

### S-04 Turn State (phase, active team, active clue, guess allowance/count)
- **Owner:** R-07 Turn Management (driven by R-04 outcomes).
- **Read:** R-08/R-09, R-11 Delivery.
- **Modify:** R-07/R-04 only, atomically.
- **Never modify:** clients, Connection, Delivery.
- **Consistency:** Strong — one active turn, one active clue, ≥1 guess before voluntary end ([INV-G2/G3/G5](../02-business-analysis/10-business-invariants.md)).
- **If violated:** Two clues/turns, wrong guess counts → unfair, corrupt play.

### S-05 Player State (nickname, team, role within a match)
- **Owner:** R-01 (membership) + R-02 (team/role in lobby); locked during match.
- **Read:** R-11 Delivery, Rules & Play (role/team for authorization).
- **Modify:** R-02 in lobby/post-match; **frozen** during a match.
- **Never modify:** anyone changes team/role mid-match.
- **Consistency:** Strong at start (locked); one team, one role ([INV-T2/T4/T5](../02-business-analysis/10-business-invariants.md)).
- **If violated:** Two Spymasters, mid-match team switch → key/turn assumptions break.

### S-06 Session/Identity State (transient token, validity)
- **Owner:** R-13 Session/Identity.
- **Read:** R-12 Connection, R-10 Validation (for reconnect authorization).
- **Modify:** R-13 only (issue/invalidate).
- **Never modify:** clients (cannot forge), other responsibilities.
- **Consistency:** Strong for single-active-connection & grace validity ([INV-P2/P4](../02-business-analysis/10-business-invariants.md)).
- **If violated:** Seat hijack, dual actors, leaked role-views.

### S-07 Connection/Presence State (connected/disconnected/grace)
- **Owner:** R-12 Connection/Presence.
- **Read:** R-01 (host migration), Rules & Play (pause decisions), R-11.
- **Modify:** R-12 only.
- **Never modify:** Rules & Play (it reacts, doesn't set presence); clients.
- **Consistency:** Eventually-consistent presence is tolerable, but pause/abandonment decisions derived from it must be consistent at the moment of action.
- **If violated:** Wrongful abandonment/pause, or ghost actors.

### S-08 Dictionary State (versions, active/deprecated/retired, pinned selection)
- **Owner:** R-06 Dictionary Provision (content team upstream).
- **Read:** R-05 Board Generation (at start), R-02 (selection).
- **Modify:** R-06 only; versions immutable; a match's pinned version never changes.
- **Never modify:** Rules & Play, clients.
- **Consistency:** Strong pinning per match ([INV-D3](../02-business-analysis/10-business-invariants.md)); catalog is read-mostly.
- **If violated:** Mid-match word change → unfair/non-reproducible board.

### S-09 Authoritative Custody & Recovery State (committed snapshots)
- **Owner:** R-14 State Store (+ R-15 Recovery).
- **Read:** Rules & Play (current state), R-15 (snapshots), R-11 (to deliver).
- **Modify:** written only by the owning mutators via atomic commit; store itself never adjudicates.
- **Never modify:** store must not change semantics; clients cannot write.
- **Consistency:** Strong — single authoritative state per room; commit-then-broadcast.
- **If violated:** Divergent truth, partial state, replayed terminals.

### S-10 Result/History State (Game Result, recorded events)
- **Owner:** R-03 Match Lifecycle (result), R-17 Observability (events).
- **Read:** R-11, R-17; retained PII-free per policy.
- **Modify:** written once at match end; immutable thereafter.
- **Never modify:** anyone after recording ([INV-O4](../02-business-analysis/10-business-invariants.md)).
- **Consistency:** Strong — one immutable result per completed match.
- **If violated:** Disputed/altered outcomes; loss of auditability.

## Cross-cutting ownership rules

1. **Single writer per state** (the owner); everyone else reads (possibly filtered).
2. **Match/turn/board mutation belongs solely to Rules & Play**; lifecycle owners set status/result.
3. **Ownership (S-03 key) is a server secret** until reveal; Operative-facing paths never carry it.
4. **Store/Delivery/Connection/Observability never adjudicate** — they hold, transport, signal, or observe.
5. **Violation of any ownership rule is a defect** that must be rejected/prevented, not tolerated.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial state-ownership analysis (S-01…S-10). |
