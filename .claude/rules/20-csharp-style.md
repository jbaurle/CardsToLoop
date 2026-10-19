# C# style

The `.editorconfig` enforces the mechanical part at build time. This file names the rules
it cannot express.

## Layout

- File-scoped namespaces. `using` directives outside the namespace, `System.*` first.
- Allman braces. Braces on every `if`, `else`, loop and `using`, even for one statement.
- Block bodies for methods and constructors; expression bodies for properties and accessors.
  A setter with more than one statement gets a block over several lines.
- No `#region`. One blank line between members, never two.
- Blank lines inside a method:
  - A block (`if`, loop, `using`, `try`) gets a blank line before and after it, also when it
    is a guard clause or holds one statement. The same for a multi-line initializer.
  - A comment inside a method starts a unit: blank line before it, none after it.
  - `InitializeComponent()` at the start and the call that finishes the work at the end, such
    as `RecalculateTotals()`, stand alone, also after a single statement.
  - Single-line statements that form one unit stay together: two sample rows, three resets.
  - No blank line directly after `{` or before `}`.
- A statement stays on one line up to 160 characters; argument lists are not wrapped for
  length alone. Longer statements break at a seam: before a chained `.Call()`, `??`, `?`, `:`,
  `&&`, `||`, the operator starts the next line; or after a comma, the remaining arguments on
  one continuation line. Exceptions: a configuration chain such as `AddMcpServer().With…()`
  keeps one call per line; parameters with a `[Description]` stay one per line;
  `[Description]` strings never wrap.
- Member order: constants → static fields → instance fields → events → properties →
  constructor → public methods → event handlers → private methods.
- File ends with the closing brace and one newline, no blank line after it.

## Language

- `var` everywhere. Explicit accessibility modifier on every member. No `this.`.
- Language keywords (`string`, `int`), not framework types (`String`, `Int32`).
- Pattern matching over `is`/`as` plus cast. Inline `out var`.
- `if (expr is { } value)` when the value is only used inside the block.
- Combine related null and type checks into one `if` instead of several early returns.
- Guard clauses belong in the called method, not at the call site.
- `sealed` where inheritance is not intended. `readonly` fields where possible.
- `async`/`await`, never `.Result` or `.Wait()`. Async methods end in `Async`.
- Catch the specific exception type and handle it or rethrow. Never swallow.
- Named constants instead of magic numbers; the name says what the value is.

## Naming

- PascalCase for types, members, constants and local functions.
- `_camelCase` for all private fields, static ones too. No `s_`.
- camelCase for locals and parameters.
- Full words, no abbreviations: `deviceContext`, not `dc`; `quantity`, not `q`.
- P/Invoke methods keep PascalCase; the native name goes into `EntryPoint`.
- WPF event handlers keep the `Control_Event` pattern that XAML generates.
