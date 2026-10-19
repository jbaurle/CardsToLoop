using System.Text.RegularExpressions;

namespace CardRunner;

public sealed record RunResult(bool Done, string Reason);

// The contract between prompt and runner: the last RESULT line of the final answer
// decides the card state. The same two lines as in board/run.ps1.
public static class ResultContract
{
    private static readonly Regex _resultLine = new(@"^RESULT: (?<outcome>done|failed)(: (?<reason>.*))?\s*$", RegexOptions.Multiline);

    public static RunResult Parse(string text)
    {
        var matches = _resultLine.Matches(text);

        if (matches.Count == 0)
        {
            return new RunResult(false, "no RESULT line in the final answer");
        }

        var last = matches[^1];
        var done = last.Groups["outcome"].Value == "done";
        return new RunResult(done, done ? "" : last.Groups["reason"].Value.Trim());
    }
}
