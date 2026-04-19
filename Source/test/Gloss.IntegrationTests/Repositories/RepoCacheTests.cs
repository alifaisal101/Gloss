using System.Net.Http.Json;
using FluentAssertions;
using Gloss.Application.MergeRequests;
using Gloss.Application.Repositories;
using Gloss.Application.Reviews;
using Gloss.Domain.Repositories;
using Moq;
using Xunit;

namespace Gloss.IntegrationTests.Repositories;

public sealed class RepoCacheTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Review_CallsRepoManagerBeforeCallingReviewProvider()
    {
        var mrId = await SetupPendingMrAsync();
        var callOrder = new List<string>();

        factory.RepoManager
            .Setup(r => r.EnsureReadyAsync(It.IsAny<Repository>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("repo"))
            .ReturnsAsync("/repos/group/project-a");
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("review"))
            .ReturnsAsync([]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        callOrder.Should().Equal(["repo", "review"]);
    }

    [Fact]
    public async Task Review_PassesHeadShaToRepoManager()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        factory.RepoManager.Verify(
            r => r.EnsureReadyAsync(It.IsAny<Repository>(), "head", It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Review_WhenRepoManagerFails_MrIsNotStuckReviewing()
    {
        var mrId = await SetupPendingMrAsync();
        factory.RepoManager
            .Setup(r => r.EnsureReadyAsync(It.IsAny<Repository>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Git clone failed"));

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        mr!.State.Should().NotBe("Reviewing");
    }

    [Fact]
    public async Task Review_PersistsLocalClonePathOnRepository()
    {
        var (mrId, repoId) = await SetupPendingMrWithRepoIdAsync();
        factory.RepoManager
            .Setup(r => r.EnsureReadyAsync(It.IsAny<Repository>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/repos/group/project-a");
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var repos = await _client.GetFromJsonAsync<RepositoryWithClonePathResponse[]>("/api/repositories");
        repos!.Single(r => r.Id == repoId).LocalClonePath.Should().Be("/repos/group/project-a");
    }

    [Fact]
    public async Task Review_OnSubsequentReview_RepoManagerReceivesExistingLocalClonePath()
    {
        var mrId = await SetupPendingMrAsync();
        var capturedPaths = new List<string?>();
        factory.RepoManager
            .Setup(r => r.EnsureReadyAsync(It.IsAny<Repository>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Repository, string, CancellationToken>((repo, _, _) => capturedPaths.Add(repo.LocalClonePath))
            .ReturnsAsync("/repos/group/project-a");
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        capturedPaths.Should().Equal([null, "/repos/group/project-a"]);
    }

    private async Task<Guid> SetupPendingMrAsync()
    {
        var (mrId, _) = await SetupPendingMrWithRepoIdAsync();
        return mrId;
    }

    private async Task<(Guid mrId, Guid repoId)> SetupPendingMrWithRepoIdAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        return (mrs!.Single().Id, repoId);
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id);
    private record MrStateResponse(string State);
    private record RepositoryWithClonePathResponse(Guid Id, string? LocalClonePath);

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
