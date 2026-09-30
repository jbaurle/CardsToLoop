---
title: Validate quantity and price before adding an item
state: New
category: Improvement
---

"Add item" accepts anything. A quantity of "abc" silently becomes 0 and the
row is added anyway.

**Acceptance criteria**

- A non-numeric or non-positive quantity blocks the add and tells the user why.
- A negative price blocks the add the same way.
- Valid input behaves exactly as today.
