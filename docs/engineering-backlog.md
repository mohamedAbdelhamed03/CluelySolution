# Engineering Backlog

Every implementation review updates this file.

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
| TD-003 | Create idempotency race | Slice 10 | Blocking | Open | — | — |
| TD-004 | Publish exception consistency | Slice 04 | Required | Open | — | — |
| TD-005 | Moderator authorization | Slice 04 | Blocking | Open | — | — |
| TD-006 | Word normalization | Slice 03 | Required | Closed | `Word.Normalize` now collapses all Unicode whitespace via `Split(null)` before lowercasing. | Slice 03 |
| TD-007 | Batch duplicate consistency | Slice 03 | Required | Closed | `WordSet.AddWords` uses a single `HashSet` for existing + batch tracking: conflicts with existing words throw; within-batch duplicates are skipped deterministically (first occurrence wins). | Slice 03 |
| TD-008 | Error code mapping | Slice 09 | Deferred | Open | Standardize domain exception → API error codes at REST boundary. | — |
| TD-009 | MetadataUpdated event | Future | Nice | Open | Emit when non-title metadata changes; deferred until consumers exist. | — |
