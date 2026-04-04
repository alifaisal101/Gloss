using System.Collections.ObjectModel;

namespace BuildingBlocks.Domain.Errors;

public record DomainError
{
    public string Code { get; }
    public string DefaultMessage { get; }
    public IReadOnlyList<object> Args { get; }

    public static readonly DomainError None = new(string.Empty, string.Empty);
    public static readonly DomainError NullValue = new("General.NullValue", "The specified result value is null.");

    public DomainError(string code, string defaultMessage, params object[] args)
    {
        Code = code;
        DefaultMessage = defaultMessage;
        Args = new ReadOnlyCollection<object>(args);
    }
    public string FormatMessage(IFormatProvider? provider = null) =>
        string.Format(provider, DefaultMessage, Args.ToArray());

    public static implicit operator string(DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.Code;
    }

    public override string ToString() => Code;
}