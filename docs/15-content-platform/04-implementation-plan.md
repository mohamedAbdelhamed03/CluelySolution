# Content Platform — 04. Implementation Plan (Phase 01)

| | |
|---|---|
| **Author role** | Principal .NET Engineer — implementation planning |
| **Version** | 1.0 |
| **Status** | Plan — **no code**; architecture frozen, no redesign |
| **Inputs (frozen)** | [01 Vision](01-business-vision.md), [02 Feature Spec v1.1](02-feature-specification.md), [ADR-011](../07-software-architecture/12-decisions/ADR-011-content-platform-ownership-publication-versioning-sharing.md), [03 Pre-Impl Review](03-pre-implementation-architecture-review.md). |
| **Standards** | [Implementation Planning Standard](../10-implementation/implementation-planning-standard.md), [Definition of Done](../10-implementation/definition-of-done.md), [Simplicity Principles](../10-implementation/simplicity-principles.md). |
| **Solution layout (observed)** | `Cluely.Domain` → `Cluely.Application` → `Cluely.Infrastructure` → `Cluely.Api`; tests in `Cluely.UnitTests`, `Cluely.ArchitectureTests`, `Cluely.IntegrationTests`. Vertical slices live in `Cluely.Application/{Area}/{UseCase}/` (`Command`,`Validator`,`Handler`,`Result`); domain aggregates in `Cluely.Domain/{Aggregate}/` (`Entities`,`ValueObjects`,`Events`,`Errors`). |

---

## 0. Planning Ground Rules & Two Load-Bearing Decisions

### 0.1 Slice discipline
Every slice must **compile**, **pass its tests**, be **independently reviewable**, contain **no TODO/fake/temporary
code**, and depend **only on already-merged work**. A slice implements its layer's responsibility **completely**
(no stubs); end-to-end user exposure is deliberately gated to the API slice, so no slice leaves *partial business
behavior within its own scope*.

### 0.2 Decision A — Layering strategy (why mostly horizontal, with vertical seams)
The prompt's suggested breakdown is **layer-oriented** (Domain → Commands → … → Persistence → API). I adopt it,
with one explicit rule that keeps every slice honest: **a consumer is never merged before the abstraction it
depends on.** Concretely — the **repository/query ports live in `Cluely.Application`** and are introduced *with*
the first command that needs them (Slice 02); their **EF implementations + migrations** are consolidated in the
Persistence slice (10); command/query handlers are **unit-tested against mocked ports** in their own slice, and
**end-to-end integration tests** land with Persistence (10) and API (09). This satisfies "compiles + passes
tests + no dependency on unfinished work" for every slice while matching the requested structure.

> **Deviation noted:** I do **not** bundle persistence *inside* each command slice (a pure vertical cut), because
> the frozen plan and the existing codebase (ports in Application, EF in Infrastructure, custody pattern) favor
> the port-first horizontal seam. This is a sequencing choice, not an architecture change.

### 0.3 Decision B — The Authentication dependency is the critical constraint
Authoring/owning/sharing content **requires durable identity**, which is **Roadmap Phase 2 (Accounts)** — **not
yet built**. Therefore:

- All Content Platform layers are built and **fully tested against the [ADR-009](../07-software-architecture/12-decisions/ADR-009-participant-lifecycle-presence-session-continuity.md)
  identity seam** — an `ICurrentUser`/`OwnerId` abstraction introduced in Slice 00. **No slice invents
  authentication.**
- **Consumption** of Official/Public content in rooms needs **no account** (SEAM-1) and can ship independently.
- **Authoring endpoints (Slice 09) require an authenticated principal**; until Phase-2 auth merges they are wired
  to the seam but **not exposed in production** (a configuration gate, not stub code). This is the single
  external blocker and is called out in [High-Risk Areas](#high-risk-areas).

### 0.4 Entry criteria absorbed from the Pre-Implementation Review (Important items)
Slice 00 lands the review's Important documentation/constant items; the `REC-*` formalizations are implemented in
the slices where they belong (idempotency → 02/04; current-version recomputation & delete-cancel → 02/04;
block-reversibility → 04; anonymous-host scope → 08; server-side visibility → 06/07 + arch tests 11; pagination →
06/10). Traceability is shown per slice.

---

## Slices

### Slice 00 — Foundations & Vocabulary
- **Goal:** Establish the module skeleton, canonical vocabulary, constants, and the identity seam so every later
  slice builds on stable ground.
- **Scope:** `Content` namespaces/folders in each project; ADR-000 vocabulary fold-in; Business Glossary entries;
  ratify `DICTIONARY_MAX_WORDS` + per-word length bounds in the Constants Catalog; introduce the identity seam.
- **Domain changes:** `OwnerId` value object; empty `Content/` aggregate folder scaffolding (no behavior);
  `ContentType`, `Visibility` value objects/enums (types only, used by Slice 01).
- **Application changes:** `ICurrentUser` port (returns the authenticated `OwnerId`, per ADR-009 seam) in
  `Common/Ports/Identity/`.
- **Infrastructure changes:** none (implementation of `ICurrentUser` deferred to the Phase-2 auth workstream; a
  test double is used until then).
- **API changes:** none.
- **Tests:** unit tests for `OwnerId`/`Visibility`/`ContentType` VO equality & validation; docs build/link check.
- **Dependencies:** none.
- **Risks:** vocabulary drift if ADR-000 not updated first — mitigated by doing it here.
- **Complexity:** **S**
- **Review traceability:** D-1, D-2, D-5, V-1..V-5, Decision B seam.

---

### Slice 01 — Content Domain (Dictionary Aggregate)
- **Goal:** A complete, invariant-enforcing Dictionary aggregate with Draft, Version, metadata, value objects,
  and domain events — pure domain, no I/O.
- **Scope:** The whole aggregate and its lifecycle logic (no persistence/app wiring).
- **Domain changes:**
  - **Aggregate root** `Dictionary : AggregateRoot` — owns one `Draft`, a `Version` history, metadata, visibility,
    share-grant references, content type, lifecycle state.
  - **Entities:** `Draft` (mutable word set), `DictionaryVersion` (immutable snapshot + immutable version metadata).
  - **Value objects:** `DictionaryId`, `VersionId` (globally-unique, unguessable — SEC-1), `VersionLabel`
    (monotonic), `Word` (normalized: trim/collapse/case-fold for comparison), `WordSet`, `Title`, `Description`,
    `Tags`, `Language`, `Region` (optional), `Provenance` (non-retaining source `VersionId` reference — REC-2).
  - **State:** `VersionState` (Draft/Validated/Published/Deprecated/Archived/Retired + Pending Review/Discoverable/
    Blocked for Public), `DictionaryState` (Active/Archived/PendingDeletion/Deleted).
  - **Invariants enforced in-aggregate:** exactly one owner (AI-CP-1), ≤1 Draft (AI-CP-3), published Versions
    immutable (AI-CP-4), unique `VersionId` (AI-CP-5), Version belongs to one Dictionary (REC-9/AI-CP-16),
    current-version pointer = newest non-retired published or null (REC-3/AI-CP-17), word validity (V-CONTENT-1..5).
  - **Domain events:** `DictionaryCreated`, `DictionaryRenamed`, `WordsChanged`, `DraftDiscarded`,
    `VersionPublished`, `VisibilityChanged`, `DictionaryShared`/`ShareRevoked`, `DictionaryCloned`,
    `DictionaryArchived`/`Restored`/`DeletionRequested`/`DeletionCancelled`/`Deleted`, `VersionSubmittedForReview`/
    `ReviewApproved`/`ReviewRejected`/`VersionBlocked`/`VersionRetired`.
  - **Errors:** `Content/Errors/*` (e.g., `NotOwnerException`, `VersionImmutableException`, `DraftTooSmallException`,
    `InvalidWordException`, `VisibilityTransitionException`).
- **Application changes:** none.
- **Infrastructure changes:** none.
- **API changes:** none.
- **Tests (`Cluely.UnitTests`):** exhaustive aggregate/VO unit tests — word normalization/dedup boundaries (24 vs
  25), publish snapshots draft, immutability of published Versions, current-version recomputation on retire/block
  (F-15), block-reversible vs retire-terminal (REC-5), delete-cancel (REC-4), clone independence (AI-CP-8),
  visibility transition gates (BR-CONTENT-032).
- **Dependencies:** Slice 00.
- **Risks:** aggregate size; mitigate by keeping lifecycle logic in the root and behavior in VOs (SRP). No
  framework leakage (DoD).
- **Complexity:** **L**
- **Review traceability:** §2 Aggregate, §3 Invariants, §4 Lifecycle, REC-2/3/5/9/10, §15 domain tests.

---

### Slice 02 — Dictionary Lifecycle Commands
- **Goal:** Create/Rename/Archive/Restore/RequestDelete/CancelDelete as application commands, owner-authorized and
  idempotent, against a defined repository port.
- **Scope:** Application vertical slices; introduce the persistence **port** (impl in Slice 10).
- **Domain changes:** none (reuses Slice 01).
- **Application changes:** `Content/CreateDictionary`, `RenameDictionary`, `ArchiveDictionary`, `RestoreDictionary`,
  `RequestDeleteDictionary`, `CancelDeleteDictionary` (each `Command`/`Validator`/`Handler`/`Result`); define
  `IDictionaryRepository` port; owner check via `ICurrentUser`; **idempotent create** keyed by client request id
  (REC-8).
- **Infrastructure changes:** none yet (port only).
- **API changes:** none yet (Slice 09).
- **Tests (`Cluely.UnitTests`):** handler tests with mocked `IDictionaryRepository`/`ICurrentUser` — ownership
  enforcement, idempotent create, archive/restore transitions, delete request + cancel (REC-4), non-owner
  rejection (SEC-3).
- **Dependencies:** Slice 01.
- **Risks:** idempotency semantics; align with ADR-010 command model.
- **Complexity:** **M**
- **Review traceability:** REC-4/8, Z-* authorization, SEC-3.

---

### Slice 03 — Word Management & Import
- **Goal:** Edit the Draft — add/remove/update words and import a word collection — with normalization,
  de-duplication, and per-rule reject reporting.
- **Scope:** Draft-editing application slices + import (logical, no format).
- **Domain changes:** none (Draft behavior already in Slice 01); confirm `WordSet` operations cover add/remove/update.
- **Application changes:** `Content/AddWords`, `RemoveWord`, `UpdateWord`, `DiscardDraft`, `ImportWords`
  (append/replace) with reject reporting (FR-CONTENT-060/061); owner-authorized.
- **Infrastructure changes:** none (import *encoding* is out of scope — the command takes an already-parsed word
  collection; parsing/format is a future/API concern, not here).
- **API changes:** none yet.
- **Tests (`Cluely.UnitTests`):** normalization/dedup, blank rejection, max-words & per-word bounds (V-CONTENT-4/5),
  append vs replace, reject-report without failing the batch, discard reverts to last published (or empty).
- **Dependencies:** Slice 01, Slice 02 (repository port).
- **Risks:** import batch semantics; keep words-only (FR-CONTENT-063).
- **Complexity:** **M**
- **Review traceability:** §9 validation, §15 domain tests.

---

### Slice 04 — Publishing, Review & Moderation
- **Goal:** Publish a Draft into an immutable Version; run validation; manage the Public review gate and
  moderation lifecycle (submit/approve/reject/block/retire/report) — all lifecycle-only, never editing words.
- **Scope:** Publication + review + moderation application slices.
- **Domain changes:** none new (states/events in Slice 01); ensure publish is atomic snapshot (F-1/F-7).
- **Application changes:** `Content/PublishDictionary` (validate→snapshot→advance current pointer, **idempotent** —
  REC-8/F-13); `SubmitForReview`, `ApproveReview`, `RejectReview` (Public); `BlockVersion` (reversible — REC-5),
  `RetireVersion` (terminal), `ReportDictionary`; moderator authorization as a **restricted second command source**
  (REC-1); current-version recomputation on retire/block (REC-3/F-15).
- **Infrastructure changes:** none yet.
- **API changes:** none yet.
- **Tests (`Cluely.UnitTests`):** publish blocked under 25 words; publish creates immutable Version; label advances
  only on success (REC-6); idempotent publish → one Version (AC-6); review approve→discoverable, reject→not
  discoverable but usable privately (F-6); block→re-review restore, retire irreversible; moderation never mutates
  words (AT-10); recompute current on retire of only version (F-15).
- **Dependencies:** Slice 01, 02, 03.
- **Risks:** two-writer concurrency (owner vs moderator) — serialize via aggregate/custody optimistic
  concurrency; idempotency of publish.
- **Complexity:** **L**
- **Review traceability:** REC-1/3/5/6/8, §4 Lifecycle L-4, F-6/7/13/15, AC-2/6.

---

### Slice 05 — Clone & Provenance
- **Goal:** Clone any Version the actor may see into a new, independent, owner-held Dictionary carrying a
  non-retaining provenance reference.
- **Scope:** Clone application slice.
- **Domain changes:** confirm `Dictionary.CloneFrom(sourceVersion, newOwner)` factory produces an independent
  aggregate seeded with words + `Provenance` (Slice 01 already models it).
- **Application changes:** `Content/CloneDictionary` — requires auth + source visibility; new owner = cloner;
  independent thereafter (AI-CP-8, BR-CONTENT-034); provenance non-retaining (REC-2).
- **Infrastructure changes:** none yet.
- **API changes:** none yet.
- **Tests (`Cluely.UnitTests`):** clone seeds words + provenance; editing/deleting clone or source never affects
  the other (AT-12); clone-while-source-publishing binds to the chosen Version (F-3); dangling provenance after
  source delete is functional (AC-7).
- **Dependencies:** Slice 01, 02; visibility check (design agrees with Slice 07, but clone-from-own/public works
  without 07 — sharing-sourced clone gains coverage after 07).
- **Risks:** provenance dangling semantics — explicitly non-retaining.
- **Complexity:** **M**
- **Review traceability:** REC-2, AI-CP-8, F-3, AC-7.

---

### Slice 06 — Discovery & Queries
- **Goal:** Browse Mine / Official / Shared-with-me / Public with server-side visibility enforcement, search over
  business fields, and pagination.
- **Scope:** Read-side queries + read model/projection (ADR-010 queries).
- **Domain changes:** none.
- **Application changes:** `IContentReadModelProvider` port; queries `ListMine`, `ListOfficial`, `ListSharedWithMe`,
  `ListPublic`, `GetDictionaryDetail`, `GetVersionHistory` — **all paginated** (P-2/P-4) and **visibility-filtered
  server-side** (SEC-2); search over Title/Description/Tags/Owner/Language/Type/Region (§20 fields).
- **Infrastructure changes:** read-model/projection implementation may be introduced here or consolidated in Slice
  10; **the projection query is defined here**, its EF realization lands in 10 (per Decision A).
- **API changes:** none yet.
- **Tests (`Cluely.UnitTests`):** visibility filtering (private/draft/non-current never returned to non-owners —
  FF-CP-002/007/AT-7); pagination boundaries; search matching; Public catalog only shows approved Versions
  (FF-CP-009).
- **Dependencies:** Slice 01, 02, 04 (needs published/reviewed content to list).
- **Risks:** visibility bypass — enforce in the provider, never the client (SEC-2).
- **Complexity:** **M**
- **Review traceability:** SEC-2, P-2/P-4, FF-CP-007/009, §20 search.

---

### Slice 07 — Sharing & Visibility
- **Goal:** Owner sets visibility (Private/Shared/Public) and grants/revokes Viewer access to specific accounts.
- **Scope:** Visibility + share/revoke application slices.
- **Domain changes:** none new (share grants + visibility in Slice 01); confirm Private→Public requires ≥1
  published Version + review submission (BR-CONTENT-032).
- **Application changes:** `Content/SetVisibility`, `ShareDictionary`, `RevokeShare` — owner-authorized; revoke
  ends future access only (AI-CP-7); Public transition triggers the review submission from Slice 04.
- **Infrastructure changes:** none yet.
- **API changes:** none yet.
- **Tests (`Cluely.UnitTests`):** default Private; share grants view/select of current Version only (no edit/draft/
  re-share); revoke removes future access but not pinned matches (F-5); Private→Public gate; visibility change
  never affects pinned matches (AI-CP-7).
- **Dependencies:** Slice 01, 02, 04 (Public review), 06 (share affects discovery results).
- **Risks:** private leakage via share — covered by SEC-2/5 and tests.
- **Complexity:** **M**
- **Review traceability:** §7 visibility, SEC-5, AI-CP-6/7.

---

### Slice 08 — Gameplay Integration (Selection & Pinning)
- **Goal:** Let a room host select permitted content; resolve to the current published Version; pin it via the
  **existing** ADR-008 contract — with zero change to gameplay rules and correct anonymous-host scope.
- **Scope:** Extend room dictionary selection to resolve Content Platform published Versions; **reuse** the
  existing `DictionaryReference` VO and `SelectDictionary`/`StartMatch` pin path.
- **Domain changes:** none to gameplay rules; the room continues to pin a **Version identity** (existing
  `DictionaryReference`) + draw the board once into immutable board state (Alt-E refinement, AT-6).
- **Application changes:** a `IPublishedVersionResolver` (or extend existing dictionary resolution) that maps a
  selected dictionary → current published `VersionId`, enforcing permission; **anonymous host may select only
  Official + Public; authenticated host additionally Mine + Shared** (REC-7/AC-3); reject unauthorized/unpublished/
  too-small selections with existing error codes (FR-CONTENT-073).
- **Infrastructure changes:** wire the resolver to the read model (identity-addressed, read-only).
- **API changes:** extend room-creation/selection endpoint inputs only (no new gameplay endpoint); consumption
  needs **no auth** (FR-CONTENT-074).
- **Tests (`Cluely.UnitTests` + `Cluely.IntegrationTests`):** selection resolves to current Version; **subsequent
  publish/retire/visibility change does not alter a running match** (AI-CP-7/12, F-4/F-5); anonymous scope
  (AC-3); replay/recovery reproduce identical words (F-17); board holds identity + words (AT-6).
- **Dependencies:** Slice 01, 04, 06, 07.
- **Risks:** **HIGH — touches the frozen gameplay boundary.** Mitigation: no rule change; reuse the existing pin;
  guard with architecture tests (Slice 11) and the gameplay-boundary tests above.
- **Complexity:** **M**
- **Review traceability:** §8 Gameplay boundary, REC-7, AC-3/4, F-4/5/17, AT-6.

---

### Slice 09 — REST API Surface
- **Goal:** Expose all commands/queries as controllers with DTOs, request validation, error mapping, and Swagger —
  authoring gated behind the identity seam.
- **Scope:** `Cluely.Api/Controllers/Content*`, `Contracts/` DTOs, `Mapping/`, following existing API conventions.
- **Domain changes:** none.
- **Application changes:** none (thin adapters only).
- **Infrastructure changes:** none beyond DI registration.
- **API changes:** endpoints for create/rename/archive/restore/delete/cancel, word/import, publish/review/moderation,
  clone, discovery/search (paginated), sharing/visibility, and the room-selection extension; **authoring endpoints
  require an authenticated principal** (config-gated until Phase-2 auth — Decision B); consumption/discovery of
  Official/Public require none per SEAM-1; map domain/application errors to the existing error contract.
- **Tests (`Cluely.IntegrationTests`):** endpoint happy/failure paths, authZ (401/403 on authoring without/with
  wrong identity), pagination contracts, Swagger generation, error mapping; **no auth required to select
  Official/Public** (SEAM-1 integration test).
- **Dependencies:** Slices 02–08; **Slice 10** (persistence) for full end-to-end integration tests.
- **Risks:** exposing authoring before auth exists — mitigated by the config gate (no stub auth).
- **Complexity:** **M**
- **Review traceability:** §16 integration tests, Decision B, SEAM-1.

---

### Slice 10 — Persistence
- **Goal:** Durable, immutable-respecting storage for the Dictionary aggregate, its Versions, share grants, and
  read-model projections, with referenced-Version retention.
- **Scope:** EF configurations, repository/read-model implementations, migrations, retention query.
- **Domain changes:** none.
- **Application changes:** none (implements ports defined in 02/06).
- **Infrastructure changes:** `Persistence/Configurations/Dictionary*Configuration`; `IDictionaryRepository` +
  `IContentReadModelProvider` implementations; a migration; **immutable Version storage** (append-only); retention
  query preventing physical delete while referenced (AI-CP-15/REC / FF-CP-011); discovery/history indexes
  (pagination P-2/P-4).
- **API changes:** none.
- **Tests (`Cluely.IntegrationTests`):** round-trip persistence; published Version immutable at rest (FF-CP-005);
  referenced-Version retained through delete request (F-8-guard, FF-CP-011); pagination/index behavior; recovery
  reproduces words (F-17).
- **Dependencies:** Slices 01–08 (schemas cover all state).
- **Risks:** retention correctness; migration safety; optimistic concurrency for two-writer (Slice 04).
- **Complexity:** **L**
- **Review traceability:** AI-CP-15, FF-CP-005/011, P-2/P-4/P-8, F-17.

---

### Slice 11 — Architecture Tests
- **Goal:** Encode the fitness functions and dependency rules as executable architecture tests.
- **Scope:** `Cluely.ArchitectureTests` additions.
- **Domain/App/Infra/API changes:** none.
- **Tests:** AT-1 (Content ⟂ Gameplay dependency direction), AT-2/FF-CP-001 (no gameplay mutates Dictionary),
  AT-3 (published Version type exposes no mutator), AT-4/FF-CP-004 (≤1 Draft), AT-5/FF-CP-003 (exactly one owner),
  AT-6 (room holds identity+words), AT-7/FF-CP-002/007 (no draft/non-current/private to non-owner), AT-8/SEC-2
  (server-side visibility), AT-9/FF-CP-009 (public needs approved Version), AT-10/FF-CP-010 (moderation lifecycle-
  only), AT-11/FF-CP-014 (one lifecycle state machine), AT-12/FF-CP-008 (clone independence), FF-CP-012 (no
  authoring without identity; no consumption requires it), FF-CP-013 (one Version per match, reproducible).
- **Dependencies:** Slices 01–10 (types must exist to assert against).
- **Risks:** brittle reflection tests — keep rules structural.
- **Complexity:** **M**
- **Review traceability:** §14 architecture tests, all FF-CP-*.

---

### Slice 12 — Hardening
- **Goal:** Production-readiness: performance validation, logging/observability, documentation, and final review.
- **Scope:** Cross-cutting hardening; no new business behavior.
- **Domain/App changes:** none.
- **Infrastructure changes:** structured logging (Serilog, existing) on content commands/moderation; retention/
  archival job for referenced-Version cleanup and PendingDeletion completion; pagination/index verification (P-*).
- **API changes:** Swagger polish; rate-limit hooks for import/search abuse (operational, SEC-6/7) if in platform scope.
- **Tests:** performance sanity on large dictionaries/many versions (bounded by constants); log assertions;
  regression sweep; DoD checklist.
- **Dependencies:** Slices 01–11.
- **Risks:** scope creep into premature optimization — restrict to identified risks (§18).
- **Complexity:** **M**
- **Review traceability:** §18 performance, D-6/D-7 docs, DoD final approval.

---

## Dependency Graph

```
00 Foundations
   │
   ▼
01 Content Domain ──────────────────────────────────────────────┐
   │                                                             │
   ▼                                                             │
02 Lifecycle Commands ── ports ──► 10 Persistence ◄──────────────┤
   │        │                          ▲                         │
   ▼        ▼                          │                         │
03 Words  05 Clone                     │                         │
   │                                   │                         │
   ▼                                   │                         │
04 Publishing/Review/Moderation ───────┤                         │
   │                                   │                         │
   ▼                                   │                         │
06 Discovery ──► 07 Sharing/Visibility ┘                         │
   │                │                                            │
   ▼                ▼                                            ▼
08 Gameplay Integration ──────────► 09 REST API ──► 11 Arch Tests ──► 12 Hardening
```

**Reading:** Domain (01) underpins everything. Command/query slices (02–08) depend on 01 and on the ports they
introduce. Persistence (10) implements those ports and must precede full end-to-end tests in 09. Architecture
tests (11) and Hardening (12) close the chain.

---

## Critical Path

`00 → 01 → 02 → 04 → 06 → 07 → 08 → 09/10 → 11 → 12`

- **01 Content Domain** and **04 Publishing** are the two heaviest (L) and gate the most downstream work — they
  are the schedule drivers.
- **08 Gameplay Integration** is the highest-risk node (touches frozen gameplay) and depends on 04/06/07.
- **09 API** and **10 Persistence** are mutually reinforcing for end-to-end tests; sequence **10 before 09's full
  integration suite** (unit-level API tests can precede).

---

## Parallelizable Work

Once **01** is merged:
- **03 Words** and **05 Clone** can proceed **in parallel** (both depend only on 01 + the Slice-02 port).
- **06 Discovery** read-model design can start in parallel with **04**'s later stages (needs published content to
  test end-to-end, but query/port shape is independent).
- **11 Architecture Tests** can be **written incrementally** alongside each slice (assert as types land) rather
  than only at the end.
- **10 Persistence** EF configs can be drafted in parallel with 02–08 (schema follows the aggregate from 01),
  merging once the aggregate shape is final.
- **Docs (Slice 00 items + D-3 index)** are parallel to all engineering.

**Not parallelizable:** 02 before 03/04/05 (introduces the repository port); 04 before 06/07/08 (needs published/
review states); 08 last among feature slices (integrates all).

---

## High-Risk Areas

| Risk | Slice | Severity | Mitigation |
|------|-------|----------|------------|
| **Authentication not yet built (Phase 2)** | 09 (all authoring) | **Critical dependency** | Build against the ADR-009 identity seam; config-gate authoring endpoints; ship consumption path independently. No stub auth. |
| **Gameplay boundary regression** | 08 | High | No rule change; reuse existing pin; guard with AT-1/2/6 + gameplay-boundary tests (F-4/5/17); code-review by a gameplay owner. |
| **Two-writer concurrency (owner vs moderator)** | 04, 10 | Medium | Single aggregate + optimistic concurrency (existing custody pattern); serialize commands (ADR-010). |
| **Server-side visibility bypass / private leak** | 06, 07 | High | Enforce visibility in the read-model provider (SEC-2/AT-8); never trust client; unguessable identities (SEC-1). |
| **Publish idempotency / duplicate versions** | 02, 04 | Medium | Idempotent commands keyed by request id (REC-8/F-13). |
| **Referenced-Version retention vs delete** | 10 | Medium | Retention query blocks physical delete while referenced (FF-CP-011). |
| **Owner/account deletion vs retention (future)** | — | Forward | Out of this phase; flagged for the Phase-2 account-deletion design (O-4). |

---

## Recommended Order

1. **00 Foundations** (docs/constants/seam) — unblocks all.
2. **01 Content Domain** — the core; heaviest; everything waits on it.
3. **02 Lifecycle Commands** — introduces the repository port.
4. **03 Words** ∥ **05 Clone** — parallel after 02.
5. **04 Publishing/Review/Moderation** — unlocks discovery/sharing/integration.
6. **06 Discovery** → **07 Sharing/Visibility**.
7. **10 Persistence** — implement ports; enable end-to-end.
8. **08 Gameplay Integration** — highest risk; after content is real and persisted.
9. **09 REST API** — expose; full integration tests (needs 10).
10. **11 Architecture Tests** — finalize (write incrementally throughout).
11. **12 Hardening** — performance, logging, docs, final review.

> Steps 3–6 offer the most parallelism; 08/09/10 form the integration cluster; 11 should be grown continuously.

---

## Definition of Done (per slice)

Every slice inherits the project [Definition of Done](../10-implementation/definition-of-done.md). A Content
Platform slice is **Done** only when **all** hold:

- **Functional:** behaves per the frozen [Feature Spec](02-feature-specification.md); the slice's business rules
  (BR-CONTENT-* / REC-*) implemented exactly; failure scenarios (§9 F-*) handled; **no partial behavior in the
  slice's scope**.
- **Architecture:** ADR-008/009/010/011 respected; Dictionary aggregate boundary unchanged; dependency direction
  Content ⟂ Gameplay preserved; no framework leakage into Domain; the relevant **FF-CP-*/AT-*** for the slice pass.
- **Code quality:** no warnings/analyzer issues; nullable satisfied; no dead/commented/TODO/fake code; no
  unnecessary abstractions.
- **Testing:** unit tests for new invariants and failure paths; architecture tests for the slice's rules;
  integration tests where the slice touches persistence/API/gameplay; existing suites green.
- **Security:** owner/visibility checks enforced **server-side** for the slice's operations (SEC-2/3/5).
- **Documentation:** public API changes documented; Engineering Decision Log updated when a non-obvious choice is
  made; any accepted technical debt recorded.
- **Review:** self-review + peer review; a second engineer could merge it with no further engineering work; for
  Slice 08, sign-off from a gameplay owner confirming **no gameplay behavior changed**.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-11 | Initial implementation plan: 13 slices (00 Foundations + 01–12) with Name/Goal/Scope/Domain/Application/Infrastructure/API/Tests/Dependencies/Risks/Complexity; two load-bearing planning decisions (port-first horizontal layering; Authentication-seam dependency); dependency graph, critical path, parallelizable work, high-risk areas, recommended order, and per-slice Definition of Done. Grounded in the observed solution layout and the Pre-Implementation Review entry criteria. No code; architecture unchanged. |
