using Gloss.Infrastructure.Reviews.Anthropic;
using Microsoft.Extensions.Configuration;

namespace Gloss.IntegrationTests.Reviews;

public sealed class LlmModelCatalogTests
{
    private static readonly LlmModelCatalog Catalog = Build();

    [Theory]
    [InlineData("claude-opus-4-8", true)]
    [InlineData("claude-sonnet-4-6", true)]
    [InlineData("claude-haiku-4-5-20251001", false)]
    [InlineData("unknown-model", false)]
    public void UsesAdaptiveThinking_MatchesConfiguredModels(string model, bool expected) =>
        Catalog.UsesAdaptiveThinking(model).Should().Be(expected);

    [Theory]
    [InlineData("claude-opus-4-8", 128000)]
    [InlineData("claude-sonnet-4-6", 64000)]
    public void MaxOutputTokens_ReturnsConfiguredLimit(string model, int expected) =>
        Catalog.MaxOutputTokens(model).Should().Be(expected);

    [Fact]
    public void MaxOutputTokens_ForUnknownModel_ReturnsNull() =>
        Catalog.MaxOutputTokens("unknown-model").Should().BeNull();

    private static LlmModelCatalog Build()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:Models:0:Id"] = "claude-opus-4-8",
                ["Anthropic:Models:0:MaxOutputTokens"] = "128000",
                ["Anthropic:Models:0:AdaptiveThinking"] = "true",
                ["Anthropic:Models:1:Id"] = "claude-sonnet-4-6",
                ["Anthropic:Models:1:MaxOutputTokens"] = "64000",
                ["Anthropic:Models:1:AdaptiveThinking"] = "true",
                ["Anthropic:Models:2:Id"] = "claude-haiku-4-5-20251001",
                ["Anthropic:Models:2:MaxOutputTokens"] = "64000",
                ["Anthropic:Models:2:AdaptiveThinking"] = "false",
            })
            .Build();
        return new LlmModelCatalog(configuration);
    }
}
