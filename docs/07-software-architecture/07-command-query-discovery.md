# 07.07 — Command & Query Discovery (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Classify every business capability as a **Command** (changes state), **Query** (reads state), or **Mixed**, with purpose, state changes, read requirements, consistency needs, expected result, and related rules. Informs — does not design — any command/query separation. |
| **Scope** | Capability classification. **No API design**, endpoints, schemas, or technology. |
| **Inputs** | [Functional Requirements](../02-business-analysis/03-functional-requirements.md), [Workflows](../02-business-analysis/08-business-workflows.md), [Domain Events](../02-business-analysis/11-domain-events-catalog.md). |
| **Outputs** | A command/query map with consistency needs, feeding interaction and architecture design. |
| **Dependencies** | [Consistency Boundaries](06-consistency-boundaries.md), [State Ownership](05-state-ownership.md). |
| **Cross References** | [Interactions](08-interaction-discovery.md). |
| **Related Business Documents** | [Business Rules](../02-business-analysis/02-business-rules.md), [Validation Rules](../02-business-analysis/09-validation-rules.md). |
| **Related Engineering Documents** | ENG-GP-01/02, ENG-RT-01/03. |

---

## Note on classification
- **Command** = intent that changes authoritative state (and typically emits events). Results are
  observed via role-filtered notifications, not by "reading back."
- **Query** = read of current (role-filtered) state; no state change.
- **Mixed** = a command whose acknowledgment also returns/derives read data (kept minimal;
  determinism/consistency still governed by the command side).

## Commands (state-changing)

| Capability | Purpose | State changes | Consistency need | Expected result | Related rules |
|-----------|---------|---------------|------------------|-----------------|---------------|
| **Create Room** | Start a private room | New room, host, code (S-01) | Strong (code uniqueness, CB-04) | Room in Lobby; code issued | [BR-RC](../02-business-analysis/02-business-rules.md) |
| **Join Room** | Add a player | Membership, identity (S-01/S-06) | Strong (capacity+nickname, CB-05) | Player admitted (or catalogued rejection) | [BR-JR](../02-business-analysis/02-business-rules.md) |
| **Leave Room** | Remove a player | Membership; maybe host migration (S-01) | Strong (host singularity, CB-07) | Player removed; room updated/expired | [BR-LR/HM](../02-business-analysis/02-business-rules.md) |
| **Select/Switch Team** | Set team | Player team (S-05) | Strong at lobby; forbidden mid-match (CB-06) | Team assigned | [BR-TA](../02-business-analysis/02-business-rules.md) |
| **Claim/Release Role** | Set Spymaster/Operative | Player role (S-05) | Strong (one Spymaster, CB-06) | Role set (or rejected) | [BR-RO](../02-business-analysis/02-business-rules.md) |
| **Select Dictionary** | Choose region | Room dictionary selection (S-08) | Strong at lobby | Selection recorded | [BR-RC-4](../02-business-analysis/02-business-rules.md) |
| **Start Match** | Begin play | Board+key+turn; status; lock (S-02/03/04) | Strong composite (CB-09) | Match In-Progress; first turn open | [BR-GS/BG](../02-business-analysis/02-business-rules.md) |
| **Submit Clue** | Direct operatives | Active clue; phase→guess (S-04) | Strong (one clue, CB-02) | Clue active; broadcast | [BR-CL](../02-business-analysis/02-business-rules.md) |
| **Submit Guess** | Reveal a card | Reveal, counts, turn/terminal (S-03/02/04) | **Strong, atomic, serialized** (CB-01/03) | Reveal + outcome events | [BR-GV/CG/IG/NC/OPP/ASN](../02-business-analysis/02-business-rules.md) |
| **End Turn** | Pass play | Turn transition (S-04) | Strong (CB-02) | Opponent becomes active | [BR-TE](../02-business-analysis/02-business-rules.md) |
| **Rematch / New Match** | Play again | Fresh match state (S-02/03/04) | Strong composite (CB-09) | New independent match | [BR-GE-4](../02-business-analysis/02-business-rules.md) |
| **Reconnect** | Resume participation | Connection/identity (S-06/07) | Strong (single connection, CB-08) | Restored role state; resume | [BR-DC](../02-business-analysis/02-business-rules.md) |
| **(System) Migrate Host** | Reassign control | Host (S-01) | Strong (CB-07) | Exactly one host | [BR-HM](../02-business-analysis/02-business-rules.md) |
| **(System) Expire Room** | Reclaim room | Room closed; code released (S-01) | Strong decision (CB-10) | Room expired; result recorded first | [BR-RX](../02-business-analysis/02-business-rules.md) |
| **(System) Pause/Resume** | Handle essential disconnect | Turn pause overlay (S-04/07) | Strong at decision | Play paused/resumed | [BR-DC-3/4](../02-business-analysis/02-business-rules.md) |
| **(System) Abandon Match** | End unplayable match | Match terminal (S-02) | Strong (CB-03) | Result=abandoned | [BR-GE-5](../02-business-analysis/02-business-rules.md) |

## Queries (read-only)

| Capability | Purpose | Read requirements | Consistency need | Expected result | Related rules |
|-----------|---------|-------------------|------------------|-----------------|---------------|
| **Get Room/Lobby State** | Show membership/setup | Room state (S-01/05) | Read latest committed | Current membership/config | [BR-RC/TA/RO](../02-business-analysis/02-business-rules.md) |
| **Get Game State (role-filtered)** | Render the match | Match/turn/board **filtered by role** (S-02/03/04) | Latest committed; **never unrevealed ownership to non-Spymasters** | Role-appropriate snapshot | [INV-B9](../02-business-analysis/10-business-invariants.md) |
| **Get Board View** | Render board | Words + reveals to all; key to Spymasters (S-03) | Role-filtered | Correct per-role board | [BR-CO-4](../02-business-analysis/02-business-rules.md) |
| **Get Result** | Show outcome | Immutable result (S-10) | Read committed | Winner/reason | [BR-GE-3](../02-business-analysis/02-business-rules.md) |

## Mixed responsibilities (command that also yields read data)

| Capability | Command side | Query-like aspect | Note |
|-----------|--------------|-------------------|------|
| **Submit Guess** | Reveal + outcome (state change) | The resulting reveal/outcome is data participants observe | The **notification** carries the derived read; determinism governed by the command (CB-01). |
| **Reconnect** | Supersede connection (state change) | Returns a role-filtered snapshot (read) | Snapshot is a filtered query bound to the reconnect command (CB-08). |
| **Start Match** | Generate + lock (state change) | Delivers initial role-filtered board | Same commit boundary; board read derived from the command (CB-09). |

## Observations for the architecture (not decisions)
- **Reads are role-dependent projections** of one authoritative state; the *same* state yields
  different query results per role — the filtering boundary is central.
- **All outcome-bearing writes are strongly consistent and serialized per room** (CB-01/02/03).
- **Results are observed via notifications**, not read-back, so command/notification ordering
  matters ([Interactions](08-interaction-discovery.md)).
- Whether to formally separate commands from queries (e.g., any read-model strategy) is an **open
  architectural decision**, listed in [Readiness Review](10-architecture-readiness-review.md) — not
  decided here.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial command/query discovery. |
