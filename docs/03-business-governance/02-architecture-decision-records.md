# 20. Architecture Decision Records (ADR) — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Accepted (records the decisions already embodied in documents 00–18) |
| **Purpose** | Record the significant **product and business-architecture** decisions behind Cluely — the *why* — so future contributors understand intent and do not accidentally reverse a deliberate choice. These ADRs are **descriptive** of the approved baseline; they change no rule. |
| **Technology** | Neutral (business/architecture rationale only; no .NET, DB, or API content). |

## Table of Contents
1. [Purpose & Usage](#1-purpose--usage)
2. [References](#2-references)
3. [ADR Format](#3-adr-format)
4. [Decision Index](#4-decision-index)
5. [Records](#5-records)
6. [Revision History](#6-revision-history)

---

## 1. Purpose & Usage

An ADR captures one decision: its context, the choice, the alternatives, why they were
rejected, and the consequences. Cluely's decisions are already implied throughout the
package; this document makes them explicit and durable. Where a decision is enforced by a
rule/invariant, the ADR references it rather than restating it.

## 2. References
- [01 — BRD](../01-product-discovery/01-business-requirements.md), [02 — SRS](../02-business-analysis/01-software-requirements.md), [03 — Business Rules](../02-business-analysis/02-business-rules.md)
- [11 — Invariants](../02-business-analysis/10-business-invariants.md), [14 — Dictionary Management](../02-business-analysis/13-dictionary-management.md)
- [24 — Product Roadmap](06-product-roadmap.md)

## 3. ADR Format

Each record: **ID**, **Status**, **Context**, **Decision**, **Alternatives Considered**,
**Why Rejected**, **Consequences**, **Future Impact**. Status ∈ {Accepted, Superseded,
Proposed}. All records below are **Accepted** for the MVP.

## 4. Decision Index

| ADR | Decision | Status |
|-----|----------|--------|
| ADR-01 | Private rooms instead of public matchmaking | Accepted |
| ADR-02 | No authentication in the MVP | Accepted |
| ADR-03 | Temporary, room-scoped player identities | Accepted |
| ADR-04 | Regional (localized) dictionaries | Accepted |
| ADR-05 | One gameplay for all countries | Accepted |
| ADR-06 | No spectators | Accepted |
| ADR-07 | No late joining into an in-progress match | Accepted |
| ADR-08 | Server-authoritative game state | Accepted |
| ADR-09 | Exactly one Host per room (with migration) | Accepted |
| ADR-10 | Exactly one Spymaster per team | Accepted |
| ADR-11 | Versioned, immutable dictionaries | Accepted |
| ADR-12 | Gameplay never depends on language | Accepted |
| ADR-13 | Technology-neutral business documentation | Accepted |
| ADR-14 | Fixed 9/8/7/1 board composition (faithful to Codenames) | Accepted |
| ADR-15 | Structural clue validation only; semantics are social | Accepted |
| ADR-16 | Grace-based disconnect tolerance & deterministic host migration | Accepted |

## 5. Records

### ADR-01 — Private rooms instead of public matchmaking
- **Status:** Accepted.
- **Context:** Codenames is played among people who know each other; the product targets that social pattern.
- **Decision:** Access a game only via a Host-created Room and a shared Room Code (BR-RC-*, [15](../02-business-analysis/14-lobby-room-lifecycle.md)).
- **Alternatives Considered:** Public matchmaking with strangers; open room browser.
- **Why Rejected:** Matchmaking introduces moderation, ranking, and abuse-handling burdens out of scope for an MVP and absent from the reference game; a room browser implies discovery/social features that were explicitly excluded (BRD §1.6).
- **Consequences:** Zero discovery surface; friends coordinate the code externally (Assumption A-1). Simpler trust model (SEC-3).
- **Future Impact:** Public matchmaking is deferred to a later phase ([Roadmap Phase 4](06-product-roadmap.md)); it can be added without changing core play.

### ADR-02 — No authentication in the MVP
- **Status:** Accepted.
- **Context:** The single biggest drop-off for casual social games is sign-up friction (BO-2).
- **Decision:** No accounts/login/registration; play with a Nickname + Room Code only (C-2, [SRS §2.14](../02-business-analysis/01-software-requirements.md#214-future-authentication-considerations)).
- **Alternatives Considered:** Mandatory accounts; optional accounts in MVP.
- **Why Rejected:** Accounts add friction and data-handling obligations without adding MVP value; optional accounts would still require the whole auth subsystem prematurely.
- **Consequences:** No personal data collected (NFR-10); identity is transient (ADR-03). Some features (history, friends) are impossible until auth exists.
- **Future Impact:** Auth is the first item in [Phase 2](06-product-roadmap.md); the identity seam (AUTH-1..5) makes it additive.

### ADR-03 — Temporary, room-scoped player identities
- **Status:** Accepted.
- **Context:** Without accounts, identity must still support reconnection and fairness.
- **Decision:** Each Player gets a transient, PII-free, Room-scoped identity/Reconnect Token (BR-JR-7, INV-P2, [16](../02-business-analysis/15-player-session-reconnection.md)).
- **Alternatives Considered:** Device fingerprinting; anonymous but persistent local IDs.
- **Why Rejected:** Fingerprinting implies tracking/PII concerns; persistent local IDs blur the no-account boundary and complicate the future-auth seam.
- **Consequences:** Reconnection works within a grace period; nothing survives the Room (INV-P2).
- **Future Impact:** A durable Account can later link to this identity without changing rules (AUTH-1..5).

### ADR-04 — Regional (localized) dictionaries
- **Status:** Accepted.
- **Context:** A global product needs culturally natural words while staying one product.
- **Decision:** Localize **only** the word source via Country Dictionaries (BO-4, [14](../02-business-analysis/13-dictionary-management.md)).
- **Alternatives Considered:** One universal word list; per-language forks of the app.
- **Why Rejected:** A universal list feels foreign in many regions; per-language forks violate one-codebase/one-gameplay (C-3) and multiply maintenance.
- **Consequences:** Content team owns dictionaries; gameplay is unaffected (INV-D1).
- **Future Impact:** New regions add with no rule/code changes (NFR-6).

### ADR-05 — One gameplay for all countries
- **Status:** Accepted.
- **Context:** Fairness and maintainability require identical rules everywhere.
- **Decision:** Counts (9/8/7/1), turn flow, and win/loss are identical for every region (C-6, INV-D1).
- **Alternatives Considered:** Regional rule variants.
- **Why Rejected:** Variants fragment the product, confuse players, and break the "single source of truth" goal.
- **Consequences:** Only words differ by region.
- **Future Impact:** Any future mode is a deliberate, separately-governed addition, never a regional side effect.

### ADR-06 — No spectators
- **Status:** Accepted.
- **Context:** Spectating is not part of the reference gameplay and risks information leaks.
- **Decision:** There is no spectator role; only participants and Waiting Players exist, and Waiting Players receive no Board/Card data mid-match (BR-JR-6a, INV-B9).
- **Alternatives Considered:** Read-only spectators; delayed spectator view.
- **Why Rejected:** Spectator views are a social feature (excluded, BRD §1.6) and threaten fairness (an Operative could watch via a second device).
- **Consequences:** Simpler visibility model; strict role-filtered delivery (NFR-3).
- **Future Impact:** If ever added, spectating must preserve the hidden-information invariant.

### ADR-07 — No late joining into an in-progress match
- **Status:** Accepted.
- **Context:** The Board, Key, and roles are fixed at Match start; injecting a player mid-match would corrupt fairness/composition.
- **Decision:** Joiners during a Match become Waiting Players for the next Match (BR-JR-6/6a, [15](../02-business-analysis/14-lobby-room-lifecycle.md)).
- **Alternatives Considered:** Assigning late joiners to a team mid-match.
- **Why Rejected:** Mid-match team/role changes are forbidden (INV-T5) and would break turn/Key assumptions.
- **Consequences:** Predictable in-match composition; waiting is clearly defined.
- **Future Impact:** None on core play; compatible with future auth.

### ADR-08 — Server-authoritative game state
- **Status:** Accepted.
- **Context:** Hidden information and fair adjudication require a single source of truth.
- **Decision:** The system (Game Engine) is the sole authority; clients submit intents and render role-filtered state (SEC-1, [SRS §2.15](../02-business-analysis/01-software-requirements.md#215-logical-architecture-system-perspective)).
- **Alternatives Considered:** Peer-to-peer / client-adjudicated state.
- **Why Rejected:** P2P cannot protect the Key or prevent cheating; it makes consistency (NFR-2) and fairness (NFR-3) unenforceable.
- **Consequences:** All rules are enforced centrally; clients cannot leak or alter state.
- **Future Impact:** Enables reliable reconnection and future features without trust in clients.

### ADR-09 — Exactly one Host per room (with migration)
- **Status:** Accepted.
- **Context:** Room setup needs a single, clear point of control that survives disconnects.
- **Decision:** Exactly one Host at all times; deterministic Host migration on loss (INV-R1, BR-HM-*).
- **Alternatives Considered:** No Host (fully democratic); multiple Hosts.
- **Why Rejected:** No Host creates deadlocks on start/config; multiple Hosts create conflicting control.
- **Consequences:** Predictable control; migration keeps rooms alive (BR-HM-2).
- **Future Impact:** Compatible with account-based ownership later.

### ADR-10 — Exactly one Spymaster per team
- **Status:** Accepted.
- **Context:** Faithful Codenames has a single clue-giver who holds the Key per team.
- **Decision:** Exactly one Spymaster per Team per Match (INV-T3, BR-RO-1/6).
- **Alternatives Considered:** Multiple Spymasters; rotating Spymaster.
- **Why Rejected:** Both change the reference mechanic and the Key-visibility model.
- **Consequences:** Clear clue authority; simple visibility rules.
- **Future Impact:** None; a future variant would be a separate, governed mode.

### ADR-11 — Versioned, immutable dictionaries
- **Status:** Accepted.
- **Context:** Corrections and offensive-word handling must not alter in-progress fairness or past reproducibility.
- **Decision:** Dictionary Versions are immutable; corrections publish new Versions; matches bind to one Version (DM-V1, INV-D3).
- **Alternatives Considered:** In-place editing of word lists.
- **Why Rejected:** In-place edits could change a running Match's word pool and break reproducibility.
- **Consequences:** Clear content lifecycle (Draft→Active→Deprecated→Retired); safe corrections.
- **Future Impact:** Supports auditability and content governance at scale.

### ADR-12 — Gameplay never depends on language
- **Status:** Accepted.
- **Context:** A global product must be provably fair regardless of language.
- **Decision:** No Business Rule, Validation, or State transition references a natural language (C-5, NFR-7, INV-D1).
- **Alternatives Considered:** Language-aware clue validation (e.g., dictionary word checks).
- **Why Rejected:** Language-aware rules would fragment fairness and semantics across regions; clue *semantics* are handled socially (ADR-15).
- **Consequences:** Identical behaviour everywhere; only words differ.
- **Future Impact:** Any language tooling must remain outside the rules engine.

### ADR-13 — Technology-neutral business documentation
- **Status:** Accepted.
- **Context:** The documentation is the single source of truth for multiple disciplines and must outlive any implementation choice.
- **Decision:** Documents describe business/behaviour only — no code, .NET, DB, API, or deployment (Documentation Rules across the package).
- **Alternatives Considered:** Mixing implementation guidance into specs.
- **Why Rejected:** Implementation detail dates quickly and constrains architecture prematurely.
- **Consequences:** Architects/developers retain freedom; QA tests behaviour, not tech.
- **Future Impact:** Implementation (e.g., a .NET backend and Flutter client) can proceed without contradicting the specs.

### ADR-14 — Fixed 9/8/7/1 board composition
- **Status:** Accepted.
- **Context:** Faithful reproduction of Codenames is the core objective.
- **Decision:** Every Board is 9 (Starting Team) + 8 + 7 Neutral + 1 Assassin = 25 (BR-BG-3, INV-B2/B3, [Constants 21](03-business-constants-catalog.md)).
- **Alternatives Considered:** Configurable board sizes/ratios.
- **Why Rejected:** Changing composition changes the game; explicitly out of scope (no new mechanics).
- **Consequences:** Deterministic fairness; the Starting Team advantage is exactly the extra agent.
- **Future Impact:** None; a variant would be a separate governed mode.

### ADR-15 — Structural clue validation only; semantics are social
- **Status:** Accepted.
- **Context:** The system can verify a clue's *shape* but not its *fairness of meaning* without language dependence (which is forbidden — ADR-12).
- **Decision:** Enforce structural rules (single word, valid number, not an unrevealed Board word); leave semantic legality to players (BR-CL-6, A-6).
- **Alternatives Considered:** Automated semantic/relatedness checks.
- **Why Rejected:** Requires language models/dictionaries per region, violating ADR-12 and adding AI (excluded).
- **Consequences:** Clean, language-neutral validation; social enforcement of spirit-of-the-rule.
- **Future Impact:** None for core play.

### ADR-16 — Grace-based disconnect tolerance & deterministic host migration
- **Status:** Accepted.
- **Context:** Mobile networks drop frequently; a game must survive brief losses (R-1, NFR-4/11).
- **Decision:** Disconnects trigger a grace period with role-appropriate pause; Host migration is deferred and deterministic (BR-DC-*, BR-HM-2, [16](../02-business-analysis/15-player-session-reconnection.md)).
- **Alternatives Considered:** Immediate removal on disconnect; random host reassignment.
- **Why Rejected:** Immediate removal ends games unfairly; random reassignment is unpredictable.
- **Consequences:** Resilient matches; predictable recovery.
- **Future Impact:** Reconnection semantics remain valid after auth is added.

## 6. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial ADR set documenting the accepted MVP decisions embodied in documents 00–18. |
