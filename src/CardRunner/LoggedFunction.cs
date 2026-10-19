using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace CardRunner;

// Wraps every tool, UI and local alike: logs the call with arguments, duration and result,
// unwraps an MCP result to its text and cuts a result above the limit. The cut is named in
// the text, so the model can ask for less next time, for instance one window instead of all.
// Without the unwrapping the model would see the MCP text as a JSON object with escaped
// quotes inside, three times the tokens for the same content.
public sealed class LoggedFunction : DelegatingAIFunction
{
    private const int MaxResultLength = 4000;
    private readonly RunLog _log;

    public LoggedFunction(AIFunction inner, RunLog log) : base(inner)
    {
        _log = log;
    }

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var argumentText = arguments.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
        string text;

        try
        {
            text = ResultText(await base.InvokeCoreAsync(arguments, cancellationToken));
        }
        catch (Exception exception)
        {
            _log.Write("tool", new { name = Name, arguments = argumentText, ms = stopwatch.ElapsedMilliseconds, error = exception.Message });
            throw;
        }

        if (text.Length > MaxResultLength)
        {
            text = text[..MaxResultLength] + $"\n[cut after {MaxResultLength} of {text.Length} characters]";
        }

        _log.Write("tool", new { name = Name, arguments = argumentText, ms = stopwatch.ElapsedMilliseconds, result = text });
        return text;
    }

    // The MCP client returns one TextContent for a text result and a list of AIContent
    // when the server sends several blocks; a local tool returns a string.
    private static string ResultText(object? result)
    {
        switch (result)
        {
            case null:
                return "";
            case string text:
                return text;
            case TextContent content:
                return content.Text;
            case IEnumerable<AIContent> contents:
                return string.Join("\n", contents.Select(ResultText));
            default:
                return JsonSerializer.Serialize(result);
        }
    }
}
