# Cluely — Technical Debt Policy

## Purpose

Technical debt is allowed only when it is explicit, justified, and tracked.

Hidden technical debt is prohibited.

---

## Rules

Never leave debt undocumented.

Every compromise must include:

* Description
* Reason
* Risk
* Impact
* Proposed future resolution

---

## Categories

### Temporary

A short-term compromise with a planned removal.

### Deferred

A valid improvement intentionally postponed to keep scope manageable.

### Architectural

A limitation that would require an ADR or broader design change.

### External

Caused by third-party dependencies, framework limitations, or external systems.

---

## Prohibited

Never:

* leave TODO comments without explanation
* silently ignore warnings
* duplicate code "for now"
* disable tests without documentation

---

## Reporting

Every implementation report must contain a **Technical Debt** section.

If there is no debt, explicitly state:

> No intentional technical debt introduced.
