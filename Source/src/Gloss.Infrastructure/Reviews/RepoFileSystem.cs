using System.Globalization;
using System.Text.RegularExpressions;

namespace Gloss.Infrastructure.Reviews;

internal sealed class RepoFileSystem : IReviewFileSystem
{
    private const int MaxSearchResults = 50;

    public string? ReadFile(string repoPath, string relativePath)
    {
        var fullPath = Resolve(repoPath, relativePath);
        if (fullPath is null || !File.Exists(fullPath))
            return null;

        try { return File.ReadAllText(fullPath); }
        catch (IOException) { return null; }
    }

    public IReadOnlyList<string> ListDirectory(string repoPath, string relativePath)
    {
        var fullPath = Resolve(repoPath, relativePath);
        if (fullPath is null || !Directory.Exists(fullPath))
            return [];

        try
        {
            return [.. Directory.GetFileSystemEntries(fullPath)
                .Select(e => Directory.Exists(e)
                    ? Path.GetFileName(e) + "/"
                    : Path.GetFileName(e))
                .Order(StringComparer.Ordinal),];
        }
        catch (IOException) { return []; }
    }

    public IReadOnlyList<string> SearchCode(string repoPath, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return [];

        Regex regex;
        try { regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.NonBacktracking); }
        catch (ArgumentException) { return []; }
        catch (NotSupportedException) { return []; }

        var results = new List<string>();
        var repoRoot = Path.GetFullPath(repoPath);

        try
        {
            foreach (var file in Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal))
                    continue;

                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch (IOException) { continue; }

                var relativePath = Path.GetRelativePath(repoRoot, file);
                for (var i = 0; i < lines.Length && results.Count < MaxSearchResults; i++)
                {
                    if (!regex.IsMatch(lines[i])) continue;

                    var lineNumber = i + 1;
                    results.Add($"{relativePath}:{lineNumber.ToString(CultureInfo.InvariantCulture)}: {lines[i].Trim()}");
                }

                if (results.Count >= MaxSearchResults) break;
            }
        }
        catch (IOException) { return results; }

        return results;
    }

    private static string? Resolve(string repoPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Path.GetFullPath(repoPath);

        var root = Path.GetFullPath(repoPath);
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
               string.Equals(full, root, StringComparison.Ordinal)
            ? full
            : null;
    }
}
