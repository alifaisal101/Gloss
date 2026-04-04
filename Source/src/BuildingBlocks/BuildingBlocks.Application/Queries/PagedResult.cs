using BuildingBlocks.Domain.Models.Pagination;

namespace BuildingBlocks.Application.Queries;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    Take Take,
    Skip Skip)
{
    public bool HasMore => Skip.Value + Items.Count < TotalCount;
}

public static class PagedResult
{
    public static PagedResult<T> Empty<T>(Take take, Skip skip) =>
        new([], 0, take, skip);
}