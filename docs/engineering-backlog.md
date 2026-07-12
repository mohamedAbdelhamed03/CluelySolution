# Engineering Backlog

Every implementation review updates this file. Last reviewed: **Backend Stabilization Priority 1 — Publish Idempotency & Content Events, 2026-07-12**.

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
| TD-001 | ShareGrant equality | Slice 07 | Blocking | Closed | `ShareGrant` equality is now by `GranteeId` only (timestamp is informational). A dictionary holds at most one grant per grantee: duplicate `ShareDictionary` commands are rejected via `DuplicateShareGrantException` and `RevokeShare` is deterministic. | feature/content-sharing |
| TD-002 | Visibility enumeration | Slice 06–07 | Required | Open | — | — |
| TD-003 | Create idempotency race and ownership scope | Slice 10 | Blocking | Closed | `SqlDictionaryRepository` persists the idempotency key with a **globally unique** filtered index, so create/clone replay is atomic and deterministic (a duplicate insert fails rather than creating a second aggregate). Because keys are globally unique, two owners cannot share a key, so the key-only replay lookup cannot return another owner's aggregate. | feature/content-persistence |
| TD-004 | Publish exception consistency | Slice 04 | Required | Open | — | — |
| TD-005 | Moderator authorization | Slice 04 | Blocking | Closed | `IContentModeratorAccessor` application seam and `ModeratorId` domain principal; moderation domain methods no longer use owner authorization. Handler enforcement deferred to Milestone B. | Slice 04 Milestone A |
| TD-006 | Word normalization | Slice 03 | Required | Closed | `Word.Normalize` now collapses all Unicode whitespace via `Split(null)` before lowercasing. | Slice 03 |
| TD-007 | Batch duplicate consistency | Slice 03 | Required | Closed | `WordSet.AddWords` uses a single `HashSet` for existing + batch tracking: conflicts with existing words throw; within-batch duplicates are skipped deterministically (first occurrence wins). | Slice 03 |
| TD-008 | Error code mapping | Slice 09 | Deferred | Open | Standardize domain exception → API error codes at REST boundary. | — |
| TD-009 | MetadataUpdated event | Future | Nice | Open | Emit when non-title metadata changes; deferred until consumers exist. | — |
| TD-010 | Content mutation intake idempotency | Slice 09–10 | Required | Open | Slice 03 mutation commands carry correlation IDs but no idempotency keys. Define replay-safe intake before these commands are exposed externally; do not alter command contracts during hardening. | — |
| TD-011 | Draft validation aggregate-version semantics | Slice 04 | Required | Open | `ValidateDraft` changes `DraftState` without advancing `AggregateVersion`. Confirm and test the approved optimistic-concurrency behavior before persistence and publishing rely on the state. | — |
| TD-012 | Reliable post-commit content-event delivery | Slice 10 | Required | Closed | `CompositeDomainEventPublisher` routes room events to SignalR and content events to in-process `ContentDomainEventPublisher` (structured logging). Integration tests use the real publisher instead of a no-op stub. | feature/backend-stabilization |
| TD-013 | Semantic no-op authoring mutations | Future | Nice | Open | Replacing a word with the same normalized value still increments version and emits `WordsChanged`; preserve current behavior until idempotency semantics are approved. | — |
| TD-014 | Content validation constants traceability | Governance | Required | Open | `DictionaryValidation` implements max-word and word-length constants flagged by Feature Spec v1.1, but the frozen Business Constants Catalog does not contain their canonical entries. Governance must ratify the catalog references; implementation values remain unchanged. | — |
| TD-015 | REC-5 unblock lifecycle mismatch | Slice 03 hardening | Required | Closed | Corrected `UnblockVersion` from `Blocked → Published` to the approved `Blocked → PendingReview`; review approval is now required before the Version becomes current/discoverable. | Slice 03 hardening |
| TD-016 | ReportDictionary application/event contract | Slice 04 | Blocking | Closed | `ReportDictionary` application slice; domain `Report(OwnerId reporter)` lifecycle-only on Shared/Public visibility; `DictionaryReported` event. Authenticated reporters only (Z-3). | Slice 04 Milestone A |
| TD-017 | Publish retry idempotency | Slice 09–10 | Required | Closed | `PublishDictionaryCommand` now requires an idempotency key; the version id is derived from that key and outcomes are persisted in `ContentCommandOutcomes` so retries replay deterministically without a second version. | feature/backend-stabilization |
| TD-018 | Moderator retire-version path | Slice 04 workflow | Required | Closed | Aligned version retirement with the moderation model per FR-CONTENT-084: domain `RetireVersion(ModeratorId, VersionId)` (no longer owner-scoped) and the `RetireVersion` handler now enforces the `IContentModeratorAccessor` seam like the other moderation handlers. | feature/content-workflow |
| TD-019 | Persistence-backed discovery read model | Slice 10 | Required | Closed | `DictionaryReadModelProvider` now projects summaries directly from queryable snapshot columns (`AsNoTracking`, no aggregate rehydration) and filters visibility server-side (owner/public/shared via the `DictionaryShareGrants` table); detail/version views deserialize a single snapshot payload. Pagination remains tracked separately as TD-020. | feature/content-persistence |
| TD-020 | Discovery pagination | Slice 09–10 | Required | Open | Discovery catalogs and version history return unbounded lists. Introduce pagination at the read-model/API boundary (pre-impl review P-2/P-4) before external exposure; do not change the query result contracts during discovery. | — |

## Slice Review Workflow

Implement → Build → Run Tests → Architecture Tests → Self Review → Update this backlog → Code Review → Merge.

Use compact per-slice review summaries. Reserve larger hardening reviews for high-risk boundaries or accumulated
cross-slice gaps.
