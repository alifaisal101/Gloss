using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Application.EventSourcing;
using Gloss.Application.Projection;
using Gloss.Application.Reviews;
using Moq;

namespace Gloss.IntegrationTests.Projection;

public sealed class ProjectionTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Publish_EnqueuesProjectionUpdateJob()
    {
        var mrId = await SetupReadyMrAsync();

        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);

        factory.JobScheduler.Verify(j => j.EnqueueProjectionUpdate(), Times.Once);
    }

    [Fact]
    public async Task GetProjection_WhenNoneExists_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/projection");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjection_AfterUpdate_ReturnsCurrentContent()
    {
        await SetupPublishedMrAsync();

        await _client.PostAsync("/api/projection/update", null);

        var response = await _client.GetFromJsonAsync<ProjectionResponse>("/api/projection");
        response!.Content.Should().Be("generated projection");
        response.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdateProjection_WhenNoEvents_DoesNotCreateProjection()
    {
        await _client.PostAsync("/api/projection/update", null);

        var response = await _client.GetAsync("/api/projection");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProjection_WhenEventsExist_CallsProjectionEngine()
    {
        await SetupPublishedMrAsync();

        await _client.PostAsync("/api/projection/update", null);

        factory.ProjectionEngine.Verify(
            e => e.BuildUpdatedProjectionAsync(
                string.Empty,
                It.Is<IReadOnlyList<StoredEvent>>(events => events.Count > 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProjection_CalledTwice_SecondCallWithNoNewEventsDoesNotCallEngine()
    {
        await SetupPublishedMrAsync();
        await _client.PostAsync("/api/projection/update", null);
        factory.ProjectionEngine.Reset();
        factory.ProjectionEngine
            .Setup(e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("second projection");

        await _client.PostAsync("/api/projection/update", null);

        factory.ProjectionEngine.Verify(
            e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProjection_CalledAfterMoreEvents_ProcessesOnlyNewEvents()
    {
        await SetupPublishedMrAsync();
        await _client.PostAsync("/api/projection/update", null);

        var capturedEvents = new List<IReadOnlyList<StoredEvent>>();
        factory.ProjectionEngine.Reset();
        factory.ProjectionEngine
            .Setup(e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<StoredEvent>, CancellationToken>((_, events, _) => capturedEvents.Add(events))
            .ReturnsAsync("updated projection");

        await SetupPublishedMrAsync();
        await _client.PostAsync("/api/projection/update", null);

        capturedEvents.Should().HaveCount(1);
        capturedEvents[0].Should().NotBeEmpty();
    }

    [Fact]
    public async Task Review_WhenProjectionExists_InjectsProjectionIntoReviewContext()
    {
        await SetupPublishedMrAsync();
        await _client.PostAsync("/api/projection/update", null);

        ReviewContext? capturedContext = null;
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReviewContext, CancellationToken>((ctx, _) => capturedContext = ctx)
            .ReturnsAsync([]);
        var mrId = await SetupPendingMrAsync();

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        capturedContext!.Projection.Should().Be("generated projection");
    }

    [Fact]
    public async Task Review_WhenNoProjectionExists_ProjectionIsNullInContext()
    {
        ReviewContext? capturedContext = null;
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReviewContext, CancellationToken>((ctx, _) => capturedContext = ctx)
            .ReturnsAsync([]);
        var mrId = await SetupPendingMrAsync();

        await _client.PostAsync($"/api/merge-requests/{mrId}/review", null);

        capturedContext!.Projection.Should().BeNull();
    }

    private async Task SetupPublishedMrAsync()
    {
        var mrId = await SetupReadyMrAsync();
        await _client.PostAsync($"/api/merge-requests/{mrId}/publish", null);
    }

    private async Task<Guid> SetupReadyMrAsync()
    {
        var mrId = await SetupPendingMrAsync();
        factory.ReviewProvider
            .Setup(p => p.ReviewAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("src/Foo.cs", 1, "Null check missing", null)]);
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
