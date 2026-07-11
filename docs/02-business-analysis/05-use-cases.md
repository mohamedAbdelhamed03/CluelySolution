# 6. Use Cases — Cluely

Each use case includes Primary Actor, Goal, Trigger, Preconditions, Main Success Scenario
(MSS), Alternative Scenarios, Exception Flows, and Postconditions. References point to
[Business Rules](02-business-rules.md) and [Functional Requirements](03-functional-requirements.md).

---

## UC-01 Create Room
- **Primary Actor:** Host
- **Goal:** Establish a private room and obtain a shareable code.
- **Trigger:** Host chooses "Create room".
- **Preconditions:** Dictionary catalog available.
- **MSS:**
  1. Host provides nickname and (optionally) dictionary.
  2. System generates a unique room code.
  3. System creates the room in Lobby with Host as sole member.
  4. System returns the room code and Host's identity token.
- **Alternative Scenarios:**
  - 1a. No dictionary chosen → default applied.
- **Exception Flows:**
  - 2a. Code collision → regenerate transparently.
  - 3a. Resource/capacity limit → creation fails with reason.
- **Postconditions:** Live room in Lobby; creator is Host. (BR-RC-*, F-01)

## UC-02 Join Room
- **Primary Actor:** Player
- **Goal:** Enter an existing room.
- **Trigger:** Player submits a room code + nickname.
- **Preconditions:** Room is live and not full.
- **MSS:**
  1. Player submits code + nickname.
  2. System validates code, capacity, nickname uniqueness.
  3. System admits the Player (unassigned team, Operative default) and issues token.
  4. System broadcasts updated membership.
- **Alternative Scenarios:**
  - 2a. Match in progress → Player admitted as a waiting member for the next match.
- **Exception Flows:**
  - 2b. Invalid/expired code → reject.
  - 2c. Room full → reject.
  - 2d. Duplicate nickname → reject; prompt to change.
- **Postconditions:** Player is a member. (BR-JR-*, F-02)

## UC-03 Configure Teams
- **Primary Actor:** Player
- **Goal:** Join/switch the desired team.
- **Trigger:** Player selects a team.
- **Preconditions:** Room in Lobby/Post-Match.
- **MSS:**
  1. Player selects Red or Blue.
  2. System assigns the player and broadcasts.
- **Alternative Scenarios:**
  - 1a. Player switches → previous team cleared.
- **Exception Flows:**
  - 1b. Attempt during active match → rejected (BR-TA-4).
- **Postconditions:** Player on exactly one team. (BR-TA-*, F-04)

## UC-04 Assign Roles
- **Primary Actor:** Player (or Host assigning)
- **Goal:** Establish one Spymaster per team.
- **Trigger:** Player claims Spymaster.
- **Preconditions:** Room in Lobby/Post-Match; player on a team.
- **MSS:**
  1. Player claims Spymaster.
  2. System verifies team has no Spymaster and assigns it.
  3. Others remain Operatives.
- **Alternative Scenarios:**
  - 1a. Player releases Spymaster → role vacant.
- **Exception Flows:**
  - 2a. Spymaster already exists → reject second claim.
  - 2b. Simultaneous claims → exactly one wins.
- **Postconditions:** ≤1 Spymaster per team. (BR-RO-*, F-05)

## UC-05 Start Game
- **Primary Actor:** Host
- **Goal:** Begin a valid match.
- **Trigger:** Host chooses "Start".
- **Preconditions:** Valid composition; dictionary ≥25 words.
- **MSS:**
  1. Host requests start.
  2. System validates composition and dictionary.
  3. System generates board+key, picks starting team, locks setup.
  4. System transitions to In-Match and broadcasts the first turn (active team AwaitingClue).
- **Alternative Scenarios:**
  - 2a. Invalid composition → start blocked with specific reason.
- **Exception Flows:**
  - 2b. Not Host → reject.
  - 2c. Dictionary <25 words → reject.
  - 3a. Player leaves mid-validation → re-validate.
- **Postconditions:** Match In-Progress. (BR-GS-*, BR-BG-*, F-06/F-07)

## UC-06 Generate Board
- **Primary Actor:** System (Game Engine)
- **Goal:** Produce the immutable board, key, and starting team.
- **Trigger:** Successful Start Game validation.
- **Preconditions:** Dictionary version resolved with ≥25 words.
- **MSS:**
  1. Select 25 distinct random words.
  2. Assign 9/8/7/1 ownership randomly; pick starting team randomly.
  3. Mark all unrevealed; deliver key to Spymasters only.
- **Alternative Scenarios:** None.
- **Exception Flows:**
  - 1a. Insufficient unique words → abort start.
- **Postconditions:** Fixed board+key for the match. (BR-BG-*, F-07)

## UC-07 Submit Clue
- **Primary Actor:** Spymaster (active team)
- **Goal:** Direct Operatives to the team's words.
- **Trigger:** Active team's clue phase begins.
- **Preconditions:** Turn = AwaitingClue; actor is active team's Spymaster.
- **MSS:**
  1. Spymaster submits word + number.
  2. System validates structure.
  3. System records clue, computes guess allowance, transitions to AwaitingGuess.
  4. System broadcasts the clue.
- **Alternative Scenarios:**
  - 1a. Number 0/unlimited → unbounded guessing (≥1).
- **Exception Flows:**
  - 2a. Multi-word clue → reject.
  - 2b. Clue equals unrevealed board word → reject.
  - 2c. Not active Spymaster / not clue phase → reject.
  - 1b. Spymaster disconnects before submitting → clue phase pauses.
- **Postconditions:** Active clue exists. (BR-CL-*, F-08)

## UC-08 Guess Words
- **Primary Actor:** Operative (active team)
- **Goal:** Reveal the team's agent cards.
- **Trigger:** Clue becomes active.
- **Preconditions:** Turn = AwaitingGuess; active clue; actor is active-team Operative.
- **MSS:**
  1. Operative selects an unrevealed card.
  2. System validates the guess.
  3. System reveals ownership and applies outcome.
  4. System rechecks win/loss; updates state.
  5. Steps 1–4 repeat while the guess is correct and guesses remain.
- **Alternative Scenarios:**
  - 3a. Own agent, guesses remain → continue (same team).
  - 3b. Own last agent → team wins; match ends.
  - 3c. Neutral → turn ends.
  - 3d. Opponent agent → opponent count drops; turn ends (or opponent wins if it was their last).
  - 3e. Guess limit reached → turn ends.
- **Exception Flows:**
  - 2a. Card already revealed → reject.
  - 2b. Out of turn/phase, or Spymaster → reject.
  - 1a. Near-simultaneous guesses → serialize; first valid applies.
  - 3f. Assassin → guessing team loses; opponent wins; match ends.
- **Postconditions:** Card(s) revealed; state updated. (BR-GV-*, BR-CG/IG/NC/OPP/ASN, F-09)

## UC-09 End Turn
- **Primary Actor:** Operative (active team)
- **Goal:** Voluntarily stop guessing and pass play.
- **Trigger:** Operative chooses "End turn".
- **Preconditions:** Turn = AwaitingGuess; ≥1 guess made this turn.
- **MSS:**
  1. Operative ends the turn.
  2. System validates ≥1 guess; transitions to opponent's AwaitingClue.
  3. System broadcasts the turn change.
- **Alternative Scenarios:** None.
- **Exception Flows:**
  - 2a. Zero guesses made → reject.
  - 2b. Non-active team → reject.
- **Postconditions:** Opponent becomes active. (BR-TE-3, BR-GV-6, F-10)

## UC-10 Switch Teams (between matches)
- **Primary Actor:** Player
- **Goal:** Rebalance teams before a rematch.
- **Trigger:** Room returns to Lobby/Post-Match.
- **Preconditions:** No active match.
- **MSS:**
  1. Player selects a different team/role.
  2. System applies and broadcasts.
- **Alternative Scenarios:** None.
- **Exception Flows:**
  - 1a. Active match → reject.
- **Postconditions:** New team/role recorded for next match. (BR-TA-5, BR-RO, F-04/F-05)

## UC-11 Finish Match
- **Primary Actor:** System (Game Engine)
- **Goal:** Conclude the match and record the result.
- **Trigger:** A reveal meets a terminal condition.
- **Preconditions:** Match In-Progress.
- **MSS:**
  1. System detects win (all agents) or loss (assassin).
  2. System transitions Match to Finished; records Game Result; may reveal full key.
  3. Room transitions to Post-Match.
- **Alternative Scenarios:**
  - 1a. Opponent's last agent revealed by the guessing team → opponent wins.
- **Exception Flows:** None (deterministic).
- **Postconditions:** Game Result recorded; room Post-Match. (BR-WIN/LOSE/ASN/GE, F-11)

## UC-12 Leave Room
- **Primary Actor:** Player (incl. Host)
- **Goal:** Exit the room.
- **Trigger:** Player chooses "Leave".
- **Preconditions:** Player is a member.
- **MSS:**
  1. Player leaves.
  2. System removes them and broadcasts.
- **Alternative Scenarios:**
  - 1a. Host leaves → host migration.
  - 1b. Last member leaves → room expires.
  - 1c. Leaving breaks team minimum mid-match → abandonment handling.
- **Exception Flows:** None.
- **Postconditions:** Membership updated or room closed. (BR-LR-*, BR-HM, F-03)

## UC-13 Reconnect Player
- **Primary Actor:** Player (reconnecting)
- **Goal:** Resume prior participation after a drop.
- **Trigger:** Player reconnects with token.
- **Preconditions:** Within grace; room live.
- **MSS:**
  1. Player reconnects with code + token.
  2. System validates token and grace window.
  3. System restores team/role and role-appropriate view; resumes any paused phase.
  4. System broadcasts reconnection.
- **Alternative Scenarios:**
  - 2a. Grace expired → treat as fresh join for next match.
- **Exception Flows:**
  - 2b. Invalid token → rejoin as new player.
  - 1a. Room expired → reconnection fails (room closed).
- **Postconditions:** Player resumes. (BR-DC-2/7, F-14)

## UC-14 End Game / Expire Room
- **Primary Actor:** System
- **Goal:** Close abandoned/idle/unplayable rooms.
- **Trigger:** Inactivity threshold, empty room, or abandonment.
- **Preconditions:** Room live.
- **MSS:**
  1. System detects the closing condition.
  2. System closes the room, discards transient state, releases the code.
  3. System notifies any connected participants.
- **Alternative Scenarios:**
  - 1a. Unplayable mid-match → record abandonment, then close/return per membership.
- **Exception Flows:**
  - 2a. Late intents after close → rejected with room-closed reason.
- **Postconditions:** Room not live; code reusable. (BR-RX-*, BR-GE-5, F-15)
