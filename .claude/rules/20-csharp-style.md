# C# style

The `.editorconfig` enforces the mechanical part at build time. This file names the rules
it cannot express.

## Layout

- File-scoped namespaces. `using` directives outside the namespace, `System.*` first.
- Allman braces. Braces on every `if`, `else`, loop and `using`, even for one statement.
- Block bodies for methods and constructors; expression bodies for properties and accessors.
  A setter with more than one statement gets a block over several lines.
- No `#region`. One blank line between members. Inside a method, blank lines only between
  logical units; statements that form one unit stay together.
- Wrapped expressions start the next line with the operator.
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
