using Gloss.Domain.Configs;

namespace Gloss.UnitTests.Configs;

public sealed class LlmProviderTests
{
    [Theory]
    [InlineData("anthropic")]
    [InlineData("ANTHROPIC")]
    [InlineData("Anthropic")]
    public void Create_Anthropic_Succeeds(string input)
    {
        var result = LlmProvider.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProvider.Anthropic);
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("OPENAI")]
    [InlineData("OpenAI")]
    public void Create_OpenAi_Succeeds(string input)
    {
        var result = LlmProvider.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProvider.OpenAi);
    }

    [Theory]
    [InlineData("ollama")]
    [InlineData("OLLAMA")]
    [InlineData("Ollama")]
    public void Create_Ollama_Succeeds(string input)
    {
        var result = LlmProvider.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProvider.Ollama);
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("gpt")]
    [InlineData("gemini")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_UnknownProvider_Fails(string input)
    {
        var result = LlmProvider.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConfigErrors.InvalidLlmProvider);
    }

    [Fact]
    public void Create_Null_Fails()
    {
        var result = LlmProvider.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConfigErrors.InvalidLlmProvider);
    }

    [Fact]
    public void Anthropic_Value_IsLowercase()
    {
        LlmProvider.Anthropic.Value.Should().Be("anthropic");
    }

    [Fact]
    public void OpenAi_Value_IsLowercase()
    {
        LlmProvider.OpenAi.Value.Should().Be("openai");
    }

    [Fact]
    public void Ollama_Value_IsLowercase()
    {
        LlmProvider.Ollama.Value.Should().Be("ollama");
    }

    [Fact]
    public void Anthropic_Equals_Anthropic()
    {
        LlmProvider.Anthropic.Should().Be(LlmProvider.Anthropic);
    }

    [Fact]
    public void Anthropic_DoesNotEqual_OpenAi()
    {
        LlmProvider.Anthropic.Should().NotBe(LlmProvider.OpenAi);
    }

    [Fact]
    public void Anthropic_DoesNotEqual_Ollama()
    {
        LlmProvider.Anthropic.Should().NotBe(LlmProvider.Ollama);
    }

    [Fact]
    public void OpenAi_DoesNotEqual_Ollama()
    {
        LlmProvider.OpenAi.Should().NotBe(LlmProvider.Ollama);
    }

    [Fact]
    public void Create_Anthropic_ReturnsSingletonInstance()
    {
        var r1 = LlmProvider.Create("anthropic");
        var r2 = LlmProvider.Create("ANTHROPIC");

        r1.Value.Should().Be(r2.Value);
    }
}
