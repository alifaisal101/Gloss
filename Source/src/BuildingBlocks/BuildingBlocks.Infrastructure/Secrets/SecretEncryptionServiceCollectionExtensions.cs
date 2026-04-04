using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Secrets;

public static class SecretEncryptionServiceCollectionExtensions
{
    public static IServiceCollection AddSecretEncryption(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<SecretEncryptionOptions>(configuration.GetSection(SecretEncryptionOptions.SectionName));
        services.AddSingleton<ISecretEncryptor, AesGcmSecretEncryptor>();
        return services;
    }
}