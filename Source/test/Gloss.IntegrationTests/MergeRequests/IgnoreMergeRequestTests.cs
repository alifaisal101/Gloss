using System.Net;
using System.Net.Http.Json;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class IgnoreMergeRequestTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Ignore_ReturnsNoContent()
    {
        var (_, mrId) = await SetupPulledMrAsync();

        var response = await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ignore_RemovesMrFromList()
    {
        var (repoId, mrId) = await SetupPulledMrAsync();

        await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        mrs.Should().BeEmpty();
    }

    [Fact]
    public async Task Ignore_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/merge-requests/{Guid.NewGuid()}/ignore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ignore_CancelsReviewJob_WhenAutoReviewEnabled()
    {
        const string jobId = "test-review-job-id";
        factory.JobScheduler.Setup(x => x.EnqueueReview(It.IsAny<Guid>())).Returns(jobId);
        var (_, mrId) = await SetupPulledMrAsync(autoReview: true);

        await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);

        factory.JobScheduler.Verify(x => x.CancelReview(jobId), Times.Once());
    }

    [Fact]
    public async Task Repoll_AfterIgnore_DoesNotRecreateIgnoredMr()
    {
        var (repoId, mrId) = await SetupPulledMrAsync();
        await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        mrs.Should().BeEmpty();
    }

    [Fact]
    public async Task Repoll_AfterIgnore_StillPullsOtherMrs()
    {
        var repoId = await SetupRepositoryAsync(autoReview: false);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new(1, "Ignore me", null, "fix/a", "main", "alice", "diff", "base", "head", "start"),
                new(2, "Keep me", null, "fix/b", "main", "bob", "diff", "base", "head", "start"),
            ]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        var toIgnore = (await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests"))!
            .Single(mr => mr.ProviderIid == 1);
        await _client.PostAsync($"/api/merge-requests/{toIgnore.Id}/ignore", null);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        mrs.Should().ContainSingle(mr => mr.ProviderIid == 2);
    }

    private async Task<Guid> SetupRepositoryAsync(bool autoReview)
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;
        await _client.PatchAsJsonAsync($"/api/repositories/{repoId}", new { autoReviewEnabled = autoReview });
        return repoId;
    }

    private async Task<(Guid repoId, Guid mrId)> SetupPulledMrAsync(bool autoReview = false)
    {
        var repoId = await SetupRepositoryAsync(autoReview);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        return (repoId, mrs!.Single().Id);
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id, int ProviderIid);

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
