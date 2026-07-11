# 11. Business Invariants — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Define every condition that MUST always hold true across the lifetime of the system, independent of any single workflow. Invariants are the safety net QA and architects check against; if any invariant is violated, the system is in an illegal state. |
| **Technology** | Neutral (no code, .NET, database, or API references). |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [Terminology](#3-terminology)
4. [Invariant Structure](#4-invariant-structure)
5. [Board & Card Invariants](#5-board--card-invariants)
6. [Game & Turn Invariants](#6-game--turn-invariants)
7. [Team & Role Invariants](#7-team--role-invariants)
8. [Room & Host Invariants](#8-room--host-invariants)
9. [Player & Connection Invariants](#9-player--connection-invariants)
10. [Dictionary Invariants](#10-dictionary-invariants)
11. [Result & Outcome Invariants](#11-result--outcome-invariants)
12. [Invariant Enforcement Summary](#12-invariant-enforcement-summary)

---

## 1. Purpose & Usage

An **invariant** is a truth that holds continuously, not just at the moment of a transition.
Business Rules (document 03) describe *what may happen*; invariants describe *what must never
stop being true*. Every invariant here is derived from — and cross-referenced to — existing
rules. This document introduces **no new rules**; it consolidates guarantees.

## 2. References

- [03 — Business Rules](02-business-rules.md)
- [07 — Domain Model](06-domain-model.md) (§7.3 Key invariants)
- [08 — State Machines](07-state-machines.md)
- [00 — README](../_meta/00-canonical-constants-and-index.md) (canonical constants)

## 3. Terminology

Terms follow the [BRD glossary](../01-product-discovery/01-business-requirements.md#111-business-glossary). Notable: *board generation*
is the instant a match's board and key are created; *reveal* is the exposure of a card's
ownership; *terminal condition* is any win/loss/assassin/abandonment event.

## 4. Invariant Structure

Each invariant lists: **ID**, **Description**, **Reason**, **Becomes true when**,
**Can no longer change when**, **Related Rules**.

- *Becomes true when* = the earliest point the invariant is established.
- *Can no longer change when* = the point after which the value is frozen (for immutability
  invariants) or the scope in which it must continuously hold.

---

## 5. Board & Card Invariants

### INV-B1 — Exactly 25 cards
- **Description:** A generated board always contains exactly 25 word cards.
- **Reason:** Faithful 5×5 Codenames board.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** For the entire match; the count never changes.
- **Related Rules:** BR-BG-1, INV of §7.3.

### INV-B2 — Ownership partition is 9 / 8 / 7 / 1
- **Description:** Card ownership always partitions into 9 starting-team agents, 8 second-team agents, 7 neutral, 1 assassin (sum 25).
- **Reason:** Faithful card distribution.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** For the entire match.
- **Related Rules:** BR-BG-3, V-BOARD-1.

### INV-B3 — Exactly one Assassin
- **Description:** A board has exactly one assassin card at all times.
- **Reason:** The single instant-loss card defines the game's core tension.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** For the entire match.
- **Related Rules:** BR-ASN-1, BR-BG-3.

### INV-B4 — Every card has exactly one ownership
- **Description:** Each card is exactly one of {Red agent, Blue agent, Neutral, Assassin}.
- **Reason:** No card may be ambiguous or unassigned.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** Immutable for the match.
- **Related Rules:** BR-CO-1, BR-BG-3.

### INV-B5 — Ownership is immutable after generation
- **Description:** A card's ownership never changes once the key is generated.
- **Reason:** Fairness; the key is fixed.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** Immediately and permanently for the match.
- **Related Rules:** BR-CO-2, BR-BG-8.

### INV-B6 — Words are distinct on a board
- **Description:** No two cards on the same board bear the same word.
- **Reason:** Avoids ambiguous clues/guesses.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** For the entire match.
- **Related Rules:** BR-BG-1/2, V-DICT-2.

### INV-B7 — Reveal is monotonic (one-way)
- **Description:** A revealed card can never become hidden again.
- **Reason:** Information, once public, stays public; prevents state rollback exploits.
- **Becomes true when:** A card is guessed and revealed.
- **Can no longer change when:** Once revealed, permanently revealed for the match.
- **Related Rules:** BR-GV-5, BR-CO-3, WordCard state (§8.6).

### INV-B8 — Board contents never change during a match
- **Description:** The set of words and their positions never change once the match starts.
- **Reason:** Board is the shared, fixed playing field.
- **Becomes true when:** Board generation completes.
- **Can no longer change when:** For the entire match.
- **Related Rules:** BR-BG-8.

### INV-B9 — Unrevealed ownership is never disclosed to Operatives
- **Description:** At no time does any interface expose an unrevealed card's ownership to an Operative or waiting member.
- **Reason:** Core fairness; the whole game depends on hidden information.
- **Becomes true when:** Board generation completes (key delivered to Spymasters only).
- **Can no longer change when:** Continuously true for the match.
- **Related Rules:** BR-CO-4, BR-BG-6, BR-JR-6a, NFR-3.

---

## 6. Game & Turn Invariants

### INV-G1 — At most one active match per room
- **Description:** A room has zero or one active match at any moment, never more.
- **Reason:** A room is a single shared game surface.
- **Becomes true when:** Room creation.
- **Can no longer change when:** Continuously true for the room's life.
- **Related Rules:** BR-GS-4, Room state (§8.1), §7.3.

### INV-G2 — Exactly one active turn
- **Description:** During an in-progress match, exactly one turn is active, belonging to exactly one team.
- **Reason:** Strict alternation; no parallel play.
- **Becomes true when:** Match starts (first turn).
- **Can no longer change when:** Continuously until the match finishes.
- **Related Rules:** BR-TO-1/2/4, Turn state (§8.3).

### INV-G3 — At most one active clue
- **Description:** At any moment there is at most one active clue, owned by the active team's Spymaster, for the current turn only.
- **Reason:** One clue governs one guessing phase.
- **Becomes true when:** A valid clue is submitted.
- **Can no longer change when:** Cleared when the turn ends; a new turn starts with no clue.
- **Related Rules:** BR-CL-7, Turn state (§8.3).

### INV-G4 — Only the active team may act
- **Description:** Only members of the active team can submit a clue (Spymaster) or guess (Operative) at any moment.
- **Reason:** Turn integrity.
- **Becomes true when:** A turn begins.
- **Can no longer change when:** Continuously true per turn.
- **Related Rules:** BR-TO-5, BR-CL-1, BR-GV-1.

### INV-G5 — At least one guess before voluntary turn end
- **Description:** A turn cannot be ended voluntarily with zero guesses after a clue.
- **Reason:** Faithful Codenames — a team must attempt at least one guess.
- **Becomes true when:** A clue becomes active.
- **Can no longer change when:** Enforced throughout the guessing phase.
- **Related Rules:** BR-GV-6, BR-TE-6, V-ENDTURN-1.

### INV-G6 — Guess count never exceeds the clue allowance
- **Description:** The number of guesses in a turn never exceeds (clue number + 1), or is unbounded only when the clue number is 0 or unlimited.
- **Reason:** Faithful guessing limit.
- **Becomes true when:** A clue becomes active.
- **Can no longer change when:** Enforced per turn.
- **Related Rules:** BR-GV-7, V-GUESS-4.

### INV-G7 — A finished match cannot resume
- **Description:** Once a match reaches a terminal condition it never returns to In-Progress; no further clue/guess is accepted.
- **Reason:** Result integrity.
- **Becomes true when:** A terminal condition is met.
- **Can no longer change when:** Permanently after finish.
- **Related Rules:** BR-GE-1/2, V-STATE-1, Game state (§8.2).

### INV-G8 — Turns strictly alternate
- **Description:** No team takes two consecutive turns; play alternates Red/Blue.
- **Reason:** Faithful turn order.
- **Becomes true when:** Match starts.
- **Can no longer change when:** Continuously true for the match.
- **Related Rules:** BR-TO-2, Round state (§8.5).

---

## 7. Team & Role Invariants

### INV-T1 — Exactly two teams
- **Description:** A match always has exactly two teams (Red and Blue).
- **Reason:** Faithful two-sided game.
- **Becomes true when:** Match configuration (Lobby) / match start.
- **Can no longer change when:** For the entire match.
- **Related Rules:** BR-TA-1.

### INV-T2 — A player belongs to at most one team
- **Description:** No player is on two teams simultaneously.
- **Reason:** Prevents divided loyalty and key leakage.
- **Becomes true when:** Team assignment.
- **Can no longer change when:** Locked during a match; changeable only between matches.
- **Related Rules:** BR-TA-2/4, INV-T5.

### INV-T3 — Each team has at most one Spymaster
- **Description:** No team ever has two Spymasters.
- **Reason:** One key-holder/clue-giver per team.
- **Becomes true when:** Role assignment.
- **Can no longer change when:** Locked during a match.
- **Related Rules:** BR-RO-1/6, V-ROLE-1.

### INV-T4 — Each player has exactly one role per match
- **Description:** A participating player is exactly one of {Spymaster, Operative} for the match.
- **Reason:** No dual roles; view/permissions are unambiguous.
- **Becomes true when:** Match start (roles locked).
- **Can no longer change when:** For the entire match.
- **Related Rules:** BR-RO-3/5, V-ROLE-2.

### INV-T5 — Team/role composition is frozen during a match
- **Description:** Team membership and roles do not change while a match is in progress.
- **Reason:** The Spymaster has seen the key; changing would corrupt fairness.
- **Becomes true when:** Match start.
- **Can no longer change when:** Until the match finishes (then editable in Lobby/Post-Match).
- **Related Rules:** BR-TA-4, BR-RO-5, BR-GS-5.

### INV-T6 — Valid start composition
- **Description:** A match only ever runs with each team holding exactly one Spymaster and at least one Operative, and total players ≥ 4.
- **Reason:** Minimum playable game.
- **Becomes true when:** Match start validation passes.
- **Can no longer change when:** Must remain satisfied during the match, else abandonment applies.
- **Related Rules:** BR-GS-1, BR-DC-5, V-START-2/3.

---

## 8. Room & Host Invariants

### INV-R1 — Every room has exactly one Host
- **Description:** A live room always has exactly one Host — never zero, never two.
- **Reason:** Single point of room control.
- **Becomes true when:** Room creation.
- **Can no longer change when:** Continuously true while the room lives (Host transfers atomically).
- **Related Rules:** BR-RC-6, BR-HM-4, V-HOST-2.

### INV-R2 — Room code uniqueness among live rooms
- **Description:** No two live rooms share a room code.
- **Reason:** Unambiguous join target.
- **Becomes true when:** Room creation.
- **Can no longer change when:** For the room's life; released only on expiry.
- **Related Rules:** BR-RC-2/3, BR-RX-4.

### INV-R3 — The Host is always a member of its room
- **Description:** The Host is one of the room's current players.
- **Reason:** Control cannot rest with a non-member.
- **Becomes true when:** Room creation / host migration.
- **Can no longer change when:** Continuously true; on Host leaving, migration re-establishes it.
- **Related Rules:** BR-RC-7, BR-HM-1/2.

### INV-R4 — An empty room does not remain live
- **Description:** A room with zero members is not in a live state.
- **Reason:** Reclaim abandoned rooms.
- **Becomes true when:** Last member leaves.
- **Can no longer change when:** Room transitions to Expired.
- **Related Rules:** BR-LR-6, BR-RX-2.

### INV-R5 — Room capacity is never exceeded
- **Description:** Member count never exceeds `ROOM_MAX_PLAYERS`.
- **Reason:** Bounded room size.
- **Becomes true when:** Room creation.
- **Can no longer change when:** Continuously enforced on join.
- **Related Rules:** BR-JR-5, V-CAP-1.

---

## 9. Player & Connection Invariants

### INV-P1 — Nickname uniqueness within a room
- **Description:** No two current members of a room share a nickname (case-insensitive, trimmed).
- **Reason:** In-room identification.
- **Becomes true when:** Player join.
- **Can no longer change when:** Continuously enforced for the room's life.
- **Related Rules:** BR-JR-3, V-NICK-3, INV of §7.3.

### INV-P2 — Identity is transient and PII-free
- **Description:** A player identity exists only for the room's lifetime and carries no personal data.
- **Reason:** No-auth MVP; privacy.
- **Becomes true when:** Player join (token issued).
- **Can no longer change when:** Discarded on removal/expiry; attachable to accounts only in a future version.
- **Related Rules:** C-7, BR-JR-7, AUTH-1.

### INV-P3 — Disconnect does not immediately remove a player
- **Description:** A disconnected player remains part of the room until the grace period expires.
- **Reason:** Tolerate transient drops.
- **Becomes true when:** Disconnect detected.
- **Can no longer change when:** Resolved on reconnect (restored) or grace expiry (removed).
- **Related Rules:** BR-DC-1/2, Connection state (§8.4).

### INV-P4 — One active connection per player identity
- **Description:** A player identity maps to at most one active connection at a time within a room.
- **Reason:** Prevents duplicate/ghost sessions and conflicting actions.
- **Becomes true when:** Player join / reconnect.
- **Can no longer change when:** Continuously true; a new connection for the same identity supersedes the prior one.
- **Related Rules:** BR-DC-2/7 (see [16 — Player Session](15-player-session-reconnection.md)).

### INV-P5 — Role-appropriate view is always preserved
- **Description:** At every moment a player sees exactly the information their role permits (Spymaster: key; Operative/waiting: no unrevealed ownership).
- **Reason:** Fairness across connects/reconnects.
- **Becomes true when:** Match start / on each (re)connection.
- **Can no longer change when:** Continuously true.
- **Related Rules:** BR-DC-7, BR-CO-4, NFR-3.

---

## 10. Dictionary Invariants

### INV-D1 — Dictionary affects words only
- **Description:** The selected dictionary changes only which words appear; it never changes counts, flow, or outcomes.
- **Reason:** One gameplay worldwide.
- **Becomes true when:** Always (design invariant).
- **Can no longer change when:** Permanently; no rule may depend on language.
- **Related Rules:** BR-BG-9, NFR-6/7, FR-36.

### INV-D2 — A playable dictionary version supplies ≥ 25 distinct words
- **Description:** Any dictionary version used for a match provides at least `DICTIONARY_MIN_WORDS` distinct usable words.
- **Reason:** A full board needs 25 unique words.
- **Becomes true when:** Dictionary version publication / selection validation.
- **Can no longer change when:** Enforced at match start.
- **Related Rules:** BR-GS-3, V-DICT-2 (see [14 — Dictionary Management](13-dictionary-management.md)).

### INV-D3 — A match's dictionary version is fixed once started
- **Description:** An in-progress match keeps the exact dictionary version it started with, even if the dictionary is updated.
- **Reason:** Board reproducibility and fairness mid-match.
- **Becomes true when:** Match start.
- **Can no longer change when:** For the entire match.
- **Related Rules:** FR-37, F-16 (exception), BR-BG-8.

---

## 11. Result & Outcome Invariants

### INV-O1 — Exactly one winner per completed match
- **Description:** Every match that finishes by play has exactly one winning team and one losing team; there is no draw.
- **Reason:** Sequential reveals make simultaneous completion impossible.
- **Becomes true when:** A terminal play condition is met.
- **Can no longer change when:** Permanently once recorded.
- **Related Rules:** BR-WIN-3, BR-TIE-1/2, V-END-1.

### INV-O2 — Terminal conditions are evaluated after every reveal
- **Description:** Win/loss is checked immediately after each card reveal, before any further action.
- **Reason:** No further play may occur after a terminal condition.
- **Becomes true when:** First reveal onward.
- **Can no longer change when:** Continuously true during the match.
- **Related Rules:** F-11, BR-GE-1, INV-G7.

### INV-O3 — Assassin resolution overrides all other outcomes
- **Description:** If the active team reveals the assassin, that outcome supersedes any other pending end condition in the same step.
- **Reason:** Deterministic, faithful precedence.
- **Becomes true when:** Assassin reveal.
- **Can no longer change when:** Immediately terminal.
- **Related Rules:** BR-ASN-4 (see [17 — Rule Precedence](16-rule-precedence.md)).

### INV-O4 — A recorded result is immutable
- **Description:** Once a Game Result is recorded, it never changes.
- **Reason:** Trust and auditability.
- **Becomes true when:** Match finish.
- **Can no longer change when:** Permanently.
- **Related Rules:** BR-GE-3, §7.3.

---

## 12. Invariant Enforcement Summary

| Scope | Invariants | Frozen at |
|-------|-----------|-----------|
| Board & cards | INV-B1..B9 | Board generation (immutable for match) |
| Game & turns | INV-G1..G8 | Continuously during match |
| Teams & roles | INV-T1..T6 | Match start (locked for match) |
| Room & host | INV-R1..R5 | Room lifetime |
| Player & connection | INV-P1..P5 | Player/room lifetime |
| Dictionary | INV-D1..D3 | Match start (version fixed) |
| Results | INV-O1..O4 | Match finish (immutable) |

> Any detected violation of an invariant indicates a defect: the system must reject the
> causing intent and preserve the last consistent state (see [10 — Validation Rules](09-validation-rules.md)).
