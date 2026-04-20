using System.Net;
using System.Net.Http.Json;
using Gloss.Application.Reviews;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class CommentCrudTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EditComment_UpdatesBodyAndReasoning()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "Updated body", Reasoning = "Updated reasoning" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        var comment = mr!.Comments.Single(c => c.Id == commentId);
        comment.Body.Should().Be("Updated body");
        comment.Reasoning.Should().Be("Updated reasoning");
    }

    [Fact]
    public async Task EditComment_CanClearReasoning()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "Some body", Reasoning = (string?)null });

        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.Comments.Single(c => c.Id == commentId).Reasoning.Should().BeNull();
    }

    [Fact]
    public async Task EditComment_LeavesOtherCommentsUnchanged()
    {
        var mrId = await SetupMrWithMultipleCommentsAsync();
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        var other = mr!.Comments.First(c => c.FilePath == "src/Bar.cs");

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{mr.Comments.First(c => c.FilePath == "src/Foo.cs").Id}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "Changed", Reasoning = (string?)null });

        var updated = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        updated!.Comments.Single(c => c.Id == other.Id).Body.Should().Be(other.Body);
    }

    [Fact]
    public async Task EditComment_WithUnknownId_ReturnsNotFound()
    {
        var mrId = await SetupPendingMrAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{Guid.NewGuid()}",
            new { FilePath = "src/Foo.cs", Line = 1, Body = "body", Reasoning = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteComment_RemovesItFromMr()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();

        var response = await _client.DeleteAsync($"/api/merge-requests/{mrId}/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteComment_WithUnknownId_ReturnsNotFound()
    {
        var mrId = await SetupPendingMrAsync();

        var response = await _client.DeleteAsync($"/api/merge-requests/{mrId}/comments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddComment_AppearsInMrComments()
    {
        var mrId = await SetupPendingMrAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/Bar.cs", Line = 5, Body = "Manual comment", Reasoning = "I added this" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.Comments.Should().ContainSingle(c =>
            c.FilePath == "src/Bar.cs" &&
            c.LineNumber == 5 &&
            c.Body == "Manual comment" &&
            c.Reasoning == "I added this");
    }

    [Fact]
    public async Task AddComment_ReturnsIdInLocationHeader()
    {
        var mrId = await SetupPendingMrAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/Bar.cs", Line = 5, Body = "Manual comment", Reasoning = (string?)null });

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/comments/");
    }

    [Fact]
    public async Task AddComment_WithEmptyBody_ReturnsBadRequest()
    {
        var mrId = await SetupPendingMrAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/Bar.cs", Line = 5, Body = "", Reasoning = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddComment_OnUnknownMr_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/merge-requests/{Guid.NewGuid()}/comments",
            new { FilePath = "src/Bar.cs", Line = 5, Body = "comment", Reasoning = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddComment_CoexistsWithReviewGeneratedComments()
    {
        var (mrId, _) = await SetupMrWithSingleCommentAsync();

        await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/New.cs", Line = 1, Body = "My own comment", Reasoning = (string?)null });

        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        mr!.Comments.Should().HaveCount(2);
        mr.Comments.Should().Contain(c => c.FilePath == "src/Foo.cs");
        mr.Comments.Should().Contain(c => c.FilePath == "src/New.cs");
    }

    private async Task<(Guid mrId, Guid commentId)> SetupMrWithSingleCommentAsync()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Foo.cs", 10, "Null check missing", "Can be null here")]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");
        return (mrId, mr!.Comments.Single().Id);
    }

    private async Task<Guid> SetupMrWithMultipleCommentsAsync()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new("src/Foo.cs", 10, "First comment", null),
                new("src/Bar.cs", 20, "Second comment", null),
            ]);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);
        return mrId;
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
    private record DraftCommentResponse(Guid Id, string FilePath, int LineNumber, string Body, string? Reasoning);
    private record MrDetailResponse(string State, IReadOnlyList<DraftCommentResponse> Comments);

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
