using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.Configs;

public static class ConfigErrors
{
    public static readonly DomainError InvalidGitProvider =
        new("Config.Validation.InvalidGitProvider", "Git provider must be 'gitlab' or 'github'.");

    public static readonly DomainError InvalidLlmProvider =
        new("Config.Validation.InvalidLlmProvider", "LLM provider must be 'anthropic', 'openai', or 'ollama'.");

    public static readonly DomainError InvalidLlmModel =
        new("Config.Validation.InvalidLlmModel", "The model is not valid for the selected LLM provider.");

    public static readonly DomainError MaxTokensExceedsModelLimit =
        new("Config.Validation.MaxTokensExceedsModelLimit", "Max tokens exceeds the selected model's maximum output tokens.");

    public static readonly DomainError InvalidGitBaseUrl =
        new("Config.Validation.InvalidGitBaseUrl", "Git base URL must be a valid absolute URL.");

    public static readonly DomainError SecretRequired =
        new("Config.Validation.SecretRequired", "Access token and API key are required on first save.");

    public static readonly DomainError MaskedSecretNotAccepted =
        new("Config.Validation.MaskedSecret", "Do not submit a masked secret. Enter the real value or leave blank to keep the current one.");
}
