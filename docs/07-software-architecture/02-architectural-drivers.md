# 07.02 — Architectural Drivers (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Identify and prioritize every driver that shapes the architecture, why it exists, and its architectural impact. Synthesizes (does not restate) the drivers already established in analysis. |
| **Scope** | Driver identification/prioritization. No decisions, technologies, or patterns. |
| **Inputs** | [Architecture Input §2–§4](../05-architecture-input/01-architecture-input-report.md), engineering priorities. |
| **Outputs** | A ranked driver set that motivates responsibilities, boundaries, and quality scenarios. |
| **Dependencies** | Business & engineering analysis. |
| **Cross References** | [Overview](01-architecture-overview.md), [Quality Attribute Scenarios](09-quality-attribute-scenarios.md). |
| **Related Business Documents** | [BRD Objectives](../01-product-discovery/01-business-requirements.md#13-business-objectives), [Invariants](../02-business-analysis/10-business-invariants.md), [Quality Metrics](../03-business-governance/04-quality-metrics.md). |
| **Related Engineering Documents** | [Engineering Challenges](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), [Enrichment §5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary). |

---

## 1. Driver ranking

Ranked by architectural influence (1 = shapes the most structure). "Impact" = what the driver forces
the architecture to provide.

| # | Driver | Why it exists | Architectural impact |
|---|--------|---------------|----------------------|
| 1 | **Gameplay Fairness (hidden information + determinism)** | The product has no value if unfair or leakable. | Forces server authority, a single point of role-filtered delivery, and deterministic conflict resolution. Everything else bends to this. |
| 2 | **Correctness of outcomes** | An incorrect result is worthless regardless of speed. | Forces atomic state mutation, valid-only transitions, and post-reveal terminal evaluation. |
| 3 | **Deterministic State & Concurrency** | Simultaneous actions must resolve identically. | Forces a defined coordination point per room and idempotent effects; drives state ownership. |
| 4 | **Real-Time Communication** | A party game must feel instant and survive bad networks. | Forces ordering/versioning, resync, and role-filtered fan-out at a delivery boundary. |
| 5 | **Recoverability / Reliability** | Crashes and drops must not lose/corrupt matches. | Forces a recovery boundary and commit-then-broadcast; bounds durability to room lifetime. |
| 6 | **Security / Integrity (no accounts)** | Cheating and leaks must be impossible via any path. | Forces per-intent authorization from authoritative state and information segregation. |
| 7 | **Reconnection continuity** | Mobile drops are routine. | Forces transient identity, single-active-connection, grace/pause, and leak-free snapshot restore. |
| 8 | **Room isolation & bounded scale** | Growth is by room count. | Forces no cross-room mutable state and bounded per-room footprint; drives cleanup/expiry. |
| 9 | **Maintainability (one gameplay worldwide)** | One codebase, localized only by dictionary. | Forces a language-neutral rules core and a content boundary that affects words only. |
| 10 | **Testability** | The hard risks are subtle. | Forces a rules core exercisable without transport and deterministic verification seams. |
| 11 | **Observability** | Correctness must be verifiable, PII-free. | Forces business-event visibility designed in, not bolted on. |
| 12 | **Future Extensibility** | Auth/later phases must be additive. | Forces an identity seam and additive boundaries — seams, not implementations. |

## 2. Driver interactions (tensions to carry forward)

- **Fairness/Correctness vs Performance/Availability** — the former win for in-room authoritative
  state ([trade-offs](../05-architecture-input/01-architecture-input-report.md#8-major-trade-offs)).
- **Determinism vs Latency** — coordination for determinism may add latency; determinism wins.
- **Recoverability vs Simplicity** — only room-lifetime recovery is required; do not over-build
  durability.
- **Scale vs Simplicity** — keep footprints bounded now; defer scale machinery (Future Optimization).

## 3. What these drivers demand of the architecture (summary)

The architecture must provide: a **single authoritative owner** of each room's state; a
**deterministic coordination** point per room; **atomic, valid** state transitions; a **role-filtered
delivery boundary** that cannot leak; **ordering/resync** for real-time; **transient identity +
reconnection**; **recovery** to a consistent point; **room isolation**; a **language-neutral rules
core**; and **observability/testability** seams — all without changing business rules.

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial architectural drivers (discovery). |
