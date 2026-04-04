using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Gloss.IntegrationTests.Repositories;

public record RepositoryResponse(
    Guid Id,
    string ProjectPath,
    string Provider,
    string? PollCron
);

public sealed class RepositoryTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_WhenNoConfigSaved_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/repositories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RepositoryResponse[]>();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WhenConfigSaved_ReturnsOneRepositoryPerProject()
    {
        await SaveConfig(["group/project-a", "group/project-b"]);

        var response = await _client.GetAsync("/api/repositories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RepositoryResponse[]>();
        body.Should().HaveCount(2);
        body.Should().ContainSingle(r => r.ProjectPath == "group/project-a");
        body.Should().ContainSingle(r => r.ProjectPath == "group/project-b");
    }

    [Fact]
    public async Task Get_RepositoriesInheritProviderFromConfig()
    {
        await SaveConfig(["group/project-a"]);

        var body = await _client.GetFromJsonAsync<RepositoryResponse[]>("/api/repositories");

        body!.Single().Provider.Should().Be("gitlab");
    }

    [Fact]
    public async Task Get_WhenConfigUpdatedWithFewerProjects_ReturnsOnlyRemainingProjects()
    {
        await SaveConfig(["group/project-a", "group/project-b"]);
        await SaveConfig(["group/project-a"]);

        var body = await _client.GetFromJsonAsync<RepositoryResponse[]>("/api/repositories");

        body.Should().HaveCount(1);
        body!.Single().ProjectPath.Should().Be("group/project-a");
    }

    [Fact]
    public async Task Patch_OverridesPollCron_ReturnsOk()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepositoryResponse[]>("/api/repositories");
        var id = repos!.Single().Id;

        var response = await _client.PatchAsJsonAsync($"/api/repositories/{id}", new { PollCron = "0 */6 * * * ?" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Patch_OverriddenPollCronIsReturnedInSubsequentGet()
    {
        await SaveConfig(["group/project-a"]);
        var repos = await _client.GetFromJsonAsync<RepositoryResponse[]>("/api/repositories");
        var id = repos!.Single().Id;

        await _client.PatchAsJsonAsync($"/api/repositories/{id}", new { PollCron = "0 */6 * * * ?" });

        var updated = await _client.GetFromJsonAsync<RepositoryResponse[]>("/api/repositories");
        updated!.Single().PollCron.Should().Be("0 */6 * * * ?");
    }

    [Fact]
    public async Task Patch_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsJsonAsync($"/api/repositories/{Guid.NewGuid()}", new { PollCron = "0 */6 * * * ?" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            DefaultPollCron = "0 */2 * * * ?"
        });
}
