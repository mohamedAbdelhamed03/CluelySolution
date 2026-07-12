# Engineering Backlog

Every implementation review updates this file. Last reviewed: **Slice 04 Milestone B — Dictionary Publishing, 2026-07-12**.

| Field | Description |
|-------|-------------|
| ID | Technical debt identifier |
| Title | Short description |
| Owner Slice | Slice responsible |
| Priority | Blocking / Required / Deferred / Nice |
| Status | Open / In Progress / Closed |
| Decision | Why deferred or resolution notes |
| Resolved In | PR / Slice |

## Items

| ID | Title | Owner Slice | Priority | Status | Decision | Resolved In |
|----|-------|-------------|----------|--------|----------|-------------|
| TD-001 | ShareGrant equality | Slice 07 | Blocking | Open | — | — |
| TD-002 | Visibility enumeration | Slice 06–07 | Required | Open | — | — |
| TD-003 | Create idempotency race and ownership scope | Slice 10 | Blocking | Open | Enforce an atomic uniqueness boundary and scope replay lookup to the authenticated owner plus request fingerprint; the current key-only lookup can race and can return another owner's result if keys collide. | — |
| TD-004 | Publish exception consistency | Slice 04 | Required | Open | — | — |
| TD-005 | Moderator authorization | Slice 04 | Blocking | Closed | `IContentModeratorAccessor` application seam and `ModeratorId` domain principal; moderation domain methods no longer use owner authorization. Handler enforcement deferred to Milestone B. | Slice 04 Milestone A |
| TD-006 | Word normalization | Slice 03 | Required | Closed | `Word.Normalize` now collapses all Unicode whitespace via `Split(null)` before lowercasing. | Slice 03 |
| TD-007 | Batch duplicate consistency | Slice 03 | Required | Closed | `WordSet.AddWords` uses a single `HashSet` for existing + batch tracking: conflicts with existing words throw; within-batch duplicates are skipped deterministically (first occurrence wins). | Slice 03 |
| TD-008 | Error code mapping | Slice 09 | Deferred | Open | Standardize domain exception → API error codes at REST boundary. | — |
| TD-009 | MetadataUpdated event | Future | Nice | Open | Emit when non-title metadata changes; deferred until consumers exist. | — |
| TD-010 | Content mutation intake idempotency | Slice 09–10 | Required | Open | Slice 03 mutation commands carry correlation IDs but no idempotency keys. Define replay-safe intake before these commands are exposed externally; do not alter command contracts during hardening. | — |
| TD-011 | Draft validation aggregate-version semantics | Slice 04 | Required | Open | `ValidateDraft` changes `DraftState` without advancing `AggregateVersion`. Confirm and test the approved optimistic-concurrency behavior before persistence and publishing rely on the state. | — |
| TD-012 | Reliable post-commit content-event delivery | Slice 10 | Required | Open | Handlers persist before publishing events. Persistence must provide the approved retry/atomicity mechanism so a publisher failure cannot permanently lose a committed content event. | — |
| TD-013 | Semantic no-op authoring mutations | Future | Nice | Open | Replacing a word with the same normalized value still increments version and emits `WordsChanged`; preserve current behavior until idempotency semantics are approved. | — |
| TD-014 | Content validation constants traceability | Governance | Required | Open | `DictionaryValidation` implements max-word and word-length constants flagged by Feature Spec v1.1, but the frozen Business Constants Catalog does not contain their canonical entries. Governance must ratify the catalog references; implementation values remain unchanged. | — |
| TD-015 | REC-5 unblock lifecycle mismatch | Slice 03 hardening | Required | Closed | Corrected `UnblockVersion` from `Blocked → Published` to the approved `Blocked → PendingReview`; review approval is now required before the Version becomes current/discoverable. | Slice 03 hardening |
| TD-016 | ReportDictionary application/event contract | Slice 04 | Blocking | Closed | `ReportDictionary` application slice; domain `Report(OwnerId reporter)` lifecycle-only on Shared/Public visibility; `DictionaryReported` event. Authenticated reporters only (Z-3). | Slice 04 Milestone A |
| TD-017 | Publish retry idempotency | Slice 09–10 | Required | Open | `PublishDictionary` generates the `VersionId` server-side (uniqueness upholds AI-CP-5, mirrors `CreateDictionary`), but the command carries no idempotency key, so a retried publish creates a new version. Define replay-safe publish intake with the persistence uniqueness boundary before external exposure. Relates to TD-003/TD-010. | — |
| TD-018 | Moderator retire-version path | Slice 04 workflow | Required | Closed | Aligned version retirement with the moderation model per FR-CONTENT-084: domain `RetireVersion(ModeratorId, VersionId)` (no longer owner-scoped) and the `RetireVersion` handler now enforces the `IContentModeratorAccessor` seam like the other moderation handlers. | feature/content-workflow |
| TD-019 | Persistence-backed discovery read model | Slice 10 | Required | Open | Discovery query slices and `IDictionaryReadModelProvider` are implemented, and the visibility rule (`DictionaryVisibilityPolicy`) is tested, but the Infrastructure adapter is interim and returns no content. A persistence-backed read-model/projection is required so discovery returns real data; it must satisfy the visibility contract enforced by the policy (including pagination — see TD-020). | — |
| TD-020 | Discovery pagination | Slice 09–10 | Required | Open | Discovery catalogs and version history return unbounded lists. Introduce pagination at the read-model/API boundary (pre-impl review P-2/P-4) before external exposure; do not change the query result contracts during discovery. | — |

## Slice Review Workflow

Implement → Build → Run Tests → Architecture Tests → Self Review → Update this backlog → Code Review → Merge.

Use compact per-slice review summaries. Reserve larger hardening reviews for high-risk boundaries or accumulated
cross-slice gaps.
