# Documentation Standards — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Mandatory — repository-wide documentation standards |
| **Purpose** | Define the conventions that keep the documentation consistent, navigable, and maintainable as it grows past 150+ documents. Applies to every document in `docs/`. |

## Table of Contents
1. [Naming Conventions](#1-naming-conventions)
2. [Folder Conventions](#2-folder-conventions)
3. [Local Numbering & Future Numbering](#3-local-numbering--future-numbering)
4. [Markdown Conventions](#4-markdown-conventions)
5. [Cross-Reference Conventions](#5-cross-reference-conventions)
6. [Document Header & Required Sections](#6-document-header--required-sections)
7. [Versioning](#7-versioning)
8. [Status Labels](#8-status-labels)
9. [Review & Approval Workflow](#9-review--approval-workflow)
10. [Document Ownership](#10-document-ownership)
11. [Revision History](#11-revision-history)
12. [Deprecation Policy](#12-deprecation-policy)
13. [Change Management](#13-change-management)

---

## 1. Naming Conventions

- File names are **`NN-kebab-case-title.md`**, where `NN` is the two-digit **local** number
  within the folder (see §3). Example: `02-business-rules.md`.
- Use lowercase, hyphen-separated words; no spaces, underscores, or camelCase.
- The file name should closely match the document's H1 title (do **not** change existing titles
  just to match; new documents should align from the start).
- Folder names are **`NN-kebab-case-phase/`** for lifecycle phases; the documentation-meta folder
  is `_meta/` (leading underscore sorts it distinctly).
- Folder READMEs are always named exactly `README.md`.

## 2. Folder Conventions

- Every folder represents **one SDLC phase** (or `_meta`).
- Every folder **must** contain a `README.md` following the folder-README template (see the
  phase READMEs and [`_meta`](README.md) as examples): Purpose, Scope, Documents, Dependencies,
  Input, Output, Entry Criteria, Exit Criteria, Related Phases, Next Phase.
- A document lives in the folder for the phase that **owns** it (produces/approves it), not merely
  references it.
- Do not nest sub-phases more than one level deep unless a phase exceeds ~25 documents; then add
  a single level of topic sub-folders each with their own README (see [Scalability Assessment](05-scalability-assessment.md)).

## 3. Local Numbering & Future Numbering

- Numbering is **local to each folder**, starting at `01`, in intended reading order.
- There is **no global cross-repository numbering**. Cross-document references use the **title +
  relative link**, not a global number.
- **Adding a document:** give it the next number in its folder, or insert and renumber only if
  reading order truly requires it (renumbering is a change-managed action — §13).
- **Reserve gaps sparingly:** prefer contiguous numbers; if frequent insertion is expected in a
  large phase, use topic sub-folders instead of number gaps.
- Two-digit numbers (`01`–`99`) per folder; if a folder would exceed 99, split into sub-folders.

## 4. Markdown Conventions

- One H1 (`#`) per document — the title.
- Start with a **metadata block** (table) and a **Table of Contents** (§6).
- Use `##`/`###` for sections; keep heading text stable (anchors depend on it).
- Prefer tables for enumerable data (rules, metrics, mappings); fenced code blocks for diagrams
  (ASCII only — no image binaries required to read the docs).
- IDs (e.g., `BR-*`, `INV-*`, `EVT-*`, `ENG-*`, `AP-*`, `ASM-*`) are **stable**; never reuse or
  renumber an ID once published.
- Keep line content readable; wrap prose reasonably. No trailing whitespace.

## 5. Cross-Reference Conventions

- Reference other documents with **relative markdown links** and a human label:
  `[Business Rules](../02-business-analysis/02-business-rules.md)`.
- Link to a section with its anchor: `...(../02-business-analysis/02-business-rules.md#312-guess-validation-br-gv)`.
- **Reference, don't restate.** Cite the single source of truth (e.g., a constant lives only in the
  [Constants Catalog](../03-business-governance/03-business-constants-catalog.md); others link to it).
- When you move or rename a document, update all inbound links in the same change (see
  [Migration Record](06-migration-plan-and-record.md) for how the mass move was done and validated).
- Cross-references should point **within or backward** along the lifecycle where possible; forward
  references (to not-yet-produced phases) are allowed but marked *(future)*.

## 6. Document Header & Required Sections

Every document must include:

- A **metadata block** (table) with at least **Version**, **Status**, **Purpose**; add **Technology**
  (Neutral until architecture) where relevant.
- A **Table of Contents**.
- **References** (or inline cross-references) to related documents.
- A **Revision History** table at the end.
- Governance/analysis documents additionally follow their type's structure (e.g., challenges use the
  challenge structure; rules use rule IDs).

## 7. Versioning

- Semantic-ish document versioning: **`MAJOR.MINOR`**.
  - **MINOR** bump for clarifications, additions, non-breaking edits.
  - **MAJOR** bump for changes that alter meaning, structure, or supersede prior guidance.
- The version lives in the metadata block and the latest Revision History row.
- Superseding a document: mark the old one **Superseded** (do not delete) and link to its successor.

## 8. Status Labels

| Label | Meaning |
|-------|---------|
| **Draft** | Being written; not yet reviewed. |
| **In Review** | Under review against the applicable checklist. |
| **Approved** | Reviewed and accepted; an authoritative source. |
| **Mandatory** | Approved and binding (e.g., principles, standards). |
| **Superseded** | Replaced by a newer document (link to it). |
| **Deprecated** | No longer applicable; retained for history (§12). |
| **Placeholder** | Folder/README scaffolding for a future phase. |

## 9. Review & Approval Workflow

1. **Author** drafts in the correct folder with required sections (Draft).
2. **Peer/lead review** against the relevant standard/checklist (In Review).
3. **Approver** (the document's accountable owner — §10) approves (Approved/Mandatory).
4. Cross-phase or governance documents additionally follow the
   [Architecture Review Checklist](../06-architecture-governance/05-architecture-review-checklist.md)
   where applicable.
5. Record the outcome in the Revision History; update inbound links and folder README if needed.

## 10. Document Ownership

- Each document has an **accountable owner role** (e.g., Product Owner, Business Analyst, Lead
  Architect, Engineering Lead, QA Lead, DevOps Lead) recorded via the folder README's ownership
  note or the document metadata.
- Owners approve changes, maintain accuracy, and sign off at phase gates.
- The **`_meta`** documents are owned by the Documentation/Tech-Writing function with the Lead
  Architect as reviewer.

## 11. Revision History

- Every document ends with a Revision History table: **Version · Date · Change**.
- Dates are absolute (`YYYY-MM-DD`).
- Each material change adds a row; never rewrite history rows.

## 12. Deprecation Policy

- Mark a superseded/obsolete document **Superseded** or **Deprecated** in its status; **do not
  delete** (history and inbound links matter).
- Add a top note linking to the replacement (or explaining removal from active use).
- Deprecated documents may be moved to an `archive/` sub-folder within their phase after a
  cool-down period, with inbound links updated in the same change.

## 13. Change Management

- **Content changes** to approved documents follow §9 and bump the version (§7).
- **Structural changes** (move/rename/renumber) are performed as a **single reviewable change** that
  moves the file *and* updates all inbound links, then validates that no link is broken (the
  method used in the [Migration Record](06-migration-plan-and-record.md)).
- **Fixed inputs** — gameplay, business rules, invariants, constants — are **not** changed by
  downstream phases; a needed change is escalated to the Business Analyst/Product Owner and, if
  accepted, versioned at the source with downstream traceability updated.
- Any structural change updates: the folder README, the [Dependency Graph](03-dependency-graph.md),
  and (for architecture-phase docs) the [Traceability Matrix](../06-architecture-governance/06-architecture-traceability-matrix.md).

## 14. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial repository-wide documentation standards, created during the SDLC reorganization. |
