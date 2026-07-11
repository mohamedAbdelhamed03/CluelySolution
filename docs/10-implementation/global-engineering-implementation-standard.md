# Cluely — Global Engineering Implementation Standard (Mandatory)

## Purpose

This document defines the **mandatory engineering workflow** for every implementation phase of Cluely.

It is **not** an architecture document.

It is **not** a coding standard.

It is the project's permanent engineering process.

Every implementation task, regardless of size, MUST follow this workflow.

Failure to follow this process means the implementation is **not complete**, even if the code compiles and all tests pass.

---

## Core Philosophy

Architecture has already been designed.

The implementation phase is **not** about inventing better architecture.

It is about implementing the approved architecture with production-quality engineering.

Every phase must leave the repository **better** than it was before.

Quality is cumulative.

The codebase should continuously improve.

Never merely extend it.

---

## Engineering Workflow

Every implementation phase MUST follow this sequence.

```text
Read Architecture
        ↓
Read Previous Reviews
        ↓
Read Engineering Checklist
        ↓
Understand Existing Code
        ↓
Implement
        ↓
Self Review
        ↓
Refactor
        ↓
Run Tests
        ↓
Run Architecture Tests
        ↓
Regression Check
        ↓
Deliver
```

No step may be skipped.

---

## Step 1 — Read Before Writing Code

Before changing a single file:

Read:

* Approved ADRs
* Software Design
* Technical Design
* Previous implementation review
* Engineering Checklist

Never rely on memory.

Never assume.

Never reinterpret.

---

## Step 2 — Understand Existing Code

Before implementing anything:

Understand:

* current architecture
* dependency graph
* existing abstractions
* naming conventions
* testing style
* error handling style
* existing patterns

Do NOT introduce a second style.

The repository should feel like it was written by one engineering team.

---

## Step 3 — Implement

Implement only the requested feature.

Do NOT redesign.

Do NOT refactor unrelated areas unless necessary.

If an improvement is unrelated:

Document it.

Do not mix multiple refactors into one implementation.

---

## Step 4 — Engineering Self Review

Before considering the task complete:

Review the implementation as if it were a Pull Request from another engineer.

Ask:

* Is this the simplest correct implementation?
* Is any abstraction unnecessary?
* Is any abstraction missing?
* Is duplication acceptable?
* Is the naming consistent?
* Does this follow the architecture?
* Would I approve this PR?

If the answer is "No",

Refactor.

---

## Step 5 — Production Review

Review:

Architecture

DDD

SOLID

Clean Code

Performance

Maintainability

Readability

Extensibility

Testability

Thread Safety

Determinism

Failure Handling

Encapsulation

Do not stop reviewing after compilation succeeds.

---

## Step 6 — Regression Prevention

Before finishing:

Read every previous engineering review.

Build a checklist of previous findings.

Verify:

None of them have returned.

The same mistake should never appear twice.

If it does,

The implementation is incomplete.

---

## Engineering Memory

Previous review findings become permanent engineering rules.

Examples:

If aggregate methods became too large,

prevent it in future aggregates.

If handlers became too complex,

prevent it in future handlers.

If architecture tests were missing,

expand them.

Never repeat a previously corrected issue.

---

## Continuous Improvement

Every phase should improve:

* naming
* architecture enforcement
* testing
* readability
* maintainability
* consistency
* documentation
* developer experience

Quality should trend upward.

Never sideways.

---

## Mandatory Review Checklist

### Architecture

☐ ADRs preserved

☐ Aggregate boundaries preserved

☐ Dependency directions preserved

☐ No architectural drift

☐ No framework leakage

---

### Domain

☐ Rich Domain Model

☐ Invariants enforced

☐ Aggregate encapsulated

☐ Events are facts

☐ Value Objects immutable

☐ No primitive obsession

☐ No public mutable state

---

### Application

☐ Handlers orchestrate only

☐ Validators contain no business rules

☐ Commands immutable

☐ Queries immutable

☐ Results immutable

☐ No duplicated orchestration

---

### Infrastructure

☐ No business rules

☐ SQL never authoritative

☐ SignalR never authoritative

☐ External systems isolated

☐ Dependency inversion respected

---

### Code Quality

☐ No dead code

☐ No duplicated code

☐ No unnecessary abstractions

☐ No over-engineering

☐ No under-engineering

☐ Consistent naming

☐ Small cohesive classes

☐ Small cohesive methods

☐ Nullable respected

☐ XML documentation where appropriate

---

### Performance

☐ No unnecessary allocations

☐ No unnecessary LINQ

☐ No repeated enumeration

☐ Appropriate collection usage

☐ Complexity justified

---

### Testing

☐ Unit tests added

☐ Existing tests pass

☐ Architecture tests pass

☐ New invariants tested

☐ Failure paths tested

☐ Regression tests added where appropriate

---

### Documentation

☐ README updated if required

☐ New conventions documented

☐ Technical debt documented

---

## Architecture Enforcement

Every implementation must strengthen architecture.

Examples:

Add Architecture Tests.

Expand engineering rules.

Strengthen dependency validation.

Improve compile-time safety.

Prevent future mistakes.

The architecture should become harder to violate over time.

---

## Pull Request Standards

Every implementation should be reviewable as one Pull Request.

The change should be:

* focused
* cohesive
* understandable
* reversible

Avoid mixing unrelated work.

---

## Deliverables

Every implementation phase must conclude with:

### 1. Implementation Summary

What was implemented.

---

### 2. Improvements Made

Engineering improvements.

Refactoring.

Performance.

Readability.

Testing.

---

### 3. Previous Findings Verified

List every finding from previous reviews.

State how each was verified.

---

### 4. New Technical Debt

Document:

* accepted debt
* deferred work
* known limitations

Nothing should remain hidden.

---

### 5. Risks

List remaining engineering risks.

Explain why they were accepted.

---

### 6. Self Review

Rate:

Architecture

Maintainability

DDD

Performance

Readability

Production Readiness

Overall Engineering Quality

Explain every score.

---

### 7. Readiness

State whether the repository is ready for the next implementation phase.

Justify the decision.

---

## Success Criteria

The implementation is complete only when:

* The feature works.
* Tests pass.
* Architecture tests pass.
* No approved ADR is violated.
* No previous engineering issue has been reintroduced.
* The repository is measurably better than before.
* The code would be approved in a production Pull Request by a Principal Engineer.

Compiling successfully is **not** sufficient.

Passing tests is **not** sufficient.

The implementation must demonstrate continuous engineering improvement and respect for the project's architecture.

This document is mandatory for **every future implementation phase** and should be read before writing any code.
