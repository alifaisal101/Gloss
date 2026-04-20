using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.MergeRequests.AddDraftComment;
using Gloss.Application.MergeRequests.DeleteDraftComment;
using Gloss.Application.MergeRequests.EditDraftComment;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.MergeRequests;

public static class DraftCommentEndpoints
{
    public static IEndpointRouteBuilder MapDraftCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/merge-requests/{mrId:guid}/comments").WithTags("DraftComments");

        group.MapPost("", async (
            Guid mrId,
            AddCommentRequest request,
            [FromServices] AddDraftCommentHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                return Results.BadRequest();

            var result = await handler.HandleAsync(
                mrId, request.FilePath, request.Line, request.Body, request.Reasoning, cancellationToken)
                .ConfigureAwait(false);
            return result.ToCreated(ctx, rm => $"/api/merge-requests/{mrId}/comments/{rm.Id}");
        });

        group.MapPut("/{commentId:guid}", async (
            Guid mrId,
            Guid commentId,
            EditCommentRequest request,
            [FromServices] EditDraftCommentHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                mrId, commentId, request.FilePath, request.Line, request.Body, request.Reasoning, cancellationToken)
                .ConfigureAwait(false);
            return result.ToNoContent(ctx);
        });

        group.MapDelete("/{commentId:guid}", async (
            Guid mrId,
            Guid commentId,
            [FromServices] DeleteDraftCommentHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, commentId, cancellationToken).ConfigureAwait(false);
            return result.ToNoContent(ctx);
        });

        return app;
    }

    private sealed record AddCommentRequest(string FilePath, int Line, string Body, string? Reasoning);
    private sealed record EditCommentRequest(string FilePath, int Line, string Body, string? Reasoning);
}
