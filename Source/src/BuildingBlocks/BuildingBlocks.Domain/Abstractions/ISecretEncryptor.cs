using BuildingBlocks.Domain.Models.Secrets;

namespace BuildingBlocks.Domain.Abstractions;

public interface ISecretEncryptor
{
    EncryptedSecret Encrypt(Secret secret);
    Secret Decrypt(EncryptedSecret encryptedSecret);
}