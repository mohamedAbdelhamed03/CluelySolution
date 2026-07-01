# 9. Business Workflows — Cluely

End-to-end process flows. Each workflow lists actor, trigger, steps, decision points, and
outcomes. Rule references point to [Business Rules](03-business-rules.md); features to
[Functional Requirements](04-functional-requirements.md).

---

## WF-01 Create Room
**Actor:** Host · **Trigger:** "Create room".
1. Host enters nickname; selects dictionary (or default).
2. System generates unique room code → creates Room (Lobby) → Host = creator.
3. System issues identity token; returns code.
**Outcome:** Live room in Lobby. (F-01, BR-RC-*)

---

## WF-02 Join Room
**Actor:** Player · **Trigger:** Enter code + nickname.
1. Validate code (live, not expired). → invalid? reject.
2. Validate capacity. → full? reject.
3. Validate nickname uniqueness. → duplicate? prompt to change.
4. Add Player (unassigned team, Operative default); issue token; broadcast.
   - If a match is in progress → Player waits for next match (BR-JR-6).
**Outcome:** Player in room. (F-02, BR-JR-*)

---

## WF-03 Configure Teams
**Actor:** Players · **Trigger:** Lobby setup.
1. Each Player selects Red or Blue.
2. System assigns; broadcasts.
3. Players may switch freely while in Lobby.
**Decision:** During active match → switching blocked (BR-TA-4).
**Outcome:** Players distributed across two teams. (F-04, BR-TA-*)

---

## WF-04 Assign Roles
**Actor:** Players / Host · **Trigger:** Lobby setup.
1. One Player per team claims Spymaster.
2. System verifies no existing Spymaster on that team → assigns; else rejects.
3. Remaining members are Operatives.
**Decision:** Simultaneous claims → exactly one succeeds (BR-RO-4/6).
**Outcome:** Each team has 1 Spymaster + ≥1 Operative. (F-05, BR-RO-*)

---

## WF-05 Start Game
**Actor:** Host · **Trigger:** "Start".
1. Validate: 2 teams; each 1 Spymaster + ≥1 Operative; ≥4 players; dictionary ≥25 words.
   - Invalid → block with specific reason.
2. Generate board + key (WF-06); pick starting team; lock setup.
3. Transition Room→InMatch, Game→InProgress, Turn→starting team AwaitingClue.
4. Broadcast role-filtered board (key to Spymasters only) and first turn.
**Outcome:** Match begins. (F-06, BR-GS-*)

---

## WF-06 Generate Board
**Actor:** System · **Trigger:** Valid start.
1. Draw 25 distinct random words from the selected dictionary version.
2. Randomly assign ownership: 9 (starting team), 8 (other), 7 neutral, 1 assassin.
3. Randomly choose which team is the starting team (the one with 9).
4. Mark all cards unrevealed.
5. Deliver the key to both Spymasters; never to Operatives.
**Outcome:** Immutable board + key. (F-07, BR-BG-*)

---

## WF-07 Submit Clue
**Actor:** Active Spymaster · **Trigger:** Turn = AwaitingClue.
1. Spymaster submits word + number.
2. Validate: single word; not equal to any unrevealed board word; number ≥0 or unlimited; actor is active Spymaster; phase is AwaitingClue.
   - Any fail → reject with reason.
3. Record clue; compute guess allowance (number + 1, or unbounded for 0/unlimited).
4. Transition Turn→AwaitingGuess; broadcast clue to all.
**Decision:** Spymaster disconnected → phase Paused until reconnect/grace (BR-DC-3).
**Outcome:** Active clue; guessing enabled. (F-08, BR-CL-*)

---

## WF-08 Guess Words
**Actor:** Active Operative · **Trigger:** Turn = AwaitingGuess with active clue.
1. Operative selects an unrevealed card.
2. Validate: active team; Operative; guessing phase; card unrevealed; within guess limit.
   - Fail → reject.
3. Reveal card ownership to all.
4. Apply outcome:
   - **Own agent:** decrement own count. If last own agent → **team wins** (→ WF-11). Else if guesses remain → stay AwaitingGuess (repeat from 1). Else → turn ends (→ WF-09 auto).
   - **Neutral:** turn ends (→ WF-09 auto).
   - **Opponent agent:** decrement opponent count. If it was opponent's last → **opponent wins** (→ WF-11). Else turn ends.
   - **Assassin:** guessing team **loses**, opponent **wins** (→ WF-11).
5. Re-check terminal conditions after every reveal.
**Decision:** Near-simultaneous guesses → serialize; first valid applies (BR-EC-13).
**Outcome:** Card(s) revealed; turn continues, ends, or match ends. (F-09, BR-GV/CG/IG/NC/OPP/ASN)

---

## WF-09 End Turn
**Actor:** Active Operative (or system on a turn-ending event) · **Trigger:** Voluntary stop or turn-ending reveal/limit.
1. (Voluntary) Validate ≥1 guess made this turn → else reject.
2. Transition Turn→TurnEnded.
3. If match not finished → pass to opponent's AwaitingClue (new turn); broadcast.
**Outcome:** Opponent becomes active, or match already ended. (F-10, BR-TE-*)

---

## WF-10 Switch Teams (between matches)
**Actor:** Players · **Trigger:** Room in Lobby/PostMatch.
1. Players change team and/or role.
2. System applies; re-validates composition before any new start.
**Decision:** Active match → blocked (BR-TA-4).
**Outcome:** New setup for the next match. (F-04/F-05, BR-TA-5)

---

## WF-11 Finish Match
**Actor:** System · **Trigger:** Terminal reveal (all agents / assassin).
1. Determine winner & reason (own-agents-complete, opponent-agents-complete, or assassin).
2. Transition Game→Finished; record Game Result; optionally reveal full key to all.
3. Transition Room→PostMatch; broadcast result.
**Outcome:** Match concluded; result recorded. (F-11, BR-WIN/LOSE/ASN/GE)

---

## WF-12 Leave Room
**Actor:** Player (incl. Host) · **Trigger:** "Leave".
1. Remove Player from team/role and membership; broadcast.
2. Decisions:
   - Leaver is Host → **host migration** (WF-14b) to longest-present connected player.
   - Leaver is last member → **room expires** (WF-15).
   - Leaver breaks team minimum mid-match → enter abandonment handling (WF-13/WF-15).
**Outcome:** Membership updated or room closed. (F-03, BR-LR-*)

---

## WF-13 Reconnect Player
**Actor:** Reconnecting Player · **Trigger:** Returns with code + token.
1. Validate token + within grace + room live.
   - Grace expired → treat as fresh join for next match (WF-02).
   - Invalid token → must rejoin as new player.
   - Room expired → fail (room closed).
2. Restore team/role and role-appropriate view (Spymaster sees key).
3. Resume any Paused phase; broadcast reconnection.
**Outcome:** Player resumes. (F-14, BR-DC-2/7)

---

## WF-14 Disconnect & Host Migration
**Actor:** System · **Trigger:** Connection lost.
### WF-14a Disconnect
1. Mark player Disconnected; start grace timer; broadcast.
2. If essential to active phase (active Spymaster / sole active Operative) → Pause phase (BR-DC-3/4).
3. If grace expires → mark Removed; re-check team composition → possible abandonment (WF-15).
### WF-14b Host Migration
1. If a disconnected/left player is the Host and grace expired → reassign Host to longest-present connected player (deterministic).
2. If no connected player remains → expire room (WF-15).
**Outcome:** Connectivity reflected; play paused/migrated as needed. (F-13, BR-DC-*, BR-HM-*)

---

## WF-15 End Game / Expire Room
**Actor:** System · **Trigger:** Idle timeout, empty room, or unplayable abandonment.
1. If mid-match abandonment → record Game Result as Abandoned (no play-winner).
2. Close room; discard transient state; release room code.
3. Notify any still-connected participants.
**Outcome:** Room no longer live; code reusable. (F-15, BR-RX-*, BR-GE-5)

---

## 9.x Happy-path overview (one full match)

```
WF-01 Create → WF-02 Join (×N) → WF-03 Teams → WF-04 Roles → WF-05 Start → WF-06 Board
   → [ WF-07 Clue → WF-08 Guess(es) → WF-09 End Turn ]  (repeat, alternating teams)
   → WF-11 Finish → (WF-10 reconfigure) → WF-05 Start (rematch) ...
```
Resilience flows (WF-13/WF-14/WF-15) may interleave at any point without corrupting the
match state, per the state machines in [State Machines](08-state-machines.md).
