# CardsToLoop

A minimal, self-contained demo of a card-driven development loop: a Kanban card
describes a bug fix or a new feature, an AI agent implements it in a real WPF app,
verifies it by driving the app's UI, and commits. The card moves on.

The loop:

```
board/cards/<card>.md
        │
        ▼
board/run.ps1           sets the card to In Work and builds the prompt
        │
        ▼
claude -p "<prompt>"    edits src/InvoiceApp and rebuilds it
        │
        ▼
ui_* MCP tools          starts the app, clicks, types, reads the result back
        │
        ▼
git commit              only when every acceptance criterion passed live
        │
        ▼
card state Done         or Failed, taken from the RESULT line of the run
```

`src/CardRunner` runs the same loop without Claude Code, as a .NET console app with
Microsoft.Extensions.AI, the Anthropic C# SDK and the MCP C# SDK.

## Parts

| Folder | What it is |
|---|---|
| `src/InvoiceApp` | The "product": a small WPF invoice editor with AutomationIds on every control. Ships with deliberate gaps that the cards describe. |
| `src/UiMcpServer` | A minimal MCP server exposing Windows UI Automation as tools (find, click, type, read, screenshot). Speaks stdio by default; `--http [url]` serves the same tools over Streamable HTTP at `/mcp` (default `http://localhost:5081`) for clients that are not a child process, such as a runner in a container. |
| `src/CardRunner` | The loop in .NET. Microsoft.Extensions.AI drives the model, the MCP C# SDK connects to UiMcpServer, seven local tools replace the shell (list, read and write files, build, start and stop the app, commit). Every round and tool call goes to a JSONL log. |
| `board/cards` | The cards: one markdown file per card, state in the frontmatter. |
| `board/run.ps1` | The runner: sets the card to In Work, builds the prompt, starts the agent, sets the card to Done or Failed. |
| `board/runs` | One log per run: the full output of Claude Code as `.log`, the rounds and tool calls of CardRunner as `.jsonl`. |
| `.mcp.json` | Tells Claude Code where the MCP server is. |
| `.claude/rules` | Style rules for the code and the comments. The agent reads them on every run. |
| `docs` | The articles as PDF. |

## Requirements

- Windows 10/11, .NET 8 SDK
- for `board/run.ps1`: [Claude Code](https://claude.ai/code) CLI on the PATH
- for `src/CardRunner`: an Anthropic API key in `ANTHROPIC_API_KEY`

## Try it

With Claude Code:

```powershell
dotnet build src/InvoiceApp/InvoiceApp.csproj
dotnet build src/UiMcpServer/UiMcpServer.csproj
./board/run.ps1 board/cards/total-ignores-the-discount.md
```

Open the folder once in Claude Code and accept the trust dialog. Otherwise the
permissions in `.claude/settings.json` are ignored and the run fails.

With the .NET runner:

```powershell
dotnet build src/InvoiceApp/InvoiceApp.csproj
dotnet build src/UiMcpServer/UiMcpServer.csproj
dotnet run --project src/CardRunner -- board/cards/total-ignores-the-discount.md
```

Model, round and token limits and the address of the UI server come from the
environment, see the header of `src/CardRunner/Program.cs`. The tag
"discount-card-dotnet-done" marks the state after its first run on the discount card.

## Articles

- [How to let Claude Code fix and test bugs in a WPF application](https://www.parago.de/en/blog/how-to-let-claude-code-fix-and-test-bugs-in-a-wpf-application/), October 2026, also as [PDF](docs/how-to-let-claude-code-fix-and-test-bugs-in-a-wpf-application.pdf). The tag "discount-card-done" marks the state after the agent run the article shows.
