# 18. Consistency Validation Report — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Review findings — **no existing document was modified** |
| **Purpose** | Report duplicate rules, contradictions, missing/broken references, terminology drift, and ambiguities discovered while completing the documentation package. This is a **findings report only**; it recommends but does not apply changes to approved documents. |
| **Scope reviewed** | Documents 00–17. |

## Table of Contents
1. [Method](#1-method)
2. [Severity Scale](#2-severity-scale)
3. [Summary](#3-summary)
4. [Findings](#4-findings)
5. [Terminology Consistency Check](#5-terminology-consistency-check)
6. [Reference Integrity Check](#6-reference-integrity-check)
7. [Coverage Confirmation](#7-coverage-confirmation)
8. [Recommendations Roll-up](#8-recommendations-roll-up)

---

## 1. Method

Each document was reviewed against the others for: duplicated/contradictory rules, missing or
broken cross-references, inconsistent entity/term names, ambiguous wording, and gaps. New
documents (11–17) were written to **reference** existing content rather than restate it; where
this report notes an issue, the **recommendation is advisory** per the task's "report, do not
auto-change" instruction.

## 2. Severity Scale

| Severity | Meaning |
|----------|---------|
| **Critical** | A contradiction that would produce different game outcomes; must fix before implementation. |
| **Major** | An ambiguity/gap that would force a developer to guess business behaviour. |
| **Minor** | Cosmetic/terminology drift; no behavioural impact but affects polish/traceability. |
| **Info** | Observation or intentional design note; no action required. |

## 3. Summary

| Severity | Count |
|----------|-------|
| Critical | 0 |
| Major | 2 |
| Minor | 5 |
| Info | 3 |

**No critical contradictions were found.** The gameplay model is internally consistent and
faithful to Codenames across all documents. The items below are refinements that raise the set
from "complete" to "enterprise-grade".

## 4. Findings

### F-VAL-01 — "Game" vs "Match" used interchangeably — **Minor**
- **Observation:** Documents use both *Game* and *Match* for the same concept (e.g., Domain
  Model entity "Game (Match)", state machine "Game (Match) state machine", workflows "Finish
  Match"). The glossary defines "Match / Game" as synonyms, so there is **no contradiction**,
  but the dual term slightly reduces precision.
- **Affected:** 01, 02, 03, 04, 06, 07, 08, 09, 12, 17.
- **Recommendation:** Adopt **Match** as the canonical noun for a single play session and
  reserve **Game** for the product/gameplay in general; add a one-line note to the glossary.
  (Advisory only.)

### F-VAL-02 — Abandonment vs "exactly one winner" phrasing — **Major**
- **Observation:** BR-WIN-3 states "Exactly one team wins each **completed** match; there is no
  draw," while BR-GE-5/BR-RX-3 introduce **abandonment** (no play-based winner). INV-O1 already
  reconciles this ("per completed match … abandonment is distinct"), but a reader consulting
  only document 03 might read BR-WIN-3 as absolute.
- **Risk:** A developer could implement abandonment as a forced/arbitrary winner.
- **Affected:** 03 (BR-WIN-3, BR-GE-5, BR-TIE-3), 11 (INV-O1), 16 (PS-25).
- **Recommendation:** Add a clarifying clause to BR-WIN-3: *"…completed by play. Abandonment
  (BR-GE-5) is not a completed match and has no play-based winner."* (Advisory; INV-O1 and
  PS-25 already encode the correct behaviour, so this is a wording tightening, not a rule
  change.)

### F-VAL-03 — Late-joiner ("waiting member") participation timing — **Major**
- **Observation:** BR-JR-6/6a establish that a mid-match joiner waits and sees no board data.
  Documents 15 (LM-2, CW-4) and 16 align. However, none of the documents state **exactly when**
  a waiting member becomes assignable to a team — at match end (Post-Match) or only after the
  room returns to Lobby. Behaviour is implied (Post-Match reconfigure) but not explicit.
- **Risk:** Ambiguity in the exact transition a waiting member follows into the next match.
- **Affected:** 03 (BR-JR-6a), 15 (LM-2, §9 RF), 12 (EVT-2).
- **Recommendation:** Add one sentence to LM-2 / RF-2: *"A waiting member becomes a normal,
  assignable member the moment the room enters Post-Match/Lobby, and may then choose team and
  role for the next match."* (Advisory.)

### F-VAL-04 — Team naming: canonical colours vs neutral labels — **Minor**
- **Observation:** README fixes teams as **Red/Blue**; some abstract passages say "two teams"
  or "the other team" generically. This is consistent (colours are the canonical instance) and
  intentional for language-neutrality, but the relationship could be stated once.
- **Affected:** 00, 01, 03, 07.
- **Recommendation:** Note in the glossary that "Red" and "Blue" are the canonical team labels
  and carry no gameplay asymmetry beyond the starting-team's 9-vs-8 agent split. (Advisory.)

### F-VAL-05 — `PLAYER_ALREADY_EXISTS` naming — **Minor / Resolved**
- **Observation:** The task listed `PLAYER_ALREADY_EXISTS` as an example error. The catalog
  (13) uses **`DUPLICATE_NICKNAME`** as canonical and explicitly notes the synonym.
- **Affected:** 13.
- **Status:** **Resolved by design**; documented as a synonym to avoid two codes for one cause.
  No action needed (Info-level).

### F-VAL-06 — Round semantics when the match ends mid-round — **Minor**
- **Observation:** Document 08 (§8.5) and 12 (EVT-20) define a Round as a pair of turns and
  allow "Complete (final)" when the match ends mid-round. This is consistent, but Round is
  purely bookkeeping and never gates play; a reader might expect Round to have rule force.
- **Affected:** 07 (Round), 08 (§8.5), 12 (EVT-15/20).
- **Recommendation:** Keep the existing explicit note ("A Round … never gates play") and
  optionally repeat it in the Domain Model Round entity. (Advisory.)

### F-VAL-07 — Guess-limit "bonus" wording for 0/unlimited — **Minor**
- **Observation:** BR-GV-7 and BR-EC-2/3/4 correctly define number+1 for N≥1 and unbounded for
  0/∞. The word "bonus guess" (BR-EC-4) applies only to the N≥1 case; for 0/∞ there is no
  distinct "bonus" since guessing is already unbounded. This is correct but could confuse.
- **Affected:** 03 (BR-GV-7, BR-EC-2/3/4), 11 (INV-G6).
- **Recommendation:** Add a parenthetical to BR-EC-4: *"(the +1 bonus concept does not apply to
  clue 0/unlimited, which are already unbounded)."* (Advisory.)

### F-VAL-08 — Idle expiry is inactivity-based, not wall-clock — **Info**
- **Observation:** Rule Precedence (17, §7) clarifies that activity resets the idle timer, so a
  room does not expire mid-active-turn. This is consistent with BR-RX-1 ("inactivity"). Flagged
  so implementers do not treat `ROOM_IDLE_EXPIRY` as an absolute session cap.
- **Affected:** 03 (BR-RX-1), 15 (IR-1), 17 (§7).
- **Status:** Consistent; no change. (Info.)

### F-VAL-09 — Operational parameters are single-sourced — **Info**
- **Observation:** All tunables (grace periods, capacity, idle expiry, code/nickname bounds,
  min words) are defined once in [00-README](../_meta/00-canonical-constants-and-index.md#configurable-operational-parameters)
  and only **referenced** elsewhere (11, 13, 14, 15, 16). No duplicate/conflicting values were
  found.
- **Status:** Good practice; no change. (Info.)

### F-VAL-10 — Duplicate-connection rule is new but non-conflicting — **Info**
- **Observation:** INV-P4 / PS-21..24 / `DUPLICATE_CONNECTION` introduce single-active-connection
  handling not explicitly present in documents 03–10. It is an **elaboration** of the existing
  transient-identity model (BR-JR-7, BR-DC-*), not a new gameplay feature, and does not
  contradict any prior rule.
- **Affected:** 11, 13, 16.
- **Recommendation:** Consider back-adding a one-line rule (e.g., BR-DC-9) to document 03 so the
  connection model is fully captured in the canonical rules file. (Advisory.)

## 5. Terminology Consistency Check

| Term | Canonical form | Drift observed | Verdict |
|------|----------------|----------------|---------|
| Match / Game | "Match" (recommended) | Used interchangeably | Minor (F-VAL-01) |
| Team labels | Red / Blue | Generic "two teams" in abstract text | Consistent (F-VAL-04) |
| Spymaster / Operative | Consistent everywhere | — | ✔ |
| Agent / Neutral / Assassin card | Consistent | "bystander" used once as gloss for neutral | ✔ (gloss only) |
| Key / Key Card | Consistent | — | ✔ |
| Room states (Lobby/InMatch/PostMatch/Expired) | Consistent across 08/15 | — | ✔ |
| Turn phases (AwaitingClue/AwaitingGuess/TurnEnded) | Consistent across 04/08/12/17 | — | ✔ |
| Starting team = 9 agents | Consistent across 00/03/07/12 | — | ✔ |
| Guesses = number + 1 (or unbounded for 0/∞) | Consistent across 03/04/11/17 | — | ✔ |

## 6. Reference Integrity Check

| Check | Result |
|-------|--------|
| New docs (11–17) cite existing rule IDs (BR-/V-/F-/EVT-/INV-). | All cited IDs exist in their source documents. |
| Canonical constants used consistently. | `BOARD_SIZE`, 9/8/7/1, `MIN_PLAYERS`, `GUESS_BONUS` match across docs. |
| Operational parameters referenced (not redefined). | Single source in README; no conflicts. |
| Error ↔ Validation ↔ Rule ↔ Invariant traceability. | Provided in [13 §12](12-business-error-catalog.md#12-traceability); spot-checked, consistent. |
| Event ↔ Workflow ↔ Rule mapping. | Provided in [12](11-domain-events-catalog.md); consistent. |
| Broken anchors / dangling IDs. | None found in spot checks; recommend one automated link-lint pass at doc freeze. |

## 7. Coverage Confirmation

The following required additions are present and cross-referenced:

| Required document | Delivered | File |
|-------------------|-----------|------|
| Business Invariants | ✔ | [11](10-business-invariants.md) |
| Domain Events Catalog | ✔ | [12](11-domain-events-catalog.md) |
| Business Error Catalog | ✔ | [13](12-business-error-catalog.md) |
| Dictionary Management Specification | ✔ | [14](13-dictionary-management.md) |
| Lobby & Room Lifecycle Specification | ✔ | [15](14-lobby-room-lifecycle.md) |
| Player Session & Reconnection Specification | ✔ | [16](15-player-session-reconnection.md) |
| Rule Precedence Specification | ✔ | [17](16-rule-precedence.md) |
| Validation report | ✔ | this document |

No gameplay was redesigned; no social/monetization/auth/AI/ranking features were added.

## 8. Recommendations Roll-up

| # | Recommendation | Severity | Owner | Affected docs |
|---|----------------|----------|-------|---------------|
| 1 | Tighten BR-WIN-3 to exclude abandonment from "one winner". | Major | BA | 03 |
| 2 | State exactly when a waiting member becomes assignable (Post-Match/Lobby). | Major | BA | 03, 15 |
| 3 | Choose "Match" as canonical; note synonym in glossary. | Minor | BA | 01 (glossary) |
| 4 | Note Red/Blue as canonical labels with only the 9-vs-8 asymmetry. | Minor | BA | 01 (glossary) |
| 5 | Add parenthetical clarifying "bonus" does not apply to clue 0/∞. | Minor | BA | 03 |
| 6 | Optionally add BR-DC-9 for single-active-connection to the canonical rules. | Minor | BA | 03 |
| 7 | Run one automated link/ID lint at documentation freeze. | Minor | QA | all |

> Per instruction, **none of these changes were applied automatically**. They are advisory and
> can be scheduled by the Business Analyst before architecture sign-off. With items 1–2
> addressed, the package has no remaining ambiguity that would block implementation.
