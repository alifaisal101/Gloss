using System.Net.Http.Json;
using Gloss.Application.MergeRequests;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class MrLiveStatusTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Closed / Merged detection ────────────────────────────────────────────

    [Fact]
    public async Task Pull_WhenTrackedMrDisappearsFromOpenList_AndProviderSaysClosed_MarksItClosed()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        factory.GitClient
            .Setup(c => c.GetMergeRequestStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformMrStatusData("Closed", DateTimeOffset.UtcNow, "alice"));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().PlatformStatus.Should().Be("Closed");
    }

    [Fact]
    public async Task Pull_WhenTrackedMrDisappearsFromOpenList_AndProviderSaysMerged_MarksItMerged()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Add feature", null, "feat/x", "main", "bob", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        factory.GitClient
            .Setup(c => c.GetMergeRequestStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformMrStatusData("Merged", DateTimeOffset.UtcNow, "bob"));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().PlatformStatus.Should().Be("Merged");
    }

    [Fact]
    public async Task Pull_WhenMrIsAlreadyClosedOrMerged_DoesNotQueryStatusAgain()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        factory.GitClient
            .Setup(c => c.GetMergeRequestStatusAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformMrStatusData("Merged", DateTimeOffset.UtcNow, "alice"));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient.Reset();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient.Verify(
            c => c.GetMergeRequestStatusAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Approval ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pull_WhenMrIsApprovedOnPlatform_ReflectsIsApprovedTrueInListResponse()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "approver", null));

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task Pull_WhenMrIsNotApproved_ReflectsIsApprovedFalseInListResponse()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(false, null, null));

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeFalse();
    }

    [Fact]
    public async Task Pull_WhenApprovalChangesOnSecondPull_UpdatesIsApproved()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(false, null, null));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "approver", null));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeTrue();
    }

    // ── Platform comments ────────────────────────────────────────────────────

    [Fact]
    public async Task Pull_FetchesPlatformDiscussionsAndSurfacesThemInMrDetail()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetMrDiscussionsAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new("bob", "This looks risky", "src/Foo.cs", 42, DateTimeOffset.UtcNow),
                new("carol", "Agreed, needs a null check", null, null, DateTimeOffset.UtcNow),
            ]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        var detail = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrs!.Single().Id}");

        detail!.PlatformComments.Should().HaveCount(2);
        detail.PlatformComments.Should().ContainSingle(c => c.AuthorUsername == "bob" && c.FilePath == "src/Foo.cs" && c.Line == 42);
        detail.PlatformComments.Should().ContainSingle(c => c.AuthorUsername == "carol" && c.FilePath == null);
    }

    [Fact]
    public async Task Pull_RefreshesPlatformDiscussionsOnSubsequentPull()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetMrDiscussionsAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("bob", "Looks good to me", null, null, DateTimeOffset.UtcNow)]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient
            .Setup(c => c.GetMrDiscussionsAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new("bob", "Looks good to me", null, null, DateTimeOffset.UtcNow),
                new("carol", "One minor nit", "src/Bar.cs", 7, DateTimeOffset.UtcNow),
            ]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        var detail = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrs!.Single().Id}");

        detail!.PlatformComments.Should().HaveCount(2);
    }

    [Fact]
    public async Task MrDetail_ReflectsIsApprovedFromLastPull()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "approver", null));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStatusResponse[]>($"/api/repositories/{repoId}/merge-requests");
        var detail = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrs!.Single().Id}");

        detail!.IsApproved.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> SetupRepositoryAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        return repos!.Single().Id;
    }

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

    private record RepoSummary(Guid Id);

    private record MrStatusResponse(Guid Id, string State, string PlatformStatus, bool IsApproved);

    private record MrDetailResponse(
        bool IsApproved,
        DateTimeOffset? PlatformStatusOccurredAt,
        string? PlatformStatusByUsername,
        IReadOnlyList<PlatformCommentResponse> PlatformComments);

    private record PlatformCommentResponse(
        string AuthorUsername,
        string Body,
        string? FilePath,
        int? Line);
}
