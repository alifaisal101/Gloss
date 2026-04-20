using System.Net;
using System.Net.Http.Json;
using Gloss.Application.Reviews;
using Gloss.Domain.Repositories;
using Moq;

namespace Gloss.IntegrationTests.Repositories;

public sealed class DeleteRepositoryTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var repoId = await SetupRepositoryAsync();

        var response = await _client.DeleteAsync($"/api/repositories/{repoId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_RemovesRepoFromList()
    {
        var repoId = await SetupRepositoryAsync();

        await _client.DeleteAsync($"/api/repositories/{repoId}");

        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        repos.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/repositories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenLocalClonePathIsSet_DeletesLocalClone()
    {
        var repoId = await SetupRepositoryWithLocalCloneAsync();

        await _client.DeleteAsync($"/api/repositories/{repoId}");

        factory.RepoManager.Verify(
            r => r.DeleteLocalCloneAsync("/repos/test", It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Delete_WhenNoLocalClonePath_DoesNotCallDeleteLocalClone()
    {
        var repoId = await SetupRepositoryAsync();

        await _client.DeleteAsync($"/api/repositories/{repoId}");

        factory.RepoManager.Verify(
            r => r.DeleteLocalCloneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Delete_CascadesAndRemovesMergeRequests()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        await _client.DeleteAsync($"/api/repositories/{repoId}");

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>("/api/merge-requests");
        mrs.Should().BeEmpty();
    }

    private async Task<Guid> SetupRepositoryAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        return repos!.Single().Id;
    }

    private async Task<Guid> SetupRepositoryWithLocalCloneAsync()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        await _client.PostAsync($"/api/merge-requests/{mrs!.Single().Id}/review", null);
        return repoId;
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
