using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Results;

namespace Gloss.Domain.Configs;

public sealed class LlmProvider : ValueObject
{
    public static readonly LlmProvider Anthropic = new("anthropic");
    public static readonly LlmProvider OpenAi = new("openai");
    public static readonly LlmProvider Ollama = new("ollama");

    public string Value { get; }

    private LlmProvider(string value) => Value = value;

    public static Result<LlmProvider> Create(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "anthropic" => Result.Success(Anthropic),
            "openai" => Result.Success(OpenAi),
            "ollama" => Result.Success(Ollama),
            _ => Result.Failure<LlmProvider>(ConfigErrors.InvalidLlmProvider),
        };

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
