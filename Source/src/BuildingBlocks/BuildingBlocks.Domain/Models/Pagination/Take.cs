using System.Globalization;

namespace BuildingBlocks.Domain.Models.Pagination;

public sealed class Take : ValueObject
{
    public int Value { get; }

    private Take(int value) => Value = value;

    public static Take Create(int value)
    {
        if (value < 1) value = 1;
        if (value > 1000) value = 1000;
        return new Take(value);
    }

    public static Take Default() => new(10);

    public static bool TryParse(string? value, out Take? result)
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

    public static int ToInt32(Take take)
    {
        ArgumentNullException.ThrowIfNull(take);
        return take.Value;
    }

    public static implicit operator int(Take take)
    {
        ArgumentNullException.ThrowIfNull(take);
        return take.Value;
    }
}