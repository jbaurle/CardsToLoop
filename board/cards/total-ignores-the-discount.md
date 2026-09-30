---
title: The total ignores the discount
state: New
category: Bug
---

The totals panel has a "Discount %" field. Typing a discount changes nothing:
the total always equals the subtotal.

**Acceptance criteria**

- With items worth 100.00 and a discount of 10, the total shows 90.00.
- An empty or non-numeric discount counts as 0, not as an error.
- The subtotal stays undiscounted.
