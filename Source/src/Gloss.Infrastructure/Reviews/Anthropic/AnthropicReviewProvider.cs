using System.Text.Json;
using Gloss.Application.Reviews;

namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed class AnthropicReviewProvider(
    IClaudeApiClient claudeApiClient,
    IReviewFileSystem reviewFileSystem) : IReviewProvider
{
    private const int MaxToolCalls = 20;

    public async Task<IReadOnlyList<ReviewComment>> ReviewAsync(ReviewContext context, CancellationToken cancellationToken)
    {
        var systemPrompt = BuildSystemPrompt(context.Diff);
        var messages = new List<ClaudeMessage>
        {
            new("user", [new ClaudeTextContent("Review this diff and explore the codebase as needed. When ready, call submit_review.")])
        };
        var tools = BuildTools();

        for (var i = 0; i < MaxToolCalls; i++)
        {
            var response = await claudeApiClient.SendAsync(systemPrompt, messages, tools, cancellationToken)
                .ConfigureAwait(false);

            if (!string.Equals(response.StopReason, "tool_use", StringComparison.Ordinal))
                return [];

            var toolUses = response.Content.OfType<ClaudeToolUseContent>().ToList();
            if (toolUses.Count == 0)
                return [];

            var toolResults = new List<IClaudeContent>();
            foreach (var toolUse in toolUses)
            {
                if (string.Equals(toolUse.Name, "submit_review", StringComparison.Ordinal))
                    return ParseComments(toolUse.InputJson);

                toolResults.Add(new ClaudeToolResultContent(toolUse.Id, ExecuteTool(toolUse, context.RepoPath)));
            }

            messages.Add(new ClaudeMessage("assistant", response.Content));
            messages.Add(new ClaudeMessage("user", toolResults));
        }

        return [];
    }

    private string ExecuteTool(ClaudeToolUseContent toolUse, string repoPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(toolUse.InputJson);
            var root = doc.RootElement;
            return toolUse.Name switch
            {
                "read_file" => root.TryGetProperty("path", out var p)
                    ? reviewFileSystem.ReadFile(repoPath, p.GetString() ?? string.Empty) ?? "File not found."
                    : "Missing path.",
                "list_directory" => ExecuteListDirectory(root, repoPath),
                "search_code" => ExecuteSearchCode(root, repoPath),
                _ => "Unknown tool."
            };
        }
        catch (JsonException)
        {
            return "Invalid tool input.";
        }
    }

    private string ExecuteListDirectory(JsonElement root, string repoPath)
    {
        if (!root.TryGetProperty("path", out var p)) return "Missing path.";
        var entries = reviewFileSystem.ListDirectory(repoPath, p.GetString() ?? string.Empty);
        return entries.Count == 0 ? "(empty)" : string.Join("\n", entries);
    }

    private string ExecuteSearchCode(JsonElement root, string repoPath)
    {
        if (!root.TryGetProperty("pattern", out var p)) return "Missing pattern.";
        var matches = reviewFileSystem.SearchCode(repoPath, p.GetString() ?? string.Empty);
        return matches.Count == 0 ? "(no matches)" : string.Join("\n", matches);
    }

    private static string BuildSystemPrompt(string diff) =>
        $"""
        You are a senior software engineer performing a code review.
        The diff below shows the proposed changes. You have tools to explore the full codebase for context.

        Use read_file, list_directory, and search_code to understand the surrounding code before forming your opinion.
        When you have enough context, call submit_review with your findings.

        Focus only on meaningful issues: bugs, null dereferences, missing error handling, security vulnerabilities, logic errors.
        Do NOT comment on style, formatting, or trivial naming.
        If there are no meaningful issues, call submit_review with an empty comments array.

        <diff>
        {diff}
        </diff>
        """;

    private static IReadOnlyList<ClaudeToolDefinition> BuildTools() =>
    [
        new("read_file",
            "Read the contents of a file (path relative to repo root).",
            """{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}"""),
        new("list_directory",
            "List files and subdirectories at the given path (relative to repo root).",
            """{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}"""),
        new("search_code",
            "Search for a pattern across all files in the repository. Returns matching lines with file:line prefix.",
            """{"type":"object","properties":{"pattern":{"type":"string"}},"required":["pattern"]}"""),
        new("submit_review",
            "Submit your code review comments and end the review session.",
            """{"type":"object","properties":{"comments":{"type":"array","items":{"type":"object","properties":{"file_path":{"type":"string"},"line":{"type":"integer"},"body":{"type":"string"},"reasoning":{"type":"string"}},"required":["file_path","line","body"]}}},"required":["comments"]}"""),
    ];

    private static List<ReviewComment> ParseComments(string inputJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (!doc.RootElement.TryGetProperty("comments", out var commentsEl))
                return [];

            var comments = new List<ReviewComment>();
            foreach (var item in commentsEl.EnumerateArray())
            {
                var filePath = item.TryGetProperty("file_path", out var fp) ? fp.GetString() : null;
                var line = item.TryGetProperty("line", out var l) ? l.GetInt32() : 0;
                var body = item.TryGetProperty("body", out var b) ? b.GetString() : null;
                var reasoning = item.TryGetProperty("reasoning", out var r) && r.ValueKind != JsonValueKind.Null
                    ? r.GetString()
                    : null;

                if (!string.IsNullOrWhiteSpace(filePath) && !string.IsNullOrWhiteSpace(body))
                    comments.Add(new ReviewComment(filePath, line, body, reasoning));
            }
            return comments;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
