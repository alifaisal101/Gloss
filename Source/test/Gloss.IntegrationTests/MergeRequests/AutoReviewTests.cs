using System.Net.Http.Json;
using FluentAssertions;
using Gloss.Application.Jobs;
using Gloss.Application.MergeRequests;
using Moq;
using Xunit;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class AutoReviewTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Pull_WithAutoReviewEnabled_EnqueuesReviewForNewMr()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.JobScheduler.Verify(j => j.EnqueueReview(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Pull_WithAutoReviewDisabled_DoesNotEnqueueReview()
    {
        var repoId = await SetupRepositoryAsync();
        await _client.PatchAsJsonAsync($"/api/repositories/{repoId}", new { AutoReviewEnabled = false });
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.JobScheduler.Verify(j => j.EnqueueReview(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Pull_IsIdempotent_DoesNotReEnqueueForExistingMr()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        factory.JobScheduler.Invocations.Clear();
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.JobScheduler.Verify(j => j.EnqueueReview(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Pull_EnqueuesReviewWithCorrectMrId()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;

        factory.JobScheduler.Verify(j => j.EnqueueReview(mrId), Times.Once);
    }

    [Fact]
    public async Task Pull_WithMultipleNewMrs_EnqueuesReviewForEach()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start"),
                new(2, "Add feature", null, "feat/x", "main", "bob", "diff", "base", "head", "start"),
            ]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.JobScheduler.Verify(j => j.EnqueueReview(It.IsAny<Guid>()), Times.Exactly(2));
    }

    private async Task<Guid> SetupRepositoryAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        return repos!.Single().Id;
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id);

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
