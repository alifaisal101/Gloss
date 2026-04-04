using System.Globalization;
using BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Api.Localization;

public static class LocalizationConfiguration
{
    /// <summary>
    /// <para>Configures request localization with Accept-Language header support.</para>
    /// <para>
    /// Supported cultures: en (default), ar (Arabic).
    /// Fallback chain: requested culture → en → generic message + log warning.
    /// </para>
    /// <para>
    /// To add a new culture: add a .resx file (Errors.{culture}.resx) and add the
    /// culture to supportedCultures below.
    /// </para>
    /// </summary>
    public static IServiceCollection AddBuildingBlocksLocalization(this IServiceCollection services)
    {
        services.AddLocalization();

        services.AddSingleton<IDomainErrorLocalizer, FallbackDomainErrorLocalizer>();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar"),
            };

            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider(),
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
            ];
        });

        return services;
    }

    /// <summary>
    /// Adds the request localization middleware. Call after UseRouting.
    /// Sets Thread.CurrentUICulture from Accept-Language header.
    /// </summary>
    public static IApplicationBuilder UseBuildingBlocksLocalization(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();
        return app;
    }
}
