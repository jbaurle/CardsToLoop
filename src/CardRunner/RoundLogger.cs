using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace CardRunner;

// Sits below the function-invoking client, so it sees every single round trip to the
// model: counts rounds and tokens, logs them and stops the run at the token limit.
// Input tokens are counted per round as billed, so the sum grows with the context.
public sealed class RoundLogger : DelegatingChatClient
{
    private readonly RunLog _log;
    private readonly long _maxTokens;

    public int Rounds { get; private set; }

    public long InputTokens { get; private set; }

    public long OutputTokens { get; private set; }

    public RoundLogger(IChatClient inner, RunLog log, long maxTokens) : base(inner)
    {
        _log = log;
        _maxTokens = maxTokens;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Rounds++;
        var stopwatch = Stopwatch.StartNew();
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        var inputTokens = response.Usage?.InputTokenCount ?? 0;
        var outputTokens = response.Usage?.OutputTokenCount ?? 0;
        InputTokens += inputTokens;
        OutputTokens += outputTokens;

        _log.Write("round", new
        {
            round = Rounds,
            ms = stopwatch.ElapsedMilliseconds,
            inputTokens,
            outputTokens,
            calls = response.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Select(call => call.Name),
            text = response.Text,
        });

        if (InputTokens + OutputTokens > _maxTokens)
        {
            throw new InvalidOperationException($"Token limit of {_maxTokens} reached after round {Rounds}.");
        }

        return response;
    }
}
