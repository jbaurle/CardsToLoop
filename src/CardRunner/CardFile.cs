using System.Text;
using System.Text.RegularExpressions;

namespace CardRunner;

// One card from board/cards: the frontmatter gives title and state, the body the task. Only
// the state line is rewritten; the rest of the file and its encoding stay as they are.
public sealed class CardFile
{
    private static readonly Regex _stateLine = new("^state: .*$", RegexOptions.Multiline);
    private static readonly Regex _titleLine = new("^title: (.*)$", RegexOptions.Multiline);
    private readonly bool _hasByteOrderMark;
    private string _text;

    public string FilePath { get; }

    public string Slug { get; }

    public string Title { get; }

    public string Text => _text;

    public CardFile(string path)
    {
        FilePath = Path.GetFullPath(path);
        Slug = Path.GetFileNameWithoutExtension(FilePath);
        var bytes = File.ReadAllBytes(FilePath);
        _hasByteOrderMark = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        _text = new UTF8Encoding(false).GetString(bytes, _hasByteOrderMark ? 3 : 0, bytes.Length - (_hasByteOrderMark ? 3 : 0));
        var title = _titleLine.Match(_text);
        Title = title.Success ? title.Groups[1].Value.Trim() : Slug;
    }

    public void SetState(string state)
    {
        _text = _stateLine.Replace(_text, "state: " + state, 1);
        File.WriteAllText(FilePath, _text, new UTF8Encoding(_hasByteOrderMark));
        Console.WriteLine($"card '{Slug}' -> {state}");
    }
}
