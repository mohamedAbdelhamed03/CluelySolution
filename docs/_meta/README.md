# `_meta/` — Documentation About the Documentation

| | |
|---|---|
| **Purpose** | Hold the documents that govern the documentation *system* itself — standards, navigation, dependency, governance, scalability, and the migration record. This folder is not an SDLC phase; it is the operating layer for the docs. |
| **Owner** | Documentation / Tech-Writing function (reviewer: Lead Architect). |

## Scope
Meta-documentation only. No product, business, engineering, or architecture content lives here —
those live in their SDLC phase folders.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 00 | [Canonical Constants & Legacy Index](00-canonical-constants-and-index.md) | Preserved former root index; holds the referenced canonical-constants and configurable-parameters tables. |
| 01 | [Documentation Standards](01-documentation-standards.md) | Naming, folders, numbering, markdown, cross-references, versioning, status, review/approval, ownership, deprecation, change management. |
| 02 | [Reading Paths](02-reading-paths.md) | Role-based recommended reading order. |
| 03 | [Dependency Graph](03-dependency-graph.md) | Logical dependency + reading order across documents. |
| 04 | [Documentation Governance](04-documentation-governance.md) | Ownership, review cadence, structural-integrity rules. |
| 05 | [Scalability Assessment](05-scalability-assessment.md) | Whether the structure scales to 50–500 docs. |
| 06 | [Migration Plan & Structure Record](06-migration-plan-and-record.md) | Old→new mapping, execution, validation, future placement. |

## Dependencies
- **Input:** the entire documentation set (this folder describes/organizes it).
- **Output:** the rules and navigation that every other folder follows.

## Entry / Exit Criteria
- **Entry:** a documentation set exists that needs governing.
- **Exit:** standards, navigation, and governance are defined and current (they are).

## Related & Next
- **Related:** every folder (all follow these standards).
- **Start here if you maintain docs:** [Documentation Standards](01-documentation-standards.md);
  otherwise begin at the [Portal](../README.md).
