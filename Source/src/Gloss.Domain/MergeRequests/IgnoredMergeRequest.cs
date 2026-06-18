using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.MergeRequests;

public sealed class IgnoredMergeRequest : AggregateRoot<Guid>
{
    public Guid RepositoryId { get; private set; }
    public int ProviderIid { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTimeOffset IgnoredAt { get; private set; }

    private IgnoredMergeRequest() : base(Guid.NewGuid()) { }

    public static IgnoredMergeRequest Create(Guid repositoryId, int providerIid, string title)
    {
        var ignored = new IgnoredMergeRequest();
        ignored.RepositoryId = repositoryId;
        ignored.ProviderIid = providerIid;
        ignored.Title = title;
        ignored.IgnoredAt = DateTimeOffset.UtcNow;
        return ignored;
    }
}
