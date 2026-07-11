# 07.10 — Architecture Readiness Review (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery review |
| **Purpose** | Review the outputs of the discovery phase (documents 01–09) and judge whether the project is ready to proceed to the *design* work of Software Architecture. Lists unresolved questions and gives a recommendation. |
| **Scope** | Self-assessment of discovery completeness. No design decisions. |
| **Inputs** | Discovery documents 01–09. |
| **Outputs** | A readiness verdict + prioritized open questions for the architecture design work. |
| **Dependencies** | All discovery docs; [Architecture Governance](../06-architecture-governance/README.md). |
| **Cross References** | [Handoff Summary](11-analysis-to-architecture-handoff.md). |
| **Related Business Documents** | [Consistency Report](../02-business-analysis/17-consistency-validation-report.md). |
| **Related Engineering Documents** | [Enrichment §5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary). |

---

## 1. Completeness assessment

| Question | Verdict | Evidence |
|----------|---------|----------|
| Have all responsibilities been identified? | ✅ Yes | [R-01…R-17](03-system-responsibilities.md) cover room, lobby, match, rules, board, dictionary, turn, clue, guess, validation, delivery, connection, identity, store, recovery, cleanup, observability. |
| Are boundaries clear? | ✅ Yes | Cohesion clusters + hard isolation rules in [04](04-responsibility-boundaries.md). |
| Are ownership rules complete? | ✅ Yes | Single-writer ownership for S-01…S-10 in [05](05-state-ownership.md). |
| Are consistency requirements understood? | ✅ Yes | CB-01…CB-10 with strong/eventual classification in [06](06-consistency-boundaries.md). |
| Are architectural drivers complete & ranked? | ✅ Yes | 12 ranked drivers in [02](02-architectural-drivers.md), traceable to analysis. |
| Are command/query needs understood? | ✅ Yes | Classification with consistency needs in [07](07-command-query-discovery.md). |
| Are interactions & ordering/timeout needs understood? | ✅ Yes | I-01…I-10 with retry/ordering/timeout in [08](08-interaction-discovery.md). |
| Are quality attributes measurable? | ✅ Yes | QS-01…QS-14 scenarios in [09](09-quality-attribute-scenarios.md). |
| Are major unknowns identified? | ✅ Yes | §2 below. |

## 2. Unresolved questions (for the architecture design work)

These are **architectural design decisions** (the "how"), deliberately left open by discovery.
Carried from [Architecture Input §9](../05-architecture-input/01-architecture-input-report.md#9-open-architectural-questions)
and [Engineering §16](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md#16-cross-cutting-open-questions),
now sharpened by this decomposition:

| # | Open question | Anchored in |
|---|---------------|-------------|
| Q1 | How is per-room **serialization/coordination** realized to guarantee determinism (CB-01/02/03)? | I-01, RP |
| Q2 | How is **authoritative state custody + atomic commit + recovery** realized within room lifetime (S-09, CB-03/09)? | ENG-RE-01 |
| Q3 | **Full-state vs delta** synchronization, and how clients detect they are behind (I-08)? | ENG-RT-01 |
| Q4 | How is the **role-filtered delivery boundary** structured so leakage is impossible on every path (QS-01)? | ENG-FP-01 |
| Q5 | How is **single-active-connection + reconnect snapshot** realized (CB-08, I-07)? | ENG-CO-04 |
| Q6 | How are **rooms isolated and scaled** (bounded footprint, timers) without cross-room state (QS-10)? | ENG-SC-* |
| Q7 | Whether/how to separate **commands from queries / read models** ([07](07-command-query-discovery.md))? | discovery |
| Q8 | How is **dictionary version pinning + retention while in use** realized (S-08)? | ENG-DC-02 |

**Parameter/policy clarifications (route to BA/PO, not design):** default grace values & mass-drop
grace; neutral clue-normalization policy for multi-script dictionaries; auditable/seeded generation
appetite; deprecated-version retention windows. *(These do not block design.)*

## 3. Risks to carry into design

The highest-risk responsibilities (from [Enrichment §5.6](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary))
map to discovery items: **Rules & Play atomicity** (R-04, CB-01/03), **Delivery filtering** (R-11,
QS-01), **Custody & Recovery** (R-14/15, QS-06), **Connectivity** (R-12, CB-08). These deserve the
first and deepest design attention and are tracked in the
[Architecture Risk Register](../06-architecture-governance/07-architecture-risk-register.md).

## 4. Recommendation

**Proceed to Software Architecture (design).** The discovery phase has:
- identified all responsibilities and their ownership;
- defined cohesion/isolation and consistency boundaries;
- classified commands/queries and their consistency needs;
- described interactions with ordering/retry/timeout requirements;
- expressed measurable quality-attribute scenarios; and
- enumerated the open **design** questions and highest-risk areas.

No business ambiguity blocks design; the remaining unknowns are exactly the decisions the design
work exists to make, and the parameter clarifications can proceed in parallel. Design should begin
with the **P1 cluster**: Rules & Play atomicity/determinism, role-filtered delivery, custody &
recovery, and reconnection — under the [Architecture Governance](../06-architecture-governance/README.md)
(record each decision as an architecture ADR; validate against the
[Review Checklist](../06-architecture-governance/05-architecture-review-checklist.md) and
[Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md)).

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial architecture readiness review (discovery). |
