using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace Gloss.IntegrationTests.Settings;

public record SettingsRequest(
    string GitProvider,
    string GitBaseUrl,
    string GitToken,
    string[] GitProjects,
    string LlmProvider,
    string LlmApiKey,
    string LlmModel,
    bool LlmReasoningEnabled,
    string DefaultPollCron
);

public record SettingsResponse(
    bool IsConfigured,
    string? GitProvider,
    string? GitBaseUrl,
    string? GitToken,
    string[]? GitProjects,
    string? LlmProvider,
    string? LlmApiKey,
    string? LlmModel,
    bool? LlmReasoningEnabled,
    string? DefaultPollCron
);

public sealed class SettingsTests : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly GlossApiFactory _factory;
    private readonly HttpClient _client;

    public SettingsTests(GlossApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_WhenNoSettingsSaved_ReturnsNotConfigured()
    {
        var response = await _client.GetAsync("/api/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SettingsResponse>();
        body!.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Put_WithValidSettings_ReturnsOk()
    {
        var response = await _client.PutAsJsonAsync("/api/settings", ValidSettings());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_AfterSavingSettings_MasksSecrets()
    {
        await _client.PutAsJsonAsync("/api/settings", ValidSettings());

        var response = await _client.GetAsync("/api/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsResponse>();

        body!.IsConfigured.Should().BeTrue();
        body.GitToken.Should().NotBe("glpat-secret-token");
        body.LlmApiKey.Should().NotBe("sk-ant-secret-key");
    }

    [Fact]
    public async Task Put_SecretsAreStoredEncrypted()
    {
        await _client.PutAsJsonAsync("/api/settings", ValidSettings());

        await using var conn = new NpgsqlConnection(_factory.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT git_token, llm_api_key FROM settings LIMIT 1";
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        reader.GetString(0).Should().NotBe("glpat-secret-token");
        reader.GetString(1).Should().NotBe("sk-ant-secret-key");
    }

    private static SettingsRequest ValidSettings() => new(
        GitProvider: "gitlab",
        GitBaseUrl: "https://gitlab.example.com",
        GitToken: "glpat-secret-token",
        GitProjects: ["group/project"],
        LlmProvider: "anthropic",
        LlmApiKey: "sk-ant-secret-key",
        LlmModel: "claude-sonnet-4-6",
        LlmReasoningEnabled: true,
        DefaultPollCron: "0 */2 * * * ?"
    );
}
