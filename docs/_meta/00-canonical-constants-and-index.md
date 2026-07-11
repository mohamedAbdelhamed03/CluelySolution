# Cluely — Business & Product Documentation

Cluely is an online, multiplayer, word-association party game whose gameplay is
**functionally equivalent to the Codenames board game**. This documentation set is the
**single source of truth** for the business of the game. It is intended for product
owners, business analysts, architects, developers, QA engineers, and designers.

> **Scope note.** These documents describe *business and behaviour only*. They do not
> prescribe implementation technologies, code, UI design, deployment topology, or new
> gameplay. The intended delivery platform context (informational only) is a **.NET**
> backend and a **Flutter** mobile client; no document below depends on that choice — all
> business rules are technology-neutral and language-neutral.

## Reference baseline

The **Codenames** board game is the functional reference. Where the original rules are
ambiguous, this documentation chooses the interpretation that matches the physical board
game. No mechanic has been invented, removed, or simplified.

## Document index

| # | Document | Purpose |
|---|----------|---------|
| 1 | [Business Requirements Document](../01-product-discovery/01-business-requirements.md) | Why the product exists; vision, objectives, scope, risks. |
| 2 | [Software Requirements Specification](../02-business-analysis/01-software-requirements.md) | IEEE-style functional & non-functional requirements and logical architecture. |
| 3 | [Business Rules Document](../02-business-analysis/02-business-rules.md) | Every rule, validation, and state transition. |
| 4 | [Functional Requirements](../02-business-analysis/03-functional-requirements.md) | Each feature with flows, pre/postconditions, failures. |
| 5 | [User Stories](../02-business-analysis/04-user-stories.md) | Stories per actor with acceptance criteria. |
| 6 | [Use Cases](../02-business-analysis/05-use-cases.md) | Detailed use cases with success/alternative/exception flows. |
| 7 | [Domain Model](../02-business-analysis/06-domain-model.md) | Business entities, responsibilities, relationships, constraints. |
| 8 | [State Machines](../02-business-analysis/07-state-machines.md) | Lifecycle states and transitions for every stateful entity. |
| 9 | [Business Workflows](../02-business-analysis/08-business-workflows.md) | End-to-end process flows. |
| 10 | [Validation Rules](../02-business-analysis/09-validation-rules.md) | Every validation, its reason, and business outcome. |
| 11 | [Business Invariants](../02-business-analysis/10-business-invariants.md) | Conditions that must always remain true across the system's lifetime. |
| 12 | [Domain Events Catalog](../02-business-analysis/11-domain-events-catalog.md) | Every meaningful business event: trigger, publisher, consumers, payload. |
| 13 | [Business Error Catalog](../02-business-analysis/12-business-error-catalog.md) | Centralized business error codes, causes, messages, recovery — the error reference. |
| 14 | [Dictionary Management Specification](../02-business-analysis/13-dictionary-management.md) | Regional dictionary lifecycle, versioning, validation, ownership. |
| 15 | [Lobby & Room Lifecycle Specification](../02-business-analysis/14-lobby-room-lifecycle.md) | Everything before/between matches: lobby, membership, host migration, rematch, expiry. |
| 16 | [Player Session & Reconnection Specification](../02-business-analysis/15-player-session-reconnection.md) | Temporary identity, disconnect/reconnect, grace, pause rules, abandonment. |
| 17 | [Rule Precedence Specification](../02-business-analysis/16-rule-precedence.md) | Deterministic ordering when multiple rules apply at once. |
| 18 | [Consistency Validation Report](../02-business-analysis/17-consistency-validation-report.md) | Gap-analysis findings across all documents (advisory; no docs auto-changed). |
| 19 | [Business Glossary](../03-business-governance/01-business-glossary.md) | Canonical, authoritative definition of every business term. |
| 20 | [Architecture Decision Records](../03-business-governance/02-architecture-decision-records.md) | The *why* behind key product/architecture decisions. |
| 21 | [Business Constants Catalog](../03-business-governance/03-business-constants-catalog.md) | Single authoritative source for every numeric business value. |
| 22 | [Non-Functional Quality Metrics](../03-business-governance/04-quality-metrics.md) | Measurable targets and acceptance criteria for each NFR. |
| 23 | [Data Lifecycle & Retention Policy](../03-business-governance/05-data-lifecycle-retention.md) | Business lifecycle of every object; PII-free, future-auth aware. |
| 24 | [Product Roadmap](../03-business-governance/06-product-roadmap.md) | Current vs future scope; deferred evolution without changing the MVP. |
| 25 | [Governance Validation Summary](../03-business-governance/07-governance-validation-summary.md) | Confirms 19–24 complement 00–18 without changing approved business. |
| 26 | [Engineering Challenges & Risk Analysis](../04-engineering-analysis/01-engineering-challenges-risk-analysis.md) | Pre-architecture feasibility study: every engineering risk & edge case, analysis only. |
| 27 | [Engineering Challenges — Enrichment Layer](../04-engineering-analysis/02-engineering-challenges-enrichment.md) | Per-challenge metadata (difficulty, RPN, testing, MVP applicability, patterns, drivers) + priority summary; companion to 26. |
| 28 | [Architecture Input Report](../05-architecture-input/01-architecture-input-report.md) | The bridge to the Architecture phase: drivers, constraints, fixed vs open decisions, success criteria, readiness. Read this first before architecture. |
| 29 | [Architecture Principles](../06-architecture-governance/01-architecture-principles.md) | Mandatory principles every architectural decision must follow. |
| 30 | [Architecture Anti-Principles](../06-architecture-governance/02-architecture-anti-principles.md) | Explicitly forbidden practices; review red flags. |
| 31 | [Architecture Decision Heuristics](../06-architecture-governance/03-architecture-decision-heuristics.md) | How to make and record architectural decisions consistently. |
| 32 | [Architecture Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md) | Measurable approval gates for the architecture deliverable. |
| 33 | [Architecture Review Checklist](../06-architecture-governance/05-architecture-review-checklist.md) | The gate completed before architecture approval. |
| 34 | [Architecture Traceability Matrix](../06-architecture-governance/06-architecture-traceability-matrix.md) | Framework linking business→engineering→architecture→impl→test→ops. |
| 35 | [Architecture Risk Register](../06-architecture-governance/07-architecture-risk-register.md) | Risks arising from architectural decisions (solution practice). |
| 36 | [Architecture Governance — Usage Summary](../06-architecture-governance/08-architecture-governance-usage.md) | How to operate the governance set (29–35): references, reviews, approvals, traceability. |

> Documents 29–36 are the **architecture governance framework** — the rulebook for the Software
> Architecture phase. They make no architectural decisions and choose no technology; they define
> how decisions are made, reviewed, measured, and traced. Start with
> [28](../05-architecture-input/01-architecture-input-report.md), then [36](../06-architecture-governance/08-architecture-governance-usage.md) for how
> to operate the framework.

> Documents 1–10 are the approved baseline. Documents 11–18 were added by the completion &
> validation pass; documents 19–25 are the governance & product-foundation layer; document 26
> is a pre-architecture engineering risk analysis. All of 11–26 **reference** the baseline and
> introduce no new gameplay. Documents 18 and 25 report findings without modifying approved
> documents; document 26 selects no technology or architecture. For terminology see
> [19](../03-business-governance/01-business-glossary.md); for all numeric values see [21](../03-business-governance/03-business-constants-catalog.md).

## Canonical constants (used consistently across all documents)

| Constant | Value | Meaning |
|----------|-------|---------|
| `BOARD_SIZE` | 25 | Word cards on the board (5×5 grid). |
| `STARTING_TEAM_AGENTS` | 9 | Agent cards owned by the team that plays first. |
| `SECOND_TEAM_AGENTS` | 8 | Agent cards owned by the team that plays second. |
| `NEUTRAL_CARDS` | 7 | Bystander cards owned by no team. |
| `ASSASSIN_CARDS` | 1 | The single losing card. |
| `TEAMS_PER_GAME` | 2 | Two opposing teams (referred to as Red and Blue). |
| `SPYMASTERS_PER_TEAM` | 1 | Exactly one Spymaster per team. |
| `MIN_OPERATIVES_PER_TEAM` | 1 | At least one Operative per team. |
| `MIN_PLAYERS` | 4 | Minimum to start (2 teams × {1 Spymaster + 1 Operative}). |
| `GUESS_BONUS` | +1 | Operatives may guess (clue number + 1) times. |

Note: `9 + 8 + 7 + 1 = 25`. The split (9 vs 8) is fixed; the team that receives 9 agents
is the **starting team** for that match.

## Configurable operational parameters

These values are operational tunables (not gameplay rules); they do not affect Codenames
fidelity. Defaults are recommendations for the MVP; the **allowed range** bounds what an
operator may set. Governing document is noted for traceability.

| Parameter | Default | Allowed range | Meaning | Governed by |
|-----------|---------|---------------|---------|-------------|
| `ROOM_MAX_PLAYERS` | 10 | 4–20 | Maximum members per room. | BR-JR-5, V-CAP-1 |
| `RECONNECT_GRACE_PERIOD` | 60 s | 15–180 s | Window for a disconnected player to reconnect and resume role before removal. | BR-DC-2/5, V-RECON-2 |
| `HOST_MIGRATION_GRACE` | 60 s | 15–180 s | Delay before a disconnected Host's privileges migrate (tolerates brief drops). | BR-DC-8, BR-HM-1 |
| `ROOM_IDLE_EXPIRY` | 30 min | 5–120 min | Inactivity period after which a room expires. | BR-RX-1, V-EXP-1 |
| `ROOM_CODE_LENGTH` | 6 chars | 4–8 chars | Length of the room code. | BR-RC-2/3 |
| `ROOM_CODE_ALPHABET` | Uppercase letters + digits, ambiguity-reduced (no `0/O`, `1/I/L`) | — | Character set for room codes; must yield a non-sequential, unguessable space. | BR-RC-2, R-3 |
| `NICKNAME_MIN_LENGTH` | 1 | 1–3 | Minimum nickname length (after trim). | V-NICK-1 |
| `NICKNAME_MAX_LENGTH` | 20 | 8–32 | Maximum nickname length. | V-NICK-2 |
| `DICTIONARY_MIN_WORDS` | 25 | ≥25 (hard floor) | Minimum usable words a dictionary version must supply to be playable. | BR-GS-3, V-DICT-2 |

Changing any parameter within its allowed range alters operational behaviour only; it never
changes the rules, counts, turn flow, or win/loss outcomes of the game.
