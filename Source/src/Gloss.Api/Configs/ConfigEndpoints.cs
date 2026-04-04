using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.Configs.GetConfig;
using Gloss.Application.Configs.SaveConfig;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.Configs;

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config").WithTags("Config");

        group.MapGet("/", async (
            [FromServices] GetConfigHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result.Value);
        });

        group.MapPut("/", async (
            SaveConfigCommand command,
            [FromServices] SaveConfigHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });

        return app;
    }
}
