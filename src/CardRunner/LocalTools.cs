using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace CardRunner;

// The tools Claude Code brings along and the runner has to provide itself: files, build,
// start and stop of the app, commit. Fixed commands instead of a shell: the model runs only
// what is here. Reads stay under src/ and .claude/rules/, writes under src/InvoiceApp/.
public sealed class LocalTools
{
    private const string AppFolder = "src/InvoiceApp";
    private const string AppProject = "src/InvoiceApp/InvoiceApp.csproj";
    private const string AppExe = "src/InvoiceApp/bin/Debug/net8.0-windows/InvoiceApp.exe";
    private const string AppProcessName = "InvoiceApp";
    private const int BuildTimeoutMs = 120_000;
    private const int GitTimeoutMs = 30_000;
    private const int StartTimeoutMs = 10_000;
    private const int StopTimeoutMs = 5_000;
    private const int BuildOutputLines = 30;
    private static readonly string[] _readRoots = ["src", ".claude/rules"];
    private static readonly string[] _writeRoots = [AppFolder];
    private static readonly string[] _skippedFolders = ["bin", "obj", ".vs"];
    private readonly string _repoRoot;

    public LocalTools(string repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public IEnumerable<AIFunction> All()
    {
        yield return AIFunctionFactory.Create(ListFiles, "list_files");
        yield return AIFunctionFactory.Create(ReadFile, "read_file");
        yield return AIFunctionFactory.Create(WriteFile, "write_file");
        yield return AIFunctionFactory.Create(BuildApp, "build_app");
        yield return AIFunctionFactory.Create(StartApp, "start_app");
        yield return AIFunctionFactory.Create(StopApp, "stop_app");
        yield return AIFunctionFactory.Create(GitCommit, "git_commit");
    }

    [Description("Lists the files under a folder of the repository, one path per line relative to the repository root. bin, obj and .vs are skipped.")]
    public string ListFiles(
        [Description("Folder relative to the repository root, default src/InvoiceApp")] string folder = AppFolder)
    {
        var root = Resolve(folder, _readRoots);
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar).Any(part => _skippedFolders.Contains(part)))
            .Select(file => Path.GetRelativePath(_repoRoot, file).Replace('\\', '/'))
            .Order();
        return string.Join("\n", files);
    }

    [Description("Reads a text file. The path is relative to the repository root and must lie under src/ or .claude/rules/.")]
    public string ReadFile(
        [Description("File path relative to the repository root")] string path)
    {
        var fullPath = Resolve(path, _readRoots);

        if (Directory.Exists(fullPath))
        {
            throw new ArgumentException($"{path} is a folder, use list_files to see its files.");
        }

        return File.ReadAllText(fullPath);
    }

    [Description("Writes a text file and replaces what was there. Only under src/InvoiceApp/. Pass the complete new content, there is no partial edit.")]
    public string WriteFile(
        [Description("File path relative to the repository root")] string path,
        [Description("The complete new content of the file")] string content)
    {
        var fullPath = Resolve(path, _writeRoots);
        File.WriteAllText(fullPath, content);
        return $"wrote {content.Length} characters to {path}";
    }

    [Description("Builds src/InvoiceApp with dotnet build. Call stop_app first, the build fails while the exe runs. Returns the errors and warnings of the build.")]
    public string BuildApp()
    {
        var (exitCode, output) = Run("dotnet", $"build {AppProject}", BuildTimeoutMs);
        var lines = output.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToList();
        var relevant = lines.Where(line => line.Contains("error", StringComparison.OrdinalIgnoreCase) || line.Contains("warning", StringComparison.OrdinalIgnoreCase));
        var summary = exitCode == 0 ? "Build succeeded." : $"Build failed with exit code {exitCode}.";
        return summary + "\n" + string.Join("\n", relevant.Distinct().Take(BuildOutputLines));
    }

    [Description("Starts the built InvoiceApp.exe and waits until it accepts input. Returns the process id. Use the ui_ tools after this.")]
    public string StartApp()
    {
        var exePath = Path.Combine(_repoRoot, AppExe);

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"InvoiceApp.exe is missing, call build_app first: {AppExe}");
        }

        var process = Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = Path.GetDirectoryName(exePath), UseShellExecute = false })!;
        process.WaitForInputIdle(StartTimeoutMs);
        return $"started InvoiceApp, process id {process.Id}";
    }

    [Description("Kills every running InvoiceApp process. Call it before build_app and at the end of the run.")]
    public string StopApp()
    {
        var processes = Process.GetProcessesByName(AppProcessName);

        foreach (var process in processes)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(StopTimeoutMs);
        }

        return $"stopped {processes.Length} InvoiceApp process(es)";
    }

    [Description("Stages the changed files under src/InvoiceApp and commits them. The first line of the message is 'card: <title>', the body says what changed and how it was verified. Returns the commit hash.")]
    public string GitCommit(
        [Description("The complete commit message, first line and body")] string message)
    {
        var messageFile = Path.GetTempFileName();
        File.WriteAllText(messageFile, message);

        try
        {
            var (addCode, addOutput) = Run("git", $"add {AppFolder}", GitTimeoutMs);

            if (addCode != 0)
            {
                throw new InvalidOperationException("git add failed: " + addOutput);
            }

            var (commitCode, commitOutput) = Run("git", $"commit -F \"{messageFile}\"", GitTimeoutMs);

            if (commitCode != 0)
            {
                throw new InvalidOperationException("git commit failed: " + commitOutput);
            }

            var (_, hash) = Run("git", "rev-parse --short HEAD", GitTimeoutMs);
            return $"committed {hash.Trim()}";
        }
        finally
        {
            File.Delete(messageFile);
        }
    }

    // Every path the model passes is checked against the allowed roots before any file
    // access; a path that escapes the repository with .. is rejected the same way.
    private string Resolve(string path, string[] roots)
    {
        var fullPath = Path.GetFullPath(path, _repoRoot);
        var allowed = roots.Select(root => Path.GetFullPath(root, _repoRoot))
            .Any(rootPath => fullPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

        if (!allowed)
        {
            throw new ArgumentException($"{path} is outside the allowed folders ({string.Join(", ", roots)}).");
        }

        return fullPath;
    }

    private (int ExitCode, string Output) Run(string fileName, string arguments, int timeoutMs)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = _repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{fileName} {arguments} did not finish within {timeoutMs / 1000} seconds.");
        }

        return (process.ExitCode, output.Result + error.Result);
    }
}
