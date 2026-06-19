using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.MergeRequests.DeleteMergeRequest;
using Gloss.Application.MergeRequests.GetMergeRequest;
using Gloss.Application.MergeRequests.IgnoreMergeRequest;
using Gloss.Application.MergeRequests.ListAllMergeRequests;
using Gloss.Application.MergeRequests.ListIgnoredMergeRequests;
using Gloss.Application.MergeRequests.ListMergeRequests;
using Gloss.Application.MergeRequests.UnignoreMergeRequest;
using Gloss.Application.MergeRequests.PullMergeRequests;
using Gloss.Application.Reviews.PublishMergeRequest;
using Gloss.Application.Reviews.ReviewMergeRequest;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.MergeRequests;

public static class MergeRequestEndpoints
{
    public static IEndpointRouteBuilder MapMergeRequestEndpoints(this IEndpointRouteBuilder app)
    {
        MapMrRoutes(app);
        MapRepositoryRoutes(app);
        return app;
    }

    private static void MapMrRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/merge-requests", async (
            [FromServices] ListAllMergeRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).WithTags("MergeRequests");

        app.MapGet("/api/merge-requests/{mrId:guid}", async (
            Guid mrId,
            [FromServices] GetMergeRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("MergeRequests");

        app.MapPost("/api/merge-requests/{mrId:guid}/review", async (
            Guid mrId,
            [FromServices] ReviewMergeRequestHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        }).WithTags("MergeRequests");

        app.MapPost("/api/merge-requests/{mrId:guid}/publish", async (
            Guid mrId,
            [FromServices] PublishMergeRequestHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        }).WithTags("MergeRequests");

        app.MapDelete("/api/merge-requests/{mrId:guid}", async (
            Guid mrId,
            [FromServices] DeleteMergeRequestHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result.ToNoContent(ctx);
        }).WithTags("MergeRequests");

        app.MapPost("/api/merge-requests/{mrId:guid}/ignore", async (
            Guid mrId,
            [FromServices] IgnoreMergeRequestHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result.ToNoContent(ctx);
        }).WithTags("MergeRequests");

        app.MapGet("/api/ignored-merge-requests", async (
            [FromServices] ListIgnoredMergeRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).WithTags("MergeRequests");

        app.MapDelete("/api/ignored-merge-requests/{ignoredId:guid}", async (
            Guid ignoredId,
            [FromServices] UnignoreMergeRequestHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(ignoredId, cancellationToken).ConfigureAwait(false);
            return result.ToNoContent(ctx);
        }).WithTags("MergeRequests");
    }

    private static void MapRepositoryRoutes(IEndpointRouteBuilder app)
    {
        var repoGroup = app.MapGroup("/api/repositories/{repositoryId:guid}").WithTags("MergeRequests");

        repoGroup.MapGet("/merge-requests", async (
            Guid repositoryId,
            [FromServices] ListMergeRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        repoGroup.MapPost("/pull-reviews", async (
            Guid repositoryId,
            [FromServices] PullMergeRequestsHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });
    }
}
