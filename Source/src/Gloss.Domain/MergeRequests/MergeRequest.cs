using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.MergeRequests;

public sealed class MergeRequest : AggregateRoot<Guid>
{
    public Guid RepositoryId { get; private set; }
    public int ProviderIid { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string SourceBranch { get; private set; } = null!;
    public string TargetBranch { get; private set; } = null!;
    public string AuthorUsername { get; private set; } = null!;
    public string Diff { get; private set; } = null!;
    public string? BaseSha { get; private set; }
    public string? HeadSha { get; private set; }
    public string? StartSha { get; private set; }

    public MergeRequestState State { get; private set; }

    private MergeRequest() : base(Guid.NewGuid()) { }

    public static MergeRequest Create(
        Guid repositoryId,
        int providerIid,
        string title,
        string? description,
        string sourceBranch,
        string targetBranch,
        string authorUsername,
        string diff,
        string? baseSha,
        string? headSha,
        string? startSha)
    {
        var mr = new MergeRequest();
        mr.RepositoryId = repositoryId;
        mr.ProviderIid = providerIid;
        mr.State = MergeRequestState.Pending;
        mr.Apply(title, description, sourceBranch, targetBranch, authorUsername, diff, baseSha, headSha, startSha);
        return mr;
    }

    public void MarkReady() => State = MergeRequestState.Ready;
    public void MarkPublished() => State = MergeRequestState.Published;

    public void Update(
        string title,
        string? description,
        string sourceBranch,
        string targetBranch,
        string authorUsername,
        string diff,
        string? baseSha,
        string? headSha,
        string? startSha) =>
        Apply(title, description, sourceBranch, targetBranch, authorUsername, diff, baseSha, headSha, startSha);

    private void Apply(
        string title,
        string? description,
        string sourceBranch,
        string targetBranch,
        string authorUsername,
        string diff,
        string? baseSha,
        string? headSha,
        string? startSha)
    {
        Title = title;
        Description = description;
        SourceBranch = sourceBranch;
        TargetBranch = targetBranch;
        AuthorUsername = authorUsername;
        Diff = diff;
        BaseSha = baseSha;
        HeadSha = headSha;
        StartSha = startSha;
    }
}
