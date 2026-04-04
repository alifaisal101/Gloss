using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.Repositories.ListRepositories;
using Gloss.Application.Repositories.UpdatePollCron;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.Repositories;

public static class RepositoryEndpoints
{
    public static IEndpointRouteBuilder MapRepositoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories").WithTags("Repositories");

        group.MapGet("/", async (
            [FromServices] ListRepositoriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdatePollCronCommand command,
            [FromServices] UpdatePollCronHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command with { RepositoryId = id }, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });

        return app;
    }
}
