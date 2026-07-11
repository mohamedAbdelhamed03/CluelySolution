# 07.09 — Quality Attribute Scenarios (Discovery)

| | |
|---|---|
| **Version** | 1.0 · **Status** | Discovery |
| **Purpose** | Express the most important quality attributes as concrete, measurable scenarios (stimulus, environment, response, expected outcome, importance) so the architecture can be evaluated against them. |
| **Scope** | Scenario definition. No mechanisms or technologies. Targets reference existing [Quality Metrics](../03-business-governance/04-quality-metrics.md); they are business expectations, not designs. |
| **Inputs** | [Architectural Drivers](02-architectural-drivers.md), [Quality Metrics](../03-business-governance/04-quality-metrics.md), [Invariants](../02-business-analysis/10-business-invariants.md). |
| **Outputs** | Evaluation scenarios for architecture reviews (feeds [Architecture Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md)). |
| **Dependencies** | [Consistency Boundaries](06-consistency-boundaries.md), [Interactions](08-interaction-discovery.md). |
| **Cross References** | [Readiness Review](10-architecture-readiness-review.md). |
| **Related Business Documents** | [Quality Metrics](../03-business-governance/04-quality-metrics.md). |
| **Related Engineering Documents** | [Enrichment (RPN/testing)](../04-engineering-analysis/02-engineering-challenges-enrichment.md). |

---

Format: **Stimulus · Environment · Response · Expected Outcome (measure) · Importance.**

## Gameplay Fairness

### QS-01 No hidden-information leakage
- **Stimulus:** An Operative's client requests/receives state on any path (initial, delta, reconnect, rematch).
- **Environment:** Match in progress; modified client attempting to read more.
- **Response:** Delivery sends only role-appropriate data.
- **Expected outcome:** **Zero** unrevealed ownership reaches any non-Spymaster (auditable across all paths). *(QM-15; INV-B9)*
- **Importance:** Critical — existential.

### QS-02 Deterministic conflict resolution
- **Stimulus:** Two Operatives guess within milliseconds.
- **Environment:** Active guessing phase, unreliable network.
- **Response:** Serialized resolution; first valid applies; second re-evaluated.
- **Expected outcome:** Exactly one reveal per slot; identical outcome on replay. *(QM-09; RP-12)*
- **Importance:** Critical.

## Correctness

### QS-03 Terminal detection
- **Stimulus:** A guess reveals a team's last agent (own or opponent's), or the assassin.
- **Environment:** Any turn, including guess-limit boundary.
- **Response:** Terminal evaluated after the reveal, before continuation; precedence applied.
- **Expected outcome:** Correct winner/loser; match ends immediately; one recorded result. *(QM-02; INV-O1/O2)*
- **Importance:** Critical.

### QS-04 Atomic state mutation
- **Stimulus:** A reveal that updates reveal-flag, counts, turn, and possibly terminal.
- **Environment:** Concurrent load; possible interruption mid-operation.
- **Response:** All-or-nothing commit; broadcast only after commit.
- **Expected outcome:** No partial state observed; invariants hold post-commit. *(INV-B2/G2/G3)*
- **Importance:** Critical.

## Availability

### QS-05 Room availability
- **Stimulus:** Players attempt to create/join/play.
- **Environment:** Normal operation over a month.
- **Response:** Service accepts and progresses rooms.
- **Expected outcome:** ≥ 99.5% monthly availability; no in-progress match loses state within room lifetime. *(QM-01)*
- **Importance:** High.

## Recoverability

### QS-06 Recover after interruption
- **Stimulus:** The authority is interrupted mid-match.
- **Environment:** In-progress match; room still within lifetime.
- **Response:** Restore to last consistent state; no replayed terminal effects.
- **Expected outcome:** 100% of in-lifetime interruptions recover without loss/corruption/double result. *(QM-16; INV-O4)*
- **Importance:** Critical.

### QS-07 Reconnect continuity
- **Stimulus:** A player drops and returns within grace.
- **Environment:** Mobile network; player is (e.g.) the active Spymaster.
- **Response:** Restore exact team/role/view; resume paused phase; single active connection.
- **Expected outcome:** p95 ≤ 2 s restore; **zero** key leakage on restore; correct resume. *(QM-07; INV-P4/P5)*
- **Importance:** High.

## Performance / Latency

### QS-08 Action propagation
- **Stimulus:** A clue/guess/reveal/turn change is committed.
- **Environment:** Full room, within a region.
- **Response:** Role-filtered update fans out to all participants.
- **Expected outcome:** p95 ≤ 300 ms, p99 ≤ 700 ms propagation. *(QM-05)*
- **Importance:** High.

### QS-09 Room creation & board generation
- **Stimulus:** Host creates a room / starts a match.
- **Environment:** Normal load.
- **Response:** Code issued / board generated.
- **Expected outcome:** Room creation p95 ≤ 300 ms; board generation p95 ≤ 500 ms. *(QM-03/04)*
- **Importance:** Medium.

## Scalability

### QS-10 Concurrent rooms
- **Stimulus:** Many independent rooms run simultaneously.
- **Environment:** Growth to MVP target and beyond.
- **Response:** Rooms remain isolated; per-room footprint bounded.
- **Expected outcome:** ≥ 1,000 concurrent rooms with QS-08 held; scale by adding capacity; no cross-room contention. *(QM-08; SCAL-1)*
- **Importance:** Medium (MVP), High (growth).

## Security / Integrity

### QS-11 Unauthorized action rejection
- **Stimulus:** A forged/out-of-turn/wrong-role intent arrives.
- **Environment:** Modified client.
- **Response:** Server authorizes against authoritative state; rejects.
- **Expected outcome:** 100% of unauthorized intents rejected with a catalogued error; no state change. *(QM-15; SEC-1/4)*
- **Importance:** Critical.

## Maintainability

### QS-12 Add a region without rule change
- **Stimulus:** A new regional dictionary is introduced.
- **Environment:** Live product.
- **Response:** Content added; rules untouched.
- **Expected outcome:** New region live with **zero** changes to rules/gameplay documents; identical behaviour. *(QM-11/13; INV-D1)*
- **Importance:** Medium.

## Observability

### QS-13 Verify a match from signals
- **Stimulus:** A match completes.
- **Environment:** Production.
- **Response:** Business events + result are observable, PII-free.
- **Expected outcome:** 100% of key lifecycle events observable; outcome reconstructable without PII/ownership. *(QM-12/18)*
- **Importance:** Medium.

## Testability

### QS-14 Deterministic verification of rules
- **Stimulus:** A rules/concurrency test suite runs.
- **Environment:** Isolated test setup (no transport required).
- **Response:** The rules core is exercised deterministically.
- **Expected outcome:** Correctness/concurrency/fairness/recovery verifiable without production infrastructure. *(AP-20)*
- **Importance:** High (given the risk profile).

## Using these scenarios
The architecture is evaluated by walking each scenario and confirming the design can meet the
expected outcome. Non-waivable scenarios (QS-01, QS-02, QS-03, QS-06, QS-11) must pass in the
[Architecture Review](../06-architecture-governance/05-architecture-review-checklist.md); they map to
the [Architecture Success Metrics](../06-architecture-governance/04-architecture-success-metrics.md).

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial quality-attribute scenarios (QS-01…QS-14). |
