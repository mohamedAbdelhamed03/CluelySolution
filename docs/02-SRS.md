# 2. Software Requirements Specification (SRS) — Cluely

*Structured in the spirit of IEEE 830 / ISO/IEC/IEEE 29148. Technology-neutral: no
implementation, deployment, or vendor decisions are specified.*

---

## 2.1 Purpose

This SRS defines the software requirements for **Cluely**, an online multiplayer
word-association game functionally equivalent to Codenames. It specifies what the system
must do (functional requirements), the qualities it must exhibit (non-functional
requirements), the business rules it must enforce, its external interfaces (as logical
contracts), and the logical architecture from a system perspective. It is the
authoritative requirements reference for engineering and QA.

## 2.2 Scope

The system enables players to create and join private rooms, organize into two teams with
Spymaster/Operative roles, and play a faithful match on a 25-card board using a localized
word dictionary, until one team wins or loses per the canonical rules. The system manages
real-time game state, role-based visibility (Spymasters see card ownership; Operatives do
not), turn flow, validation, disconnect/reconnect, host migration, and room lifecycle.

**Excluded** (see [BRD §1.6](01-BRD.md#16-out-of-scope)): authentication, accounts,
monetization, AI, ranking, chat/voice, achievements, new modes, custom word lists.

## 2.3 Product overview

Cluely presents each player with a shared board. Role-dependent views:

- **Spymaster view:** all 25 words plus the secret key (each card's ownership), plus a clue
  entry capability when it is the team's turn.
- **Operative view:** all 25 words with only *revealed* ownership shown; a guessing
  capability when it is the team's turn and a clue is active.

The system is the **authority** for all game state and rules; clients render state and
submit intents (create, join, set team/role, clue, guess, end turn, leave). The system
validates every intent against the current state and business rules and broadcasts
authoritative state changes to all room participants with the correct per-role visibility.

## 2.4 Product perspective

Cluely is a **new, self-contained** product (not a component of a larger system). It
comprises a server-side authority and client applications. The intended platforms are a
**.NET** backend and a **Flutter** mobile client; however, **no requirement in this
document depends on those choices**. The system is designed so that an **authentication
subsystem can be introduced later** by attaching durable identity to the existing
temporary-player abstraction, without altering the rules engine, room model, or workflows.

## 2.5 Users (actor classes)

| Actor | Description | Key capabilities |
|-------|-------------|------------------|
| **Host** | The player who created the room (a Player with extra privileges). | Create room, choose dictionary, manage setup, start match, control room, migrate-on-leave. |
| **Player** | Any temporary participant in a room. | Join/leave room, set nickname, pick team, pick role. |
| **Spymaster** | A Player assigned the Spymaster role on a team. | See the key, submit clues on their turn. |
| **Operative** | A Player assigned the Operative role on a team. | Submit guesses on their turn, end the turn. |

A single Player simultaneously holds a base role (Player), may be the Host, and during a
match holds exactly one game role (Spymaster or Operative) on exactly one team.

## 2.6 Assumptions and dependencies

- See [BRD §1.8](01-BRD.md#18-assumptions). Key dependency: a curated, versioned set of
  regional **word dictionaries** must exist and be selectable.
- Real-time bidirectional communication between server and clients is available.
- Clients are responsible only for presentation and intent submission, never for rule
  adjudication.

## 2.7 Constraints

See [BRD §1.7](01-BRD.md#17-constraints). Notably: no authentication; one gameplay; only
the dictionary is localized; rules are language-independent; future-auth-ready.

---

## 2.8 Functional Requirements

Requirements use **FR-x**. "The system shall" is implied. Each maps to detailed flows in
[Functional Requirements](04-functional-requirements.md) and rules in
[Business Rules](03-business-rules.md).

### Room management
- **FR-1** Allow a Player to create a room, becoming its Host, and generate a unique room code.
- **FR-2** Allow the Host to select the **regional dictionary** for the room at creation (or before match start).
- **FR-3** Allow a Player to join an existing room using a valid, non-expired room code and a nickname.
- **FR-4** Enforce **nickname uniqueness** within a room.
- **FR-5** Allow a Player to leave a room at any time.
- **FR-6** Reassign the Host role automatically (**host migration**) when the Host leaves or is removed.
- **FR-7** Expire and close rooms that are idle/abandoned beyond a defined inactivity threshold.
- **FR-8** Prevent joining a room that is full, expired, or non-existent.

### Team & role setup
- **FR-9** Allow Players to join one of the two teams (Red/Blue).
- **FR-10** Allow Players to switch teams while the match has not started (and per rules between matches).
- **FR-11** Allow exactly **one Spymaster per team**; allow a Player to claim/assign the Spymaster role.
- **FR-12** Assign **Operative** as the default/remaining game role.
- **FR-13** Prevent match start unless each team has exactly one Spymaster and at least one Operative.

### Match lifecycle
- **FR-14** Allow the Host to start the match once setup is valid.
- **FR-15** Generate a board of **25 distinct word cards** drawn from the selected dictionary.
- **FR-16** Generate the secret **key**: 9 agents to the starting team, 8 to the second team, 7 neutral, 1 assassin.
- **FR-17** Randomly determine the **starting team** (the team assigned 9 agents) and set turn order.
- **FR-18** Expose the key only to **Spymasters**; never reveal unrevealed card ownership to Operatives.

### Turn & play
- **FR-19** Allow the **active team's Spymaster** to submit a clue (one word + a number, including 0 / unlimited).
- **FR-20** Validate clue **structure** (single word, not equal to any unrevealed board word, valid number).
- **FR-21** After a valid clue, allow the **active team's Operatives** to submit guesses one card at a time.
- **FR-22** Reveal a guessed card's ownership and update state accordingly.
- **FR-23** Continue the turn on a **correct (own-team)** guess, up to the allowed number of guesses.
- **FR-24** End the turn on an **incorrect** guess (neutral, opponent, or limit reached) or on voluntary stop.
- **FR-25** Require at least **one guess** per turn after a clue is given.
- **FR-26** Pass play to the opposing team when a turn ends.
- **FR-27** Detect and apply **win** when a team's last agent card is revealed.
- **FR-28** Detect and apply **instant loss** when the assassin card is revealed (opposing team wins).
- **FR-29** Record the **Game Result** (winning team, reason) at match end.
- **FR-30** Allow the room to start a **new match (rematch)** after a match ends, reshuffling roles/teams as permitted.

### Resilience
- **FR-31** Detect player disconnects and mark connection state without immediately destroying the player.
- **FR-32** Allow a disconnected player to **reconnect** within a grace period and resume their role.
- **FR-33** Preserve and correctly restore each reconnecting player's role-appropriate view.
- **FR-34** Keep the match consistent if a Spymaster or Operative disconnects (block dependent actions, allow continuation per rules).

### Localization
- **FR-35** Support multiple regional dictionaries; the active dictionary affects **only the word source**.
- **FR-36** Ensure all rules, flows, and outcomes are identical regardless of selected dictionary.
- **FR-37** Reference dictionaries by **version** for reproducibility of a board's word source.

---

## 2.9 Non-functional Requirements

| # | Category | Requirement |
|---|----------|-------------|
| NFR-1 | **Real-time responsiveness** | State changes (clue, guess, reveal, turn change) propagate to all participants within a small, perceptibly-immediate latency budget. |
| NFR-2 | **Consistency** | The server is the single source of truth; no two clients ever see contradictory authoritative state. |
| NFR-3 | **Fairness / integrity** | Card ownership is never disclosed to Operatives before reveal, by any interface. |
| NFR-4 | **Availability** | A match in progress survives transient client disconnects and brief server interruptions without data loss of game state for the room's active lifetime. |
| NFR-5 | **Scalability** | The system supports many independent concurrent rooms; rooms are isolated and do not contend with one another. |
| NFR-6 | **Localizability** | Adding a new regional dictionary requires no change to rules or code paths. |
| NFR-7 | **Language independence** | No rule, validation, or state transition depends on the natural language of words. |
| NFR-8 | **Usability** | A new player can join and understand their role-appropriate view without instruction beyond the room code and nickname. |
| NFR-9 | **Portability** | Business logic is expressible independently of client platform (mobile/other). |
| NFR-10 | **Privacy** | No personal data is collected; identity is a transient nickname only. |
| NFR-11 | **Recoverability** | Game state can be restored to the last consistent point after an interruption within the room's lifetime. |
| NFR-12 | **Extensibility (future auth)** | Identity is abstracted so durable accounts can later attach without changing game rules. |
| NFR-13 | **Observability** | The system records enough about results and lifecycle events to verify correctness and diagnose issues (without personal data). |
| NFR-14 | **Maintainability** | Single codebase, single gameplay; regional differences isolated to dictionary data. |

---

## 2.10 Business Rules

Business rules are enumerated authoritatively in
[Business Rules Document](03-business-rules.md). The SRS references them; it does not
duplicate them. Every functional requirement above is governed by the corresponding rules
(e.g., FR-16 ↔ board composition rules, FR-27/28 ↔ win/loss rules).

---

## 2.11 External Interfaces (logical contracts)

Interfaces are described **logically** (intents and notifications), not as wire formats,
protocols, or endpoints.

### 2.11.1 Player-facing interface (intents the system accepts)
- Create Room (dictionary selection) → Room Code.
- Join Room (room code, nickname).
- Leave Room.
- Select Team; Select/claim Role.
- Start Match (Host only).
- Submit Clue (word, number) — Spymaster, active team.
- Submit Guess (card) — Operative, active team.
- End Turn — Operative, active team.
- Request Rematch / Start New Match (Host).
- Reconnect (room code, prior player identity token).

### 2.11.2 System-to-client notifications (authoritative state)
- Room state (players, teams, roles, host, status).
- Match started / board (role-filtered: key only to Spymasters).
- Clue given (word, number, active team).
- Card revealed (card, ownership now visible to all).
- Turn changed (new active team, remaining guesses reset).
- Score update (agents remaining per team).
- Match ended (winning team, reason).
- Connection state changes (player connected/disconnected/reconnected, host migrated).
- Errors / validation rejections (with reason codes).

### 2.11.3 Content interface
- Dictionary catalog (available regions and versions).
- Dictionary content (words for a region/version) — consumed by board generation only.

> **Role-filtered delivery is mandatory:** the system must never send unrevealed card
> ownership to a client whose player is not a Spymaster.

---

## 2.12 Security Considerations

(No authentication exists in this version; these are the integrity/abuse considerations.)

- **SEC-1 Authoritative server.** All adjudication occurs server-side; clients cannot
  alter game state except by submitting intents that are validated.
- **SEC-2 Information segregation.** The key (card ownership) is delivered only to
  Spymasters; Operative clients never receive unrevealed ownership data.
- **SEC-3 Room access control.** Possession of a valid, non-expired room code is the sole
  gate; codes are unguessable enough and rooms expire.
- **SEC-4 Action authorization.** Each intent is checked against the actor's current role,
  team, and the game state (e.g., only the active team's Spymaster may clue).
- **SEC-5 Reconnection identity.** A reconnecting player proves continuity via an opaque
  session/player token issued at join, scoped to that room, without personal data.
- **SEC-6 Abuse limits.** Rate/shape limits on room creation, joins, and intents to deter
  flooding (business intent; mechanism unspecified).
- **SEC-7 Content safety.** Only curated dictionaries are used; no user-supplied words.
- **SEC-8 No PII.** Only a transient nickname is stored for the room's lifetime.

## 2.13 Scalability Considerations

- **SCAL-1** Rooms are independent units of work and state; the system scales by the number
  of concurrent rooms, which do not share mutable state.
- **SCAL-2** A room's working state is bounded (≤ a small number of players, a single 25-card
  board), so per-room resource usage is small and predictable.
- **SCAL-3** Dictionary content is read-mostly and shared; it can be cached/replicated.
- **SCAL-4** Expiration of idle rooms reclaims resources and bounds total live state.
- **SCAL-5** Real-time delivery must fan out room events to that room's participants only.

## 2.14 Future Authentication Considerations

- **AUTH-1** Identity is modeled as an abstraction: a **Player** has a transient identity
  today; later it may be linked to a durable **Account** without changing the Player's role
  in game rules.
- **AUTH-2** Joining, team/role selection, and play **must not assume** persistent identity;
  introducing accounts must remain optional and additive.
- **AUTH-3** Reconnection tokens are already non-personal and room-scoped; an account system
  can later associate them with a durable user.
- **AUTH-4** No business rule, workflow, validation, or state machine may need modification
  to support authentication; auth attaches only at the identity boundary.
- **AUTH-5** Nickname uniqueness is per-room today; an account system can layer durable
  display names later without changing in-room uniqueness rules.

---

## 2.15 Logical architecture (system perspective)

*High-level components and responsibilities only. No technologies, deployment, or code.*

### 2.15.1 High-level components

| Component | Responsibility | Boundary |
|-----------|----------------|----------|
| **Room Service** | Owns room lifecycle: creation, codes, membership, host, expiration. | Does not adjudicate gameplay. |
| **Lobby / Setup Service** | Manages pre-match team and role configuration and start validation. | Hands a valid configuration to the Game Engine; no turn logic. |
| **Game Engine (Rules Authority)** | Generates board/key, enforces all rules, processes clues/guesses, computes turns and win/loss, produces Game Result. | Pure rules; language-independent; no transport, no UI, no identity. |
| **State & Session Store** | Holds authoritative room/game/connection state for the room's lifetime. | Storage of state only; no rules. |
| **Real-time Delivery / Notification** | Distributes authoritative, **role-filtered** state to participants; receives intents. | Transport boundary; never adjudicates. |
| **Dictionary / Content Provider** | Supplies versioned regional word lists for board generation. | Read-only content; no rules. |
| **Connection Manager** | Tracks per-player connectivity, drives disconnect/reconnect and host migration. | Connection state only; defers rule effects to Game Engine. |
| **Identity Abstraction** | Issues transient, room-scoped player identity/reconnect tokens. | The seam where future authentication attaches. |

### 2.15.2 Major subsystems and interactions

```
        intents                         role-filtered state
Clients ───────►  Real-time Delivery  ◄───────────────────────┐
                         │                                      │
                         ▼                                      │
                  ┌──────────────┐    config    ┌────────────────────┐
                  │ Room Service │──────────────►│ Lobby/Setup Service│
                  └──────┬───────┘               └─────────┬──────────┘
                         │ membership/host                  │ valid setup
                         ▼                                  ▼
                  ┌──────────────┐  reads words   ┌────────────────────┐
                  │ Connection   │                │   Game Engine       │
                  │ Manager      │───events──────►│  (Rules Authority)  │◄── Dictionary
                  └──────┬───────┘                └─────────┬──────────┘    Provider
                         │                                  │ state/result
                         └───────────► State & Session Store ◄───────────────┘
                                          ▲
                                  Identity Abstraction (transient tokens; future-auth seam)
```

### 2.15.3 Boundaries (invariants)

- The **Game Engine** is the only component that decides legality and outcome; it has **no
  knowledge** of transport, presentation, or language.
- The **Dictionary Provider** influences **only** which words appear; it cannot influence
  rules, turn flow, or outcomes.
- The **Real-time Delivery** layer enforces **role-based visibility** at the boundary but
  performs **no** adjudication.
- **Identity** is isolated so that authentication can be added at one seam (AUTH-1..5)
  without touching gameplay.

> This section deliberately omits any .NET solution structure, projects, layers, hosting,
> databases, or protocols. Those are implementation decisions outside the SRS.
