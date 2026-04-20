using System.Net.Http.Json;
using BuildingBlocks.Application.EventSourcing;
using Gloss.Application.Reviews;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class CommentEventTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddComment_AppendsCommentAddedEvent()
    {
        var mrId = await SetupPendingMrAsync();

        await _client.PostAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments",
            new { FilePath = "src/Foo.cs", Line = 1, Body = "my comment", Reasoning = (string?)null });

        var events = await QueryEventsAsync("CommentAdded");
        events.Should().ContainSingle();
    }

    [Fact]
    public async Task EditComment_AppendsCommentEditedEvent()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "updated body", Reasoning = (string?)null });

        var events = await QueryEventsAsync("CommentEdited");
        events.Should().ContainSingle();
    }

    [Fact]
    public async Task EditComment_CommentEditedEventContainsBodyBeforeAndAfter()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();

        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "updated body", Reasoning = (string?)null });

        var events = await QueryEventsAsync("CommentEdited");
        var payload = events.Single().Payload.RootElement;
        payload.GetProperty("BodyBefore").GetString().Should().Be("Null check missing");
        payload.GetProperty("BodyAfter").GetString().Should().Be("updated body");
    }

    [Fact]
    public async Task DeleteComment_AppendsCommentDeletedEvent()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();

        await _client.DeleteAsync($"/api/merge-requests/{mrId}/comments/{commentId}");

        var events = await QueryEventsAsync("CommentDeleted");
        events.Should().ContainSingle();
    }

    [Fact]
    public async Task Publish_AppendsCommentAcceptedEventForEachUnchangedComment()
    {
        var mrId = await SetupReadyMrAsync(comments: [
            new("src/Foo.cs", 10, "First comment", null),
            new("src/Bar.cs", 20, "Second comment", null),
        ]);

        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        var events = await QueryEventsAsync("CommentAccepted");
        events.Should().HaveCount(2);
    }

    [Fact]
    public async Task Publish_DoesNotAppendCommentAcceptedForEditedComment()
    {
        var (mrId, commentId) = await SetupMrWithSingleCommentAsync();
        await _client.PutAsJsonAsync(
            $"/api/merge-requests/{mrId}/comments/{commentId}",
            new { FilePath = "src/Foo.cs", Line = 10, Body = "edited body", Reasoning = (string?)null });

        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        var accepted = await QueryEventsAsync("CommentAccepted");
        accepted.Should().BeEmpty();
    }

    private async Task<IReadOnlyList<StoredEvent>> QueryEventsAsync(string eventType)
    {
        using var scope = factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return await eventStore.QueryAsync(new EventQuery { EventType = eventType });
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

    private async Task<Guid> SetupReadyMrAsync(IReadOnlyList<ReviewComment> comments)
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);
        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);
        return mrId;
    }

    private async Task<Guid> SetupPendingMrAsync()
    {
        await SaveConfigAsync(["group/project-a"]);
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
    private record DraftCommentResponse(Guid Id);
    private record MrDetailResponse(IReadOnlyList<DraftCommentResponse> Comments);

    private Task<HttpResponseMessage> SaveConfigAsync(string[] projects) =>
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
