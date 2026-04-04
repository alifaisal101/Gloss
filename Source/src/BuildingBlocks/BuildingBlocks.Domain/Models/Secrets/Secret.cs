using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Domain.Models.Secrets;

public sealed class Secret : ValueObject, IMaskable
{
    public string Value { get; }

    private Secret(string value) => Value = value;

    public static Result<Secret> Create(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Result.Failure<Secret>(new DomainError("Secret.Empty", "A secret value cannot be empty.")) :
            Result.Success(new Secret(value));

    public MaskedSecret Mask() => MaskedSecret.From(this);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}