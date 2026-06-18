using System.Net;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models.Secrets;
using Gloss.Domain.Configs;
using Gloss.Infrastructure.Reviews.Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gloss.IntegrationTests.Reviews;

public sealed class AnthropicThinkingRequestTests
{
    private const string AdaptiveModel = "claude-opus-4-8";
    private const string LegacyModel = "claude-sonnet-4-6";

    [Fact]
    public async Task Reasoning_OnAdaptiveModel_UsesAdaptiveThinkingNotBudget()
    {
        var body = await CaptureRequestBodyAsync(AdaptiveModel);

        var thinking = body.GetProperty("thinking");
        thinking.GetProperty("type").GetString().Should().Be("adaptive");
        thinking.TryGetProperty("budget_tokens", out _).Should().BeFalse();
        body.TryGetProperty("output_config", out var outputConfig).Should().BeTrue();
        outputConfig.GetProperty("effort").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Reasoning_OnLegacyModel_UsesEnabledThinkingWithBudget()
    {
        var body = await CaptureRequestBodyAsync(LegacyModel, thinkingBudget: 12000);

        var thinking = body.GetProperty("thinking");
        thinking.GetProperty("type").GetString().Should().Be("enabled");
        thinking.GetProperty("budget_tokens").GetInt32().Should().Be(12000);
        body.TryGetProperty("output_config", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ReasoningDisabled_SendsNoThinkingOrOutputConfig()
    {
        var body = await CaptureRequestBodyAsync(AdaptiveModel, reasoning: false);

        body.TryGetProperty("thinking", out _).Should().BeFalse();
        body.TryGetProperty("output_config", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(5000, "low")]
    [InlineData(15000, "medium")]
    [InlineData(30000, "high")]
    public async Task AdaptiveModel_MapsThinkingBudgetToEffort(int thinkingBudget, string expectedEffort)
    {
        var body = await CaptureRequestBodyAsync(AdaptiveModel, thinkingBudget: thinkingBudget);

        body.GetProperty("output_config").GetProperty("effort").GetString().Should().Be(expectedEffort);
    }

    private static async Task<JsonElement> CaptureRequestBodyAsync(string model, bool reasoning = true, int thinkingBudget = 10000)
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.test/") };

        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Decrypt(It.IsAny<EncryptedSecret>())).Returns(Secret.Create("sk-ant-test").Value);

        var config = Config.Create(
            GitProvider.GitLab,
            new Uri("https://gitlab.example.com"),
            EncryptedSecret.FromCipherText("git-cipher"),
            [],
            LlmProvider.Anthropic,
            EncryptedSecret.FromCipherText("llm-cipher"),
            model,
            reasoning,
            16000,
            thinkingBudget,
            "0 */2 * * * ?");

        var configRepository = new Mock<IConfigRepository>();
        configRepository.Setup(r => r.FindAsync(It.IsAny<CancellationToken>())).ReturnsAsync(config);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:ApiVersion"] = "2023-06-01",
                ["Anthropic:DefaultModel"] = LegacyModel,
                ["Anthropic:AdaptiveThinkingModels:0"] = AdaptiveModel,
            })
            .Build();

        var client = new AnthropicApiClient(
            httpClient, configRepository.Object, encryptor.Object, configuration, NullLogger<AnthropicApiClient>.Instance);

        await client.SendAsync(
            "system prompt",
            [new ClaudeMessage("user", [new ClaudeTextContent("review")])],
            [],
            CancellationToken.None);

        using var doc = JsonDocument.Parse(handler.RequestBody!);
        return doc.RootElement.Clone();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"stop_reason":"end_turn","content":[]}""", Encoding.UTF8, "application/json"),
            };
        }
    }
}
