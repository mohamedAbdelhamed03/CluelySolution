# 8. State Machines — Cluely

Complete lifecycle states and transitions for every stateful entity. Each transition lists
its **trigger**, **guard/condition**, and **resulting state**, with the governing rule from
[Business Rules](03-business-rules.md). States are exhaustive; no transition is omitted.

---

## 8.1 Room state machine

**States:** `Lobby`, `InMatch`, `PostMatch`, `Expired`.

```
            create                      start match (valid)
   (none) ─────────► Lobby ───────────────────────────────► InMatch
                       ▲  │                                     │
        new match      │  │ all members leave / idle            │ match ends
   (Host) ─────────────┘  │                                     ▼
                          │                                  PostMatch
                          │                                     │  │
                          └─────────────────────────────────────┘  │ all leave / idle / abandon
                                  start new match (valid)           ▼
                                                                 Expired
```

| From | Trigger | Guard | To | Rule |
|------|---------|-------|----|------|
| (none) | Room created | — | Lobby | BR-RC-5 |
| Lobby | Host starts match | Valid composition + dictionary | InMatch | BR-GS-1..5 |
| Lobby | All members leave / idle timeout | — | Expired | BR-RX-1/2, BR-LR-6 |
| InMatch | Match reaches win/loss | Terminal condition | PostMatch | BR-GE-1/4 |
| InMatch | Unplayable after grace (abandonment) | Team below minimum | PostMatch (abandoned) or Expired | BR-DC-5, BR-RX-3 |
| PostMatch | Host starts new match | Valid composition | InMatch | BR-GS-*, BR-HOST-2 |
| PostMatch | Players reconfigure | — | Lobby | BR-TA-5 |
| PostMatch | All leave / idle | — | Expired | BR-RX-1/2 |
| Lobby/InMatch/PostMatch | Inactivity threshold | No activity | Expired | BR-RX-1 |

Terminal state: `Expired` (room closed; code released).

---

## 8.2 Game (Match) state machine

**States:** `NotStarted`, `InProgress`, `Finished` with sub-results `{WonByRed, WonByBlue, Abandoned}`.

```
   NotStarted ──start──► InProgress ──terminal condition──► Finished
                              │
                              └── abandonment ─────────────► Finished (Abandoned)
```

| From | Trigger | Guard | To | Rule |
|------|---------|-------|----|------|
| NotStarted | Board generated | Valid start | InProgress | BR-GS-4 |
| InProgress | Team reveals its last agent | — | Finished (that team wins) | BR-WIN-1 |
| InProgress | Active team reveals assassin | — | Finished (opponent wins) | BR-ASN-2/3 |
| InProgress | Active team reveals opponent's last agent | — | Finished (opponent wins) | BR-OPP-4, BR-EC-1 |
| InProgress | Abandonment (composition lost after grace) | Cannot continue | Finished (Abandoned) | BR-GE-5, BR-DC-5 |

Notes: win/loss is evaluated **after every reveal** (F-11). No draw state exists (BR-TIE-*).

---

## 8.3 Turn state machine

**States:** `AwaitingClue`, `AwaitingGuess`, `TurnEnded`. (Plus transient `Paused` overlay
during disconnects.)

```
   AwaitingClue ──valid clue──► AwaitingGuess ──turn-ending event──► TurnEnded
        ▲                            │  ▲                                  │
        │                            │  └── correct guess, guesses remain  │
        │                            └─────────────────────────────────────┘
        │                                                                   │
        └────────────── next team's turn begins ◄──────────────────────────┘
```

| From | Trigger | Guard | To | Rule |
|------|---------|-------|----|------|
| AwaitingClue | Spymaster submits valid clue | Active team's Spymaster | AwaitingGuess | BR-CL-1, BR-TO-3 |
| AwaitingGuess | Correct (own-agent) guess | Guesses remain, not last agent | AwaitingGuess | BR-CG-3 |
| AwaitingGuess | Correct guess reveals last own agent | — | TurnEnded → Match Finished | BR-CG-5, BR-WIN-1 |
| AwaitingGuess | Neutral guess | — | TurnEnded | BR-NC-2 |
| AwaitingGuess | Opponent-agent guess | — | TurnEnded (or opponent wins) | BR-OPP-3/4 |
| AwaitingGuess | Assassin guess | — | TurnEnded → Match Finished (loss) | BR-ASN-2 |
| AwaitingGuess | Guess limit reached | — | TurnEnded | BR-TE-2, BR-GV-7 |
| AwaitingGuess | Voluntary end | ≥1 guess made | TurnEnded | BR-TE-3, BR-GV-6 |
| TurnEnded | Pass play | Match not finished | AwaitingClue (opponent) | BR-TE-5, BR-TO-2 |
| AwaitingClue/AwaitingGuess | Essential player disconnects | — | Paused (same phase) | BR-DC-3/4 |
| Paused | Reconnect within grace | — | resume prior phase | BR-DC-2 |
| Paused | Grace expires, unplayable | — | TurnEnded → abandonment | BR-DC-5 |

---

## 8.4 Player Connection state machine

**States:** `Connected`, `Disconnected`, `Reconnected`(→Connected), `Removed`.

```
   Connected ──drop──► Disconnected ──reconnect (within grace)──► Connected
                            │
                            └── grace expires ──► Removed
   Connected ──leave──────────────────────────► Removed
```

| From | Trigger | Guard | To | Rule |
|------|---------|-------|----|------|
| (join) | Player joins | — | Connected | BR-JR-7 |
| Connected | Connection lost | — | Disconnected (grace timer starts) | BR-DC-1 |
| Disconnected | Reconnect with valid token | Within grace, room live | Connected (role restored) | BR-DC-2/7 |
| Disconnected | Grace expires | — | Removed | BR-DC-5, BR-EC-12 |
| Connected/Disconnected | Player leaves | — | Removed | BR-LR-1/2 |
| Connected (Host) | Host disconnects, grace expires | — | Removed → host migration | BR-HM-1, BR-DC-8 |

Effects: a `Disconnected` essential player pauses the active phase (8.3); `Removed`
triggers team-composition re-check and possible abandonment.

---

## 8.5 Round state machine

**States:** `Open`, `Complete`.

```
   Open ──both teams have taken their turn──► Complete ──► (next Round Open)
        └── match ends mid-round ──► Complete (final)
```

| From | Trigger | Guard | To | Rule |
|------|---------|-------|----|------|
| (game start) | First turn begins | — | Open | BR-TO-1 |
| Open | First team's turn ends, second begins | Match not finished | Open (still) | BR-TO-2 |
| Open | Both teams' turns ended | Match not finished | Complete → next Round Open | — |
| Open | Match reaches terminal condition | — | Complete (final) | BR-GE-1 |

A Round is a bookkeeping pairing of two turns; it never gates play independently of the
Turn machine.

---

## 8.6 WordCard reveal state (supporting)

**States:** `Unrevealed`, `Revealed`.

| From | Trigger | Guard | To | Rule |
|------|---------|-------|----|------|
| Unrevealed | Guessed | Valid guess | Revealed (ownership public) | BR-GV-5, BR-CO-3 |
| Revealed | — | — | (terminal; cannot revert/guess again) | BR-GV-2 |

---

## 8.7 Consistency notes

- Match-end transitions (8.2) **preempt** turn transitions (8.3): the instant a terminal
  reveal occurs, the Turn closes and the Game becomes Finished in the same step.
- The `Paused` overlay (8.3) does not change whose turn it is; it only blocks the dependent
  action until reconnection or grace expiry.
- Host migration (8.4) affects only room-control privileges, never the Turn/Game state
  (BR-EC-11).
