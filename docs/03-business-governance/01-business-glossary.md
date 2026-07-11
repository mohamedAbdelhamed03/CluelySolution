# 19. Business Glossary — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Provide the single authoritative definition for every business term used across the Cluely documentation package. Where document 01 introduced a short glossary, **this document supersedes it as the canonical terminology source**; all other documents should be read with these definitions. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [Reading the Entries](#3-reading-the-entries)
4. [Glossary](#4-glossary)
   - [Product & Structure](#41-product--structure)
   - [People & Roles](#42-people--roles)
   - [Board & Cards](#43-board--cards)
   - [Play Concepts](#44-play-concepts)
   - [Outcome Concepts](#45-outcome-concepts)
   - [Dictionary & Localization](#46-dictionary--localization)
   - [Identity & Session](#47-identity--session)
   - [Documentation Artefacts](#48-documentation-artefacts)
   - [Content Platform](#49-content-platform)
5. [Deprecated & Synonym Index](#5-deprecated--synonym-index)
6. [Revision History](#6-revision-history)

---

## 1. Purpose & Usage

Terminology drift is the most common source of ambiguity in a large specification. This
glossary fixes one precise meaning per term. Finding [F-VAL-01](../02-business-analysis/17-consistency-validation-report.md#4-findings)
noted "Game" and "Match" were used interchangeably; this glossary resolves that (see **Match**).
No definition here changes any business rule; it only names things consistently.

## 2. References
- [00 — README](../_meta/00-canonical-constants-and-index.md) (constants), [01 — BRD §Glossary](../01-product-discovery/01-business-requirements.md#111-business-glossary)
- [03 — Business Rules](../02-business-analysis/02-business-rules.md), [07 — Domain Model](../02-business-analysis/06-domain-model.md)
- [11 — Invariants](../02-business-analysis/10-business-invariants.md), [21 — Business Constants Catalog](03-business-constants-catalog.md)

## 3. Reading the Entries

Each entry includes: **Definition**, **Related Documents**, **Related Rules**, **Notes**,
**Synonyms**, **Deprecated terms**. Numeric values are **not** restated here — they live in the
[Business Constants Catalog (21)](03-business-constants-catalog.md).

---

## 4. Glossary

### 4.1 Product & Structure

#### Product (Cluely)
- **Definition:** The single global online multiplayer word-association game, functionally equivalent to Codenames, with one codebase and one gameplay worldwide.
- **Related Documents:** 01, 02. **Related Rules:** C-3/C-6. **Notes:** Only the word source is localized. **Synonyms:** Cluely. **Deprecated:** —

#### Room
- **Definition:** A private session container identified by a Room Code that groups Players and hosts one Match at a time, from creation to expiration.
- **Related Documents:** 07, 15. **Related Rules:** BR-RC-*, INV-R1..R5. **Notes:** May host multiple consecutive Matches. **Synonyms:** — **Deprecated:** —

#### Lobby
- **Definition:** The Room's pre-match / between-match phase in which Players gather, choose Teams and Roles, and the Host configures and starts the Match.
- **Related Documents:** 15, 08 (§8.1). **Related Rules:** BR-TA-3, BR-GS-*. **Notes:** Corresponds to Room states Lobby and Post-Match→reconfigure. **Synonyms:** — **Deprecated:** —

#### Match
- **Definition:** One complete play session on one Board, ending in a win, a loss, or abandonment. **Canonical term** for a single game instance.
- **Related Documents:** 03, 07, 08 (§8.2). **Related Rules:** BR-GS-*, BR-GE-*. **Notes:** Resolves F-VAL-01. **Synonyms:** *Game* (as a game instance). **Deprecated:** using "Game" to mean a single instance — prefer **Match**.

#### Game
- **Definition:** (a) The product's gameplay in general; (b) the Domain Model entity representing a Match instance ("Game (Match)").
- **Related Documents:** 07, 08. **Related Rules:** BR-GS-*. **Notes:** For a single instance, prefer **Match**. **Synonyms:** Match (sense b). **Deprecated:** —

#### Round
- **Definition:** A bookkeeping pairing of two consecutive Turns (one per Team). Never gates play on its own.
- **Related Documents:** 07, 08 (§8.5), 12. **Related Rules:** BR-TO-2. **Notes:** A Match ending mid-round completes the Round as final. **Synonyms:** — **Deprecated:** —

#### Room Capacity
- **Definition:** The maximum number of members a Room may hold at once.
- **Related Documents:** 15, 21. **Related Rules:** BR-JR-5, INV-R5, V-CAP-1. **Notes:** Value in [Constants (21)](03-business-constants-catalog.md) as `ROOM_MAX_PLAYERS`. **Synonyms:** Max Players. **Deprecated:** —

### 4.2 People & Roles

#### Player
- **Definition:** A temporary participant in a Room, identified only by a Nickname for the Room's lifetime.
- **Related Documents:** 07, 16. **Related Rules:** BR-JR-*, INV-P2. **Notes:** Holds one Team and one Role per Match. **Synonyms:** Participant. **Deprecated:** —

#### Temporary Player / Temporary Identity
- **Definition:** The no-account identity model in which a Player exists only for the duration of Room participation, carrying no personal data.
- **Related Documents:** 16, 02 (§2.14). **Related Rules:** C-7, BR-JR-7, INV-P2. **Notes:** Future-auth attaches here without rule changes. **Synonyms:** Transient identity. **Deprecated:** —

#### Waiting Player / Waiting Member
- **Definition:** A Player who is in the Room but not yet participating — either unassigned to a Team in Lobby, or a person who joined during an in-progress Match and must wait for the next Match.
- **Related Documents:** 15 (LR-2, LM-2), 03 (BR-JR-6a). **Related Rules:** BR-JR-6/6a. **Notes:** Receives no Board/Card data during an in-progress Match (INV-B9). **Synonyms:** Spectator-in-waiting (informal — **not** a spectator feature). **Deprecated:** —

#### Host
- **Definition:** The single Player holding Room-control privileges (configure, start, rematch, Lobby-only removal). Transferable via Host migration.
- **Related Documents:** 15 (§7), 07. **Related Rules:** BR-RC-6, BR-HM-*, INV-R1. **Notes:** Always a current Room member. **Synonyms:** Room owner. **Deprecated:** —

#### Team
- **Definition:** One of exactly two opposing sides in a Match.
- **Related Documents:** 03, 07. **Related Rules:** BR-TA-1, INV-T1. **Notes:** Instantiated as Red and Blue. **Synonyms:** Side. **Deprecated:** —

#### Red Team / Blue Team
- **Definition:** The two canonical Team labels. They carry **no** gameplay asymmetry beyond the Starting Team's 9-vs-8 Agent split for a given Match.
- **Related Documents:** 00, 07. **Related Rules:** BR-TA-1, BR-BG-3/4. **Notes:** Colours are cosmetic identifiers; either can be the Starting Team. **Synonyms:** — **Deprecated:** "Team A / Team B" — prefer **Red / Blue**.

#### Spymaster
- **Definition:** The single member of a Team who can see the Key and gives Clues.
- **Related Documents:** 03, 07. **Related Rules:** BR-RO-1, BR-CL-1, INV-T3. **Notes:** Exactly one per Team per Match; never guesses. **Synonyms:** Spy chief, clue-giver. **Deprecated:** —

#### Operative
- **Definition:** A Team member who sees only the words (no unrevealed ownership) and makes Guesses based on the Clue.
- **Related Documents:** 03, 07. **Related Rules:** BR-RO-2, BR-GV-1. **Notes:** At least one per Team; never sees the Key. **Synonyms:** Field agent, guesser. **Deprecated:** —

### 4.3 Board & Cards

#### Board
- **Definition:** The fixed 5×5 arrangement of 25 Word Cards for a Match.
- **Related Documents:** 03, 07. **Related Rules:** BR-BG-1, INV-B1/B8. **Notes:** Immutable once generated. **Synonyms:** Grid. **Deprecated:** —

#### Word Card
- **Definition:** One Board position bearing a word and a hidden Card Ownership.
- **Related Documents:** 07, 08 (§8.6). **Related Rules:** BR-CO-1, INV-B4. **Notes:** Either Unrevealed or Revealed. **Synonyms:** Card, tile. **Deprecated:** —

#### Card Ownership
- **Definition:** The classification of a Word Card as Red Agent, Blue Agent, Neutral, or Assassin.
- **Related Documents:** 03, 07. **Related Rules:** BR-CO-1/2, INV-B4/B5. **Notes:** Immutable; public only when Revealed. **Synonyms:** Card identity, allegiance. **Deprecated:** —

#### Agent Card
- **Definition:** A Word Card belonging to a Team (Red or Blue).
- **Related Documents:** 03. **Related Rules:** BR-BG-3, BR-CG-*. **Notes:** 9 for the Starting Team, 8 for the other. **Synonyms:** Team card. **Deprecated:** —

#### Neutral Card
- **Definition:** A Word Card belonging to no Team; revealing it ends the Turn.
- **Related Documents:** 03. **Related Rules:** BR-NC-*. **Notes:** 7 per Board. **Synonyms:** Bystander, innocent. **Deprecated:** —

#### Assassin
- **Definition:** The single Word Card that causes the guessing Team to lose the Match instantly when Revealed.
- **Related Documents:** 03, 17. **Related Rules:** BR-ASN-*, INV-B3/O3. **Notes:** Exactly one; overrides all other outcomes. **Synonyms:** Assassin card, black card. **Deprecated:** —

#### Key / Key Card
- **Definition:** The secret map of every Card's Ownership, visible only to Spymasters.
- **Related Documents:** 03, 07. **Related Rules:** BR-BG-6, INV-B9. **Notes:** Both Spymasters see the full Key. **Synonyms:** Key map, spymaster map. **Deprecated:** —

#### Reveal
- **Definition:** The act of exposing a Word Card's Ownership when it is Guessed; a one-way, permanent state change.
- **Related Documents:** 03, 08 (§8.6). **Related Rules:** BR-GV-5, INV-B7. **Notes:** Public to all once done. **Synonyms:** Uncover, flip. **Deprecated:** —

### 4.4 Play Concepts

#### Turn
- **Definition:** One Team's full opportunity: a Clue phase (Spymaster) followed by a Guessing phase (Operatives), until the Turn ends.
- **Related Documents:** 03, 08 (§8.3). **Related Rules:** BR-TO-3, BR-TE-*. **Notes:** Exactly one active Turn at a time. **Synonyms:** — **Deprecated:** —

#### Turn State
- **Definition:** The phase of the current Turn: AwaitingClue, AwaitingGuess, or TurnEnded (with a transient Paused overlay).
- **Related Documents:** 08 (§8.3). **Related Rules:** BR-CL-1, BR-GV-1. **Notes:** — **Synonyms:** Turn phase. **Deprecated:** —

#### Clue
- **Definition:** The Spymaster's instruction to their Operatives: exactly one word plus a Hint Number.
- **Related Documents:** 03, 07. **Related Rules:** BR-CL-*, INV-G3. **Notes:** One active Clue per Turn; immutable once given. **Synonyms:** Hint. **Deprecated:** —

#### Hint Number (Clue Number)
- **Definition:** The number accompanying a Clue word: an integer ≥ 0, or "unlimited". It defines the guess allowance for the Turn.
- **Related Documents:** 03, 21. **Related Rules:** BR-CL-5, BR-GV-7. **Notes:** N≥1 → up to N+1 Guesses; 0/unlimited → unbounded (min 1). **Synonyms:** Clue count. **Deprecated:** —

#### Guess
- **Definition:** An Operative's selection of an Unrevealed Word Card to Reveal.
- **Related Documents:** 03, 07. **Related Rules:** BR-GV-*, INV-G6. **Notes:** At least one required per Turn. **Synonyms:** Pick, selection. **Deprecated:** —

#### Game State
- **Definition:** The complete current condition of a Match (Board role-filtered, active Team, Turn/phase, remaining Agent counts, active Clue, status) as delivered to clients.
- **Related Documents:** 07, 02. **Related Rules:** BR-CO-4, NFR-3. **Notes:** Always role-filtered. **Synonyms:** Match state. **Deprecated:** —

### 4.5 Outcome Concepts

#### Winner
- **Definition:** The Team that first has all its Agent Cards Revealed, or whose opponent Reveals the Assassin.
- **Related Documents:** 03, 17. **Related Rules:** BR-WIN-*, INV-O1. **Notes:** Exactly one per play-completed Match. **Synonyms:** Winning Team. **Deprecated:** —

#### Loser
- **Definition:** The Team that Reveals the Assassin, or whose opponent completes its Agents first.
- **Related Documents:** 03. **Related Rules:** BR-LOSE-*. **Notes:** Complement of Winner. **Synonyms:** Losing Team. **Deprecated:** —

#### Abandonment
- **Definition:** A Match that cannot continue (a Team lost required composition after grace) and ends with **no play-based Winner**.
- **Related Documents:** 16 (PS-25), 03. **Related Rules:** BR-GE-5, BR-RX-3, BR-TIE-3. **Notes:** Distinct from a win/loss; not a draw. **Synonyms:** Forfeit-by-abandon. **Deprecated:** —

#### Game Result
- **Definition:** The recorded outcome of a Match: winning Team, losing Team, reason (all-agents / assassin / abandonment), and final Board snapshot.
- **Related Documents:** 07, 12 (EVT-21). **Related Rules:** BR-GE-3, INV-O4. **Notes:** Immutable once recorded. **Synonyms:** Result, outcome. **Deprecated:** —

### 4.6 Dictionary & Localization

#### Dictionary
- **Definition:** A curated word source. In context, usually a Country Dictionary.
- **Related Documents:** 14, 07. **Related Rules:** BR-BG-9, INV-D1. **Notes:** Affects words only, never rules. **Synonyms:** Word set, word library. **Deprecated:** —

#### Country Dictionary
- **Definition:** The curated word set for a specific country/region (e.g., Egypt, Saudi Arabia, USA, France, Germany).
- **Related Documents:** 14. **Related Rules:** DM-C1. **Notes:** One per region; independent of others. **Synonyms:** Regional dictionary. **Deprecated:** —

#### Dictionary Version
- **Definition:** An immutable, published snapshot of a Country Dictionary's word list.
- **Related Documents:** 14. **Related Rules:** DM-V1, INV-D3. **Notes:** A Match binds to one Version for its whole life. **Synonyms:** Word set version. **Deprecated:** —

#### Localized Dictionary
- **Definition:** The concept that the word source is the only localized component of Cluely; gameplay is identical everywhere.
- **Related Documents:** 01, 14. **Related Rules:** C-4, INV-D1. **Notes:** Localization ≠ rule change. **Synonyms:** Localization. **Deprecated:** —

### 4.7 Identity & Session

#### Session (Player Session)
- **Definition:** A Player's live participation in a Room, from join to removal or Room expiry.
- **Related Documents:** 16. **Related Rules:** BR-JR-7, INV-P2. **Notes:** Never outlives its Room. **Synonyms:** Player session. **Deprecated:** —

#### Reconnect Token
- **Definition:** A transient, Room-scoped, PII-free credential issued at join that lets a returning client resume the same Player within the grace period.
- **Related Documents:** 16 (§3). **Related Rules:** BR-JR-7, BR-DC-2, INV-P2. **Notes:** Sole means of resuming a seat. **Synonyms:** Session token, resume token. **Deprecated:** —

#### Room Code
- **Definition:** The short, unique, non-sequential, shareable code used to join a live Room.
- **Related Documents:** 07, 15. **Related Rules:** BR-RC-2/3, INV-R2. **Notes:** Released for reuse on expiry. **Synonyms:** Join code, invite code. **Deprecated:** —

### 4.8 Documentation Artefacts

#### Business Rule
- **Definition:** A normative statement of what may/must/must-not happen in the business (IDs `BR-*`).
- **Related Documents:** 03. **Notes:** The authoritative rule set. **Synonyms:** Rule. **Deprecated:** —

#### Validation Rule
- **Definition:** A check applied to an intent, with a reason and business outcome (IDs `V-*`).
- **Related Documents:** 10. **Notes:** Maps to Business Errors. **Synonyms:** Validation. **Deprecated:** —

#### Invariant
- **Definition:** A condition that must always remain true across the system's lifetime (IDs `INV-*`).
- **Related Documents:** 11. **Notes:** Violation = defect. **Synonyms:** Business invariant. **Deprecated:** —

#### Domain Event
- **Definition:** A past-tense business fact emitted when state changes (IDs `EVT-*`).
- **Related Documents:** 12. **Notes:** Business, not infrastructure. **Synonyms:** Event. **Deprecated:** —

#### Business Error
- **Definition:** A named business reason an intent was rejected (UPPER_SNAKE_CASE codes).
- **Related Documents:** 13. **Notes:** The error reference for clients. **Synonyms:** Error code. **Deprecated:** —

#### Workflow
- **Definition:** An end-to-end business process flow (IDs `WF-*`).
- **Related Documents:** 09. **Notes:** — **Synonyms:** Process flow. **Deprecated:** —

#### Use Case
- **Definition:** A detailed actor-goal interaction with success/alternative/exception flows (IDs `UC-*`).
- **Related Documents:** 06. **Notes:** — **Synonyms:** — **Deprecated:** —

#### Acceptance Criteria
- **Definition:** The Given/When/Then conditions that confirm a User Story is satisfied.
- **Related Documents:** 05. **Notes:** Testable by QA. **Synonyms:** AC. **Deprecated:** —

### 4.9 Content Platform

*Post-MVP feature terms, governed by [ADR-011](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md)
and the [Content Platform Feature Specification](../15-content-platform/02-feature-specification.md).
"Dictionary" and "Dictionary Version" (§4.6) keep their definitions; here they generalize to owned, typed content.*

#### Draft
- **Definition:** The single mutable working copy of a Dictionary's words; the only editable content.
- **Related Documents:** ADR-011, Feature Spec §5. **Notes:** Never used by a match. **Synonyms:** — **Deprecated:** —

#### Dictionary Owner
- **Definition:** The single account with exclusive authoring, visibility, sharing, and lifecycle rights over a Dictionary.
- **Related Documents:** ADR-011 §6.3. **Notes:** Exactly one per Dictionary (BR-CONTENT-001). **Synonyms:** Owner. **Deprecated:** —

#### Visibility
- **Definition:** A Dictionary's controlled exposure: Private (default), Shared, or Public (Organization reserved).
- **Related Documents:** Feature Spec §7. **Notes:** Private content never leaks. **Synonyms:** — **Deprecated:** —

#### Content Type
- **Definition:** A Dictionary's classification by ownership: Official or User (more added additively).
- **Related Documents:** ADR-011 §10. **Notes:** One-per-region (DM-C1) applies to Official only. **Synonyms:** — **Deprecated:** —

#### Share
- **Definition:** Granting Viewer access to a Dictionary's current published Version to specific accounts; revocable.
- **Related Documents:** Feature Spec §11. **Notes:** Recipients cannot edit or re-share. **Synonyms:** — **Deprecated:** —

#### Clone
- **Definition:** Creating a new, independent, owner-held Dictionary seeded from a Version's words plus a provenance reference.
- **Related Documents:** Feature Spec §11. **Notes:** Independent of its source. **Synonyms:** — **Deprecated:** —

#### Publish
- **Definition:** Validating a Draft and snapshotting it into a new immutable Version.
- **Related Documents:** Feature Spec §8. **Notes:** Corrections are new Versions, never in-place edits. **Synonyms:** — **Deprecated:** —

#### Moderation Review
- **Definition:** The Public-only lifecycle gate approving a published Version for discoverability.
- **Related Documents:** Feature Spec §18. **Notes:** Distinct from future user "reviews". **Synonyms:** Review. **Deprecated:** —

---

## 5. Deprecated & Synonym Index

| Preferred term | Synonyms accepted | Deprecated (avoid) |
|----------------|-------------------|--------------------|
| Match | Game (as an instance) | "Game" for a single instance |
| Red Team / Blue Team | Side | Team A / Team B |
| Hint Number | Clue count | — |
| Neutral Card | Bystander, innocent | — |
| Assassin | Black card | — |
| Waiting Player | Waiting member | "Spectator" (no spectator feature exists) |
| Reconnect Token | Session/resume token | — |
| Operative | Field agent, guesser | — |
| Spymaster | Clue-giver, spy chief | — |

## 6. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial canonical glossary; supersedes the short glossary in [01 §1.11](../01-product-discovery/01-business-requirements.md#111-business-glossary); resolves F-VAL-01 (Match vs Game) and F-VAL-04 (Red/Blue). |
| 1.1 | 2026-07-11 | Content Platform Slice 00: added §4.9 Content Platform terms (Draft, Dictionary Owner, Visibility, Content Type, Share, Clone, Publish, Moderation Review). No change to existing entries. |
