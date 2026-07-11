# Cluely — Simplicity Principles

## Philosophy

The simplest correct solution is preferred over the most clever solution.

The goal is maintainable software, not impressive software.

---

## Rules

Prefer:

* explicit code over magic
* composition over inheritance
* small abstractions over generic frameworks
* readability over cleverness
* duplication over premature abstraction
* current requirements over speculative flexibility

---

## Never Introduce

Do not add:

* generic repositories
* unnecessary base classes
* abstractions without multiple consumers
* patterns without a demonstrated problem
* configurable systems that have only one valid configuration

---

## Decision Test

Before introducing an abstraction, ask:

1. Does this solve a real problem today?
2. Will removing it make the code simpler?
3. Can another engineer understand it immediately?
4. Is there measurable benefit?

If the answer is "No", do not introduce it.

---

## Principle

Every abstraction carries a maintenance cost.

Only keep abstractions that clearly earn that cost.
