using System.Net.Http.Json;
using Gloss.Application.MergeRequests;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class MrApprovalTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Pull_WhenMrIsApproved_StoresApproverUsernameInListResponse()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "bob", DateTimeOffset.UtcNow));

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrApprovalResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeTrue();
        mrs.Single().ApprovedByUsername.Should().Be("bob");
    }

    [Fact]
    public async Task Pull_WhenMrIsApproved_StoresApprovalTimestampInListResponse()
    {
        var repoId = await SetupRepositoryAsync();
        var approvedAt = new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "bob", approvedAt));

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrApprovalResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().ApprovedAt.Should().Be(approvedAt);
    }

    [Fact]
    public async Task Pull_WhenMrIsNotApproved_NoApprovalDataInResponse()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(false, null, null));

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrApprovalResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeFalse();
        mrs.Single().ApprovedByUsername.Should().BeNull();
        mrs.Single().ApprovedAt.Should().BeNull();
    }

    [Fact]
    public async Task Pull_WhenApprovalRevokedOnSecondPull_ClearsApprovalData()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "bob", DateTimeOffset.UtcNow));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(false, null, null));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrApprovalResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeFalse();
        mrs.Single().ApprovedByUsername.Should().BeNull();
    }

    [Fact]
    public async Task Pull_WhenApprovedWithNoTimestamp_StillStoresApproverUsername()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "carol", null));

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrApprovalResponse[]>($"/api/repositories/{repoId}/merge-requests");
        mrs!.Single().IsApproved.Should().BeTrue();
        mrs.Single().ApprovedByUsername.Should().Be("carol");
        mrs.Single().ApprovedAt.Should().BeNull();
    }

    [Fact]
    public async Task Pull_ApprovalDataAlsoAppearsInMrDetail()
    {
        var repoId = await SetupRepositoryAsync();
        var approvedAt = new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetApprovalStatusAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusData(true, "bob", approvedAt));
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrApprovalResponse[]>($"/api/repositories/{repoId}/merge-requests");
        var detail = await _client.GetFromJsonAsync<MrApprovalResponse>($"/api/merge-requests/{mrs!.Single().Id}");

        detail!.IsApproved.Should().BeTrue();
        detail.ApprovedByUsername.Should().Be("bob");
        detail.ApprovedAt.Should().Be(approvedAt);
    }

    private async Task<Guid> SetupRepositoryAsync()
    {
        await _client.PutAsJsonAsync("/api/config", new
        {
            GitProvider = "gitlab",
            GitBaseUrl = "https://gitlab.example.com",
            GitToken = "glpat-token",
            GitProjects = new[] { "group/project-a" },
            LlmProvider = "anthropic",
            LlmApiKey = "sk-ant-key",
            LlmModel = "claude-sonnet-4-6",
            LlmReasoningEnabled = true,
            LlmMaxTokens = 16000,
            LlmThinkingBudget = 10000,
            DefaultPollCron = "0 */2 * * * ?"
        });
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        return repos!.Single().Id;
    }

    private record RepoSummary(Guid Id);
    private record MrApprovalResponse(Guid Id, bool IsApproved, string? ApprovedByUsername, DateTimeOffset? ApprovedAt);
}
