# CardsToLoop

A minimal, self-contained demo of a card-driven development loop: a Kanban card
describes a change, an AI agent implements it in a real WPF app, verifies it by
driving the app's UI, and commits. The card moves on.

The loop:

```
board/cards/*.md  ->  runner  ->  claude -p "<card>"  ->  edit + build
                                        |
                                  MCP UI tools  ->  click, type, read back
                                        |
                                  commit  ->  card state: Done
```

## Parts

| Folder | What it is |
|---|---|
| `src/InvoiceApp` | The "product": a small WPF invoice editor with AutomationIds on every control. Ships with deliberate gaps that the cards describe. |
| `src/UiMcpServer` | A minimal MCP server exposing Windows UI Automation as tools (find, click, type, read, screenshot). Speaks stdio by default; `--http [url]` serves the same tools over Streamable HTTP at `/mcp` (default `http://localhost:5081`) for clients that are not a child process, such as a runner in a container. |
| `board/cards` | The cards: one markdown file per card, state in the frontmatter. |
| `board/run.ps1` | The runner: picks a card, builds the prompt, starts the agent, moves the card on success. |

## Requirements

- Windows 10/11, .NET 8 SDK
- [Claude Code](https://claude.ai/code) CLI on the PATH

## Try it

```powershell
dotnet build src/InvoiceApp/InvoiceApp.csproj
./board/run.ps1 board/cards/total-ignores-the-discount.md
```
