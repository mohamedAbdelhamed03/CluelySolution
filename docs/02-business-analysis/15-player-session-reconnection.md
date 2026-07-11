# 16. Player Session & Reconnection Specification — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Specify the complete **business** of temporary player identity: session lifetime, disconnect/reconnect, grace periods, pause rules per role, duplicate connections, and abandonment — with no accounts and no implementation detail. Structured so authentication can attach later without change. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Scope](#1-purpose--scope)
2. [References](#2-references)
3. [Temporary Identity & Reconnect Token](#3-temporary-identity--reconnect-token)
4. [Session Lifetime](#4-session-lifetime)
5. [Disconnect Handling](#5-disconnect-handling)
6. [Grace Period](#6-grace-period)
7. [Role-Specific Pause Rules](#7-role-specific-pause-rules)
8. [Reconnection & Validation](#8-reconnection--validation)
9. [Duplicate Connections & Multiple Devices](#9-duplicate-connections--multiple-devices)
10. [Network Scenarios](#10-network-scenarios)
11. [Abandonment & Session Expiration](#11-abandonment--session-expiration)
12. [Scenario Matrix](#12-scenario-matrix)
13. [Future Authentication Note](#13-future-authentication-note)

---

## 1. Purpose & Scope

Cluely has **no accounts** (BRD §1.6). A player exists only as a **temporary, room-scoped
identity** for the lifetime of their participation. This document specifies how that identity
behaves across drops and returns so that a brief network loss never ends a game or corrupts
state (NFR-4/11). It complements [15 — Lobby & Room Lifecycle](14-lobby-room-lifecycle.md) and
the [Connection state machine §8.4](07-state-machines.md#84-player-connection-state-machine).

## 2. References
- [03 — Business Rules](02-business-rules.md) (BR-JR-7, BR-DC-*, BR-HM-*, BR-EC-12)
- [08 — State Machines §8.4](07-state-machines.md#84-player-connection-state-machine)
- [11 — Business Invariants](10-business-invariants.md) (INV-P1..P5)
- [12 — Domain Events](11-domain-events-catalog.md) (EVT-22..25), [13 — Errors](12-business-error-catalog.md) (RECONNECT_*, DUPLICATE_CONNECTION, GAME_PAUSED)
- [00 — README](../_meta/00-canonical-constants-and-index.md) (grace-period parameters)

## 3. Temporary Identity & Reconnect Token

- **PS-1** On joining, a player receives a **transient, room-scoped identity/reconnect token**
  (BR-JR-7). It contains **no personal data** (INV-P2).
- **PS-2** The token proves continuity: it lets a returning client resume the same player
  (team, role, view) within the grace window.
- **PS-3** The token is valid **only** for the specific room and **only** while that player's
  session is live (until removal or room expiry).
- **PS-4** The token is the sole means of resuming a seat; losing it means rejoining as a new
  player (RECONNECT_TOKEN_INVALID).

## 4. Session Lifetime

A player session spans from join to removal:

```
join ──► Active (Connected) ──drop──► Disconnected(grace) ──reconnect──► Active
                                            │ grace expires
                                            ▼
                                         Removed (seat released)
Active ──leave──► Removed
room expires ──► all sessions Removed
```

- **PS-5** A session ends when the player leaves, is removed after grace expiry, is removed by
  the Host in Lobby, or the room expires.
- **PS-6** A session never outlives its room; there is no cross-room or cross-session identity
  (INV-P2).

## 5. Disconnect Handling

- **PS-7** On connection loss, the player is marked **Disconnected**; they are **not** removed
  immediately (BR-DC-1, INV-P3). Emits `PlayerDisconnected` (EVT-22).
- **PS-8** The grace timer starts (`RECONNECT_GRACE_PERIOD`; Host uses
  `HOST_MIGRATION_GRACE`).
- **PS-9** Whether play pauses depends on the player's role and criticality (§7).
- **PS-10** Other participants see the disconnected status but the match state is otherwise
  preserved (NFR-2/11).

## 6. Grace Period

- **PS-11** The grace period is the window in which a disconnected player may reconnect and
  resume their exact team/role (BR-DC-2).
- **PS-12** Reconnect **within** grace → full resume (INV-P5). Reconnect **after** grace →
  treated as a fresh join for the next match (BR-EC-12, RECONNECT_WINDOW_EXPIRED).
- **PS-13** Grace durations are operational parameters (see [README](../_meta/00-canonical-constants-and-index.md)); changing
  them never affects rules.
- **PS-14** If grace expires and the player was **essential** and irreplaceable, abandonment
  applies (§11).

## 7. Role-Specific Pause Rules

Play pauses only when a **disconnect removes the ability of the active team to proceed**:

| Disconnected player | Effect | Rule |
|---------------------|--------|------|
| **Active team's Spymaster (during clue phase)** | Clue phase **pauses**; no clue can be given until they return or grace expires. Emits `GamePaused` (EVT-24). | BR-DC-3 |
| **Active team's Spymaster (during opponent's turn)** | No immediate effect; may return before their next clue phase. | BR-DC-3 |
| **Active team's sole available Operative (guessing phase)** | Guessing **pauses** until return or grace expiry. | BR-DC-4 |
| **One of several active-team Operatives** | No pause; remaining Operatives continue (team still able to act). | BR-DC-6 |
| **Any waiting-team player** | No pause; their turn isn't active. | BR-DC-6 |
| **Host (any role)** | No play effect; only room control is at risk → host migration after grace. | BR-DC-8, BR-HM |

- **PS-15** Pause **freezes the current phase**; it never changes whose turn it is or any
  card/board state.
- **PS-16** On the awaited player's return, play **resumes** the same phase (`GameResumed`,
  EVT-25).

## 8. Reconnection & Validation

- **PS-17** A reconnect provides the room code + reconnect token (WF-13).
- **PS-18** Validation:
  - Token valid and room-scoped (V-RECON-1) → else `RECONNECT_TOKEN_INVALID` → rejoin new.
  - Within grace and room live (V-RECON-2) → else `RECONNECT_WINDOW_EXPIRED` / `ROOM_CLOSED`.
- **PS-19** On success, the system restores team, role, and **role-appropriate view**
  (Spymaster sees the key; Operative/waiting does not — INV-P5, BR-DC-7) and resends the
  current authoritative state. Emits `PlayerReconnected` (EVT-23).
- **PS-20** Any paused phase resumes (§7).

## 9. Duplicate Connections & Multiple Devices

- **PS-21** A player identity maps to **at most one active connection** at a time (INV-P4).
- **PS-22** If a second connection presents the **same valid token** (e.g., a new tab/device
  or a browser refresh that reopened before the old socket closed), the **newest connection
  supersedes** the older one; the older is dropped. Optionally the client is informed with
  `DUPLICATE_CONNECTION`.
- **PS-23** Two **different** identities may of course play from two devices; the constraint is
  per-identity, not per-device.
- **PS-24** This prevents a single player from acting twice or holding two role-views (fairness,
  INV-P5).

## 10. Network Scenarios

| Scenario | Business handling |
|----------|-------------------|
| **Browser/app refresh** | The client reconnects with the stored token → treated as reconnection (§8); if within grace, seamless resume. |
| **Brief network loss** | Marked Disconnected; within grace → resume with no state change (§5–6). |
| **Long network loss (> grace)** | Removed; returns as a fresh join / waiting member (PS-12). |
| **Switching networks (Wi-Fi ↔ mobile)** | New connection with same token supersedes old (§9). |
| **App backgrounded then resumed** | If the connection dropped, behaves as refresh/reconnect. |
| **Device dies / app closed** | Disconnect → grace → removal if not resumed. |

## 11. Abandonment & Session Expiration

- **PS-25** If grace expires for an **essential, irreplaceable** player (active team left with
  no Spymaster or no Operative), the match cannot continue and ends by **abandonment**
  (BR-DC-5, BR-RX-3). Emits `GameFinished` with reason=abandonment (EVT-21); no play-based
  winner (INV-O1 applies only to play-completed matches; abandonment is distinct — BR-TIE-3).
- **PS-26** After abandonment, the room returns to Lobby (if members remain) or expires
  (WF-15, IR-3).
- **PS-27** A removed session's token is invalidated; the seat is released and may be taken by
  a new join (subject to nickname uniqueness — a returning person may reuse their nickname if
  free).
- **PS-28** All sessions end when the room expires (PS-6).

## 12. Scenario Matrix

| # | Who disconnects | When | Pause? | If returns in grace | If grace expires |
|---|-----------------|------|--------|---------------------|------------------|
| 1 | Active Spymaster | Clue phase | Yes | Resume clue phase | Abandonment (no other SM) |
| 2 | Active Spymaster | Opponent's turn | No | Resume before next clue | Abandonment when their turn arrives |
| 3 | Sole active Operative | Guess phase | Yes | Resume guessing | Abandonment |
| 4 | One of many Operatives | Any | No | Rejoin seamlessly | Team continues; seat released |
| 5 | Waiting-team player | Any | No | Rejoin seamlessly | Seat released |
| 6 | Host | Any | No (play) | Keep Host | Host migrates (or room expires if none left) |
| 7 | All but one | Mid-match | Yes (as essential lost) | If enough return, resume | Abandonment / room expiry |

## 13. Future Authentication Note

- **PS-29** Identity is deliberately abstracted (INV-P2, AUTH-1..5). Today the reconnect token
  is transient and PII-free; a future authentication subsystem could associate it with a
  durable account **without changing** any rule, workflow, pause behaviour, or invariant in
  this document. Reconnection semantics remain identical; only the identity's durability would
  change.
