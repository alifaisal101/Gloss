using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests;

public sealed record MergeRequestReadModel(
    Guid Id,
    Guid RepositoryId,
    int ProviderIid,
    string Title,
    string SourceBranch,
    string TargetBranch,
    string AuthorUsername,
    string State)
{
    public static MergeRequestReadModel From(MergeRequest mr)
    {
        ArgumentNullException.ThrowIfNull(mr);
        return new(mr.Id, mr.RepositoryId, mr.ProviderIid, mr.Title, mr.SourceBranch, mr.TargetBranch, mr.AuthorUsername, mr.State.ToString());
    }
}
