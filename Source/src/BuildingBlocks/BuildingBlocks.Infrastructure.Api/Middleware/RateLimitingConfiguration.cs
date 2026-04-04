using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Api.Middleware;

public static class RateLimitingConfiguration
{
    /// <summary>
    /// Adds fixed-window rate limiting per client IP.
    /// Returns 429 Too Many Requests with standard ApiResponse body.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var config = configuration.GetSection(RateLimitConfig.SectionName).Get<RateLimitConfig>() ?? new RateLimitConfig();

        if (!config.Enabled) return services;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.OnRejected = async (ctx, cancellationToken) =>
            {
                ctx.HttpContext.Response.ContentType = "application/json";
                await ctx.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = 429,
                    error = new { code = "RateLimit.Exceeded", message = "Too many requests. Please try again later." },
                    traceId = ctx.HttpContext.TraceIdentifier,
                }, cancellationToken).ConfigureAwait(false);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.PermitLimit,
                        Window = TimeSpan.FromSeconds(config.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = config.QueueLimit,
                    }));
        });

        return services;
    }

    public static IApplicationBuilder UseBuildingBlocksRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}
