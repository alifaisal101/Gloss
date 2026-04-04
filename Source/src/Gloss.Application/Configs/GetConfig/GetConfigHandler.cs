using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.Configs;

namespace Gloss.Application.Configs.GetConfig;

public sealed class GetConfigHandler(IConfigRepository repository, ISecretEncryptor encryptor)
{
    public async Task<Result<ConfigReadModel>> HandleAsync(CancellationToken cancellationToken)
    {
        var config = await repository.FindAsync(cancellationToken).ConfigureAwait(false);

        return config is null
            ? Result.Success(ConfigReadModel.NotConfigured)
            : Result.Success(ConfigReadModel.From(config, encryptor));
    }
}
