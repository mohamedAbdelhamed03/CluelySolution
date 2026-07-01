# 3. Business Rules Document — Cluely

Every rule below is **normative**. Rules are language-independent and identical for all
regional dictionaries. Rule IDs are stable references used by other documents.

Legend: **MUST** = mandatory; **MUST NOT** = forbidden; **MAY** = allowed option.

---

## 3.1 Room creation

| ID | Rule |
|----|------|
| BR-RC-1 | Any Player MAY create a room; upon creation they become the **Host**. |
| BR-RC-2 | The system MUST generate a **unique, non-sequential room code** for each live room. |
| BR-RC-3 | A room code MUST be unique among all currently live (non-expired) rooms. |
| BR-RC-4 | At creation the Host MUST select a **regional dictionary** (a default MAY be pre-selected); it MAY be changed until the match starts. |
| BR-RC-5 | A newly created room starts in the **Lobby** state with the Host as its only member. |
| BR-RC-6 | A room MUST belong to exactly one Host at any time. |
| BR-RC-7 | The creator MUST also occupy a team/role like any Player (the Host is a Player with privileges). |

## 3.2 Joining a room

| ID | Rule |
|----|------|
| BR-JR-1 | A Player MUST provide a valid room code and a nickname to join. |
| BR-JR-2 | The room code MUST correspond to a **live, non-expired** room, else join is rejected. |
| BR-JR-3 | A nickname MUST be **unique within the room** (case-insensitive, trimmed); duplicates are rejected. |
| BR-JR-4 | A nickname MUST be non-empty and within the allowed length/character bounds (see [Validation](10-validation-rules.md)). |
| BR-JR-5 | A Player MUST NOT join a room that is **full** (at the room's max capacity). |
| BR-JR-6 | A Player MUST NOT join a room whose **match is in progress** as an active game participant. (MVP: late joiners wait for the next match; they do not enter an in-progress match.) |
| BR-JR-6a | A waiting member of an in-progress match MUST NOT receive any board, key, clue, or card data while that match is running; they receive only room membership and lifecycle information. This preserves information fairness (no spectator view) per NFR-3. They begin receiving game state only when the next match starts and they are a participant. |
| BR-JR-7 | On joining, the system MUST issue the Player a **transient, room-scoped identity/reconnect token**. |
| BR-JR-8 | A joining Player defaults to **unassigned team** / **Operative** role until they choose otherwise. |

## 3.3 Leaving a room

| ID | Rule |
|----|------|
| BR-LR-1 | A Player MAY leave a room at any time. |
| BR-LR-2 | When a Player leaves, they MUST be removed from their team and relinquish any role. |
| BR-LR-3 | If the leaving Player is the **Host**, **host migration** (BR-HM) MUST occur. |
| BR-LR-4 | If the leaving Player is the **active team's Spymaster** mid-clue, see disconnect/role-vacancy rules (BR-DC). |
| BR-LR-5 | If, after a Player leaves, a team no longer satisfies minimum composition during a match, the match MUST follow disconnect/abandonment rules (BR-DC, BR-RX). |
| BR-LR-6 | When the **last** Player leaves a room, the room MUST be closed/expired. |

## 3.4 Host responsibilities & host migration (BR-HM)

| ID | Rule |
|----|------|
| BR-HOST-1 | Only the Host MAY change room-level settings (dictionary, max capacity if configurable) and **start the match**. |
| BR-HOST-2 | Only the Host MAY initiate a **rematch / new match**. |
| BR-HOST-3 | The Host MAY remove a Player from the room (kick) **in the Lobby only**. This is a **room-management necessity** for private digital rooms (recovering from a stuck/abandoned slot or an accidental joiner so a valid composition can be formed), not a gameplay or social feature; it has no in-match effect and mirrors the table-owner's control of who sits at the physical table. |
| BR-HM-1 | If the Host leaves or disconnects beyond the grace period, the system MUST reassign Host to another **connected** Player. |
| BR-HM-2 | Host reassignment MUST be deterministic (e.g., longest-present connected Player) so the outcome is predictable. |
| BR-HM-3 | If no connected Player remains, the room MUST expire (BR-RX). |
| BR-HM-4 | Host privileges MUST transfer fully and atomically; there is never more than one Host. |

## 3.5 Team assignment

| ID | Rule |
|----|------|
| BR-TA-1 | There MUST be exactly **two teams**: Red and Blue. |
| BR-TA-2 | A Player MUST belong to at most **one** team at a time. |
| BR-TA-3 | Players MAY choose or switch teams freely **while in Lobby** (match not started). |
| BR-TA-4 | Team switching MUST NOT be allowed **during an active match** (it would corrupt the key/turn assumptions). |
| BR-TA-5 | Between matches (room returns to Lobby), Players MAY switch teams and roles again. |
| BR-TA-6 | A match MUST NOT start unless **both** teams have at least one Spymaster and one Operative (BR-GS-1). |
| BR-TA-7 | Team sizes MAY be unequal in number of Operatives; only the role minimums are enforced. |

## 3.6 Role assignment

| ID | Rule |
|----|------|
| BR-RO-1 | Each team MUST have exactly **one Spymaster** at match start. |
| BR-RO-2 | All other team members are **Operatives**. |
| BR-RO-3 | A Player MUST hold exactly one game role per match: Spymaster **or** Operative. |
| BR-RO-4 | The Spymaster role MAY be claimed/assigned in Lobby; if two players contend, the system MUST resolve to exactly one (first-claim wins, or Host assigns). |
| BR-RO-5 | Role changes MUST NOT occur during an active match (the Spymaster has seen the key). |
| BR-RO-6 | A team MUST NOT have two Spymasters; the second claim is rejected while one exists. |

## 3.7 Game start (BR-GS)

| ID | Rule |
|----|------|
| BR-GS-1 | The match MUST NOT start unless: 2 teams exist, each has exactly 1 Spymaster and ≥1 Operative, and total players ≥ `MIN_PLAYERS` (4). |
| BR-GS-2 | Only the **Host** MAY start the match. |
| BR-GS-3 | A dictionary MUST be selected and contain at least 25 usable words for the chosen region/version. |
| BR-GS-4 | On start, the system MUST generate the board (BR-BG) and key, transition the room to **In-Match**, and the game to its initial turn. |
| BR-GS-5 | Once started, Lobby-only settings (team/role/dictionary) are **locked** for the match's duration. |

## 3.8 Board generation (BR-BG)

| ID | Rule |
|----|------|
| BR-BG-1 | The board MUST contain exactly **25 distinct word cards** in a 5×5 layout. |
| BR-BG-2 | Words MUST be selected **at random without repetition** from the room's selected dictionary version. |
| BR-BG-3 | The key MUST assign ownership: **9** agents to the **starting team**, **8** agents to the other team, **7** neutral, **1** assassin (total 25). |
| BR-BG-4 | The **starting team** (the one receiving 9 agents) MUST be chosen at random. |
| BR-BG-5 | The assignment of ownership to specific board positions MUST be random. |
| BR-BG-6 | The full key MUST be visible to **both Spymasters** and to **no Operative**. |
| BR-BG-7 | Every card begins **unrevealed**. |
| BR-BG-8 | The board, key, and starting team MUST be fixed for the entire match once generated. |
| BR-BG-9 | The dictionary affects only the **word text**; ownership distribution and counts are identical for all dictionaries. |

## 3.9 Card ownership

| ID | Rule |
|----|------|
| BR-CO-1 | Each card has exactly one ownership: **Red agent**, **Blue agent**, **Neutral**, or **Assassin**. |
| BR-CO-2 | Ownership is **immutable** for the match once the key is generated. |
| BR-CO-3 | Ownership becomes **public** only when the card is **revealed**. |
| BR-CO-4 | Unrevealed ownership MUST NOT be disclosed to any Operative by any interface. |

## 3.10 Turn order

| ID | Rule |
|----|------|
| BR-TO-1 | The **starting team** takes the first turn. |
| BR-TO-2 | Turns MUST alternate strictly between the two teams. |
| BR-TO-3 | A turn consists of exactly one **clue phase** (Spymaster) followed by one **guessing phase** (Operatives). |
| BR-TO-4 | A new turn for a team MUST NOT begin until the previous (opponent's) turn has ended. |
| BR-TO-5 | Only the **active team** may act (clue or guess) during its turn. |

## 3.11 Giving clues (BR-CL)

| ID | Rule |
|----|------|
| BR-CL-1 | Only the **active team's Spymaster** MAY submit a clue, and only during the **clue phase**. |
| BR-CL-2 | A clue MUST consist of exactly **one word** and **one number**. |
| BR-CL-3 | The clue word MUST be a **single token** (no spaces; no multiple words). |
| BR-CL-4 | The clue word MUST NOT match (case-insensitively) any **unrevealed** word currently on the board. |
| BR-CL-5 | The clue number MUST be an integer **≥ 0**, or the special value **"unlimited" (∞)**. |
| BR-CL-6 | The clue number SHOULD relate to how many of the team's words the clue targets, but the system enforces only the **structural** rules (BR-CL-2..5); semantic correctness is a **social** matter, not system-enforced. |
| BR-CL-7 | Exactly **one** clue MAY be active per turn; a Spymaster MUST NOT change a clue once submitted. |
| BR-CL-8 | A clue word that matches an **already-revealed** board word MAY be permitted (faithful to the board game, where revealed words are covered and a clue may reference them). The system MUST NOT block clue words that equal revealed words. |
| BR-CL-9 | The clue (word + number) MUST be broadcast to **all** participants once accepted. |
| BR-CL-10 | After a valid clue, the **maximum number of guesses** for the turn is defined by BR-GV-7. |

## 3.12 Guess validation (BR-GV)

| ID | Rule |
|----|------|
| BR-GV-1 | Only an **Operative of the active team** MAY submit a guess, and only during the **guessing phase** (after a clue is active). |
| BR-GV-2 | A guess MUST target an **unrevealed** card on the current board; revealed cards MUST NOT be guessable. |
| BR-GV-3 | A guess MUST NOT be accepted before a clue exists for the current turn. |
| BR-GV-4 | A guess MUST NOT be accepted from a Spymaster, a non-active team, or an Operative when it is not their team's turn. |
| BR-GV-5 | Each accepted guess MUST immediately **reveal** the targeted card's ownership to all participants. |
| BR-GV-6 | The team MUST make **at least one** guess after a clue (it cannot end the turn with zero guesses). |
| BR-GV-7 | The number of guesses permitted in a turn is: **clue number + `GUESS_BONUS` (1)** when the clue number ≥ 1; **unbounded** (but ≥ 1) when the clue number is **0** or **"unlimited"**. |
| BR-GV-8 | When operatives disagree, the system MUST require a single submitted guess; how the team decides which operative submits is a **social/coordination** matter (MVP: any active-team Operative may submit; first accepted guess applies). |

## 3.13 Correct guesses

| ID | Rule |
|----|------|
| BR-CG-1 | A guess of a card owned by the **guessing team** (own agent) is **correct**. |
| BR-CG-2 | On a correct guess, the card is revealed as the team's agent and the team's **remaining agent count decreases by one**. |
| BR-CG-3 | After a correct guess, if guesses remain (BR-GV-7) and the team has not won, the team MAY continue guessing. |
| BR-CG-4 | After a correct guess, the team MAY **voluntarily end** its turn (provided ≥1 guess was made, which it was). |
| BR-CG-5 | If the correct guess reveals the team's **last** agent, the team **wins immediately** (BR-WIN-1). |

## 3.14 Incorrect guesses

| ID | Rule |
|----|------|
| BR-IG-1 | A guess of a **neutral** card ends the turn immediately (BR-NC). |
| BR-IG-2 | A guess of an **opposing team's agent** reveals that card *for the opponent* and ends the turn immediately (BR-OPP). |
| BR-IG-3 | A guess of the **assassin** causes the guessing team to **lose immediately** (BR-ASN). |
| BR-IG-4 | Reaching the guess limit (BR-GV-7) without an incorrect guess ends the turn (BR-TE-2). |

## 3.15 Neutral cards (BR-NC)

| ID | Rule |
|----|------|
| BR-NC-1 | A neutral card belongs to no team. |
| BR-NC-2 | Guessing a neutral card reveals it as neutral and **ends the active team's turn immediately**. |
| BR-NC-3 | A neutral reveal changes **no** team's agent count. |

## 3.16 Opposing-team card (BR-OPP)

| ID | Rule |
|----|------|
| BR-OPP-1 | Guessing a card owned by the **other** team reveals it as that team's agent. |
| BR-OPP-2 | The **other** team's remaining agent count **decreases by one** (the guessing team helped them). |
| BR-OPP-3 | The active team's turn **ends immediately**. |
| BR-OPP-4 | If this reveal was the **other** team's **last** agent, the **other team wins immediately** (BR-WIN-1 applies to whichever team's last agent is revealed). |

## 3.17 Assassin card (BR-ASN)

| ID | Rule |
|----|------|
| BR-ASN-1 | There is exactly **one** assassin card. |
| BR-ASN-2 | If the active team guesses the assassin, that team **loses the match immediately**. |
| BR-ASN-3 | The opposing team **wins immediately** as a result. |
| BR-ASN-4 | Assassin resolution **overrides** all other end conditions and ends the match at once. |

## 3.18 Winning conditions (BR-WIN)

| ID | Rule |
|----|------|
| BR-WIN-1 | A team **wins** the moment **all of its agent cards are revealed**, regardless of which team's guess revealed the last one. |
| BR-WIN-2 | A team **wins** if the **opposing** team reveals the **assassin** (BR-ASN-3). |
| BR-WIN-3 | Exactly one team wins each completed match; there is **no draw** (see BR-TIE). |
| BR-WIN-4 | On a win, the match transitions to **Finished** and a **Game Result** is recorded (winning team + reason). |

## 3.19 Losing conditions (BR-LOSE)

| ID | Rule |
|----|------|
| BR-LOSE-1 | A team **loses** if it reveals the **assassin** (BR-ASN-2). |
| BR-LOSE-2 | A team **loses** if the **opposing** team reveals all of the opposing team's agents first (the mirror of BR-WIN-1). |
| BR-LOSE-3 | Losing is always the complement of the other team winning; there is exactly one winner and one loser per completed match. |

## 3.20 Turn ending (BR-TE)

| ID | Rule |
|----|------|
| BR-TE-1 | A turn ends on any **incorrect** guess (neutral, opponent, assassin → assassin also ends the match). |
| BR-TE-2 | A turn ends when the **guess limit** is reached. |
| BR-TE-3 | A turn ends when the active team **voluntarily stops** after making ≥1 guess. |
| BR-TE-4 | A turn ends when a **win/loss** condition is met (the match also ends). |
| BR-TE-5 | On turn end without match end, play passes to the **opposing team's** clue phase (BR-TO-2). |
| BR-TE-6 | A team MUST NOT end its turn before making at least one guess for the active clue (BR-GV-6). |

## 3.21 Game (match) ending (BR-GE)

| ID | Rule |
|----|------|
| BR-GE-1 | A match ends when a **win** (BR-WIN) or **loss/assassin** (BR-ASN/BR-LOSE) condition is met. |
| BR-GE-2 | On match end, no further clues or guesses are accepted; the full key MAY be revealed to all. |
| BR-GE-3 | A **Game Result** MUST be recorded: winning team, losing team, reason (all agents / assassin), and final board state. |
| BR-GE-4 | After match end, the room returns to a **Post-Match** state from which the Host MAY start a rematch (BR-HOST-2) or players MAY leave. |
| BR-GE-5 | A match MAY also end by **abandonment** (BR-RX-3) if it can no longer be played; this is recorded distinctly (no winner by play). |

## 3.22 Player disconnect / role vacancy (BR-DC)

| ID | Rule |
|----|------|
| BR-DC-1 | A disconnect MUST mark the player's **connection state** as disconnected without immediately removing them. |
| BR-DC-2 | A disconnected player MAY **reconnect within a grace period** and resume the same team and role. |
| BR-DC-3 | If the **active team's Spymaster** is disconnected during the clue phase, the clue phase MUST **pause** (no clue can be given) until reconnection or grace expiry. |
| BR-DC-4 | If the **active team's** only available Operative is disconnected during the guessing phase, guessing MUST **pause** until reconnection or grace expiry. |
| BR-DC-5 | If the grace period expires and a team can no longer satisfy minimum composition (no Spymaster, or no Operative), the match MUST be resolved by **abandonment** (BR-RX-3) or, if configured, the opposing team is awarded the match. (MVP: abandonment.) |
| BR-DC-6 | A non-essential Operative disconnecting (team still has ≥1 active Operative) MUST NOT pause play. |
| BR-DC-7 | Reconnection MUST restore the player's **role-appropriate view** (Spymaster sees key; Operative does not). |
| BR-DC-8 | Disconnect of the Host triggers **host migration** only after the grace period (BR-HM-1), to tolerate brief drops. |

## 3.23 Room expiration (BR-RX)

| ID | Rule |
|----|------|
| BR-RX-1 | A room MUST **expire** after a defined period of **inactivity** (no intents/connections). |
| BR-RX-2 | A room MUST expire when it becomes **empty** (no remaining players) (BR-LR-6). |
| BR-RX-3 | A match that can no longer be played (insufficient composition after grace) MUST end by **abandonment**; the room then expires or returns to Lobby per remaining membership. |
| BR-RX-4 | Expired rooms MUST release their room code for potential future reuse and discard transient state. |
| BR-RX-5 | Expiration MUST be communicated to any still-connected participants. |

## 3.24 Tie situations (BR-TIE)

| ID | Rule |
|----|------|
| BR-TIE-1 | There is **no draw** in normal play: win conditions are checked the instant a card is revealed, so exactly one team reaches a terminal condition first. |
| BR-TIE-2 | Because reveals are sequential (one card per guess), two teams CANNOT reach "all agents revealed" simultaneously. |
| BR-TIE-3 | The only non-win/loss outcome is **abandonment** (BR-RX-3), which is **not** a tie but an unfinished match with no play-based winner. |

## 3.25 Edge cases (BR-EC)

| ID | Rule |
|----|------|
| BR-EC-1 | **Guessing the opponent's last agent for them:** the opponent wins immediately (BR-OPP-4 + BR-WIN-1), even though the active team made the guess. |
| BR-EC-2 | **Clue number 0 ("none"):** operatives MAY guess any number of times (≥1) until an incorrect guess or voluntary stop; faithful to the board game's "0" meaning. |
| BR-EC-3 | **Clue number "unlimited" (∞):** same unbounded guessing as 0 (used to revisit prior unguessed clues); ≥1 guess required. |
| BR-EC-4 | **Bonus guess after exhausting the clue number:** when clue number ≥1, the team MAY take one extra guess (number + 1) to recover words from previous clues. |
| BR-EC-5 | **Last operative leaves mid-turn:** see BR-DC-4/5. |
| BR-EC-6 | **Spymaster attempts to guess / Operative attempts to clue:** rejected (BR-CL-1, BR-GV-1). |
| BR-EC-7 | **Guess on already-revealed card:** rejected (BR-GV-2). |
| BR-EC-8 | **Action after match end:** rejected (BR-GE-2). |
| BR-EC-9 | **Clue equal to a still-unrevealed board word:** rejected (BR-CL-4); equal to a revealed word: allowed (BR-CL-8). |
| BR-EC-10 | **Simultaneous Spymaster claims:** exactly one succeeds (BR-RO-4/6). |
| BR-EC-11 | **Host migration during active match:** match continues uninterrupted; only room-control privileges move (BR-HM-4). |
| BR-EC-12 | **Reconnect after grace expiry:** treated as a new join into Lobby/next match, not resumption of the old role (BR-DC-2). |
| BR-EC-13 | **Two operatives submit guesses near-simultaneously:** the system serializes; the **first** accepted guess applies, the second is re-evaluated against the new state (and rejected if no longer valid) (BR-GV-8). |

---

## 3.26 State transitions (summary index)

Complete state machines are in [State Machines](08-state-machines.md). The authoritative
transitions are:

- **Room:** Lobby → In-Match → Post-Match → (Lobby | Expired).
- **Game/Match:** NotStarted → InProgress → Finished (Won/Lost/Abandoned).
- **Turn:** AwaitingClue → AwaitingGuess → TurnEnded → (next team AwaitingClue | MatchEnd).
- **Player Connection:** Connected → Disconnected → (Reconnected→Connected | Removed).
- **Round:** a pair of turns; Open → Complete.

Every transition above is governed by the rules in this document and is cross-referenced in
the state-machine document.
