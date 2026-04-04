using BuildingBlocks.Domain.Abstractions;
using Gloss.Domain.Configs;

namespace Gloss.Application.Configs;

public sealed record ConfigReadModel(
    bool IsConfigured,
    string? GitProvider,
    Uri? GitBaseUrl,
    bool GitTokenSet,
    IReadOnlyList<string>? GitProjects,
    string? LlmProvider,
    bool LlmApiKeySet,
    string? LlmModel,
    bool? LlmReasoningEnabled,
    string? DefaultPollCron)
{
    public static readonly ConfigReadModel NotConfigured = new(
        IsConfigured: false,
        GitProvider: null,
        GitBaseUrl: null,
        GitTokenSet: false,
        GitProjects: null,
        LlmProvider: null,
        LlmApiKeySet: false,
        LlmModel: null,
        LlmReasoningEnabled: null,
        DefaultPollCron: null);

    public static ConfigReadModel From(Config config, ISecretEncryptor encryptor)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(encryptor);

        return new(
            IsConfigured: true,
            GitProvider: config.GitProvider.Value,
            GitBaseUrl: config.GitBaseUrl,
            GitTokenSet: true,
            GitProjects: config.GitProjects.ToList(),
            LlmProvider: config.LlmProvider.Value,
            LlmApiKeySet: true,
            LlmModel: config.LlmModel,
            LlmReasoningEnabled: config.LlmReasoningEnabled,
            DefaultPollCron: config.DefaultPollCron);
    }
}
