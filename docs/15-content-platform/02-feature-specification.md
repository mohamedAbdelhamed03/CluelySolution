# Content Platform — 02. Feature Specification (Dictionary Management)

| | |
|---|---|
| **Bounded Context** | Content Platform |
| **Capability** | Dictionary Management (first capability) |
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Role of this document** | **Implementation contract.** Consolidates Business Requirements + Use Cases + SRS Addendum into one specification. ADR-011 can be written directly from it; implementation may begin after ADR-011 with no further business analysis. |
| **Builds on** | [01 — Business Vision](01-business-vision.md) (approved; **not repeated here**). |
| **Compatible with** | [ADR-002](../07-software-architecture/12-decisions/ADR-002-authoritative-game-state.md), [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md), [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md), [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md), [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md). |
| **Technology** | **Neutral** — no storage, database, API, format, framework, or code. Behavior only. |

> **Reading note.** This document specifies *behavior*. Where it resolves an [01 Open Question](01-business-vision.md#15-open-questions-non-blocking),
> the resolution is a **product decision**; ADR-011 makes the *architectural* form binding. Terms:
> a **Dictionary** is the owned container; a **Version** is an immutable published snapshot; a **Draft** is
> the single mutable working copy of a dictionary. A match consumes a **Version**, never a Dictionary or Draft.

## Table of Contents
1. [Feature Scope](#1-feature-scope)
2. [Business Capabilities](#2-business-capabilities)
3. [Functional Requirements](#3-functional-requirements)
4. [Business Rules](#4-business-rules)
5. [Lifecycle](#5-lifecycle)
6. [Ownership](#6-ownership)
7. [Visibility](#7-visibility)
8. [Versioning](#8-versioning)
9. [Validation Rules](#9-validation-rules)
10. [Discovery](#10-discovery)
11. [Sharing](#11-sharing)
12. [Import / Export](#12-import--export)
13. [Gameplay Integration](#13-gameplay-integration)
14. [Constraints](#14-constraints)
15. [Acceptance Criteria](#15-acceptance-criteria)
16. [Traceability](#16-traceability)
17. [Dictionary & Version Metadata](#17-dictionary--version-metadata) *(v1.1)*
18. [Publication Pipeline & Review](#18-publication-pipeline--review) *(v1.1)*
19. [Content Origins (Import Sources)](#19-content-origins-import-sources) *(v1.1)*
20. [Search Metadata](#20-search-metadata) *(v1.1)*
21. [Statistics (Future)](#21-statistics-future) *(v1.1)*
22. [Soft-Delete Lifecycle](#22-soft-delete-lifecycle) *(v1.1)*
23. [Official Content Boundary](#23-official-content-boundary) *(v1.1)*
24. [Future Collaboration (Deferred)](#24-future-collaboration-deferred) *(v1.1)*
25. [Non-Goals](#25-non-goals) *(v1.1)*
26. [Revision History](#26-revision-history)

---

## 1. Feature Scope

### 1.1 In scope
- Authenticated users create, name, and manage **Dictionaries** and their single working **Draft**.
- Add/edit/remove words in a Draft, by manual entry or **import**.
- **Validate** and **publish** a Draft into an immutable **Version**.
- **Visibility** control (Private / Shared / Public); **Organization** as a forward-compatible placeholder.
- **Share** a dictionary with specific accounts; **revoke** shares.
- **Clone** a dictionary from a version the actor may see.
- **Export** a published version's word collection (logical operation; encoding out of scope).
- **Discovery**: browse Mine / Official / Shared-with-me / Public.
- **Gameplay selection**: a host selects a published version when creating a room (consumes via the existing
  [ADR-008 pinning](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#8-match-pinning-critical)).
- **Lifecycle** management: draft → validated → published → archived → retired; dictionary archive/delete.
- **Moderation lifecycle** actions on shared/public content (report → block → retire), acting on state, never on words.

### 1.2 Out of scope *(per [01 §12](01-business-vision.md#12-out-of-scope))*
Monetization/marketplace; AI generation; moderation *tooling*; organization *administration*; ratings/reviews/
social; favorites; ownership transfer; import/export *encoding/format*; any change to gameplay, rules, constants,
or the pinning contract; all technology choices; the authentication mechanism itself.

### 1.3 Assumptions
Inherits [01 §13 VA-1..VA-6](01-business-vision.md#13-assumptions). Load-bearing: durable accounts exist
(Phase 2); consuming published content in a room stays account-free; the [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)
content model holds for user content; `DICTIONARY_MIN_WORDS` (25) and word-uniqueness apply to all content types.

### 1.4 Dependencies
| Dep | On |
|-----|----|
| DEP-1 | **Authentication / durable identity** ([Roadmap Phase 2](../03-business-governance/06-product-roadmap.md#5-phase-2--accounts--continuity-future), [ADR-009 identity seam](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)). |
| DEP-2 | **Content model** — versioned/immutable/pinned ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| DEP-3 | **Room isolation** — content is shared read-only, never room-coupling ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)). |
| DEP-4 | **Command/Query stance** — authoring actions are Commands; browsing/reading are Queries ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)). |
| DEP-5 | **Retention** — referenced versions are protected from deletion ([Data Lifecycle](../03-business-governance/05-data-lifecycle-retention.md)). |

---

## 2. Business Capabilities

| # | Capability | Essence |
|---|-----------|---------|
| CAP-1 | **Authoring** | Create dictionaries; edit a single mutable draft (words, name). |
| CAP-2 | **Lifecycle** | Move content through draft → validated → published → archived → retired; archive/delete dictionaries. |
| CAP-3 | **Ownership** | Exactly one owner per dictionary; owner holds all authoring rights; future editors/viewers. |
| CAP-4 | **Visibility** | Private / Shared / Public (Organization future); default Private. |
| CAP-5 | **Versioning** | Publish immutable versions; preserve history; select the current published version. |
| CAP-6 | **Validation** | Enforce word quality/uniqueness/count before publish. |
| CAP-7 | **Import** | Bring an external word collection into a draft. |
| CAP-8 | **Export** | Emit a published version's word collection. |
| CAP-9 | **Discovery** | Browse Mine / Official / Shared / Public. |
| CAP-10 | **Sharing** | Grant/revoke access; clone; (future) transfer ownership. |
| CAP-11 | **Gameplay Selection** | Offer selectable published versions to a room host. |
| CAP-12 | **Moderation** | Lifecycle-only safeguards on shared/public content. |

---

## 3. Functional Requirements

> Actor "**user**" = an authenticated account holder. "**owner**" = the user who owns the dictionary in
> question. "**host**" = a room creator (may be anonymous). Every FR that changes content state is a **Command**;
> every FR that lists/reads is a **Query** ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md)).

### Authoring (CAP-1)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-001 | An authenticated user can **create a dictionary**; the creator becomes its sole owner; it starts **Private** with an empty **Draft**. |
| FR-CONTENT-002 | The owner can **rename** a dictionary and edit its **description**. |
| FR-CONTENT-003 | The owner can **add** one or more words to the Draft. |
| FR-CONTENT-004 | The owner can **edit** or **remove** words in the Draft. |
| FR-CONTENT-005 | The owner can **discard** all unpublished Draft changes, reverting the Draft to the last published version's words (or empty if none). |
| FR-CONTENT-006 | The system **normalizes** each word on entry (trim; collapse internal whitespace) for comparison and storage. |

### Validation & Versioning (CAP-5, CAP-6)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-010 | The owner can **validate** a Draft on demand; the system reports every failing rule (§9) without publishing. |
| FR-CONTENT-011 | The owner can **publish** a Draft; the system validates first and, on success, creates a new **immutable Version**. |
| FR-CONTENT-012 | Publishing a version **does not** empty or delete the Draft; the Draft continues as the working copy for the next version. |
| FR-CONTENT-013 | The system assigns each Version a **globally-unique, immutable identity** and an incrementing, owner-visible **version label**. |
| FR-CONTENT-014 | The owner can **view version history** of a dictionary (all published versions, newest first). |
| FR-CONTENT-015 | The system maintains, per dictionary, a **current published version** = the newest non-retired published version; this is what gameplay selection resolves to. |
| FR-CONTENT-016 | The owner can **edit a published version's content only by editing the Draft and publishing a new version**; published words are never mutated. |

### Ownership & Permissions (CAP-3)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-020 | Every dictionary has **exactly one owner** at all times. |
| FR-CONTENT-021 | Only the **owner** may author, validate, publish, change visibility, share, archive, or delete a dictionary. |
| FR-CONTENT-022 | A user can **list dictionaries they own**. |
| FR-CONTENT-023 | *(Future)* An owner may grant an **Editor** role; *(future)* transfer ownership. Specified as forward-compatible; not built in this capability. |

### Visibility (CAP-4)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-030 | The owner can set visibility to **Private**, **Shared**, or **Public**. Default on creation is **Private**. |
| FR-CONTENT-031 | A **Private** dictionary is visible and selectable only to its owner. |
| FR-CONTENT-032 | A **Shared** dictionary is additionally visible/selectable to accounts on its **share list** (§11). |
| FR-CONTENT-033 | A **Public** dictionary is discoverable and selectable by any user, and **usable in rooms by anyone** once it has a published version. |
| FR-CONTENT-034 | Setting visibility to **Public** requires the dictionary to have ≥1 published version and to pass the **publication review gate** (§5) before it becomes discoverable. |
| FR-CONTENT-035 | Lowering visibility (e.g., Public→Private) **immediately** removes future discoverability/selectability; it **never** affects rooms that already pinned a version (§13). |

### Discovery (CAP-9)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-040 | A user can browse **Mine** (owned), **Official**, **Shared-with-me**, and **Public** catalogs. |
| FR-CONTENT-041 | Discovery results **exclude** any dictionary the actor is not permitted to see (§7). |
| FR-CONTENT-042 | Discovery lists **dictionaries** and expose their **current published version** for selection; drafts and non-current versions are not discoverable to non-owners. |
| FR-CONTENT-043 | Discovery supports **search/filter** by name and content type (behavioral requirement; mechanism is Technical Design). |
| FR-CONTENT-044 | *(Future)* A user can mark dictionaries as **Favorites**. Placeholder; not built here. |

### Sharing & Cloning (CAP-10)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-050 | The owner can **share** a dictionary with one or more specific accounts (grant **Viewer** access). |
| FR-CONTENT-051 | The owner can **revoke** any share grant. |
| FR-CONTENT-052 | Sharing grants access to the **current published version** for selection; recipients cannot edit, publish, re-share, or see the Draft. |
| FR-CONTENT-053 | A user can **clone** any dictionary version they may see (own / shared-to-them / public), creating a **new dictionary they own**, seeded from that version's words, starting **Private** with a Draft. |
| FR-CONTENT-054 | A clone records a **provenance reference** to its source version; the clone is otherwise **fully independent** (editing/deleting it never affects the source). |
| FR-CONTENT-055 | *(Future)* Ownership **transfer**. Placeholder; not built here. |

### Import / Export (CAP-7, CAP-8)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-060 | The owner can **import** a word collection into a Draft, choosing **append** or **replace**; import applies normalization (FR-CONTENT-006) and de-duplication (§9). |
| FR-CONTENT-061 | Import **reports** rejected entries (blank/duplicate/over-limit) without failing the whole operation; the Draft receives the accepted words. |
| FR-CONTENT-062 | The owner (or any actor permitted to see a version) can **export** a **published version's** word collection. |
| FR-CONTENT-063 | Import/export operate on **words only**; they never carry ownership, visibility, shares, or lifecycle state. |

### Gameplay Selection (CAP-11)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-070 | When creating a room, a host can **select a dictionary** from content they are permitted to see (own / shared / public / official). |
| FR-CONTENT-071 | Selection resolves to the dictionary's **current published version**; the room **pins that version identity** at match start ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). |
| FR-CONTENT-072 | If no dictionary is selected, the configured **default official dictionary** applies (preserves MVP behavior, [DM-S3](../02-business-analysis/13-dictionary-management.md#7-selection)). |
| FR-CONTENT-073 | Selecting an **unauthorized or unpublished** dictionary is rejected before match start; selecting one whose current version fails the play-time floor returns the existing `DICTIONARY_TOO_SMALL` / `DICTIONARY_NOT_FOUND` errors. |
| FR-CONTENT-074 | **Playing with** a selected published version requires **no authentication** of the joining players ([SEAM-1](01-business-vision.md#5-the-load-bearing-seam--authored-vs-consumed)). |

### Lifecycle & Moderation (CAP-2, CAP-12)
| ID | Requirement |
|----|-------------|
| FR-CONTENT-080 | The owner can **archive** a dictionary: it leaves all discovery/selection but its published versions remain reproducible for existing matches. |
| FR-CONTENT-081 | The owner can **restore** an archived dictionary to its prior visibility. |
| FR-CONTENT-082 | The owner can **request deletion**; deletion is honored only when **no live or reproducible match references** any of its versions, otherwise the dictionary is retained (archived) until unreferenced ([DEP-5](#14-constraints)). |
| FR-CONTENT-083 | A user can **report** a shared/public dictionary. |
| FR-CONTENT-084 | A **moderator** can **block** (remove from discovery/new selection) or **retire** a version in response to a report — acting on lifecycle state only, **never** editing words and **never** touching a running/completed match ([ADR-008 §11](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#11-moderation-architecture)). |
| FR-CONTENT-085 | Blocking/retiring **never** hard-deletes a version still referenced by a live/reproducible match. |

---

## 4. Business Rules

### Ownership & permissions
| ID | Rule |
|----|------|
| BR-CONTENT-001 | A dictionary has **exactly one owner**; ownership is never null, ambiguous, or duplicated. |
| BR-CONTENT-002 | Only the owner may author, validate, publish, set visibility, share, archive, or delete a dictionary. |
| BR-CONTENT-003 | Authoring, publishing, sharing, and clone-source access **require authentication**; consuming a published version in a room does **not**. |

### Immutability & versioning
| ID | Rule |
|----|------|
| BR-CONTENT-010 | A **published version is immutable**: its words never change after publication. |
| BR-CONTENT-011 | **Editing a published dictionary always produces a new draft/version** — never an in-place edit (corrections = new version). |
| BR-CONTENT-012 | A dictionary has **at most one mutable Draft**; all other content is immutable published versions. |
| BR-CONTENT-013 | Each version identity is **globally unique and immutable**; no two versions share an identity. |
| BR-CONTENT-014 | Published versions are **retained while referenced** by any live or reproducible match; they are never removed underneath such a match. |
| BR-CONTENT-015 | The **current published version** is the newest non-retired published version; if none exists, the dictionary is not selectable for play. |

### Gameplay isolation
| ID | Rule |
|----|------|
| BR-CONTENT-020 | **Running and completed matches always reference a pinned published version**; they never reference a Draft or a mutable dictionary. |
| BR-CONTENT-021 | **No content action** (publish, edit, visibility change, share, revoke, archive, delete, block, retire) **ever alters a match that already pinned a version**. |
| BR-CONTENT-022 | Content **selects words only**; it never affects counts, turn flow, or outcomes ([INV-D1](../02-business-analysis/10-business-invariants.md)). |
| BR-CONTENT-023 | Two rooms consuming the same version are **not coupled**; content is shared **read-only**, never shared mutable state ([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)). |

### Visibility & sharing
| ID | Rule |
|----|------|
| BR-CONTENT-030 | Default visibility is **Private**; a dictionary is never public by accident. |
| BR-CONTENT-031 | **Private content never appears** in any discovery, share, or selection surface to non-owners. |
| BR-CONTENT-032 | A dictionary must have **≥1 published version and pass the review gate** before it can become **Public/discoverable**. |
| BR-CONTENT-033 | Revoking a share or lowering visibility **removes future access only**; it never revokes a version already pinned by a room. |
| BR-CONTENT-034 | A **clone is a new, independent dictionary owned by the cloner**; it inherits words from a source version and a provenance reference, but no ownership, visibility, or shares. |

### Content type & scope
| ID | Rule |
|----|------|
| BR-CONTENT-040 | Every dictionary has a **content type** (official / user / …); all types obey the **same** lifecycle, immutability, versioning, and pinning. |
| BR-CONTENT-041 | The **one-per-region** constraint ([DM-C1](../02-business-analysis/13-dictionary-management.md#4-country-dictionary)) applies to **official** type only; user/community/org dictionaries are owner-scoped and **many-per-owner**. |
| BR-CONTENT-042 | Word-validity rules (§9) — including `DICTIONARY_MIN_WORDS` and uniqueness — apply to **all** content types. |

### Lifecycle & moderation
| ID | Rule |
|----|------|
| BR-CONTENT-050 | Lifecycle transitions follow §5; forbidden transitions are rejected. |
| BR-CONTENT-051 | Moderation acts on **lifecycle state** (block/retire), **never** on published words and **never** on a running/completed match. |
| BR-CONTENT-052 | An **archived or retired** version/dictionary is **not selectable** for new matches but remains **reproducible** for matches that pinned it. |
| BR-CONTENT-053 | Deletion is **logical and deferred** while any version is referenced by a live/reproducible match. |

---

## 5. Lifecycle

Two nested lifecycles: the **Version** lifecycle (primary) and the **Dictionary (container)** lifecycle.

### 5.1 Version lifecycle
```
Draft ──validate──► Validated ──publish──► Published ─(newest)─► [Current]
  ▲                                            │
  └──── discard / continue editing ────────────┘   (edits create a NEW Draft→…→Published)
                                             Published ──supersede──► Deprecated
                                             Published ──archive────► Archived
                                             Published ──retire─────► Retired  (terminal)
```
For **Public** dictionaries, an additional **Review** gate sits between Published and *Discoverable*:
`Published ──review/approve──► Discoverable` (block ⇒ back to non-discoverable). Private/Shared content skips
Review (report-driven moderation applies post-publication).

| State | Meaning | Selectable (new matches)? | Used by matches that pinned it? | Mutable? |
|-------|---------|:-------------------------:|:-------------------------------:|:--------:|
| **Draft** | The single mutable working copy. | No | No | **Yes** |
| **Validated** | Draft passed all §9 rules; ready to publish. Transient. | No | No | Yes (re-edit reverts to Draft) |
| **Published** | Immutable snapshot. | Yes (if current & authorized) | Yes | **No** |
| **Deprecated** | Superseded by a newer published version. | No | **Yes** | No |
| **Archived** | Withdrawn from selection; retained for reproducibility/audit. | No | **Yes** | No |
| **Retired** | Withdrawn from all new use (e.g., moderation). Terminal. | No | Yes, while still referenced | No |

**Allowed:** Draft→Validated→Published; Published→Deprecated (on newer publish); Published/Deprecated→Archived→
Retired; Published→Retired (emergency). **Forbidden:** any edit of Published+ content; Draft→Published skipping
validation; Retired→any; removing a version referenced by a live/reproducible match.

### 5.2 Dictionary (container) lifecycle
```
Active ──archive──► Archived ──restore──► Active
Active/Archived ──request-delete──► PendingDeletion ──(unreferenced)──► Deleted
```
An **Active** dictionary participates in discovery/selection per its visibility. **Archived** hides it while
preserving reproducibility. **PendingDeletion** blocks new use and completes only when no version is
referenced by a live/reproducible match (BR-CONTENT-053).

---

## 6. Ownership

| Aspect | Specification |
|--------|---------------|
| **Owner** | The account that created the dictionary (or received it via future transfer). Exactly one. Holds **all** rights: author, validate, publish, set visibility, share, revoke, archive, restore, delete, export. |
| **Editor** *(future)* | An account granted authoring rights by the owner (edit Draft, validate, publish). **Cannot** change ownership, delete, or change visibility. Placeholder — model reserves the role; not built here. |
| **Viewer** | An account with **read/select** access via a share grant, or any user for Public content. May **view/select the current published version, export, and clone**. **Cannot** edit, publish, see the Draft, re-share, or change lifecycle. |
| **Moderator** | A platform-trusted role acting on **shared/public** content lifecycle only (block/retire). Not an owner; cannot edit words or affect matches. |
| **Anonymous player** | Not an actor of authoring. May **consume** a published version selected for a room. |

Permission summary:

| Action | Owner | Editor* | Viewer | Moderator | Anonymous |
|--------|:-----:|:------:|:------:|:---------:|:---------:|
| Edit Draft / import | ✔ | ✔ | – | – | – |
| Validate / publish | ✔ | ✔ | – | – | – |
| Set visibility / share / revoke | ✔ | – | – | – | – |
| Archive / restore / delete | ✔ | – | – | – | – |
| View/select current version | ✔ | ✔ | ✔ | ✔ | ✔ (in a room) |
| Export / clone | ✔ | ✔ | ✔ | ✔ | – |
| Block / retire (shared/public) | – | – | – | ✔ | – |

\* future role.

---

## 7. Visibility

| Level | Who can discover/select | Behavior |
|-------|-------------------------|----------|
| **Private** *(default)* | Owner only. | Fully hidden from everyone else; selectable only by the owner as host. |
| **Shared** | Owner + accounts on the **share list**. | Recipients see and select the current published version; cannot edit/publish/re-share/see Draft (§6). |
| **Public** | Any user (discovery); anyone (in-room consumption). | Discoverable in the Public catalog after the review gate (§5); usable in rooms by anyone incl. anonymous players. |
| **Organization** *(future)* | Members of the owning organization. | Same model, org-scoped visibility; reserved, not built here. |

Cross-cutting behavior:
- Visibility is **explicit** and owner-controlled; changes take effect for **future** discovery/selection only,
  never retroactively on pinned matches (BR-CONTENT-033).
- **Public** requires published content + review (BR-CONTENT-032); **Private→Public** is a deliberate, gated action.
- No visibility level exposes a **Draft** to non-owners.

---

## 8. Versioning

| Aspect | Specification |
|--------|---------------|
| **Publishing** | Validates the Draft (§9); on success mints an **immutable Version** with a unique identity and an incrementing label; the Draft persists for further editing. |
| **Immutable versions** | Never change after publication; corrections are always **new versions** (BR-CONTENT-010/011). |
| **Draft creation** | Each dictionary has one Draft; after a publish, the Draft carries the just-published words forward as the editable baseline for the next version. **Discard** reverts the Draft to the last published version (or empty). |
| **Historical preservation** | All published versions are retained and reproducible while referenced; version history is owner-viewable; deprecated/archived versions remain reproducible (BR-CONTENT-014, [ADR-008 §9](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#9-versioning-model)). |
| **Current published version** | The newest non-retired published version; what selection/sharing resolves to (FR-CONTENT-015). |
| **Gameplay pinning** | At match start the room pins the **selected version's identity**; thereafter that match is fixed to it for life ([ADR-008 §8](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#8-match-pinning-critical)); publishing a newer version affects **future** matches only. |

---

## 9. Validation Rules

Applied at validate (FR-CONTENT-010) and enforced at publish (FR-CONTENT-011). All rules apply to **every**
content type (BR-CONTENT-042).

| ID | Rule | Outcome on failure |
|----|------|--------------------|
| V-CONTENT-1 | Each word is **non-blank** after normalization (not empty/whitespace-only). | Reject the word; report. |
| V-CONTENT-2 | Words are **unique within a version** — case-insensitive, trimmed, whitespace-collapsed. | Drop duplicates; report. |
| V-CONTENT-3 | A version has **≥ `DICTIONARY_MIN_WORDS` (25)** distinct usable words to publish ([INV-D2](../02-business-analysis/10-business-invariants.md)). | Block publish; report shortfall. |
| V-CONTENT-4 | A version has **≤ `DICTIONARY_MAX_WORDS`** words. *(New operational constant; default proposed in Technical Design, ratified in [Constants Catalog](../03-business-governance/03-business-constants-catalog.md).)* | Reject over-limit entries; report. |
| V-CONTENT-5 | Each word is within **per-word length bounds** *(new operational constant, as above)*. | Reject the word; report. |
| V-CONTENT-6 | *(Guidance, not machine rule)* Words are **single, guessable, culturally appropriate** terms (curation responsibility, [DM-Q1](../02-business-analysis/13-dictionary-management.md#9-word-quality--offensive-content)). | Curation/moderation, not publish-blocking. |
| V-CONTENT-7 | **Offensive/infringing content** *(future machine assist; today via moderation lifecycle)* is handled by **block/retire + corrected new version**, never in-place edit. | Post-publication moderation (§5, [ADR-008 §11](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#11-moderation-architecture)). |
| V-CONTENT-8 | A **single "language/consistency" expectation** is advisory: a dictionary's words are expected coherent for its intended audience; the engine remains **language-independent** ([INV-D1](../02-business-analysis/10-business-invariants.md)) and enforces no natural-language rule. | Curation guidance only. |

> **New constants flagged:** `DICTIONARY_MAX_WORDS` and per-word length bounds are introduced by this feature as
> **operational parameters** (not gameplay rules). Their defaults/ranges are proposed in Technical Design and
> ratified in the [Business Constants Catalog](../03-business-governance/03-business-constants-catalog.md); they
> never affect Codenames fidelity.

---

## 10. Discovery

| Catalog | Contents | Visible to |
|---------|----------|-----------|
| **Mine** | Dictionaries the actor owns (any state). | Owner. |
| **Official** | Official-type dictionaries ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md)). | All users; consumable by anyone in a room. |
| **Shared-with-me** | Dictionaries shared to the actor. | Grant recipients. |
| **Public** | Public dictionaries past the review gate. | All users. |
| **Favorites** *(future)* | User-marked dictionaries. | Owner. Placeholder. |

Behavior: discovery surfaces **dictionaries** and their **current published version**; never Drafts, private
content, or non-current versions to non-owners (FR-CONTENT-041/042). Search/filter by name and content type is
required behavior (mechanism deferred to Technical Design).

---

## 11. Sharing

| Operation | Specification |
|-----------|---------------|
| **Share** | Owner grants **Viewer** access to specific accounts; recipient gains view/select/export/clone of the current published version (BR-CONTENT-002, FR-CONTENT-050/052). |
| **Revoke** | Owner removes a grant; future access ends immediately; **pinned matches are unaffected** (BR-CONTENT-033). |
| **Clone** | Any actor who may see a version creates a **new owned dictionary** seeded from that version's words + a provenance reference; independent thereafter (FR-CONTENT-053/054, BR-CONTENT-034). |
| **Ownership transfer** *(future)* | Reserved; not built here (FR-CONTENT-055). |
| **Link/organization sharing** *(future)* | Reserved; the share model is account-grant based today. |

---

## 12. Import / Export

| Operation | Specification |
|-----------|---------------|
| **Import words** | Adds an external word collection into a **Draft** (append or replace); applies normalization + de-duplication; reports rejects without failing the batch (FR-CONTENT-060/061). Words only (BR-CONTENT — FR-CONTENT-063). |
| **Export dictionary** | Emits a **published version's** word collection to the actor (if permitted). Words only; no ownership/visibility/shares/lifecycle (FR-CONTENT-062/063). |
| **Clone published content** | See §11 (a Content Platform operation, not a raw data copy). |

> **Formats/encodings are explicitly out of scope** (OOS-7); only the **logical operations and their word-level
> semantics** are specified here.

---

## 13. Gameplay Integration

The Content Platform is **strictly upstream** of gameplay ([SEAM-2](01-business-vision.md#5-the-load-bearing-seam--authored-vs-consumed)).
It meets gameplay at exactly one contract:

1. **Selection is a lobby/room-creation choice.** A host selects a dictionary from permitted content; the system
   resolves it to the dictionary's **current published version** (FR-CONTENT-070/071).
2. **The room pins the version identity at match start**, into its authoritative aggregate — the existing
   [ADR-008 pinning](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#8-match-pinning-critical);
   the board's words are drawn **once** into immutable board state.
3. **Reaffirmed invariants** (nothing new for gameplay):
   - **Rooms pin published versions** — never Drafts or mutable dictionaries (BR-CONTENT-020).
   - **Active matches never change** — no content action touches a pinned match (BR-CONTENT-021, [AI-CONTENT-3/4/7](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-)).
   - **Gameplay never edits content** — content is read-only at runtime (BR-CONTENT-023, [AI-CONTENT-1/11](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-)).
   - **Determinism preserved** — same pinned version ⇒ same word set ([FF-CONTENT-9](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#15-architecture-fitness-functions-ff-content-)).
   - **No auth to play** — consuming a pinned version needs no account (FR-CONTENT-074).
4. **Selection as a Command; content reads as Queries** ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md#21-impact-on-future-adrs)): choosing/ resolving/pinning a version is part of the coordinated Start command; browsing/exporting are read-only queries against immutable content.

---

## 14. Constraints

Constraints inherited from prior ADRs and baseline, all binding on this feature:

| ID | Constraint | Source |
|----|-----------|--------|
| CON-1 | Published versions are **immutable**; corrections are new versions. | [ADR-008 AI-CONTENT-14](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-2 | A match **pins exactly one version identity**, fixed for its life. | [ADR-008 AI-CONTENT-4/13](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-3 | Content updates **never affect active/completed matches**. | [ADR-008 AI-CONTENT-7](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-4 | Content is **read-only at runtime**; rooms never mutate content. | [ADR-008 AI-CONTENT-1/11](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-5 | Content **never couples rooms**; shared read-only, not shared mutable. | [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md), [ADR-008 AI-CONTENT-12](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-6 | Content **selects words only** — never rules, counts, flow, outcomes. | [INV-D1](../02-business-analysis/10-business-invariants.md), [ADR-008 AI-CONTENT-8](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-7 | Version identities are **globally unique and immutable**. | [ADR-008 AI-CONTENT-9](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-8 | Moderation acts on **lifecycle only**, never on published content or a running match. | [ADR-008 AI-CONTENT-10](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-9 | Recovery/migration **preserve the pinned version identity** and board. | [ADR-005](../07-software-architecture/12-decisions/ADR-005-state-recovery-resilience.md)/[ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md), [ADR-008 AI-CONTENT-5/6](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-) |
| CON-10 | **Authentication** required to author/manage/share; **not** required to play with content. | [01 SEAM-1](01-business-vision.md#5-the-load-bearing-seam--authored-vs-consumed), C-2, [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md) |
| CON-11 | Authoring actions are **Commands**; content reads are **Queries**. | [ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md) |
| CON-12 | `DICTIONARY_MIN_WORDS` (25) & uniqueness apply to **all** content types. | [Constants](../_meta/00-canonical-constants-and-index.md), [INV-D2](../02-business-analysis/10-business-invariants.md) |
| CON-13 | Gameplay, rules, and constants are **never changed** by this feature. | Roadmap [G-1](../03-business-governance/06-product-roadmap.md#10-guardrails) |

---

## 15. Acceptance Criteria

| Capability | Acceptance criteria | Covers |
|-----------|---------------------|--------|
| **Authoring** | An authenticated user creates a Private dictionary with an empty Draft, adds/edits/removes words, and discards changes back to the last published state. | FR-001..006 |
| **Validation** | Validating a Draft with <25 distinct words, duplicates, or blanks reports each failure and does **not** publish; a compliant Draft validates clean. | FR-010, V-CONTENT-1..5 |
| **Versioning** | Publishing a valid Draft creates an immutable, uniquely-identified version; the Draft persists; the current-version pointer advances; history is viewable; a published version's words cannot be edited. | FR-011..016, BR-010/011/013/015 |
| **Ownership** | Only the owner can author/publish/share/set-visibility/delete; a non-owner attempting any is rejected; every dictionary always has exactly one owner. | FR-020/021, BR-001/002 |
| **Visibility** | Default is Private; Private content never appears to non-owners; Public requires a published version + review; lowering visibility affects future selection only, never pinned matches. | FR-030..035, BR-030..033 |
| **Discovery** | Mine/Official/Shared/Public catalogs return only permitted dictionaries and their current versions; drafts/private/non-current versions never leak. | FR-040..043, BR-031 |
| **Sharing & clone** | Sharing grants view/select to specific accounts; revoke ends future access but not pinned matches; a clone is a new owned, independent dictionary with provenance. | FR-050..054, BR-033/034 |
| **Import/export** | Import appends/replaces into a Draft with normalization + dedup and reports rejects; export emits a published version's words only. | FR-060..063 |
| **Gameplay selection** | A host selects permitted content; the room pins the current published version; anonymous players play it without accounts; unauthorized/unpublished/too-small selections are rejected. | FR-070..074, BR-020..023 |
| **Lifecycle & moderation** | Archive/restore/delete honor referenced-version retention; report→block→retire remove content from new selection without editing words or affecting any pinned match. | FR-080..085, BR-050..053, CON-3/8 |
| **Determinism (guardrail)** | For any completed match, re-resolving its pinned version reproduces the identical word source regardless of later content changes. | CON-2/3/9, [FF-CONTENT-3/7](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#15-architecture-fitness-functions-ff-content-) |

---

## 16. Traceability

| Requirement group | Business Vision | ADR-008 | ADR-009 | ADR-010 | Future ADR-011 |
|-------------------|-----------------|---------|---------|---------|----------------|
| Authoring (FR-001..006) | CG-1, PP-11/12 | §7 (identity), §14 | identity seam (auth to author) | Commands | Ownership/authoring model |
| Validation (FR-010, V-*) | PP-7/8, VA-6 | §9, FF-9 | – | – | Validation invariants |
| Versioning (FR-011..016, BR-010..015) | PP-1/11 | §7/§9, AI-CONTENT-14 | – | – | Immutability/versioning |
| Ownership (FR-020..023, BR-001/002) | PP-5, §8 | §11 (ownership) | durable identity | – | Ownership decision |
| Visibility (FR-030..035, BR-030..033) | PP-6, §8 | §12 (distribution) | – | Queries | Visibility model |
| Discovery (FR-040..044) | CG-3, §9 | §12 | – | Queries | Isolation/visibility |
| Sharing/clone (FR-050..055, BR-034) | CG-2 | §14 (identity/immutability) | – | Commands | Sharing/cloning |
| Import/export (FR-060..063) | CG-2 | §9 (words only) | – | Commands/Queries | (encoding → Tech Design) |
| Gameplay selection (FR-070..074) | SEAM-1/2, PP-9 | §8 (pinning), §21 | account-free play | selection = Command | Integration contract |
| Lifecycle/moderation (FR-080..085, BR-050..053) | PP-13, CG-7 | §7/§11, AI-CONTENT-10 | – | Commands | Lifecycle/moderation |
| Constraints (CON-1..13) | §6.3 red lines | §14 invariants | identity/auth | interaction stance | invariants restated |

---

> **Additive enrichment (v1.1).** Sections 17–25 below are **strictly additive** — they add metadata,
> pipeline, origin, search, statistics, soft-delete, official-boundary, deferred-collaboration, and non-goal
> detail. They introduce **no** change to the ownership, visibility, versioning, gameplay-pinning,
> authentication-seam, lifecycle, business rules, or functional requirements of v1.0. **No existing ID, section,
> or traceability link is altered.**

---

## 17. Dictionary & Version Metadata

Metadata is split by **mutability**: **Dictionary metadata** is mutable and lives on the owned container;
**Version metadata** is captured at publish and is **immutable** thereafter (part of the immutable snapshot).
This split is the metadata expression of BR-CONTENT-010/012 and adds no new mutable state to versions.

### 17.1 Dictionary Metadata *(mutable; belongs to the container)*
| Field | Meaning | Notes |
|-------|---------|-------|
| MD-1 **Title** | Human-readable name. | Editable by owner (FR-CONTENT-002). |
| MD-2 **Description** | Free-text purpose/summary. | Editable (FR-CONTENT-002). |
| MD-3 **Owner** | The single owning account. | BR-CONTENT-001; changes only via future transfer. |
| MD-4 **Visibility** | Private / Shared / Public (Organization future). | §7; owner-controlled. |
| MD-5 **Content Type** | official / user / … | BR-CONTENT-040; set at creation, not freely re-typed. |
| MD-6 **Tags** | Owner-supplied keywords for discovery. | Searchable (§20); advisory only. |
| MD-7 **Language** | Intended audience language (advisory label). | Never enforced by the engine ([INV-D1](../02-business-analysis/10-business-invariants.md), V-CONTENT-8). |
| MD-8 **Region / Culture** *(optional)* | Cultural scope; **required for official** (DM-C1), optional for user content. | BR-CONTENT-041. |
| MD-9 **Created Date** | When the dictionary was created. | System-set. |
| MD-10 **Last Updated** | When the container/metadata/draft last changed. | System-maintained. |
| MD-11 **Current Published Version** | Pointer to the newest non-retired version. | FR-CONTENT-015. |

### 17.2 Version Metadata *(immutable; captured at publish)*
| Field | Meaning | Notes |
|-------|---------|-------|
| VM-1 **Version Number/Label** | Incrementing, owner-visible label. | FR-CONTENT-013. |
| VM-2 **Version Identity** | Globally-unique immutable identity. | BR-CONTENT-013, CON-7. |
| VM-3 **Publish Date** | When the version was published. | Immutable. |
| VM-4 **Validation Status** | Result recorded at publish (passed). | Snapshot of §9 outcome. |
| VM-5 **Version Notes** | Optional owner note describing the change. | Immutable once published. |
| VM-6 **Word Count** | Distinct usable words in the snapshot. | Derived at publish. |
| VM-7 **Provenance** | Origin of the words (§19): manual / clone(source version) / import / official-seed. | Immutable; supports clone lineage (FR-CONTENT-054). |

> **Rule reaffirmed:** editing any Version metadata is impossible; a correction is a **new version** with its own
> immutable metadata (BR-CONTENT-011).

---

## 18. Publication Pipeline & Review

Elaborates §5 without changing it. **Only Public content passes moderation review**; Private and Shared content
publish directly once validated.

**Private / Shared:**
```
Draft ──validate──► Validated ──publish──► Published   (immediately selectable to owner / share list)
```
**Public:**
```
Draft ──validate──► Validated ──publish──► Published ──submit──► Pending Review ──approve──► Discoverable
                                                                     │
                                                                     └── reject ──► Published (not discoverable)
```
| Stage | Applies to | Meaning |
|-------|-----------|---------|
| **Pending Review** | Public only | Awaiting moderator decision; the version is already **immutable and playable by the owner/shared parties**, but **not yet publicly discoverable**. |
| **Discoverable** | Public only | Approved; appears in the Public catalog (FR-CONTENT-033/034, BR-CONTENT-032). |
| **Rejected** | Public only | Not discoverable; the immutable version still exists and remains usable privately; owner may revise via a new version. |

**Invariants held:** review acts on **discoverability lifecycle**, never on words (CON-8); a Public dictionary
cannot be discoverable without a published version + approval (BR-CONTENT-032); rejection never edits or deletes
the version.

---

## 19. Content Origins (Import Sources)

Every Draft's words originate from one or more **logical origins**, recorded for provenance (VM-7). Origins are
behavioral, not formats (formats remain out of scope, OOS-7).

| Origin | Meaning | Relates to |
|--------|---------|-----------|
| ORG-1 **Manual Creation** | Words typed directly into the Draft. | FR-CONTENT-003/004 |
| ORG-2 **Clone** | Seeded from a source version's words; carries a provenance reference. | FR-CONTENT-053/054 |
| ORG-3 **Import** | Brought in from an external word collection (append/replace). | FR-CONTENT-060/061 |
| ORG-4 **Official Seed** | Seeded from an official dictionary as a starting point (a clone whose source is official). | §23, FR-CONTENT-053 |

A Draft may combine origins (e.g., clone then manual edits); provenance records the **originating** source for
lineage. Origins never carry ownership, visibility, or lifecycle (FR-CONTENT-063, BR-CONTENT-034).

---

## 20. Search Metadata

Discovery search/filter (FR-CONTENT-043) operates over these **business fields** only; mechanism is Technical
Design.

| Field | Searchable | Source |
|-------|:---------:|--------|
| Title (MD-1) | ✔ | Dictionary metadata |
| Description (MD-2) | ✔ | Dictionary metadata |
| Tags (MD-6) | ✔ | Dictionary metadata |
| Owner (MD-3) | ✔ (e.g., "by owner") | Dictionary metadata |
| Language (MD-7) | ✔ (filter) | Dictionary metadata |
| Content Type / Region (MD-5/8) | ✔ (filter) | Dictionary metadata |

Search **never** exposes content the actor may not see (FR-CONTENT-041, BR-CONTENT-031): private dictionaries,
Drafts, and non-current versions never appear to non-owners regardless of matching fields.

---

## 21. Statistics (Future)

Reserved, **future** metadata — not built in this capability, listed so the model anticipates it (extends
[01 §12 OOS-5](01-business-vision.md#12-out-of-scope)). Purely observational; may **never** affect gameplay,
selection fairness, or content mutability.

| Future field | Meaning |
|--------------|---------|
| STAT-1 **Plays** *(future)* | How often the dictionary's versions were used in rooms. |
| STAT-2 **Favorites** *(future)* | Count of users who favorited it (FR-CONTENT-044). |
| STAT-3 **Last Played** *(future)* | Most recent room usage. |
| STAT-4 **Published Versions** *(future)* | Count of published versions. |

> These are **read-only projections** ([ADR-010](../07-software-architecture/12-decisions/ADR-010-command-query-strategy.md));
> deriving them never mutates content nor influences a running match.

---

## 22. Soft-Delete Lifecycle

Elaborates FR-CONTENT-082 and §5.2 without changing them. Deletion is **logical and retention-bounded**:

```
Owner Delete Request ──► Hidden ──► Retention ──► Permanent Delete
                                       │
                                       └── (blocked while any published version is referenced
                                            by a live or reproducible match)
```
| Stage | Meaning | Selectable / Discoverable? | Reproducible for pinned matches? |
|-------|---------|:--------------------------:|:--------------------------------:|
| **Hidden** | Removed from all discovery/selection on request. | No | Yes |
| **Retention** | Held while any version is still referenced (live or reproducible match). | No | Yes |
| **Permanent Delete** | Physically removable **only** when no published version is referenced. | No | N/A (nothing references it) |

**Invariant:** permanent deletion occurs **only when no published version is referenced** (BR-CONTENT-014/053,
CON-9, [Data Lifecycle](../03-business-governance/05-data-lifecycle-retention.md)); a match's board words persist
as immutable board state regardless ([ADR-008 §13](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#13-recovery-model-using-adr-005)).

---

## 23. Official Content Boundary

Official dictionaries are the **official content type** (BR-CONTENT-040) and obey the same lifecycle/immutability/
pinning — but they are **not authored through Dictionary Management's user authoring surface**.

| OFF | Rule |
|-----|------|
| OFF-1 | **Official content is managed only by platform administrators / the Content-Localization function** ([DM-O1](../02-business-analysis/13-dictionary-management.md#11-business-ownership)); ordinary users **cannot** create, edit, publish, or retire official content. |
| OFF-2 | Users **may consume** official content (select in rooms) and **may clone** it as a starting point (Official Seed, ORG-4) — the clone becomes an ordinary **user** dictionary they own (BR-CONTENT-034). |
| OFF-3 | Official content keeps its MVP constraints, incl. **one-per-region** (DM-C1); user clones of it are **not** bound by DM-C1 (BR-CONTENT-041). |
| OFF-4 | The user authoring surface (FR-CONTENT-001..021) applies to **user-type** dictionaries; official content follows its existing [Dictionary Management](../02-business-analysis/13-dictionary-management.md) governance. |

---

## 24. Future Collaboration (Deferred)

Reserved and explicitly **deferred** (extends FR-CONTENT-023/055, [01 §12 OOS-5](01-business-vision.md#12-out-of-scope)).
Named so the ownership/permission model (§6) stays forward-compatible; **none is built in this capability**.

| Deferred | Meaning | Model reservation |
|----------|---------|-------------------|
| COL-1 **Editors** | Additional accounts granted authoring rights by the owner. | Editor role reserved in §6; still single **owner** (BR-CONTENT-001). |
| COL-2 **Comments** | Discussion on a dictionary. | Metadata-adjacent; never affects words/versions. |
| COL-3 **Ratings** | User ratings of public content. | Observational (like §21); never affects fairness. |
| COL-4 **Reviews** | Written reviews of public content. | Observational; distinct from **moderation review** (§18). |

---

## 25. Non-Goals

This feature (and the Content Platform generally) **never**:

| NG | Non-goal |
|----|----------|
| NG-CONTENT-1 | Changes **gameplay**. |
| NG-CONTENT-2 | Changes **board generation** (beyond supplying the pinned word source). |
| NG-CONTENT-3 | Changes **game rules**. |
| NG-CONTENT-4 | Changes **scoring / win-loss resolution**. |
| NG-CONTENT-5 | Changes **turn flow**. |
| NG-CONTENT-6 | Changes the **canonical constants** ([Constants](../_meta/00-canonical-constants-and-index.md)). |
| NG-CONTENT-7 | Introduces **AI generation**. |
| NG-CONTENT-8 | Introduces **monetization**. |
| NG-CONTENT-9 | Introduces a **marketplace**. |
| NG-CONTENT-10 | Requires **authentication to play** with published content ([SEAM-1](01-business-vision.md#5-the-load-bearing-seam--authored-vs-consumed)). |
| NG-CONTENT-11 | Lets **content mutate a running or completed match** (CON-3, BR-CONTENT-021). |
| NG-CONTENT-12 | Chooses any **technology** (storage/API/format/framework) — that is Technical Design. |

> NG-CONTENT-1..6 restate Roadmap [G-1](../03-business-governance/06-product-roadmap.md#10-guardrails) at feature
> scope; NG-CONTENT-10/11 are the feature's hardest red lines and map to guardrail metrics
> [SM-11/13](01-business-vision.md#11-success-metrics).

---

## 26. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-11 | Initial consolidated Feature Specification for **Dictionary Management** (replaces Business Requirements + Use Cases + SRS Addendum): scope, 12 capabilities, functional requirements (FR-CONTENT-001..085), business rules (BR-CONTENT-001..053), nested version/dictionary lifecycle, ownership & permission matrix, visibility model, versioning, validation rules (V-CONTENT-1..8, two new operational constants flagged), discovery, sharing/cloning, import/export (formats out of scope), gameplay integration contract, inherited constraints (CON-1..13), acceptance criteria per capability, and full traceability to the Business Vision and ADR-008/009/010 plus forward-reference to ADR-011. Technology-neutral; no gameplay change. |
| 1.1 | 2026-07-11 | **Additive enrichment only** (no behavior/ID/traceability change): §17 Dictionary vs Version metadata (mutable vs immutable split); §18 publication pipeline & Public-only review; §19 content origins/import sources; §20 search metadata; §21 future statistics; §22 soft-delete lifecycle detail; §23 official-content boundary; §24 deferred collaboration; §25 explicit Non-Goals. Existing sections 1–16 and all IDs unchanged. |
