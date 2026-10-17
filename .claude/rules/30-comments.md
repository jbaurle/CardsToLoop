# Comments and prose

Applies to code comments, `[Description]` texts, README, cards and commit messages.

## Language

- Plain, simple English. Short sentences, active voice, no jargon, no marketing words.
- Swaps: "in order to" → "to", "leverage" / "utilise" → "use", "constitutes a" → "is a".
- No dash as a thought dash, neither `-` nor `–` nor `—`. Use a period, comma, colon or semicolon.
- No filler sentences. A comment that repeats the code is deleted.

## What gets a comment

- The reason, not the mechanics: a deliberate gap, a workaround, an OS or library constraint,
  an order of operations that matters, a limit and why it has that value.
- No XML docs, no `<summary>`. A class gets a `//` block directly above its declaration that
  says what it is for and how it is used.
- MCP tools: the `[Description]` is what the agent reads. Name what the tool does, the fallback
  order and the unit of every parameter.
- `// NOTE:` marks something that looks wrong but is intended, such as a gap a card will close.
  A NOTE about the whole class sits directly above the class, not at the top of the file.

## Shape

- A comment sits above the code it explains, never at the end of the line, except for a
  single named value such as `// 2 = PW_RENDERFULLCONTENT`.
- Multi-line comments: the first line is the longest, about 80 characters, up to 100 when
  that avoids a short last line. Following lines break within that width. A line never ends
  with a conjunction such as "and" or "or".
- Commit messages: first line says what changed, the body says why and how it was verified.
