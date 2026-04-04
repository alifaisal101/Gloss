using BuildingBlocks.Domain.Models.Pagination;

namespace BuildingBlocks.Application.Queries;
/// <summary>
/// Paged query with Take/Skip value objects.
/// No primitive obsession — validated pagination from the domain.
/// </summary>
public interface IPagedQuery : ICachedQuery
{
    Take Take { get; }
    Skip Skip { get; }
}