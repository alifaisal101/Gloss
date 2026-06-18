using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace Gloss.IntegrationTests.Configs;

public record ConfigRequest(
    string GitProvider,
    string GitBaseUrl,
    string? GitToken,
    string[] GitProjects,
    string LlmProvider,
    string? LlmApiKey,
    string LlmModel,
    bool LlmReasoningEnabled,
    int LlmMaxTokens,
    int LlmThinkingBudget,
    string DefaultPollCron
);

public record ConfigResponse(
    bool IsConfigured,
    string? GitProvider,
    string? GitBaseUrl,
    bool GitTokenSet,
    string[]? GitProjects,
    string? LlmProvider,
    bool LlmApiKeySet,
    string? LlmModel,
    bool? LlmReasoningEnabled,
    int? LlmMaxTokens,
    int? LlmThinkingBudget,
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

    // Production bug: LlmModel was saved with no validation, so "claude-opus-4.8" (dot instead of
    // hyphen — an unknown id) was accepted and then 404'd on every call. A model must be validated
    // *against the selected provider*: only valid for the provider that actually serves it. The two
    // cross-provider cases (an OpenAI model under anthropic, a Claude model under openai) are what
    // "tie the model to the provider" means.
    [Theory]
    [InlineData("anthropic", "claude-opus-4.8")]
    [InlineData("anthropic", "gpt-4o")]
    [InlineData("anthropic", "")]
    [InlineData("openai", "claude-opus-4-8")]
    public async Task Put_WithModelNotValidForProvider_IsRejected(string provider, string model)
    {
        var response = await _client.PutAsJsonAsync("/api/config",
            ValidConfig() with { LlmProvider = provider, LlmModel = model });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("anthropic", "claude-opus-4-8")]
    [InlineData("anthropic", "claude-sonnet-4-6")]
    public async Task Put_WithModelValidForProvider_ReturnsOk(string provider, string model)
    {
        var response = await _client.PutAsJsonAsync("/api/config",
            ValidConfig() with { LlmProvider = provider, LlmModel = model });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_AfterSavingConfig_SecretsAreNotExposed()
    {
        await _client.PutAsJsonAsync("/api/config", ValidConfig());

        var body = await _client.GetFromJsonAsync<ConfigResponse>("/api/config");

        body!.IsConfigured.Should().BeTrue();
        body.GitTokenSet.Should().BeTrue();
        body.LlmApiKeySet.Should().BeTrue();
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

    [Fact]
    public async Task Put_WithMaskedSecret_ReturnsBadRequest()
    {
        await _client.PutAsJsonAsync("/api/config", ValidConfig());

        var response = await _client.PutAsJsonAsync("/api/config", ValidConfig() with { GitToken = "glpat-****" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Put_WithNullSecret_KeepsExistingEncryptedValue()
    {
        await _client.PutAsJsonAsync("/api/config", ValidConfig());

        await _client.PutAsJsonAsync("/api/config", ValidConfig() with { GitToken = null, LlmApiKey = null });

        await using var conn = new NpgsqlConnection(factory.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """SELECT "GitToken" FROM configs LIMIT 1""";
        var storedToken = (string)(await cmd.ExecuteScalarAsync())!;
        storedToken.Should().NotBe("glpat-secret-token");
        storedToken.Should().NotBeNullOrEmpty();
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
        LlmMaxTokens: 16000,
        LlmThinkingBudget: 10000,
        DefaultPollCron: "0 */2 * * * ?"
    );
}
