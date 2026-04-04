using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.Repositories;

public static class RepositoryErrors
{
    public static readonly DomainError NotFound =
        new("Repository.NotFound", "Repository not found.");
}
