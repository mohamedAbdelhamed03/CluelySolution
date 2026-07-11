# ADR-008 — Dictionary & Content Architecture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How does Cluely model, version, distribute, evolve, and consume dictionaries while preserving fairness, determinism, localization, room isolation, and future extensibility?* |
| **Complies with** | [ADR-000](ADR-000-architecture-vocabulary.md), [ADR-001](ADR-001-overall-architecture-style.md), [ADR-002](ADR-002-authoritative-game-state.md), [ADR-003](ADR-003-per-room-coordination-model.md), [ADR-004](ADR-004-real-time-communication-delivery.md), [ADR-005](ADR-005-state-recovery-resilience.md), [ADR-006](ADR-006-role-based-information-visibility.md), [ADR-007](ADR-007-room-isolation-distribution.md), [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md). Extends; redefines none. Consistent with the business [Dictionary Management](../../02-business-analysis/13-dictionary-management.md) spec. |
| **Scope note** | Defines the **content architecture** only. It chooses **no** database/SQL/PostgreSQL/Redis/blob/CDN/CMS/admin-UI/search/AI-moderation/storage/framework ([§20 Non-Goals](#20-non-goals)). |

---

## 1. Executive Summary

A **Dictionary** in Cluely is **versioned, immutable, read-only, region-scoped content** — a curated set
of words for a country/culture — that the game **consumes but never owns**. It is a **first-class
architectural component** (not incidental application data) because gameplay depends entirely on
culturally appropriate words, yet it must **never** influence the rules, the outcome, ownership, or
isolation of a room.

The chosen model is a **Versioned, Immutable, Country-Scoped Content Library, pinned per match**: content
is organized into **Country Dictionaries**, each publishing **immutable Dictionary Versions**; when a
match starts, the room **pins exactly one Dictionary Version** into its authoritative aggregate
([ADR-002](ADR-002-authoritative-game-state.md)) as a **reference (identity), not the words themselves**;
the board's 25 words are drawn **once** at generation and become part of the immutable board state.
Thereafter, **new content never touches the running match** — publishing, deprecating, or even retiring a
version **cannot change a board in play**, because the match holds a **pinned, immutable version
reference** and a **fixed board**.

**Why dictionaries are architectural:** localization is the *only* localized component of Cluely, and it
sits on the fault line between "must feel culturally natural" and "must never change fairness/determinism".
Getting the boundary wrong (mutable content, unpinned versions, content that leaks into rules) would break
the game's two core promises — **one gameplay worldwide** and **fair, reproducible matches**
([INV-D1/D3](../../02-business-analysis/10-business-invariants.md)). **Why localization affects gameplay:**
the *words* determine whether players can reason about clues at all; a word set that is foreign, ambiguous,
or offensive to a region ruins the experience even though the *rules* are identical. **Why dictionary
correctness affects fairness:** duplicate, offensive, or ambiguous words distort play; **why evolution must
never invalidate active games:** a board mid-play must remain exactly as generated — content freshness is
**subordinate to fairness**.

Content is **read-only and shared** ([ADR-007](ADR-007-room-isolation-distribution.md)): it may be freely
**replicated/cached**, it **never** couples rooms, **never** affects ownership, and **never** mutates any
room. Recovery ([ADR-005](ADR-005-state-recovery-resilience.md)) and migration
([ADR-007](ADR-007-room-isolation-distribution.md)) preserve the **pinned version identity** with the
aggregate, so a recovered/migrated match reproduces the **same** board.

> One-line statement: **country-scoped, versioned, immutable, read-only content; pinned by version
> reference at match start; drawn once into an immutable board; new content never touches a running match;
> shared/replicable but never room-coupling and never rule-changing.**

---

## 2. Problem Statement

**Why Cluely cannot rely on a single global dictionary.** A globally-uniform word list feels foreign in
most regions and undermines play. Cluely is global **with one gameplay**, localized **only** by the word
source ([BRD Localization](../../01-product-discovery/01-business-requirements.md), [INV-D1](../../02-business-analysis/10-business-invariants.md)).
The architectural concerns:

- **Cultural vocabulary:** words familiar in one culture are unknown in another (foods, figures, places).
- **Ambiguity / slang:** a word's associations differ by culture; ambiguous or slang terms break clue-giving.
- **Offensive words:** what is acceptable varies by region; offensive content is a reputational/ethical hazard.
- **Regional knowledge:** clues rely on shared cultural knowledge, which is regional, not merely linguistic.
- **Language evolution:** vocabulary drifts; content must evolve **without** rewriting history or breaking active matches.
- **Fairness:** duplicate/ambiguous/offensive words distort play; content quality is a fairness concern.
- **Future community/AI content:** user- or AI-generated dictionaries raise moderation, determinism, and integrity concerns that must be contained by the architecture, not bolted on.

These are **architectural** because they determine ownership (who may change content), immutability
(can a running match change?), isolation (can content couple rooms?), reproducibility (can a past match be
reproduced?), and evolvability (can new content ship safely?) — properties that must be guaranteed by the
model, not by an admin tool.

---

## 3. Dictionary Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Content is Read-Only** | Content is never mutated in place; corrections publish new versions. | Reproducibility; safe evolution ([INV-D1](../../02-business-analysis/10-business-invariants.md), DM-V1). |
| **Gameplay Owns Nothing (of content)** | The rules core/aggregate own the *board*, but **not** the dictionary; they hold only a *reference*. | Separation of content from truth. |
| **Rooms Reference Content** | A room pins a **version reference**, not editable content. | Immutability during play. |
| **Content Never Owns Rooms** | Content cannot influence a room's ownership, lifecycle, or coordination. | Isolation ([ADR-007](ADR-007-room-isolation-distribution.md)). |
| **Dictionary Before Match** | A valid dictionary version must exist and be selected before a match starts. | [BR-GS-3](../../02-business-analysis/02-business-rules.md). |
| **Version Before Gameplay** | A specific version is resolved/pinned before any board is generated. | Determinism/reproducibility. |
| **Content Is Immutable During Play** | Once a match starts, its words never change. | [INV-D3](../../02-business-analysis/10-business-invariants.md). |
| **Localization Is Cultural** | Localization is by **culture/country**, not merely language. | §5. |
| **Content Is Replaceable** | A region's active version can be superseded for **new** matches. | Evolution. |
| **Content Is Independently Evolvable** | Regions evolve independently; no cross-region coupling. | Maintainability. |
| **Content Never Changes Truth** | Content selects *words*; it never affects rules, counts, turn flow, or outcomes. | [INV-D1](../../02-business-analysis/10-business-invariants.md). |
| **Content Never Breaks Running Matches** | Publish/deprecate/retire cannot alter a board in play. | Fairness over freshness. |

---

## 4. Candidate Content Models

Evaluated for localized, fair, reproducible, isolated content. None dismissed without reasoning.

### CM1 — Single Global Dictionary
- **Overview:** One universal word list for all.
- **Localization/Fairness:** **Fails** — culturally foreign; ambiguous across regions.
- **Correctness/Reproducibility:** Fine, but unplayable-feeling globally.
- **Verdict:** **Rejected** — violates the localization requirement ([BRD](../../01-product-discovery/01-business-requirements.md)).

### CM2 — Language-Based Dictionary (by language: Arabic, English, Spanish…)
- **Overview:** One dictionary per language.
- **Advantages:** Fewer sets than per-country.
- **Disadvantages:** **Language ≠ culture** — "Spanish" spans Mexico/Spain/Argentina with very different vocabulary, slang, and offense norms; "Arabic" spans Egypt/Saudi/Gulf; "English" spans US/UK/India. A language set is **too coarse** to be culturally appropriate → ambiguity and offense (§5).
- **Verdict:** **Rejected** in favor of country scope (see §5).

### CM3 — Country-Based Dictionary (RECOMMENDED base)
- **Overview:** One Country Dictionary per country/culture (Egypt, Saudi Arabia, United States, Mexico, Brazil, Japan, France…).
- **Advantages:** Matches **cultural context** (shared knowledge, slang, offense norms) that clue-giving depends on; aligns with the business spec ([Dictionary Management](../../02-business-analysis/13-dictionary-management.md), DM-C1).
- **Disadvantages:** More sets to curate (offset by shared tooling); intra-country variants may still exist (→ regional variants, §10).
- **Verdict:** **Selected base.**

### CM4 — Region Packs / CM5 Theme Packs / CM6 Community Dictionaries
- **Overview:** Regional variants (dialect/region within a country), thematic packs (seasonal, educational, corporate, tournament), and community-contributed sets.
- **Advantages:** Rich localization/extensibility; future growth/monetization.
- **Disadvantages:** Community/AI content needs moderation and determinism guarantees.
- **Verdict:** **Adopted as composable extensions** of CM3 (§10) — each is *content* under the same versioned/immutable/pinned model; **none** changes rules.

### CM7 — Versioned Content Library (RECOMMENDED structure)
- **Overview:** All content lives in a **library** of **versioned** entries.
- **Advantages:** Reproducibility, safe evolution, historical fidelity, auditability.
- **Verdict:** **Selected structure.**

### CM8 — Immutable Dictionaries (RECOMMENDED property)
- **Overview:** Published versions never change; corrections = new versions.
- **Advantages:** Running matches/history safe; deterministic; reproducible.
- **Verdict:** **Selected property.**

### CM9 — Mutable Dictionaries
- **Overview:** Edit content in place.
- **Disadvantages:** **Fails** — an in-place edit could change a running match's word pool or break reproducibility ([INV-D3](../../02-business-analysis/10-business-invariants.md)).
- **Verdict:** **Disqualified.**

### CM10 — Hybrid (FINAL): Versioned, Immutable, Country-Scoped Content Library, pinned per match
- **Overview:** **CM3 (country scope)** + **CM7 (versioned library)** + **CM8 (immutable)** + **match pinning** + composable **CM4/5/6** extensions.
- **Verdict:** **This is the decision.**

### Evaluation summary

| Criterion | CM1 Global | CM2 Language | **CM3+CM7+CM8 (chosen)** | CM9 Mutable |
|-----------|:----------:|:------------:|:------------------------:|:-----------:|
| Correctness | 4 | 4 | **5** | 2 |
| Fairness | 2 | 3 | **5** | 2 |
| Localization | 1 | 3 | **5** | 3 |
| Maintainability | 5 | 4 | **4** | 2 |
| Moderation | 4 | 4 | **5** | 2 |
| Future growth | 2 | 3 | **5** | 3 |
| Testing | 5 | 4 | **5** | 2 |
| Scalability | 5 | 4 | **5** | 3 |
| Business flexibility | 2 | 3 | **5** | 3 |
| Reproducibility | 5 | 5 | **5** | 1 |

---

## 5. Final Dictionary Model

**Adopt CM10 — a Versioned, Immutable, Country-Scoped Content Library, pinned per match**, with composable
regional/theme/community/AI packs as content under the same model.

**Why country-based beats language-based:** clue-giving relies on **shared cultural knowledge**, not merely
a shared language. A **language** spans many cultures with divergent vocabulary, slang, taboos, and
references:
- **Spanish** → Mexico vs Spain vs Argentina vs Colombia differ sharply (foods, idioms, offense norms).
- **Arabic** → Egypt vs Saudi Arabia vs the Gulf differ in dialect and cultural references.
- **English** → United States vs United Kingdom vs India differ in vocabulary and connotation.
- **Portuguese** → Brazil vs Portugal; **French** → France vs Québec vs West Africa.

Country examples the model targets: **Egypt, Saudi Arabia, United States, Mexico, Brazil, Japan, France**
— *not* Arabic/English/Spanish. **Cultural context matters more than language** because a fair clue depends
on the *audience sharing the association*, which is cultural. Country scope is the practical unit of shared
culture; finer distinctions (dialects/regions) compose as **regional variants** (§10) without changing the
model.

- **Why it fits Cluely:** it delivers culturally natural words (fair, playable) while keeping content
  **immutable, versioned, pinned, read-only, and rule-neutral** — satisfying both product promises.
- **Aligns with prior ADRs:** the pinned **version reference** is authoritative aggregate state (ADR-002),
  drawn once into the immutable board; content is **shared read-only** and non-coupling (ADR-007);
  recovery/migration preserve the pin (ADR-005/007); content never appears in a way that leaks the key
  (ADR-006); it never influences coordination (ADR-003) or delivery (ADR-004).

---

## 6. Dictionary Identity

| Concept | Definition |
|---------|-----------|
| **Dictionary (Country Dictionary)** | A curated word source for a **country/culture** (e.g., Egypt). It is a logical container of versions; matches never consume it directly. |
| **Dictionary Version** | An **immutable, published snapshot** of a Country Dictionary's word list — the unit a match pins and reproduces from. |
| **Content Pack** | A packaged unit of content — a Country Dictionary version, a regional variant, or a theme/seasonal/educational/corporate/tournament pack — all governed by the same versioned/immutable model. |
| **Regional Dictionary / Variant** | A country dictionary specialized for a region/dialect within a country (composes with the country base — §10). |
| **Theme** | A thematic overlay/pack (seasonal, educational, corporate, tournament) that is *content*, still versioned/immutable/pinned; it **never** changes rules. |
| **Match Dictionary** | The **specific pinned Dictionary Version** a running match references — the identity recorded in the room aggregate ([ADR-002](ADR-002-authoritative-game-state.md)). |
| **What uniquely identifies content** | A **globally-unique content identity** = (region/pack identity + version) — an immutable, unique **Dictionary Version identity** ([AI-CONTENT-9](#14-architectural-invariants-ai-content-)). |

---

## 7. Dictionary Lifecycle

States (extending the business [Dictionary Lifecycle §6](../../02-business-analysis/13-dictionary-management.md#6-dictionary-lifecycle)):

```
Draft → Review → Approved → Published → Active → Deprecated → Archived → Retired
```

| State | Purpose | Allowed transitions | Forbidden transitions | Compatibility / Impact |
|-------|---------|---------------------|-----------------------|------------------------|
| **Draft** | Under curation. | → Review | → Active (skipping review) | Not selectable; no matches. |
| **Review** | Under moderation/quality review. | → Approved; → Draft (reject) | → Published (bypassing approval) | Not selectable. |
| **Approved** | Passed review; not yet published. | → Published | → mutate content (immutability) | Not yet selectable. |
| **Published** | **Immutable**, available. | → Active | any content edit | Selectable once Active; **never** editable. |
| **Active** | The region's current selectable version. | → Deprecated | editing content | New matches pin this; running matches unaffected. |
| **Deprecated** | Superseded by a newer Active version. | → Archived / Retired | editing content | **Still used by matches that pinned it** (INV-D3). |
| **Archived** | Retained for reproducibility/audit; not selectable. | → Retired | editing content | History reproducible; no new selection. |
| **Retired** | Withdrawn from all **new** use. | (terminal) | editing; reselection | **Must not** be removed while any live match pins it (§8). |

**Invariant across states:** a **Published+** version is **immutable**; corrections are **new versions**;
transitions **never** alter a version's content and **never** affect a match that pinned it.

---

## 8. Match Pinning *(critical)*

- **When a room starts, which dictionary is used?** The room's selected **region** resolves to that
  region's **Active** Dictionary Version; the match **pins that specific version's identity** into its
  authoritative aggregate ([ADR-002 S-Dictionary](ADR-002-authoritative-game-state.md#6-state-inventory), [BR-GS-3](../../02-business-analysis/02-business-rules.md)).
- **Can it change?** **No.** Once pinned at start, the match's dictionary version is **immutable for the
  match's life** ([INV-D3](../../02-business-analysis/10-business-invariants.md)).
- **What happens if a new version is published?** **Nothing to the running match.** Publishing/activating a
  newer version affects **only future matches**; the running match keeps its pinned version and its already-
  generated board.
- **Can active rooms migrate (to a new version)?** **No.** Active matches never migrate content versions;
  they finish on their pinned version. (Team/role changes between matches may pick a new region → a new
  match pins the then-Active version.)
- **How are completed matches affected?** **Not at all.** A completed match's result and board are immutable
  ([INV-O4](../../02-business-analysis/10-business-invariants.md)); its pinned version is recorded for
  reproducibility.
- **How does recovery preserve the dictionary?** The **pinned version identity** is part of the recovered
  aggregate ([ADR-005](ADR-005-state-recovery-resilience.md)); the **board words** are recovered as
  immutable board state — so a recovered match reproduces the **same** board (§13).
- **How does migration preserve the dictionary?** [ADR-007](ADR-007-room-isolation-distribution.md) moves
  **ownership**, preserving the aggregate — including the pinned version identity and the board; a migrated
  match is byte-for-byte the same content.
- **How does distribution preserve the dictionary?** Content is **shared, read-only, replicable** (§12); any
  node owning the room resolves the **same immutable version** by identity — determinism is location-independent.

**Pinning is the linchpin:** it converts mutable-over-time content into a **fixed input** for a match,
which is what makes fairness, determinism, reproducibility, recovery, and migration all hold for content.

---

## 9. Versioning Model

- **Version identity:** each Dictionary Version has a **globally-unique, immutable identity** (region/pack +
  version); a match records this identity.
- **Semantic layers (guidance):**
  - **Major** — a substantial content change (large word-set revision) → a new Active version for new matches.
  - **Minor** — additions/small curation changes → a new version; running matches unaffected.
  - **Patch** — corrections (e.g., removing an offensive/duplicate word) → a **new** version (never an in-place edit).
- **Compatibility:**
  - **Backward compatibility:** older versions remain **fully usable/reproducible** (immutability) — a match
    on an old version always reproduces the same board.
  - **Forward compatibility:** new versions are used **only** by new matches; they never retro-apply.
- **Content evolution:** happens by **publishing new versions**; the region's Active pointer advances; older
  versions become Deprecated/Archived but **remain reproducible** while referenced.
- **Historical reproducibility:** because versions are immutable and matches pin a version identity, **any
  past match's word source is exactly reproducible** — a first-class guarantee (FF-6/7).
- **Architectural guarantee:** versioning **never** changes rules, counts, or outcomes — only *which words*
  a **future** match may draw (INV-D1).

---

## 10. Regional Architecture

Composition (all are **content** under the same versioned/immutable/pinned model; **none** changes rules):

| Layer | Definition | Composition |
|-------|-----------|-------------|
| **Country dictionaries** | The base cultural unit (Egypt, USA, Japan…). | The primary selectable content. |
| **Regional variants / future dialects** | Specializations within a country (region/dialect). | Compose **on top of** the country base as their own versioned content; a match pins one resolved variant version. |
| **Theme packs** | Thematic word sets (e.g., a topic). | A pack the room may select; pinned like any version. |
| **Seasonal packs** | Time-limited themes. | Selectable while active; pinned per match; do not expire a running match. |
| **Educational packs** | Curated for learning contexts. | Same model; region/audience scoped. |
| **Corporate packs** | Private/org-specific content. | Same model; access-scoped (future). |
| **Tournament packs** | Curated for events. | Same model; fixed version for fairness across a tournament. |

**Composition rule:** any pack is resolved to **one immutable Dictionary Version** before a match; **exactly
one** version is pinned per match; packs never combine into mutable, cross-room state and never alter
gameplay rules (INV-D1). How packs are *authored/combined* is a content-team concern; the **runtime**
consumes a single resolved, pinned version.

---

## 11. Moderation Architecture

Architecture-only (tools/AI are Non-Goals):

| Function | Architectural definition | Ownership |
|----------|--------------------------|-----------|
| **Review** | The lifecycle gate Draft→Review→Approved (§7). | Content/moderation team. |
| **Approval** | Authorizing a version for publication. | Content owner/approver. |
| **Publishing** | Making an approved version immutable and available. | Content team. |
| **Deprecation** | Superseding an Active version for new matches. | Content team. |
| **Blocking** | Preventing a version from being **selected** for new matches (without editing it). | Content/admin. |
| **Emergency removal** | Retiring a version from **new** selection immediately (e.g., discovered offense) — **never** editing it and **never** removing it while a live match pins it. | Content/admin. |
| **Future AI moderation** | An *input* to review/approval; it **cannot** publish or edit content directly — it only advises the human gate. | Governed addition. |
| **Community moderation** | Reports/flags feed review; they **cannot** mutate published content. | Governed addition. |
| **Administrative ownership** | Content lifecycle is owned by the **Content/Localization team & Product Owner**; runtime (host/players) may only **select** a region — never create/edit/retire content ([DM-O1/O3](../../02-business-analysis/13-dictionary-management.md)). |

**Moderation invariant:** moderation acts on the **lifecycle** (review/approve/deprecate/block/retire) —
it **never edits published content** and **never changes history or a running match** (§14).

---

## 12. Distribution Model (using ADR-007)

| Question | Answer |
|----------|--------|
| **Who owns dictionaries?** | The **Content/Localization function** owns the content library (authoring/lifecycle). At runtime, content is a **shared read-only resource**, owned by **no room** ([ADR-007 §6](ADR-007-room-isolation-distribution.md#6-distribution-unit)). |
| **Can dictionaries move?** | They need not "move" like a room; being **read-only and identity-addressed**, any node resolves a version by identity. |
| **Can they be cached?** | **Yes** — freely, as immutable content keyed by version identity (cache-friendly). |
| **Can they be replicated?** | **Yes** — read-only replication anywhere; immutability makes replicas equivalent. |
| **Can rooms modify dictionaries?** | **No.** Rooms only **reference** a pinned version (AI-CONTENT-1). |
| **Can dictionaries affect ownership?** | **No.** Content never influences room ownership/placement/fencing ([ADR-007](ADR-007-room-isolation-distribution.md)). |
| **Can dictionaries couple rooms?** | **No.** Shared read-only content is **not** shared *mutable* state; two rooms reading the same version are **not** coupled (no cross-room dependency) — [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md) is about **mutable** sharing. |
| **How does distribution interact with content?** | A room's owner resolves the **pinned version by identity** from a replicated read-only source; determinism holds regardless of which node/replica serves it. |

---

## 13. Recovery Model (using ADR-005)

| Question | Answer |
|----------|--------|
| **What happens after recovery?** | The recovered aggregate contains the **pinned version identity** and the **immutable board**; the match reproduces the **same** words. |
| **How does a room recover its dictionary?** | It doesn't "reload words to decide the board" — the **board words are recovered as board state**; the **version identity** is recovered for reproducibility/audit; any need to reference the version resolves it by **immutable identity**. |
| **What if the dictionary changed (a new version was published)?** | **Irrelevant** to the recovered match — it keeps its **pinned** version and **already-generated** board (INV-D3). |
| **What survives?** | The **pinned version identity** and the **board (words, key, reveal flags)** — as part of the recovered aggregate. |
| **What is reconstructed?** | Nothing about content needs reconstruction beyond re-referencing the pinned identity; derived projections recompute (ADR-006). |
| **What is reloaded?** | If the board words are stored as board state (they are), **nothing** need be reloaded from the content library to continue; the library is only needed to *generate* a board (at start) or to *reproduce/audit* historically by identity. |

**Recovery guarantee:** recovery reproduces **identical word selection** because the board is immutable
state and the version is pinned by immutable identity (FF-3).

---

## 14. Architectural Invariants (AI-CONTENT-*)

- **AI-CONTENT-1:** **Rooms never modify dictionaries** — they only reference a pinned version.
- **AI-CONTENT-2:** **Dictionaries never modify rooms** — content cannot mutate room state/lifecycle/ownership.
- **AI-CONTENT-3:** **Content is immutable during gameplay** — a running match's words never change.
- **AI-CONTENT-4:** **The dictionary version is pinned** at match start and fixed for the match's life.
- **AI-CONTENT-5:** **Recovery preserves dictionary identity** (and the immutable board).
- **AI-CONTENT-6:** **Migration preserves the dictionary version** (moves ownership, not content).
- **AI-CONTENT-7:** **Dictionary updates never affect active games** — publish/deprecate/retire touch only future matches.
- **AI-CONTENT-8:** **Regional/theme content never changes gameplay rules** (counts, turn flow, outcomes) — content selects words only.
- **AI-CONTENT-9:** **Dictionary Version identity is globally unique and immutable** — no two versions share an identity; a version's content never changes.
- **AI-CONTENT-10:** **Moderation never changes history or a running match** — it acts on lifecycle, not published content.
- **AI-CONTENT-11:** **Content is read-only at runtime** — no runtime actor edits content.
- **AI-CONTENT-12:** **Content never couples rooms** — shared read-only access creates no cross-room dependency.
- **AI-CONTENT-13:** **A match pins exactly one Dictionary Version.**
- **AI-CONTENT-14:** **Published versions are immutable** — corrections are new versions.

---

## 15. Architecture Fitness Functions (FF-CONTENT-*)

- **FF-1:** **Every match references exactly one dictionary version** (recorded identity).
- **FF-2:** **The dictionary version never changes during gameplay** (pinned-identity check across the match).
- **FF-3:** **Recovery reproduces identical word selection** (recovered board == pre-failure board).
- **FF-4:** **Migration preserves dictionary identity** (post-migration pinned version == pre-migration).
- **FF-5:** **No room mutates content** (content is read-only; no write path from a room).
- **FF-6:** **Published dictionaries are immutable** (a Published+ version's content never changes).
- **FF-7:** **Historical matches remain reproducible** (given a recorded version identity, the word source is reproducible).
- **FF-8:** **Regional/theme packs remain isolated** (a pack never leaks into another region's content or into rules).
- **FF-9:** **Content is deterministic** (same version identity ⇒ same word set; board generation given the same inputs is reproducible).
- **FF-10:** **No two versions share an identity** (uniqueness check).
- **FF-11:** **Content never appears in a way that changes rules/outcomes** (rule-neutrality check; composes with the rules-core purity of [ADR-001/003](ADR-003-per-room-coordination-model.md)).

Map to [Success Metrics ASM-01/02](../../06-architecture-governance/04-architecture-success-metrics.md),
[QS-12/13](../09-quality-attribute-scenarios.md).

---

## 16. Security Analysis

Separating **architectural guarantees** from **future technical controls** (storage/AI/moderation-tools — Non-Goals).

| Threat | Architectural guarantee | Future technical control |
|--------|-------------------------|--------------------------|
| **Unauthorized content changes** | Content is read-only at runtime; only the content lifecycle (review→publish) can introduce versions; rooms/players cannot edit ([AI-CONTENT-1/11](#14-architectural-invariants-ai-content-)). | Access-controlled authoring/publishing (technical design). |
| **Malicious dictionaries** | Content passes the **review/approval** lifecycle gate before Published/Active; unreviewed content is never selectable (§7/§11). | Moderation tooling/AI advisory (future). |
| **Dictionary spoofing** | A match pins a **specific, globally-unique version identity**; resolution is by immutable identity, not by a mutable name (AI-CONTENT-9). | Integrity-verified content (signing) (technical design). |
| **Version confusion** | Immutable, uniquely-identified versions; a match records exactly one (FF-1/10). | — |
| **Content poisoning (edit published content)** | **Impossible** — Published+ versions are immutable; corrections are new versions (AI-CONTENT-14/FF-6). | Integrity checks. |
| **Regional abuse (offensive words)** | Handled by **retire + new version** (never in-place edit); emergency removal blocks new selection without touching running matches/history (§7/§11). | Review/AI moderation (future). |
| **Moderation bypass** | Publishing requires the approval gate; AI/community moderation are **advisory inputs**, they cannot publish/edit directly (§11). | Enforced approval workflow (technical design). |
| **Future AI-generated dictionaries** | AI content is **just content** — it must pass the same review/approval and versioning; it cannot bypass moderation or mutate published content (§11). | AI provenance/verification (future). |
| **Future user-generated dictionaries** | Same — community content enters via review; immutable once published; determinism preserved (FF-9). | Reputation/moderation systems (future). |

**Bottom line:** the architecture guarantees **no runtime content mutation, no in-place edits, no bypass of
the approval gate, unique immutable versions, and no impact on running matches/history** — so even
malicious/AI/community content is **contained** by review + immutability + pinning. Residuals (authoring
access control, content signing, moderation tooling) are named **future technical controls**.

---

## 17. Trade-off Analysis

- **Correctness/Fairness:** Maximized — pinned, immutable, rule-neutral content; reproducible boards.
- **Localization:** Excellent — country scope delivers cultural appropriateness; variants/themes compose.
- **Moderation:** Strong — lifecycle gate + immutability + retire/replace; safe corrections.
- **Scalability:** Excellent — read-only, cacheable, replicable content; no room coupling.
- **Maintainability:** High — versioned library; regions evolve independently.
- **Evolution:** Strong — new content ships as new versions without touching running matches or history.
- **Testing:** Strong — determinism per version; reproducibility of historical matches; isolation checks.
- **Business flexibility:** High — regional/theme/seasonal/educational/corporate/tournament packs, and future
  community/AI/premium content, all fit the model.
- **Developer experience:** Clear — "resolve a region → pin one immutable version → draw the board once."
- **Future monetization:** Enabled additively (premium/marketplace packs are content under the same model),
  **without** compromising fairness (content never changes rules) — monetization design is out of scope but
  **unblocked**.
- **Cost of country scope:** more curation than language scope — accepted (shared tooling; cultural fidelity
  is the point).

---

## 18. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| Offensive/duplicate word ships in a version | Business/Moderation | Review gate; retire + new version; FF-6; never in-place edit. |
| Curation cost of many countries | Business/Operational | Shared tooling; prioritize high-demand regions; regional variants added incrementally. |
| A running match affected by a content change | Fairness (critical) | Pinning + immutability (AI-CONTENT-3/4/7); FF-2/3. |
| Version identity collision | Versioning | Globally-unique immutable identities (AI-CONTENT-9); FF-10. |
| Retiring a version still referenced by a live match | Operational | Never remove a version while a live match pins it (§7 Retired); retention until unreferenced ([ADR-005](ADR-005-state-recovery-resilience.md)/[Data Lifecycle](../../03-business-governance/05-data-lifecycle-retention.md)). |
| Community/AI content bypasses moderation | Security | Same review/approval gate; immutability; advisory-only AI (§11/§16). |
| Content accidentally coupling rooms | Isolation | Read-only, identity-addressed content; AI-CONTENT-12; FF-8. |
| Localization drift/inconsistency across variants | Localization | Country base + versioned variants; independent evolution; review. |

---

## 19. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | **Country is the right unit of shared culture** for word appropriateness. | High | If finer granularity is needed, **regional variants** (§10) refine it without model change. |
| AS-2 | **Board words are stored as board state** at generation (so recovery needs no content reload to continue). *(Follows ADR-002.)* | Very High | If words were re-derived at recovery, the pinned version identity + deterministic generation still reproduce them — model holds. |
| AS-3 | **Content can be made immutable & uniquely versioned.** *(Design established in business analysis.)* | Very High | Without immutability, reproducibility/fairness break — the model **requires** it (non-negotiable). |
| AS-4 | **A single pinned version per match suffices** (no mid-match content blending). *(Fact from rules.)* | Fact | — |
| AS-5 | **Content is read-only at runtime**; authoring is offline/administrative. | Very High | If runtime editing were introduced, it would violate AI-CONTENT-11 and must be rejected. |
| AS-6 | **Country scope is affordable** to curate/maintain with shared tooling. | Medium–High | If too costly, start with fewer countries + language-fallback variants — still country-shaped, not language-scoped as the model. |

---

## 20. Non-Goals

This ADR does **not** decide: **database, storage, search, CMS, CDN, admin UI, moderation tools, AI
services, infrastructure, frameworks, deployment, or translation systems**. It defines **only** the content
architecture (model, identity, lifecycle, pinning, versioning, distribution, recovery, moderation-as-
architecture, invariants). Those belong to **Technical Design**.

---

## 21. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-008 |
|-------------|-------------------------------|
| **ADR-010 Command/Query Strategy** | Selecting a region is a **command** (lobby, coordinated); resolving the pinned version at start is part of the Start command; content reads are **read-only queries** against immutable versions; content never a command target during a match. |
| **Software Design** | Introduce a **Content/Dictionary Provider** as a read-only, versioned, identity-addressed component; the rules core draws words at generation only; the aggregate holds the pinned version identity. |
| **Persistence Design** | Store versions **immutably** and **uniquely identified**; retain versions while referenced by live/reproducible matches; board words persist as board state. |
| **Content Management** | Realize the lifecycle (review→publish→deprecate→retire) and moderation gates; enforce immutability and unique identity. |
| **Operations** | Distribute content as replicable read-only data; monitor that no running match's pinned version changes; retention of referenced versions. |
| **Future Marketplace / Premium / Community / AI / Tournament content** | All are **content under this model**: versioned, immutable, moderated via the gate, pinned per match, rule-neutral; monetization/marketplace mechanics are separate future ADRs but must conform. |
| **Future Analytics** | May analyze which versions/regions are used (PII-free); never mutates content or influences a running match. |

---

## 22. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | [BR-RC-4](../../02-business-analysis/02-business-rules.md) (dictionary selection), [BR-BG-2/9](../../02-business-analysis/02-business-rules.md) (draw words; words-only), [BR-GS-3](../../02-business-analysis/02-business-rules.md) (≥25 words), [Dictionary Management](../../02-business-analysis/13-dictionary-management.md). |
| **Business Invariants** | [INV-D1](../../02-business-analysis/10-business-invariants.md) (words only, never rules), [INV-D2](../../02-business-analysis/10-business-invariants.md) (≥25 distinct), [INV-D3](../../02-business-analysis/10-business-invariants.md) (pinned per match), [INV-B6](../../02-business-analysis/10-business-invariants.md) (distinct words), [INV-O4](../../02-business-analysis/10-business-invariants.md) (immutable result). |
| **Engineering Challenges** | [ENG-DC-01/02/03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) (quality/duplicates/offensive; version mismatch/update-during-match; insufficient/unsupported). |
| **Quality Attribute Scenarios** | [QS-12](../09-quality-attribute-scenarios.md) (add region without rule change), [QS-13](../09-quality-attribute-scenarios.md) (localization parity), [QS-01](../09-quality-attribute-scenarios.md) (no leak — content never carries the key). |
| **ADR-001/002** | Content is external read-only input; only the pinned **version identity** is aggregate state; board words are immutable board state. |
| **ADR-003/004** | Content never influences coordination or delivery; region selection is a coordinated command; content never delivered as authoritative truth. |
| **ADR-005** | Recovery preserves the pinned version and the immutable board (reproduces identical words). |
| **ADR-006** | Content (board words) appears in projections; the **key** derives from generation and lives only in aggregate + Spymaster projection; content never leaks it. |
| **ADR-007** | Content is shared, read-only, replicable; never room-owned; never couples rooms or affects ownership. |
| **ADR-009** | Content is participant-agnostic; unaffected by participation lifecycle. |
| **Governance** | [AP-01/02/04/12/14](../../06-architecture-governance/01-architecture-principles.md); [AAP-08/11/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 23. Architecture Review

- **Decision:** **Versioned, Immutable, Country-Scoped Content Library, pinned per match** — country
  dictionaries publishing immutable, uniquely-identified versions; a match pins exactly one version identity
  and draws its board once into immutable board state; content is read-only, shared/replicable, rule-neutral,
  and non-coupling; regional/theme/community/AI packs compose under the same model; moderation acts on the
  lifecycle, never on published content or running matches.
- **Confidence:** **High** — it is entailed by the localization requirement + fairness/determinism invariants
  and aligns with the frozen business Dictionary Management spec; alternatives are disqualified (global,
  language-scoped, mutable).
- **Remaining risks:** curation cost of country scope (business/ops); moderation-gate enforcement and content
  integrity (technical controls); retention of referenced versions (persistence/ops).
- **Open questions (delegated, non-blocking):** storage/CMS/CDN realization; content signing/integrity;
  moderation/AI tooling; marketplace/monetization mechanics (future ADRs); exact regional-variant taxonomy
  (content team).
- **Review triggers:** community/AI/premium content programs (must conform via the gate); a business change
  that would let content affect rules (would violate INV-D1/AI-CONTENT-8 — reject); multi-region content
  distribution specifics.
- **Readiness for ADR-010:** **READY.** Content is fixed as read-only, versioned, pinned input; region
  selection is a coordinated command and content reads are read-only queries — exactly the classification
  **ADR-010 (Command/Query Strategy)** formalizes.

---

## 24. Adversarial Architecture Review — "Attempt to Break the Dictionary Model"

**Attack · Expected Outcome · Architectural Protection · Residual Risk · Mitigation.**

1. **Can a running match receive a new dictionary?**
   - *Expected:* No. *Protection:* pinned + immutable (AI-CONTENT-3/4/7; FF-2). *Residual:* none. *Mitigation:* —
2. **Can recovery load a different version?**
   - *Expected:* No. *Protection:* recovery restores the **pinned identity** + immutable board (AI-CONTENT-5; FF-3). *Residual:* durable-record integrity. *Mitigation:* immutable, verified storage (technical design).
3. **Can migration change the dictionary?**
   - *Expected:* No. *Protection:* migration moves ownership, preserving the aggregate incl. pinned version (AI-CONTENT-6; FF-4; [ADR-007](ADR-007-room-isolation-distribution.md)). *Residual:* none. *Mitigation:* —
4. **Can two rooms accidentally share mutable content?**
   - *Expected:* No. *Protection:* content is **read-only**; sharing read-only content is not coupling (AI-CONTENT-11/12; FF-5/8). *Residual:* none. *Mitigation:* —
5. **Can moderators alter active games?**
   - *Expected:* No. *Protection:* moderation acts on lifecycle, not published content/running matches (AI-CONTENT-10). *Residual:* none. *Mitigation:* —
6. **Can dictionary updates reveal hidden information?**
   - *Expected:* No. *Protection:* content is words only; the key derives at generation and lives server-side (ADR-006); updates touch only future matches. *Residual:* none. *Mitigation:* —
7. **Can regional dictionaries change gameplay rules?**
   - *Expected:* No. *Protection:* content selects words only; rules are language/content-neutral (AI-CONTENT-8; [INV-D1](../../02-business-analysis/10-business-invariants.md); FF-11). *Residual:* none. *Mitigation:* —
8. **Can dictionary deletion invalidate history?**
   - *Expected:* No. *Protection:* a version referenced by a live/reproducible match is **not removed**; matches store board words as state; retired versions retained while referenced (§7/§13). *Residual:* premature deletion by ops error. *Mitigation:* retention policy (referenced-versions protected); board words persist as state.
9. **Can two versions have the same identity?**
   - *Expected:* No. *Protection:* globally-unique immutable identity (AI-CONTENT-9; FF-10). *Residual:* identity-scheme bug. *Mitigation:* uniqueness check.
10. **Can content updates create unfair matches?**
    - *Expected:* No (for running matches). *Protection:* running matches are pinned/immutable; new content only affects future matches equally (AI-CONTENT-7). *Residual:* a poor version harms *future* matches. *Mitigation:* review gate; retire + new version.
11. **Can distribution duplicate mutable content?**
    - *Expected:* No. *Protection:* content is immutable/read-only; replicas are equivalent; there is no mutable content to duplicate (AI-CONTENT-11; §12). *Residual:* none. *Mitigation:* —
12. **Can cached content become authoritative?**
    - *Expected:* Harmless. *Protection:* a match's authoritative truth is the **board state** in its aggregate; cached content is only used to **generate** a board at start (once) — a cache serves immutable content by identity, never a second truth. *Residual:* stale cache serving a wrong-identity version. *Mitigation:* identity-addressed cache; immutability makes a correct-identity hit always correct.
13. **Can user-generated dictionaries break determinism?**
    - *Expected:* No. *Protection:* UGC enters via review, becomes an **immutable version**; determinism per version holds (FF-9); a match pins one version. *Residual:* low-quality content. *Mitigation:* moderation gate.
14. **Can AI-generated dictionaries bypass moderation?**
    - *Expected:* No. *Protection:* AI content is content — it must pass the same review/approval gate; AI is advisory, cannot publish/edit (§11/§16). *Residual:* weak moderation process. *Mitigation:* enforced approval workflow (technical/ops).
15. **Can dictionary changes affect replay or recovery?**
    - *Expected:* No. *Protection:* replay/recovery use the **pinned version identity** + immutable board; later content changes are irrelevant (AI-CONTENT-5; FF-3/7). *Residual:* none. *Mitigation:* —

**Conclusion:** the content model **cannot change a running or completed match, cannot leak hidden
information, cannot change rules, cannot couple rooms, cannot lose reproducibility, and cannot be mutated
at runtime** — **by construction** — because content is **country-scoped, versioned, immutable,
uniquely-identified, read-only, pinned per match, and rule-neutral**, with moderation acting only on the
**lifecycle**. Even community/AI/premium content is contained by **review + immutability + pinning**. The
only genuine residuals — **curation quality/cost**, **content-integrity/authoring access control**, and
**referenced-version retention** — are **future technical/operational/business controls**, delegated and
named, not architectural weaknesses.

---

## Final Deliverable — Answers

- **What is a dictionary?** **Versioned, immutable, country-scoped, read-only content** — a curated word set
  for a culture — that gameplay **consumes** (to generate a board) but never owns or edits.
- **Why is a dictionary not part of room state?** Because content is **shared, read-only, and evolvable
  independently**; a room holds only a **pinned version *identity*** (and the drawn board words as immutable
  board state), not editable content — so content can evolve without touching truth.
- **Why country-based rather than language-based?** Because fair clue-giving depends on **shared cultural
  knowledge**, which is **cultural/country**-level, not language-level; one language (Spanish/Arabic/English)
  spans many cultures with divergent vocabulary, slang, and taboos → country scope is the right unit
  (variants refine it further).
- **Why is a dictionary pinned when the match starts?** So the match has a **fixed, immutable input**:
  pinning converts evolving content into a stable version, guaranteeing fairness, determinism,
  reproducibility, recovery, and migration for content.
- **What survives recovery?** The **pinned version identity** and the **immutable board** (words/key/reveals)
  — reproducing the same words.
- **What survives migration?** The same — migration moves **ownership**, preserving the pinned version and
  board.
- **How do active matches remain unaffected by new content?** They keep their **pinned version** and
  **already-generated board**; publish/deprecate/retire affect only **future** matches.
- **How are dictionaries distributed safely?** As **read-only, immutable, identity-addressed content** that
  is freely **replicable/cacheable**, owned by **no room**, coupling **no** rooms, affecting **no** ownership.
- **Why are dictionaries read-only?** Because immutability is what guarantees reproducibility, fairness, safe
  evolution (correct-by-new-version), and containment of community/AI content — an in-place edit could change
  a running match or break history.
- **How does the architecture support future community dictionaries, AI-generated content, premium packs,
  seasonal events, and enterprise content without changing the core?** All are **content under the same
  model**: they enter via the **review/approval** gate, become **immutable, uniquely-versioned** packs, are
  **pinned per match**, remain **read-only and rule-neutral**, and compose as regional/theme packs.
  Marketplace/monetization/AI-generation/moderation *mechanisms* are separate future concerns that must
  **conform to** this model — none requires changing it.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: versioned, immutable, country-scoped content library pinned per match; identity, lifecycle, pinning, versioning, regional/moderation/distribution/recovery models, invariants, fitness functions, security & adversarial review, verdict. |
