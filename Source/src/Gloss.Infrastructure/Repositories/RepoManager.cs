using BuildingBlocks.Domain.Abstractions;
using Gloss.Application.Repositories;
using Gloss.Domain.Configs;
using Gloss.Domain.Repositories;
using Git = LibGit2Sharp;
using Microsoft.Extensions.Configuration;

namespace Gloss.Infrastructure.Repositories;

internal sealed class RepoManager(
    IConfigRepository configRepository,
    ISecretEncryptor encryptor,
    IConfiguration configuration) : IRepoManager
{
    public async Task<string> EnsureReadyAsync(
        Repository repository,
        string headSha,
        CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null)
            throw new InvalidOperationException("Gloss is not configured.");

        var token = encryptor.Decrypt(config.GitToken).Value;
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');
        var cloneUrl = $"{baseUrl}/{repository.ProjectPath}.git";

        if (repository.LocalClonePath is null)
        {
            var basePath = configuration["RepoBasePath"] ?? "/repos";
            var localPath = Path.Combine(basePath, repository.Id.ToString());
            await Task.Run(() => Clone(cloneUrl, localPath, token), cancellationToken).ConfigureAwait(false);
            return localPath;
        }

        await Task.Run(() => Fetch(repository.LocalClonePath, token), cancellationToken).ConfigureAwait(false);
        return repository.LocalClonePath;
    }

    private static void Clone(string cloneUrl, string localPath, string token)
    {
        if (Directory.Exists(localPath))
            Directory.Delete(localPath, recursive: true);
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        var options = new Git.CloneOptions
        {
            FetchOptions =
            {
                CredentialsProvider = (_, _, _) =>
                    new Git.UsernamePasswordCredentials { Username = "oauth2", Password = token }
            }
        };
        Git.Repository.Clone(cloneUrl, localPath, options);
    }

    public Task DeleteLocalCloneAsync(string localClonePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(localClonePath))
            Directory.Delete(localClonePath, recursive: true);
        return Task.CompletedTask;
    }

    private static void Fetch(string localPath, string token)
    {
        using var repo = new Git.Repository(localPath);
        var options = new Git.FetchOptions
        {
            CredentialsProvider = (_, _, _) =>
                new Git.UsernamePasswordCredentials { Username = "oauth2", Password = token }
        };
        Git.Commands.Fetch(repo, "origin", [], options, null);
    }
}
