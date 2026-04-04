using System.Globalization;

namespace BuildingBlocks.Domain.Models.Pagination;

public sealed class Skip : ValueObject
{
    public int Value { get; }

    private Skip(int value) => Value = value;

    public static Skip Create(int value) => new(Math.Max(0, value));

    public static Skip Default() => new(0);

    public static bool TryParse(string? value, out Skip? result)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            result = Create(intValue);
            return true;
        }
        result = Default();
        return true;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static int ToInt32(Skip skip) => skip?.Value ?? 0;

    public static implicit operator int(Skip skip)
    {
        ArgumentNullException.ThrowIfNull(skip);
        return skip.Value;
    }
}