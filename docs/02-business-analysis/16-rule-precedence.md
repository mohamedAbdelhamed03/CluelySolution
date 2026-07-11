# 17. Rule Precedence Specification — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Guarantee **deterministic** behaviour when multiple rules or conditions could apply to the same moment, by defining a strict precedence order. This removes ambiguity before implementation; the engine must always resolve conflicts identically. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [Global Precedence Ladder](#3-global-precedence-ladder)
4. [Reveal Resolution Order](#4-reveal-resolution-order)
5. [Turn & Match End Conflicts](#5-turn--match-end-conflicts)
6. [Connection vs Play Conflicts](#6-connection-vs-play-conflicts)
7. [Room-Control vs Match Conflicts](#7-room-control-vs-match-conflicts)
8. [Concurrency Resolution](#8-concurrency-resolution)
9. [Conflict Scenarios Catalog](#9-conflict-scenarios-catalog)

---

## 1. Purpose & Usage

Certain events can appear to happen "at the same time" (e.g., a guess that both reveals a
team's last agent **and** could trigger a turn end, or a Host disconnect that coincides with a
match ending). This document defines **which rule wins**, **which are ignored/subsumed**, and
**why**, so outcomes are always deterministic (NFR-2). It introduces no new rules; it orders
existing ones.

## 2. References
- [03 — Business Rules](02-business-rules.md), [08 — State Machines](07-state-machines.md)
- [11 — Business Invariants](10-business-invariants.md) (INV-O2/O3, INV-G7)
- [12 — Domain Events](11-domain-events-catalog.md), [16 — Player Session](15-player-session-reconnection.md)

## 3. Global Precedence Ladder

When multiple conditions are pending in the **same processing step**, resolve strictly
top-down. Higher rows **override** lower rows.

| Rank | Condition | Wins over | Business reasoning | Rule |
|------|-----------|-----------|--------------------|------|
| **1** | **Assassin revealed** | everything below | Instant loss is absolute; nothing can supersede it. | BR-ASN-4, INV-O3 |
| **2** | **Victory (a team's last agent revealed)** | turn end, room/connection effects | A completed win terminates the match immediately. | BR-WIN-1, INV-O2 |
| **3** | **Match end bookkeeping (result recorded, Post-Match)** | turn transitions | Once terminal, no further play. | BR-GE-1/2, INV-G7 |
| **4** | **Turn end** | next-turn start | The current turn must close before the next begins. | BR-TE-*, §8.3 |
| **5** | **Next turn start / pass of play** | routine state broadcast | Only after the prior turn ended and match not finished. | BR-TE-5, BR-TO-2 |
| **6** | **Connection effects (pause/abandonment)** | host migration timing | Play-continuity is evaluated before control changes. | BR-DC-3/4/5 |
| **7** | **Host migration** | room expiration (if a player remains) | Preserve the room before considering closure. | BR-HM-1/2 |
| **8** | **Room expiration** | — | Lowest: only when nothing above keeps the room alive. | BR-RX-* |

Key consequence: **Assassin > Victory > Turn End > Host Migration > Room Expiration**, exactly
as the reference intuition requires.

## 4. Reveal Resolution Order

For a **single** accepted guess, evaluate in this fixed order (each reveal is atomic —
INV-B7, INV-O2):

1. **Reveal the card** (make ownership public) — always first (EVT-18).
2. **If Assassin** → guessing team loses, opponent wins, **match ends** (Rank 1). Stop.
3. **Else update agent counts** for the owning team (own or opponent).
4. **If any team's agents now all revealed** → that team **wins**, **match ends** (Rank 2). Stop.
5. **Else classify the guess:**
   - **Own agent** → correct; if guesses remain and not won → continue (same turn); else turn ends.
   - **Neutral** → turn ends (Rank 4).
   - **Opponent agent** → opponent count already updated in step 3; turn ends (Rank 4).
6. **If turn ends and match not over** → start opponent's turn (Rank 5).

> Note the deliberate ordering of steps 3–4: revealing the **opponent's last** agent (even by
> the active team) updates the opponent's count (step 3) and immediately triggers the
> opponent's victory (step 4) — matching BR-EC-1. Victory checking happens for **whichever**
> team's agents are now complete.

## 5. Turn & Match End Conflicts

| Conflict | Resolution | Reason |
|----------|-----------|--------|
| A guess is both "correct" and "reveals last own agent". | **Victory wins** (Rank 2); the "continue guessing" option is void. | Match end overrides turn continuation (INV-G7). |
| A guess reaches the guess limit **and** wins on the same reveal. | **Victory wins**; limit is moot. | Rank 2 > Rank 4. |
| Voluntary end-turn submitted at nearly the same instant a guess resolves to a win. | The **win** (already-resolved reveal) wins; the end-turn is rejected (`GAME_ALREADY_FINISHED`). | Serialize; terminal state precludes further intents (§8, INV-G7). |
| Assassin reveal coincides with what would have been a win for the other team via counts. | **Assassin (Rank 1)** determines the outcome: guessing team loses, opponent wins. | Assassin is absolute (INV-O3). Note: in practice a single card cannot be both, but rank order guarantees determinism. |

## 6. Connection vs Play Conflicts

| Conflict | Resolution | Reason |
|----------|-----------|--------|
| Essential player disconnects **exactly as** a terminal reveal occurs. | **Match end wins** (Rank 2/3); the disconnect has no effect on an already-finished match. | A finished match cannot pause/abandon (INV-G7). |
| Essential player disconnects during an active phase (no terminal event). | **Pause** (Rank 6) applies; play freezes pending reconnect/grace. | BR-DC-3/4. |
| Grace expires (abandonment) **and** the awaited player reconnects at the boundary. | Reconnection **within** grace wins; abandonment applies only strictly **after** grace. | PS-11/12; boundary resolves in favour of continuity. |
| Non-essential Operative disconnects during guessing. | **No effect** on play (Rank below play); team continues. | BR-DC-6. |

## 7. Room-Control vs Match Conflicts

| Conflict | Resolution | Reason |
|----------|-----------|--------|
| Host leaves/disconnects during an active match. | **Play continues**; only **host migration** (Rank 7) occurs, after grace for disconnects. Match state untouched. | BR-EC-11, HM-6. |
| Host migration would occur but no connected player remains. | **Room expiration** (Rank 8) instead. | BR-HM-3. |
| Room idle-timer elapses mid-turn while players are actually active. | Activity **resets** the idle timer; expiration does not fire while there is activity. | BR-RX-1 (inactivity, not elapsed wall-clock). |
| Match ends and room empties simultaneously. | **Record result** (Rank 3) first, then **expire** (Rank 8). | Result integrity before cleanup (INV-O4). |

## 8. Concurrency Resolution

- **CR-1 Single authority & serialization:** all intents are processed by the authority in a
  **serialized** order; there is no true simultaneity in resolution (SEC-1, BR-EC-13).
- **CR-2 First valid wins:** for near-simultaneous guesses, the **first** accepted guess is
  applied; any later guess is **re-evaluated against the new state** and rejected if no longer
  valid (e.g., card now revealed → `CARD_ALREADY_REVEALED`) (BR-GV-8, INV-B7).
- **CR-3 Terminal freeze:** once a match is terminal, all later play intents are rejected
  (`GAME_ALREADY_FINISHED`), regardless of arrival order (INV-G7).
- **CR-4 Idempotent effects:** re-delivery of an already-applied intent must not double-apply
  (a card reveals once; an agent count decrements once).
- **CR-5 Deterministic tie-breaks:** where a deterministic choice is needed (e.g., host
  migration target), the rule is fixed (longest-present connected player) so the result is
  reproducible (BR-HM-2).

## 9. Conflict Scenarios Catalog

| ID | Scenario | Winning rule | Ignored/subsumed | Rank | Reference |
|----|----------|--------------|------------------|------|-----------|
| RP-1 | Guess reveals assassin. | Guessing team loses; opponent wins; match ends. | Any pending turn/continue logic. | 1 | BR-ASN-* |
| RP-2 | Guess reveals own last agent. | Guessing team wins; match ends. | "Continue guessing", guess limit. | 2 | BR-WIN-1 |
| RP-3 | Active team reveals opponent's last agent. | Opponent wins; match ends. | Turn-end/continue. | 2 | BR-EC-1, BR-OPP-4 |
| RP-4 | Correct guess with guesses remaining, not last agent. | Turn continues (same team). | Turn end. | 4→continue | BR-CG-3 |
| RP-5 | Neutral / opponent guess (not last agent). | Turn ends; pass play. | Continue. | 4→5 | BR-NC-2, BR-OPP-3 |
| RP-6 | Essential disconnect during active phase. | Pause. | Next-turn start. | 6 | BR-DC-3/4 |
| RP-7 | Essential disconnect coincident with match end. | Match end. | Pause/abandonment. | 2/3 | INV-G7 |
| RP-8 | Grace expiry vs boundary reconnect. | Reconnect (if within grace). | Abandonment. | 6 | PS-11/12 |
| RP-9 | Host loss during match. | Host migration (control only). | Any match interruption. | 7 | BR-EC-11 |
| RP-10 | Host loss with no players left. | Room expiration. | Host migration. | 8 | BR-HM-3 |
| RP-11 | Result recorded vs room emptied. | Record result, then expire. | Immediate expiry. | 3→8 | INV-O4, BR-RX-2 |
| RP-12 | Two operatives guess at once. | First valid applies; second re-evaluated. | Second guess (if now invalid). | CR-2 | BR-GV-8 |

> This ladder is **normative**: any implementation must produce identical outcomes for these
> scenarios. Determinism here is what makes the game provably fair (NFR-2, INV-O1).
