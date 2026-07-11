# 12. Domain Events Catalog — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Catalog every meaningful **business** event the system produces, so that state changes are observable, consistent, and traceable. Events describe *what happened in the business*, not infrastructure/transport concerns. |
| **Technology** | Neutral (no message brokers, protocols, schemas, or code). |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [Conventions](#3-conventions)
4. [Room & Lobby Events](#4-room--lobby-events)
5. [Setup Events (Team / Role / Dictionary)](#5-setup-events)
6. [Match & Board Events](#6-match--board-events)
7. [Turn & Play Events](#7-turn--play-events)
8. [Outcome Events](#8-outcome-events)
9. [Connection Events](#9-connection-events)
10. [Event Sequence Overview](#10-event-sequence-overview)

---

## 1. Purpose & Usage

Every business action that changes state emits one or more **domain events**. Events are the
authoritative record of "what happened" and drive client updates, result recording, and
lifecycle handling. This catalog is a **business** artifact: payloads are described as
business fields, not wire formats. Each event maps to the workflows in
[09 — Business Workflows](08-business-workflows.md) and rules in
[03 — Business Rules](02-business-rules.md).

## 2. References
- [03 — Business Rules](02-business-rules.md), [04 — Functional Requirements](03-functional-requirements.md)
- [07 — Domain Model](06-domain-model.md), [08 — State Machines](07-state-machines.md), [09 — Business Workflows](08-business-workflows.md)
- [11 — Business Invariants](10-business-invariants.md)

## 3. Conventions

- **Event ID:** `EVT-<n>`; **Name:** past-tense business fact.
- **Publisher:** the business component that owns the state change (per [SRS §2.15](01-software-requirements.md#215-logical-architecture-system-perspective)): Room Service, Lobby/Setup, Game Engine, Connection Manager.
- **Consumers:** business roles/components reacting to the event. "All participants (role-filtered)" means the Real-time Delivery layer applies role visibility (Operatives never receive unrevealed ownership — INV-B9).
- **Payload:** business fields only. Card ownership appears in a payload **only when revealed** or **only to Spymasters** (explicitly noted).
- Events are **facts**: they are emitted only after the corresponding state change is committed by the authority.

---

## 4. Room & Lobby Events

### EVT-1 RoomCreated
- **Description:** A new private room was created.
- **Trigger:** Host completes room creation (WF-01).
- **Publisher:** Room Service.
- **Consumers:** Host client; room registry.
- **Payload:** room code, room state (Lobby), host reference (nickname), selected dictionary region, capacity.
- **Preconditions:** None (no auth).
- **Postconditions:** Live room in Lobby with Host as sole member (INV-R1).

### EVT-2 PlayerJoined
- **Description:** A player joined the room.
- **Trigger:** Valid join (WF-02).
- **Publisher:** Room Service.
- **Consumers:** All participants (membership view).
- **Payload:** player reference (nickname), current team (unassigned), role (Operative default), room member list.
- **Preconditions:** Room live, not full, nickname unique (V-ROOM-*, V-CAP-1, V-NICK-3).
- **Postconditions:** Player is a member; nickname uniqueness holds (INV-P1).

### EVT-3 PlayerLeft
- **Description:** A player left the room.
- **Trigger:** Leave intent or removal (WF-12).
- **Publisher:** Room Service.
- **Consumers:** All participants.
- **Payload:** player reference, reason (left / removed / grace-expired), updated member list.
- **Preconditions:** Player was a member.
- **Postconditions:** Player removed from team/role; may trigger EVT-5 or EVT-4.

### EVT-4 RoomExpired
- **Description:** A room expired (idle, empty, or abandoned).
- **Trigger:** Expiry condition (WF-15).
- **Publisher:** Room Service.
- **Consumers:** Any still-connected participants.
- **Payload:** room code, reason (idle / empty / abandoned), timestamp.
- **Preconditions:** Room live.
- **Postconditions:** Room not live; code released (INV-R4, BR-RX-4).

### EVT-5 HostTransferred
- **Description:** Host privileges moved to another player (host migration).
- **Trigger:** Host leaves/disconnects beyond grace (WF-14b).
- **Publisher:** Room Service.
- **Consumers:** All participants.
- **Payload:** previous host reference, new host reference, reason.
- **Preconditions:** At least one connected player remains (else EVT-4).
- **Postconditions:** Exactly one Host (INV-R1, BR-HM-4).

### EVT-6 PlayerRemovedByHost
- **Description:** The Host removed a player in Lobby (room-management).
- **Trigger:** Host kick (BR-HOST-3), Lobby only.
- **Publisher:** Room Service.
- **Consumers:** All participants; removed player.
- **Payload:** removed player reference, host reference.
- **Preconditions:** Room in Lobby; requester is Host (V-HOST-1).
- **Postconditions:** Player removed; equivalent to EVT-3 with reason=removed.

### EVT-7 RoomClosed
- **Description:** The room was closed and its transient state discarded.
- **Trigger:** Terminal cleanup after EVT-4 (WF-15).
- **Publisher:** Room Service.
- **Consumers:** Room registry.
- **Payload:** room code, final reason.
- **Preconditions:** Room expired.
- **Postconditions:** Code available for reuse (BR-RX-4).

---

## 5. Setup Events

### EVT-8 TeamChanged
- **Description:** A player selected or switched teams.
- **Trigger:** Team selection in Lobby/Post-Match (WF-03).
- **Publisher:** Lobby/Setup Service.
- **Consumers:** All participants.
- **Payload:** player reference, previous team, new team.
- **Preconditions:** No active match (V-TEAM-1); valid team (V-TEAM-2).
- **Postconditions:** Player on exactly one team (INV-T2).

### EVT-9 RoleChanged
- **Description:** A player claimed or released a role (Spymaster/Operative).
- **Trigger:** Role claim/release (WF-04).
- **Publisher:** Lobby/Setup Service.
- **Consumers:** All participants.
- **Payload:** player reference, team, previous role, new role.
- **Preconditions:** No active match; team has no existing Spymaster when claiming (V-ROLE-1/2).
- **Postconditions:** ≤1 Spymaster per team (INV-T3).

### EVT-10 DictionarySelected
- **Description:** The Host selected the room's regional dictionary.
- **Trigger:** Dictionary selection (WF-05 setup / F-16).
- **Publisher:** Lobby/Setup Service.
- **Consumers:** All participants.
- **Payload:** region, dictionary version reference.
- **Preconditions:** Room in Lobby; dictionary exists (V-DICT-1).
- **Postconditions:** Room's DictionarySelection recorded (BR-RC-4).

---

## 6. Match & Board Events

### EVT-11 GameStarted
- **Description:** A match started.
- **Trigger:** Host starts a valid match (WF-05).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** match reference, participating teams/roles snapshot, dictionary version, room state → In-Match.
- **Preconditions:** Valid composition + dictionary (V-START-1..4).
- **Postconditions:** Match In-Progress; setup locked (INV-T5, BR-GS-4).

### EVT-12 BoardGenerated
- **Description:** The 25-card board and key were generated.
- **Trigger:** Board generation within start (WF-06).
- **Publisher:** Game Engine.
- **Consumers:** All participants (role-filtered): **words** to everyone; **key** to Spymasters only.
- **Payload:** 25 words with positions (all unrevealed); ownership map delivered **only to Spymasters** (INV-B9).
- **Preconditions:** ≥25 distinct words (V-DICT-2, INV-D2).
- **Postconditions:** Immutable board+key (INV-B1..B8).

### EVT-13 StartingTeamSelected
- **Description:** The starting team (holding 9 agents) was chosen.
- **Trigger:** Board generation (WF-06).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** starting team (Red/Blue), agent counts (9 vs 8).
- **Preconditions:** Board generated.
- **Postconditions:** Turn order fixed; first turn assigned (BR-BG-4, BR-TO-1).

---

## 7. Turn & Play Events

### EVT-14 TurnStarted
- **Description:** A team's turn began (clue phase).
- **Trigger:** Match start or previous turn ended (WF-05/WF-09).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** active team, turn number, phase = AwaitingClue.
- **Preconditions:** Match In-Progress; no other active turn (INV-G2).
- **Postconditions:** Exactly one active turn.

### EVT-15 RoundStarted
- **Description:** A new round (pair of turns) opened.
- **Trigger:** Start of the starting team's turn in a new round (WF, §8.5).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** round number, opening team.
- **Preconditions:** Match In-Progress.
- **Postconditions:** Round in Open state.

### EVT-16 ClueSubmitted
- **Description:** The active Spymaster submitted a clue.
- **Trigger:** Valid clue (WF-07).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** clue word, clue number (or "unlimited"), derived guess allowance, active team.
- **Preconditions:** Turn = AwaitingClue; active Spymaster; structural validity (V-CLUE-1..5).
- **Postconditions:** Exactly one active clue; phase → AwaitingGuess (INV-G3).

### EVT-17 GuessSubmitted
- **Description:** An active Operative submitted a guess (intent accepted for processing).
- **Trigger:** Valid guess (WF-08).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** guessing player reference, targeted card position, active team.
- **Preconditions:** Turn = AwaitingGuess; active clue; active-team Operative; card unrevealed; within limit (V-GUESS-1..5).
- **Postconditions:** Leads to exactly one EVT-18 (serialized — INV-G6, BR-EC-13).

### EVT-18 CardRevealed
- **Description:** A guessed card's ownership became public.
- **Trigger:** A guess is processed (WF-08).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** card position, word, **now-public ownership** (Red/Blue/Neutral/Assassin), updated remaining agent counts.
- **Preconditions:** Valid guess accepted.
- **Postconditions:** Card permanently revealed (INV-B7); triggers outcome evaluation (EVT-19/20/21/22 or continued play).

### EVT-19 TurnEnded
- **Description:** The active team's turn ended.
- **Trigger:** Incorrect guess, limit reached, or voluntary stop (WF-08/WF-09).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** ended team, reason (neutral / opponent-card / limit / voluntary / assassin / win), guesses used.
- **Preconditions:** ≥1 guess made if voluntary (INV-G5).
- **Postconditions:** If match not finished → EVT-14 for opponent (BR-TE-5).

### EVT-20 RoundFinished
- **Description:** Both teams completed their turns for the round (or the match ended mid-round).
- **Trigger:** Second team's turn ends, or terminal condition (§8.5).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** round number, final round status.
- **Preconditions:** Round was Open.
- **Postconditions:** Round Complete; next round may open.

---

## 8. Outcome Events

### EVT-21 GameFinished
- **Description:** The match reached a terminal condition.
- **Trigger:** All-agents win, assassin loss, or abandonment (WF-11/WF-15).
- **Publisher:** Game Engine.
- **Consumers:** All participants.
- **Payload:** winning team, losing team, reason (all-agents / assassin / abandonment), final board (full key may be revealed), room state → Post-Match.
- **Preconditions:** Terminal condition met (INV-O2).
- **Postconditions:** Result recorded and immutable (INV-O1/O4); no further play (INV-G7).

---

## 9. Connection Events

### EVT-22 PlayerDisconnected
- **Description:** A player's connection was lost.
- **Trigger:** Connection loss detected (WF-14a).
- **Publisher:** Connection Manager.
- **Consumers:** All participants.
- **Payload:** player reference, role/team, whether essential to the active phase, grace deadline.
- **Preconditions:** Player was Connected.
- **Postconditions:** Player marked Disconnected; may pause the active phase (BR-DC-3/4, INV-P3).

### EVT-23 PlayerReconnected
- **Description:** A disconnected player returned within the grace period.
- **Trigger:** Valid reconnect (WF-13).
- **Publisher:** Connection Manager.
- **Consumers:** All participants; reconnecting player (full role-appropriate state resent).
- **Payload:** player reference, restored team/role, resumed phase.
- **Preconditions:** Valid token; within grace; room live (V-RECON-1/2).
- **Postconditions:** Player Connected; paused phase resumes; role view restored (INV-P5).

### EVT-24 GamePaused / EVT-25 GameResumed *(phase-level)*
- **EVT-24 GamePaused** — **Trigger:** an essential player (active Spymaster / sole active Operative / Host mid-critical) disconnects (BR-DC-3/4). **Publisher:** Game Engine (on Connection Manager signal). **Payload:** paused phase, reason, awaited player. **Postcondition:** dependent intents blocked until resume or grace expiry.
- **EVT-25 GameResumed** — **Trigger:** the awaited player reconnects (EVT-23) or a permitted substitute resolves the block. **Payload:** resumed phase. **Postcondition:** play continues from the same phase (does not change whose turn it is).

> If grace expires without resolution, the match resolves by abandonment → EVT-21
> (reason=abandonment) and possibly EVT-4/EVT-7.

---

## 10. Event Sequence Overview

Happy-path emission order for one match:

```
RoomCreated
  → PlayerJoined ×N
  → (TeamChanged | RoleChanged | DictionarySelected)*
  → GameStarted → BoardGenerated → StartingTeamSelected
  → RoundStarted → TurnStarted
      → ClueSubmitted → (GuessSubmitted → CardRevealed)+ → TurnEnded
      → TurnStarted (opponent) → ...
  → [RoundFinished → RoundStarted → ...]*
  → GameFinished
  → (TeamChanged/RoleChanged for rematch → GameStarted ...)  |  (PlayerLeft* → RoomExpired → RoomClosed)
```

Resilience events (`PlayerDisconnected`, `GamePaused`, `PlayerReconnected`, `GameResumed`,
`HostTransferred`) may interleave at any point without violating the invariants in
[11 — Business Invariants](10-business-invariants.md).
