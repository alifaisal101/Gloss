using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Infrastructure.Api.Observability;

public static class OpenTelemetryConfiguration
{
    /// <summary>
    /// Activity source for custom spans across building blocks.
    /// Usage: using var activity = Telemetry.Source.StartActivity("ProcessBatch");
    /// </summary>
    public static readonly ActivitySource Source = new("BuildingBlocks");
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var config = configuration.GetSection(ObservabilityConfig.SectionName).Get<ObservabilityConfig>()
            ?? new ObservabilityConfig();

        if (!config.Enabled) return services;
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(config.ServiceName, serviceVersion: config.ServiceVersion)
            .AddAttributes([new KeyValuePair<string, object>("deployment.environment",
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"),]);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("BuildingBlocks")
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
                                             && !ctx.Request.Path.StartsWithSegments("/ready", StringComparison.OrdinalIgnoreCase);
                    })
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (!string.IsNullOrEmpty(config.OtlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));
                else if (config.ConsoleExporter)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(config.OtlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));
            });

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(otel =>
            {
                otel.SetResourceBuilder(resourceBuilder);
                otel.IncludeScopes = true;
                otel.IncludeFormattedMessage = true;

                if (!string.IsNullOrEmpty(config.OtlpEndpoint))
                    otel.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));
                else if (config.ConsoleExporter)
                    otel.AddConsoleExporter();
            });
        });

        return services;
    }
}
