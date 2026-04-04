using BuildingBlocks.Domain.Models.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Infrastructure.Api.Endpoints;

/// <summary>
/// Shared query string binding for paginated endpoints.
/// Converts raw int query params → validated Take/Skip value objects.
/// Bound via [AsParameters] in minimal API endpoints.
/// </summary>
public sealed class PaginationParams
{
    [FromQuery(Name = "take")] public int? RawTake { get; init; }
    [FromQuery(Name = "skip")] public int? RawSkip { get; init; }

    public Take GetTake() => Take.Create(RawTake ?? 20);
    public Skip GetSkip() => Skip.Create(RawSkip ?? 0);
}
