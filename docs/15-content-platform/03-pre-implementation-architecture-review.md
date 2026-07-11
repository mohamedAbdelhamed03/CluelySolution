# Content Platform — 03. Pre-Implementation Architecture Review

| | |
|---|---|
| **Bounded Context** | Content Platform (Dictionary Management) |
| **Version** | 1.0 |
| **Status** | Review record — **advisory; changes no frozen decision, ID, or behavior** |
| **Reviewer role** | Principal Software Architect — pre-implementation gate |
| **Scope** | [01 — Business Vision](01-business-vision.md), [02 — Feature Specification v1.1](02-feature-specification.md), [ADR-011](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md), treated as one system. |
| **Mandate** | **Strengthen only.** No new features/requirements, no redesign, no aggregate split/merge, no change to ownership/versioning/visibility/gameplay/ADR decisions or any requirement/rule ID. |
| **Technology** | Neutral. |

> **How to read the recommendations.** Findings are classified **Critical / Important / Nice-to-Have**. Every
> proposed formalization is given a **non-binding `REC-*` identifier** and mapped to where it *would* live if
> adopted (e.g., "next free `BR-CONTENT-054`"). **No frozen ID is renamed or renumbered here.** All `REC-*`
> items are *formalizations of behavior already specified or entailed* — none invents new behavior.

## Table of Contents
1. [Vocabulary Review](#1-vocabulary-review)
2. [Aggregate Review](#2-aggregate-review)
3. [Invariant Review](#3-invariant-review)
4. [Lifecycle Review](#4-lifecycle-review)
5. [Authorization Review](#5-authorization-review)
6. [Ownership Review](#6-ownership-review)
7. [Versioning Review](#7-versioning-review)
8. [Gameplay Boundary Review](#8-gameplay-boundary-review)
9. [Failure Scenario Review](#9-failure-scenario-review)
10. [Future Extensibility Review](#10-future-extensibility-review)
11. [Consistency Review](#11-consistency-review)
12. [Missing Business Rules (Formalizations)](#12-missing-business-rules-formalizations)
13. [Missing Acceptance Criteria](#13-missing-acceptance-criteria)
14. [Architecture Test Opportunities](#14-architecture-test-opportunities)
15. [Domain (Unit) Test Opportunities](#15-domain-unit-test-opportunities)
16. [Integration Test Opportunities](#16-integration-test-opportunities)
17. [Security Review](#17-security-review)
18. [Performance Review](#18-performance-review)
19. [Documentation Review](#19-documentation-review)
20. [Final Readiness Report](#20-final-readiness-report)
21. [Revision History](#21-revision-history)

---

## 1. Vocabulary Review

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| V-1 | **"Active" (ADR-008) vs "Current published version" (Feature Spec FR-CONTENT-015).** Two names for the region's/dictionary's selectable version. Not a conflict, but two labels for one concept. | Important | Fold into [ADR-000](../07-software-architecture/12-decisions/ADR-000-architecture-vocabulary.md): state that **"current published version"** (Content Platform, user content) is the general form of **"Active version"** (ADR-008, official content). Neither is redefined. |
| V-2 | **"Review" is overloaded**: moderation **Review** (publication gate, Feature §18) vs future user **Reviews** (COL-4). The spec already distinguishes them (§24 note). | Nice-to-Have | Add a one-line glossary disambiguation; keep both names but always qualify the future one as "user reviews". |
| V-3 | **Withdrawal verbs — Archive / Block / Retire / Deprecate** all remove content from some use. Individually defined but never contrasted side-by-side. | Important | Add a **withdrawal-semantics table** (below) to ADR-000 / glossary; no behavior change. |
| V-4 | **"Version Number" / "Version Label" / "Version identity"** (VM-1/VM-2) — human label vs machine identity. Slight overload. | Nice-to-Have | Clarify in ADR-000: **Version Label** = human-facing incrementing number; **Version Identity** = globally-unique immutable machine identity. Distinct fields (already both present). |
| V-5 | ADR-011 §21 already mandates folding Content-Platform terms into ADR-000 on acceptance. | Important | Execute the ADR-000 update as the **first documentation task** of implementation (see [§19](#19-documentation-review)). |

**Withdrawal-semantics table (recommended for ADR-000/glossary — clarification only):**

| Term | Acts on | Reversible? | Removes from new selection? | Affects pinned matches? |
|------|---------|:-----------:|:---------------------------:|:-----------------------:|
| **Deprecated** | Version (auto on newer publish) | n/a (state) | Yes | No |
| **Archived** | Version or Dictionary (owner) | Yes (restore) | Yes | No |
| **Blocked** | Version (moderator) | **Yes (re-review)** — see [L-4](#4-lifecycle-review) | Yes | No |
| **Retired** | Version (moderator/owner) | **No (terminal)** | Yes | No |

---

## 2. Aggregate Review

The single **Dictionary aggregate** (root = Dictionary; owns Draft, Version history, metadata, visibility, share
grants) is sound and correctly the authoring consistency boundary ([ADR-011 §6.2](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md#62-aggregates)).
Findings:

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| A-1 | **Two writer roles on one aggregate.** The owner writes authoring commands; a **moderator** also issues commands (block/retire/approve) — a *second, restricted* writer. ADR-011 covers *what* moderation may do (AI-CP-13) but not that it is a **distinct restricted command source** on the aggregate. | Important | Formalize (`REC-BR`, see [§12 REC-1](#12-missing-business-rules-formalizations)): moderator commands are a **restricted lifecycle-only** command set on the Dictionary aggregate, serialized with owner commands by the same single-writer boundary. No new aggregate. |
| A-2 | **Clone provenance reference (VM-7) crosses aggregate instances.** A clone's provenance points at a *source Version identity* in another Dictionary aggregate. Whether that reference **retains** the source (blocks its deletion) is unspecified. | Important | Formalize ([§12 REC-2](#12-missing-business-rules-formalizations)): provenance is an **informational, non-retaining** reference; it may **dangle** if the source is later deleted/retired. Clones stay independent (AI-CP-8); provenance never keeps a source alive. |
| A-3 | **Review/moderation state lives on the Version** (Pending Review / Discoverable / Blocked) — correctly *inside* the Dictionary aggregate, not a separate Moderation aggregate. | — (confirmation) | Keep as-is. Do **not** introduce a Moderation aggregate; it would create a cross-aggregate invariant. |
| A-4 | **Share grants belong to the Dictionary aggregate** (ADR-011 §6.2). Recipient identities are *references*, not owned entities. | — (confirmation) | Keep. Revocation is an aggregate command. |
| A-5 | **"Current published version" is derived state** (newest non-retired published). Is it stored (pointer) or computed? FR-CONTENT-015 implies a maintained pointer; recomputation triggers (retire/block/publish) are implicit. | Important | Formalize recomputation ([§12 REC-3](#12-missing-business-rules-formalizations)); treat the pointer as **aggregate-maintained derived state** recomputed on every publish/retire/block. |

**Verdict:** every invariant maps to the Dictionary aggregate; the only cross-instance link (provenance) is
correctly non-authoritative once A-2 is formalized.

---

## 3. Invariant Review

Existing invariants ([ADR-011 AI-CP-1..15](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md#12-architectural-invariants-ai-cp-),
extending ADR-008 AI-CONTENT-1..14) are coherent and non-conflicting. Gaps found — all are *entailed* by existing
rules and merely unstated:

| # | Missing/implicit invariant | Class | Proposed (non-binding) |
|---|----------------------------|-------|------------------------|
| I-1 | **A Version belongs to exactly one Dictionary** (composition; no shared Versions). Entailed but not explicit. | Important | `REC-INV-1` → candidate **AI-CP-16**. |
| I-2 | **The current-published-version pointer references a published, non-retired Version, or is null** (⇒ not selectable). Entailed by BR-CONTENT-015. | Important | `REC-INV-2` → candidate **AI-CP-17**; ties to [A-5](#2-aggregate-review). |
| I-3 | **Blocking/retiring the current version recomputes the pointer**; if none remain, the dictionary becomes non-selectable while remaining reproducible for pinned matches. | Important | `REC-INV-3` (behavioral); see [§9 F-15](#9-failure-scenario-review). |
| I-4 | **A Version's word set never changes cardinality after publish** (Word Count VM-6 is immutable). Entailed by AI-CP-4. | Nice-to-Have | `REC-INV-4`. |
| I-5 | **Provenance is non-authoritative** (may dangle) — see [A-2](#2-aggregate-review). | Important | `REC-INV-5`. |
| I-6 | **Cross-boundary invariant "Room references a Version by identity" (AI-CP-10)** is *owned by the seam contract*, enforced on the Gameplay side (room stores identity). Ownership of this invariant should be named to avoid it being "everyone's and no one's". | Nice-to-Have | Document as a **seam invariant**, enforced by Gameplay's pinning (ADR-008 §8); Content Platform guarantees only that a published identity exists and is immutable. |

No **conflicting** or **duplicate** invariants found. No **unowned** invariant remains after I-6 is labeled.

---

## 4. Lifecycle Review

Version: `Draft → Validated → Published → (Deprecated) → Archived → Retired`; Public adds
`Published → Pending Review → Discoverable`. Dictionary: `Active → Archived → PendingDeletion → Deleted`. Review:

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| L-1 | **Terminal states** — `Retired` (Version) and `Deleted` (Dictionary) are terminal but not labeled as such in one place. | Nice-to-Have | Mark terminal states explicitly in the lifecycle tables. |
| L-2 | **`PendingDeletion` cancelability** unspecified — can an owner **cancel a delete request** during Retention? | Important | Formalize ([§12 REC-4](#12-missing-business-rules-formalizations)): delete requests are **cancellable during Retention** (`PendingDeletion → Active/Archived`); only physical removal (unreferenced) is irreversible. |
| L-3 | **`Validated` is transient**; any Draft edit returns it to `Draft`. Documented in §5.1 but the "edit invalidates Validated" transition is implicit. | Nice-to-Have | State the `Validated → Draft (on edit)` transition explicitly. |
| L-4 | **Is `Blocked` reversible?** Moderation can block a Public version's discoverability; whether a blocked version can be **re-reviewed/unblocked** is unspecified. `Retired` is terminal; `Blocked` should not be. | Important | Formalize ([§12 REC-5](#12-missing-business-rules-formalizations)): **Block is reversible** (`Blocked → Pending Review → Discoverable`); **Retire is terminal**. Distinguishes recoverable moderation from permanent withdrawal. |
| L-5 | **Forbidden transitions** are listed for Version (§5.1) but not enumerated for the **Dictionary container**. | Nice-to-Have | Add container forbidden transitions (e.g., `Deleted → any`, `PendingDeletion → Public` while referenced). |
| L-6 | **Label counter on failed publish** — does the version label advance if publish fails validation? | Nice-to-Have | Formalize: the **label advances only on successful publish** ([§12 REC-6](#12-missing-business-rules-formalizations)). |

Every state and the main transitions are documented; L-2/L-4 are the two real gaps (both produce a
**deterministic** outcome once formalized).

---

## 5. Authorization Review

Matrix in [Feature Spec §6](02-feature-specification.md#6-ownership) covers most operations. Gaps:

| # | Operation | Gap | Class | Recommendation |
|---|-----------|-----|-------|----------------|
| Z-1 | **Select (host, possibly anonymous)** | An **anonymous host's visibility scope** is unstated. They have no identity, so cannot see Mine/Shared. | Important | Formalize ([§12 REC-7](#12-missing-business-rules-formalizations)): an **anonymous host may select only Official and Public** content; an **authenticated host** additionally selects **Mine + Shared-with-me**. Preserves SEAM-1 and prevents any private/shared exposure to anonymous actors. |
| Z-2 | **Search / browse discovery** | Who may browse each catalog? Implicitly authenticated (FR-CONTENT-040). Anonymous browsing of the Public catalog is neither granted nor denied. | Nice-to-Have | State: **authoring/discovery surfaces require authentication**; anonymous actors interact with content only via **in-room selection** (Z-1 scope). No behavior change — just closes the ambiguity. |
| Z-3 | **Report** | FR-CONTENT-083 says "a user"; anonymous reporting unspecified. | Nice-to-Have | Restrict **report to authenticated users** (accountability); anonymous reporting deferred. |
| Z-4 | **Moderator assignment** | *Who grants the Moderator role* is out of scope (tooling). | — | Confirm as Non-Goal (OOS-3); note the role is platform-granted, enforced server-side. |
| Z-5 | **Every read enforces visibility server-side** | The matrix implies it; not stated as an authorization invariant. | Important | See [§17 SEC-2](#17-security-review) — make server-side visibility enforcement an explicit security rule. |

No **privilege gaps** (no operation is left with an undefined actor) once Z-1 is formalized.

---

## 6. Ownership Review

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| O-1 | **Exactly one owner** (AI-CP-1) — sound. | — | Keep. |
| O-2 | **Clone ownership = cloner** (BR-CONTENT-034) — sound; independent, provenance-tagged. | — | Keep; formalize provenance non-retention ([A-2](#2-aggregate-review)). |
| O-3 | **Official ownership = platform** ([Feature §23](02-feature-specification.md#23-official-content-boundary)) — sound and boundaried. | — | Keep. |
| O-4 | **Owner account deletion (future).** Accounts are a Phase-2 dependency. When an owner's account is deleted, the fate of owned Dictionaries — and the tension with **referenced-Version retention (AI-CP-15)** — is unspecified. A naïve cascade delete would violate retention. | **Important (forward)** | Flag as a **forward policy gap** for the account-deletion/privacy design: owned content must be **reassigned/anonymized/archived**, never hard-deleted while a Version is referenced. Not blocking this capability (no accounts yet), but must be resolved before account deletion ships. |
| O-5 | **Future organization ownership & editor role** — reserved additively ([ADR-011 §6.3](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md#63-ownership-model)). Single-owner invariant still holds (org identity = the one owner). | — | Keep; no ambiguity. |

No ambiguous or duplicate ownership. O-4 is the one real forward gap and is inherited from the (future) accounts
dependency, not from this design.

---

## 7. Versioning Review

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| VR-1 | **Draft uniqueness** (≤1 Draft, AI-CP-3) — sound. | — | Keep; architecture test [AT-4](#14-architecture-test-opportunities). |
| VR-2 | **Version immutability** (AI-CP-4) — sound. | — | Keep. |
| VR-3 | **Version identity uniqueness** (AI-CP-5) — mechanism deferred to Tech Design; must be **collision-free and unguessable** (see [SEC-1](#17-security-review)). | Important | Directive to Tech Design: identity is globally unique **and** non-enumerable. |
| VR-4 | **Historical versions retained while referenced** (AI-CP-15) — sound; interacts with soft-delete (§22) and O-4. | — | Keep. |
| VR-5 | **Pinned version** (AI-CP-12) — sound (ADR-008 seam). | — | Keep. |
| VR-6 | **Selection cannot pick an arbitrary/older version** — selection resolves to the **current** published version (FR-CONTENT-071). This is a deliberate constraint (smaller attack surface, simpler UX). Not stated as an invariant. | Nice-to-Have | Formalize: **gameplay pins the current published version only** — never a Draft, older, deprecated, or non-current version chosen ad hoc. Reinforces [§8](#8-gameplay-boundary-review). |
| VR-7 | **Empty/short publish blocked** (V-CONTENT-3, ≥25) — sound. | — | Keep. |

No missing versioning edge case beyond VR-3 (identity properties) and VR-6 (explicit selection constraint).

---

## 8. Gameplay Boundary Review

The boundary is the strongest part of the design. Verified against the eight prohibitions:

| Gameplay must NOT… | Guaranteed by | Status |
|--------------------|---------------|--------|
| Edit content | AI-CP-9, FF-CP-001 (no Room→Dictionary write path) | ✔ |
| See Drafts | FR-CONTENT-042, FF-CP-002 (matches reference published Versions only) | ✔ |
| See unpublished/non-current versions | FR-CONTENT-042/071 (resolves to current published) + VR-6 | ✔ |
| Mutate dictionaries | FF-CP-001 | ✔ |
| Mutate versions | AI-CP-4, FF-CP-005 | ✔ |
| Break room isolation | ADR-007, AI-CP-11 (shared read-only, non-coupling) | ✔ |
| Break determinism | AI-CP-12, ADR-008 FF-9 (same pinned Version ⇒ same words) | ✔ |
| Break replay | ADR-008 FF-3/7, ADR-011 §14.8 (identity + immutable board) | ✔ |
| Break ADR-008 | ADR-011 §15 compliance row (extends, redefines nothing) | ✔ |

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| G-1 | The **board stores drawn words as immutable board state AND the pinned Version identity** (ADR-011 Alt E refinement). This double-store is the correctness linchpin; it should be an explicit architecture test. | Important | Add [AT-6](#14-architecture-test-opportunities). |
| G-2 | Confirm no read path lets a room **enumerate** a dictionary's version history or draft. | Important | Architecture test [AT-7](#14-architecture-test-opportunities); server-side visibility ([SEC-2](#17-security-review)). |

**Verdict:** the gameplay boundary is airtight; findings are test-hardening, not defects.

---

## 9. Failure Scenario Review

Expanding ADR-011 §14's ten scenarios to the full requested set — every one has a **deterministic** outcome:

| # | Scenario | Deterministic outcome | Basis |
|---|----------|-----------------------|-------|
| F-1 | **Concurrent publish** (owner, two sessions) | Single-writer aggregate serializes; two publishes ⇒ two ordered Versions (or the second no-ops if content-identical, see F-13). | §6.2, ADR-010 |
| F-2 | **Delete during publish** | Publish and delete are serialized aggregate commands; whichever commits first wins; a delete after publish enters Retention (version now exists/possibly referenced). | ADR-011 §14.1/14.3 |
| F-3 | **Clone while source publishing** | Clone binds to a **specific already-published Version**; a concurrent new publish is irrelevant to the clone. | AI-CP-8, §14.5 |
| F-4 | **Archive during gameplay** | Future selection stops; **pinned matches unaffected**; board state + pinned identity persist. | AI-CP-7 |
| F-5 | **Visibility change during gameplay** | Same as F-4 — future selection only. | AI-CP-7, §14.4 |
| F-6 | **Review rejection** | Immutable Version remains usable privately/shared; not discoverable; owner revises via new Version. | §14.2 |
| F-7 | **Publish rejection (validation)** | No Version created; Draft unchanged; label not advanced (REC-6). | §14.1, L-6 |
| F-8 | **Owner deleted (future)** | **Forward gap O-4** — must reassign/anonymize/archive, never violate retention. | O-4 |
| F-9 | **Organization removed (future)** | Same policy family as F-8; org-owned content archived/transferred, referenced Versions retained. | O-4/O-5 |
| F-10 | **Import failure** | Batch reports rejects; accepted words applied to Draft; operation never partially corrupts a Version (imports touch Drafts only). | FR-CONTENT-060/061 |
| F-11 | **Validation failure** | Reported per-rule; no publish; Draft editable. | §9, §14.7 |
| F-12 | **Corrupt Draft** | Cannot publish (validation gate); never a play source. | FF-CP-002, §14.7 |
| F-13 | **Duplicate publish request / retry publish** | **Requires idempotency** — a retried identical publish must **not** create a second Version. Architecturally: publish is an **idempotent command** keyed by client request id (ADR-010 command model). | **See REC-8 / SEC-4** |
| F-14 | **Corrupt Version (at rest)** | Immutable + (future) integrity-verified; a detected corruption marks the Version **unavailable for new selection**; matches that pinned it rely on **board state** (separate), so play/replay is unaffected. | ADR-008 §16 (integrity, future control) |
| F-15 | **Moderate the only published version** | Pointer recomputes; dictionary becomes **non-selectable** (no current version) yet **reproducible** for pinned matches. | REC-3, I-3 |
| F-16 | **Replay attack** (resubmit captured publish/share/clone command) | Rejected by auth + command idempotency + ADR-004 versioning; no duplicate state effect. | ADR-004/010, SEC-4 |
| F-17 | **Recovery** (node/room failure mid-match) | Recovered aggregate carries pinned identity + immutable board; identical words reproduce; no content reload needed. | ADR-005, ADR-008 §13 |

**New architectural directive surfaced:** **publish (and share/clone/revoke) commands must be idempotent** to
make F-13/F-16 deterministic — a design constraint for Tech Design, consistent with ADR-010, not a behavior
change. ([§12 REC-8](#12-missing-business-rules-formalizations).)

---

## 10. Future Extensibility Review

Verified that each future capability is **additive** over the one Dictionary aggregate (ownership/visibility
metadata + lifecycle reuse) with **no redesign**:

| Future feature | Added as | Redesign? |
|----------------|----------|:---------:|
| Premium content | Entitlement/visibility metadata (mechanics = own ADR) | No |
| Marketplace | Separate context consuming published Versions; conforms to seam | No |
| AI dictionaries | System-authored, same validation+review+immutability gate | No |
| Ratings / Reviews | Observational metadata (COL-3/4), never affects fairness | No |
| Editors | Reserved role in §6 ownership model | No |
| Organizations | Owner = org identity; Organization visibility (reserved) | No |
| **Collections / Folders** | **New grouping metadata over Dictionaries** (a Dictionary may belong to owner collections) | No — additive metadata |
| Tags | Already present (MD-6) | No |
| Analytics | Read-only projections (§21 statistics, ADR-010 queries) | No |
| Search engine | Realizes §20 search fields (Tech Design) | No |
| Recommendations | Read-only projection over public catalog + analytics | No |

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| E-1 | **Collections/Folders** are the only listed item without an explicit anchor in the frozen docs. They are trivially additive (grouping metadata) but unmentioned. | Nice-to-Have | Note in ADR-011 §10 / a future-extensibility appendix that **grouping (collections/folders) is additive metadata**, not an aggregate change. Do not build now. |

**Verdict:** extensibility goal met; no redesign required for any named future capability.

---

## 11. Consistency Review

Cross-checked the three documents for contradictions:

| Axis | Vision (01) | Feature Spec (02) | ADR-011 | Consistent? |
|------|-------------|-------------------|---------|:-----------:|
| Auth seam (author vs consume) | SEAM-1/2, PP-9 | FR-074, CON-10, NG-CONTENT-10 | AI-CP-2, §9 | ✔ |
| Ownership cardinality | PP-5 | BR-CONTENT-001 | AI-CP-1 | ✔ |
| Immutability / new-version | PP-1/11 | BR-CONTENT-010/011 | AI-CP-4 | ✔ |
| Typed ownership; DM-C1 official-only | §8 | BR-CONTENT-040/041 | §10, §15 (ADR-008 row) | ✔ |
| Visibility levels | §8 | §7 | §6.5 | ✔ |
| Public review gate | (implied) | §18 | §6.5, §14.2 | ✔ |
| Clone independence + provenance | CG-2 | BR-CONTENT-034, VM-7 | AI-CP-8 | ✔ |
| Gameplay pinning unchanged | SEAM-2 | §13 | §7, §15 (ADR-008) | ✔ |
| No gameplay change | §6.3 red lines | NG-CONTENT-1..6 | §16, §22 | ✔ |

| # | Finding | Class | Recommendation |
|---|---------|-------|----------------|
| C-1 | Only the **"Active" vs "current published version"** wording ([V-1](#1-vocabulary-review)) is a naming inconsistency, not a semantic one. | Important | Reconcile in ADR-000 ([V-1](#1-vocabulary-review)). |

**No semantic contradictions found** across the three documents.

---

## 12. Missing Business Rules (Formalizations)

Each is a **formalization of behavior already specified or entailed** — *no new behavior*. Proposed with the
next free IDs **for adoption during ADR-000/spec maintenance**; not applied here.

| REC | Formalizes | Proposed home | Class |
|-----|-----------|---------------|-------|
| **REC-1** | Moderator is a **restricted, lifecycle-only** second command source on the Dictionary aggregate, serialized with owner commands. | `BR-CONTENT-054` | Important |
| **REC-2** | Clone **provenance is informational and non-retaining**; may dangle if source is deleted/retired. | `BR-CONTENT-055` | Important |
| **REC-3** | The **current-published-version pointer** is aggregate-maintained derived state, recomputed on publish/retire/block; may become null ⇒ not selectable. | `BR-CONTENT-056` | Important |
| **REC-4** | A **delete request is cancellable during Retention** (`PendingDeletion → Active/Archived`); only unreferenced physical removal is irreversible. | `BR-CONTENT-057` | Important |
| **REC-5** | **Block is reversible** (`Blocked → Pending Review → Discoverable`); **Retire is terminal**. | `BR-CONTENT-058` | Important |
| **REC-6** | The **version label advances only on successful publish**. | `BR-CONTENT-059` | Nice-to-Have |
| **REC-7** | **Anonymous host** may select only **Official + Public**; authenticated host additionally **Mine + Shared**. | `BR-CONTENT-060` | Important |
| **REC-8** | **Publish/share/clone/revoke commands are idempotent** (retry/duplicate produces no second effect). | `BR-CONTENT-061` | Important |
| **REC-9** | A **Version belongs to exactly one Dictionary** (composition). | `AI-CP-16` | Important |
| **REC-10** | The **current pointer references a published, non-retired Version or is null**. | `AI-CP-17` | Important |

---

## 13. Missing Acceptance Criteria

Additive to [Feature Spec §15](02-feature-specification.md#15-acceptance-criteria); each maps to existing FRs/RECs:

| # | Capability | Proposed acceptance criterion |
|---|-----------|-------------------------------|
| AC-1 | **Metadata** | Editing a Dictionary's title/description/tags/visibility updates container metadata; a published Version's metadata (label/identity/publish date/word count/provenance) cannot be changed. |
| AC-2 | **Review** | A Public dictionary appears in the Public catalog only after review approval; a **blocked** version can be re-reviewed and restored; a **retired** version cannot. |
| AC-3 | **Anonymous host scope** | An anonymous host is offered only Official + Public content; no Private/Shared content is ever visible or selectable to an anonymous actor. |
| AC-4 | **Current version recomputation** | Retiring/blocking the current version advances the pointer to the newest remaining non-retired published version, or makes the dictionary non-selectable if none remain — with pinned matches unaffected. |
| AC-5 | **Soft delete** | A delete request can be cancelled during Retention; permanent deletion occurs only when no published Version is referenced by any live/reproducible match. |
| AC-6 | **Idempotent publish** | Submitting the same publish request twice yields exactly one Version. |
| AC-7 | **Provenance** | Deleting a source dictionary leaves clones fully functional; provenance becomes a dangling (informational) reference. |

---

## 14. Architecture Test Opportunities

Beyond [FF-CP-001..014](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md#13-architecture-fitness-functions-ff-cp-):

| AT | Rule to assert |
|----|----------------|
| AT-1 | **No Content Platform type references a Gameplay type** (dependency-direction test: Content ⟂ Gameplay; the seam is one-way, Gameplay reads a Version identity). |
| AT-2 | **No Gameplay type may mutate a Dictionary/Version** (no write path; == FF-CP-001 as a compile/dependency test). |
| AT-3 | **Published Version type exposes no mutating operation** (immutability by type shape). |
| AT-4 | **A Dictionary exposes at most one Draft** (cardinality; == FF-CP-004). |
| AT-5 | **A Dictionary has exactly one owner reference, never null** (== FF-CP-003). |
| AT-6 | **A room's match state holds both a pinned Version identity and drawn board words** (Alt E refinement; [G-1](#8-gameplay-boundary-review)). |
| AT-7 | **No query surface returns a Draft or non-current Version to a non-owner** (== FF-CP-002/007). |
| AT-8 | **Visibility is checked server-side on every content read** ([SEC-2](#17-security-review)). |
| AT-9 | **Public discovery entries all have an approved Version** (== FF-CP-009). |
| AT-10 | **Moderator commands cannot reach word content** (restricted command surface; REC-1, == FF-CP-010). |
| AT-11 | **Official and user Dictionaries traverse one lifecycle state machine** (== FF-CP-014). |
| AT-12 | **Clone shares no reference-identity of mutable state with its source** (== FF-CP-008). |

---

## 15. Domain (Unit) Test Opportunities

Edge cases for the Dictionary aggregate / value objects:

- Publish with exactly 24 vs 25 distinct words (boundary of `DICTIONARY_MIN_WORDS`).
- Duplicate words differing only by case/whitespace collapse to one (V-CONTENT-2).
- Blank/whitespace-only word rejected (V-CONTENT-1).
- Word at/over `DICTIONARY_MAX_WORDS` and per-word length bounds (V-CONTENT-4/5).
- Publish snapshots the Draft; subsequent Draft edits do **not** alter the published Version.
- Discard reverts Draft to the last published Version (or empty when none).
- Retire current version ⇒ pointer recomputes; only-version retire ⇒ pointer null (REC-3, F-15).
- Clone seeds words + provenance; editing clone leaves source untouched (AI-CP-8).
- Block then re-review restores discoverability; retire cannot be undone (REC-5).
- Cancel delete during Retention restores the dictionary (REC-4).
- Visibility transition Private→Public requires ≥1 published Version + review (BR-CONTENT-032).
- Idempotent publish: identical request twice ⇒ one Version (REC-8).
- Version label advances only on successful publish (REC-6).

---

## 16. Integration Test Opportunities

*(Concerns named at the architecture level; transports are Tech-Design detail — no API/framework specified here.)*

| Area | Test intent |
|------|-------------|
| **Authoring command path** | Create→edit→validate→publish produces one immutable Version; unauthorized actor rejected. |
| **Authentication seam** | Authoring requires an authenticated identity; **in-room consumption of a published Version succeeds with no account** (SEAM-1). |
| **Gameplay integration** | Selecting a dictionary pins its current published Version; a subsequent publish/retire/visibility change does **not** alter the running match (AI-CP-7/12). |
| **Persistence** | Published Versions persist immutably; referenced Versions are retained through a delete request (AI-CP-15). |
| **Recovery** | A room recovered mid-match reproduces identical words from pinned identity + board state (ADR-005/008 §13). |
| **Real-time delivery** | Content changes never emit into a running match's event stream (no Content→Room path; ADR-004 unaffected). |
| **Discovery/query** | Mine/Official/Shared/Public return only permitted content; anonymous host sees only Official+Public (REC-7). |
| **Moderation** | Report→block→retire removes content from new selection without editing words or touching a pinned match. |

---

## 17. Security Review

| # | Threat | Class | Architectural response |
|---|--------|-------|------------------------|
| SEC-1 | **Enumeration of private/shared content** via guessable identifiers | Important | Version/Dictionary identities must be **non-sequential and unguessable** (mirrors room-code rule [R-3](../01-product-discovery/01-business-requirements.md#19-risks)); AND every read is access-checked server-side (SEC-2) — never rely on obscurity alone. Directive to Tech Design (VR-3). |
| SEC-2 | **Visibility bypass** (reading a dictionary you shouldn't) | **Important** | **Server-side visibility enforcement on every content read** — an explicit rule (candidate `AI-CP-18` / architecture test AT-8). Client never adjudicates visibility. |
| SEC-3 | **Ownership escalation / unauthorized publish/clone/edit** | Important | Owner-only commands (BR-CONTENT-002) enforced server-side against the authenticated identity; clone requires auth+visibility; moderator restricted to lifecycle (REC-1). |
| SEC-4 | **Replay / duplicate-command** (publish/share) | Important | Idempotent commands (REC-8) + auth + ADR-004/010 command versioning (F-13/F-16). |
| SEC-5 | **Private dictionary leakage via clone/share/provenance** | Important | Clone from a permitted Version only; provenance is an **identity reference, not content** (does not expose source words to non-viewers); revoke removes future access (AI-CP-6/7). |
| SEC-6 | **Mass import abuse** (giant/looping imports) | Nice-to-Have (operational) | Bounded by `DICTIONARY_MAX_WORDS` + per-word bounds; rate limiting is an operational control (Non-Goal here). |
| SEC-7 | **Search abuse** (scraping the catalog) | Nice-to-Have (operational) | Pagination + rate limiting (operational); search returns only permitted content (SEC-2). |
| SEC-8 | **Future moderation abuse** (false reports, moderator overreach) | Nice-to-Have (future) | Moderator restricted to lifecycle (REC-1); report accountability (Z-3); tooling deferred. |

**Key security directive:** **SEC-2 (server-side visibility enforcement) + SEC-1 (unguessable identities)**
together close the private-leak surface — both belong in Tech Design and as architecture tests.

---

## 18. Performance Review

Architectural risks only (no premature optimization):

| # | Concern | Class | Note |
|---|---------|-------|------|
| P-1 | **Large dictionaries** | Nice-to-Have | Bounded by `DICTIONARY_MAX_WORDS`; publish validation is O(words) — acceptable. |
| P-2 | **Many versions per dictionary** | Important | Version history must be **paginated**, and old Versions **archivable**; retention keeps only *referenced* Versions hot (AI-CP-15). |
| P-3 | **Many clones** | Nice-to-Have | Clones are independent aggregates; no fan-out cost on the source (provenance is a back-reference only). |
| P-4 | **Discovery/search scale** | Important | Discovery and version history **must be paginated** (consistent with the MVP's existing read-model pagination); search realized over §20 fields (Tech Design). |
| P-5 | **Publication cost** | Nice-to-Have | Validate+snapshot is per-dictionary, low-rate; not on the gameplay hot path. |
| P-6 | **Import cost** | Nice-to-Have | Bounded by max-words; batch with reject reporting. |
| P-7 | **Statistics (future)** | Nice-to-Have | Read-only projections computed async; never on the authoring/gameplay hot path. |
| P-8 | **Retention growth** | Important | Referenced-Version retention can accumulate; needs an archival/cold-storage strategy (Tech Design / Data Lifecycle). |

**Directive:** **pagination is mandatory** for discovery, catalogs, and version history (P-2/P-4) — a design
constraint, consistent with existing system conventions.

---

## 19. Documentation Review

| # | Item | Class | Action |
|---|------|-------|--------|
| D-1 | **ADR-000 not yet updated** with Content-Platform vocabulary (ADR-011 §21 mandate; V-1..V-5). | Important | Update ADR-000 as the first implementation-phase doc task (folds "current published version", withdrawal-verb table, label vs identity). |
| D-2 | **Business Glossary** ([03-business-governance/01](../03-business-governance/01-business-glossary.md)) lacks Content-Platform terms. | Important | Add Dictionary/Draft/Version/Owner/Visibility/Share/Clone/Publish/Review/Moderate entries. |
| D-3 | **Main docs index** ([docs/README.md](../README.md)) does not list `15-content-platform`. | Nice-to-Have | Add an index entry (the folder README/section set). |
| D-4 | **ADR index** ([12-decisions/README.md](../07-software-architecture/12-decisions/README.md)) — **already updated** with ADR-011. | — (done) | Confirmed. |
| D-5 | **Constants Catalog** must ratify `DICTIONARY_MAX_WORDS` and per-word bounds (flagged in Feature §9). | Important | Ratify during Tech Design. |
| D-6 | **Diagrams** — the context/aggregate/lifecycle are described in tables/ASCII; a C4-style container view and a state diagram would aid implementers. | Nice-to-Have | Add to Software Design (06) if produced. |
| D-7 | **Cross-references & traceability** across 01/02/ADR-011 verified intact; the `15-content-platform` folder would benefit from a short **README** tying the three documents together. | Nice-to-Have | Add folder README. |

---

## 20. Final Readiness Report

### Strengths
- **Airtight gameplay boundary** — the authored/consumed seam, pinning, and immutability make it *structurally
  impossible* for content to affect a running/completed match or to require auth to play ([§8](#8-gameplay-boundary-review)).
- **One aggregate, one owner, one Draft** — a clean single-writer consistency boundary that mirrors the room
  Authority and keeps concurrency simple.
- **Immutability + versioning + provenance** deliver reproducibility, safe evolution, non-destructive
  moderation, and independent clones from one model.
- **Extensibility proven** — every named future capability is additive over the same aggregate ([§10](#10-future-extensibility-review)).
- **Zero contradictions** across Vision, Spec, and ADR-011 ([§11](#11-consistency-review)); full ADR-000..010
  compliance.

### Weaknesses (all addressable additively — none redesign)
- A handful of **implicit rules** were unstated: moderator-as-second-writer, provenance non-retention,
  current-version recomputation, block-reversibility, delete cancelation, anonymous-host scope, idempotent
  publish (§12 REC-1..10).
- **Server-side visibility enforcement** and **unguessable identities** were implied, not stated as security
  rules (§17 SEC-1/2).
- **Pagination** for discovery/history was assumed, not required (§18 P-2/P-4).

### Risks
- **Forward (accounts) risk O-4** — owner/organization deletion vs referenced-Version retention must be resolved
  before account deletion ships (not blocking this capability; no accounts yet).
- **Moderation operational load** for public content (tooling deferred by design).
- **Retention growth** needs an archival strategy (P-8).

### Technical Debt (accepted, tracked)
- ADR-000 / Business Glossary vocabulary fold-in (D-1/D-2).
- `DICTIONARY_MAX_WORDS` + per-word bounds ratification (D-5).
- Two future-role models (Editor, ownership transfer) intentionally deferred.

### Recommended Improvements — classified
**Critical:** *none.*

**Important** (do before/at implementation start; all additive):
REC-1..5, REC-7..10 formalizations (§12); SEC-1/2 security rules and AT-8; block-reversibility & delete-cancel
lifecycle transitions (L-2/L-4); anonymous-host selection scope (Z-1); pagination mandate (P-2/P-4); ADR-000 &
Glossary updates (D-1/D-2); `DICTIONARY_MAX_WORDS` ratification (D-5); forward policy note for owner deletion
(O-4); architecture tests AT-1..12.

**Nice-to-Have:** V-2/V-4, L-1/L-3/L-5/L-6, REC-6, E-1 (collections note), D-3/D-6/D-7, additional domain tests
(§15).

**Rejected (would change behavior — out of mandate):** none proposed; every recommendation is a formalization,
test, security-hardening, or documentation action that leaves all frozen behavior and IDs unchanged.

### Verdict
No **Critical** issues exist. Every finding is an additive formalization, test, or documentation action that
strengthens — and never changes — the frozen design. The Important items are best executed as the **opening
tasks of the implementation phase** (ADR-000/glossary update, the REC-* formalizations, security rules,
pagination, and the architecture-test suite), none of which require a new architecture document.

> ## Architecture Approved for Implementation.

---

## 21. Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-11 | Pre-implementation architecture review of the Content Platform (Vision + Feature Spec v1.1 + ADR-011) across 20 categories: vocabulary, aggregate, invariant, lifecycle, authorization, ownership, versioning, gameplay boundary, failure scenarios (17), extensibility, consistency, formalized rules (REC-1..10), acceptance criteria, architecture/domain/integration tests, security, performance, documentation, and a final readiness report. No Critical issues; all recommendations additive. **Verdict: Architecture Approved for Implementation.** Changes no frozen decision, ID, or behavior. |
