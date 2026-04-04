using BuildingBlocks.Domain.Abstractions;
using Gloss.Domain.Configs;

namespace Gloss.Application.Configs;

public sealed record ConfigReadModel(
    bool IsConfigured,
    string? GitProvider,
    Uri? GitBaseUrl,
    string? GitToken,
    IReadOnlyList<string>? GitProjects,
    string? LlmProvider,
    string? LlmApiKey,
    string? LlmModel,
    bool? LlmReasoningEnabled,
    string? DefaultPollCron)
{
    public static readonly ConfigReadModel NotConfigured = new(
        IsConfigured: false,
        GitProvider: null,
        GitBaseUrl: null,
        GitToken: null,
        GitProjects: null,
        LlmProvider: null,
        LlmApiKey: null,
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
            GitToken: null,
            GitProjects: config.GitProjects.ToList(),
            LlmProvider: config.LlmProvider.Value,
            LlmApiKey: null,
            LlmModel: config.LlmModel,
            LlmReasoningEnabled: config.LlmReasoningEnabled,
            DefaultPollCron: config.DefaultPollCron);
    }
}
