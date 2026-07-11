# 13. Business Error Catalog — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Centralize every **business** error the system can return, so that all rejections are consistent, named, and traceable. This becomes the authoritative error reference for client messaging and future API contracts. |
| **Technology** | Neutral (no HTTP status codes, exceptions, or code). |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [Conventions](#3-conventions)
4. [Room & Access Errors](#4-room--access-errors)
5. [Player & Nickname Errors](#5-player--nickname-errors)
6. [Setup (Team/Role/Dictionary) Errors](#6-setup-errors)
7. [Match Start Errors](#7-match-start-errors)
8. [Clue Errors](#8-clue-errors)
9. [Guess & Turn Errors](#9-guess--turn-errors)
10. [Match State Errors](#10-match-state-errors)
11. [Connection & Reconnection Errors](#11-connection--reconnection-errors)
12. [Error → Validation → Rule Traceability](#12-traceability)

---

## 1. Purpose & Usage

Every rejected intent returns exactly one **error code** from this catalog. Errors are
**business** outcomes (the intent was not allowed), not infrastructure failures. Each maps to
a validation in [10 — Validation Rules](09-validation-rules.md) and a rule in
[03 — Business Rules](02-business-rules.md). No rejection silently changes game state.

## 2. References
- [03 — Business Rules](02-business-rules.md), [10 — Validation Rules](09-validation-rules.md)
- [04 — Functional Requirements](03-functional-requirements.md), [11 — Business Invariants](10-business-invariants.md)

## 3. Conventions

- **Error Code:** stable `UPPER_SNAKE_CASE`.
- **Business Meaning:** what the rejection means in domain terms.
- **Cause:** the condition that triggers it.
- **Expected User Message:** neutral, language-independent guidance (final wording is localized by the client; the message text here is illustrative English).
- **Suggested Recovery:** the action that resolves it.
- **Related:** validation ID + business rule.

---

## 4. Room & Access Errors

### ROOM_NOT_FOUND
- **Meaning:** No live room matches the provided code.
- **Cause:** Code never existed or refers to a closed room.
- **User Message:** "We couldn't find a room with that code."
- **Recovery:** Re-check the code or ask the host for a new one.
- **Related:** V-ROOM-1, BR-JR-2.

### INVALID_ROOM_CODE
- **Meaning:** The code is malformed (wrong length/characters).
- **Cause:** Input doesn't match the room-code format.
- **User Message:** "That room code doesn't look right."
- **Recovery:** Re-enter the code.
- **Related:** V-ROOM-1, BR-RC-2.

### ROOM_EXPIRED
- **Meaning:** The room existed but has expired/closed.
- **Cause:** Idle timeout, empty, or abandonment.
- **User Message:** "This room has closed."
- **Recovery:** Create or join a new room.
- **Related:** V-ROOM-2, BR-RX-1/2.

### ROOM_FULL
- **Meaning:** The room is at maximum capacity.
- **Cause:** Member count = `ROOM_MAX_PLAYERS`.
- **User Message:** "This room is full."
- **Recovery:** Wait for a slot or use another room.
- **Related:** V-CAP-1, BR-JR-5.

### GAME_IN_PROGRESS_CANNOT_JOIN
- **Meaning:** Cannot join an active match as a participant.
- **Cause:** Match is In-Progress; joiners wait for the next match.
- **User Message:** "A match is in progress — you'll join the next one."
- **Recovery:** Wait as a waiting member (no board data shown — BR-JR-6a).
- **Related:** BR-JR-6/6a.

### ROOM_CLOSED
- **Meaning:** The room closed during the operation.
- **Cause:** Expiry/host-loss concurrent with an intent.
- **User Message:** "This room is no longer available."
- **Recovery:** Start/join a new room.
- **Related:** BR-RX-*, BR-HM-3.

---

## 5. Player & Nickname Errors

### NICKNAME_REQUIRED
- **Meaning:** No nickname was provided.
- **Cause:** Empty/whitespace nickname.
- **User Message:** "Please enter a nickname."
- **Recovery:** Provide a non-empty nickname.
- **Related:** V-NICK-1, BR-JR-4.

### NICKNAME_INVALID
- **Meaning:** Nickname violates length/character bounds.
- **Cause:** Too short/long or disallowed characters.
- **User Message:** "Please choose a nickname between the allowed length."
- **Recovery:** Adjust the nickname.
- **Related:** V-NICK-2, BR-JR-4.

### DUPLICATE_NICKNAME
- **Meaning:** Nickname already in use in this room.
- **Cause:** Case-insensitive match with an existing member.
- **User Message:** "That nickname is taken in this room."
- **Recovery:** Choose a different nickname.
- **Related:** V-NICK-3, BR-JR-3. *(Synonym of a would-be `PLAYER_ALREADY_EXISTS`; this catalog uses DUPLICATE_NICKNAME as the canonical code.)*

### NOT_A_MEMBER
- **Meaning:** Actor is not a member of the room for a member-only action.
- **Cause:** Stale/foreign identity.
- **User Message:** "You're not part of this room."
- **Recovery:** Rejoin with the room code.
- **Related:** V-MEMBER-1.

---

## 6. Setup Errors

### TEAM_INVALID
- **Meaning:** Selected team is not Red or Blue.
- **Cause:** Invalid team value.
- **User Message:** "Please pick a valid team."
- **Recovery:** Select Red or Blue.
- **Related:** V-TEAM-2, BR-TA-1.

### TEAM_CHANGE_NOT_ALLOWED
- **Meaning:** Team switching is not allowed right now.
- **Cause:** A match is in progress.
- **User Message:** "You can't switch teams during a match."
- **Recovery:** Wait until the match ends.
- **Related:** V-TEAM-1, BR-TA-4.

### TEAM_NOT_SELECTED
- **Meaning:** An action requires the player to be on a team first.
- **Cause:** Claiming a role or starting while unassigned.
- **User Message:** "Join a team first."
- **Recovery:** Select a team.
- **Related:** BR-RO (precondition), V-START-2/3.

### ROLE_ALREADY_TAKEN
- **Meaning:** The team already has a Spymaster.
- **Cause:** A second Spymaster claim.
- **User Message:** "This team already has a Spymaster."
- **Recovery:** Stay as Operative or ask the current Spymaster to release it.
- **Related:** V-ROLE-1, BR-RO-1/6, INV-T3.

### ROLE_CHANGE_NOT_ALLOWED
- **Meaning:** Role changes are locked.
- **Cause:** A match is in progress.
- **User Message:** "Roles are locked during a match."
- **Recovery:** Wait until the match ends.
- **Related:** V-ROLE-2, BR-RO-5.

### DICTIONARY_NOT_FOUND
- **Meaning:** Selected dictionary/region does not exist.
- **Cause:** Invalid region selection.
- **User Message:** "That word set isn't available."
- **Recovery:** Choose an available region.
- **Related:** V-DICT-1, BR-RC-4.

---

## 7. Match Start Errors

### NOT_ROOM_HOST
- **Meaning:** Only the Host may perform this action.
- **Cause:** A non-Host tried to start/configure/kick.
- **User Message:** "Only the host can do that."
- **Recovery:** Ask the host.
- **Related:** V-START-1, V-HOST-1, BR-GS-2, BR-HOST-1.

### GAME_ALREADY_STARTED
- **Meaning:** The match is already running.
- **Cause:** A second start attempt or setup change mid-match.
- **User Message:** "The match has already started."
- **Recovery:** None; wait for it to end.
- **Related:** BR-GS-5, INV-G7.

### MATCH_CONFIGURATION_INVALID
- **Meaning:** Team/role composition is not valid to start.
- **Cause:** A team lacks a Spymaster, lacks an Operative, or fewer than 4 players.
- **User Message:** "Each team needs one Spymaster and at least one Operative (4+ players)."
- **Recovery:** Fix team/role setup.
- **Related:** V-START-2/3, BR-GS-1, INV-T6.

### DICTIONARY_TOO_SMALL
- **Meaning:** The dictionary version has fewer than 25 usable words.
- **Cause:** Under-provisioned dictionary.
- **User Message:** "This word set doesn't have enough words to play."
- **Recovery:** Choose another region/version.
- **Related:** V-DICT-2, V-START-4, BR-GS-3, INV-D2.

---

## 8. Clue Errors

### NOT_SPYMASTER
- **Meaning:** Only a Spymaster may submit a clue.
- **Cause:** An Operative attempted to clue.
- **User Message:** "Only the Spymaster can give clues."
- **Recovery:** Wait for the Spymaster.
- **Related:** V-CLUE-1, BR-CL-1.

### NOT_YOUR_TURN
- **Meaning:** It is not the actor's team's turn / phase.
- **Cause:** Clue or guess out of turn/phase.
- **User Message:** "It's not your team's turn."
- **Recovery:** Wait for your turn.
- **Related:** V-CLUE-2, V-GUESS-2, BR-TO-5.

### INVALID_CLUE
- **Meaning:** The clue is structurally invalid.
- **Cause:** More than one word; equals an unrevealed board word; invalid number. *(Sub-reasons below let the client explain precisely.)*
- **User Message:** "That clue isn't allowed."
- **Recovery:** Submit a valid one-word clue and number.
- **Related:** V-CLUE-3/4/5, BR-CL-2..5.
  - `CLUE_NOT_SINGLE_WORD` — more than one word/token (V-CLUE-3, BR-CL-3).
  - `CLUE_MATCHES_BOARD_WORD` — equals an unrevealed board word (V-CLUE-4, BR-CL-4).
  - `CLUE_NUMBER_INVALID` — number not ≥0 or "unlimited" (V-CLUE-5, BR-CL-5).

### CLUE_ALREADY_GIVEN
- **Meaning:** A clue already exists for this turn.
- **Cause:** Attempt to submit/change a second clue.
- **User Message:** "You've already given a clue this turn."
- **Recovery:** Wait for guessing to finish.
- **Related:** BR-CL-7, INV-G3.

---

## 9. Guess & Turn Errors

### NOT_OPERATIVE
- **Meaning:** Only an Operative may guess.
- **Cause:** A Spymaster attempted to guess.
- **User Message:** "Spymasters can't guess."
- **Recovery:** Let an Operative guess.
- **Related:** V-GUESS-1, BR-GV-1.

### NO_ACTIVE_CLUE
- **Meaning:** Guessing before a clue exists.
- **Cause:** Guess submitted in AwaitingClue phase.
- **User Message:** "Wait for your Spymaster's clue."
- **Recovery:** Wait for the clue.
- **Related:** V-GUESS-2, BR-GV-3.

### CARD_ALREADY_REVEALED
- **Meaning:** The targeted card is already revealed.
- **Cause:** Guess on a revealed card.
- **User Message:** "That card is already revealed."
- **Recovery:** Choose an unrevealed card.
- **Related:** V-GUESS-3, BR-GV-2, INV-B7.

### GUESS_LIMIT_REACHED
- **Meaning:** No guesses remain for this turn.
- **Cause:** Guess beyond (clue number + 1).
- **User Message:** "No guesses left this turn."
- **Recovery:** The turn ends automatically.
- **Related:** V-GUESS-4, BR-GV-7, INV-G6.

### INVALID_GUESS
- **Meaning:** The guess target is invalid (out of range / not on board).
- **Cause:** Malformed card reference.
- **User Message:** "That isn't a valid card."
- **Recovery:** Select a card on the board.
- **Related:** V-GUESS-3, BR-GV-2.

### END_TURN_BEFORE_GUESS
- **Meaning:** Cannot end the turn without at least one guess.
- **Cause:** End-turn with zero guesses this turn.
- **User Message:** "Make at least one guess before ending your turn."
- **Recovery:** Guess once, then end.
- **Related:** V-ENDTURN-1, BR-GV-6, INV-G5.

### NOT_ACTIVE_TEAM
- **Meaning:** A non-active team tried to guess/end turn.
- **Cause:** Action by the waiting team.
- **User Message:** "It's the other team's turn."
- **Recovery:** Wait for your turn.
- **Related:** V-ENDTURN-2, V-GUESS-1, BR-TO-5.

---

## 10. Match State Errors

### GAME_ALREADY_FINISHED
- **Meaning:** The match is over; no play accepted.
- **Cause:** Clue/guess/end-turn after finish.
- **User Message:** "This match has ended."
- **Recovery:** Start a rematch (host) or leave.
- **Related:** V-STATE-1, BR-GE-2, INV-G7.

### GAME_NOT_STARTED
- **Meaning:** A play action was attempted before the match began.
- **Cause:** Clue/guess in Lobby.
- **User Message:** "The match hasn't started yet."
- **Recovery:** Wait for the host to start.
- **Related:** V-STATE-2, BR-GS-4.

### ACTION_OUT_OF_PHASE
- **Meaning:** The action doesn't match the current turn phase.
- **Cause:** e.g., guessing during AwaitingClue.
- **User Message:** "You can't do that right now."
- **Recovery:** Wait for the correct phase.
- **Related:** V-STATE-2, Turn state (§8.3).

---

## 11. Connection & Reconnection Errors

### RECONNECT_TOKEN_INVALID
- **Meaning:** The reconnect token is missing/invalid.
- **Cause:** Wrong or absent room-scoped token.
- **User Message:** "We couldn't restore your session — please rejoin."
- **Recovery:** Rejoin as a new player.
- **Related:** V-RECON-1, BR-DC-2.

### RECONNECT_WINDOW_EXPIRED
- **Meaning:** The grace period passed before reconnection.
- **Cause:** Late return.
- **User Message:** "Your seat was released — you'll join the next match."
- **Recovery:** Rejoin as a new player / waiting member.
- **Related:** V-RECON-2, BR-DC-5, BR-EC-12.

### DUPLICATE_CONNECTION
- **Meaning:** A second active connection for the same player identity was detected.
- **Cause:** Multiple devices/tabs using the same identity.
- **User Message:** "You're already connected on another device."
- **Recovery:** Continue on one connection; the newest supersedes (see [16 — Player Session](15-player-session-reconnection.md)).
- **Related:** INV-P4.

### GAME_PAUSED
- **Meaning:** Play is paused awaiting an essential player.
- **Cause:** Active Spymaster / sole active Operative disconnected.
- **User Message:** "Waiting for a player to reconnect…"
- **Recovery:** Wait for reconnection or grace expiry.
- **Related:** BR-DC-3/4, EVT-24.

---

## 12. Traceability

| Error Code | Validation | Business Rule | Invariant |
|-----------|-----------|---------------|-----------|
| ROOM_NOT_FOUND / INVALID_ROOM_CODE | V-ROOM-1 | BR-JR-2, BR-RC-2 | INV-R2 |
| ROOM_EXPIRED / ROOM_CLOSED | V-ROOM-2 | BR-RX-1/2 | INV-R4 |
| ROOM_FULL | V-CAP-1 | BR-JR-5 | INV-R5 |
| GAME_IN_PROGRESS_CANNOT_JOIN | — | BR-JR-6/6a | INV-B9 |
| NICKNAME_REQUIRED/INVALID | V-NICK-1/2 | BR-JR-4 | — |
| DUPLICATE_NICKNAME | V-NICK-3 | BR-JR-3 | INV-P1 |
| TEAM_INVALID / TEAM_CHANGE_NOT_ALLOWED | V-TEAM-1/2 | BR-TA-1/4 | INV-T1/T2 |
| ROLE_ALREADY_TAKEN | V-ROLE-1 | BR-RO-1/6 | INV-T3 |
| ROLE_CHANGE_NOT_ALLOWED | V-ROLE-2 | BR-RO-5 | INV-T5 |
| NOT_ROOM_HOST | V-START-1/V-HOST-1 | BR-GS-2/BR-HOST-1 | INV-R1 |
| GAME_ALREADY_STARTED | — | BR-GS-5 | INV-G7 |
| MATCH_CONFIGURATION_INVALID | V-START-2/3 | BR-GS-1 | INV-T6 |
| DICTIONARY_TOO_SMALL | V-DICT-2/V-START-4 | BR-GS-3 | INV-D2 |
| DICTIONARY_NOT_FOUND | V-DICT-1 | BR-RC-4 | INV-D1 |
| NOT_SPYMASTER | V-CLUE-1 | BR-CL-1 | INV-G4 |
| NOT_YOUR_TURN / NOT_ACTIVE_TEAM | V-CLUE-2/V-GUESS-1 | BR-TO-5 | INV-G4 |
| INVALID_CLUE (+sub) | V-CLUE-3/4/5 | BR-CL-2..5 | — |
| CLUE_ALREADY_GIVEN | — | BR-CL-7 | INV-G3 |
| NOT_OPERATIVE | V-GUESS-1 | BR-GV-1 | INV-G4 |
| NO_ACTIVE_CLUE | V-GUESS-2 | BR-GV-3 | INV-G3 |
| CARD_ALREADY_REVEALED | V-GUESS-3 | BR-GV-2 | INV-B7 |
| GUESS_LIMIT_REACHED | V-GUESS-4 | BR-GV-7 | INV-G6 |
| END_TURN_BEFORE_GUESS | V-ENDTURN-1 | BR-GV-6 | INV-G5 |
| GAME_ALREADY_FINISHED | V-STATE-1 | BR-GE-2 | INV-G7 |
| GAME_NOT_STARTED / ACTION_OUT_OF_PHASE | V-STATE-2 | BR-GS-4 | INV-G2 |
| RECONNECT_TOKEN_INVALID | V-RECON-1 | BR-DC-2 | INV-P2 |
| RECONNECT_WINDOW_EXPIRED | V-RECON-2 | BR-DC-5 | INV-P3 |
| DUPLICATE_CONNECTION | — | — | INV-P4 |
| GAME_PAUSED | — | BR-DC-3/4 | INV-P3 |
