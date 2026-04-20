using System.Net;
using System.Net.Http.Json;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public record MergeRequestResponse(
    Guid Id,
    Guid RepositoryId,
    int ProviderIid,
    string Title,
    string SourceBranch,
    string TargetBranch,
    string AuthorUsername,
    string State,
    string ProjectPath
);

public sealed class MergeRequestTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_BeforePull_ReturnsEmpty()
    {
        var repoId = await SetupRepositoryAsync();

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>($"/api/repositories/{repoId}/merge-requests");

        body.Should().BeEmpty();
    }

    [Fact]
    public async Task Pull_WhenProviderReturnsNoMrs_StoresNone()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>($"/api/repositories/{repoId}/merge-requests");
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task Pull_StoresMergeRequestsFromProvider()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new(1, "Fix null ref", null, "fix/null-ref", "main", "alice", "diff --git ...", "base", "head", "start"),
                new(2, "Add caching", "Adds Redis cache", "feat/cache", "main", "bob", "diff --git ...", "base", "head", "start"),
            ]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>($"/api/repositories/{repoId}/merge-requests");
        body.Should().HaveCount(2);
        body.Should().ContainSingle(mr => mr.ProviderIid == 1 && mr.Title == "Fix null ref");
        body.Should().ContainSingle(mr => mr.ProviderIid == 2 && mr.Title == "Add caching");
    }

    [Fact]
    public async Task Pull_IsIdempotent_DoesNotDuplicateOnSecondPull()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix null ref", null, "fix/null-ref", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>($"/api/repositories/{repoId}/merge-requests");
        body.Should().HaveCount(1);
    }

    [Fact]
    public async Task Pull_SetsMrStateToPending()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>($"/api/repositories/{repoId}/merge-requests");
        body!.Single().State.Should().Be("Pending");
    }

    [Fact]
    public async Task Pull_WithUnknownRepositoryId_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/repositories/{Guid.NewGuid()}/pull-reviews", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsMergeRequestsAcrossAllRepositories()
    {
        await SaveConfig(["group/project-a", "group/project-b"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix null ref", null, "fix/null-ref", "main", "alice", "diff", "base", "head", "start")]);
        foreach (var repo in repos!)
            await _client.PostAsync($"/api/repositories/{repo.Id}/pull-reviews", null);

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>("/api/merge-requests");

        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ReturnsMrWithProjectPath()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MergeRequestResponse[]>("/api/merge-requests");

        mrs!.Single().ProjectPath.Should().Be("group/project-a");
    }

    [Fact]
    public async Task RemovingProjectFromConfig_DeletesItssMergeRequests()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        await SaveConfig([]);

        var mrs = await _client.GetFromJsonAsync<MergeRequestResponse[]>("/api/merge-requests");
        mrs.Should().BeEmpty();
    }

    [Fact]
    public async Task PollAll_FetchesMrsForEveryRepository()
    {
        await SaveConfig(["group/project-a", "group/project-b"]);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix null ref", null, "fix/null-ref", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync("/api/repositories/poll-all", null);

        var body = await _client.GetFromJsonAsync<MergeRequestResponse[]>("/api/merge-requests");
        body.Should().HaveCount(2);
    }

    private async Task<Guid> SetupRepositoryAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        return repos!.Single().Id;
    }

    private record RepoSummary(Guid Id);

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
