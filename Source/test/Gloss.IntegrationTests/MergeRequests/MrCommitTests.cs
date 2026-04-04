using System.Net.Http.Json;
using FluentAssertions;
using Gloss.Application.MergeRequests;
using Moq;
using Xunit;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class MrCommitTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Pull_StoresCommitsForEachMr()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetCommitsAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new("abc123", "Add null check", "alice", "diff --git a/Foo.cs b/Foo.cs\n..."),
                new("def456", "Fix formatting", "alice", "diff --git a/Bar.cs b/Bar.cs\n..."),
            ]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");

        mr!.Commits.Should().HaveCount(2);
        mr.Commits.Should().ContainSingle(c => c.Sha == "abc123" && c.Title == "Add null check");
        mr.Commits.Should().ContainSingle(c => c.Sha == "def456" && c.Title == "Fix formatting");
    }

    [Fact]
    public async Task GetById_IncludesCommitDiff()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .Setup(c => c.GetCommitsAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("abc123", "Add null check", "alice", "diff --git a/Foo.cs b/Foo.cs\n+++ fix")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");

        mr!.Commits.Single().Diff.Should().Contain("diff --git a/Foo.cs b/Foo.cs");
    }

    [Fact]
    public async Task Pull_IsIdempotent_ReplacesCommitsOnSecondPull()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);
        factory.GitClient
            .SetupSequence(c => c.GetCommitsAsync("group/project-a", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("abc123", "First commit", "alice", "diff1")])
            .ReturnsAsync([new("abc123", "First commit", "alice", "diff1"), new("def456", "Second commit", "bob", "diff2")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);
        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");

        mr!.Commits.Should().HaveCount(2);
    }

    [Fact]
    public async Task Pull_WithNoCommits_StoresEmptyCommitList()
    {
        var repoId = await SetupRepositoryAsync();
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(1, "Fix bug", null, "fix/bug", "main", "alice", "diff", "base", "head", "start")]);

        await _client.PostAsync($"/api/repositories/{repoId}/pull-reviews", null);

        var mrs = await _client.GetFromJsonAsync<MrSummary[]>($"/api/repositories/{repoId}/merge-requests");
        var mrId = mrs!.Single().Id;
        var mr = await _client.GetFromJsonAsync<MrDetailResponse>($"/api/merge-requests/{mrId}");

        mr!.Commits.Should().BeEmpty();
    }

    private async Task<Guid> SetupRepositoryAsync()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepoSummary[]>("/api/repositories");
        return repos!.Single().Id;
    }

    private record RepoSummary(Guid Id);
    private record MrSummary(Guid Id);
    private record MrCommitItem(string Sha, string Title, string AuthorName, string Diff);
    private record MrDetailResponse(string State, IReadOnlyList<MrCommitItem> Commits);

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
