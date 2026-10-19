using System.Diagnostics;
using Anthropic;
using CardRunner;
using Microsoft.Extensions.AI;

// Runs one card through the agent loop, same contract as board/run.ps1: card to In Work,
// system prompt plus card to the model, tools until the final answer, card to Done or
// Failed from its RESULT line. Exit code 0 for done, 1 for failed, 2 when the run itself
// broke before the model could answer.
//
// Settings come from the environment:
//   ANTHROPIC_API_KEY       read by the Anthropic SDK
//   RUNNER_MODEL            default claude-sonnet-5-5
//   RUNNER_MAX_ITERATIONS   model rounds per run, default 60
//   RUNNER_MAX_TOKENS       input plus output tokens per run, default 400000
//   RUNNER_UI_SERVER        path to UiMcpServer.exe, relative to the repository root, or
//                           the http:// address of a server started with --http
const string DefaultModel = "claude-sonnet-5-5";
const string DefaultUiServer = "src/UiMcpServer/bin/Debug/net8.0-windows/UiMcpServer.exe";
const int DefaultMaxIterations = 60;
const long DefaultMaxTokens = 400_000;
const int MaxOutputTokens = 4096;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: CardRunner <board/cards/card.md>");
    return 2;
}

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
{
    Console.Error.WriteLine("ANTHROPIC_API_KEY is not set.");
    return 2;
}

var card = new CardFile(args[0]);
var repoRoot = FindRepoRoot(card.FilePath);
var model = Setting("RUNNER_MODEL", DefaultModel);
var maxIterations = int.Parse(Setting("RUNNER_MAX_ITERATIONS", DefaultMaxIterations.ToString()));
var maxTokens = long.Parse(Setting("RUNNER_MAX_TOKENS", DefaultMaxTokens.ToString()));
var uiServerSetting = Setting("RUNNER_UI_SERVER", DefaultUiServer);
var logPath = Path.Combine(repoRoot, "board", "runs", $"{card.Slug}-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");

using var log = new RunLog(logPath);
Console.WriteLine($"starting run, log: {logPath}");
card.SetState("In Work");

var stopwatch = Stopwatch.StartNew();
var rounds = new RoundLogger(new AnthropicClient().AsIChatClient(model), log, maxTokens);
var exitCode = 2;
RunResult result;

try
{
    await using var uiServer = await UiServer.ConnectAsync(uiServerSetting, repoRoot);
    var tools = new List<AITool>();
    tools.AddRange((await uiServer.ListToolsAsync()).Select(tool => new LoggedFunction(tool, log)));
    tools.AddRange(new LocalTools(repoRoot).All().Select(tool => new LoggedFunction(tool, log)));

    // The function-invoking client is the loop: it calls the model, runs every requested
    // tool, appends the results and calls the model again until the answer is plain text.
    var client = rounds.AsBuilder()
        .UseFunctionInvocation(configure: options =>
        {
            options.MaximumIterationsPerRequest = maxIterations;
            options.IncludeDetailedErrors = true;
        })
        .Build();

    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "instructions.md"))),
        new(ChatRole.User, "THE CARD\n--------\n" + card.Text),
    };

    log.Write("start", new { card = card.Slug, model, maxIterations, maxTokens, tools = tools.Select(tool => tool.Name) });
    // No temperature: the Claude 5 models reject the parameter.
    var options = new ChatOptions { Tools = tools, MaxOutputTokens = MaxOutputTokens };
    var response = await client.GetResponseAsync(messages, options);
    result = ResultContract.Parse(response.Text);
    exitCode = result.Done ? 0 : 1;
}
catch (Exception exception)
{
    result = new RunResult(false, exception.Message);
    log.Write("error", new { message = exception.ToString() });
}

stopwatch.Stop();
log.Write("result", new
{
    done = result.Done,
    reason = result.Reason,
    rounds = rounds.Rounds,
    inputTokens = rounds.InputTokens,
    outputTokens = rounds.OutputTokens,
    seconds = (int)stopwatch.Elapsed.TotalSeconds,
});

Console.WriteLine(result.Done ? "run finished: RESULT: done" : $"run finished: RESULT: failed: {result.Reason}");
Console.WriteLine($"rounds {rounds.Rounds}, tokens {rounds.InputTokens} in, {rounds.OutputTokens} out, {stopwatch.Elapsed:mm\\:ss} min");
card.SetState(result.Done ? "Done" : "Failed");
return exitCode;

static string Setting(string name, string fallback)
{
    return Environment.GetEnvironmentVariable(name) ?? fallback;
}

// The repository root is the first folder above the card that holds both src and board.
static string FindRepoRoot(string cardPath)
{
    var folder = Path.GetDirectoryName(cardPath);

    while (folder is not null)
    {
        if (Directory.Exists(Path.Combine(folder, "src")) && Directory.Exists(Path.Combine(folder, "board")))
        {
            return folder;
        }

        folder = Path.GetDirectoryName(folder);
    }

    throw new DirectoryNotFoundException($"No repository root with src and board above {cardPath}.");
}
