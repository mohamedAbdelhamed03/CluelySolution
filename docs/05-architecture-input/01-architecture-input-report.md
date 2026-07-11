# 28. Architecture Input Report — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Briefing — **input to the Software Architecture phase; not the architecture** |
| **Purpose** | The first document a software architect reads. It summarizes, prioritizes, and cross-references the approved Business Analysis (00–25) and Engineering Analysis (26–27) so architecture can begin with full context — without duplicating those documents. |
| **Technology** | Neutral (no technology, framework, project structure, or design chosen). |

## Table of Contents
1. [Executive Summary](#1-executive-summary)
2. [Business Drivers](#2-business-drivers)
3. [Engineering Drivers](#3-engineering-drivers)
4. [Quality Attribute Priorities](#4-quality-attribute-priorities)
5. [Business Constraints](#5-business-constraints)
6. [Engineering Constraints](#6-engineering-constraints)
7. [Architecture Drivers](#7-architecture-drivers)
8. [Major Trade-Offs](#8-major-trade-offs)
9. [Open Architectural Questions](#9-open-architectural-questions)
10. [Fixed Decisions](#10-fixed-decisions)
11. [Decision Matrix](#11-decision-matrix)
12. [Architecture Success Criteria](#12-architecture-success-criteria)
13. [Architecture Readiness Assessment](#13-architecture-readiness-assessment)
14. [Revision History](#14-revision-history)

> **How to use this report:** each section links to the authoritative source. Where a claim is
> made, the bracketed reference is where the detail lives. Nothing here supersedes the source
> documents; this is a prioritized briefing.

---

## 1. Executive Summary

**Product vision.** Cluely is a global, online, multiplayer word-association game that is
**functionally equivalent to Codenames**, playable with zero sign-up friction — join with a
room code and a nickname, and play. One product, one codebase, one gameplay worldwide; only
the word library is localized per region. [→ [01 BRD](../01-product-discovery/01-business-requirements.md), [24 Roadmap](../03-business-governance/06-product-roadmap.md)]

**Core gameplay.** Two teams (Red/Blue) share a fixed 5×5 board of 25 word cards partitioned
9/8/7/1 (starting-team agents / second-team agents / neutral / assassin). Each team has one
**Spymaster** (sees the secret key, gives a one-word clue + a number) and one or more
**Operatives** (see only words, guess cards). Guessing your own agent lets you continue (up to
number+1 guesses; 0/unlimited → unbounded, min 1); a neutral or opponent card ends the turn;
the assassin loses the match instantly. First team to reveal all its agents wins. [→ [03 Business Rules](../02-business-analysis/02-business-rules.md), [19 Glossary](../03-business-governance/01-business-glossary.md), [21 Constants](../03-business-governance/03-business-constants-catalog.md)]

**Product scope.** Real-time multiplayer; private rooms via room code; temporary nicknames (no
accounts); team/role setup; the full turn loop; disconnect/reconnect with grace; deterministic
host migration; regional dictionaries; rematch; room lifecycle & expiry. [→ [01 §1.5](../01-product-discovery/01-business-requirements.md#15-scope-in-scope), [15 Lobby & Room Lifecycle](../02-business-analysis/14-lobby-room-lifecycle.md)]

**MVP scope.** Exactly the above — a faithful, no-auth, private-room experience that a group of
4+ friends can play end-to-end, globally, with only the dictionary localized. Delivery context
(informational only) is a backend service and a mobile client; **no business rule depends on
that choice.** [→ [01 §1.5](../01-product-discovery/01-business-requirements.md#15-scope-in-scope), [20 ADRs](../03-business-governance/02-architecture-decision-records.md)]

**Out of scope.** Authentication, accounts, profiles; monetization; AI of any kind; ranking,
leaderboards, achievements; chat/voice/social features; spectators; late joining into an
in-progress match; new game modes; custom user word lists. All are deferred or excluded. [→ [01 §1.6](../01-product-discovery/01-business-requirements.md#16-out-of-scope), [24 Roadmap](../03-business-governance/06-product-roadmap.md)]

**Current project maturity.** The Business Analysis is **complete and approved** (BRD, SRS,
Business Rules, Functional Requirements, User Stories, Use Cases, Domain Model, State Machines,
Workflows, Validation Rules), consolidated by a governance layer (Glossary, ADRs, Constants
Catalog, Quality Metrics, Data Lifecycle, Roadmap) and validated for internal consistency
(two consistency reports found **0 critical contradictions**; a few advisory wording items
remain, none blocking). A pre-architecture **Engineering Challenges & Risk Analysis** (42
challenges) and an **enrichment layer** (difficulty, RPN, testing, MVP applicability, patterns,
drivers, priority table) are complete. The project is at the **boundary between analysis and
architecture** — this report is the bridge. [→ [18 Consistency Report](../02-business-analysis/17-consistency-validation-report.md), [25 Governance Summary](../03-business-governance/07-governance-validation-summary.md), [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)/[27](../04-engineering-analysis/02-engineering-challenges-enrichment.md)]

**One-paragraph mental model for the architect.** Cluely is a *server-authoritative,
hidden-information, real-time, room-isolated* multiplayer game. The hard parts are not the
rules (they are fully specified and simple) but *enforcing them correctly under concurrency,
unreliable networks, and reconnection while never leaking hidden information*. Everything the
architecture must protect is captured as **business invariants** [→ [11](../02-business-analysis/10-business-invariants.md)]
and **rule precedence** [→ [17](../02-business-analysis/16-rule-precedence.md)].

---

## 2. Business Drivers

Ranked by architectural influence (1 = highest).

| # | Driver | Why it matters (architectural influence) | Source |
|---|--------|------------------------------------------|--------|
| 1 | **Gameplay fairness** | The entire product depends on hidden information never leaking and outcomes being provably correct/deterministic. Shapes state ownership, delivery filtering, and concurrency. | [BO-1](../01-product-discovery/01-business-requirements.md#13-business-objectives), [INV-B9](../02-business-analysis/10-business-invariants.md), [17](../02-business-analysis/16-rule-precedence.md) |
| 2 | **Responsive real-time gameplay** | A party game must feel instant; drives the real-time synchronization and latency budgets. | [NFR-1](../02-business-analysis/01-software-requirements.md#29-non-functional-requirements), [QM-05](../03-business-governance/04-quality-metrics.md) |
| 3 | **No authentication in MVP (zero friction)** | Removes the biggest drop-off; forces a temporary-identity model and a clean future-auth seam. | [BO-2](../01-product-discovery/01-business-requirements.md#13-business-objectives), [ADR-02](../03-business-governance/02-architecture-decision-records.md) |
| 4 | **Private multiplayer (code-based rooms)** | Rooms are the unit of isolation and scaling; drives room lifecycle and access model. | [ADR-01](../03-business-governance/02-architecture-decision-records.md), [15](../02-business-analysis/14-lobby-room-lifecycle.md) |
| 5 | **Fast room creation & join** | Onboarding must be near-instant; influences how rooms/codes/state are established. | [QM-04](../03-business-governance/04-quality-metrics.md) |
| 6 | **Regional dictionaries (one gameplay)** | Localization is data-only; drives a content-provider boundary that can never affect rules. | [BO-4](../01-product-discovery/01-business-requirements.md#13-business-objectives), [14](../02-business-analysis/13-dictionary-management.md), [INV-D1](../02-business-analysis/10-business-invariants.md) |
| 7 | **Resilience to mobile networks** | Drops are routine; drives reconnection, grace, and host-migration handling. | [R-1](../01-product-discovery/01-business-requirements.md#19-risks), [16](../02-business-analysis/15-player-session-reconnection.md) |
| 8 | **Future extensibility** | Auth, profiles, stats, matchmaking are deferred but must remain *additive*; drives seams and boundaries. | [ADR-02](../03-business-governance/02-architecture-decision-records.md), [24](../03-business-governance/06-product-roadmap.md) |

---

## 3. Engineering Drivers

The challenges with the greatest architectural impact (full analysis in [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), prioritized in [27](../04-engineering-analysis/02-engineering-challenges-enrichment.md)). Not a repeat of all 42.

| Driver | Why it matters | Key challenge IDs |
|--------|----------------|-------------------|
| **Authoritative state & atomicity** | Every rule is enforced centrally; multi-field mutations (reveal → counts → terminal → turn) must be all-or-nothing or state corrupts. | ENG-ST-01, ENG-ST-02, ENG-GP-05 |
| **Concurrency determinism** | Simultaneous guesses/joins/leaves/host actions must resolve identically every time (first-valid-wins). | ENG-GP-01, ENG-CO-01/02/04, [17](../02-business-analysis/16-rule-precedence.md) |
| **Real-time synchronization** | Lost/duplicated/out-of-order messages and slow clients must never desync or double-apply. | ENG-RT-01/02/03 |
| **Hidden-information protection** | A single leaked delivery path destroys the game; the most existential risk. | ENG-FP-01, ENG-ST-03 |
| **Reconnection & session continuity** | Brief drops must not end matches; role-appropriate state must be restored without leaks. | ENG-RT-03, ENG-CO-04, ENG-SE-02/03 |
| **Reliability & recovery** | Crashes/interruptions mid-operation must recover to a consistent point for the room's lifetime. | ENG-RE-01/02, ENG-ST-04 |
| **Room lifecycle & isolation** | Rooms are independent units; cleanup and expiry must be race-safe and not close live rooms. | ENG-CO-05, ENG-RM-01/02 |
| **Scalability of many small rooms** | Growth is by room count; per-room footprint, timers, fan-out, and cleanup must stay bounded. | ENG-SC-01/02/03/04 |

---

## 4. Quality Attribute Priorities

Ranked. When two conflict, the **higher-ranked attribute wins** — this ordering is the
tie-breaker the architecture should encode.

| Rank | Attribute | Reasoning / precedence |
|------|-----------|------------------------|
| 1 | **Correctness** | Rules and outcomes must be exactly right; nothing is worth an incorrect result. |
| 2 | **Gameplay Fairness** | Hidden information + determinism; the product has no value if unfair. Beats performance/availability. |
| 3 | **Consistency** | Single authoritative truth; no contradictory client views. Prefer consistency over raw availability for in-room state. |
| 4 | **Reliability / Recoverability** | Matches survive drops/crashes within the room's life. |
| 5 | **Security (integrity)** | Server authority, action authorization, no info leakage (no accounts/PII in MVP). |
| 6 | **Performance / Latency** | Real-time feel — important, but never at the expense of 1–5. |
| 7 | **Availability** | High, but for in-progress room state, correctness/consistency take precedence over availability. |
| 8 | **Testability** | The system must be verifiable (concurrency/chaos/security testing) — a first-class concern given the risks. |
| 9 | **Maintainability** | One codebase, one gameplay; regional differences isolated to data. |
| 10 | **Extensibility** | Future-auth and later phases must be additive (seams, not rewrites). |
| 11 | **Observability** | Enough business-event/result visibility to verify correctness — PII-free. |
| 12 | **Scalability** | Matters at growth; keep footprints bounded now, don't pre-scale. |
| 13 | **Operational Simplicity** | Favor the simplest option that satisfies 1–5 for the MVP. |

> Precedence rule of thumb: **Correctness > Fairness > Consistency > Reliability > everything
> else.** [→ [22 Quality Metrics](../03-business-governance/04-quality-metrics.md), [17 Rule Precedence](../02-business-analysis/16-rule-precedence.md)]

---

## 5. Business Constraints

Architecture must respect all of these; they are **not negotiable**. (Summary — authoritative
in the linked docs.)

- Business rules cannot change; gameplay must remain **faithful to Codenames**. [→ [03](../02-business-analysis/02-business-rules.md), [ADR-14](../03-business-governance/02-architecture-decision-records.md)]
- **One gameplay worldwide**, one codebase; rules are **language-independent**. [→ [C-3/C-5/C-6](../01-product-discovery/01-business-requirements.md#17-constraints), [INV-D1](../02-business-analysis/10-business-invariants.md)]
- **Regional dictionaries only** are localized; content is versioned/immutable; no user-supplied words. [→ [14](../02-business-analysis/13-dictionary-management.md)]
- **Temporary identities only** — no accounts/registration/login/profiles; PII-free. [→ [ADR-02/03](../03-business-governance/02-architecture-decision-records.md), [16](../02-business-analysis/15-player-session-reconnection.md)]
- **Private rooms only**, joined by room code; no matchmaking/discovery; **no spectators**; **no late joining** into an active match. [→ [ADR-01/06/07](../03-business-governance/02-architecture-decision-records.md), [15](../02-business-analysis/14-lobby-room-lifecycle.md)]
- Structural invariants that must always hold: exactly **25 cards**, **9/8/7/1** partition, **1 assassin**, **2 teams**, **1 Host per room**, **1 Spymaster per team**, **1 active turn**, **1 active clue**, **1 authoritative game state**, immutable ownership after generation, one-way reveals, exactly one winner per completed match. [→ [11 Invariants](../02-business-analysis/10-business-invariants.md)]
- Fixed constants (board/composition/guessing) must not change; operational parameters are tunable within stated ranges only. [→ [21 Constants](../03-business-governance/03-business-constants-catalog.md)]
- Future authentication must be **additive** — no rule/workflow change required to add it. [→ [SRS §2.14](../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations)]

---

## 6. Engineering Constraints

Discovered/confirmed during Engineering Analysis; architecture must honor them (no solutions
proposed here). [→ [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), [11](../02-business-analysis/10-business-invariants.md), [17](../02-business-analysis/16-rule-precedence.md)]

- **Concurrency must be deterministic** — near-simultaneous actions resolve identically (first-valid-wins). [ENG-GP-01, [17 §8](../02-business-analysis/16-rule-precedence.md)]
- **Hidden information must never leak** — unrevealed ownership never reaches non-Spymasters on any path (initial, delta, reconnect, rematch, telemetry). [ENG-FP-01, INV-B9]
- **Duplicate/replayed commands must not corrupt state** — state-changing intents must be effectively idempotent. [ENG-GP-02, ENG-FP-03, CR-4]
- **State transitions must always be valid** — undefined transitions are rejected by default. [ENG-ST-01, [08](../02-business-analysis/07-state-machines.md)]
- **State mutations must be atomic** — no partial multi-field updates; never broadcast half-applied state. [ENG-ST-02]
- **Game state must remain recoverable** to the last consistent point within the room's lifetime. [ENG-ST-04, ENG-RE-01]
- **Reconnect must preserve gameplay** — restore exact team/role/view and resume paused phases within grace. [ENG-RT-03, INV-P5]
- **Ordering/terminal precedence must be deterministic** — assassin > victory > turn-end > host-migration > room-expiration. [17](../02-business-analysis/16-rule-precedence.md)]
- **One active connection per identity** — newest supersedes; no dual actors. [ENG-CO-04, INV-P4]
- **Room isolation** — rooms share no mutable state; expiry is race-safe. [ENG-CO-05, ENG-RM-01]

---

## 7. Architecture Drivers

Problems the architecture must explicitly solve. Priority uses the [27](../04-engineering-analysis/02-engineering-challenges-enrichment.md) RPN banding (P1 highest).

| Driver | Why it exists | Business impact | Engineering impact | Priority |
|--------|---------------|-----------------|--------------------|----------|
| **Authoritative game state** | Rules & hidden info require a single source of truth. | Fairness, correct outcomes. | Central state ownership; role-filtered projections. | P1 |
| **State management & atomicity** | Multi-field mutations must be consistent. | No corrupt/illegal states. | Atomic apply; invariant checks; valid-transition-only. | P1 |
| **Concurrency control** | Simultaneous actions across gameplay/room/session. | Deterministic fairness. | Serialization / conflict resolution; idempotency. | P1 |
| **Real-time synchronization** | Unreliable networks, many participants. | Responsive, consistent play. | Ordering/versioning, resync, filtered delivery. | P1 |
| **Hidden-information protection** | The game's core secret. | Existential fairness. | Server-only ownership; audited delivery paths. | P1 |
| **Session management & reconnection** | No accounts; mobile drops. | Matches survive; no leaks. | Transient identity, tokens, grace, single connection. | P1/P2 |
| **Recovery** | Crashes/interruptions happen. | No lost/corrupted matches. | Recoverable state; idempotent recovery boundary. | P1/P2 |
| **Validation pipeline** | Every intent must be checked. | Consistent, clear rejections. | Uniform authorization + validation before effects. | P2 |
| **Room lifecycle** | Create→play→rematch→expire. | Reliable rooms; clean reclaim. | Race-safe lifecycle, host migration, expiry. | P2 |
| **Dictionary management** | Localized, versioned content. | Culturally natural, fair. | Content boundary; pin version per match. | P2/P3 |
| **Scalability** | Growth by room count. | Sustained experience at scale. | Bounded per-room resources; efficient timers/fan-out/cleanup. | P3 (Future Optimization) |

---

## 8. Major Trade-Offs

To be **evaluated** during architecture (not resolved here). For each, the report notes which
quality attribute (from §4) should generally dominate given Cluely's priorities.

| Trade-off | Tension | Cluely-specific note |
|-----------|---------|----------------------|
| **Consistency vs Availability** | Strong in-room consistency may reduce availability during faults. | For in-progress room state, **consistency/fairness win** (§4). |
| **Latency vs Reliability** | Guarantees (ordering, idempotency, recovery) add overhead. | Real-time feel matters, but **not at the cost of correctness**. |
| **Memory vs Persistence** | In-memory speed vs recoverable durable state. | Only **room-lifetime** recoverability is required — avoid over-engineering durability. [ENG-RE-01, [23](../03-business-governance/05-data-lifecycle-retention.md)] |
| **Performance vs Simplicity** | Optimizations add complexity. | MVP favors **operational simplicity** that still meets §4 ranks 1–5. |
| **Scalability vs Operational Complexity** | Scaling patterns add moving parts. | Keep footprints bounded now; **defer scale complexity** (Future Optimization). |
| **Flexibility vs Maintainability** | Generality vs a focused, single-gameplay codebase. | One gameplay worldwide favors **maintainability/consistency**. |
| **Immediate Delivery vs Future Extensibility** | MVP speed vs seams for auth/later phases. | Extensibility must be **additive seams**, not upfront build. [ADR-02, [24](../03-business-governance/06-product-roadmap.md)] |
| **Full-state vs Delta sync** | Simplicity/robustness vs bandwidth. | Open cross-cutting question. [ENG-RT-01, [26 §16](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md#16-cross-cutting-open-questions)] |

---

## 9. Open Architectural Questions

Architecture must answer these; this report only records them (consolidated from [26 §16](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md#16-cross-cutting-open-questions) and [27 §5.4](../04-engineering-analysis/02-engineering-challenges-enrichment.md#54-challenges-that-require-architectural-decisions-gate-the-architecture-phase)).

1. How is **authoritative game state** owned, mutated atomically, and projected per role?
2. How are **concurrent actions coordinated** deterministically (serialization vs optimistic concurrency)? [ENG-GP-01, ENG-CO-*]
3. How are **rooms isolated** and their state kept independent and reclaimable? [ENG-SC-01]
4. How does **reconnection** deliver a correct, fast, role-filtered snapshot and resume pauses? [ENG-RT-03]
5. How is **hidden information** guaranteed never to leave the server for non-Spymasters, across *all* delivery paths? [ENG-FP-01]
6. How is **message ordering/exactly-once effect** guaranteed under loss/dup/reorder? [ENG-RT-01]
7. **Full-state vs delta** synchronization, and how a client detects it is behind? [ENG-RT-01/03]
8. What is the **recovery/durability boundary** for in-progress matches (room-lifetime recoverability without long-term storage)? [ENG-RE-01]
9. How are **timers/expiry** managed efficiently at scale, with reset-on-activity semantics? [ENG-SC-02, ENG-CO-05]
10. How are **dictionary versions loaded, pinned per match, and retained** while in use? [ENG-DC-02, [14](../02-business-analysis/13-dictionary-management.md)]
11. How is **single-active-connection per identity** enforced? [ENG-CO-04]
12. How is the **validation/authorization pipeline** structured so every intent is checked before any effect? [ENG-FP-02/04]

**Questions needing business/parameter clarification (route to BA/PO, not rule changes):**
default **grace values** & whether mass-disconnect grace differs; **neutral normalization
policy** for multi-script dictionaries; appetite for **auditable/seeded** board generation;
**deprecated-version retention** windows. [→ [27 §5.5](../04-engineering-analysis/02-engineering-challenges-enrichment.md#55-challenges-that-require-business-clarification-route-to-the-baproduct-owner)]

---

## 10. Fixed Decisions

Finalized; **not open for architecture to change**. [→ [20 ADRs](../03-business-governance/02-architecture-decision-records.md), [11 Invariants](../02-business-analysis/10-business-invariants.md), [21 Constants](../03-business-governance/03-business-constants-catalog.md)]

- **Gameplay & business rules** — faithful Codenames; counts, turn flow, win/loss, precedence. [03, 11, 17]
- **Board composition** — 25 cards, 9/8/7/1, one assassin, two teams. [ADR-14, 21]
- **Roles** — exactly one Host per room; exactly one Spymaster per team. [ADR-09/10]
- **Dictionary strategy** — regional, versioned, immutable; localization is data-only; no user words. [ADR-04/11]
- **Language independence** — no rule depends on natural language. [ADR-12]
- **Identity** — temporary, room-scoped, PII-free; no auth in MVP; future-auth additive. [ADR-02/03]
- **Access model** — private rooms + code; no matchmaking, spectators, or late joining. [ADR-01/06/07]
- **Server authority** — all adjudication server-side; clients submit intents only. [ADR-08]
- **MVP scope**, **business terminology** (canonical Glossary), and **business invariants**. [01, 19, 11]
- **Technology-neutral business documentation** — implementation choices belong to architecture, not the specs. [ADR-13]

---

## 11. Decision Matrix

| Decision | Status | Owner | Reason | Architecture may change it? |
|----------|--------|-------|--------|------------------------------|
| Gameplay & business rules | **Fixed** | Product / BA | Faithful Codenames; approved SoT | **No** |
| Board composition (9/8/7/1, 25, assassin) | **Fixed** | Product / BA | Faithful mechanics [ADR-14] | **No** |
| Roles (1 Host, 1 Spymaster/team) | **Fixed** | Product / BA | Faithful + control model [ADR-09/10] | **No** |
| Dictionary strategy (regional, versioned) | **Fixed** | Content / PO | Localization = data-only [ADR-04/11] | **No** (may design *how* to load/pin) |
| Temporary identities / no MVP auth | **Fixed** | Product | Zero friction [ADR-02/03] | **No** (must keep future-auth seam) |
| Private rooms / no spectators / no late join | **Fixed** | Product | Faithful social model [ADR-01/06/07] | **No** |
| Server-authoritative state | **Fixed** | Product / Eng | Fairness & integrity [ADR-08] | **No** (must design *how*) |
| MVP scope & business terminology | **Fixed** | Product / BA | Approved [01, 19] | **No** |
| Authentication & later phases | **Future** | Product | Deferred [24] | Not now (design additive seams only) |
| **Technology stack** | **Open** | Architecture | To be decided | **Yes** |
| **Concurrency strategy** | **Open** | Architecture | Determinism required, method open [ENG-GP-01] | **Yes** |
| **State management approach** | **Open** | Architecture | Atomicity required, method open [ENG-ST-02] | **Yes** |
| **Persistence / recovery strategy** | **Open** | Architecture | Room-lifetime recoverability required [ENG-RE-01] | **Yes** |
| **Real-time communication design** | **Open** | Architecture | Ordering/resync required [ENG-RT-01] | **Yes** |
| **Room isolation & scaling model** | **Open** | Architecture | Bounded, isolated rooms [ENG-SC-01] | **Yes** |
| **Session/reconnection mechanism** | **Open** | Architecture | Preserve gameplay & filtering [ENG-RT-03] | **Yes** |
| **Deployment / infrastructure** | **Open** | Architecture / Ops | Out of analysis scope | **Yes** |

---

## 12. Architecture Success Criteria

A successful architecture must achieve the following; each maps to an approved requirement.

| Criterion | Why it exists | Source |
|-----------|---------------|--------|
| **Business rules fully enforceable server-side** | Correctness/fairness are non-negotiable. | [03](../02-business-analysis/02-business-rules.md), [ADR-08](../03-business-governance/02-architecture-decision-records.md) |
| **Deterministic gameplay** | Same inputs → same outcome; provable fairness. | [17](../02-business-analysis/16-rule-precedence.md), [QM-09](../03-business-governance/04-quality-metrics.md) |
| **Hidden information never leaks** | The game's existential requirement. | [INV-B9](../02-business-analysis/10-business-invariants.md), [QM-15](../03-business-governance/04-quality-metrics.md) |
| **Reliable real-time synchronization** | Responsive, consistent multiplayer under real networks. | [NFR-1/2](../02-business-analysis/01-software-requirements.md#29-non-functional-requirements), [QM-05/10](../03-business-governance/04-quality-metrics.md) |
| **Reconnection preserves gameplay** | Mobile drops must not end matches. | [16](../02-business-analysis/15-player-session-reconnection.md), [QM-07](../03-business-governance/04-quality-metrics.md) |
| **Recoverable state (room-lifetime)** | No lost/corrupted matches on interruption. | [QM-16](../03-business-governance/04-quality-metrics.md) |
| **Future authentication supported additively** | Protects the roadmap without rework. | [SRS §2.14](../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations), [24](../03-business-governance/06-product-roadmap.md) |
| **Localization is data-only** | One gameplay worldwide; new regions add without rule changes. | [INV-D1](../02-business-analysis/10-business-invariants.md), [QM-13](../03-business-governance/04-quality-metrics.md) |
| **Simple MVP delivery** | Avoid over-engineering; ship the MVP. | [27 §5.3](../04-engineering-analysis/02-engineering-challenges-enrichment.md#53-challenges-that-can-be-safely-postponed-avoid-over-engineering-now) |
| **High testability** | The risk profile demands concurrency/chaos/security verification. | [26](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), [27 §3](../04-engineering-analysis/02-engineering-challenges-enrichment.md) |
| **Scalable evolution** | Grow by room count without redesign. | [SCAL-1](../02-business-analysis/01-software-requirements.md#213-scalability-considerations), [QM-08](../03-business-governance/04-quality-metrics.md) |

---

## 13. Architecture Readiness Assessment

| Dimension | Assessment | Notes |
|-----------|------------|-------|
| **Business completeness** | ✅ Complete | Full BA package (00–10) + governance (19–25); validated twice. |
| **Requirement clarity** | ✅ Clear | Rules, validations, invariants, events, errors, precedence all specified with IDs. |
| **Terminology & constants** | ✅ Single-sourced | Canonical [Glossary (19)](../03-business-governance/01-business-glossary.md) and [Constants (21)](../03-business-governance/03-business-constants-catalog.md). |
| **Engineering risks** | ✅ Identified & prioritized | 42 challenges with RPN, difficulty, MVP applicability [26/27]. |
| **Open questions** | ⚠️ Known & bounded | All are *architectural* or *parameter* questions (§9), none are rule ambiguities. |
| **Missing information** | ⚠️ Minor, non-blocking | A few advisory wording tightenings ([18 §8](../02-business-analysis/17-consistency-validation-report.md#8-recommendations-roll-up)) and parameter values (grace defaults, normalization policy) — resolvable in parallel with early architecture. |
| **Decision readiness** | ✅ Fixed vs open is explicit | [Decision Matrix (§11)](#11-decision-matrix) clearly separates the two. |

**Final recommendation:** **Proceed to the Software Architecture phase.** The business is
complete, internally consistent, and technology-neutral; engineering risks are enumerated and
prioritized; and fixed-vs-open decisions are unambiguous. The remaining items are *architectural
questions to be answered by the architecture itself* (§9) plus a small set of *parameter/policy
clarifications* that the BA/Product Owner can resolve in parallel and that do **not** block
starting. Recommended first focus for the architect: the **P1 cluster** — authoritative state &
atomicity, deterministic concurrency, real-time synchronization, hidden-information protection,
reconnection, and recovery ([27 §5.1/5.4](../04-engineering-analysis/02-engineering-challenges-enrichment.md#5-architects-focus-summary)).

## 14. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial Architecture Input Report bridging Business/Engineering analysis to the Architecture phase. Summary/prioritization only; no architecture or technology decisions. |
