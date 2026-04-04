using System.Security.Cryptography;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models.Secrets;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Secrets;

public sealed class AesGcmSecretEncryptor : ISecretEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmSecretEncryptor(IOptions<SecretEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _key = Convert.FromBase64String(options.Value.KeyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("SecretEncryption:KeyBase64 must be a 32-byte (256-bit) key encoded as base64.");
    }

    public EncryptedSecret Encrypt(Secret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var plainText = System.Text.Encoding.UTF8.GetBytes(secret.Value);
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherText = new byte[plainText.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        var combined = new byte[NonceSize + cipherText.Length + TagSize];
        nonce.CopyTo(combined, 0);
        cipherText.CopyTo(combined, NonceSize);
        tag.CopyTo(combined, NonceSize + cipherText.Length);

        return EncryptedSecret.FromCipherText(Convert.ToBase64String(combined));
    }

    public Secret Decrypt(EncryptedSecret encryptedSecret)
    {
        ArgumentNullException.ThrowIfNull(encryptedSecret);

        var combined = Convert.FromBase64String(encryptedSecret.CipherText);
        var nonce = combined[..NonceSize];
        var cipherText = combined[NonceSize..^TagSize];
        var tag = combined[^TagSize..];
        var plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainText);

        var result = Secret.Create(System.Text.Encoding.UTF8.GetString(plainText));
        return result.Value;
    }
}
