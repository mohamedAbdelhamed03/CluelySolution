# 14. Dictionary Management Specification — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Define the complete **business** of word dictionaries: how regional word sets are structured, versioned, validated, activated, retired, selected, and owned — without touching gameplay rules. The dictionary is the **only** localized component of Cluely. |
| **Technology** | Neutral (no storage, format, or code). |

## Table of Contents
1. [Purpose & Scope](#1-purpose--scope)
2. [References](#2-references)
3. [Concepts](#3-concepts)
4. [Country Dictionary](#4-country-dictionary)
5. [Dictionary Version](#5-dictionary-version)
6. [Dictionary Lifecycle](#6-dictionary-lifecycle)
7. [Selection](#7-selection)
8. [Validation & Constraints](#8-validation--constraints)
9. [Word Quality & Offensive Content](#9-word-quality--offensive-content)
10. [Version Compatibility & Updates](#10-version-compatibility--updates)
11. [Business Ownership](#11-business-ownership)
12. [Business Constraints Summary](#12-business-constraints-summary)

---

## 1. Purpose & Scope

Cluely is one product, one codebase, one gameplay worldwide; **only the word source is
localized**. This document specifies dictionary management as a business capability. It does
**not** change any gameplay rule (see [INV-D1](10-business-invariants.md#inv-d1--dictionary-affects-words-only)):
counts (9/8/7/1), turn flow, and win/loss are identical for every dictionary.

## 2. References
- [01 — BRD §Localization](../01-product-discovery/01-business-requirements.md) and [SRS FR-35..37](01-software-requirements.md#28-functional-requirements)
- [03 — Business Rules](02-business-rules.md) (BR-RC-4, BR-BG-2/9, BR-GS-3)
- [07 — Domain Model](06-domain-model.md) (Dictionary, DictionaryVersion, CountryDictionary)
- [11 — Business Invariants](10-business-invariants.md) (INV-D1..D3)

## 3. Concepts

| Term | Meaning |
|------|---------|
| **Dictionary (Country Dictionary)** | A curated word set for a specific country/region (e.g., Egypt, Saudi Arabia, USA, France, Germany). |
| **Dictionary Version** | An immutable, published snapshot of a Country Dictionary's word list. |
| **Active Version** | The version offered for new match selection for a region. |
| **Selection** | The Host's choice of a region for a room; it resolves to that region's active version. |
| **Usable Word** | A word eligible for board placement (passes quality/uniqueness checks). |

## 4. Country Dictionary

- **Purpose:** Provide culturally natural words so gameplay feels local while remaining
  identical in rules.
- **Responsibilities:** Own a coherent set of words appropriate to its region; carry a
  human-readable region identity; hold a history of versions.
- **Business constraints:**
  - **DM-C1** Each region has exactly one Country Dictionary.
  - **DM-C2** A Country Dictionary is meaningful only through its **versions**; matches never
    consume a dictionary directly, only a specific version (INV-D3).
  - **DM-C3** Regions are independent; adding, editing, or retiring one region never affects
    another (NFR-6).

## 5. Dictionary Version

- **Purpose:** Give each match a reproducible, immutable word source.
- **Responsibilities:** Contain the full list of usable words for that snapshot; carry a
  version identity; record its lifecycle state.
- **Business constraints:**
  - **DM-V1** A published version is **immutable**: its word list never changes after
    publication. Corrections require a **new** version.
  - **DM-V2** A version must contain at least `DICTIONARY_MIN_WORDS` (25) **distinct** usable
    words to be publishable (BR-GS-3, INV-D2).
  - **DM-V3** Words within a version are **unique** (no duplicates, case-insensitive, trimmed).
  - **DM-V4** A match records the **exact version** it used; that reference never changes for
    the match (INV-D3, FR-37).

## 6. Dictionary Lifecycle

A Country Dictionary Version moves through these business states:

```
Draft ──review/approve──► Published ──activate──► Active ──deactivate──► Deprecated ──retire──► Retired
   │                                                  │
   └───────────── reject ─────────────► Draft         └── superseded by newer version ──► Deprecated
```

| State | Meaning | Selectable for new matches? | Used by in-progress matches? |
|-------|---------|-----------------------------|------------------------------|
| **Draft** | Under construction/curation. | No | No |
| **Published** | Approved & immutable, not yet the default. | No (not until Active) | No |
| **Active** | The current version offered for a region. | **Yes** | Yes (once started) |
| **Deprecated** | Superseded by a newer Active version. | No | **Yes** — matches that started on it continue (INV-D3) |
| **Retired** | Withdrawn from all use. | No | No new; existing matches finish or abandon per retirement policy (DM-L4) |

- **Activation (DM-L1):** Making a Published version Active makes it the region's selectable
  version. Exactly one version per region is Active at a time.
- **Retirement (DM-L2):** A version may be retired (e.g., quality/offensive-content issue). It
  is removed from selection immediately.
- **DM-L3** Activating a new version **automatically deprecates** the previously Active one
  for that region.
- **DM-L4** Retiring a version does **not** rewrite any in-progress match's fixed version
  (INV-D3); it only prevents new selection. Whether affected in-progress matches continue is a
  content-ownership decision (default: they finish normally).

## 7. Selection

- **DM-S1** The Host selects a **region** (not a version) at room creation or in Lobby
  (BR-RC-4); the system resolves it to that region's **Active** version.
- **DM-S2** Selection is locked at match start; the match binds to the resolved version for
  its whole life (BR-GS-5, INV-D3).
- **DM-S3** If no region is selected, a configured **default region** applies.
- **DM-S4** Changing the region between matches is allowed and affects only the next match
  (F-16); it never changes rules (INV-D1).
- **DM-S5** A region whose Active version fails validation (too small/unavailable) cannot be
  selected to start; the system returns `DICTIONARY_TOO_SMALL` or `DICTIONARY_NOT_FOUND`
  ([13](12-business-error-catalog.md)).

## 8. Validation & Constraints

| ID | Rule |
|----|------|
| DM-VAL1 | A version is publishable only if it has ≥25 distinct usable words (INV-D2, V-DICT-2). |
| DM-VAL2 | Words are unique within a version (DM-V3). |
| DM-VAL3 | At match start, the resolved version must exist and be Active/Deprecated-in-use with ≥25 usable words (V-DICT-1/2). |
| DM-VAL4 | Word selection for a board draws **distinct** words at random from the version (BR-BG-2, INV-B6). |
| DM-VAL5 | The dictionary influences **only** which words appear; it never alters counts, phases, or outcomes (INV-D1). |

## 9. Word Quality & Offensive Content

- **DM-Q1 Quality:** Words should be single, guessable, culturally appropriate terms suitable
  for a general audience in the target region. Curation is a **content-team** responsibility,
  not a runtime rule.
- **DM-Q2 Offensive content handling:** Words deemed offensive/inappropriate for a region are
  excluded during curation. If an offensive word is discovered in a Published/Active version,
  the remedy is to **publish a corrected new version** and **retire** the affected one
  (DM-V1 immutability + DM-L2 retirement). Words are never edited in place.
- **DM-Q3 Cultural appropriateness:** Each region's list is curated so words feel natural
  locally; this is the sole purpose of localization (BRD §Localization).
- **DM-Q4** No user-supplied words exist in this version (out of scope, BRD §1.6); only
  curated content is used.

## 10. Version Compatibility & Updates

- **DM-U1** Versions are **forward-only**: updates produce new versions; old versions remain
  immutable for reproducibility (DM-V1).
- **DM-U2** In-progress matches are **never** affected by a new version (INV-D3); they keep
  their bound version until they finish.
- **DM-U3** New matches automatically use the region's **Active** version at start time.
- **DM-U4** There is no cross-region compatibility concern: a match uses exactly one region's
  one version; regions never mix on a board.
- **DM-U5** Because rules are language-independent (INV-D1), no version change can create a
  gameplay incompatibility — only the words differ.

## 11. Business Ownership

- **DM-O1** A **Content/Localization team** owns dictionary curation, review, approval,
  activation, and retirement.
- **DM-O2** The **Product Owner** approves which regions exist and the default region.
- **DM-O3** Runtime (Host/players) may only **select** a region; they cannot create, edit, or
  retire dictionaries.
- **DM-O4** The game (Game Engine) is a **consumer** of a resolved version and never mutates
  dictionary content.

## 12. Business Constraints Summary

| Constraint | Source |
|-----------|--------|
| One Country Dictionary per region; regions independent. | DM-C1/C3 |
| Versions are immutable; corrections = new versions. | DM-V1, DM-U1 |
| ≥25 distinct usable words required to publish/play. | DM-V2, INV-D2 |
| Words unique within a version. | DM-V3, INV-B6 |
| Exactly one Active version per region at a time. | DM-L1/L3 |
| Match binds to one version for its whole life. | DM-S2, INV-D3 |
| Selection chooses a region; system resolves the version. | DM-S1 |
| Dictionary affects words only — never rules/outcomes. | DM-VAL5, INV-D1 |
| Offensive content handled by retire + new version, never in-place edits. | DM-Q2 |
| Curated content only; no user-supplied words. | DM-Q4 |
| Content team owns lifecycle; runtime only selects. | DM-O1/O3 |
