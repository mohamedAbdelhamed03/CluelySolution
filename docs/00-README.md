# Cluely — Business & Product Documentation

Cluely is an online, multiplayer, word-association party game whose gameplay is
**functionally equivalent to the Codenames board game**. This documentation set is the
**single source of truth** for the business of the game. It is intended for product
owners, business analysts, architects, developers, QA engineers, and designers.

> **Scope note.** These documents describe *business and behaviour only*. They do not
> prescribe implementation technologies, code, UI design, deployment topology, or new
> gameplay. The intended delivery platform context (informational only) is a **.NET**
> backend and a **Flutter** mobile client; no document below depends on that choice — all
> business rules are technology-neutral and language-neutral.

## Reference baseline

The **Codenames** board game is the functional reference. Where the original rules are
ambiguous, this documentation chooses the interpretation that matches the physical board
game. No mechanic has been invented, removed, or simplified.

## Document index

| # | Document | Purpose |
|---|----------|---------|
| 1 | [Business Requirements Document](01-BRD.md) | Why the product exists; vision, objectives, scope, risks. |
| 2 | [Software Requirements Specification](02-SRS.md) | IEEE-style functional & non-functional requirements and logical architecture. |
| 3 | [Business Rules Document](03-business-rules.md) | Every rule, validation, and state transition. |
| 4 | [Functional Requirements](04-functional-requirements.md) | Each feature with flows, pre/postconditions, failures. |
| 5 | [User Stories](05-user-stories.md) | Stories per actor with acceptance criteria. |
| 6 | [Use Cases](06-use-cases.md) | Detailed use cases with success/alternative/exception flows. |
| 7 | [Domain Model](07-domain-model.md) | Business entities, responsibilities, relationships, constraints. |
| 8 | [State Machines](08-state-machines.md) | Lifecycle states and transitions for every stateful entity. |
| 9 | [Business Workflows](09-business-workflows.md) | End-to-end process flows. |
| 10 | [Validation Rules](10-validation-rules.md) | Every validation, its reason, and business outcome. |

## Canonical constants (used consistently across all documents)

| Constant | Value | Meaning |
|----------|-------|---------|
| `BOARD_SIZE` | 25 | Word cards on the board (5×5 grid). |
| `STARTING_TEAM_AGENTS` | 9 | Agent cards owned by the team that plays first. |
| `SECOND_TEAM_AGENTS` | 8 | Agent cards owned by the team that plays second. |
| `NEUTRAL_CARDS` | 7 | Bystander cards owned by no team. |
| `ASSASSIN_CARDS` | 1 | The single losing card. |
| `TEAMS_PER_GAME` | 2 | Two opposing teams (referred to as Red and Blue). |
| `SPYMASTERS_PER_TEAM` | 1 | Exactly one Spymaster per team. |
| `MIN_OPERATIVES_PER_TEAM` | 1 | At least one Operative per team. |
| `MIN_PLAYERS` | 4 | Minimum to start (2 teams × {1 Spymaster + 1 Operative}). |
| `GUESS_BONUS` | +1 | Operatives may guess (clue number + 1) times. |

Note: `9 + 8 + 7 + 1 = 25`. The split (9 vs 8) is fixed; the team that receives 9 agents
is the **starting team** for that match.

## Configurable operational parameters

These values are operational tunables (not gameplay rules); they do not affect Codenames
fidelity. Defaults are recommendations for the MVP; the **allowed range** bounds what an
operator may set. Governing document is noted for traceability.

| Parameter | Default | Allowed range | Meaning | Governed by |
|-----------|---------|---------------|---------|-------------|
| `ROOM_MAX_PLAYERS` | 10 | 4–20 | Maximum members per room. | BR-JR-5, V-CAP-1 |
| `RECONNECT_GRACE_PERIOD` | 60 s | 15–180 s | Window for a disconnected player to reconnect and resume role before removal. | BR-DC-2/5, V-RECON-2 |
| `HOST_MIGRATION_GRACE` | 60 s | 15–180 s | Delay before a disconnected Host's privileges migrate (tolerates brief drops). | BR-DC-8, BR-HM-1 |
| `ROOM_IDLE_EXPIRY` | 30 min | 5–120 min | Inactivity period after which a room expires. | BR-RX-1, V-EXP-1 |
| `ROOM_CODE_LENGTH` | 6 chars | 4–8 chars | Length of the room code. | BR-RC-2/3 |
| `ROOM_CODE_ALPHABET` | Uppercase letters + digits, ambiguity-reduced (no `0/O`, `1/I/L`) | — | Character set for room codes; must yield a non-sequential, unguessable space. | BR-RC-2, R-3 |
| `NICKNAME_MIN_LENGTH` | 1 | 1–3 | Minimum nickname length (after trim). | V-NICK-1 |
| `NICKNAME_MAX_LENGTH` | 20 | 8–32 | Maximum nickname length. | V-NICK-2 |
| `DICTIONARY_MIN_WORDS` | 25 | ≥25 (hard floor) | Minimum usable words a dictionary version must supply to be playable. | BR-GS-3, V-DICT-2 |

Changing any parameter within its allowed range alters operational behaviour only; it never
changes the rules, counts, turn flow, or win/loss outcomes of the game.
