using System.Net.Http.Json;
using Gloss.Application.Reviews;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class MrWorkflowStatusTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Seen ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMrDetail_WhenMrIsReady_TransitionsToSeen()
    {
        var mrId = await SetupReadyMrAsync();

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");

        detail!.State.Should().Be("Seen");
    }

    [Fact]
    public async Task GetMrDetail_WhenMrIsReady_SeenStateIsReflectedInListToo()
    {
        var (repoId, mrId) = await SetupReadyMrWithRepoAsync();
        await _client.GetAsync($"/api/merge-requests/{mrId}");

        var mrs = await _client.GetFromJsonAsync<MrStateResponse[]>($"/api/repositories/{repoId}/merge-requests");

        mrs!.Single(m => m.Id == mrId).State.Should().Be("Seen");
    }

    [Fact]
    public async Task GetMrDetail_WhenMrIsAlreadySeen_KeepsItSeen()
    {
        var mrId = await SetupReadyMrAsync();
        await _client.GetAsync($"/api/merge-requests/{mrId}");

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");

        detail!.State.Should().Be("Seen");
    }

    [Fact]
    public async Task GetMrDetail_WhenMrIsPending_DoesNotMarkSeen()
    {
        var mrId = await SetupPendingMrAsync();

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");

        detail!.State.Should().Be("Pending");
    }

    [Fact]
    public async Task GetMrDetail_WhenMrIsStaged_DoesNotRegressToSeen()
    {
        var (mrId, commentId) = await SetupReadyMrWithCommentAsync();
        await _client.GetAsync($"/api/merge-requests/{mrId}");
        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "edited", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");

        detail!.State.Should().Be("Staged");
    }

    // ── Staged ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditComment_WhenMrIsReady_TransitionsToStaged()
    {
        var (mrId, commentId) = await SetupReadyMrWithCommentAsync();

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "updated", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Staged");
    }

    [Fact]
    public async Task EditComment_WhenMrIsSeen_TransitionsToStaged()
    {
        var (mrId, commentId) = await SetupReadyMrWithCommentAsync();
        await _client.GetAsync($"/api/merge-requests/{mrId}");

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "updated", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Staged");
    }

    [Fact]
    public async Task EditComment_WhenMrIsAlreadyStaged_KeepsItStaged()
    {
        var (mrId, commentId) = await SetupReadyMrWithCommentAsync();
        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "first edit", Reasoning = (string?)null });

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "second edit", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Staged");
    }

    [Fact]
    public async Task DeleteComment_WhenMrIsReady_TransitionsToStaged()
    {
        var (mrId, commentId) = await SetupReadyMrWithCommentAsync();

        await _client.DeleteAsync($"/api/merge-requests/{mrId}/comments/{commentId}");

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Staged");
    }

    [Fact]
    public async Task AddComment_WhenMrIsReady_TransitionsToStaged()
    {
        var mrId = await SetupReadyMrAsync();

        await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/New.cs", Line = 1, Body = "manual comment", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Staged");
    }

    [Fact]
    public async Task AddComment_WhenMrIsPending_TransitionsToStaged()
    {
        var mrId = await SetupPendingMrAsync();

        await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/New.cs", Line = 1, Body = "manual comment", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Staged");
    }

    [Fact]
    public async Task EditComment_WhenMrIsPublished_DoesNotRegressToStaged()
    {
        var (mrId, commentId) = await SetupReadyMrWithCommentAsync();
        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "post-publish edit", Reasoning = (string?)null });

        var detail = await _client.GetFromJsonAsync<MrStateResponse>($"/api/merge-requests/{mrId}");
        detail!.State.Should().Be("Published");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> SetupReadyMrAsync()
    {
        var (_, mrId) = await SetupReadyMrWithRepoAsync();
        return mrId;
    }

    private async Task<(Guid repoId, Guid mrId)> SetupReadyMrWithRepoAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStateResponse[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;

        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        return (repoId, mrId);
    }

    private async Task<(Guid mrId, Guid commentId)> SetupReadyMrWithCommentAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        var repoId = repos!.Single().Id;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff --git a/src/Foo.cs", "base", "head", "start")]);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrStateResponse[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;

        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Foo.cs", 10, "Null check missing", "Can be null")]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var detail = await _client.GetFromJsonAsync<MrDetailWithComments>($"/api/merge-requests/{mrId}");
        return (mrId, detail!.Comments.Single().Id);
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

        var mrs = await _client.GetFromJsonAsync<MrStateResponse[]>($"/api/repositories/{repoId}/merge-requests");
        return mrs!.Single().Id;
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
    private record MrStateResponse(Guid Id, string State);
    private record MrDetailWithComments(string State, IReadOnlyList<CommentSummary> Comments);
    private record CommentSummary(Guid Id);
}
