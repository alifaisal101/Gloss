using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.Configs;

public static class ConfigErrors
{
    public static readonly DomainError InvalidGitProvider =
        new("Config.Validation.InvalidGitProvider", "Git provider must be 'gitlab' or 'github'.");

    public static readonly DomainError InvalidLlmProvider =
        new("Config.Validation.InvalidLlmProvider", "LLM provider must be 'anthropic', 'openai', or 'ollama'.");

    public static readonly DomainError InvalidGitBaseUrl =
        new("Config.Validation.InvalidGitBaseUrl", "Git base URL must be a valid absolute URL.");
}
