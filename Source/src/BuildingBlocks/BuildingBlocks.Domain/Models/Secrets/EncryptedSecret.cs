namespace BuildingBlocks.Domain.Models.Secrets;

public sealed class EncryptedSecret : ValueObject, IMaskable
{
    public string CipherText { get; }

    private EncryptedSecret(string cipherText) => CipherText = cipherText;

    public static EncryptedSecret FromCipherText(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        return new EncryptedSecret(cipherText);
    }

    public MaskedSecret Mask() => MaskedSecret.Redacted;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CipherText;
    }
}