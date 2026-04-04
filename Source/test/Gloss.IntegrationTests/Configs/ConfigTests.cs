using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace Gloss.IntegrationTests.Configs;

public record ConfigRequest(
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

public record ConfigResponse(
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

public sealed class ConfigTests(GlossApiFactory factory) : IClassFixture<GlossApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_WhenNoConfigSaved_ReturnsNotConfigured()
    {
        var response = await _client.GetAsync("/api/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ConfigResponse>();
        body!.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Put_WithValidConfig_ReturnsOk()
    {
        var response = await _client.PutAsJsonAsync("/api/config", ValidConfig());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_AfterSavingConfig_MasksSecrets()
    {
        await _client.PutAsJsonAsync("/api/config", ValidConfig());

        var response = await _client.GetAsync("/api/config");
        var body = await response.Content.ReadFromJsonAsync<ConfigResponse>();

        body!.IsConfigured.Should().BeTrue();
        body.GitToken.Should().NotBe("glpat-secret-token");
        body.LlmApiKey.Should().NotBe("sk-ant-secret-key");
    }

    [Fact]
    public async Task Put_SecretsAreStoredEncrypted()
    {
        await _client.PutAsJsonAsync("/api/config", ValidConfig());

        await using var conn = new NpgsqlConnection(factory.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """SELECT "GitToken", "LlmApiKey" FROM configs LIMIT 1""";
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        reader.GetString(0).Should().NotBe("glpat-secret-token");
        reader.GetString(1).Should().NotBe("sk-ant-secret-key");
    }

    private static ConfigRequest ValidConfig() => new(
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