using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Infrastructure.Api.Health;

public static class HealthCheckConfiguration
{
    /// <summary>
    /// <para>
    /// Registers health checks for ALL infrastructure:
    ///   - PostgreSQL (per connection string)
    ///   - Redis (if Cache enabled)
    ///   - Hangfire (if enabled)
    ///   - Custom module checks (via IHealthCheck registration with tags)
    /// </para>
    /// <para>
    /// Three endpoints:
    ///   GET /health        → Liveness (is the process alive?). Returns 200/503, no details.
    ///   GET /ready         → Readiness (can it serve traffic?). Checks Postgres + Redis + Hangfire.
    ///   GET /health/detail → Full diagnostic JSON. Every check with duration, status, description.
    ///                        Use this in your monitoring dashboard.
    /// </para>
    /// <para>
    /// Tags:
    ///   "live"  → included in /health
    ///   "ready" → included in /ready
    ///   (untagged checks appear in /health/detail only)
    /// </para>
    /// </summary>
    public static IServiceCollection AddBuildingBlocksHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var builder = services.AddHealthChecks();

        builder.AddCheck("self", () => HealthCheckResult.Healthy("Process is running"),
            tags: ["live"]);

        var connectionStrings = configuration.GetSection("ConnectionStrings");
        foreach (var cs in connectionStrings.GetChildren())
        {
            if (cs.Key.Equals("Hangfire", StringComparison.OrdinalIgnoreCase)) continue;

            var connStr = cs.Value;
            if (string.IsNullOrEmpty(connStr)) continue;

            builder.AddNpgSql(
                connectionString: connStr,
                name: $"postgres-{cs.Key.ToLowerInvariant()}",
                tags: ["ready", "db"],
                timeout: TimeSpan.FromSeconds(5));
        }

        var redisConnStr = configuration["Cache:RedisConnectionString"];
        if (!string.IsNullOrEmpty(redisConnStr))
        {
            builder.AddRedis(
                redisConnectionString: redisConnStr,
                name: "redis",
                tags: ["ready", "cache"],
                timeout: TimeSpan.FromSeconds(3));
        }

        var hangfireEnabled = configuration.GetValue("Hangfire:Enabled", true);
        var hangfireConnStr = configuration.GetConnectionString("Hangfire");
        if (hangfireEnabled && !string.IsNullOrEmpty(hangfireConnStr))
        {
            builder.AddHangfire(opts =>
            {
                opts.MinimumAvailableServers = 1;
                opts.MaximumJobsFailed = 50;
            },
            name: "hangfire",
            tags: ["ready", "jobs"]);
        }

        return services;
    }

    /// <summary>
    /// Maps the three health endpoints:
    ///   /health        → Liveness probe (Kubernetes livenessProbe)
    ///   /ready         → Readiness probe (Kubernetes readinessProbe)
    ///   /health/detail → Full diagnostic with HealthChecks.UI JSON format
    /// </summary>
    public static IEndpointRouteBuilder MapBuildingBlocksHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteLivenessResponse,
        });

        endpoints.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        endpoints.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        return endpoints;
    }

    private static async Task WriteLivenessResponse(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
        }, cancellationToken: ctx.RequestAborted).ConfigureAwait(false);
    }
}
