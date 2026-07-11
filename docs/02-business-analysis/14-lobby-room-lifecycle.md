# 15. Lobby & Room Lifecycle Specification — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Specify everything that happens **before, between, and around matches**: the room and lobby lifecycles, waiting/ready states, membership changes, host migration, setup changes, rematch, idle/expiry, capacity, and their edge cases. Consolidates and cross-references existing rules; introduces no new gameplay. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Scope](#1-purpose--scope)
2. [References](#2-references)
3. [Room vs Lobby](#3-room-vs-lobby)
4. [Room Lifecycle](#4-room-lifecycle)
5. [Lobby Lifecycle & Ready State](#5-lobby-lifecycle--ready-state)
6. [Membership Changes](#6-membership-changes)
7. [Host Leaving & Migration](#7-host-leaving--migration)
8. [Setup Changes (Team / Role / Dictionary)](#8-setup-changes)
9. [Rematch Flow](#9-rematch-flow)
10. [Capacity & Waiting Players](#10-capacity--waiting-players)
11. [Idle Rooms & Expiration](#11-idle-rooms--expiration)
12. [Edge Cases](#12-edge-cases)
13. [Lifecycle Diagrams](#13-lifecycle-diagrams)

---

## 1. Purpose & Scope

The pre-match and between-match experience is where most room-management complexity lives.
This document is the single reference for it. Gameplay itself is specified in
[03 — Business Rules](02-business-rules.md), [08 — State Machines](07-state-machines.md), and
[09 — Business Workflows](08-business-workflows.md); this document focuses on the surrounding
room/lobby behaviour and references those where play begins.

## 2. References
- [03 — Business Rules](02-business-rules.md) (BR-RC, BR-JR, BR-LR, BR-HM, BR-TA, BR-RO, BR-GS, BR-RX)
- [08 — State Machines §8.1 Room](07-state-machines.md#81-room-state-machine)
- [09 — Business Workflows](08-business-workflows.md) (WF-01..WF-05, WF-10, WF-12, WF-15)
- [11 — Business Invariants](10-business-invariants.md) (INV-R*, INV-T*), [16 — Player Session](15-player-session-reconnection.md)

## 3. Room vs Lobby

- A **Room** is the persistent private container (identified by a room code) that exists from
  creation to expiration and can host multiple consecutive matches.
- The **Lobby** is the room's **pre-match / between-match phase**, where players gather,
  choose teams and roles, and the Host configures and starts the match.
- Relationship: `Room` has lifecycle states {Lobby, InMatch, PostMatch, Expired} (§8.1). The
  "Lobby" here corresponds to the room in the **Lobby** and **Post-Match→reconfigure** phases.

## 4. Room Lifecycle

States (authoritative in [§8.1](07-state-machines.md#81-room-state-machine)): **Lobby →
InMatch → PostMatch → (Lobby | Expired)**.

| Phase | Entry | What can happen | Exit |
|-------|-------|-----------------|------|
| **Lobby** | Room created (WF-01) or returned from Post-Match | Join/leave, team/role changes, dictionary selection, host actions, start | Start match → InMatch; empty/idle → Expired |
| **InMatch** | Valid start (WF-05) | Play only (setup locked, INV-T5) | Terminal condition → PostMatch; abandonment → PostMatch/Expired |
| **PostMatch** | Match finished (WF-11) | View result, reconfigure, start rematch, leave | Rematch → InMatch; reconfigure → Lobby; empty/idle → Expired |
| **Expired** | Empty/idle/abandoned (WF-15) | — (terminal) | Room closed; code released |

Invariants throughout: exactly one Host (INV-R1), unique live code (INV-R2), capacity bound
(INV-R5), at most one active match (INV-G1).

## 5. Lobby Lifecycle & Ready State

- **LR-1** On entering Lobby, the room shows current membership, each player's team (or
  unassigned), and role (Operative by default).
- **LR-2 Waiting players:** members not yet assigned to a team are **waiting**; they are in the
  room and counted toward capacity but do not participate until assigned and a match starts.
- **LR-3 Ready condition (composition validity):** the room is **ready to start** when both
  teams have exactly one Spymaster and at least one Operative and total players ≥ 4
  (BR-GS-1, INV-T6). This is a **derived** state, continuously recomputed as membership/roles
  change; it is not a per-player "ready" toggle (no such feature exists in the reference game).
- **LR-4** Only the Host can start, and only while the ready condition holds (V-START-*).
- **LR-5** If the ready condition is lost (e.g., the sole Spymaster leaves), the start action
  is unavailable until it is restored.

## 6. Membership Changes

### Joining (WF-02)
- **LM-1** A player joins with a valid code + unique nickname; enters as waiting/Operative
  (BR-JR-1..8). Emits `PlayerJoined` (EVT-2).
- **LM-2** Joining during an in-progress match makes the player a **waiting member** for the
  next match with **no board/card data** (BR-JR-6/6a, INV-B9).

### Leaving (WF-12)
- **LM-3** Any player may leave anytime; they are removed from team/role (BR-LR-1/2). Emits
  `PlayerLeft` (EVT-3).
- **LM-4** If leaving drops a team below minimum **in Lobby**, the room simply becomes
  not-ready (LR-5). If it happens **mid-match**, disconnect/abandonment handling applies
  ([16](15-player-session-reconnection.md), BR-DC-5).
- **LM-5** If the last member leaves, the room expires (BR-LR-6, INV-R4).

### Host removal (Lobby only)
- **LM-6** The Host may remove a player in Lobby as a room-management action (BR-HOST-3);
  emits `PlayerRemovedByHost` (EVT-6). Not available during a match.

## 7. Host Leaving & Migration

- **HM-1** The Host is always a current member (INV-R3).
- **HM-2** If the Host **leaves** (explicit), migration occurs immediately (WF-12 → WF-14b).
- **HM-3** If the Host **disconnects**, migration is deferred until `HOST_MIGRATION_GRACE`
  expires, to tolerate brief drops (BR-DC-8).
- **HM-4** Migration target is **deterministic**: the longest-present connected player
  (BR-HM-2). Emits `HostTransferred` (EVT-5).
- **HM-5** If no connected player remains, the room expires (BR-HM-3, WF-15).
- **HM-6** Migration transfers **only** room-control privileges; it never alters match/turn
  state (BR-EC-11). During a match, play continues uninterrupted.
- **HM-7** After migration there is exactly one Host (INV-R1, V-HOST-2).

## 8. Setup Changes

Allowed **only** while no match is in progress (Lobby / Post-Match→reconfigure):

- **SC-1 Team change:** a player selects/switches Red/Blue (WF-03, BR-TA-3). Emits
  `TeamChanged` (EVT-8). Blocked mid-match (BR-TA-4 → `TEAM_CHANGE_NOT_ALLOWED`).
- **SC-2 Role change:** a player claims/releases Spymaster (WF-04, BR-RO-4). Emits
  `RoleChanged` (EVT-9). At most one Spymaster per team (INV-T3). Blocked mid-match
  (BR-RO-5 → `ROLE_CHANGE_NOT_ALLOWED`).
- **SC-3 Dictionary change:** the Host selects a region (F-16, BR-RC-4). Emits
  `DictionarySelected` (EVT-10). Applies to the next match only; locked at start
  (DM-S2/S4).
- **SC-4** All setup changes recompute the ready condition (LR-3).

## 9. Rematch Flow

- **RF-1** When a match finishes, the room enters **Post-Match** (BR-GE-4) and the result is
  shown (EVT-21).
- **RF-2** From Post-Match, players may reconfigure teams/roles (returning to Lobby semantics,
  BR-TA-5) and the Host may start a new match (BR-HOST-2).
- **RF-3** A rematch generates a **fresh** board/key/starting team (WF-05/WF-06); nothing from
  the prior match carries over except membership and any chosen new setup.
- **RF-4** Rematch requires the ready condition again (V-START-*); if players left, setup must
  be fixed first.
- **RF-5** Each match is independent; there is no cumulative score, series, or ranking (out of
  scope, BRD §1.6).

## 10. Capacity & Waiting Players

- **CW-1** Room membership never exceeds `ROOM_MAX_PLAYERS` (INV-R5, V-CAP-1). Excess joins
  are rejected with `ROOM_FULL`.
- **CW-2** Waiting players (unassigned, or joined mid-match) count toward capacity.
- **CW-3** A team may have many Operatives; only the role minimums are enforced (BR-TA-7).
  There is no maximum team size beyond room capacity.
- **CW-4** Waiting members receive lobby/membership updates only; during an in-progress match
  they receive **no** board/card data (BR-JR-6a, INV-B9).

## 11. Idle Rooms & Expiration

- **IR-1** A room expires after `ROOM_IDLE_EXPIRY` of inactivity (no intents/connections)
  (BR-RX-1). Emits `RoomExpired` (EVT-4) then `RoomClosed` (EVT-7).
- **IR-2** A room expires immediately when it becomes empty (BR-RX-2, INV-R4).
- **IR-3** A mid-match that becomes unplayable after grace ends by **abandonment**
  (BR-RX-3, BR-DC-5); the room then expires or returns to Lobby per remaining membership.
- **IR-4** On expiry, transient state is discarded and the room code is released for reuse
  (BR-RX-4, INV-R2). Still-connected participants are notified (BR-RX-5).
- **IR-5** Any intent arriving after closure is rejected with `ROOM_CLOSED`/`ROOM_EXPIRED`.

## 12. Edge Cases

| ID | Situation | Handling |
|----|-----------|----------|
| LEC-1 | Host disconnects briefly then returns within grace. | No migration; Host resumes (HM-3, [16](15-player-session-reconnection.md)). |
| LEC-2 | Sole Spymaster leaves in Lobby. | Room becomes not-ready; another player must claim Spymaster (LR-5). |
| LEC-3 | Two players claim Spymaster at once. | Exactly one succeeds; other gets `ROLE_ALREADY_TAKEN` (INV-T3, BR-RO-4/6). |
| LEC-4 | Player joins mid-match. | Waiting member, no board data (LM-2, BR-JR-6a). |
| LEC-5 | All but one player leave mid-match. | Abandonment after grace (IR-3, BR-DC-5). |
| LEC-6 | Host leaves during a match. | Migration of control only; play continues (HM-6, BR-EC-11). |
| LEC-7 | Room empties exactly as a match ends. | Room expires from Post-Match (IR-2). |
| LEC-8 | Duplicate nickname on join. | Rejected with `DUPLICATE_NICKNAME`; prompt to change (INV-P1). |
| LEC-9 | Dictionary changed then a player leaves, breaking readiness. | Start remains blocked until composition restored (SC-4, LR-3). |
| LEC-10 | Rematch requested but a team now lacks a Spymaster. | Blocked with `MATCH_CONFIGURATION_INVALID` until fixed (RF-4). |
| LEC-11 | Idle timer elapses during Post-Match. | Room expires (IR-1). |
| LEC-12 | Last connected player is a disconnected Host awaiting grace, everyone else left. | Room expires (HM-5, IR-2). |

## 13. Lifecycle Diagrams

### 13.1 Room lifecycle (with lobby detail)

```
                 ┌────────────────────────── Lobby ──────────────────────────┐
                 │  join / leave / team / role / dictionary / host actions    │
   create ──────►│  ready condition = (2 teams, each 1 SM + ≥1 OP, ≥4)        │
                 └───────┬───────────────────────────────────────────┬────────┘
                         │ start (Host, ready)                        │ empty / idle
                         ▼                                            ▼
                    ┌─────────┐   finish / assassin / abandon    ┌─────────┐
                    │ InMatch │─────────────────────────────────►│ Expired │
                    └────┬────┘                                  └─────────┘
                         │ match ends                                 ▲
                         ▼                                            │
                   ┌───────────┐   reconfigure ──► Lobby              │ empty / idle
                   │ PostMatch │──────────────────────────────────────┘
                   └─────┬─────┘
                         │ rematch (Host, ready)
                         └──────────────► InMatch
```

### 13.2 Host migration flow

```
Host leaves (explicit) ─────────────► migrate now
Host disconnects ──► wait HOST_MIGRATION_GRACE ──► returned? ──yes──► keep Host
                                                     │ no
                                                     ▼
                                     connected player exists? ──yes──► migrate to longest-present
                                                     │ no
                                                     ▼
                                                Room expires
```

### 13.3 Join decision

```
join(code, nickname)
  code live? ─no─► ROOM_NOT_FOUND / ROOM_EXPIRED
  room full? ─yes─► ROOM_FULL
  nickname unique? ─no─► DUPLICATE_NICKNAME
  match in progress? ─yes─► admit as WAITING member (no board data)
                     └no──► admit to Lobby (Operative, unassigned)
```
