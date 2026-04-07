using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.MergeRequests.PollAllRepositories;
using Gloss.Application.Repositories.ListRepositories;
using Gloss.Application.Repositories.UpdateRepository;
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

        group.MapPost("/poll-all", async (
            [FromServices] PollAllRepositoriesHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateRepositoryCommand command,
            [FromServices] UpdateRepositoryHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command with { RepositoryId = id }, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });

        return app;
    }
}
