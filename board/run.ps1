# Runs one card through the agent: set the card to In Work, build the prompt,
# start Claude Code non-interactively, and move the card to Done or Failed
# depending on the RESULT marker the run prints as its last line.
param(
    [Parameter(Mandatory = $true)]
    [string]$CardPath
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$card = Get-Item $CardPath
$cardText = Get-Content $card.FullName -Raw -Encoding UTF8

function Set-CardState([string]$state)
{
    $script:cardText = $script:cardText -replace "(?m)^state: .*$", "state: $state"
    Set-Content $card.FullName $script:cardText -Encoding UTF8 -NoNewline
    Write-Host "card '$($card.Name)' -> $state"
}

Set-CardState "In Work"

$prompt = @"
You are working one card of a card-driven development loop in this repository.

THE CARD
--------
$cardText

THE PRODUCT
-----------
- WPF app: src/InvoiceApp (project src/InvoiceApp/InvoiceApp.csproj)
- Built exe: src/InvoiceApp/bin/Debug/net8.0-windows/InvoiceApp.exe
- Every control has an AutomationId; discover them with ui_find_elements.

HOW TO WORK
-----------
1. Start the app (Start-Process the exe), reproduce or inspect the behavior the
   card describes using the ui_* MCP tools. Take a screenshot as evidence.
2. Implement the change in src/InvoiceApp. Keep it minimal and in the style of
   the existing code.
3. Stop the app (taskkill /IM InvoiceApp.exe /F) BEFORE rebuilding - the build
   fails while the exe runs. Rebuild with: dotnet build src/InvoiceApp/InvoiceApp.csproj
4. Start the app again and verify every acceptance criterion of the card live
   through the ui_* tools: change values, click, read the results back.
   Take an after screenshot. A build that compiles proves nothing.
5. Stop the app. Commit ONLY the source changes (git add the changed files
   under src/, never git add -A) with a message whose first line is:
   card: <the card title>
   and whose body says in two or three sentences what was changed and how it
   was verified live.

RULES
-----
- Do not touch board/, README.md or anything outside src/ and the commit.
- If an acceptance criterion cannot be met, do not commit half work.

THE LAST LINE of your final message must be exactly one of:
RESULT: done
RESULT: failed: <one-line reason>
"@

$logDir = Join-Path $repoRoot "board\runs"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory $logDir | Out-Null }
$logPath = Join-Path $logDir ("{0}-{1:yyyyMMdd-HHmmss}.log" -f $card.BaseName, (Get-Date))

Write-Host "starting run, log: $logPath"
Push-Location $repoRoot
try
{
    # claude writes warnings to stderr. With "Stop", Windows PowerShell turns them
    # into a terminating error and the card would stay In Work.
    $ErrorActionPreference = "Continue"
    $output = claude -p $prompt --mcp-config .mcp.json --strict-mcp-config 2>&1
    $output | Set-Content $logPath -Encoding UTF8
}
finally
{
    Pop-Location
}

$resultLine = @($output | Where-Object { $_ -match "^RESULT:" }) | Select-Object -Last 1
Write-Host "run finished: $resultLine"

if ($resultLine -match "^RESULT: done")
{
    Set-CardState "Done"
}
else
{
    Set-CardState "Failed"
}
