# ADR-011 — Content Platform: Ownership, Publication, Versioning & Sharing

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-11 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How does Cluely let any authenticated user create, own, version, publish, share, clone, and moderate word content — as a first-class product platform — without changing gameplay and without weakening any fairness, determinism, immutability, isolation, or hidden-information guarantee?* |
| **Complies with** | [ADR-000](ADR-000-architecture-vocabulary.md), [ADR-001](ADR-001-overall-architecture-style.md), [ADR-002](ADR-002-authoritative-game-state.md), [ADR-003](ADR-003-per-room-coordination-model.md), [ADR-004](ADR-004-real-time-communication-delivery.md), [ADR-005](ADR-005-state-recovery-resilience.md), [ADR-006](ADR-006-role-based-information-visibility.md), [ADR-007](ADR-007-room-isolation-distribution.md), [ADR-008](ADR-008-dictionary-content-architecture.md), [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md), [ADR-010](ADR-010-command-query-strategy.md). **Extends** ADR-008 (realizes its §16/§21 "future user/community content" seam); **redefines none**. |
| **Realizes** | [Content Platform 01 — Business Vision](../../15-content-platform/01-business-vision.md), [02 — Feature Specification](../../15-content-platform/02-feature-specification.md). |
| **Supersedes (additively, per Roadmap [G-5](../../03-business-governance/06-product-roadmap.md#10-guardrails))** | The MVP out-of-scope item [BRD §1.6](../../01-product-discovery/01-business-requirements.md#16-out-of-scope) "custom user-supplied word lists" and the runtime-authoring prohibition [DM-O3](../../02-business-analysis/13-dictionary-management.md#11-business-ownership) — **for authoring surfaces only**; both remain in force **during gameplay**. |
| **Scope note** | Defines the **Content Platform architecture** only: bounded context, aggregates, ownership, lifecycle, versioning, visibility, sharing, cloning, publication/review, moderation, gameplay integration, identity, invariants, fitness functions. It chooses **no** database/SQL/storage/blob/CDN/search-engine/API/REST/GraphQL/transport/CMS/moderation-tool/AI-service/framework ([§20 Non-Goals](#20-non-goals)). |

---

## 1. Executive Summary

The **Content Platform** is a **new bounded context** that owns everything about *authoring* word content, and
it is **strictly upstream** of gameplay. Its unit of value is an **owned, typed Dictionary** that produces
**immutable, uniquely-identified published Versions**. Gameplay consumes content through **exactly one narrow
contract** — the existing [ADR-008](ADR-008-dictionary-content-architecture.md) rule that a room **pins one
published Version identity** at match start — and through **nothing else**.

The chosen model is **Owner-Scoped, Typed, Versioned-Immutable Content with a Single Mutable Draft, published
through a lifecycle gate, consumed only by version pinning**:

- **One aggregate = one Dictionary.** A Dictionary has **exactly one owner**, **exactly one mutable Draft**, and
  a history of **immutable Versions**. It is the consistency boundary ([ADR-001/002](ADR-002-authoritative-game-state.md))
  for all authoring.
- **Authoring is isolated from consumption.** Creating/editing/publishing/sharing content is an **authenticated**
  activity in the Content Platform; **consuming** a published Version in a room is **account-free**
  ([ADR-009 identity seam](ADR-009-participant-lifecycle-presence-session-continuity.md)) and unchanged from the MVP.
- **Drafts are mutable; Versions are immutable.** Editing published content is impossible — a correction is a
  **new Version** (never an in-place edit).
- **Rooms pin Versions.** A running/completed match references a **pinned Version identity**; no content action
  ever reaches into it.
- **Clones are independent.** A clone is a **new owned Dictionary** seeded from a source Version's words plus a
  provenance reference — never a shared mutable link to the source.
- **Moderation changes lifecycle, not words.** Review/block/retire act on **discoverability/selectability
  state**; published words are never edited and no match is ever touched.
- **One model, all types.** Official / user / organization / community / educational / premium / AI content are
  the **same aggregate** differing only in **who owns** and **who may see**; all obey one lifecycle,
  immutability, versioning, and pinning.

> One-line statement: **an owner-scoped, typed Dictionary aggregate with one mutable Draft and immutable
> published Versions; authoring is authenticated and isolated upstream; gameplay consumes content only by
> pinning a Version identity; clones are independent; moderation acts on lifecycle, never on words — so openness
> is added without touching a single gameplay guarantee.**

---

## 2. Problem Statement

The MVP dictionary is **closed**: a Content-Localization team owns one Country Dictionary per region
([DM-C1/O1/O3](../../02-business-analysis/13-dictionary-management.md)) and players may only *select* a region.
The Content Platform opens authoring to **every authenticated user** (and later organizations, community,
premium, AI). This raises architectural questions that the model — not an admin tool — must answer:

- **Ownership:** who may change a given piece of content, and can ownership ever be ambiguous or duplicated?
- **Isolation of authoring from play:** can opening *authoring* accidentally require accounts to *play*, or let
  an edit reach a live match?
- **Mutability boundary:** what is editable, what is frozen, and where exactly is the line?
- **Reproducibility:** can a past match's word source always be reproduced despite arbitrary later edits?
- **Containment of untrusted content:** user/community/AI words include offensive, infringing, and low-quality
  material — can the model contain that by construction rather than by trust?
- **Extensibility:** can organization/premium/marketplace/AI content be added **without redesign**?

These are **architectural** because they fix ownership, immutability, isolation, determinism, and evolvability —
properties that must be **guaranteed**, not left to a UI. ADR-008 already chose the *content* model (versioned,
immutable, pinned) and explicitly named this expansion as a **future governed addition** ([ADR-008 §16/§21](ADR-008-dictionary-content-architecture.md#16-security-analysis));
ADR-011 makes the **ownership/authoring/sharing** decisions that realize it.

---

## 3. Content Platform Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Authoring ≠ Consumption** | Producing content is an authenticated, upstream activity; consuming it is account-free play. | Protects C-2/BO-2 while opening authoring ([§7](#7-gameplay-integration-the-single-seam)). |
| **One Owner** | Every Dictionary has exactly one owning identity. | Unambiguous authority over change ([AI-CP-1](#12-architectural-invariants-ai-cp-)). |
| **One Draft, Many Versions** | Exactly one mutable Draft; all published Versions immutable. | A single, clear mutability boundary ([AI-CP-3/4](#12-architectural-invariants-ai-cp-)). |
| **Correct by New Version** | Corrections publish a new Version; nothing is edited in place. | Reproducibility & safe evolution (ADR-008 CM8). |
| **Rooms Reference, Never Own** | A match holds a pinned Version identity, not editable content. | Immutability during play (ADR-008 §8). |
| **Clones Are Independent** | A clone is new owned content seeded from a Version, not a live link. | No shared mutable state ([AI-CP-8](#12-architectural-invariants-ai-cp-)). |
| **Moderation Acts on Lifecycle** | Review/block/retire change state; they never edit words or matches. | Safety without rewriting history (ADR-008 §11). |
| **One Model, All Types** | Ownership/visibility vary; lifecycle/immutability/versioning/pinning do not. | Extensibility without redesign ([§10](#10-future-extensibility)). |
| **Explicit Visibility** | Private by default; exposure is deliberate and enforced. | Private content never leaks ([AI-CP-6](#12-architectural-invariants-ai-cp-)). |
| **Content Selects Words Only** | Content never affects rules, counts, flow, or outcomes. | Determinism/fairness ([INV-D1](../../02-business-analysis/10-business-invariants.md)). |

---

## 4. Candidate Models & Alternatives

Evaluated against ownership clarity, immutability, determinism, isolation, safety, and extensibility. None
dismissed without reasoning. (These extend, and stay consistent with, ADR-008's CM1–CM10 content-model choice.)

### Alternative A — Mutable dictionaries *(REJECTED)*
- **Overview:** Content is edited in place; "the dictionary" is a single evolving list.
- **Why rejected:** An in-place edit could change a **running match's** word pool or break **reproducibility**
  of a past match ([INV-D3](../../02-business-analysis/10-business-invariants.md), ADR-008 CM9). Sharing a mutable
  list creates **shared mutable state** across rooms ([AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md),
  [ADR-007](ADR-007-room-isolation-distribution.md)). It makes moderation destructive (editing history) and makes
  clone semantics ambiguous. **Disqualified** — it violates the non-negotiable immutability/determinism core.

### Alternative B — Versioned immutable dictionaries with one mutable Draft *(ACCEPTED)*
- **Overview:** A Dictionary owns one mutable **Draft** and a history of **immutable Versions**; publishing
  snapshots the Draft into a new Version; matches pin a Version.
- **Why accepted:** It is the **only** model that simultaneously delivers editability (the Draft), immutability
  (Versions), reproducibility (pinned identities), safe evolution (new Version), non-destructive moderation
  (lifecycle state), and independent clones. It is a **direct extension of ADR-008** (CM7+CM8) with an ownership
  and Draft boundary added. **This is the decision** ([§5](#5-the-content-platform-model)).

### Alternative C — Shared collaborative editing (multi-writer live Draft) *(REJECTED for this capability)*
- **Overview:** Multiple accounts edit the same Draft concurrently in real time.
- **Why rejected now:** It introduces **multi-writer concurrency** on the authoring aggregate — a second hard
  problem (conflict resolution, presence, merge) orthogonal to the content guarantees — with **no** requirement
  in the Feature Spec. The **single-owner, single-Draft** aggregate keeps authoring a **single-writer**
  consistency boundary ([ADR-001/002](ADR-002-authoritative-game-state.md)), mirroring the room-Authority model.
  Collaboration is **reserved additively** (Editor role, [Feature Spec §24](../../15-content-platform/02-feature-specification.md#24-future-collaboration-deferred))
  and can be added later **without** changing the aggregate boundary. **Deferred, not disqualified.**

### Alternative D — Gameplay references mutable dictionaries *(REJECTED)*
- **Overview:** A room points at "the dictionary," resolving current words at read time.
- **Why rejected:** A late edit or new publish would change the running match — the exact failure ADR-008 pinning
  exists to prevent ([AI-CONTENT-3/4/7](ADR-008-dictionary-content-architecture.md#14-architectural-invariants-ai-content-)).
  It also couples every room reading the same content into **shared mutable state**. **Disqualified.**

### Alternative E — Gameplay copies the words (no version reference at all) *(EVALUATED)*
- **Overview:** At match start the room copies the word list into the board and keeps **no** Version identity.
- **Trade-offs:** The board **already** stores its drawn 25 words as immutable board state (ADR-002/008 §13), so
  *the running match is safe either way*. But dropping the **Version identity** loses **auditability and
  historical reproducibility** — you could no longer prove *which* Version a past match used, reconcile a match
  to a retired/blocked Version, or attribute provenance. It also weakens recovery/migration audit (ADR-005/007).
- **Verdict:** **Partially adopted, refined.** Cluely keeps the ADR-008 rule: the room stores the **drawn board
  words as immutable board state** *and* the **pinned Version identity** (identity, not the full list). This
  gives copy-level safety **plus** reference-level auditability — the best of both. Storing the *full editable
  word list* in the room is rejected (that would be content living in gameplay).

### Evaluation summary
| Criterion | A Mutable | **B Versioned+Draft (chosen)** | C Collaborative | D Ref-mutable | E Copy-only |
|-----------|:---------:|:------------------------------:|:---------------:|:-------------:|:-----------:|
| Ownership clarity | 3 | **5** | 2 | 3 | 4 |
| Immutability/determinism | 1 | **5** | 3 | 1 | 4 |
| Reproducibility/audit | 1 | **5** | 3 | 1 | 3 |
| Isolation (no coupling) | 1 | **5** | 2 | 1 | 5 |
| Safety/moderation | 2 | **5** | 3 | 2 | 3 |
| Extensibility | 3 | **5** | 4 | 2 | 3 |
| Complexity (lower=better) | 4 | **4** | 1 | 4 | 5 |

---

## 5. The Content Platform Model

**Adopt Alternative B**, refined by E's identity+board-state rule:

- A **Dictionary** aggregate = **one owner** + **one mutable Draft** + **immutable Version history** + typed
  metadata + visibility + share grants.
- **Publishing** validates the Draft and snapshots it into a new **immutable Version** with a globally-unique
  identity; the Draft persists for further editing.
- **Gameplay** resolves a selected Dictionary to its **current published Version** and **pins that Version's
  identity** at match start; the board draws its 25 words **once** into immutable board state (ADR-008 §8/§13).
- **Clones** create a new owned Dictionary seeded from a Version's words + provenance; **independent** thereafter.
- **Moderation** acts on the **lifecycle** (review/block/retire); it never edits words or a pinned match.
- **All content types** (official/user/org/community/premium/educational/AI) are the **same aggregate**,
  differing only in **ownership** and **visibility**.

This sits entirely **upstream** of gameplay and integrates through the **single ADR-008 pinning seam** ([§7](#7-gameplay-integration-the-single-seam)).

---

## 6. Structural Definitions

### 6.1 Bounded Context
The **Content Platform** is a bounded context **separate from Gameplay**. Its ubiquitous language (Dictionary,
Draft, Version, Owner, Visibility, Share, Clone, Publish, Review, Moderate) is distinct from Gameplay's (Room,
Match, Board, Turn, Participant). The two contexts meet **only** at the pinning seam; they share **no** mutable
model. *(Why its own context, [§8](#8-why-a-separate-bounded-context).)*

### 6.2 Aggregates
| Aggregate | Root | Owns | Consistency boundary |
|-----------|------|------|----------------------|
| **Dictionary** | Dictionary | Owner reference, metadata, visibility, **one Draft**, **Version history**, share grants. | All authoring/publishing/visibility/sharing for that dictionary is transactionally consistent within it (single-writer = the owner). |
| **Version** *(within Dictionary)* | — | Immutable word set + immutable Version metadata + lifecycle state. | Immutable once published; identity globally unique. Referenced by matches **by identity**, never embedded in the room aggregate as editable content. |

- The **Dictionary is the authoring consistency boundary** ([ADR-001/002](ADR-002-authoritative-game-state.md)):
  one owner writes; there is no cross-dictionary transaction.
- A **Version is not owned by any Room**; a Room holds only its **identity** (+ drawn board words as board
  state) — [ADR-007/008](ADR-007-room-isolation-distribution.md).

### 6.3 Ownership model
Exactly one **owner** (an [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md) durable
identity) per Dictionary; the owner holds all authoring/visibility/sharing/lifecycle rights. **Viewer** access
is granted by share or Public visibility (read/select/export/clone). **Editor** and ownership **transfer** are
**reserved** (deferred, [Feature Spec §6/§24](../../15-content-platform/02-feature-specification.md#6-ownership)).
**Official** content is owned/managed by the platform/Content-Localization function, not user authoring
([Feature Spec §23](../../15-content-platform/02-feature-specification.md#23-official-content-boundary)).

### 6.4 Dictionary & Version lifecycles
Per [Feature Spec §5](../../15-content-platform/02-feature-specification.md#5-lifecycle):
Version — `Draft → Validated → Published → (Deprecated) → Archived → Retired`; Dictionary container —
`Active → Archived → PendingDeletion → Deleted` (soft-delete, retention-bounded, [Feature Spec §22](../../15-content-platform/02-feature-specification.md#22-soft-delete-lifecycle)).

### 6.5 Visibility, Sharing, Clone, Publication, Review, Moderation models
| Model | Architectural definition |
|-------|--------------------------|
| **Visibility** | Private (owner) / Shared (owner + grantees) / Public (all; review-gated) / Organization (future). Default Private. Changes affect **future** selection only, never pinned matches. |
| **Sharing** | Owner grants **Viewer** access to specific identities; revocable; grants **read/select** of the current Version, never edit/Draft/re-share. |
| **Clone** | New owned Dictionary seeded from a source Version's words + provenance; **independent**; source immutability preserved. |
| **Publication** | Validate → snapshot immutable Version → advance current-version pointer. Draft persists. |
| **Review** | **Public-only** gate: `Published → Pending Review → Discoverable` (reject → not discoverable). Acts on discoverability, not words. |
| **Moderation** | Report → block (remove from new selection/discovery) → retire (terminal). Lifecycle-only; never edits words; never touches a pinned match. |

### 6.6 Identity model
Authoring authorization uses the **durable account identity** from [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)/[Roadmap
Phase 2](../../03-business-governance/06-product-roadmap.md#5-phase-2--accounts--continuity-future). **Consumption
of a published Version requires no identity** — the room's anonymous participants play it unchanged (the
[ADR-009 "participation independent of authentication"](ADR-009-participant-lifecycle-presence-session-continuity.md)
seam). This is the load-bearing split: **authoring binds to an account; playing does not.**

---

## 7. Gameplay Integration — the Single Seam

The Content Platform touches gameplay through **one** contract and no other:

1. **Selection (upstream, a Command).** A host selects a Dictionary from permitted content; the system resolves
   it to the **current published Version** ([Feature Spec FR-070/071](../../15-content-platform/02-feature-specification.md#3-functional-requirements),
   [ADR-010](ADR-010-command-query-strategy.md): selection/resolution is part of the coordinated Start command).
2. **Pinning (the seam).** At match start the room **pins the Version identity** into its authoritative aggregate
   and draws the board **once** into immutable board state — the **existing** [ADR-008 §8](ADR-008-dictionary-content-architecture.md#8-match-pinning-critical) rule, unchanged.
3. **No other coupling.** The Content Platform has **no write path** into any Room; a Room has **no write path**
   into any Dictionary. Content reads (browse/export) are **Queries** ([ADR-010](ADR-010-command-query-strategy.md))
   over immutable content. *Why authoring is isolated from consumption, [§9](#9-why-authoring-is-isolated-from-consumption).*

---

## 8. Why a Separate Bounded Context

- **Different invariants.** Gameplay's invariants are about turns, hidden information, and determinism; the
  Content Platform's are about ownership, immutability, and visibility. Fusing them would entangle two
  independent rule sets and risk content concerns leaking into rules ([INV-D1](../../02-business-analysis/10-business-invariants.md)).
- **Different lifecycle & cadence.** Content evolves offline over days; a match lives minutes. A shared model
  would couple two very different change rates.
- **Different actors & identity.** Authoring needs **durable accounts**; play needs **none**. A separate context
  lets the identity seam sit exactly on the boundary ([§6.6](#66-identity-model)).
- **Isolation & distribution.** ADR-007 wants content **shared, read-only, non-coupling**; a distinct context
  makes that structural — content is produced in one place, referenced (never owned) by rooms.
- **Extensibility.** A dedicated context is where org/premium/marketplace/AI capabilities accrue **without**
  reopening gameplay ([§10](#10-future-extensibility)).

**Why Dictionaries are not part of Gameplay:** because content is **shared, read-only, independently evolvable,
and owned outside any room** — a room holds only a **pinned Version identity** and drawn board words, never
editable content. Putting content inside gameplay would create shared mutable state, couple rooms, and let
content changes threaten running matches — every failure the architecture forbids.

---

## 9. Why Authoring Is Isolated From Consumption

- **Protects zero-signup play.** If authoring and consumption shared a surface, requiring an account to author
  could bleed into requiring one to play — breaking C-2/BO-2. The split keeps *playing* account-free
  ([AI-CP-2](#12-architectural-invariants-ai-cp-), [ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)).
- **Protects running matches.** Consumption is **read-only by pinned identity**; authoring is **write, upstream,
  before any match**. With no shared write path, no edit can reach a live match ([AI-CP-9](#12-architectural-invariants-ai-cp-)).
- **Enables independent scaling & caching.** Read-only, immutable content is freely replicable/cacheable by
  identity (ADR-008 §12); authoring is a low-rate write path. Isolation lets each evolve independently.

**Why content is read-only during gameplay:** because a match's fairness/determinism depend on a **fixed input**;
the pinned immutable Version *is* that fixed input; any runtime write would reintroduce the very mutability the
model exists to remove. **Why Drafts are mutable while Versions are immutable:** editing must live somewhere (the
Draft) but *play and history must be reproducible* (the Versions) — separating them gives both editability and
immutability with a single, unambiguous boundary. **Why moderation changes lifecycle instead of words:** editing
published words would mutate history and possibly a referenced Version; changing **state** (block/retire) removes
content from *new* use while leaving every past match exactly reproducible.

---

## 10. Future Extensibility

All are the **same Dictionary aggregate**, differing only in ownership/visibility; each obeys one lifecycle,
immutability, versioning, and pinning — **added without redesign** (ADR-008 §21):

| Capability | Added via | Touches gameplay? |
|-----------|-----------|-------------------|
| **Organization content** | Owner = org identity; Organization visibility. | No |
| **Educational content** | User/org content, curriculum-tagged. | No |
| **Community/public catalog** | Public visibility + review gate. | No |
| **Premium / marketplace** | Entitlement/visibility metadata over the same aggregate (mechanics = separate future ADR). | No |
| **Seasonal / tournament / localization packs** | Typed/official content, composed per ADR-008 §10. | No |
| **AI-generated content** | System authors on an owner's behalf; enters the **same** validation + review gate; immutable once published. | No |

The extensibility test for any future capability: *does it change the aggregate boundary, the pinning seam, or
an invariant?* If yes, it needs a superseding ADR; if no (the common case), it is **additive metadata/visibility**.

---

## 11. Isolation, Determinism & Security

| Concern | Architectural guarantee | Future technical control (Non-Goal) |
|---------|-------------------------|-------------------------------------|
| **Private-content leak** | Explicit visibility; Private never enters discovery/share/selection for non-owners ([AI-CP-6](#12-architectural-invariants-ai-cp-)). | Access-controlled queries (Tech Design). |
| **Edit reaches a live match** | No write path Content→Room; pinning + immutability ([AI-CP-9](#12-architectural-invariants-ai-cp-), ADR-008). | — |
| **Room mutates content** | No write path Room→Dictionary ([AI-CP-10](#12-architectural-invariants-ai-cp-)). | — |
| **Room coupling via shared content** | Content read-only, identity-addressed; sharing read-only ≠ coupling ([ADR-007](ADR-007-room-isolation-distribution.md), [AI-CP-11](#12-architectural-invariants-ai-cp-)). | — |
| **Untrusted (offensive/infringing) content** | Public passes review before discoverable; report→block→retire; corrections = new Version ([§6.5](#65-visibility-sharing-clone-publication-review-moderation-models)). | Moderation/AI tooling, content signing (Tech Design). |
| **Ownership spoofing / duplicate ownership** | Exactly one owner per Dictionary ([AI-CP-1](#12-architectural-invariants-ai-cp-)); authoring bound to durable identity (ADR-009). | AuthZ enforcement (Tech Design). |
| **Version confusion / poisoning** | Globally-unique immutable Version identity; published words never change ([AI-CP-4/5](#12-architectural-invariants-ai-cp-), ADR-008 AI-CONTENT-9/14). | Integrity/signing (Tech Design). |
| **Determinism** | Same pinned Version ⇒ same words; board drawn once into board state ([AI-CP-12](#12-architectural-invariants-ai-cp-), ADR-008 FF-9). | — |
| **Hidden-information leak via content** | Content is words only; the key derives at generation and lives server-side ([ADR-006](ADR-006-role-based-information-visibility.md)); content changes touch only future matches. | — |

---

## 12. Architectural Invariants (AI-CP-*)

*These **extend** ADR-008's `AI-CONTENT-*` (which remain in force); they do not redefine them.*

- **AI-CP-1:** A Dictionary has **exactly one owner** — never null, ambiguous, or duplicated.
- **AI-CP-2:** **Authoring requires authentication; consuming a published Version does not.**
- **AI-CP-3:** A Dictionary has **at most one mutable Draft**; all other content is immutable Versions.
- **AI-CP-4:** **Published Versions are immutable** — corrections are new Versions (never in-place edits).
- **AI-CP-5:** Every Version identity is **globally unique and immutable**.
- **AI-CP-6:** **Private content never leaks** — visibility is explicit and enforced for every read/select surface.
- **AI-CP-7:** Visibility/share changes affect **future** selection only — **never** a pinned match.
- **AI-CP-8:** **Clones are independent** — new owned Dictionaries seeded from a Version + provenance, with no live link to the source.
- **AI-CP-9:** **Gameplay never edits Content** and **Content never edits Gameplay** — there is no cross-context write path.
- **AI-CP-10:** A **Room references a Version by identity**; it never embeds an editable Dictionary/Draft.
- **AI-CP-11:** Content is **shared read-only** and **never couples rooms**.
- **AI-CP-12:** **A pinned Version never changes** for the life of a match ⇒ deterministic word source.
- **AI-CP-13:** **Moderation acts on lifecycle** (review/block/retire) — never on published words, never on a match.
- **AI-CP-14:** **Official and user content share the same lifecycle, immutability, versioning, and pinning** — differing only in ownership and visibility.
- **AI-CP-15:** **Deletion is logical and retention-bounded** — a Version referenced by a live/reproducible match is never physically removed.

---

## 13. Architecture Fitness Functions (FF-CP-*)

*Measurable, testable rules. They **extend** ADR-008's `FF-CONTENT-*`.*

- **FF-CP-001:** **No aggregate outside the Content Platform may mutate a Dictionary** (no write path Room→Dictionary).
- **FF-CP-002:** **No running Room references a Draft** — every match reference is a **published** Version identity.
- **FF-CP-003:** **Every Dictionary has exactly one owner** at all times (ownership-cardinality check).
- **FF-CP-004:** **Every Dictionary has ≤ 1 Draft** (draft-cardinality check).
- **FF-CP-005:** **No published Version's words change** after publication (immutability check).
- **FF-CP-006:** **A pinned Version identity is unchanged** across a match's life (== ADR-008 FF-2 at platform scope).
- **FF-CP-007:** **Private/Shared content never appears** to an unauthorized actor in any discovery/select/share result.
- **FF-CP-008:** **A clone shares no mutable state** with its source (editing/deleting either never affects the other).
- **FF-CP-009:** **Public content is discoverable only after review approval** (no Public-catalog entry lacks an approved Version).
- **FF-CP-010:** **Moderation actions change only lifecycle state** — never word content, never a match.
- **FF-CP-011:** **A referenced Version is never physically deleted** (retention check; complements ADR-008 §24.8).
- **FF-CP-012:** **No authoring action is reachable without an authenticated identity**, and **no consumption path requires one**.
- **FF-CP-013:** **Every match records exactly one Version identity** and reproduces identical words from it (== ADR-008 FF-1/7 at platform scope).
- **FF-CP-014:** **Official and user Dictionaries traverse the identical lifecycle state machine** (single-state-machine check).

Map to [Success Metrics ASM-01/02](../../06-architecture-governance/04-architecture-success-metrics.md),
[QS-12/13](../09-quality-attribute-scenarios.md), and Vision guardrail metrics [SM-11/12/13](../../15-content-platform/01-business-vision.md#11-success-metrics).

---

## 14. Failure Scenarios

| # | Scenario | Architectural outcome | Protection |
|---|----------|-----------------------|-----------|
| 1 | **Publishing failure** (validation fails mid-publish) | No Version is created; the Draft is unchanged; the current-version pointer does not move. | Publish = validate-then-snapshot atomically within the Dictionary aggregate (§6.2); Version creation is all-or-nothing. |
| 2 | **Review rejection** (Public) | The immutable Version still exists and remains usable **privately/shared**; it is simply **not discoverable**; owner may revise via a **new** Version. | Review acts on discoverability, not words ([AI-CP-13](#12-architectural-invariants-ai-cp-)); rejection edits/deletes nothing. |
| 3 | **Deletion while referenced** | Deletion is **deferred**; the Dictionary enters Hidden/Retention; physical removal waits until no Version is referenced by a live/reproducible match. | [AI-CP-15](#12-architectural-invariants-ai-cp-), FF-CP-011; board words persist as board state regardless (ADR-008 §13). |
| 4 | **Visibility change mid-use** (Public→Private while rooms use it) | Future discovery/selection stops immediately; **already-pinned matches are unaffected**. | [AI-CP-7](#12-architectural-invariants-ai-cp-); pinning holds the Version identity + board state. |
| 5 | **Clone during source publication** | The clone seeds from a **specific, already-published Version** (an immutable snapshot); a concurrent new publish on the source is irrelevant to the clone. | Clones bind to a Version identity, not to "the latest" mutable state ([AI-CP-8](#12-architectural-invariants-ai-cp-)). |
| 6 | **Concurrent edits** (owner edits Draft from two sessions) | Single-owner, single-Draft aggregate serializes writes; last-writer-wins on the Draft is safe because the Draft is not authoritative for any match. | Single-writer authoring boundary ([§6.2](#62-aggregates), Alternative C rationale); no match depends on the Draft. |
| 7 | **Draft corruption** (invalid/partial words accumulate) | Publishing is **blocked** by validation (§9); the corrupt Draft can never become a Version; owner may discard to the last Version. | Validation gate (FF-CP-005 precondition); Draft is never a play source ([FF-CP-002](#13-architecture-fitness-functions-ff-cp-)). |
| 8 | **Historical replay** (reproduce a past match) | The pinned Version identity + immutable board reproduce the **identical** word source, regardless of later publishes/blocks/retires/deletes-attempts. | [AI-CP-12](#12-architectural-invariants-ai-cp-), FF-CP-013; ADR-008 §13 FF-3/7. |
| 9 | **Recovery** (node/room failure mid-match) | The recovered aggregate carries the pinned Version identity + immutable board; the same words reproduce; no content reload is required to continue. | [ADR-005](ADR-005-state-recovery-resilience.md), ADR-008 §13; content is not room-owned. |
| 10 | **Moderation on a live/completed match's Version** | Block/retire remove it from **new** selection/discovery; the running/completed match keeps its pinned Version and finishes/records normally. | [AI-CP-13](#12-architectural-invariants-ai-cp-), FF-CP-010; ADR-008 AI-CONTENT-10. |

---

## 15. ADR Compliance

| ADR | Compatibility statement |
|-----|-------------------------|
| **[ADR-000](ADR-000-architecture-vocabulary.md)** | Introduces Content-Platform terms (Dictionary/Draft/Version/Owner/Visibility/Share/Clone/Publish/Review) that **extend**, and do not redefine, existing vocabulary. *(Per ADR-000's rule, these terms should be folded into ADR-000 on acceptance — [§21](#21-impact-on-adr-000-and-downstream).)* No contradiction. |
| **[ADR-002](ADR-002-authoritative-game-state.md)** | The room's authoritative state still holds the **pinned Version identity** + immutable board; content is external read-only input. No new authoritative game state. No contradiction. |
| **[ADR-003](ADR-003-per-room-coordination-model.md)** | Content never influences per-room coordination; region/dictionary selection is a coordinated Start-time choice, as before. No contradiction. |
| **[ADR-004](ADR-004-real-time-communication-delivery.md)** | Content is never delivered as authoritative truth; no content path changes event delivery/versioning. No contradiction. |
| **[ADR-005](ADR-005-state-recovery-resilience.md)** | Recovery preserves the pinned Version identity + board; retention protects referenced Versions ([AI-CP-15](#12-architectural-invariants-ai-cp-)). No contradiction. |
| **[ADR-006](ADR-006-role-based-information-visibility.md)** | Content is words only; the key derives at generation and stays server-side; content never leaks hidden info. No contradiction. |
| **[ADR-007](ADR-007-room-isolation-distribution.md)** | Content is shared read-only, identity-addressed, room-owned by none, coupling no rooms ([AI-CP-10/11](#12-architectural-invariants-ai-cp-)). No contradiction. |
| **[ADR-008](ADR-008-dictionary-content-architecture.md)** | **Directly extends** it: realizes the §16/§21 user/community-content seam; keeps versioned/immutable/pinned/read-only content and every `AI-CONTENT-*`/`FF-CONTENT-*`. Recasts "one Country Dictionary per region" as **official-type-only** (DM-C1), consistent with ADR-008 §6/§10. No contradiction. |
| **[ADR-009](ADR-009-participant-lifecycle-presence-session-continuity.md)** | Uses the durable-identity seam for **authoring**; preserves "participation independent of authentication" for **play**. No contradiction. |
| **[ADR-010](ADR-010-command-query-strategy.md)** | Authoring actions are **Commands**; browse/export/stats are **Queries**; selection/pinning is part of the Start Command; no read becomes authoritative. No contradiction. |

**Business compatibility:** consistent with the frozen [Dictionary Management](../../02-business-analysis/13-dictionary-management.md)
(official lane unchanged), [Invariants INV-D1/D2/D3](../../02-business-analysis/10-business-invariants.md), and
Roadmap [G-1/G-2/G-5](../../03-business-governance/06-product-roadmap.md#10-guardrails).

---

## 16. Trade-off Analysis

- **Fairness/determinism:** Maximized — immutable Versions, pinned per match, rule-neutral (unchanged from ADR-008).
- **Openness vs safety:** Openness is **contained**, not traded — review + immutability + pinning bound untrusted content.
- **Editability vs immutability:** Resolved cleanly by the **Draft/Version** split (one boundary, no ambiguity).
- **Simplicity:** Single-owner/single-Draft keeps authoring a **single-writer** boundary; concurrency (collaboration) is deferred, not incurred now.
- **Extensibility:** High — org/premium/marketplace/AI slot in as ownership/visibility metadata over one aggregate.
- **Auditability:** High — Version identities + provenance make history and lineage reproducible.
- **Cost:** Moderation load and (future) tooling for public content — accepted, named as future controls.

---

## 17. Risks

| Risk | Type | Mitigation |
|------|------|-----------|
| Offensive/infringing user content reaches players | Safety/Moderation | Public review gate; report→block→retire; corrections = new Version; SLA metric [SM-10](../../15-content-platform/01-business-vision.md#11-success-metrics). |
| Auth seam misread → sign-up to play | Product (critical) | [AI-CP-2](#12-architectural-invariants-ai-cp-), FF-CP-012; guardrail metric SM-13. |
| An edit path leaks into a live match | Fairness (critical) | No cross-context write path ([AI-CP-9](#12-architectural-invariants-ai-cp-)); FF-CP-001/002; pinning. |
| Private content leaks via share/clone/browse | Privacy | Explicit visibility ([AI-CP-6](#12-architectural-invariants-ai-cp-)); FF-CP-007; guardrail metric SM-12. |
| Deletion removes a referenced Version | Reproducibility | Retention-bounded soft delete ([AI-CP-15](#12-architectural-invariants-ai-cp-)); FF-CP-011. |
| Moderation load outpaces capacity | Operational | Lifecycle designed for scale; tooling as fast-follow (Non-Goal); Private-by-default limits blast radius. |
| Scope creep toward marketplace/AI now | Delivery | Strict Non-Goals ([§20](#20-non-goals)); each future capability passes its own governance (G-2). |
| Ownership ambiguity on transfer (future) | Ownership | Single-owner invariant ([AI-CP-1](#12-architectural-invariants-ai-cp-)); transfer deferred behind its own design. |

---

## 18. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| CP-AS-1 | **Durable accounts exist** (Phase 2) to own content. | High | Capability is blocked until accounts land; the model is unchanged. |
| CP-AS-2 | **Single-owner, single-Draft authoring suffices** for this capability. | High | If real-time collaboration is required, add the Editor role/model additively (Alt C) without changing the aggregate. |
| CP-AS-3 | **Consuming a Version stays account-free.** | Very High | If play required auth, C-2/BO-2 break — such a design is rejected ([AI-CP-2](#12-architectural-invariants-ai-cp-)). |
| CP-AS-4 | **The ADR-008 content model holds for user/community/AI content.** | Very High | ADR-008 §16/§21 designed for exactly this; if not, a new content ADR would be needed. |
| CP-AS-5 | **A single pinned Version per match still suffices** (no mid-match blending). | Fact (ADR-008 AS-4) | — |
| CP-AS-6 | **`DICTIONARY_MIN_WORDS`/uniqueness apply to all types**; two new operational constants (`DICTIONARY_MAX_WORDS`, per-word bounds) will be ratified. | High | Defaults set in Technical Design + Constants Catalog; no gameplay impact. |

---

## 19. Adversarial Architecture Review — "Attempt to Break the Content Platform"

1. **Can a user edit a published Version's words?** *No* — Versions immutable; only new Versions ([AI-CP-4](#12-architectural-invariants-ai-cp-), FF-CP-005).
2. **Can authoring force players to sign in?** *No* — consumption path requires no identity ([AI-CP-2](#12-architectural-invariants-ai-cp-), FF-CP-012).
3. **Can a content edit change a running match?** *No* — no cross-context write path; pinning + board state ([AI-CP-9/12](#12-architectural-invariants-ai-cp-), FF-CP-001/002).
4. **Can a room mutate a Dictionary?** *No* — Room→Dictionary write path does not exist (FF-CP-001).
5. **Can a clone corrupt or leak its source?** *No* — clones are independent, seeded from an immutable Version ([AI-CP-8](#12-architectural-invariants-ai-cp-), FF-CP-008).
6. **Can Private content appear to others?** *No* — explicit visibility ([AI-CP-6](#12-architectural-invariants-ai-cp-), FF-CP-007).
7. **Can Public content skip moderation?** *No* — discoverable only after review approval (FF-CP-009).
8. **Can moderation rewrite history or a match?** *No* — lifecycle-only ([AI-CP-13](#12-architectural-invariants-ai-cp-), FF-CP-010).
9. **Can deleting a Dictionary break a past match?** *No* — referenced Versions retained; board words persist ([AI-CP-15](#12-architectural-invariants-ai-cp-), FF-CP-011).
10. **Can two owners claim one Dictionary?** *No* — exactly one owner ([AI-CP-1](#12-architectural-invariants-ai-cp-), FF-CP-003).
11. **Can a Draft be used in a match?** *No* — matches pin **published** Versions only (FF-CP-002).
12. **Can official and user content diverge in lifecycle and create special-case bugs?** *No* — one state machine for all types ([AI-CP-14](#12-architectural-invariants-ai-cp-), FF-CP-014).
13. **Can AI/community content bypass the gate?** *No* — same validation + review + immutability + pinning ([§10](#10-future-extensibility), ADR-008 §16).
14. **Can content leak the key / hidden info?** *No* — content is words only; key stays server-side ([ADR-006](ADR-006-role-based-information-visibility.md)).

**Conclusion:** the Content Platform **cannot change a running/completed match, cannot require auth to play,
cannot leak private content, cannot couple rooms, cannot lose reproducibility, and cannot be edited after
publication** — **by construction** — because content is **owner-scoped, single-Draft, versioned-immutable,
review-gated, read-only at runtime, and pinned per match**, with moderation acting only on lifecycle. The only
genuine residuals — **moderation quality/cost, authoring access-control enforcement, and referenced-Version
retention** — are named **future technical/operational controls**, not architectural weaknesses.

---

## 20. Non-Goals

This ADR decides **no**: database, storage, blob/CDN, search engine, API/REST/GraphQL, transport, CMS,
moderation/AI tooling, content encoding/import-export format, authentication mechanism, marketplace/monetization
mechanics, or organization-administration model. It defines **only** the Content Platform architecture (context,
aggregates, ownership, lifecycle, versioning, visibility, sharing, cloning, publication/review, moderation,
gameplay integration, identity, invariants, fitness functions). Those belong to **Technical Design** and their
own future ADRs.

---

## 21. Impact on ADR-000 and Downstream

| Target | Constraint imposed by ADR-011 |
|--------|-------------------------------|
| **ADR-000** | Fold in Content-Platform vocabulary (Dictionary, Draft, Version, Owner, Visibility, Share, Clone, Publish, Review, Moderate, Content Type) as canonical terms on acceptance. |
| **Software Design (06)** | Introduce the **Content Platform** module with the Dictionary aggregate, Draft/Version model, ownership/visibility/share/clone policies, publication/review/moderation services, and the read-only provider that gameplay consumes by Version identity. |
| **Technical Design (07)** | Realize immutable Version storage, retention of referenced Versions, search over the business fields (§20 Search Metadata), import/export encoding, and access control — **conforming to** every `AI-CP-*`/`FF-CP-*`. |
| **Constants Catalog** | Ratify `DICTIONARY_MAX_WORDS` and per-word length bounds as operational parameters. |
| **Operations/Data Lifecycle** | Retention of referenced Versions; moderation SLA; soft-delete completion when unreferenced. |
| **Future ADRs (org/premium/marketplace/AI)** | All must conform to this model — additive ownership/visibility over one aggregate; any change to the aggregate boundary, pinning seam, or an invariant requires a superseding ADR. |

---

## 22. Architecture Review

- **Decision:** **Owner-scoped, typed Dictionary aggregate with one mutable Draft and immutable published
  Versions; authoring authenticated and isolated upstream; gameplay consumes content only by pinning a Version
  identity; clones independent; moderation on lifecycle only; one model for all content types.**
- **Confidence:** **High** — it is the direct realization of ADR-008's anticipated seam, entailed by the
  immutability/determinism invariants plus the ownership need that authentication unlocks; alternatives (mutable,
  ref-mutable) are disqualified and collaboration is safely deferred.
- **Remaining risks:** moderation quality/cost; authoring access-control enforcement; referenced-Version
  retention — all named future technical/operational controls.
- **Open questions (delegated, non-blocking):** storage/search/format realization; access-control mechanism;
  organization/premium/marketplace/AI mechanics (future ADRs); exact Editor/transfer model (deferred).
- **Review triggers:** real-time collaboration requirement (revisit Alt C); any proposal to let content affect
  rules (reject — violates INV-D1/AI-CONTENT-8); monetization/marketplace programs (own ADRs conforming here).
- **Readiness for Software Design (06):** **READY** — context, aggregates, ownership, lifecycle, visibility,
  sharing, cloning, publication/review/moderation, gameplay seam, identity, invariants, and fitness functions
  are fixed and testable.

---

## Final Deliverable — Answers

- **Why is the Content Platform its own bounded context?** Different invariants (ownership/immutability/visibility
  vs turns/hidden-info/determinism), different cadence, different identity needs (authoring needs accounts, play
  does not), and isolation/extensibility — fusing them would couple content to rules and threaten running matches.
- **Why are Dictionaries not part of Gameplay?** Content is shared, read-only, independently evolvable, and owned
  outside any room; a room holds only a **pinned Version identity** (+ drawn board words), never editable content.
- **Why is authoring isolated from consumption?** To keep play account-free (C-2/BO-2) and to guarantee no edit
  path can reach a live match — authoring writes upstream; consumption reads a pinned immutable Version.
- **Why are Versions immutable?** So play and history are reproducible and safe to evolve; a correction is a new
  Version, never a mutation.
- **Why are Rooms pinned to Versions?** Pinning converts evolving content into a fixed input, guaranteeing
  fairness, determinism, reproducibility, recovery, and migration (ADR-008 §8).
- **Why are Clones independent?** A clone is new owned content seeded from an immutable Version + provenance —
  never a live link — so no shared mutable state and no source coupling.
- **Why exactly one owner?** So authority over change is unambiguous; ownership is never null or duplicated.
- **Why is content read-only during gameplay?** Because a match's fairness/determinism depend on a fixed input;
  the pinned immutable Version is that input; runtime writes would reintroduce forbidden mutability.
- **Why are Drafts mutable while Versions are immutable?** Editing must live somewhere (the Draft) while play and
  history must be reproducible (the Versions) — one boundary delivers both.
- **Why does moderation change lifecycle instead of words?** Editing published words would mutate history and
  possibly a referenced Version; changing state (block/retire) removes content from *new* use while leaving every
  past match exactly reproducible.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-11 | Initial decision: **Content Platform — owner-scoped, typed Dictionary aggregate with one mutable Draft and immutable published Versions; authoring authenticated and isolated upstream; gameplay consumes by pinning a Version identity; independent clones; lifecycle-only moderation; one model for all content types.** Realizes ADR-008 §16/§21. Includes bounded-context/aggregate/ownership/lifecycle/visibility/sharing/clone/publication/review/moderation/identity models, alternatives A–E, architectural invariants (AI-CP-1..15), fitness functions (FF-CP-001..014), failure scenarios, full ADR-000..010 compliance, security & adversarial review, and impact/readiness. Technology-neutral; no gameplay change. |
