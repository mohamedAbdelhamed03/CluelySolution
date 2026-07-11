# 34. Architecture Traceability Matrix — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Framework — the traceability structure to maintain through the lifecycle |
| **Purpose** | Provide the **framework** that links Business Drivers → Engineering Drivers → Architecture Drivers → Architecture Decisions → Implementation → Testing → Operations. This document defines the columns, the linking rules, and a seeded (analysis-side) matrix; the Architecture/Implementation/Testing/Operations columns are populated *later* by their phases. |
| **Technology** | Neutral. No implementation details are populated. |

## Table of Contents
1. [Purpose & Principle](#1-purpose--principle)
2. [References](#2-references)
3. [Traceability Chain & Column Definitions](#3-traceability-chain--column-definitions)
4. [Linking Rules](#4-linking-rules)
5. [Seeded Matrix (Analysis Side)](#5-seeded-matrix-analysis-side)
6. [Per-Decision Traceability Record (Template)](#6-per-decision-traceability-record-template)
7. [Maintaining Traceability Through the Lifecycle](#7-maintaining-traceability-through-the-lifecycle)
8. [Revision History](#8-revision-history)

---

## 1. Purpose & Principle

Traceability guarantees that **nothing in the architecture is arbitrary** and **nothing in the
business is dropped**. Every architecture decision must trace *backward* to a driver and
*forward* to how it will be verified and operated. Gaps in either direction are review findings
([33 G2](05-architecture-review-checklist.md), [32 ASM-04](04-architecture-success-metrics.md)).

## 2. References
- [28 Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) (drivers), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md) (challenges)
- [22 Quality Metrics](../03-business-governance/04-quality-metrics.md), [32 Architecture Success Metrics](04-architecture-success-metrics.md)

## 3. Traceability Chain & Column Definitions

```
Business Driver → Engineering Driver → Architecture Driver → Architecture Decision → Implementation → Testing → Operations
```

| Column | Meaning | Owned/Populated by | Source of IDs |
|--------|---------|--------------------|---------------|
| **Business Driver** | The business goal influencing architecture. | Business Analysis (done) | [28 §2](../05-architecture-input/01-architecture-input-report.md#2-business-drivers) |
| **Engineering Driver** | The engineering concern it creates. | Engineering Analysis (done) | [28 §3](../05-architecture-input/01-architecture-input-report.md#3-engineering-drivers), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) |
| **Architecture Driver** | The problem architecture must solve. | Architecture Input (done) | [28 §7](../05-architecture-input/01-architecture-input-report.md#7-architecture-drivers) |
| **Architecture Decision** | The recorded decision (architecture ADR). | **Architecture phase** | Architecture ADR IDs (future) |
| **Implementation** | The realizing work item/module reference. | **Implementation phase** | To be assigned |
| **Testing** | The verification approach/suite reference. | **Testing phase** | [22 QM-*](../03-business-governance/04-quality-metrics.md), [27 testing notes](../04-engineering-analysis/02-engineering-challenges-enrichment.md) |
| **Operations** | The runtime signal/runbook that confirms it in production. | **Operations phase** | [12 Events](../02-business-analysis/11-domain-events-catalog.md), [22 metrics](../03-business-governance/04-quality-metrics.md) |

The last four columns are intentionally **left as framework placeholders** here (per the task:
"do not populate implementation details").

## 4. Linking Rules

- **LR-1 Backward completeness:** every Architecture Decision links to ≥1 Architecture Driver, which links to ≥1 Engineering and Business Driver.
- **LR-2 Forward completeness:** every Architecture Decision links forward to a Testing approach and (where runtime-observable) an Operations signal.
- **LR-3 Coverage:** every `BR-*`, `INV-*`, and P1 engineering challenge appears somewhere in the matrix ([32 ASM-01/02/03](04-architecture-success-metrics.md)).
- **LR-4 No orphans:** any decision with no backward link is flagged as *possible over-engineering* ([35 Risk](07-architecture-risk-register.md), AAP-05/12).
- **LR-5 No gaps:** any driver with no forward decision is flagged as *unaddressed requirement*.
- **LR-6 Change propagation:** if any linked item changes, all downstream links are re-reviewed (§7).

## 5. Seeded Matrix (Analysis Side)

The Business→Engineering→Architecture columns are populated now (they are approved); the
remaining columns are placeholders for later phases. Rows are grouped by the primary Architecture
Driver from [28 §7](../05-architecture-input/01-architecture-input-report.md#7-architecture-drivers).

| Business Driver | Engineering Driver | Architecture Driver | Architecture Decision | Implementation | Testing (planned) | Operations (planned) |
|-----------------|--------------------|---------------------|-----------------------|----------------|-------------------|----------------------|
| Fairness (BD-1) | Hidden info (ENG-FP-01), Authoritative state | **Authoritative game state** | _TBD (arch ADR)_ | _TBD_ | Security + integration (QM-15) | Leak-audit signals (PII-free) |
| Fairness (BD-1) | Concurrency determinism (ENG-GP-01, ENG-CO-*) | **Concurrency control** | _TBD_ | _TBD_ | Concurrency tests (QM-09) | Conflict-resolution metrics |
| Fairness (BD-1) | State atomicity (ENG-ST-02) | **State management & atomicity** | _TBD_ | _TBD_ | Unit + recovery (QM-02) | Invariant-violation alerts |
| Responsive real-time (BD-2) | Msg loss/dup/order (ENG-RT-01/03) | **Real-time synchronization** | _TBD_ | _TBD_ | Chaos + E2E (QM-05/10) | Latency + resync metrics |
| No-auth/zero-friction (BD-3) | Session/identity (ENG-SE-*), reconnection (ENG-RT-03) | **Session management & reconnection** | _TBD_ | _TBD_ | Recovery + security (QM-07) | Reconnect success rate |
| Private multiplayer (BD-4) | Room lifecycle & isolation (ENG-RM-*, ENG-CO-05) | **Room lifecycle** | _TBD_ | _TBD_ | Integration + load (QM-08) | Expiry/host-migration signals |
| Fast room creation (BD-5) | Room setup path | **Room lifecycle** | _TBD_ | _TBD_ | Performance (QM-04) | Creation latency |
| Regional dictionaries (BD-6) | Version pinning (ENG-DC-02) | **Dictionary management** | _TBD_ | _TBD_ | Integration (QM-13) | Version-in-use signal |
| Resilience (BD-7) | Crash/recovery (ENG-RE-01/02) | **Recovery** | _TBD_ | _TBD_ | Chaos + recovery (QM-16) | Recovery events |
| Fairness/Correctness (BD-1) | Validation (ENG-FP-02/04, ENG-ST-01) | **Validation pipeline** | _TBD_ | _TBD_ | Unit + security (QM-15) | Rejected-intent metrics (by error) |
| Future extensibility (BD-8) | Additive seams (AUTH) | **Session/identity seam** | _TBD_ | _TBD_ | Design review (ASM-15) | n/a (MVP) |
| Scalability (BD-* growth) | Footprint/timers/fan-out (ENG-SC-*) | **Scalability** | _TBD (deferred)_ | _TBD_ | Load/stress/soak (QM-08) | Resource + cleanup metrics |

> Coverage note: `BR-*` and `INV-*` roll up under the Authoritative-state / State-management /
> Validation rows; the architecture phase must expand this into per-rule/per-invariant links to
> satisfy [ASM-01/02](04-architecture-success-metrics.md).

## 6. Per-Decision Traceability Record (Template)

Each architecture decision carries a record with this shape (populated during architecture):

```
Decision ID:            ARCH-ADR-xxx
Title:
Backward links:
  Architecture Driver(s):   [28 §7 id]
  Engineering Driver(s):    [ENG-xx-nn]
  Business Driver(s):       [28 §2 id]
  Rules/Invariants covered: [BR-*, INV-*]
Forward links:
  Implementation ref:       [to be assigned]
  Testing approach:         [QM-*, test type]
  Operations signal:        [event/metric/runbook]
Principles satisfied:       [AP-*]     Anti-principles avoided: [AAP-*]
Verification (how we know it's right): [ASM-*, checklist item 33-x]
```

## 7. Maintaining Traceability Through the Lifecycle

| Phase | Traceability action |
|-------|---------------------|
| **Business Analysis** (done) | Source drivers, rules, invariants — the anchor of all links. |
| **Engineering Analysis** (done) | Challenges linked to drivers; priorities set ([27](../04-engineering-analysis/02-engineering-challenges-enrichment.md)). |
| **Software Architecture** | Populate the *Architecture Decision* column; every decision gets a §6 record; run [33](05-architecture-review-checklist.md)/[32](04-architecture-success-metrics.md). |
| **Technical Design** | Refine decisions into design elements; extend links (design→decision). |
| **Implementation** | Populate the *Implementation* column (work-item references); keep links current per commit/change. |
| **Testing** | Populate the *Testing* column; each test suite references the driver/decision it verifies ([22](../03-business-governance/04-quality-metrics.md)). |
| **Operations** | Populate the *Operations* column; each runtime signal/runbook references what it confirms ([12](../02-business-analysis/11-domain-events-catalog.md)/[22](../03-business-governance/04-quality-metrics.md)). |

**Governance of the matrix:**
- **Owner:** the Lead Architect maintains it during architecture; each phase lead maintains its column thereafter.
- **Cadence:** reviewed at every architecture review, at design sign-off, and whenever a linked item changes (LR-6).
- **Health checks:** no orphan decisions (LR-4), no unaddressed drivers (LR-5), 100% rule/invariant/P1 coverage (LR-3).
- **Single source:** this matrix is the one place the end-to-end linkage lives; other documents reference it, not copy it (AP-07).

## 8. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial traceability framework with analysis-side seeding; later columns left as placeholders. |
