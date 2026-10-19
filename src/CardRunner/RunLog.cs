using System.Text.Encodings.Web;
using System.Text.Json;

namespace CardRunner;

// One JSON object per line in board/runs/<slug>-<time>.jsonl: the start, every model
// round, every tool call and the result. The file is the only record of what the model did.
public sealed class RunLog : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly StreamWriter _writer;

    public RunLog(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _writer = new StreamWriter(path, append: false) { AutoFlush = true };
    }

    public void Write(string type, object data)
    {
        _writer.WriteLine(JsonSerializer.Serialize(new { time = DateTime.Now.ToString("HH:mm:ss.fff"), type, data }, _jsonOptions));
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}
