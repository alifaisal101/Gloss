namespace Gloss.Application.MergeRequests;

public sealed record MrCommitReadModel(string Sha, string Title, string AuthorName, string Diff)
{
    public static MrCommitReadModel From(Domain.MergeRequests.MrCommit c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new(c.Sha, c.Title, c.AuthorName, c.Diff);
    }
}
