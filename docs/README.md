# Cluely — Documentation Portal

Welcome to the Cluely documentation system. Cluely is a global, online, multiplayer
word-association game **functionally equivalent to Codenames**, played in private rooms with
temporary nicknames (no accounts). This portal is the single entry point to all project
documentation, organized by **software development lifecycle (SDLC) phase**.

> **New here?** Read this portal, then jump to your [role reading path](_meta/02-reading-paths.md).

---

## 1. Project Overview

- **What:** A faithful digital Codenames — two teams, a 5×5 board of 25 words, Spymasters give
  one-word clues, Operatives guess; first team to reveal all its agents wins; the assassin loses
  instantly.
- **How you play:** A host creates a private room, shares a room code, friends join with a
  nickname. No sign-up.
- **Global, one gameplay:** One codebase, one ruleset worldwide; only the **word dictionary** is
  localized per region.
- **Delivery context (informational only):** a backend service + a mobile client; no business
  documentation depends on that choice.
- **Status:** Business Analysis, Governance, Engineering Analysis, the Architecture Input Report,
  and the Architecture Governance framework are **complete and approved**. The Software
  Architecture phase is ready to begin.

## 2. Documentation Philosophy

1. **Single source of truth.** Each fact — a rule, a term, a constant — has exactly one
   authoritative home; everything else references it.
2. **Lifecycle-organized.** Documents live in the SDLC phase that owns them, not in a flat pile.
3. **Traceable.** Business → Engineering → Architecture → Design → Implementation → Testing →
   Operations is a continuous, linked chain.
4. **Technology-neutral (through governance).** Business and analysis documents prescribe no
   technology; that begins in Software Architecture.
5. **Faithful & immutable core.** Gameplay, business rules, invariants, and constants are fixed
   inputs; downstream phases implement them, never change them.

## 3. Documentation Lifecycle

```
Product Discovery → Business Analysis → Business Governance → Engineering Analysis
      → Architecture Input → Architecture Governance → [Software Architecture]
      → [Software Design] → [Technical Design] → [Implementation] → [Testing]
      → [Deployment] → [Operations] → [Future Evolution]
```

Phases in `[brackets]` are **future** — their folders exist with a README describing intended
contents so no future reorganization is needed.

## 4. Folder Structure

| Folder | Phase | Contents |
|--------|-------|----------|
| [`01-product-discovery/`](01-product-discovery/README.md) | Product Discovery | Why the product exists (BRD). |
| [`02-business-analysis/`](02-business-analysis/README.md) | Business Analysis | Requirements, rules, domain model, state machines, workflows, validations, invariants, events, errors. |
| [`03-business-governance/`](03-business-governance/README.md) | Business Governance | Glossary, business ADRs, constants, quality metrics, data lifecycle, roadmap. |
| [`04-engineering-analysis/`](04-engineering-analysis/README.md) | Engineering Analysis | Engineering challenges & risk analysis + enrichment. |
| [`05-architecture-input/`](05-architecture-input/README.md) | Architecture Input | The briefing that bridges analysis to architecture. |
| [`06-architecture-governance/`](06-architecture-governance/README.md) | Architecture Governance | Principles, anti-principles, heuristics, success metrics, review checklist, traceability, risk register. |
| [`07-software-architecture/`](07-software-architecture/README.md) | Software Architecture | Architecture decisions (ADRs), discovery/decomposition. |
| [`08-software-design/`](08-software-design/README.md) | Software Design | Logical design: domain model & ubiquitous language, module decomposition, aggregate & application design. |
| [`09-technical-design/`](09-technical-design/README.md) | Technical Design *(future)* | Detailed designs, data/API contracts, security design. |
| [`10-implementation/`](10-implementation/README.md) | Implementation *(future)* | Developer guides, coding standards. |
| [`11-testing/`](11-testing/README.md) | Testing *(future)* | Test strategy, plans, reports. |
| [`12-deployment/`](12-deployment/README.md) | Deployment *(future)* | Deployment guides, environment/config docs. |
| [`13-operations/`](13-operations/README.md) | Operations *(future)* | Runbooks, playbooks, observability. |
| [`14-future-evolution/`](14-future-evolution/README.md) | Future Evolution *(future)* | Post-MVP phase designs (auth, stats, matchmaking…). |
| [`_meta/`](_meta/README.md) | Documentation-about-documentation | Standards, reading paths, dependency graph, governance, scalability, migration record. |

Each folder has a **README** explaining its purpose, inputs, outputs, entry/exit criteria, and
next phase — navigate by reading folder READMEs top-down.

## 5. Reading Guide

- **First-time reader:** this portal → [01 Product Discovery](01-product-discovery/README.md) →
  [02 Business Analysis](02-business-analysis/README.md) → onward.
- **By role:** see [Reading Paths](_meta/02-reading-paths.md) (Product Manager, Business Analyst,
  Software Architect, Backend/Frontend Developer, QA, DevOps, Technical Writer, New Team Member).
- **By dependency:** see the [Dependency Graph](_meta/03-dependency-graph.md).
- **Terminology / numbers:** the canonical [Glossary](03-business-governance/01-business-glossary.md)
  and [Constants Catalog](03-business-governance/03-business-constants-catalog.md).

## 6. Contribution Guidelines (summary)

- Add a document to the **phase folder that owns it**, using that folder's **local numbering**.
- Give it Version, Status, Purpose, TOC, References, Revision History (see
  [Documentation Standards](_meta/01-documentation-standards.md)).
- **Reference**, don't duplicate: link to the single source of truth.
- Never change gameplay, business rules, invariants, or constants in a downstream phase — those
  are fixed; escalate to the Business Analyst instead.
- Update the folder README and, if cross-phase, the
  [Dependency Graph](_meta/03-dependency-graph.md) and
  [Traceability Matrix](06-architecture-governance/06-architecture-traceability-matrix.md).

## 7. Documentation Standards

Full standards — naming, folders, markdown, cross-references, versioning, status labels, review
and approval workflow, deprecation, ownership, revision history, future numbering — are in
[`_meta/01-documentation-standards.md`](_meta/01-documentation-standards.md).

## 8. Current Progress

| Phase | State |
|-------|-------|
| Product Discovery | ✅ Complete |
| Business Analysis | ✅ Complete & validated |
| Business Governance | ✅ Complete |
| Engineering Analysis | ✅ Complete (challenges + enrichment) |
| Architecture Input | ✅ Complete |
| Architecture Governance | ✅ Complete (framework ready) |
| Software Architecture | ✅ Complete — Discovery/Decomposition (01–11) + ADR-000…ADR-010 |
| Software Design | ✅ Complete (MVP logical design) — Domain Model, Module Decomposition, C4 L1/L2, Aggregate Design, Application Layer (01–06) |
| Technical Design → Operations | ⏳ Not started (folders scaffolded) |

## 9. Future Planned Documentation

Where upcoming documents will live — no future reorganization required. See each future folder's
README and the [Migration & Structure Record](_meta/06-migration-plan-and-record.md#5-future-documentation-placement).

- **Software Architecture:** architecture ADRs, context/component designs, concurrency & real-time
  design, state-ownership design.
- **Software Design:** domain model & ubiquitous language, module decomposition, aggregate design,
  application/domain-service design, C4 views.
- **Technical Design:** data model design, API/interface contracts, security design, dictionary
  loading design.
- **Implementation:** developer setup, coding standards, module guides.
- **Testing:** test strategy, concurrency/chaos/security test plans, coverage reports.
- **Deployment:** deployment guides, environment/config, release process.
- **Operations:** runbooks, incident playbooks, observability & alerting.
- **Future Evolution:** authentication, profiles, stats, matchmaking (per the
  [Product Roadmap](03-business-governance/06-product-roadmap.md)).

---

*This portal is maintained per the [Documentation Standards](_meta/01-documentation-standards.md).
Structure and history: [Migration & Structure Record](_meta/06-migration-plan-and-record.md).*
