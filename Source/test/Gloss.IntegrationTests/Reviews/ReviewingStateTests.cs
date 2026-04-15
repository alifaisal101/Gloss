using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Gloss.Application.Reviews;
using Moq;
using Xunit;

namespace Gloss.IntegrationTests.Reviews;

public sealed class ReviewingStateTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Review_SetsStateToReviewingBeforeLlmCall()
    {
        var mrId = await SetupPendingMrAsync();
        var stateWhenLlmCalled = "unknown";

        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken _) =>
            {
                var mr = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
                stateWhenLlmCalled = mr?.State ?? "null";
                return Array.Empty<ReviewComment>();
            });

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        stateWhenLlmCalled.Should().Be("Reviewing");
    }

    [Fact]
    public async Task Review_WhenAlreadyReviewing_ReturnsConflict()
    {
        var mrId = await SetupPendingMrAsync();
        var firstCallInLlm = new TaskCompletionSource();
        var allowFirstToComplete = new TaskCompletionSource();
        var callCount = 0;

        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken _) =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    firstCallInLlm.TrySetResult();
                    await allowFirstToComplete.Task;
                }
                return Array.Empty<ReviewComment>();
            });

        var firstReview = _client.PostAsync($"/api/merge-requests/{mrId}/review", null);
        await firstCallInLlm.Task;

        var secondResponse = await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        allowFirstToComplete.SetResult();
        await firstReview;
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
    private record MrStateResponse(string State);

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
