using System.Net;
using System.Net.Http.Json;
using Gloss.Application.MergeRequests;
using Moq;

namespace Gloss.IntegrationTests.Repositories;

public sealed class PollStatusTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PollAll_SetsIsPollingDuringExecution()
    {
        await SetupConfigAsync(["group/project-a"]);
        bool? capturedIsPolling = null;

        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken _) =>
            {
                var config = await _client.GetFromJsonAsync<ConfigPollResponse>("/api/config");
                capturedIsPolling = config?.IsPolling;
                return (IReadOnlyList<MergeRequestData>)[];
            });

        await _client.PostAsync("/api/repositories/poll-all", null);

        capturedIsPolling.Should().BeTrue();
    }

    [Fact]
    public async Task PollAll_ClearsIsPollingAfterCompletion()
    {
        await SetupConfigAsync(["group/project-a"]);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _client.PostAsync("/api/repositories/poll-all", null);

        var config = await _client.GetFromJsonAsync<ConfigPollResponse>("/api/config");
        config!.IsPolling.Should().BeFalse();
    }

    [Fact]
    public async Task PollAll_ClearsIsPollingOnError()
    {
        await SetupConfigAsync(["group/project-a"]);
        factory.GitClient
            .Setup(c => c.GetOpenMergeRequestsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Git provider rejected", null, HttpStatusCode.Unauthorized));

        await _client.PostAsync("/api/repositories/poll-all", null);

        var config = await _client.GetFromJsonAsync<ConfigPollResponse>("/api/config");
        config!.IsPolling.Should().BeFalse();
    }

    private Task<HttpResponseMessage> SetupConfigAsync(string[] projects) =>
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

    private record ConfigPollResponse(bool IsPolling);
}
