using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Domain.Models.Secrets;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Jobs;
using Gloss.Domain.Configs;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Configs.SaveConfig;

public sealed class SaveConfigHandler(
    IConfigRepository repository,
    IRepositoryRepository repositoryRepository,
    IDomainContext domainContext,
    ISecretEncryptor encryptor,
    IJobScheduler jobScheduler)
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

        var existing = await repository.FindAsync(cancellationToken).ConfigureAwait(false);

        var encryptedGitToken = ResolveSecret(command.GitToken, existing?.GitToken, out var gitTokenError);
        if (gitTokenError is not null) return gitTokenError;

        var encryptedLlmApiKey = ResolveSecret(command.LlmApiKey, existing?.LlmApiKey, out var llmKeyError);
        if (llmKeyError is not null) return llmKeyError;

        Config config;
        if (existing is null)
        {
            config = Config.Create(
                gitProviderResult.Value,
                command.GitBaseUrl,
                encryptedGitToken!,
                command.GitProjects,
                llmProviderResult.Value,
                encryptedLlmApiKey!,
                command.LlmModel,
                command.LlmReasoningEnabled,
                command.DefaultPollCron);
        }
        else
        {
            existing.Update(
                gitProviderResult.Value,
                command.GitBaseUrl,
                encryptedGitToken!,
                command.GitProjects,
                llmProviderResult.Value,
                encryptedLlmApiKey!,
                command.LlmModel,
                command.LlmReasoningEnabled,
                command.DefaultPollCron);
            config = existing;
        }

        domainContext.Save<Config, Guid>(config);

        await SyncRepositoriesAsync(command.GitProjects, gitProviderResult.Value.Value, cancellationToken).ConfigureAwait(false);

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        jobScheduler.SchedulePollAll(command.DefaultPollCron);

        return Result.Success();
    }

    private EncryptedSecret? ResolveSecret(string? incoming, EncryptedSecret? existing, out DomainError? error)
    {
        error = null;
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            if (incoming.Contains('*'))
            {
                error = ConfigErrors.MaskedSecretNotAccepted;
                return null;
            }
            var result = Secret.Create(incoming.Trim());
            if (result.IsFailure) { error = result.Error; return null; }
            return encryptor.Encrypt(result.Value);
        }
        if (existing is not null) return existing;
        error = ConfigErrors.SecretRequired;
        return null;
    }

    private async Task SyncRepositoriesAsync(IReadOnlyList<string> projects, string provider, CancellationToken cancellationToken)
    {
        var existingRepos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);

        var existingPaths = existingRepos.Select(r => r.ProjectPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newPaths = projects.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var repo in existingRepos.Where(r => !newPaths.Contains(r.ProjectPath)))
            domainContext.Remove<Repository, Guid>(repo);

        foreach (var path in projects.Where(p => !existingPaths.Contains(p)))
            domainContext.Save<Repository, Guid>(Repository.Create(path, provider));
    }
}
