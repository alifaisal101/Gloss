using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models.Secrets;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.Configs;

namespace Gloss.Application.Configs.SaveConfig;

public sealed class SaveConfigHandler(
    IConfigRepository repository,
    IDomainContext domainContext,
    ISecretEncryptor encryptor)
{
    public async Task<VoidResult> HandleAsync(SaveConfigCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.GitBaseUrl.IsAbsoluteUri)
            return ConfigErrors.InvalidGitBaseUrl;

        var gitProviderResult = GitProvider.Create(command.GitProvider);
        if (gitProviderResult.IsFailure) return gitProviderResult.Error;

        var llmProviderResult = LlmProvider.Create(command.LlmProvider);
        if (llmProviderResult.IsFailure) return llmProviderResult.Error;

        var gitTokenResult = Secret.Create(command.GitToken);
        if (gitTokenResult.IsFailure) return gitTokenResult.Error;

        var llmApiKeyResult = Secret.Create(command.LlmApiKey);
        if (llmApiKeyResult.IsFailure) return llmApiKeyResult.Error;

        var encryptedGitToken = encryptor.Encrypt(gitTokenResult.Value);
        var encryptedLlmApiKey = encryptor.Encrypt(llmApiKeyResult.Value);

        var existing = await repository.FindAsync(cancellationToken).ConfigureAwait(false);

        Config config;
        if (existing is null)
        {
            config = Config.Create(
                gitProviderResult.Value,
                command.GitBaseUrl,
                encryptedGitToken,
                command.GitProjects,
                llmProviderResult.Value,
                encryptedLlmApiKey,
                command.LlmModel,
                command.LlmReasoningEnabled,
                command.DefaultPollCron);
        }
        else
        {
            existing.Update(
                gitProviderResult.Value,
                command.GitBaseUrl,
                encryptedGitToken,
                command.GitProjects,
                llmProviderResult.Value,
                encryptedLlmApiKey,
                command.LlmModel,
                command.LlmReasoningEnabled,
                command.DefaultPollCron);
            config = existing;
        }

        domainContext.Save<Config, Guid>(config);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
