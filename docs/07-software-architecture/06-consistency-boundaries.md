# 07.06 — Consistency Boundaries (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Identify where **strong consistency** is required, and for each: business reason, engineering impact, failure consequences, consistency requirement, and trade-offs. |
| **Scope** | Consistency-requirement analysis. No mechanisms, technologies, or patterns chosen. |
| **Inputs** | [State Ownership](05-state-ownership.md), [Rule Precedence](../02-business-analysis/16-rule-precedence.md), [Invariants](../02-business-analysis/10-business-invariants.md). |
| **Outputs** | The set of operations that must be strongly consistent, for the architecture to guarantee. |
| **Dependencies** | [Responsibility Boundaries](04-responsibility-boundaries.md). |
| **Cross References** | [Command & Query Discovery](07-command-query-discovery.md), [Interactions](08-interaction-discovery.md). |
| **Related Business Documents** | [Business Rules](../02-business-analysis/02-business-rules.md), [State Machines](../02-business-analysis/07-state-machines.md). |
| **Related Engineering Documents** | ENG-GP-01/03/05, ENG-CO-01/02/04, ENG-RT-03, ENG-RE-01. |

---

## Principle
Within a **single room**, authoritative game state requires **strong consistency**: participants
must never observe or act on contradictory authoritative state that affects an outcome. **Across
rooms**, no consistency relationship exists (rooms are isolated). Some *presence/observability*
signals may be eventually consistent, but any **decision** derived from them at the moment of action
must be consistent.

## Consistency boundaries (CB-01 … CB-10)

### CB-01 Card Reveal & Guess Resolution
- **Business reason:** A guess reveals ownership, updates counts, and may end the turn or the match — atomically, in precedence order.
- **Engineering impact:** Requires serialized, atomic resolve (reveal→count→terminal→turn); first-valid-wins on races.
- **Failure consequences:** Double-reveal, wrong counts, skipped win/assassin → **corrupted outcome** (critical).
- **Consistency requirement:** **Strong**, atomic per guess ([INV-B7/O2](../02-business-analysis/10-business-invariants.md), [RP-1..12](../02-business-analysis/16-rule-precedence.md)).
- **Trade-offs:** Caps per-room guess throughput; acceptable — rooms are small.

### CB-02 Turn Change
- **Business reason:** Ending a turn flips active team, clears the clue, resets allowance — together.
- **Engineering impact:** Atomic transition; guard against partial/duplicate application.
- **Failure consequences:** Two active turns/clues, stale allowance → illegal state.
- **Consistency requirement:** **Strong**, atomic ([INV-G2/G3](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** Minimal; must coordinate with match-end preemption.

### CB-03 Game Completion (win/loss/assassin)
- **Business reason:** The moment a terminal condition holds, the match ends and records one result.
- **Engineering impact:** Terminal check after every reveal, before continuation; single-winner enforcement.
- **Failure consequences:** Play past completion; multiple/incorrect results.
- **Consistency requirement:** **Strong**; terminal freeze ([INV-G7/O1](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** None acceptable — correctness is paramount.

### CB-04 Room Creation (code uniqueness)
- **Business reason:** A room code must be unique among live rooms.
- **Engineering impact:** Atomic allocation/uniqueness check.
- **Failure consequences:** Code collision → ambiguous joins.
- **Consistency requirement:** **Strong** for the code space at allocation ([INV-R2](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** Trivial contention at creation.

### CB-05 Join (capacity & nickname uniqueness)
- **Business reason:** Never exceed capacity; nicknames unique within a room.
- **Engineering impact:** Atomic admit (capacity + nickname together).
- **Failure consequences:** Over-capacity; duplicate nicknames.
- **Consistency requirement:** **Strong** at admit ([INV-R5/P1](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** Serializes bursts of joins per room (small cost).

### CB-06 Team & Role Assignment (and the start boundary)
- **Business reason:** One Spymaster per team; composition frozen at start.
- **Engineering impact:** Atomic role claim; hard lock at start; reject in-flight setup at the boundary.
- **Failure consequences:** Two Spymasters; mid-match team/role change → key/turn corruption.
- **Consistency requirement:** **Strong** at claim and at start-lock ([INV-T3/T5](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** Must resolve simultaneous claims to exactly one.

### CB-07 Host Migration
- **Business reason:** Exactly one host at all times; deterministic successor.
- **Engineering impact:** Atomic transfer against post-leave membership; deterministic tie-break.
- **Failure consequences:** Zero/two hosts; orphaned room.
- **Consistency requirement:** **Strong** for host singularity ([INV-R1](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** Control-only; must not touch match state.

### CB-08 Reconnect (identity & snapshot)
- **Business reason:** A returning player resumes exact team/role/view within grace; one active connection.
- **Engineering impact:** Atomic supersede of prior connection; consistent role-filtered snapshot.
- **Failure consequences:** Dual actors; wrong/leaky view; lost resume.
- **Consistency requirement:** **Strong** for single-active-connection and snapshot correctness ([INV-P4/P5](../02-business-analysis/10-business-invariants.md)).
- **Trade-offs:** Snapshot cost per reconnect.

### CB-09 Match Start / Board Generation
- **Business reason:** Board+key+starting team fixed atomically; setup locked.
- **Engineering impact:** Composite operation with one commit boundary (validate→generate→lock→open turn).
- **Failure consequences:** Half-started match; inconsistent board/setup.
- **Consistency requirement:** **Strong**, atomic composite ([BR-GS-4](../02-business-analysis/02-business-rules.md)).
- **Trade-offs:** Re-validate at the commit if membership changed.

### CB-10 Room Expiry vs Activity / Result Recording
- **Business reason:** Never close a live room; record result before closing.
- **Engineering impact:** Atomic re-check at expiry fire; order result-record before expiry.
- **Failure consequences:** Live room closed; lost result.
- **Consistency requirement:** **Strong** at the decision point ([BR-RX](../02-business-analysis/02-business-rules.md), [RP-11](../02-business-analysis/16-rule-precedence.md)).
- **Trade-offs:** Idle timing may be coarse (eventually-consistent) as long as the decision is atomic.

## Where eventual consistency is acceptable
- **Presence indicators** (someone is "connecting") for display — provided pause/abandonment
  *decisions* are made against consistent state at the moment of action (CB-08).
- **Observability/metrics** (S-10) — lag is fine; they never drive outcomes.
- **Idle-timer granularity** — expiry timing may be approximate; the *expiry decision* is strong (CB-10).

## Cross-room note
No consistency relationship exists **between** rooms. This is a deliberate isolation boundary that
enables scale ([AAP-08](../06-architecture-governance/02-architecture-anti-principles.md)); the
architecture must not introduce cross-room consistency.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial consistency-boundary analysis (CB-01…CB-10). |
