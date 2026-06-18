using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Results;

namespace Gloss.Domain.Configs;

public sealed class LlmProvider : ValueObject
{
    public static readonly LlmProvider Anthropic = new("anthropic", "claude-");
    public static readonly LlmProvider OpenAi = new("openai", "gpt-", "o1", "o3", "o4");
    public static readonly LlmProvider Ollama = new("ollama");

    private readonly string[] _modelPrefixes;

    public string Value { get; }

    private LlmProvider(string value, params string[] modelPrefixes)
    {
        Value = value;
        _modelPrefixes = modelPrefixes;
    }

    public static Result<LlmProvider> Create(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "anthropic" => Result.Success(Anthropic),
            "openai" => Result.Success(OpenAi),
            "ollama" => Result.Success(Ollama),
            _ => Result.Failure<LlmProvider>(ConfigErrors.InvalidLlmProvider),
        };

    /// <summary>
    /// A model is valid only for the provider that serves it. Hosted providers require a known family
    /// prefix and a clean lowercase, hyphenated id, so "claude-opus-4.8" (a dot instead of a hyphen)
    /// and an OpenAI model selected under Anthropic are both rejected. Ollama runs arbitrary local
    /// models, so any non-empty name is accepted.
    /// </summary>
    public bool IsValidModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model)) return false;
        if (_modelPrefixes.Length == 0) return true;
        return IsCleanModelId(model)
            && Array.Exists(_modelPrefixes, prefix => model.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool IsCleanModelId(string model) =>
        model.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
