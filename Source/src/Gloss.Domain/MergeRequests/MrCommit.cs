using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.MergeRequests;

public sealed class MrCommit : AggregateRoot<Guid>
{
    public Guid MergeRequestId { get; private set; }
    public string Sha { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string AuthorName { get; private set; } = null!;
    public string Diff { get; private set; } = null!;

    private MrCommit() : base(Guid.NewGuid()) { }

    public static MrCommit Create(Guid mergeRequestId, string sha, string title, string authorName, string diff)
    {
        var c = new MrCommit();
        c.MergeRequestId = mergeRequestId;
        c.Sha = sha;
        c.Title = title;
        c.AuthorName = authorName;
        c.Diff = diff;
        return c;
    }
}
