# 08 — Software Design

| | |
|---|---|
| **Phase** | Software Design |
| **Status** | Complete (MVP logical design) — **Domain Model (01)**, **Module Decomposition (02)**, **C4 System Context L1 (03)**, **C4 Container L2 (04)**, **Aggregate Design (05)**, and **Application Layer Design (06)** authored and approved. Ready for 09 Technical Design. |
| **Why this phase exists** | To turn the **approved architecture** (phase 07: Discovery 01–11 and ADR-000…ADR-010) into a precise **logical design** developers can implement consistently — the domain model, module decomposition, aggregate and application design — **without** changing the architecture or the business, and **without** yet choosing technologies. |
| **Owner** | Lead Architect / Senior Engineers. |

## Purpose
Produce the **conceptual, technology-neutral design** that every implementation must follow: the
ubiquitous language, bounded contexts, aggregates, entities, value objects, domain services, domain
events, invariants, policies, ownership, and lifecycles — all **derived from**, and conforming to,
the frozen architecture. This is the bridge between *how the system must behave* (architecture) and
*how it is built in detail* (technical design and implementation).

## Scope
Logical/domain design and module decomposition. **In scope:** domain model, bounded contexts,
aggregate boundaries, module structure, C4 conceptual views, application/domain-service design.
**Out of scope:** data-model/persistence design, API/message contracts, security-mechanism design,
and any technology, framework, protocol, or storage choice — those belong to
[09 Technical Design](../09-technical-design/README.md) and later.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Domain Model & Ubiquitous Language](01-domain-model-and-ubiquitous-language.md) | The canonical, technology-neutral domain model: ubiquitous language, bounded contexts, aggregates, entities, value objects, domain services, events, invariants, policies, ownership, lifecycles — the single source of truth every later design derives from. |
| 02 | [Module Decomposition](02-module-decomposition.md) | The canonical logical module set (M1 Room & Lobby, M2 Gameplay, M3 Content, M4 Delivery, M5 Connectivity & Identity, M6 Recovery & State Custody): responsibilities, boundaries, logical interfaces, dependency rules/graph, communication, state ownership, cross-module contracts, lifecycle, concurrency ownership, extension points, and adversarial review. |
| 03 | [C4 System Context (Level 1)](03-c4-system-context.md) | The executive overview: Cluely as one self-contained system, its actors (Player, Host; future Spectator/Bot/AI/Administrator/Tournament Organizer), future external systems, trust boundaries, relationships, and context invariants — Mermaid diagrams, no modules/technology. |
| 04 | [C4 Container Diagram (Level 2)](04-c4-container-diagram.md) | The system box opened into six logical containers (C1–C6 = modules M1–M6): responsibilities, container/dependency/command-flow + runtime-collaboration (sequence) diagrams, interaction catalogue, state ownership, command/query routing, failure & trust boundaries, lifecycle, quality attributes, and adversarial review — no technology. |
| 05 | [Aggregate Design](05-aggregate-design.md) | The internal design of the two frozen aggregates (A1 Room/Match, A2 Dictionary Version): roots, entities, value objects, state model, lifecycle, consistency boundary, a two-column invariant-enforcement matrix (owned-by vs enforced-at for all 37 INV), event production (EVT-1…25), interaction scenarios, evolution, adversarial review, and 13 fitness functions. Mermaid class/ownership/lifecycle/interaction diagrams; no persistence/technology. |
| 06 | [Application Layer Design](06-application-layer-design.md) | The stateless Application-Service orchestration seam (coordinates; owns no state/aggregate; not a new module): command & query pipelines, use-case catalog, layered validation, three-way authorization, four-way idempotency, error strategy, post-commit event publication, C4-owned projection coordination, cross-module coordination, and 12 fitness functions. Mermaid pipeline/validation/cross-module + sequence diagrams; technology-neutral. |

*The MVP logical design set (01–06) is complete.* *(Introduce topic sub-folders once this phase
exceeds ~25 documents — see [Scalability Assessment](../_meta/05-scalability-assessment.md).)*

## Dependencies
- **Input:** [07 Software Architecture](../07-software-architecture/README.md) — Discovery/Decomposition
  (01–11) and [ADR-000…ADR-010](../07-software-architecture/12-decisions/README.md) — plus the fixed
  business inputs ([02 Business Analysis](../02-business-analysis/README.md),
  [03 Business Governance](../03-business-governance/README.md)).
- **Output:** an approved domain model & module design consumed by
  [09 Technical Design](../09-technical-design/README.md) and
  [10 Implementation](../10-implementation/README.md).

## Entry Criteria
- Software Architecture approved: Discovery/Decomposition complete and ADR-000…ADR-010 Accepted.

## Exit Criteria
- The domain model is complete, internally consistent, fully **traceable** to business rules,
  invariants, and ADRs, and **compliant** with every ADR; it passes design review and is stable
  enough that implementation can begin without inventing new business concepts or changing aggregate
  boundaries.

## Related Phases & Next
- Follows **07 Software Architecture** (consumes its decisions); precedes **09 Technical Design**.
→ [09 — Technical Design](../09-technical-design/README.md)
