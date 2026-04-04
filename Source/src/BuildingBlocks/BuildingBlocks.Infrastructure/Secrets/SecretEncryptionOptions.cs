namespace BuildingBlocks.Infrastructure.Secrets;

public sealed class SecretEncryptionOptions
{
    public const string SectionName = "SecretEncryption";

    public string KeyBase64 { get; set; } = string.Empty;
}