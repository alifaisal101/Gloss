using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests.BusinessRules;

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
    public string? ReviewJobId { get; private set; }

    private MergeRequest() : base(Guid.NewGuid()) { }

    public void SetReviewJobId(string jobId) => ReviewJobId = jobId;

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

    public VoidResult MarkReviewing()
    {
        var shaRule = CheckRule(new MergeRequestHasHeadSha(HeadSha));
        if (shaRule.IsFailure) return shaRule.Error;

        var reviewingRule = CheckRule(new MergeRequestNotAlreadyReviewing(State));
        if (reviewingRule.IsFailure) return reviewingRule.Error;

        var diffRule = CheckRule(new MergeRequestDiffNotTooLarge(Diff));
        if (diffRule.IsFailure) return diffRule.Error;

        State = MergeRequestState.Reviewing;
        return Result.Success();
    }

    public void MarkReady() => State = MergeRequestState.Ready;

    public VoidResult MarkPublished()
    {
        var readyRule = CheckRule(new MergeRequestIsReady(State));
        if (readyRule.IsFailure) return readyRule.Error;

        State = MergeRequestState.Published;
        return Result.Success();
    }

    public void ResetToPending() => State = MergeRequestState.Pending;

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
