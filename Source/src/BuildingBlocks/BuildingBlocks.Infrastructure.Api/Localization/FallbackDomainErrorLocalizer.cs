using System.Globalization;
using System.Resources;
using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Infrastructure.Api.Localization.Resources;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Api.Localization;

/// <summary>
/// <para>Domain error localizer with three-level fallback:</para>
/// <para>
///   1. Look up error code in module resources (registered via DI)
///   2. If not found → look up in core resources (English fallback)
///   3. If still not found → use DomainError.DefaultMessage + LOG WARNING
/// </para>
/// <para>
/// Modules register their ResourceManagers as singletons:
///   services.AddSingleton(new ResourceManager("MyModule.Resources.Errors", typeof(X).Assembly));
/// </para>
/// <para>This localizer auto-discovers them all.</para>
/// </summary>
internal sealed partial class FallbackDomainErrorLocalizer(
    IEnumerable<ResourceManager> moduleResources,
    ILogger<FallbackDomainErrorLocalizer> logger)
    : IDomainErrorLocalizer
{
    private static readonly ResourceManager CoreResources =
        new("BuildingBlocks.Infrastructure.Api.Localization.Resources.IErrors", typeof(IErrors).Assembly);

    private static readonly CultureInfo EnglishFallback = new("en");

    private readonly IReadOnlyList<ResourceManager> _moduleResources = moduleResources.ToList();

    public string Localize(DomainError domainError, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(domainError);

        var message = TryResolve(domainError.Code, culture);

        if (message is null && !culture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
            message = TryResolve(domainError.Code, EnglishFallback);

        if (message is null)
        {
            LogMissingLocalization(domainError.Code, culture.Name);
            message = domainError.DefaultMessage;
        }

        return domainError.Args.Count > 0
            ? string.Format(culture, message, domainError.Args.ToArray())
            : message;
    }

    private string? TryResolve(string code, CultureInfo culture)
    {
        foreach (var rm in _moduleResources)
        {
            var result = rm.GetString(code, culture);
            if (!string.IsNullOrEmpty(result)) return result;
        }

        return CoreResources.GetString(code, culture);
    }

    [LoggerMessage(EventId = 3000, Level = LogLevel.Warning,
        Message = "[Localization] No message found for '{Code}' in culture '{Culture}'. Using DefaultMessage.")]
    private partial void LogMissingLocalization(string code, string culture);
}
