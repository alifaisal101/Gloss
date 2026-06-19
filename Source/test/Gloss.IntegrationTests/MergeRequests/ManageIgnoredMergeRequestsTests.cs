using System.Net;
using System.Net.Http.Json;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class ManageIgnoredMergeRequestsTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListIgnored_AfterIgnoring_ReturnsTheIgnoredMr()
    {
        var (_, mrId) = await SetupPulledMrAsync();
        await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);

        var ignored = await _client.GetFromJsonAsync<IgnoredMrResponse[]>("/api/ignored-merge-requests");

        ignored.Should().ContainSingle(i => i.ProviderIid == 1 && i.Title == "Fix bug" && i.ProjectPath == "group/project-a");
    }

    [Fact]
    public async Task Unignore_RemovesItFromTheIgnoredList()
    {
        var (_, mrId) = await SetupPulledMrAsync();
        await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);
        var ignoredId = (await _client.GetFromJsonAsync<IgnoredMrResponse[]>("/api/ignored-merge-requests"))!.Single().Id;

        var response = await _client.DeleteAsync($"/api/ignored-merge-requests/{ignoredId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var ignored = await _client.GetFromJsonAsync<IgnoredMrResponse[]>("/api/ignored-merge-requests");
        ignored.Should().BeEmpty();
    }

    [Fact]
    public async Task Unignore_ThenRepoll_RecreatesTheMr()
    {
        var (repoId, mrId) = await SetupPulledMrAsync();
        await _client.PostAsync($"/api/merge-requests/{mrId}/ignore", null);
        var ignoredId = (await _client.GetFromJsonAsync<IgnoredMrResponse[]>("/api/ignored-merge-requests"))!.Single().Id;

        await _client.DeleteAsync($"/api/ignored-merge-requests/{ignoredId}");
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        mrs.Should().ContainSingle(mr => mr.ProviderIid == 1);
    }

    private async Task<(Guid repoId, Guid mrId)> SetupPulledMrAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;
        await _client.PatchAsJsonAsync($"/api/repositories/{repoId}", new { autoReviewEnabled = false });

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        return (repoId, mrs!.Single().Id);
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id, int ProviderIid);
    private record IgnoredMrResponse(Guid Id, Guid RepositoryId, int ProviderIid, string Title, string ProjectPath, DateTimeOffset IgnoredAt);

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
