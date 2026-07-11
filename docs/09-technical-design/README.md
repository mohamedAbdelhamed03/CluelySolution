# 09 — Technical Design

| | |
|---|---|
| **Phase** | Technical Design |
| **Status** | In progress — **Technical Design Foundation (01)**, **Persistence & Data Model Design (02)**, and **Interface Contracts / Application Ports (03)** authored; REST API design next. |
| **Why this phase exists** | To realize the approved architecture & logical design with **concrete technologies** — the stack, solution structure, data models, interface/API contracts, security design — without changing the architecture or the business. This is where technology first appears. |
| **Owner** | Lead Architect / Senior Engineers. |

## Purpose
Detailed, buildable design: the technology foundation, then component and contract designs, all
conforming to [08 Software Design](../08-software-design/README.md) and the ADRs.

## Scope
Design detail and technology mapping; not implementation code, not architecture-level decisions.

## Documents
| # | Document | Purpose |
|---|----------|---------|
| 01 | [Technical Design Foundation & Technology Mapping](01-technical-design-foundation.md) | Selects the MVP stack (.NET 10/ASP.NET Core, SQL Server, SignalR, System.Text.Json, Serilog, FluentValidation, xUnit, Docker/Linux); maps every frozen concept to technology (in-memory authority vs SQL custody; SignalR transport-only; pure Domain; carriers); fixes solution structure, layer/dependency directions, and cross-cutting conventions; 10 FF-TD fitness functions. |
| 02 | [Persistence & Data Model Design](02-persistence-and-data-model-design.md) | The non-authoritative persistence model: A1 stored as an opaque snapshot + append-only event tail (no relational decomposition), A2 as immutable relational content by ID, registry/read-models as supporting stores; snapshot/tail/recovery, durable-before-observable commit ordering, concurrency, repository responsibilities, mandatory caching (authority never cached), future distribution; 13 FF-PS fitness functions. Mermaid aggregate→persistence / recovery / snapshot / ER / caching diagrams. |
| 03 | [Interface Contracts (Application Ports)](03-interface-contracts-application-ports.md) | The last transport-neutral document: the hexagonal port layer — inbound Command/Query ports (08.06 use cases 1:1) and outbound driven ports (custody/content/delivery/presence/registry); conceptual command/query contracts, error/versioning/idempotency/correlation/security-boundary models, transport→port-only dependency rules, extension rules; 11 FF-IF fitness functions. Mermaid port-architecture / command / query / classification / dependency diagrams. |

*Planned next:* REST API Design · SignalR Message Design · Security Design · Dictionary-Loading Design
— each a transport/mechanism mapping onto the ports frozen in document 03.
- Topic sub-folders once >~25 docs.

## Dependencies
- **Input:** [08 Software Design](../08-software-design/README.md) (domain & module design) and [07 Software Architecture](../07-software-architecture/README.md) + fixed business inputs.
- **Output:** designs consumed by Implementation.

## Entry / Exit Criteria
- **Entry:** architecture approved. **Exit:** designs reviewed, traceable to architecture decisions.

## Related Phases & Next
- Follows **08**; precedes **10 Implementation**.
→ [10 — Implementation](../10-implementation/README.md)
