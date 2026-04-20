using System.Net.Http.Json;
using Gloss.Infrastructure.Reviews.Anthropic;
using Moq;

namespace Gloss.IntegrationTests.Reviews;

public sealed class AgenticLoopTests(AgenticGlossApiFactory factory) : IClassFixture<AgenticGlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Review_WhenClaudeSubmitsImmediately_CreatesDraftComments()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ClaudeApiClient
            .Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ClaudeMessage>>(),
                It.IsAny<IReadOnlyList<ClaudeToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResponse("tool_use",
            [
                new ClaudeToolUseContent("id1", "submit_review",
                    """{"comments": [{"file_path": "src/Foo.cs", "line": 10, "body": "Null check missing", "reasoning": "Can be null here"}]}""")
            ]));

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.State.Should().Be("Ready");
        mr.Comments.Should().ContainSingle(c => c.FilePath == "src/Foo.cs" && c.LineNumber == 10 && c.Body == "Null check missing");
    }

    [Fact]
    public async Task Review_WhenClaudeReadsFileThenSubmits_ServesFileContent()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewFileSystem
            .Setup(fs => fs.ReadFile("/repos/test", "src/Foo.cs"))
            .Returns("namespace Foo { public class Bar { } }");
        factory.ClaudeApiClient
            .SetupSequence(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ClaudeMessage>>(),
                It.IsAny<IReadOnlyList<ClaudeToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResponse("tool_use",
            [
                new ClaudeToolUseContent("id1", "read_file", """{"path": "src/Foo.cs"}""")
            ]))
            .ReturnsAsync(new ClaudeResponse("tool_use",
            [
                new ClaudeToolUseContent("id2", "submit_review",
                    """{"comments": [{"file_path": "src/Foo.cs", "line": 1, "body": "Missing null guard", "reasoning": null}]}""")
            ]));

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        factory.ReviewFileSystem.Verify(fs => fs.ReadFile("/repos/test", "src/Foo.cs"), Times.Once());
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.State.Should().Be("Ready");
        mr.Comments.Should().ContainSingle(c => c.FilePath == "src/Foo.cs");
    }

    [Fact]
    public async Task Review_WhenClaudeRequestsPathTraversal_ToolResultContainsFileNotFound()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewFileSystem
            .Setup(fs => fs.ReadFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null);

        var callCount = 0;
        IReadOnlyList<ClaudeMessage>? secondCallMessages = null;
        factory.ClaudeApiClient
            .Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ClaudeMessage>>(),
                It.IsAny<IReadOnlyList<ClaudeToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<ClaudeMessage>, IReadOnlyList<ClaudeToolDefinition>, CancellationToken>(
                (_, msgs, _, _) => { if (++callCount == 2) secondCallMessages = [..msgs]; })
            .ReturnsAsync(() => callCount == 1
                ? new ClaudeResponse("tool_use",
                    [new ClaudeToolUseContent("id1", "read_file", """{"path": "../../../etc/passwd"}""")])
                : new ClaudeResponse("tool_use",
                    [new ClaudeToolUseContent("id2", "submit_review", """{"comments": []}""")]));

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        secondCallMessages.Should().NotBeNull();
        var toolResultMessage = secondCallMessages!.Last();
        toolResultMessage.Role.Should().Be("user");
        var toolResult = toolResultMessage.Content.OfType<ClaudeToolResultContent>().Single();
        toolResult.Result.Should().ContainEquivalentOf("not found");
    }

    [Fact]
    public async Task Review_WhenMaxToolCallsExceeded_CompletesWithEmptyComments()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewFileSystem
            .Setup(fs => fs.ListDirectory(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([]);
        factory.ClaudeApiClient
            .Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ClaudeMessage>>(),
                It.IsAny<IReadOnlyList<ClaudeToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResponse("tool_use",
            [
                new ClaudeToolUseContent("id1", "list_directory", """{"path": "."}""")
            ]));

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.State.Should().Be("Ready");
        mr.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task Review_ToolResultIsAppendedToConversation()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewFileSystem
            .Setup(fs => fs.ReadFile("/repos/test", "src/Foo.cs"))
            .Returns("public class Foo { }");

        var messagesPerCall = new List<IReadOnlyList<ClaudeMessage>>();
        var callCount = 0;
        factory.ClaudeApiClient
            .Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ClaudeMessage>>(),
                It.IsAny<IReadOnlyList<ClaudeToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<ClaudeMessage>, IReadOnlyList<ClaudeToolDefinition>, CancellationToken>(
                (_, msgs, _, _) => messagesPerCall.Add([..msgs]))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new ClaudeResponse("tool_use",
                        [new ClaudeToolUseContent("id1", "read_file", """{"path": "src/Foo.cs"}""")])
                    : new ClaudeResponse("tool_use",
                        [new ClaudeToolUseContent("id2", "submit_review", """{"comments": []}""")]);
            });

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        messagesPerCall.Should().HaveCount(2);
        var secondCallMessages = messagesPerCall[1];
        secondCallMessages.Should().HaveCount(3);
        secondCallMessages[1].Role.Should().Be("assistant");
        secondCallMessages[1].Content.OfType<ClaudeToolUseContent>().Should().ContainSingle(t => t.Name == "read_file");
        secondCallMessages[2].Role.Should().Be("user");
        secondCallMessages[2].Content.OfType<ClaudeToolResultContent>().Should().ContainSingle(r => r.Result.Contains("public class Foo"));
    }

    private async Task<Guid> SetupPendingMrAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        return mrs!.Single().Id;
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id);
    private record DraftCommentResponse(Guid Id, string FilePath, int LineNumber, string Body, string? Reasoning);
    private record MrDetailResponse(string State, IReadOnlyList<DraftCommentResponse> Comments);

    private Task<HttpResponseMessage> SaveConfig(string[] projects) =>
        _client.PutAsJsonAsync("/api/config", new
        {
            GitProvider = "gitlab",
            GitBaseUrl = "https://gitlab.example.com",
            GitToken = "glpat-token",
            GitProjects = projects,
            LlmProvider = "anthropic",
            LlmApiKey = "sk-ant-key",
            LlmModel = "claude-sonnet-4-6",
            LlmReasoningEnabled = false,
            LlmMaxTokens = 16000,
            LlmThinkingBudget = 0,
            DefaultPollCron = "0 */2 * * * ?"
        });
}
