using System.Globalization;
using System.Resources;
using BuildingBlocks.Domain.Errors;

namespace BuildingBlocks.Infrastructure.Localization;

public class ResourceBasedLocalizer(ResourceManager resourceManager) : IDomainErrorLocalizer
{
    public string Localize(DomainError domainError, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(domainError);
        var localizedString = resourceManager.GetString(domainError.Code, culture);

        if (string.IsNullOrEmpty(localizedString)) localizedString = domainError.DefaultMessage;

        return domainError.Args.Count > 0 ? string.Format(culture, localizedString, domainError.Args.ToArray()) :
            localizedString;
    }
}