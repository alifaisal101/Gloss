namespace BuildingBlocks.Domain.Models.Secrets;

public sealed class MaskedSecret : ValueObject
{
    public static readonly MaskedSecret Redacted = new("****");

    private const int PrefixLength = 4;
    private const int MinLengthForPrefix = 8;

    public string Value { get; }

    private MaskedSecret(string value) => Value = value;

    internal static MaskedSecret From(Secret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        var masked = secret.Value.Length >= MinLengthForPrefix
            ? $"{secret.Value.AsSpan(0, PrefixLength)}****"
            : "****";
        return new MaskedSecret(masked);
    }

    public override string ToString() => Value;

    public static implicit operator string(MaskedSecret masked)
    {
        ArgumentNullException.ThrowIfNull(masked);
        return masked.Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}