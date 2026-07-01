# 10. Validation Rules — Cluely

Every validation, with its **trigger**, **reason**, **expected behavior**, and **business
outcome**. Validation IDs are referenced from [Functional Requirements](04-functional-requirements.md)
and tie back to [Business Rules](03-business-rules.md). All validations are
language-independent.

---

## 10.1 Nickname

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-NICK-1 | Nickname is non-empty (after trim). | A player must be identifiable. | Reject empty nickname; prompt for one. | Join blocked until provided. |
| V-NICK-2 | Nickname within allowed length & character bounds. | Prevent abuse/overflow/display issues. | Reject out-of-bounds nickname with reason. | Join blocked until corrected. |
| V-NICK-3 | Nickname unique within the room (case-insensitive, trimmed). | Avoid in-room confusion (BR-JR-3). | Reject duplicate; prompt for a different one. | Player not added until unique. |

## 10.2 Room access

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-ROOM-1 | Room code corresponds to an existing live room. | Cannot join a non-existent room (BR-JR-2). | Reject with "invalid room code". | Join refused. |
| V-ROOM-2 | Room is not expired/closed. | Expired rooms hold no state (BR-RX). | Reject with "room expired/closed". | Join refused; suggest new room. |
| V-CAP-1 | Room is below max capacity. | Bounded room size (BR-JR-5). | Reject with "room full". | Join refused. |
| V-MEMBER-1 | Actor is a current member for member-only actions. | Only members may act. | Reject non-member intents. | Action ignored. |

## 10.3 Team & role

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-TEAM-1 | Team selection/switch only when no active match. | Switching mid-match corrupts the key/turn (BR-TA-4). | Reject during InMatch. | Team stays as locked at start. |
| V-TEAM-2 | Selected team is one of {Red, Blue}. | Only two valid teams (BR-TA-1). | Reject invalid team. | No assignment change. |
| V-ROLE-1 | Team has at most one Spymaster. | Exactly one Spymaster per team (BR-RO-1/6). | Reject second Spymaster claim. | Claimant stays Operative. |
| V-ROLE-2 | Role changes only when no active match. | Spymaster has seen the key (BR-RO-5). | Reject mid-match role change. | Roles locked for match. |

## 10.4 Match start

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-START-1 | Actor is the Host. | Only Host may start (BR-GS-2). | Reject non-Host start. | Match not started. |
| V-START-2 | Two teams each have exactly 1 Spymaster. | Required composition (BR-GS-1). | Block start; report which team is missing/extra. | Match not started. |
| V-START-3 | Each team has ≥1 Operative and total players ≥4. | Minimum playable game (BR-GS-1). | Block start; report deficiency. | Match not started. |
| V-START-4 | Selected dictionary version has ≥25 usable words. | Need 25 distinct words (BR-GS-3). | Block start; report dictionary issue. | Match not started. |

## 10.5 Board generation

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-DICT-1 | Selected region/dictionary exists in catalog. | Must resolve a real word source. | Reject invalid selection; fall back to default or block. | No board from invalid source. |
| V-DICT-2 | Resolved dictionary version supplies ≥25 distinct words. | Board needs 25 unique words (BR-BG-1/2). | Abort start if insufficient. | Match not started. |
| V-BOARD-1 | Ownership counts equal 9 + 8 + 7 + 1 = 25. | Faithful composition (BR-BG-3). | Reject/regenerate if counts wrong. | Only valid boards play. |

## 10.6 Clue

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-CLUE-1 | Submitter is the active team's Spymaster. | Only active Spymaster clues (BR-CL-1). | Reject otherwise. | No clue recorded. |
| V-CLUE-2 | Turn is in AwaitingClue phase. | Clue only in clue phase (BR-TO-3). | Reject out-of-phase clue. | No clue recorded. |
| V-CLUE-3 | Clue is exactly one word (single token, no spaces). | Codenames clues are one word (BR-CL-2/3). | Reject multi-word clue. | Spymaster re-submits. |
| V-CLUE-4 | Clue word ≠ any **unrevealed** board word (case-insensitive). | Cannot clue a visible board word (BR-CL-4). | Reject; revealed words are allowed (BR-CL-8). | Spymaster re-submits. |
| V-CLUE-5 | Clue number is an integer ≥0 **or** "unlimited". | Valid guess allowance (BR-CL-5). | Reject invalid numbers. | Spymaster re-submits. |

## 10.7 Guess

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-GUESS-1 | Submitter is an Operative of the active team. | Only active Operatives guess (BR-GV-1/4). | Reject Spymaster/other-team/out-of-turn guesses. | No reveal. |
| V-GUESS-2 | Turn is in AwaitingGuess phase with an active clue. | No guessing before a clue (BR-GV-3). | Reject premature guess. | No reveal. |
| V-GUESS-3 | Target card is currently **unrevealed**. | Cannot re-guess revealed cards (BR-GV-2). | Reject guess on revealed card. | No reveal. |
| V-GUESS-4 | Guess count for the turn is within the allowed limit. | Bounded by clue number + 1 (or unbounded for 0/∞) (BR-GV-7). | Reject guess beyond limit; turn ends. | Turn passes to opponent. |
| V-GUESS-5 | Guesses are serialized; first valid wins on conflict. | Avoid double-reveal races (BR-EC-13). | Re-evaluate later guess against new state; reject if invalid. | Single consistent reveal. |

## 10.8 End turn

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-ENDTURN-1 | At least one guess was made this turn. | Must guess ≥1 before stopping (BR-GV-6, BR-TE-6). | Reject end-turn with zero guesses. | Team must guess first. |
| V-ENDTURN-2 | Requester is an Operative of the active team. | Only active team ends its own turn. | Reject otherwise. | Turn unchanged. |

## 10.9 Lifecycle / match-state

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-END-1 | Exactly one winner is recorded per completed match. | No draw; single terminal (BR-WIN-3, BR-TIE). | Enforce single winner at finish. | Consistent result. |
| V-STATE-1 | No clue/guess accepted when Game is Finished. | Match is over (BR-GE-2). | Reject post-match actions ("game already finished"). | No state change. |
| V-STATE-2 | Actions match the current Turn phase. | Phase integrity (8.3). | Reject phase-mismatched intents. | No state change. |

## 10.10 Connection / reconnection

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-CONN-1 | Disconnect marks state without removing the player immediately. | Tolerate transient drops (BR-DC-1). | Start grace timer; pause if essential. | Play resumes on reconnect. |
| V-RECON-1 | Reconnect token is valid and room-scoped. | Prove continuity without PII (SEC-5). | Reject invalid token → rejoin as new player. | Old role not auto-restored. |
| V-RECON-2 | Reconnect occurs within the grace period and room is live. | Late returns are not resumptions (BR-EC-12). | If expired → fresh join; if room closed → fail. | Role restored only within grace. |

## 10.11 Host & room control

| ID | Validation | Reason | Expected behavior | Business outcome |
|----|-----------|--------|-------------------|------------------|
| V-HOST-1 | Host-only actions require the Host. | Protect room control (BR-HOST-1). | Reject non-Host control actions. | No change. |
| V-HOST-2 | Exactly one Host exists after any migration. | Single point of control (BR-HM-4). | Enforce atomic, deterministic migration. | Predictable host. |
| V-EXP-1 | Idle/empty/unplayable rooms expire. | Reclaim resources (BR-RX-1/2). | Close room; release code; notify. | Room no longer live. |

---

## 10.12 Validation summary by intent

| Intent | Validations applied |
|--------|---------------------|
| Create room | V-NICK-1/2, V-DICT-1 |
| Join room | V-ROOM-1/2, V-CAP-1, V-NICK-1/2/3 |
| Select team | V-MEMBER-1, V-TEAM-1/2 |
| Claim role | V-MEMBER-1, V-ROLE-1/2 |
| Start match | V-START-1/2/3/4, V-DICT-2, V-BOARD-1 |
| Submit clue | V-CLUE-1/2/3/4/5, V-STATE-1/2 |
| Submit guess | V-GUESS-1/2/3/4/5, V-STATE-1/2 |
| End turn | V-ENDTURN-1/2, V-STATE-2 |
| Leave / migrate | V-MEMBER-1, V-HOST-2 |
| Reconnect | V-RECON-1/2, V-CONN-1 |
| Room control | V-HOST-1, V-EXP-1 |

Every rejection returns a **specific reason code** so clients can present an actionable
message; no rejection silently alters game state.
