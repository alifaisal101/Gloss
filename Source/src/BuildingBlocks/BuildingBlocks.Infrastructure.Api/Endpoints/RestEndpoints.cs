using BuildingBlocks.Domain.Models.Pagination;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Infrastructure.Api.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildingBlocks.Infrastructure.Api.Endpoints;

/// <summary>
/// Opinionated REST endpoint builders. Enforce HTTP semantics:
///
///   POST   → 201 Created (with Location header)
///   GET    → 200 OK
///   DELETE → 204 No Content
///   PUT    → 200 OK
///
/// All errors → standardized ApiResponse with correct status code.
/// All pagination → Take/Skip value objects enforced.
///
/// Usage:
///   group.MapCreate&lt;CreateSubCommand, Guid&gt;("/",
///       handler.HandleAsync, id => $"/api/v1/subs/{id}");
///
///   group.MapRemove&lt;Guid&gt;("/{id:guid}",
///       (id, cancellationToken) => handler.HandleAsync(new DeleteCmd(id), cancellationToken));
///
///   group.MapPagedQuery&lt;GetSubsQuery, Subscription&gt;("/user/{userId:guid}",
///       (userId, take, skip, cancellationToken) => handler.HandleAsync(new GetSubsQuery(userId, take, skip), cancellationToken));
///
/// No manual Results.Ok/BadRequest/Created. Convention-driven.
/// Another dev CAN'T return 200 from a DELETE or 201 from a GET.
/// </summary>
public static class RestEndpoints
{
    // ─── POST → 201 Created ──────────────────────────────────

    /// <summary>
    /// POST that creates a resource. Always returns 201 with Location header on success.
    /// </summary>
    public static RouteHandlerBuilder MapCreate<TCommand, TId>(
        this RouteGroupBuilder group,
        string pattern,
        Func<TCommand, CancellationToken, Task<Result<TId>>> handler,
        Func<TId, string> locationFactory)
    {
        return group.MapPost(pattern, async (TCommand cmd, HttpContext ctx, CancellationToken cancellationToken) =>
        {
            var result = await handler(cmd, cancellationToken).ConfigureAwait(false);
            return result.ToCreated(ctx, id => locationFactory(id));
        });
    }

    // ─── DELETE → 204 No Content ─────────────────────────────

    /// <summary>
    /// DELETE that removes a resource. Always returns 204 on success.
    /// </summary>
    public static RouteHandlerBuilder MapRemove(
        this RouteGroupBuilder group,
        string pattern,
        Func<Guid, CancellationToken, Task<VoidResult>> handler)
    {
        return group.MapDelete(pattern, async (Guid id, HttpContext ctx, CancellationToken cancellationToken) =>
        {
            var result = await handler(id, cancellationToken).ConfigureAwait(false);
            return result.ToNoContent(ctx);
        });
    }

    /// <summary>DELETE with a typed ID.</summary>
    public static RouteHandlerBuilder MapRemove<TId>(
        this RouteGroupBuilder group,
        string pattern,
        Func<TId, CancellationToken, Task<VoidResult>> handler)
        where TId : IParsable<TId>
    {
        return group.MapDelete(pattern, async (TId id, HttpContext ctx, CancellationToken cancellationToken) =>
        {
            var result = await handler(id, cancellationToken).ConfigureAwait(false);
            return result.ToNoContent(ctx);
        });
    }

    // ─── GET (single) → 200 OK ──────────────────────────────

    /// <summary>
    /// GET that returns a single resource. 200 or mapped error.
    /// </summary>
    public static RouteHandlerBuilder MapGet<TResult>(
        this RouteGroupBuilder group,
        string pattern,
        Func<Guid, CancellationToken, Task<Result<TResult>>> handler)
    {
        return group.MapGet(pattern, async (Guid id, HttpContext ctx, CancellationToken cancellationToken) =>
        {
            var result = await handler(id, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });
    }

    // ─── GET (paged) → 200 OK ────────────────────────────────

    /// <summary>
    /// GET that returns a paged list. Always 200. Enforces Take/Skip value objects.
    /// Route must have a {parentId:guid} parameter.
    /// </summary>
    public static RouteHandlerBuilder MapPagedQuery<TResult>(
        this RouteGroupBuilder group,
        string pattern,
        Func<Guid, Take, Skip, CancellationToken, Task<TResult>> handler)
    {
        return group.MapGet(pattern, async (
            Guid parentId,
            int? take,
            int? skip,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var t = Take.Create(take ?? 20);
            var s = Skip.Create(skip ?? 0);
            var result = await handler(parentId, t, s, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });
    }

    // ─── PUT → 200 OK ────────────────────────────────────────

    /// <summary>
    /// PUT that updates a resource. Always returns 200 on success.
    /// </summary>
    public static RouteHandlerBuilder MapUpdate<TCommand>(
        this RouteGroupBuilder group,
        string pattern,
        Func<TCommand, CancellationToken, Task<VoidResult>> handler)
    {
        return group.MapPut(pattern, async (TCommand cmd, HttpContext ctx, CancellationToken cancellationToken) =>
        {
            var result = await handler(cmd, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });
    }
}
