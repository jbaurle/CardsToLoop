# General rules

- Keep changes minimal and focused on the request. No drive-by refactors, renames or reformatting.
- Prefer explicit, readable code over clever code.
- Before adding a helper, check whether one exists. Remove dead code that a change leaves behind.
- Do not add NuGet packages unless the task needs them.
- Never modify `bin/**`, `obj/**` or generated files (`*.g.cs`).
- Never speculate. Verify a claim about an API or a behavior against the code or the assembly before
  recommending or applying it. If that is not possible, say so and mark the recommendation as unverified.
- Every interactive control in `src/InvoiceApp` has an `AutomationId`. The MCP tools address controls by it.
- The code is read by people who follow the article, not only by the compiler. Write for them.
