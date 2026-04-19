using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Gloss.Application.MergeRequests;
using Gloss.Application.Reviews;
using Moq;
using Xunit;

namespace Gloss.IntegrationTests.Publish;

public sealed class PublishTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Publish_TransitionsMrFromReadyToPublished()
    {
        var mrId = await SetupReadyMrAsync();

        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.State.Should().Be("Published");
    }

    [Fact]
    public async Task Publish_CallsGitClientForEachDraftComment()
    {
        var mrId = await SetupReadyMrAsync(comments: [
            new("src/Foo.cs", 10, "Null check missing", null),
            new("src/Bar.cs", 42, "Magic number", null),
        ]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        factory.GitClient.Verify(c => c.PublishCommentAsync(
            It.IsAny<string>(), It.IsAny<int>(),
            "base-sha", "head-sha", "start-sha",
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        factory.GitClient.Verify(c => c.PublishCommentAsync(
            "group/project-a", 1,
            "base-sha", "head-sha", "start-sha",
            "src/Foo.cs", 10, "Null check missing",
            It.IsAny<CancellationToken>()), Times.Once);

        factory.GitClient.Verify(c => c.PublishCommentAsync(
            "group/project-a", 1,
            "base-sha", "head-sha", "start-sha",
            "src/Bar.cs", 42, "Magic number",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_WithNoDraftComments_Succeeds()
    {
        var mrId = await SetupReadyMrAsync(comments: []);

        var response = await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.GitClient.Verify(c => c.PublishCommentAsync(
            It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WithUnknownMrId_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/merge-requests/{Guid.NewGuid()}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publish_WhenMrIsStillPending_ReturnsBadRequest()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base-sha", "head-sha", "start-sha")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;

        var response = await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publish_WhenGitProviderReturns401_ReturnsError()
    {
        var mrId = await SetupReadyMrAsync(comments: [new("src/Foo.cs", 1, "Issue", null)]);
        factory.GitClient
            .Setup(c => c.PublishCommentAsync(
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized));

        var response = await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> SetupReadyMrAsync(IReadOnlyList<ReviewComment>? comments = null)
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base-sha", "head-sha", "start-sha")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments ?? [new("src/Foo.cs", 10, "Null check missing", null)]);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);
        return mrId;
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id);
    private record MrDetailResponse(string State);

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
