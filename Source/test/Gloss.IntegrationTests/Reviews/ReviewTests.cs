using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Gloss.Application.MergeRequests;
using Gloss.Application.Reviews;
using Moq;
using Xunit;

namespace Gloss.IntegrationTests.Reviews;

public record DraftCommentResponse(
    Guid Id,
    string FilePath,
    int LineNumber,
    string Body,
    string? Reasoning
);

public record MergeRequestDetailResponse(
    Guid Id,
    string Title,
    string State,
    string ProjectPath,
    string SourceBranch,
    string TargetBranch,
    string Diff,
    IReadOnlyList<DraftCommentResponse> Comments
);

public sealed class ReviewTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Review_TransitionsMrFromPendingToReady()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Foo.cs", 10, "Null check missing", null)]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MergeRequestDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.State.Should().Be("Ready");
    }

    [Fact]
    public async Task Review_CreatesDraftCommentsFromLlmResponse()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new("src/Foo.cs", 10, "Null check missing", "Parameter can be null here"),
                new("src/Bar.cs", 42, "Magic number", null),
            ]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MergeRequestDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.Comments.Should().HaveCount(2);
        mr.Comments.Should().ContainSingle(c => c.FilePath == "src/Foo.cs" && c.LineNumber == 10 && c.Reasoning == "Parameter can be null here");
        mr.Comments.Should().ContainSingle(c => c.FilePath == "src/Bar.cs" && c.LineNumber == 42);
    }

    [Fact]
    public async Task Review_WhenReviewedAgain_ReplacesExistingComments()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Foo.cs", 10, "First review comment", null)]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Bar.cs", 5, "Second review comment", null)]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MergeRequestDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.Comments.Should().HaveCount(1);
        mr.Comments.Should().ContainSingle(c => c.FilePath == "src/Bar.cs" && c.Body == "Second review comment");
    }

    [Fact]
    public async Task GetById_ReturnsDiffAndFullShape()
    {
        var mrId = await SetupPendingMrAsync();

        var mr = await _client.GetFromJsonAsync<MergeRequestDetailResponse>($"/api/merge-requests/{mrId}");

        mr!.Title.Should().Be("Fix bug");
        mr.State.Should().Be("Pending");
        mr.ProjectPath.Should().Be("group/project-a");
        mr.SourceBranch.Should().Be("fix/bug");
        mr.TargetBranch.Should().Be("main");
        mr.Diff.Should().Be("diff --git a/src/Foo.cs");
        mr.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task Review_WithLargeDiff_ReturnsError()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Big MR", null, "fix/big", "main", "alice", new string('+', 50_001))]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;

        var response = await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Review_WithUnknownMrId_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/merge-requests/{Guid.NewGuid()}/review", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveConfig_SchedulesPollingJobForEachRepository()
    {
        await SaveConfig(["group/project-a", "group/project-b"]);

        var response = await _client.GetAsync("/api/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JobResponse[]>();
        body.Should().HaveCount(2);
        body.Should().ContainSingle(j => j.RepositoryPath == "group/project-a");
        body.Should().ContainSingle(j => j.RepositoryPath == "group/project-b");
    }

    private async Task<Guid> SetupPendingMrAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        return mrs!.Single().Id;
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id);
    private record JobResponse(string RepositoryPath, string Cron);

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
            LlmReasoningEnabled = true,
            LlmMaxTokens = 16000,
            LlmThinkingBudget = 10000,
            DefaultPollCron = "0 */2 * * * ?"
        });
}
