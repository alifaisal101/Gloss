using System.Globalization;

namespace BuildingBlocks.Domain.Errors;

public interface IDomainErrorLocalizer
{
    string Localize(DomainError domainError, CultureInfo culture);
}