# Content Platform — 01. Business Vision

| | |
|---|---|
| **Bounded Context** | Content Platform |
| **First Capability** | Dictionary Management |
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Phase** | First major product capability after the MVP; **feature design only — not implementation** |
| **Depends on** | Authentication ([Roadmap Phase 2](../03-business-governance/06-product-roadmap.md#5-phase-2--accounts--continuity-future)); the content model of [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md); the frozen [Dictionary Management](../02-business-analysis/13-dictionary-management.md) business spec. |
| **Supersedes (additively)** | The MVP out-of-scope items [BRD §1.6](../01-product-discovery/01-business-requirements.md#16-out-of-scope) "Custom user-supplied word lists" and "Custom user-supplied word lists (only curated regional dictionaries)"; the runtime-authoring prohibition in [DM-O3](../02-business-analysis/13-dictionary-management.md#11-business-ownership). Reversal is **governed** per Roadmap guardrail [G-5](../03-business-governance/06-product-roadmap.md#10-guardrails) and recorded in **ADR-011** (this feature). |
| **Technology** | **Neutral** — no storage, database, SQL, API, framework, format, or code. Business & product only. |

---

## Table of Contents
1. [Purpose & Scope of this Document](#1-purpose--scope-of-this-document)
2. [Why this Feature Exists](#2-why-this-feature-exists)
3. [Product Vision](#3-product-vision)
4. [Business Goals](#4-business-goals)
5. [The Load-Bearing Seam — Authored vs Consumed](#5-the-load-bearing-seam--authored-vs-consumed)
6. [Relationship to the Frozen Baseline](#6-relationship-to-the-frozen-baseline)
7. [Personas](#7-personas)
8. [Content Types & Ownership Model (Conceptual)](#8-content-types--ownership-model-conceptual)
9. [Long-Term Strategy — the Content Platform](#9-long-term-strategy--the-content-platform)
10. [Product Principles](#10-product-principles)
11. [Success Metrics](#11-success-metrics)
12. [Out of Scope](#12-out-of-scope)
13. [Assumptions](#13-assumptions)
14. [Risks (Business-Level)](#14-risks-business-level)
15. [Open Questions (Non-Blocking)](#15-open-questions-non-blocking)
16. [Traceability](#16-traceability)
17. [Revision History](#17-revision-history)

---

## 1. Purpose & Scope of this Document

This is the **first** deliverable in the design of a new bounded context, **Content Platform**, whose first
capability is **Dictionary Management**. It establishes *why the capability exists, what it is for, whom it
serves, and where it must never go* — before any requirement, use case, decision, or design is written.

It is **vision and product strategy only**. It defines no functional requirement (that is document 02), no
use case (03), no requirement specification (04), no architectural decision (ADR-011), and no design (06/07).
It chooses **no** technology, storage, format, or interface.

**One-line statement:** *Turn the closed, team-curated dictionary of the MVP into an open, multi-tenant
**Content Platform** where any authenticated person, team, or organization can create, publish, share, clone,
and browse word collections — while every promise the MVP made about fairness, determinism, immutability,
and zero-signup play remains exactly as strong as before.*

---

## 2. Why this Feature Exists

The MVP treats dictionaries as a **closed, centrally-curated resource**: a Content/Localization team owns one
Country Dictionary per region ([DM-C1](../02-business-analysis/13-dictionary-management.md#4-country-dictionary),
[DM-O1/O3](../02-business-analysis/13-dictionary-management.md#11-business-ownership)), and players may only
*select* a region — never create content ([BRD §1.6](../01-product-discovery/01-business-requirements.md#16-out-of-scope)).
That was the right MVP scope: it kept the game faithful, launchable, and free of moderation load. It is not a
long-term product ceiling.

The reasons this capability exists now:

| # | Driver | Explanation |
|---|--------|-------------|
| WHY-1 | **Content is the game's replay engine.** | Codenames' fun is bounded by its word pool. A fixed, official-only pool caps replayability. Letting people bring their own words (a class's vocabulary, a friend group's in-jokes, a company's product names, a fandom's lexicon) makes the *same faithful game* endlessly re-playable and personal. |
| WHY-2 | **Demand for personalization is the top retention lever.** | Party games live or die on "make it *ours*". A themed board (our office, our wedding, our D&D campaign) is the difference between one session and a habit. |
| WHY-3 | **The architecture already anticipated it.** | [ADR-008 §16/§21](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#16-security-analysis) explicitly named "future user-generated dictionaries" and "future community/premium/AI content" as **governed additions that conform to the same review + immutability + pinning model**. This feature is that seam being realized, not a redesign. |
| WHY-4 | **Authentication (Phase 2) unlocks ownership.** | The MVP had no durable identity, so content could have no durable owner. Once accounts exist ([Roadmap Phase 2](../03-business-governance/06-product-roadmap.md#5-phase-2--accounts--continuity-future)), *owned* content becomes possible — and ownership is the precondition for creation, sharing, and a marketplace. |
| WHY-5 | **It is the foundation of a content economy, not a single feature.** | Educational packs, organization content, premium packs, seasonal/tournament packs, community marketplace, and AI-generated content are all *the same thing* — owned, versioned, immutable content. Building the platform once means every one of those ships later **without redesign**. |
| WHY-6 | **A moat that compounds.** | A growing library of community and official content is a network effect a competitor cannot copy overnight. The platform is where durable product value accrues after the game itself is commoditized. |

**If we do not build it:** Cluely stays a faithful but finite Codenames clone whose content roadmap is
throttled by a single internal team, with no personalization, no community, and no path to monetized content.

---

## 3. Product Vision

> **Cluely becomes a platform for word content, not just a game.**
> Anyone can turn any set of words into a playable dictionary in minutes; publish it as an immutable,
> shareable version; hand it to friends, a classroom, or a company; discover what the community has made;
> and clone-and-remix anything they are allowed to see — all while every match remains as fair, deterministic,
> and reproducible as it is today, and while playing still requires nothing but a room code.

The vision has three horizons:

- **Near:** *Personal & shared dictionaries.* An authenticated user creates private word collections, drafts
  and publishes immutable versions, shares them with specific people, and selects them when creating a room.
- **Mid:** *Community & organization content.* Public discovery/browsing, cloning, import/export of
  collections, organization-owned libraries, and educational content — a living catalog of word sets.
- **Long:** *A content economy.* Official, premium, seasonal, tournament, localization, and AI-generated
  packs; a community marketplace; curation and recognition — all as *content under one model*.

The through-line: **the game consumes content; the platform produces it; the two meet at exactly one narrow,
immutable seam.**

---

## 4. Business Goals

| # | Goal | Rationale | Relates to |
|---|------|-----------|-----------|
| CG-1 | **Let any authenticated user author, publish, and manage their own dictionaries.** | Personalization & replayability (WHY-1/2). | BO-1 (authentic play, now extensible) |
| CG-2 | **Enable sharing and reuse of content between users** (share, clone, import/export). | Social spread; content compounds (WHY-2/6). | BO-3 (friend-group play) |
| CG-3 | **Provide a browsable catalog of community and official content.** | Discovery drives the network effect (WHY-6). | New |
| CG-4 | **Preserve every fairness/determinism/immutability guarantee of the MVP, unchanged.** | Non-negotiable; the platform must never weaken the game (WHY-3). | BO-6, [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) |
| CG-5 | **Preserve zero-signup play.** Authoring needs an account; *playing with* published content does not. | Protects the MVP's biggest advantage (WHY-4). | BO-2, C-2 |
| CG-6 | **Establish one content model that all future content types reuse without redesign.** | Platform, not feature (WHY-5). | [ADR-008 §21](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#21-impact-on-future-adrs) |
| CG-7 | **Contain user/community/AI content by construction** (review + immutability + pinning), so openness never compromises safety or history. | Moderation and integrity at scale (WHY-5). | [ADR-008 §11/§16](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#11-moderation-architecture) |
| CG-8 | **Unblock future monetization** (premium/marketplace) additively, without ever letting content affect rules. | Content economy (WHY-5/6). | [ADR-008 §17](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#17-trade-off-analysis) |

---

## 5. The Load-Bearing Seam — Authored vs Consumed

This is the single most important distinction in the entire feature. **Two activities that sound related are
architecturally and commercially opposite, and must never be conflated.**

| | **Authoring** (the Content Platform) | **Consuming** (gameplay, unchanged) |
|---|---|---|
| **What** | Create, draft, validate, publish, share, clone, import, export, browse, moderate content. | Select a published version at room creation; draw a board from it once; play. |
| **Who** | **Authenticated** users, organizations, official teams (WHY-4, CG-1). | **Anyone** — including fully anonymous, account-free players (BO-2, C-2). |
| **When** | *Before/outside* a match, in a creator surface. | *At and during* a match, in a room. |
| **Mutability** | Drafts are editable; **published versions are immutable forever** (new content = new version). | Content is **read-only**; a match pins one immutable version and never observes any change to it. |
| **Governed by** | ADR-011 (this feature). | [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) (frozen); this feature **does not change it**. |

**The seam:** the Content Platform produces **published, immutable version identities**. Gameplay consumes a
dictionary **only** by pinning one such identity into a room's authoritative aggregate — exactly the existing
[ADR-008 §8 pinning contract](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#8-match-pinning-critical).
Nothing in the Content Platform ever reaches into a running or completed match.

**Two rules fall directly out of the seam and are stated here so no later document can lose them:**

- **SEAM-1 — Authoring requires an account; playing with content does not.** Requiring authentication to
  *create* content does not, and must not, require authentication to *play with* published content in a room.
  Room creation may let a host pick a published dictionary; joining and playing remain code-only and
  account-free. Any design that forces sign-up to *play* violates C-2 and is rejected.
- **SEAM-2 — The platform is strictly upstream of gameplay.** The Content Platform's only downstream effect is
  that a *published version identity becomes selectable*. It has **no** write path into any match, ever.

---

## 6. Relationship to the Frozen Baseline

The MVP baseline is frozen. This feature is an **additive, governed evolution** — it adds a parallel,
user-owned content lane alongside the existing official lane; it does not rewrite the official lane, and it
does not touch gameplay.

### 6.1 What is preserved unchanged

- **Gameplay, rules, and constants** — untouched (Roadmap [G-1](../03-business-governance/06-product-roadmap.md#10-guardrails)).
  Dictionaries still affect **words only**, never counts, turn flow, or outcomes ([INV-D1](../02-business-analysis/10-business-invariants.md)).
- **The ADR-008 content model** — versioned, immutable, read-only, pinned-per-match content
  ([AI-CONTENT-1..14](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-)).
  Every new content type obeys it.
- **Zero-signup play** — BO-2, C-2 (see [SEAM-1](#5-the-load-bearing-seam--authored-vs-consumed)).
- **The official/localization lane** — the Content/Localization team keeps curating official country
  dictionaries exactly as specified in [Dictionary Management](../02-business-analysis/13-dictionary-management.md);
  they are simply recast as the **"official" owner type** of a broader model.

### 6.2 What is deliberately superseded (governed by Roadmap G-5, recorded in ADR-011)

| Frozen item | Was | Becomes | Mechanism |
|-------------|-----|---------|-----------|
| [BRD §1.6](../01-product-discovery/01-business-requirements.md#16-out-of-scope) "Custom user-supplied word lists" | Out of scope | **In scope** as a governed, authenticated capability | Superseding ADR-011 + this vision |
| [DM-O3](../02-business-analysis/13-dictionary-management.md#11-business-ownership) "Runtime … cannot create, edit, or retire dictionaries" | Absolute prohibition | Prohibition **relaxed for authoring surfaces**, kept absolute **during gameplay** ([SEAM-2](#5-the-load-bearing-seam--authored-vs-consumed)) | ADR-011 |
| [DM-Q4](../02-business-analysis/13-dictionary-management.md#9-word-quality--offensive-content) "No user-supplied words" | True for MVP | User-supplied words allowed **through the review/immutability gate** | ADR-011 + moderation model |

> **Guardrail honored:** Roadmap [G-5](../03-business-governance/06-product-roadmap.md#10-guardrails) requires that
> any reversal of an MVP decision be recorded as a new superseding ADR. **ADR-011** (the fifth deliverable of
> this feature) is that record. This document only *declares intent*; ADR-011 makes it binding.

### 6.3 What must never happen (inherited red lines)

Openness never buys these back. The platform must **never** allow:
published content to change; a running match to observe a content mutation; shared *mutable* content state;
an edit to affect historical matches; leaking of private content; a break of room isolation
([ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md)); or a break of
determinism. These are the [Critical Constraints](#10-product-principles) and are non-negotiable across every
future content type.

---

## 7. Personas

Derived from the capability list. Each persona's needs seed the requirements (doc 02) and use cases (doc 03).

| # | Persona | Who | Primary jobs-to-be-done | Success looks like |
|---|---------|-----|-------------------------|--------------------|
| P-1 | **Creator / Hobbyist** | An authenticated player who wants a personal, themed board. | Create a dictionary, add words (type or import), validate, publish a version, use it in a room, tweak and re-publish. | "I made an 'our friend group' pack in five minutes and we played it tonight." |
| P-2 | **Educator** | A teacher building vocabulary/subject packs for a class. | Author curated learning content, publish stable versions, share with students, reuse across terms, clone-and-adapt each year. | "My Unit-3 vocabulary pack is a published version my class plays every year." |
| P-3 | **Organization / Team Admin** | An admin curating content for a company or community org. | Own an organization library, control who may author and who may see it, publish internal packs (onboarding, product names, events). | "Our onboarding pack is private to the company and every new hire plays it." |
| P-4 | **Community Browser / Consumer** | A player looking for something fun to play, possibly anonymous. | Browse public dictionaries, pick one when hosting a room, clone a public one to adapt it. | "I found a great movies pack in the catalog and hosted a game with it." |
| P-5 | **Moderator** | A trusted reviewer safeguarding shared/public content. | Review, approve, block, or emergency-retire content on the **lifecycle** — never editing published words or touching live matches. | "I pulled an offensive pack from discovery without affecting any game in progress." |
| P-6 | **Official Content Author** | The existing Content/Localization team, recast. | Author and publish **official** country dictionaries and localization/seasonal/tournament packs under the same model. | "Official Egypt v4 published; nothing about how I work changed." |
| P-7 | **Platform / Product Owner** | Owns catalog quality, policy, and (future) monetization. | Set visibility/sharing/moderation policy; steward the catalog; enable premium/marketplace later. | "The catalog grows and stays safe; premium packs are a config away, not a rebuild." |

> The **anonymous player** remains a first-class actor of the *game* (BO-2) but is **not** an actor of the
> *Content Platform's authoring* surface — they consume published content only ([SEAM-1](#5-the-load-bearing-seam--authored-vs-consumed)).

---

## 8. Content Types & Ownership Model (Conceptual)

The MVP's [DM-C1](../02-business-analysis/13-dictionary-management.md#4-country-dictionary) ("exactly one
Country Dictionary per region") constrains **official** content only. It must **not** leak into the general
model: user, community, and organization dictionaries are **owner-scoped**, freely themed, and **many per
owner** — they are not country-scoped and not subject to DM-C1.

The vision therefore introduces **content type** as a first-class, extensible concept. Every type obeys the
**same** lifecycle, immutability, versioning, and pinning ([ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md));
types differ only in **who owns** them and **who may see** them.

| Content Type | Owner | Typical scope | DM-C1 (one-per-region)? | Status |
|--------------|-------|---------------|:-----------------------:|--------|
| **Official** | Content/Localization team | Country/culture | **Yes** (unchanged) | Exists; recast into model |
| **User** | An individual account | Any theme | No | **This feature** |
| **Organization** | An org/team | Any theme, org-visible | No | Near/mid (same model) |
| **Educational** | Educator / institution | Subject/curriculum | No | Mid (same model) |
| **Community (public)** | An individual, shared publicly | Any theme | No | Near/mid (same model) |
| **Premium / Marketplace** | Official or vetted creators | Any theme, monetized | No | Long (same model) |
| **Seasonal / Tournament / Localization pack** | Official | Event/region | Type-specific | Composable ([ADR-008 §10](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#10-regional-architecture)) |
| **AI-generated** | System, on behalf of an owner | Any theme | No | Long (same gate) |

> **This document does not fix the taxonomy or the ownership/visibility rules** — it establishes that ownership
> and visibility are **typed, first-class, and extensible**. The precise model is decided in **ADR-011** and
> specified in documents 02 and 06. The single invariant asserted here: *whatever the type, it is versioned,
> immutable-once-published, and pinned per match — no exceptions.*

---

## 9. Long-Term Strategy — the Content Platform

**Do not design "Custom Dictionaries." Design the Content Platform whose first capability is Dictionary
Management.** The strategic test for every decision in this feature: *does it make the next content capability
cheaper, or does it hard-code an assumption that a future capability must undo?*

```
                         ┌─────────────────────────────────────────────┐
                         │              CONTENT PLATFORM                │
                         │   (owned, typed, versioned, immutable,       │
                         │        moderated, read-only content)         │
                         └─────────────────────────────────────────────┘
   Capability 1 ─────────►  Dictionary Management   ◄──── THIS FEATURE
   Capability 2 ─────────►  Community browse / clone / import-export
   Capability 3 ─────────►  Organization & educational libraries
   Capability 4 ─────────►  Premium packs & marketplace / monetization
   Capability 5 ─────────►  Seasonal / tournament / localization packs
   Capability 6 ─────────►  AI-generated content (through the same gate)
                         │
                         ▼   (one narrow, immutable seam — ADR-008 pinning)
                         GAMEPLAY  (faithful Codenames — never changes)
```

**Strategic bets:**

- **One model, many capabilities.** Every future capability above is *content under the same versioned,
  immutable, moderated, pinned model*. Adding one must require **no redesign** of the platform or the game
  ([ADR-008 §21](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#21-impact-on-future-adrs)).
- **The seam is the product's spine.** Because content meets gameplay only at the immutable pinning seam, the
  platform can grow arbitrarily rich while the game stays provably fair and reproducible. This is the durable
  competitive advantage.
- **Openness is contained, not traded.** Community/AI content enters through review + immutability + pinning
  ([CG-7](#4-business-goals)); we get network effects *and* safety, rather than choosing one.
- **Monetization is unblocked, not built now.** Premium/marketplace mechanics are a later capability that
  *slots into* the model; nothing in this feature should make them harder, and nothing here builds them.

---

## 10. Product Principles

Guiding, testable principles for the whole feature (and its successors). *Critical Constraints* are hard red
lines; violating one invalidates a design.

| # | Principle | Kind |
|---|-----------|------|
| PP-1 | **Immutability after publication.** A published version never changes; corrections are new versions. | Critical |
| PP-2 | **Running matches never observe content mutation.** Publish/deprecate/retire/delete touch only future matches. | Critical |
| PP-3 | **No shared *mutable* content state; content is read-only at runtime.** | Critical |
| PP-4 | **Edits never affect historical matches.** History is reproducible from pinned version identities. | Critical |
| PP-5 | **Explicit ownership.** Every dictionary has exactly one clear owner; ownership is never ambiguous or duplicated. | Critical |
| PP-6 | **Explicit visibility.** Private content never leaks; visibility is deliberate and enforced, never accidental. | Critical |
| PP-7 | **Determinism preserved.** Same pinned version ⇒ same word set; board generation stays reproducible ([FF-CONTENT-9](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#15-architecture-fitness-functions-ff-content-)). | Critical |
| PP-8 | **Content selects words only — never rules, counts, flow, or outcomes** ([INV-D1](../02-business-analysis/10-business-invariants.md)). | Critical |
| PP-9 | **Zero-signup play preserved.** Authoring needs an account; playing with published content does not. | Critical |
| PP-10 | **One model for all content types.** Ownership/visibility vary; lifecycle, immutability, versioning, and pinning do not. | Guiding |
| PP-11 | **Editing creates a new version.** The unit of change is a new immutable version, never an in-place edit. | Guiding |
| PP-12 | **Frictionless authoring.** Creating and publishing a usable dictionary should feel like minutes, not a chore. | Guiding |
| PP-13 | **Safe by construction.** Openness is contained by the review + immutability + pinning gate, not by trust. | Guiding |
| PP-14 | **Extensibility first.** Prefer designs that make the next content capability cheaper. | Guiding |

---

## 11. Success Metrics

Product/business KPIs for the capability. *(Measurable NFR quality targets — latency, availability, etc. —
are defined later in the SRS Addendum, document 04, not here.)*

| # | Metric | What it measures | Direction |
|---|--------|------------------|-----------|
| SM-1 | **Creation adoption** — % of authenticated users who create ≥1 dictionary. | Does authoring resonate? (CG-1) | ↑ |
| SM-2 | **Publish rate** — % of created dictionaries that reach a published version. | Do drafts become usable content? (CG-1) | ↑ |
| SM-3 | **Time-to-first-publish** — median time from create to first published version. | Authoring friction (PP-12). | ↓ |
| SM-4 | **Play-through rate** — % of published dictionaries used in ≥1 room. | Does authored content get played? (WHY-1) | ↑ |
| SM-5 | **Share reach** — dictionaries shared, and distinct recipients per shared dictionary. | Social spread (CG-2). | ↑ |
| SM-6 | **Clone rate** — clones created from shareable/public dictionaries. | Remix & reuse (CG-2). | ↑ |
| SM-7 | **Catalog growth** — count of public/discoverable dictionaries over time. | Network effect (CG-3/6). | ↑ |
| SM-8 | **Browse-to-use conversion** — % of catalog browse sessions that end in selecting a dictionary for a room. | Discovery effectiveness (CG-3). | ↑ |
| SM-9 | **Import/export usage** — collections imported/exported. | Portability adoption. | ↑ |
| SM-10 | **Moderation response SLA** — time from a report/flag to a lifecycle action (block/retire). | Safety at scale (CG-7). | ↓ |
| SM-11 | **Content-fairness incidents** — matches provably affected by a content change. | Guardrail health (PP-1/2/4). | **0 (hard)** |
| SM-12 | **Private-leak incidents** — private content exposed without authorization. | Guardrail health (PP-6). | **0 (hard)** |
| SM-13 | **Zero-signup regressions** — flows that newly require an account to *play*. | Guardrail health (PP-9, C-2). | **0 (hard)** |

> SM-11/12/13 are **guardrail metrics**: their target is zero, and any nonzero value is a defect, not a KPI to
> optimize.

---

## 12. Out of Scope

Excluded from **this capability** (Dictionary Management). Each is *"same architecture, later capability"*
unless marked as a permanent red line.

| # | Out of scope | Nature |
|---|--------------|--------|
| OOS-1 | **Monetization & marketplace mechanics** (pricing, purchase, payout, entitlements). | Later capability; must slot into the same model ([CG-8](#4-business-goals)). |
| OOS-2 | **AI generation of content.** | Later capability; enters through the *same* review/immutability gate ([ADR-008 §16](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#16-security-analysis)). |
| OOS-3 | **Moderation *tooling* / AI-moderation systems.** | The moderation **lifecycle** is in scope; the **tools** that operate it are a later/technical concern ([ADR-008 §11/§20](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#11-moderation-architecture)). |
| OOS-4 | **Organization/educational *administration* features** (org management, roles, billing). | Later capability; the *content* model already accommodates org-owned content ([§8](#8-content-types--ownership-model-conceptual)). |
| OOS-5 | **Ratings, reviews, comments, social graph around content.** | Later capability; not required for the first capability. |
| OOS-6 | **Any change to gameplay, rules, constants, or the pinning contract.** | **Permanent red line** — never in scope for any content capability ([PP-8](#10-product-principles), Roadmap [G-1](../03-business-governance/06-product-roadmap.md#10-guardrails)). |
| OOS-7 | **Technology choices** — storage, database, API, import/export *encoding*, search engine, infrastructure. | Deferred to Technical Design (document 07); this feature designs the *logical* model only. |
| OOS-8 | **Authentication mechanism itself.** | Owned by [Roadmap Phase 2](../03-business-governance/06-product-roadmap.md#5-phase-2--accounts--continuity-future); this feature **depends on** it and does not design it. |

---

## 13. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| VA-1 | **Durable accounts exist** (Phase 2) before this capability ships, giving content a durable owner. | High | Without accounts, ownership is impossible; the capability is blocked, not redesigned. |
| VA-2 | **The ADR-008 content model holds for user/community/AI content** as ADR-008 §16/§21 asserted. | Very High | If it did not, the whole feature would need a new content ADR — but ADR-008 explicitly designed for this. |
| VA-3 | **Consuming published content in a room can remain account-free** ([SEAM-1](#5-the-load-bearing-seam--authored-vs-consumed)). | Very High | If playing required auth, C-2/BO-2 break — such a design is rejected, not accepted. |
| VA-4 | **A single pinned version per match still suffices** — no mid-match content blending, regardless of type. | Fact ([ADR-008 AS-4](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#19-assumptions)) | — |
| VA-5 | **Users will supply words that require moderation** (offensive, low-quality, infringing). | High | The review + immutability gate is designed for exactly this; higher volume raises tooling need (OOS-3), not model change. |
| VA-6 | **`DICTIONARY_MIN_WORDS` (25) and word-uniqueness rules apply to user content too.** | High | If user content had different validity rules, boards could fail to generate — so the same floor applies ([§2 DM-VAL](../02-business-analysis/13-dictionary-management.md#8-validation--constraints)). |

---

## 14. Risks (Business-Level)

| # | Risk | Impact | Likelihood | Mitigation (business-level) |
|---|------|--------|-----------|------------------------------|
| VR-1 | **Offensive/inappropriate user content** reaches players. | High | High | Review lifecycle gate before public/shared visibility; report→block→retire; immutability + new-version corrections ([ADR-008 §11](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md#11-moderation-architecture)); moderation SLA (SM-10). |
| VR-2 | **Copyright/IP-infringing content** is published. | High | Medium | Ownership + takedown via lifecycle (block/retire, never edit-in-place); policy owned by Product (P-7); a later technical/legal control. |
| VR-3 | **Scope creep** — the feature drifts toward building the whole content economy at once. | High | Medium | Strict [Out of Scope](#12-out-of-scope); "one document at a time" governance; each future capability passes its own governance (Roadmap [G-2](../03-business-governance/06-product-roadmap.md#10-guardrails)). |
| VR-4 | **The auth seam is misread**, forcing sign-up to *play*. | Critical | Medium | [SEAM-1](#5-the-load-bearing-seam--authored-vs-consumed) stated as a critical principle (PP-9); guardrail metric SM-13 (target 0). |
| VR-5 | **A design lets content touch a running match** (freshness over fairness). | Critical | Low | Inherited ADR-008 pinning/immutability invariants (PP-1/2/4); guardrail metric SM-11 (target 0); adversarial review in ADR-011. |
| VR-6 | **Private content leaks** through sharing/cloning/browsing. | High | Medium | Explicit visibility (PP-6); guardrail metric SM-12 (target 0); visibility/sharing rules specified in doc 02 and ADR-011. |
| VR-7 | **Moderation load** outpaces capacity as the catalog grows. | Medium | High | Lifecycle designed for scale; tooling (OOS-3) as a fast-follow; visibility defaults limit blast radius (e.g., private-by-default). |
| VR-8 | **Curation cost / low-quality catalog** dilutes discovery value. | Medium | Medium | Validation floor (VA-6); clone-and-improve (CG-2); official + community curation; recognition mechanics (later). |
| VR-9 | **DM-C1 leaks** into user content, wrongly forcing one-per-region/uniqueness on personal packs. | Medium | Medium | Typed ownership ([§8](#8-content-types--ownership-model-conceptual)); DM-C1 scoped to *official* type only. |

---

## 15. Open Questions (Non-Blocking)

These are **deliberately deferred** to later documents; naming them here prevents them from silently biasing
the vision.

| # | Question | Deferred to |
|---|----------|-------------|
| OQ-1 | Exact **visibility levels** (private / shared-with / organization / public) and their precise semantics. | Doc 02 (requirements) + ADR-011 |
| OQ-2 | Exact **sharing model** (direct grant vs link vs org membership) and revocation semantics. | Doc 02 + ADR-011 |
| OQ-3 | **Clone semantics** — provenance/attribution, whether clones carry a lineage link, private-source cloning rules. | Doc 02 + ADR-011 |
| OQ-4 | **Deletion vs archiving** policy for owned content still referenced by matches. | Doc 02 + [Data Lifecycle](../03-business-governance/05-data-lifecycle-retention.md) |
| OQ-5 | Whether **user content is country/region-tagged** at all, and how it relates to the official localization taxonomy. | ADR-011 + doc 06 |
| OQ-6 | **Moderation trigger** for user content: pre-publication review vs post-publication report-driven, per visibility level. | ADR-011 + doc 02 |
| OQ-7 | **Import/export** logical shape (what a "word collection" contains) — the *format encoding* stays out of scope (OOS-7). | Doc 07 |
| OQ-8 | Whether **organizations** are a Phase-2 identity primitive or a later addition. | Roadmap / Phase 2 alignment |

---

## 16. Traceability

| Dimension | References |
|-----------|-----------|
| **Business (baseline)** | [BRD §1.2/1.3/1.6](../01-product-discovery/01-business-requirements.md) (vision/objectives/scope), [C-2/C-4](../01-product-discovery/01-business-requirements.md#17-constraints) (no-auth-to-play; only content localized). |
| **Dictionary business** | [Dictionary Management DM-C1/O1/O3/Q4/V1](../02-business-analysis/13-dictionary-management.md) (recast into a typed, owned model). |
| **Invariants** | [INV-D1/D2/D3](../02-business-analysis/10-business-invariants.md) (words-only; ≥25 distinct; pinned per match) — preserved. |
| **Architecture** | [ADR-008](../07-software-architecture/12-decisions/ADR-008-dictionary-content-architecture.md) (content model; §16/§21 anticipated this seam), [ADR-007](../07-software-architecture/12-decisions/ADR-007-room-isolation-distribution.md) (isolation). |
| **Roadmap / governance** | [Roadmap Phase 2](../03-business-governance/06-product-roadmap.md#5-phase-2--accounts--continuity-future) (accounts dependency), [G-1/G-2/G-5](../03-business-governance/06-product-roadmap.md#10-guardrails) (guardrails honored). |
| **Constants** | [`DICTIONARY_MIN_WORDS`=25](../_meta/00-canonical-constants-and-index.md) (applies to all content types, VA-6). |
| **Forward** | This vision → doc 02 (Business Requirements) → doc 03 (Use Cases) → doc 04 (SRS Addendum) → **ADR-011** → doc 06 (Software Design) → doc 07 (Technical Design). |

---

## 17. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-11 | Initial Business Vision for the **Content Platform** bounded context and its first capability, **Dictionary Management**: why it exists, product vision & horizons, business goals, the authored-vs-consumed seam, relationship to the frozen baseline (additive supersession via Roadmap G-5 / ADR-011), personas, typed ownership model, long-term platform strategy, product principles & critical constraints, success metrics, out-of-scope, assumptions, risks, and open questions. Technology-neutral; introduces no gameplay change. |
