using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Infrastructure.Api.Documentation;
using BuildingBlocks.Infrastructure.Api.Features;
using BuildingBlocks.Infrastructure.Api.Health;
using BuildingBlocks.Infrastructure.Api.Jobs;
using BuildingBlocks.Infrastructure.Api.Localization;
using BuildingBlocks.Infrastructure.Api.Logging;
using BuildingBlocks.Infrastructure.Api.Middleware;
using BuildingBlocks.Infrastructure.Api.Observability;
using BuildingBlocks.Infrastructure.Api.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Api;

public static class ApiConfiguration
{
    /// <summary>
    /// <para>
    /// Registers ALL API infrastructure in one call:</para>
    /// <para>
    ///   - Scalar interactive docs + OpenAPI 3.1 spec generation
    ///   - Enterprise logging (silenced noise, source-generated)
    ///   - Localization (Arabic + English, Accept-Language header)
    ///   - OpenTelemetry (traces, metrics, structured logs → OTLP)
    ///   - Rate limiting (fixed-window per client IP)
    ///   - API versioning (URL segment: /api/v{n}/...)
    ///   - Feature flags (Microsoft.FeatureManagement with targeting, or Unleash)
    ///   - Hangfire (PostgreSQL storage, recurring job auto-discovery)
    ///   - Health checks (/health, /ready, /health/detail)
    ///   - Global exception handler (→ ApiResponse on unhandled)
    ///   - JSON conventions (camelCase, enums as strings)
    /// </para>
    /// </summary>
    public static IServiceCollection AddBuildingBlocksApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksLogging();
        services.AddBuildingBlocksLocalization();
        services.AddBuildingBlocksObservability(configuration);
        services.AddBuildingBlocksRateLimiting(configuration);
        services.AddBuildingBlocksApiVersioning();
        services.AddBuildingBlocksFeatureFlags(configuration);
        services.AddBuildingBlocksHangfire(configuration);
        services.AddBuildingBlocksHealthChecks(configuration);
        services.AddBuildingBlocksDocumentation(configuration);

        services.AddHttpContextAccessor();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }

    /// <summary>
    /// <para>
    /// Configures the middleware pipeline. Order matters:</para>
    /// <para>
    ///   1. Exception handler (outermost — catches everything)
    ///   2. Rate limiting (reject early)
    ///   3. Correlation ID (tracing header)
    ///   4. Request logging (timing + structured logs)
    ///   5. Localization (Accept-Language → CurrentUICulture)
    ///   6. Hangfire dashboard (/hangfire)
    /// </para>
    /// </summary>
    public static IApplicationBuilder UseBuildingBlocksApi(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        app.UseExceptionHandler();
        app.UseBuildingBlocksRateLimiting();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseBuildingBlocksLocalization();
        app.UseBuildingBlocksHangfire(configuration);
        return app;
    }
}
