using System.Net;
using System.Net.Http.Json;
using Gloss.Application.Reviews;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class DeleteMergeRequestTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var mrId = await SetupPendingMrAsync();

        var response = await _client.DeleteAsync($"/api/merge-requests/{mrId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_RemovesMrFromList()
    {
        var (repoId, mrId) = await SetupPendingMrWithRepoAsync();

        await _client.DeleteAsync($"/api/merge-requests/{mrId}");

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        mrs.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/merge-requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_CascadesAndRemovesDraftComments()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Foo.cs", 1, "comment", null)]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        await _client.DeleteAsync($"/api/merge-requests/{mrId}");

        var response = await _client.GetAsync($"/api/merge-requests/{mrId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SetupPendingMrAsync()
    {
        var (_, mrId) = await SetupPendingMrWithRepoAsync();
        return mrId;
    }

    private async Task<(Guid repoId, Guid mrId)> SetupPendingMrWithRepoAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        return (repoId, mrs!.Single().Id);
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
