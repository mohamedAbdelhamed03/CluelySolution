# 7. Domain Model — Cluely

The domain model describes business **entities**, their **responsibilities**,
**relationships**, and **business constraints**. It is conceptual and technology-neutral —
no storage, schema, or type decisions. Identity is **transient** (no accounts); the model
is structured so durable identity can attach later at the Player/Identity boundary.

---

## 7.1 Entity overview & relationships

```
Room 1───* Player                 Room 1───0..1 Game (current match)
Room 1───1 Host (a Player)         Room 1───1 DictionarySelection ──► DictionaryVersion ──► CountryDictionary
Game 1───1 Board                   Game 1───2 Team
Game 1───* Turn (ordered)          Game 1───0..1 GameResult
Team 1───1 Spymaster (Player)      Team 1───* Operative (Player)
Board 1───25 WordCard              WordCard 1───1 CardOwnership
Turn 1───0..1 Clue                 Turn 1───* Guess
Round 1───2 Turn                   Player 1───1 PlayerConnection
```

Cardinality notes: a Room holds many Players but at most one active Game; a Game has exactly
two Teams and an ordered sequence of Turns; a Board has exactly 25 WordCards.

---

## 7.2 Entities

### Room
- **Purpose:** Private session container that groups players around a code and one match at a time.
- **Responsibilities:** Hold membership; own the room code; track lifecycle state (Lobby/In-Match/Post-Match/Expired); reference the selected dictionary; designate the Host; enforce capacity; drive expiration.
- **Relationships:** Has many Players; has one Host (a Player); has 0..1 current Game; has one DictionarySelection.
- **Constraints:** Unique live room code (BR-RC-2/3); exactly one Host (BR-RC-6); at most one active Game; expires when empty/idle/unplayable (BR-RX-*).

### Player
- **Purpose:** A temporary participant identified only by a nickname for the room's lifetime.
- **Responsibilities:** Hold nickname; hold transient identity/reconnect token; reference current team and game role; track connection via PlayerConnection.
- **Relationships:** Belongs to one Room; optionally on one Team; holds one Role per match; has one PlayerConnection.
- **Constraints:** Nickname unique within room (BR-JR-3); at most one team (BR-TA-2); exactly one role per match (BR-RO-3); identity is transient (no PII). **Future-auth seam:** may later link to a durable Account without changing this entity's role in rules (AUTH-1).

### Host (role of a Player)
- **Purpose:** The player holding room-control privileges.
- **Responsibilities:** Configure room settings; select dictionary; start match/rematch; manage members in Lobby.
- **Relationships:** Is exactly one Player of the Room.
- **Constraints:** Exactly one Host at all times; transfers via host migration (BR-HM-*).

### Game (Match)
- **Purpose:** One complete play session on one board.
- **Responsibilities:** Own the Board, the two Teams, the ordered Turns, the active team pointer, status (NotStarted/InProgress/Finished), and the GameResult.
- **Relationships:** Belongs to one Room; has one Board; has two Teams; has many Turns; has 0..1 GameResult.
- **Constraints:** Board+key immutable once generated (BR-BG-8); exactly one winner per completed match (BR-WIN-3); terminal on win/loss/abandonment (BR-GE-1/5).

### Board
- **Purpose:** The 5×5 arrangement of 25 word cards for a match.
- **Responsibilities:** Hold the 25 WordCards and their layout; expose revealed state; carry the key (ownership map) for Spymaster delivery.
- **Relationships:** Belongs to one Game; has exactly 25 WordCards.
- **Constraints:** Exactly 25 distinct words (BR-BG-1/2); ownership counts 9/8/7/1 (BR-BG-3); fixed for the match (BR-BG-8).

### WordCard
- **Purpose:** A single board position bearing a word and a hidden ownership.
- **Responsibilities:** Hold its word text (from the dictionary), its CardOwnership, and its revealed flag.
- **Relationships:** Belongs to one Board; has one CardOwnership.
- **Constraints:** Ownership immutable (BR-CO-2); ownership disclosed only when revealed (BR-CO-3/4); a revealed card cannot be guessed again (BR-GV-2).

### CardOwnership (value)
- **Purpose:** Classify a card as Red agent, Blue agent, Neutral, or Assassin.
- **Responsibilities:** Encode which team (or neutral/assassin) owns the card.
- **Relationships:** Belongs to one WordCard.
- **Constraints:** Exactly one of {Red, Blue, Neutral, Assassin} (BR-CO-1); counts across the board sum to 9/8/7/1.

### Dictionary / CountryDictionary
- **Purpose:** A curated word source for a specific country/region (e.g., Egypt, Saudi Arabia, USA, France, Germany).
- **Responsibilities:** Provide a culturally appropriate set of words; be selectable by the Host.
- **Relationships:** Has many DictionaryVersions; referenced by Room's DictionarySelection.
- **Constraints:** Must contain ≥25 usable words to be playable (BR-GS-3); affects only word text, never rules (BR-BG-9).

### DictionaryVersion
- **Purpose:** A versioned snapshot of a country dictionary's contents.
- **Responsibilities:** Provide a reproducible word set for board generation.
- **Relationships:** Belongs to a CountryDictionary; referenced by a Game's board generation.
- **Constraints:** Immutable once published; an in-progress match keeps the version it started with (F-16 exception).

### DictionarySelection (value on Room)
- **Purpose:** Record which region/version the room will use.
- **Responsibilities:** Resolve to a DictionaryVersion at board generation.
- **Constraints:** Changeable only in Lobby (BR-RC-4); locked during a match.

### Team
- **Purpose:** One of two opposing sides (Red/Blue).
- **Responsibilities:** Hold its members; designate its Spymaster; track its remaining agent count.
- **Relationships:** Belongs to a Game; has one Spymaster (Player) and ≥1 Operative (Player).
- **Constraints:** Exactly one Spymaster (BR-RO-1/6); ≥1 Operative (BR-TA-6); colour is Red or Blue (BR-TA-1).

### Role (value)
- **Purpose:** A player's function within a team: Spymaster or Operative.
- **Responsibilities:** Determine permissions (clue vs guess) and view (key vs no key).
- **Constraints:** One role per player per match (BR-RO-3); not changeable mid-match (BR-RO-5).

### Turn
- **Purpose:** One team's full opportunity: a clue followed by guessing.
- **Responsibilities:** Reference the active team; hold the Clue; hold the sequence of Guesses; track remaining guesses; track its phase (AwaitingClue/AwaitingGuess/Ended).
- **Relationships:** Belongs to a Game; has 0..1 Clue; has many Guesses; pairs into a Round.
- **Constraints:** One clue per turn (BR-CL-7); ≥1 guess before voluntary end (BR-GV-6); ends per BR-TE-*.

### Clue (value)
- **Purpose:** The Spymaster's instruction: one word + a number (≥0 or unlimited).
- **Responsibilities:** Carry the word, the number, and the derived guess allowance.
- **Relationships:** Belongs to one Turn.
- **Constraints:** Single word (BR-CL-2/3); not equal to an unrevealed board word (BR-CL-4); number ≥0 or unlimited (BR-CL-5); immutable once given (BR-CL-7).

### Guess
- **Purpose:** An Operative's selection of a card to reveal.
- **Responsibilities:** Reference the targeted WordCard, the guessing Operative, and the resulting reveal outcome.
- **Relationships:** Belongs to one Turn; targets one WordCard.
- **Constraints:** Targets an unrevealed card (BR-GV-2); valid only in guessing phase by active-team Operative (BR-GV-1); serialized (BR-EC-13).

### Round
- **Purpose:** A pair of opposing turns (one per team).
- **Responsibilities:** Group two consecutive turns for narrative/sequence tracking.
- **Relationships:** Contains two Turns; belongs to a Game.
- **Constraints:** Completes when both teams have taken their turn (or the match ends earlier).

### GameResult
- **Purpose:** The recorded outcome of a match.
- **Responsibilities:** Record winning team, losing team, reason (all-agents / assassin / abandonment), and final board snapshot.
- **Relationships:** Belongs to one Game.
- **Constraints:** Exactly one per completed match; abandonment has no play-based winner (BR-GE-5, BR-TIE-3).

### GameState (aggregate view)
- **Purpose:** The complete current condition of a match for delivery to clients.
- **Responsibilities:** Compose board (role-filtered), active team, current turn/phase, remaining agent counts, active clue, status.
- **Relationships:** Derived from Game + Board + Turn + Teams.
- **Constraints:** Must be role-filtered (Operatives never receive unrevealed ownership) (BR-CO-4, NFR-3).

### PlayerConnection
- **Purpose:** Track a player's live connectivity.
- **Responsibilities:** Hold connection state (Connected/Disconnected/Reconnected/Removed) and grace timing; drive pause/migration/abandonment effects.
- **Relationships:** Belongs to one Player.
- **Constraints:** Disconnect doesn't immediately remove the player (BR-DC-1); reconnection within grace restores role (BR-DC-2/7).

### RoomCode (value)
- **Purpose:** The shareable key to a private room.
- **Responsibilities:** Uniquely identify a live room; be easy to share.
- **Constraints:** Unique among live rooms (BR-RC-3); non-sequential/unguessable (R-3); released on expiry (BR-RX-4).

### IdentityToken (value) — future-auth seam
- **Purpose:** Transient, room-scoped proof of continuity for reconnection.
- **Responsibilities:** Allow a returning player to resume; contain no PII.
- **Constraints:** Scoped to one room; valid within grace; later associable to a durable Account without rule changes (AUTH-3).

---

## 7.3 Key invariants (cross-entity)

| Invariant | Source |
|-----------|--------|
| Board ownership always partitions into 9 + 8 + 7 + 1 = 25. | BR-BG-3 |
| Exactly one Host per Room at all times. | BR-RC-6, BR-HM-4 |
| Exactly one Spymaster per Team during a match. | BR-RO-1/6 |
| A card's ownership is immutable and only public when revealed. | BR-CO-2/3 |
| Operatives never receive unrevealed ownership. | BR-CO-4, NFR-3 |
| Exactly one winner per completed match (no draw). | BR-WIN-3, BR-TIE-* |
| Dictionary affects words only, never rules. | BR-BG-9 |
| Identity is transient and PII-free; attachable to accounts later. | C-7, AUTH-1 |
