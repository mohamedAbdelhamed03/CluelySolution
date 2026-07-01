# 4. Functional Requirements — Cluely

Each feature is specified with Purpose, Actors, Preconditions, Main Flow, Alternative
Flows, Postconditions, governing Business Rules, Validation Rules, Failure Scenarios, and
Exceptions. Rule references point to [Business Rules](03-business-rules.md) and
[Validation Rules](10-validation-rules.md).

---

## F-01 Create Room

- **Purpose:** Allow a player to start a new private session and become its Host.
- **Actors:** Host (initiating Player).
- **Preconditions:** None (no auth). A regional dictionary catalog is available.
- **Main Flow:**
  1. Player requests room creation, providing a nickname and a chosen dictionary (or accepts default).
  2. System generates a unique, non-sequential room code.
  3. System creates the room in **Lobby** state with the creator as Host and first member.
  4. System issues the Host a transient room-scoped identity token and returns the room code.
- **Alternative Flows:**
  - A1: Dictionary not chosen → system applies a configured default.
- **Postconditions:** A live room exists in Lobby; creator is Host and a Player.
- **Business Rules:** BR-RC-1..7, BR-HOST-1.
- **Validation Rules:** V-NICK-*, V-DICT-1.
- **Failure Scenarios:** Code-generation collision → regenerate; content catalog unavailable → creation fails with error.
- **Exceptions:** System cannot allocate a room (capacity/limits) → reject with reason.

## F-02 Join Room

- **Purpose:** Allow a player to enter an existing private room.
- **Actors:** Player.
- **Preconditions:** A live, non-expired room exists for the given code.
- **Main Flow:**
  1. Player submits room code + nickname.
  2. System validates code (live, not expired), room not full, nickname unique.
  3. System adds the Player (unassigned team, default Operative) and issues identity token.
  4. System broadcasts updated room membership to all participants.
- **Alternative Flows:**
  - A1: Match in progress → Player joins as a **waiting member** for the next match (BR-JR-6).
- **Postconditions:** Player is a member of the room and visible to others.
- **Business Rules:** BR-JR-1..8.
- **Validation Rules:** V-ROOM-1, V-ROOM-2, V-NICK-1..3, V-CAP-1.
- **Failure Scenarios:** Invalid/expired code → reject; room full → reject; duplicate nickname → reject and prompt for another.
- **Exceptions:** Transient network failure during join → Player may retry with same nickname.

## F-03 Leave Room

- **Purpose:** Allow a player to exit a room cleanly.
- **Actors:** Player (incl. Host).
- **Preconditions:** Player is a member of the room.
- **Main Flow:**
  1. Player requests to leave.
  2. System removes the Player from team/role and membership.
  3. System broadcasts updated membership.
- **Alternative Flows:**
  - A1: Leaver is Host → host migration (BR-HM).
  - A2: Leaver is last member → room expires (BR-LR-6).
  - A3: Leaver causes a team to drop below minimum mid-match → disconnect/abandonment rules (BR-DC-5).
- **Postconditions:** Player no longer in room; room state updated or closed.
- **Business Rules:** BR-LR-1..6, BR-HM-1..4.
- **Validation Rules:** V-MEMBER-1.
- **Failure Scenarios:** None significant; leaving always succeeds.
- **Exceptions:** Leave during match end resolution → applied after result recorded.

## F-04 Select / Switch Team

- **Purpose:** Let players organize into Red/Blue before a match.
- **Actors:** Player.
- **Preconditions:** Room in **Lobby** (or Post-Match before a new match starts).
- **Main Flow:**
  1. Player selects a team (Red or Blue).
  2. System assigns the player to that team and broadcasts the change.
- **Alternative Flows:**
  - A1: Player switches teams → previous assignment cleared, new applied.
- **Postconditions:** Player belongs to exactly one team.
- **Business Rules:** BR-TA-1..7.
- **Validation Rules:** V-TEAM-1 (not during active match), V-TEAM-2 (valid team).
- **Failure Scenarios:** Attempt during active match → rejected.
- **Exceptions:** N/A.

## F-05 Assign / Claim Role (Spymaster / Operative)

- **Purpose:** Establish exactly one Spymaster per team; others are Operatives.
- **Actors:** Player; Host (may assign).
- **Preconditions:** Room in Lobby/Post-Match; player is on a team.
- **Main Flow:**
  1. Player claims Spymaster for their team (or Host assigns it).
  2. System verifies the team has no existing Spymaster; assigns the role.
  3. Remaining team members are Operatives by default.
- **Alternative Flows:**
  - A1: Player releases Spymaster → role becomes vacant; another may claim.
- **Postconditions:** Team has at most one Spymaster.
- **Business Rules:** BR-RO-1..6.
- **Validation Rules:** V-ROLE-1 (one spymaster), V-ROLE-2 (not during match).
- **Failure Scenarios:** Second Spymaster claim while one exists → rejected (BR-EC-10).
- **Exceptions:** Simultaneous claims → exactly one wins; the other is informed.

## F-06 Start Match

- **Purpose:** Begin a match once setup is valid.
- **Actors:** Host.
- **Preconditions:** Room in Lobby; valid composition; dictionary has ≥25 words.
- **Main Flow:**
  1. Host requests start.
  2. System validates composition (2 teams; each 1 Spymaster + ≥1 Operative; ≥4 players).
  3. System generates board + key, picks starting team, locks setup, transitions to In-Match.
  4. System broadcasts the role-filtered board and the first turn (active team's clue phase).
- **Alternative Flows:**
  - A1: Composition invalid → start rejected with specific reason.
- **Postconditions:** Match In-Progress; Turn = active team AwaitingClue.
- **Business Rules:** BR-GS-1..5, BR-BG-1..9, BR-TO-1.
- **Validation Rules:** V-START-1..4.
- **Failure Scenarios:** Dictionary <25 words → reject; not Host → reject.
- **Exceptions:** Player leaves between validation and generation → re-validate before generating.

## F-07 Generate Board (system feature within Start)

- **Purpose:** Produce the 25-card board, key, and starting team.
- **Actors:** System (Game Engine).
- **Preconditions:** Valid start; dictionary version resolved.
- **Main Flow:**
  1. Select 25 distinct random words from dictionary version.
  2. Randomly assign 9/8/7/1 ownership and a random starting team.
  3. Mark all cards unrevealed; deliver key to Spymasters only.
- **Alternative Flows:** None.
- **Postconditions:** Immutable board+key fixed for the match.
- **Business Rules:** BR-BG-1..9, BR-CO-1..4.
- **Validation Rules:** V-DICT-2 (sufficient words), V-BOARD-1 (counts sum to 25).
- **Failure Scenarios:** Insufficient unique words → start aborted.
- **Exceptions:** N/A.

## F-08 Submit Clue

- **Purpose:** Let the active Spymaster direct their Operatives.
- **Actors:** Spymaster (active team).
- **Preconditions:** Match In-Progress; Turn = active team **AwaitingClue**; submitter is that team's Spymaster.
- **Main Flow:**
  1. Spymaster submits a clue word + number (≥0 or unlimited).
  2. System validates structure (one word; not equal to any unrevealed board word; valid number).
  3. System records the active clue, computes allowed guesses, transitions Turn to **AwaitingGuess**.
  4. System broadcasts the clue to all participants.
- **Alternative Flows:**
  - A1: Clue number 0 / unlimited → unbounded guesses (≥1) enabled (BR-EC-2/3).
- **Postconditions:** An active clue exists; Operatives may guess.
- **Business Rules:** BR-CL-1..10, BR-GV-7.
- **Validation Rules:** V-CLUE-1..5.
- **Failure Scenarios:** Multi-word clue → reject; clue equals unrevealed board word → reject; not active Spymaster → reject; not clue phase → reject.
- **Exceptions:** Spymaster disconnects before submitting → clue phase pauses (BR-DC-3).

## F-09 Submit Guess

- **Purpose:** Let Operatives reveal cards based on the clue.
- **Actors:** Operative (active team).
- **Preconditions:** Match In-Progress; Turn = **AwaitingGuess**; active clue exists; submitter is active-team Operative.
- **Main Flow:**
  1. Operative selects an unrevealed card.
  2. System validates (active team, Operative, guessing phase, card unrevealed, within guess limit).
  3. System reveals the card's ownership to all and applies the outcome:
     - Own agent → decrement team's agents; continue if guesses remain (F-08 outcome rules).
     - Neutral → end turn.
     - Opponent agent → decrement opponent's agents; end turn.
     - Assassin → guessing team loses; match ends.
  4. System checks win/loss after each reveal and updates state.
- **Alternative Flows:**
  - A1: Correct guess and guesses remain → Turn stays AwaitingGuess (same team).
  - A2: Correct guess reveals team's last agent → team wins; match ends.
  - A3: Guess limit reached → turn ends.
- **Postconditions:** Card revealed; scores/turn/match state updated.
- **Business Rules:** BR-GV-1..8, BR-CG-*, BR-IG-*, BR-NC-*, BR-OPP-*, BR-ASN-*, BR-WIN-*, BR-LOSE-*.
- **Validation Rules:** V-GUESS-1..5.
- **Failure Scenarios:** Guess on revealed card → reject; guess out of turn/phase → reject; Spymaster guess → reject; guess after limit → reject.
- **Exceptions:** Near-simultaneous guesses → serialize; first valid applies (BR-EC-13).

## F-10 End Turn

- **Purpose:** Let the active team voluntarily stop guessing.
- **Actors:** Operative (active team).
- **Preconditions:** Turn = AwaitingGuess; at least one guess already made this turn.
- **Main Flow:**
  1. Operative chooses to end the turn.
  2. System validates ≥1 guess made; transitions Turn to **TurnEnded**, then to opponent's **AwaitingClue**.
  3. System broadcasts turn change.
- **Alternative Flows:** None.
- **Postconditions:** Opposing team becomes active in clue phase.
- **Business Rules:** BR-TE-3, BR-TE-6, BR-CG-4, BR-GV-6.
- **Validation Rules:** V-ENDTURN-1 (≥1 guess), V-ENDTURN-2 (active team).
- **Failure Scenarios:** End turn with zero guesses → reject; end turn by non-active team → reject.
- **Exceptions:** N/A.

## F-11 Resolve Win / Loss (system feature)

- **Purpose:** Detect and apply terminal conditions.
- **Actors:** System (Game Engine).
- **Preconditions:** A reveal just occurred.
- **Main Flow:**
  1. After each reveal, check: assassin revealed? any team's agents all revealed?
  2. If assassin → guessing team loses, other wins; if a team's last agent → that team wins.
  3. Transition Match to **Finished**; record Game Result; optionally reveal full key.
- **Alternative Flows:** None — checked deterministically after every reveal.
- **Postconditions:** Match Finished with recorded result; room → Post-Match.
- **Business Rules:** BR-WIN-*, BR-LOSE-*, BR-ASN-*, BR-GE-1..4, BR-TIE-*.
- **Validation Rules:** V-END-1 (single winner).
- **Failure Scenarios:** None (deterministic).
- **Exceptions:** N/A.

## F-12 Rematch / Start New Match

- **Purpose:** Play again in the same room.
- **Actors:** Host.
- **Preconditions:** Room in **Post-Match**; ≥ minimum players present.
- **Main Flow:**
  1. Room returns to Lobby; players may switch teams/roles (BR-TA-5, BR-RO).
  2. Host starts a new match → new board/key/starting team generated (F-06/F-07).
- **Alternative Flows:**
  - A1: Composition no longer valid → start blocked until fixed.
- **Postconditions:** A new, independent match begins.
- **Business Rules:** BR-GE-4, BR-HOST-2, BR-GS-*.
- **Validation Rules:** V-START-1..4.
- **Failure Scenarios:** Too few players → cannot start.
- **Exceptions:** N/A.

## F-13 Disconnect Handling (system feature)

- **Purpose:** Tolerate transient loss of connectivity without corrupting state.
- **Actors:** System (Connection Manager), affected Player.
- **Preconditions:** Player was connected.
- **Main Flow:**
  1. System detects loss of connection; marks player **Disconnected**; starts grace timer.
  2. If the player is essential to the active phase (active Spymaster / sole active Operative), the relevant phase **pauses**.
  3. System broadcasts the connection-state change.
- **Alternative Flows:**
  - A1: Host disconnects → after grace, host migration (BR-HM).
  - A2: Grace expires and team cannot meet minimum → abandonment (BR-RX-3).
- **Postconditions:** Connection state reflects reality; play paused if needed.
- **Business Rules:** BR-DC-1..8.
- **Validation Rules:** V-CONN-1.
- **Failure Scenarios:** Mass disconnect → room may expire (BR-HM-3, BR-RX).
- **Exceptions:** N/A.

## F-14 Reconnect Player

- **Purpose:** Restore a returning player to their prior position.
- **Actors:** Player (reconnecting).
- **Preconditions:** Within grace period; valid room-scoped identity token; room still live.
- **Main Flow:**
  1. Player reconnects with room code + identity token.
  2. System validates token + grace window; restores team/role and **role-appropriate view**.
  3. Paused phase resumes; system broadcasts reconnection.
- **Alternative Flows:**
  - A1: Grace expired → treated as a fresh join into Lobby/next match (BR-EC-12).
- **Postconditions:** Player resumes; play continues.
- **Business Rules:** BR-DC-2, BR-DC-7, BR-EC-12.
- **Validation Rules:** V-RECON-1 (valid token), V-RECON-2 (within grace).
- **Failure Scenarios:** Invalid/expired token → must rejoin as new player.
- **Exceptions:** Room expired during absence → reconnection fails with room-closed reason.

## F-15 Room Expiration (system feature)

- **Purpose:** Reclaim abandoned/idle rooms.
- **Actors:** System.
- **Preconditions:** Room idle beyond threshold, empty, or unplayable.
- **Main Flow:**
  1. System detects inactivity/empty/abandonment.
  2. System closes the room, discards transient state, releases the room code.
  3. System notifies any still-connected participants.
- **Alternative Flows:** None.
- **Postconditions:** Room no longer live; code reusable.
- **Business Rules:** BR-RX-1..5.
- **Validation Rules:** V-EXP-1.
- **Failure Scenarios:** N/A.
- **Exceptions:** Activity arrives during closing → closing proceeds; late intents rejected with room-closed.

## F-16 Select / Localize Dictionary (system & Host feature)

- **Purpose:** Provide culturally appropriate words without altering rules.
- **Actors:** Host (selects), System (applies at board generation).
- **Preconditions:** A dictionary catalog with versions per region exists.
- **Main Flow:**
  1. Host selects a region's dictionary in Lobby.
  2. System records the selected dictionary **version** for the room.
  3. On match start, board generation draws only words from that version.
- **Alternative Flows:**
  - A1: No selection → default region applies.
- **Postconditions:** Board words come from the chosen region; rules unchanged.
- **Business Rules:** BR-RC-4, BR-BG-2, BR-BG-9, FR-35..37.
- **Validation Rules:** V-DICT-1 (valid region), V-DICT-2 (≥25 words).
- **Failure Scenarios:** Selected dictionary unavailable/too small → block start with reason.
- **Exceptions:** Dictionary updated between matches → new match uses current version; in-progress match keeps its fixed version.
