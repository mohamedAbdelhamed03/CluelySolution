# Migration Plan & Structure Record — Cluely Documentation

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Record (migration executed) |
| **Purpose** | Record the reorganization from a flat, globally-numbered file collection into an SDLC-phase structure with local numbering: the complete document mapping (old → new), how it was executed and validated, and where future documents belong. Serves as both the **migration plan** and its **completion record**. |

## Table of Contents
1. [What Changed & What Did Not](#1-what-changed--what-did-not)
2. [Complete Document Mapping (Old → New)](#2-complete-document-mapping-old--new)
3. [Execution Method](#3-execution-method)
4. [Validation Result](#4-validation-result)
5. [Future Documentation Placement](#5-future-documentation-placement)
6. [Rollback Note](#6-rollback-note)
7. [Revision History](#revision-history)

---

## 1. What Changed & What Did Not

**Changed (reorganization only):**
- Files moved from flat `docs/NN-*.md` into **SDLC phase folders** with **local numbering**.
- Internal cross-reference **link paths** were updated so nothing breaks.
- A **root portal** README, **per-folder READMEs**, and a **`_meta/`** governance set were added.

**Not changed (per the task's rules):**
- **No document content, meaning, wording, structure, or headings** were altered.
- **No IDs** (`BR-/INV-/EVT-/ENG-/AP-/ASM-…`) changed.
- **No document titles** changed.
- **No documents merged or split.**
- Only link **targets** (relative paths) were repointed to reflect the new locations — the visible
  link **text/labels** are unchanged.

The former root index `00-README.md` was preserved as
[`_meta/00-canonical-constants-and-index.md`](00-canonical-constants-and-index.md) because it holds
the referenced **canonical constants** and **configurable-parameters** tables; a new
[portal `README.md`](../README.md) was created as the entry point.

## 2. Complete Document Mapping (Old → New)

| Old (flat) | New (SDLC phase / local number) | Phase |
|------------|----------------------------------|-------|
| `00-README.md` | [`_meta/00-canonical-constants-and-index.md`](00-canonical-constants-and-index.md) | Meta (preserved) |
| `01-BRD.md` | [`01-product-discovery/01-business-requirements.md`](../01-product-discovery/01-business-requirements.md) | Product Discovery |
| `02-SRS.md` | [`02-business-analysis/01-software-requirements.md`](../02-business-analysis/01-software-requirements.md) | Business Analysis |
| `03-business-rules.md` | [`02-business-analysis/02-business-rules.md`](../02-business-analysis/02-business-rules.md) | Business Analysis |
| `04-functional-requirements.md` | [`02-business-analysis/03-functional-requirements.md`](../02-business-analysis/03-functional-requirements.md) | Business Analysis |
| `05-user-stories.md` | [`02-business-analysis/04-user-stories.md`](../02-business-analysis/04-user-stories.md) | Business Analysis |
| `06-use-cases.md` | [`02-business-analysis/05-use-cases.md`](../02-business-analysis/05-use-cases.md) | Business Analysis |
| `07-domain-model.md` | [`02-business-analysis/06-domain-model.md`](../02-business-analysis/06-domain-model.md) | Business Analysis |
| `08-state-machines.md` | [`02-business-analysis/07-state-machines.md`](../02-business-analysis/07-state-machines.md) | Business Analysis |
| `09-business-workflows.md` | [`02-business-analysis/08-business-workflows.md`](../02-business-analysis/08-business-workflows.md) | Business Analysis |
| `10-validation-rules.md` | [`02-business-analysis/09-validation-rules.md`](../02-business-analysis/09-validation-rules.md) | Business Analysis |
| `11-business-invariants.md` | [`02-business-analysis/10-business-invariants.md`](../02-business-analysis/10-business-invariants.md) | Business Analysis |
| `12-domain-events-catalog.md` | [`02-business-analysis/11-domain-events-catalog.md`](../02-business-analysis/11-domain-events-catalog.md) | Business Analysis |
| `13-business-error-catalog.md` | [`02-business-analysis/12-business-error-catalog.md`](../02-business-analysis/12-business-error-catalog.md) | Business Analysis |
| `14-dictionary-management.md` | [`02-business-analysis/13-dictionary-management.md`](../02-business-analysis/13-dictionary-management.md) | Business Analysis |
| `15-lobby-room-lifecycle.md` | [`02-business-analysis/14-lobby-room-lifecycle.md`](../02-business-analysis/14-lobby-room-lifecycle.md) | Business Analysis |
| `16-player-session-reconnection.md` | [`02-business-analysis/15-player-session-reconnection.md`](../02-business-analysis/15-player-session-reconnection.md) | Business Analysis |
| `17-rule-precedence.md` | [`02-business-analysis/16-rule-precedence.md`](../02-business-analysis/16-rule-precedence.md) | Business Analysis |
| `18-consistency-validation-report.md` | [`02-business-analysis/17-consistency-validation-report.md`](../02-business-analysis/17-consistency-validation-report.md) | Business Analysis |
| `19-business-glossary.md` | [`03-business-governance/01-business-glossary.md`](../03-business-governance/01-business-glossary.md) | Business Governance |
| `20-architecture-decision-records.md` | [`03-business-governance/02-architecture-decision-records.md`](../03-business-governance/02-architecture-decision-records.md) | Business Governance |
| `21-business-constants-catalog.md` | [`03-business-governance/03-business-constants-catalog.md`](../03-business-governance/03-business-constants-catalog.md) | Business Governance |
| `22-quality-metrics.md` | [`03-business-governance/04-quality-metrics.md`](../03-business-governance/04-quality-metrics.md) | Business Governance |
| `23-data-lifecycle-retention.md` | [`03-business-governance/05-data-lifecycle-retention.md`](../03-business-governance/05-data-lifecycle-retention.md) | Business Governance |
| `24-product-roadmap.md` | [`03-business-governance/06-product-roadmap.md`](../03-business-governance/06-product-roadmap.md) | Business Governance |
| `25-governance-validation-summary.md` | [`03-business-governance/07-governance-validation-summary.md`](../03-business-governance/07-governance-validation-summary.md) | Business Governance |
| `26-engineering-challenges-risk-analysis.md` | [`04-engineering-analysis/01-engineering-challenges-risk-analysis.md`](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) | Engineering Analysis |
| `27-engineering-challenges-enrichment.md` | [`04-engineering-analysis/02-engineering-challenges-enrichment.md`](../04-engineering-analysis/02-engineering-challenges-enrichment.md) | Engineering Analysis |
| `28-architecture-input-report.md` | [`05-architecture-input/01-architecture-input-report.md`](../05-architecture-input/01-architecture-input-report.md) | Architecture Input |
| `29-architecture-principles.md` | [`06-architecture-governance/01-architecture-principles.md`](../06-architecture-governance/01-architecture-principles.md) | Architecture Governance |
| `30-architecture-anti-principles.md` | [`06-architecture-governance/02-architecture-anti-principles.md`](../06-architecture-governance/02-architecture-anti-principles.md) | Architecture Governance |
| `31-architecture-decision-heuristics.md` | [`06-architecture-governance/03-architecture-decision-heuristics.md`](../06-architecture-governance/03-architecture-decision-heuristics.md) | Architecture Governance |
| `32-architecture-success-metrics.md` | [`06-architecture-governance/04-architecture-success-metrics.md`](../06-architecture-governance/04-architecture-success-metrics.md) | Architecture Governance |
| `33-architecture-review-checklist.md` | [`06-architecture-governance/05-architecture-review-checklist.md`](../06-architecture-governance/05-architecture-review-checklist.md) | Architecture Governance |
| `34-architecture-traceability-matrix.md` | [`06-architecture-governance/06-architecture-traceability-matrix.md`](../06-architecture-governance/06-architecture-traceability-matrix.md) | Architecture Governance |
| `35-architecture-risk-register.md` | [`06-architecture-governance/07-architecture-risk-register.md`](../06-architecture-governance/07-architecture-risk-register.md) | Architecture Governance |
| `36-architecture-governance-usage.md` | [`06-architecture-governance/08-architecture-governance-usage.md`](../06-architecture-governance/08-architecture-governance-usage.md) | Architecture Governance |

*Note: "architecture-decision-records" in Business Governance are the **business** ADRs (product
decisions). Architecture-phase ADRs will live under Software Architecture (§5).*

## 3. Execution Method

1. **Designed** the target hierarchy (SDLC phases) and the old→new mapping (§2).
2. **Moved** each file to its new phase folder and local number.
3. **Repointed** every internal cross-reference link to the correct relative path for its new
   location — link **targets** only; labels/anchors unchanged.
4. **Added** the root portal, per-folder READMEs, and the `_meta/` governance set.
5. **Validated** link integrity across the whole tree.

Link repointing and validation were performed by a scripted pass (deterministic, reviewable) rather
than by hand, to avoid human error across hundreds of links.

## 4. Validation Result

- **631** internal markdown links checked after migration.
- **0 broken** links (100% resolve to existing files).
- All **37** documents present at their new locations; no content diffs beyond link targets.

## 5. Future Documentation Placement

New documents go into the phase that **owns** them, with that folder's local numbering — **no
future reorganization required**:

| Future document type | Folder |
|----------------------|--------|
| Architecture ADRs, context/component design, concurrency/real-time/state-ownership design | [`07-software-architecture/`](../07-software-architecture/README.md) |
| Domain model & ubiquitous language, module decomposition, aggregate & application design, C4 views | [`08-software-design/`](../08-software-design/README.md) |
| Detailed design, data model design, API/interface contracts, security design | [`09-technical-design/`](../09-technical-design/README.md) |
| Developer guides, coding standards, module docs | [`10-implementation/`](../10-implementation/README.md) |
| Test strategy, test plans, coverage/reports | [`11-testing/`](../11-testing/README.md) |
| Deployment guides, environment/config, release process | [`12-deployment/`](../12-deployment/README.md) |
| Runbooks, incident playbooks, observability/alerting | [`13-operations/`](../13-operations/README.md) |
| Post-MVP designs (auth, profiles, stats, matchmaking) | [`14-future-evolution/`](../14-future-evolution/README.md) |
| Documentation-system docs (standards, governance, this record) | [`_meta/`](README.md) |

When a phase exceeds ~25 documents, introduce **one level of topic sub-folders** per the
[Scalability Assessment](05-scalability-assessment.md#4-recommendations-apply-as-thresholds-are-hit).

## 6. Rollback Note

The mapping in §2 is bijective; a rollback would reverse each move and repoint links back to flat
names. There is no need to roll back — validation is clean — but the record makes it reversible.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Recorded the flat→SDLC reorganization: mapping, method, validation (631 links, 0 broken), future placement. |
