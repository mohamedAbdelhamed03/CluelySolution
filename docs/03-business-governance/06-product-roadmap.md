# 24. Product Roadmap — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review — **strategic; does not change the MVP** |
| **Purpose** | Document the intended future evolution of Cluely **without altering the approved MVP**. It separates **Current Scope** (built and specified in documents 00–23) from **Future Scope** (deferred), so stakeholders share a direction without scope creep. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Ground Rules](#1-purpose--ground-rules)
2. [References](#2-references)
3. [Scope Boundary: Current vs Future](#3-scope-boundary-current-vs-future)
4. [Phase 1 — Current MVP](#4-phase-1--current-mvp-current-scope)
5. [Phase 2 — Accounts & Continuity](#5-phase-2--accounts--continuity-future)
6. [Phase 3 — Progression & Recognition](#6-phase-3--progression--recognition-future)
7. [Phase 4 — Public Matchmaking & Competitive](#7-phase-4--public-matchmaking--competitive-future)
8. [Phase 5 — Platform Expansion](#8-phase-5--platform-expansion-future)
9. [Cross-Phase Dependencies](#9-cross-phase-dependencies)
10. [Guardrails](#10-guardrails)
11. [Revision History](#11-revision-history)

---

## 1. Purpose & Ground Rules

This roadmap is **directional only**. Nothing here modifies gameplay, rules, or the MVP. Each
future phase lists Business Goals, Features, Dependencies, Business Value, Risks, and Success
Criteria. Everything beyond Phase 1 is explicitly **out of the current scope** ([BRD §1.6](../01-product-discovery/01-business-requirements.md#16-out-of-scope))
and must pass its own product/BA governance before work begins.

## 2. References
- [01 — BRD](../01-product-discovery/01-business-requirements.md) (scope/out-of-scope), [02 — SRS §2.14](../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations)
- [20 — ADRs](02-architecture-decision-records.md), [23 — Data Lifecycle §6](05-data-lifecycle-retention.md#6-privacy--future-authentication-impact)

## 3. Scope Boundary: Current vs Future

| | Current Scope (Phase 1) | Future Scope (Phases 2–5) |
|---|---|---|
| Identity | Temporary, no accounts | Durable accounts, profiles |
| Access | Private rooms + code | + Public matchmaking |
| Persistence | Room-bounded, PII-free | Match history, stats |
| Recognition | None | Leaderboards, achievements |
| Platforms | Mobile (planned) | Additional platforms |
| Gameplay | Faithful Codenames (fixed) | **Unchanged** — never modified by roadmap |

> **The gameplay column never changes.** Future phases add *surrounding* capabilities, not new
> mechanics ([ADR-05/14](02-architecture-decision-records.md)).

## 4. Phase 1 — Current MVP *(Current Scope)*

- **Status:** Specified in documents 00–23; the approved baseline.
- **Business Goals:** Deliver faithful, no-signup, private-room Codenames online; global via localized dictionaries.
- **Features:** Private rooms + room code; temporary nicknames; team/role setup; faithful match loop; disconnect/reconnect; host migration; regional dictionaries; rematch.
- **Dependencies:** Curated regional dictionaries; server-authoritative engine ([ADR-08](02-architecture-decision-records.md)).
- **Business Value:** Authentic experience with the lowest possible friction (BO-1/BO-2).
- **Risks:** Disconnect handling, abandoned rooms, content appropriateness — all mitigated in [BRD §1.9](../01-product-discovery/01-business-requirements.md#19-risks).
- **Success Criteria:** [BRD §1.10](../01-product-discovery/01-business-requirements.md#110-success-criteria) (SC-1..SC-7).

## 5. Phase 2 — Accounts & Continuity *(Future)*

- **Business Goals:** Let players carry an identity across sessions and reconnect with friends.
- **Features:** Authentication; persistent profiles; friends; match history.
- **Dependencies:** The identity seam (AUTH-1..5) already designed into the MVP; the transient identity/Reconnect Token model ([16](../02-business-analysis/15-player-session-reconnection.md)); a future privacy/retention policy ([23 §6](05-data-lifecycle-retention.md#6-privacy--future-authentication-impact)).
- **Business Value:** Retention, re-engagement, social continuity — the natural first expansion.
- **Risks:** Privacy/data obligations begin here; must not add friction to casual play (keep guest play available); must not alter gameplay.
- **Success Criteria:** Accounts added with **zero** changes to core rules/workflows (validates AUTH-4); guest/no-account play still possible; history reconstructable from existing PII-free records plus new account links.

## 6. Phase 3 — Progression & Recognition *(Future)*

- **Business Goals:** Reward continued play.
- **Features:** Statistics; leaderboards; achievements.
- **Dependencies:** Phase 2 (durable identity + history) — recognition requires persistent players.
- **Business Value:** Engagement and competitiveness for returning players.
- **Risks:** Could distort casual spirit; must remain optional and outside core rules ([ADR-05](02-architecture-decision-records.md)); explicitly excluded from MVP ([BRD §1.6](../01-product-discovery/01-business-requirements.md#16-out-of-scope)).
- **Success Criteria:** Progression features layer on top without touching match adjudication; opt-in; no gameplay change.

## 7. Phase 4 — Public Matchmaking & Competitive *(Future)*

- **Business Goals:** Let strangers find balanced games; support competitive play.
- **Features:** Public matchmaking; competitive/ranked modes (as governed additions, not rule changes to the base game).
- **Dependencies:** Phases 2–3 (identity, stats, ranking); moderation and abuse-handling capabilities; reverses [ADR-01](02-architecture-decision-records.md) deliberately and with governance.
- **Business Value:** Broadens the audience beyond existing friend groups.
- **Risks:** Moderation load, fairness/abuse, matchmaking quality; must not compromise the hidden-information model (INV-B9) or determinism ([17](../02-business-analysis/16-rule-precedence.md)).
- **Success Criteria:** Matchmaking produces valid rooms that run the **unchanged** faithful game; moderation policy in place.

## 8. Phase 5 — Platform Expansion *(Future)*

- **Business Goals:** Reach players on more surfaces.
- **Features:** Additional client platforms beyond the initial mobile target.
- **Dependencies:** The technology-neutral business core ([ADR-13](02-architecture-decision-records.md)) and server-authoritative model ([ADR-08](02-architecture-decision-records.md)) that already decouple rules from any client.
- **Business Value:** Wider reach and cross-platform play.
- **Risks:** Consistency across platforms; must reuse the same authoritative engine (no divergent rules).
- **Success Criteria:** New platforms consume the same rules/events with identical outcomes (QM-13 parity).

## 9. Cross-Phase Dependencies

```
Phase 1 (MVP) ──► Phase 2 (Accounts) ──► Phase 3 (Progression) ──► Phase 4 (Matchmaking/Competitive)
     │                                                                     
     └───────────────────────────────────────────────────► Phase 5 (Platform Expansion)
```

- Phase 2 unlocks Phases 3 and 4 (persistent identity is a prerequisite for stats/ranking/matchmaking).
- Phase 5 depends only on the MVP's technology-neutral core and can proceed relatively independently.

## 10. Guardrails

| # | Guardrail |
|---|-----------|
| G-1 | No future phase changes the faithful Codenames gameplay or its constants ([ADR-14](02-architecture-decision-records.md), [21](03-business-constants-catalog.md)). |
| G-2 | No future phase is part of the current MVP scope; each requires its own approval. |
| G-3 | Authentication (Phase 2) must remain **additive** — guest play persists (AUTH-2/4). |
| G-4 | Privacy obligations begin only when durable person-data is introduced ([23 §6](05-data-lifecycle-retention.md#6-privacy--future-authentication-impact)). |
| G-5 | Any reversal of an MVP decision (e.g., matchmaking vs [ADR-01](02-architecture-decision-records.md)) must be recorded as a new, superseding ADR. |
| G-6 | Hidden-information and determinism invariants hold in every phase (INV-B9, [17](../02-business-analysis/16-rule-precedence.md)). |

## 11. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial roadmap; separates current MVP scope from deferred future phases without changing the MVP. |
