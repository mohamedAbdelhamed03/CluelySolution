# 21. Business Constants Catalog — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review — **canonical source for all numeric business values** |
| **Purpose** | Provide the single authoritative catalog of every numeric business value in Cluely. All other documents should **reference** these identifiers rather than restate values. Consolidates the constants and operational parameters first introduced in [00 — README](../_meta/00-canonical-constants-and-index.md). |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [Categories](#3-categories)
4. [Fixed Gameplay Constants](#4-fixed-gameplay-constants-immutable)
5. [Composition Constants](#5-composition-constants)
6. [Guessing Constants & Rules](#6-guessing-constants--rules)
7. [Operational Parameters (Tunable)](#7-operational-parameters-tunable)
8. [Identity & Content Constants](#8-identity--content-constants)
9. [Single-Source Policy](#9-single-source-policy)
10. [Revision History](#10-revision-history)

---

## 1. Purpose & Usage

Numeric values scattered across documents are a maintenance hazard. This catalog is the **one
place** each value is defined. Two classes exist:

- **Fixed gameplay constants** — faithful to Codenames; **must not change** (changing them
  changes the game — see [ADR-14](02-architecture-decision-records.md)).
- **Operational parameters** — tunable within a range without affecting rules (grace, capacity,
  timeouts, code/nickname bounds).

## 2. References
- [00 — README](../_meta/00-canonical-constants-and-index.md) (origin of these values), [03 — Business Rules](../02-business-analysis/02-business-rules.md)
- [04 — Functional Requirements](../02-business-analysis/03-functional-requirements.md), [10 — Validation Rules](../02-business-analysis/09-validation-rules.md)
- [11 — Invariants](../02-business-analysis/10-business-invariants.md), [19 — Glossary](01-business-glossary.md)

## 3. Categories

| Category | Mutability | Section |
|----------|-----------|---------|
| Fixed gameplay | Immutable (faithful) | §4 |
| Composition | Immutable (faithful) | §5 |
| Guessing | Immutable (faithful) | §6 |
| Operational | Tunable within range | §7 |
| Identity & content | Tunable within range | §8 |

Entry fields: **Identifier**, **Business Meaning**, **Default Value**, **Reason**,
**Where Used**, **Related Rules / FR / Validations**.

---

## 4. Fixed Gameplay Constants (Immutable)

### CONST-BOARD-SIZE
- **Meaning:** Total Word Cards on a Board (5×5 grid). **Default:** `25`.
- **Reason:** Faithful Codenames board.
- **Where Used:** Board generation, Domain Model (Board).
- **Related:** BR-BG-1 · F-07 · V-BOARD-1 · INV-B1.

### CONST-CARDS-PER-BOARD
- **Meaning:** Synonym anchor for the 25-card count used in prose. **Default:** `25`.
- **Reason:** Single reference for "25 cards".
- **Where Used:** All gameplay docs.
- **Related:** BR-BG-1 · INV-B1. **Note:** Same value as CONST-BOARD-SIZE; not a separate quantity.

### CONST-STARTING-TEAM-AGENTS
- **Meaning:** Agent Cards owned by the Starting Team. **Default:** `9`.
- **Reason:** Faithful starting-team advantage.
- **Where Used:** Key generation, turn order.
- **Related:** BR-BG-3/4 · INV-B2 · [ADR-14](02-architecture-decision-records.md).

### CONST-SECOND-TEAM-AGENTS
- **Meaning:** Agent Cards owned by the second Team. **Default:** `8`.
- **Reason:** Faithful composition.
- **Where Used:** Key generation.
- **Related:** BR-BG-3 · INV-B2.

### CONST-NEUTRAL-CARDS
- **Meaning:** Bystander Cards owned by no Team. **Default:** `7`.
- **Reason:** Faithful composition.
- **Where Used:** Key generation, neutral handling.
- **Related:** BR-BG-3 · BR-NC-* · INV-B2.

### CONST-ASSASSIN-CARDS
- **Meaning:** Instant-loss Cards. **Default:** `1`.
- **Reason:** Faithful single Assassin.
- **Where Used:** Key generation, assassin handling, precedence.
- **Related:** BR-ASN-1 · INV-B3 · [17](../02-business-analysis/16-rule-precedence.md).

> Invariant: `9 + 8 + 7 + 1 = 25` (INV-B2). These five values are interdependent and fixed.

---

## 5. Composition Constants

### CONST-TEAMS-PER-GAME
- **Meaning:** Teams per Match. **Default:** `2`.
- **Reason:** Faithful two-sided game.
- **Where Used:** Setup, start validation.
- **Related:** BR-TA-1 · INV-T1.

### CONST-SPYMASTERS-PER-TEAM
- **Meaning:** Spymasters per Team (exact). **Default:** `1`.
- **Reason:** One Key-holder/clue-giver per Team.
- **Where Used:** Role assignment, start validation.
- **Related:** BR-RO-1/6 · V-ROLE-1 · INV-T3 · [ADR-10](02-architecture-decision-records.md).

### CONST-MIN-OPERATIVES-PER-TEAM
- **Meaning:** Minimum Operatives per Team. **Default:** `1`.
- **Reason:** A Team needs someone to guess.
- **Where Used:** Start validation.
- **Related:** BR-TA-6 · V-START-3 · INV-T6.

### CONST-MIN-PLAYERS
- **Meaning:** Minimum players to start a Match. **Default:** `4` (2 Teams × {1 Spymaster + 1 Operative}).
- **Reason:** Minimum playable faithful game.
- **Where Used:** Start validation.
- **Related:** BR-GS-1 · V-START-3 · INV-T6.

### CONST-MAX-SPYMASTERS-PER-TEAM
- **Meaning:** Upper bound on Spymasters per Team. **Default:** `1` (equals the exact count).
- **Reason:** No second Spymaster permitted.
- **Where Used:** Role claim validation.
- **Related:** BR-RO-6 · INV-T3.

### CONST-MAX-OPERATIVES-PER-TEAM
- **Meaning:** Upper bound on Operatives per Team. **Default:** bounded only by room capacity (`ROOM_MAX_PLAYERS` minus Spymasters).
- **Reason:** No fixed cap in the reference game; only the room size limits it.
- **Where Used:** Team composition.
- **Related:** BR-TA-7 · CONST-ROOM-MAX-PLAYERS.

---

## 6. Guessing Constants & Rules

### CONST-GUESS-BONUS
- **Meaning:** Extra guesses beyond the Hint Number when it is ≥ 1. **Default:** `+1`.
- **Reason:** Faithful "number + 1" bonus guess.
- **Where Used:** Turn guess allowance.
- **Related:** BR-GV-7 · INV-G6.

### RULE-MAX-GUESSES (derived, not a single number)
- **Meaning:** Maximum guesses allowed in a Turn.
- **Default logic:**
  - Hint Number `N ≥ 1` → up to `N + CONST-GUESS-BONUS` guesses.
  - Hint Number `0` or `unlimited` → **unbounded** guesses, minimum `1`.
- **Reason:** Faithful Codenames guessing limits (the +1 bonus does not apply to 0/unlimited, which are already unbounded).
- **Where Used:** Guess validation, turn end.
- **Related:** BR-GV-6/7 · BR-EC-2/3/4 · V-GUESS-4 · INV-G5/G6.

### CONST-MIN-GUESSES-PER-TURN
- **Meaning:** Minimum guesses required before a Turn may be voluntarily ended. **Default:** `1`.
- **Reason:** A Team must attempt at least one guess.
- **Where Used:** End-turn validation.
- **Related:** BR-GV-6 · V-ENDTURN-1 · INV-G5.

---

## 7. Operational Parameters (Tunable)

These carry an **allowed range**; changing within range never affects rules ([ADR-05/12](02-architecture-decision-records.md)).

### CONST-ROOM-MAX-PLAYERS
- **Meaning:** Maximum members per Room. **Default:** `10`. **Range:** `4–20`.
- **Reason:** Bounded room size for manageable play.
- **Where Used:** Join/capacity.
- **Related:** BR-JR-5 · V-CAP-1 · INV-R5.

### CONST-RECONNECT-GRACE-PERIOD
- **Meaning:** Window for a disconnected Player to reconnect and resume role. **Default:** `60 s`. **Range:** `15–180 s`.
- **Reason:** Tolerate transient mobile drops.
- **Where Used:** Disconnect/reconnect.
- **Related:** BR-DC-2/5 · V-RECON-2 · [16](../02-business-analysis/15-player-session-reconnection.md).

### CONST-HOST-MIGRATION-GRACE
- **Meaning:** Delay before a disconnected Host's privileges migrate. **Default:** `60 s`. **Range:** `15–180 s`.
- **Reason:** Tolerate brief Host drops before reassigning control.
- **Where Used:** Host migration.
- **Related:** BR-DC-8 · BR-HM-1.

### CONST-ROOM-IDLE-EXPIRY
- **Meaning:** Inactivity period after which a Room expires. **Default:** `30 min`. **Range:** `5–120 min`.
- **Reason:** Reclaim abandoned/idle Rooms.
- **Where Used:** Room expiration.
- **Related:** BR-RX-1 · V-EXP-1 · [15](../02-business-analysis/14-lobby-room-lifecycle.md). **Note:** Inactivity-based, reset by activity (F-VAL-08).

### CONST-ROOM-EXPIRATION-ON-EMPTY
- **Meaning:** Whether an empty Room expires immediately. **Default:** `immediate (true)`. **Range:** fixed policy.
- **Reason:** No purpose to an empty Room.
- **Where Used:** Room expiration.
- **Related:** BR-RX-2 · INV-R4.

---

## 8. Identity & Content Constants

### CONST-ROOM-CODE-LENGTH
- **Meaning:** Length of a Room Code. **Default:** `6`. **Range:** `4–8`.
- **Reason:** Short enough to share, large enough to be unguessable.
- **Where Used:** Room creation.
- **Related:** BR-RC-2/3 · INV-R2 · R-3.

### CONST-ROOM-CODE-ALPHABET
- **Meaning:** Character set for Room Codes. **Default:** uppercase letters + digits, ambiguity-reduced (exclude `0/O`, `1/I/L`). **Range:** any unambiguous, non-sequential set.
- **Reason:** Reduce mis-entry; keep codes non-sequential.
- **Where Used:** Room code generation.
- **Related:** BR-RC-2 · R-3.

### CONST-NICKNAME-MIN-LENGTH
- **Meaning:** Minimum Nickname length (trimmed). **Default:** `1`. **Range:** `1–3`.
- **Reason:** A Player must be identifiable.
- **Where Used:** Join validation.
- **Related:** V-NICK-1 · BR-JR-4.

### CONST-NICKNAME-MAX-LENGTH
- **Meaning:** Maximum Nickname length. **Default:** `20`. **Range:** `8–32`.
- **Reason:** Prevent overflow/display abuse.
- **Where Used:** Join validation.
- **Related:** V-NICK-2 · BR-JR-4.

### CONST-DICTIONARY-MIN-WORDS
- **Meaning:** Minimum distinct usable words a Dictionary Version must supply. **Default:** `25` (hard floor). **Range:** `≥ 25`.
- **Reason:** A full Board needs 25 unique words.
- **Where Used:** Dictionary publish/select/start.
- **Related:** BR-GS-3 · V-DICT-2 · INV-D2 · [14](../02-business-analysis/13-dictionary-management.md).

---

## 9. Single-Source Policy

- **CSP-1** This catalog is the **only** authoritative source for numeric business values.
- **CSP-2** Other documents should cite the **identifier** (e.g., `CONST-MIN-PLAYERS`) rather
  than repeat the number. Where the [README](../_meta/00-canonical-constants-and-index.md) currently lists values inline, those
  values are **identical** to this catalog; the README table is the historical origin and this
  catalog is the governing reference going forward (advisory — no existing document is modified,
  per [18](../02-business-analysis/17-consistency-validation-report.md)).
- **CSP-3** Changing a **Fixed** constant (§4–6) is prohibited (it would change the game —
  [ADR-14](02-architecture-decision-records.md)); only a governed new game mode could do so.
- **CSP-4** Changing an **Operational** parameter (§7–8) within its range requires no rule change
  and no re-approval of gameplay documents.
- **CSP-5** No conflicting values exist across the package (verified in [25 — Final Validation](07-governance-validation-summary.md)).

## 10. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial catalog; consolidates constants and operational parameters from [00 — README](../_meta/00-canonical-constants-and-index.md) as the single numeric source. |
