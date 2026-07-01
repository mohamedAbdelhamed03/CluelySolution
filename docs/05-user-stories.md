# 5. User Stories — Cluely

Format: *As a [actor], I want [capability], so that [benefit].* Each story has acceptance
criteria (AC) in Given/When/Then form. Actors: **Host**, **Player**, **Spymaster**,
**Operative**. Stories reference rules in [Business Rules](03-business-rules.md).

---

## 5.1 Host

### US-H1 — Create a room
*As a Host, I want to create a private room and get a shareable code, so that my friends can join.*
- AC1: Given I request a room, when it is created, then I receive a unique room code and become Host.
- AC2: Given the room is created, then it starts in Lobby with me as the only member.
- AC3: Given I created the room, then I can select the regional dictionary.

### US-H2 — Choose the dictionary
*As a Host, I want to pick my region's word dictionary, so that words feel natural to us.*
- AC1: Given I am in Lobby, when I choose a region, then subsequent board words come only from that region's dictionary.
- AC2: Given I change the dictionary before start, then the change takes effect for the next match.
- AC3: Given I make no choice, then a default dictionary is used.
- AC4: Changing the dictionary never changes any rule, count, or flow.

### US-H3 — Start the match
*As a Host, I want to start the match once teams are ready, so that play begins.*
- AC1: Given each team has exactly one Spymaster and at least one Operative and ≥4 players, when I start, then a board is generated and play begins.
- AC2: Given composition is invalid, when I start, then I am told the specific reason and the match does not begin.
- AC3: Given I am not the Host, then I cannot start the match.

### US-H4 — Manage the room
*As a Host, I want to manage room setup, so that the game is fair and ready.*
- AC1: Given I am Host in Lobby, then I may remove a disruptive player.
- AC2: Given a match is in progress, then team/role/dictionary settings are locked.

### US-H5 — Start a rematch
*As a Host, I want to start a new match in the same room, so that we can play again.*
- AC1: Given a match finished, when I start a rematch, then a fresh board/key/starting team is generated.
- AC2: Given players changed teams/roles between matches, then the new match uses the new setup.

### US-H6 — Keep the room alive on disconnect
*As a Host, I want the room to survive my brief disconnect, so that play is not lost.*
- AC1: Given I disconnect briefly, then Host is not reassigned until the grace period expires.
- AC2: Given my grace period expires, then Host migrates to another connected player deterministically.

---

## 5.2 Player

### US-P1 — Join with a code
*As a Player, I want to join a room with a code and nickname, so that I can play without signing up.*
- AC1: Given a valid, non-expired code, when I join with a unique nickname, then I become a room member.
- AC2: Given an invalid/expired code, then I am rejected with a clear reason.
- AC3: Given my nickname duplicates an existing one, then I am asked to pick another.
- AC4: Given the room is full, then I cannot join.
- AC5: I am never asked to register, log in, or create an account.

### US-P2 — Pick / switch a team
*As a Player, I want to choose my team before the match, so that I play with whom I want.*
- AC1: Given I am in Lobby, when I select a team, then I belong to that team only.
- AC2: Given I am in Lobby, when I switch teams, then my previous team assignment is cleared.
- AC3: Given a match is in progress, then I cannot switch teams.

### US-P3 — Choose a role
*As a Player, I want to choose Spymaster or stay Operative, so that I play the role I want.*
- AC1: Given my team has no Spymaster, when I claim it, then I become the Spymaster.
- AC2: Given my team already has a Spymaster, then my claim is rejected.
- AC3: Given two of us claim simultaneously, then exactly one becomes Spymaster.

### US-P4 — Leave the room
*As a Player, I want to leave at any time, so that I am not stuck.*
- AC1: Given I am a member, when I leave, then I am removed and others are notified.
- AC2: Given I was Host, when I leave, then Host migrates automatically.
- AC3: Given I was the last member, when I leave, then the room expires.

### US-P5 — See an appropriate, fair view
*As a Player, I want to see only what my role allows, so that the game is fair.*
- AC1: Given I am an Operative, then I never see the hidden ownership of unrevealed cards.
- AC2: Given I am a Spymaster, then I see the full key.
- AC3: Given a card is revealed, then everyone sees its ownership.

### US-P6 — Reconnect after a drop
*As a Player, I want to reconnect and resume, so that a brief network drop doesn't end my game.*
- AC1: Given I drop and return within the grace period with my token, then I resume my exact team and role.
- AC2: Given I return as a Spymaster, then my key view is restored.
- AC3: Given the grace period expired, then I rejoin as a new player for the next match.

---

## 5.3 Spymaster

### US-S1 — See the key
*As a Spymaster, I want to see which cards belong to my team, so that I can craft clues.*
- AC1: Given the match started, then I can see each card's ownership (mine, opponent, neutral, assassin).
- AC2: No Operative can see this information.

### US-S2 — Give a clue
*As a Spymaster, I want to submit a one-word clue and a number, so that my Operatives can guess.*
- AC1: Given it is my team's clue phase, when I submit one word + a number, then the clue is broadcast to all.
- AC2: Given I submit more than one word, then the clue is rejected.
- AC3: Given my clue word equals an unrevealed board word, then it is rejected.
- AC4: Given I submit a number ≥1, then my team may guess up to number + 1 times.
- AC5: Given I submit 0 or "unlimited", then my team may guess unboundedly (at least once).
- AC6: Given it is not my team's turn, or I am not the active Spymaster, then I cannot submit a clue.
- AC7: Once submitted, I cannot edit or retract the clue for that turn.

### US-S3 — Wait during opponents' turn
*As a Spymaster, I want the system to prevent me from acting out of turn, so that fairness holds.*
- AC1: Given it is the opponent's turn, then I cannot submit a clue.
- AC2: I cannot submit a guess at any time (Spymasters never guess).

---

## 5.4 Operative

### US-O1 — Guess a word
*As an Operative, I want to select a card based on the clue, so that we reveal our agents.*
- AC1: Given an active clue and it is my team's guessing phase, when I select an unrevealed card, then it is revealed to all.
- AC2: Given the card is my team's agent, then our remaining count drops and we may keep guessing within the limit.
- AC3: Given the card is neutral, then our turn ends.
- AC4: Given the card is the opponent's agent, then it counts for them and our turn ends.
- AC5: Given the card is the assassin, then we lose immediately and the opponent wins.
- AC6: Given I try to guess a revealed card, then it is rejected.
- AC7: Given there is no active clue, then I cannot guess.

### US-O2 — Continue or stop guessing
*As an Operative, I want to keep guessing or stop, so that we manage risk.*
- AC1: Given a correct guess and remaining guesses, then we may guess again.
- AC2: Given we have made at least one guess, then we may end the turn voluntarily.
- AC3: Given we have made zero guesses, then we cannot end the turn.
- AC4: Given we reach the guess limit, then the turn ends automatically.

### US-O3 — Win the match
*As an Operative, I want to reveal our last agent, so that we win.*
- AC1: Given my guess reveals our last agent, then we win and the match ends immediately.
- AC2: Given the opponent's guess revealed our last agent for us, then we still win.

### US-O4 — Act only in turn
*As an Operative, I want the system to enforce turn order, so that the game is fair.*
- AC1: Given it is not my team's turn, then I cannot guess.
- AC2: Given I am an Operative, then I never see unrevealed ownership.

---

## 5.5 Cross-cutting (all actors)

### US-X1 — Language-neutral fairness
*As any player, I want identical rules regardless of dictionary, so that the game is fair everywhere.*
- AC1: Given any regional dictionary, then counts (9/8/7/1), turn flow, and win/loss are identical.

### US-X2 — No account required
*As any player, I want to play without an account, so that there is zero friction.*
- AC1: I can complete an entire match using only a nickname and a room code.
