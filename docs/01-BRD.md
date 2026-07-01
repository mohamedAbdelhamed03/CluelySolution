# 1. Business Requirements Document (BRD) — Cluely

## 1.1 Product overview

Cluely is an online multiplayer word-association game played by two competing teams.
Each team has one **Spymaster**, who can see the secret identity of every word on a shared
board, and one or more **Operatives**, who can see only the words. The Spymaster gives a
one-word clue paired with a number; the Operatives interpret the clue to identify which
words on the board belong to their team. The first team to correctly identify all of its
own words wins. A single hidden "assassin" word causes an instant loss if guessed.

Cluely is the digital, online-multiplayer realization of this gameplay. It is functionally
equivalent to the Codenames board game. The game is played in **private rooms**: a host
creates a room, shares a **room code**, and invited friends join with a **temporary
nickname**. No account, registration, or login exists in this version.

## 1.2 Product vision

To let groups of friends play a faithful, frictionless, online version of the Codenames
word game from anywhere, on mobile, with **zero sign-up friction** — join with a code, pick
a nickname, and play. The product is **global with a single codebase and single gameplay**;
only the **word library is localized** per country/region so the words feel culturally
natural while the rules remain identical worldwide.

## 1.3 Business objectives

| # | Objective | Rationale |
|---|-----------|-----------|
| BO-1 | Deliver an MVP that faithfully reproduces Codenames gameplay online. | Authentic experience drives adoption and word-of-mouth. |
| BO-2 | Enable play with **no authentication barrier**. | Removes the single biggest drop-off point for casual social games. |
| BO-3 | Support **private, code-based rooms** for friend groups. | Matches how the physical game is played — among known people. |
| BO-4 | Provide **localized word libraries** without forking gameplay. | One product serves all regions; lowers maintenance cost. |
| BO-5 | Architect so that **authentication can be added later** without changing core game business. | Protects future roadmap (accounts, history) without rework. |
| BO-6 | Ensure the rules engine is **language-independent**. | Guarantees identical fairness and behaviour in every market. |

## 1.4 Stakeholders

| Stakeholder | Interest |
|-------------|----------|
| **Players** (end users) | A fair, fun, faithful, low-friction game. |
| **Host** (a player) | Ability to create a room, invite friends, and control match setup. |
| **Product Owner** | Delivery of an MVP matching the vision and scope. |
| **Business Analyst** | A complete, unambiguous specification of the business. |
| **Solution Architect** | Clear component boundaries and responsibilities. |
| **Developers (.NET backend, Flutter mobile)** | Implementable, technology-neutral rules. |
| **QA Engineers** | Testable rules, validations, and state transitions. |
| **Content/Localization team** | Owns and curates regional word dictionaries. |
| **Game Designer** | Confirms fidelity to the reference gameplay. |

## 1.5 Scope (in scope)

- Online real-time multiplayer for **2 teams**.
- **Private rooms** created by a host and joined via a **room code**.
- **Temporary nicknames** (no accounts, no persistence of identity).
- **Team assignment** (Red/Blue) and **role assignment** (Spymaster/Operative).
- **Board generation** of 25 words drawn from a selected **regional dictionary**.
- Full turn loop: **clue submission**, **guessing**, **turn ending**, **win/loss resolution**.
- Faithful handling of agent / neutral / assassin cards.
- **Disconnect / reconnect** handling and **host migration**.
- **Room lifecycle** including expiration of idle/abandoned rooms.
- **Localization of the word library** by country/region.
- Multiple consecutive matches (rematch) within the same room.

## 1.6 Out of scope

The following are explicitly **excluded** from this version:

- User authentication, accounts, registration, login, profiles.
- Monetization, in-app purchases, advertising.
- AI players, AI clue generation, or AI assistance of any kind.
- Ranking systems, leaderboards, ELO, matchmaking with strangers.
- Achievements, badges, progression, persistence of player history.
- Text chat, voice chat, video, emoji reactions, or any communication channel beyond gameplay actions.
- New game modes, custom rule variants, or rule changes (e.g., timers as a win condition, duet/cooperative modes).
- Custom user-supplied word lists (only curated regional dictionaries).
- Spectator/social features not present in the original gameplay.
- Public matchmaking or discovery of rooms.

> Note: items such as a per-turn timer are **not** part of the faithful baseline as a
> winning/losing condition and are therefore out of scope unless they exist purely as an
> optional courtesy that does not alter the rules. See [Assumptions](#18-assumptions).

## 1.7 Constraints

| # | Constraint |
|---|-----------|
| C-1 | The first release is an **MVP**. |
| C-2 | **No authentication** mechanism may be required to play. |
| C-3 | There is **one codebase, one gameplay, one product** globally. |
| C-4 | The **only** localized component is the word library/dictionary. |
| C-5 | Business rules **must never depend on a specific language**. |
| C-6 | Gameplay must remain **functionally equivalent to Codenames**; no rule may be added, removed, or simplified. |
| C-7 | Identity is **temporary**; a player exists only for the lifetime of their participation in a room. |
| C-8 | The design must allow authentication to be added later **without changing core game business**. |

## 1.8 Assumptions

| # | Assumption |
|---|-----------|
| A-1 | Players already know each other and coordinate verbally/externally; Cluely provides only the game surface, not communication. |
| A-2 | A room code is sufficient to gate access to a private room. |
| A-3 | A device/session can represent exactly one player at a time within a room. |
| A-4 | Each region's dictionary is curated and approved by the content team; word *meaning/relatedness* of clues is judged socially by players, not by the system. |
| A-5 | Network connectivity is intermittent on mobile; reconnection within a grace period is expected and supported. |
| A-6 | Clue *semantic legality* (is the clue truly one word, fairly related, not cheating) is partly a social contract; the system enforces only the *structural* clue rules (single word, valid number, not a visible board word). |
| A-7 | A minimum of 4 players is required to start a match (one Spymaster and one Operative per team). |

## 1.9 Risks

| # | Risk | Impact | Likelihood | Mitigation (business-level) |
|---|------|--------|-----------|------------------------------|
| R-1 | Players disconnect mid-match, stalling the game. | High | High | Reconnect grace period; host can reassign roles / continue; turn responsibilities can be transferred. |
| R-2 | Host leaves, abandoning the room. | High | Medium | Automatic **host migration** to another connected player. |
| R-3 | Room codes collide or are guessed. | Medium | Low | Sufficiently large, unique, non-sequential code space; rooms expire. |
| R-4 | Inappropriate or culturally inappropriate words in a dictionary. | Medium | Medium | Curated, reviewed regional dictionaries; versioning to allow corrections. |
| R-5 | Abandoned rooms accumulate. | Low | High | Room **expiration** by inactivity. |
| R-6 | Clue cheating (multi-word clues, board words) damages fairness. | Medium | Medium | Structural clue validation; social enforcement of semantics. |
| R-7 | Duplicate nicknames cause confusion. | Low | Medium | Nickname uniqueness validation within a room. |
| R-8 | Future authentication forces redesign. | Medium | Low | Identity abstraction so a temporary player can later be linked to an account. |

## 1.10 Success criteria

| # | Success criterion |
|---|-------------------|
| SC-1 | Four+ players can create/join a private room with only a nickname and code and complete a full match. |
| SC-2 | The match outcome is always correct per Codenames rules (agents/neutral/assassin/win/loss). |
| SC-3 | Switching dictionaries (e.g., Egypt → USA) changes only the words, never the rules or flow. |
| SC-4 | Disconnect/reconnect and host migration never corrupt game state. |
| SC-5 | No part of the rules engine references a natural language. |
| SC-6 | The same game can be played end-to-end with the planned .NET backend and Flutter mobile client. |
| SC-7 | Adding authentication later requires no change to the core game rules or workflows. |

## 1.11 Business glossary

| Term | Definition |
|------|------------|
| **Room** | A private session container identified by a room code, holding players and one active or pending match. |
| **Room Code** | A short, unique, shareable code used to join a private room. |
| **Host** | The player who created the room and holds setup/control privileges; transferable. |
| **Player** | A temporary participant identified only by a nickname for the room's lifetime. |
| **Team** | One of two opposing sides (Red, Blue). |
| **Role** | A player's function within a team: Spymaster or Operative. |
| **Spymaster** | The single team member who can see card ownership and gives clues. |
| **Operative** | A team member who sees only words and guesses based on the Spymaster's clue. |
| **Board** | The 5×5 arrangement of 25 word cards for a match. |
| **Word Card** | One of 25 cards bearing a word; has a hidden ownership (agent/neutral/assassin). |
| **Agent Card** | A word card belonging to a team (Red or Blue). |
| **Neutral Card** | A word card belonging to no team (bystander). |
| **Assassin Card** | The single card that causes the guessing team to instantly lose. |
| **Key / Key Card** | The secret map of which card belongs to which team/neutral/assassin, visible only to Spymasters. |
| **Starting Team** | The team that receives 9 agents and plays the first turn. |
| **Clue** | A single word plus a non-negative number (or "unlimited") given by a Spymaster. |
| **Clue Number** | The count accompanying the clue word; defines how many guesses are intended/allowed. |
| **Guess** | An Operative's selection of a word card to reveal. |
| **Turn** | One team's full opportunity: a clue followed by guessing until it ends. |
| **Round** | A pair of opposing turns (one per team); a match consists of multiple rounds. |
| **Reveal** | The act of exposing a card's hidden ownership when guessed. |
| **Match / Game** | One complete play session on one board, ending in a win or loss. |
| **Dictionary** | A curated set of words for a specific country/region. |
| **Dictionary Version** | A versioned snapshot of a dictionary's contents. |
| **Game Result** | The recorded outcome of a match: winning team and reason. |
| **Game State** | The complete current condition of a match (board, turn, scores, status). |
