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
    public MergeRequestStatus Status { get; private set; } = null!;
    public PlatformMrStatus PlatformStatus { get; private set; } = null!;
    public string? ReviewJobId { get; private set; }
    public ApprovalStatus Approval { get; private set; } = null!;

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
        mr.Status = new MergeRequestStatus.Pending(DateTimeOffset.UtcNow);
        mr.PlatformStatus = new PlatformMrStatus.Open();
        mr.Approval = new ApprovalStatus.NotApproved();
        mr.Apply(title, description, sourceBranch, targetBranch, authorUsername, diff, baseSha, headSha, startSha);
        return mr;
    }

    public VoidResult BeginReview()
    {
        var shaRule = CheckRule(new MergeRequestHasHeadSha(HeadSha));
        if (shaRule.IsFailure) return shaRule.Error;

        var reviewingRule = CheckRule(new MergeRequestNotAlreadyReviewing(Status));
        if (reviewingRule.IsFailure) return reviewingRule.Error;

        var diffRule = CheckRule(new MergeRequestDiffNotTooLarge(Diff));
        if (diffRule.IsFailure) return diffRule.Error;

        Status = new MergeRequestStatus.Reviewing(DateTimeOffset.UtcNow);
        return Result.Success();
    }

    public void CompleteReview() => Status = new MergeRequestStatus.Ready(DateTimeOffset.UtcNow);

    public VoidResult Publish(Guid? byUserId = null)
    {
        var readyRule = CheckRule(new MergeRequestIsReady(Status));
        if (readyRule.IsFailure) return readyRule.Error;

        Status = new MergeRequestStatus.Published(DateTimeOffset.UtcNow, byUserId);
        return Result.Success();
    }

    public void ResetToPending() => Status = new MergeRequestStatus.Pending(DateTimeOffset.UtcNow);

    public void MarkSeen(Guid? byUserId = null)
    {
        if (Status is MergeRequestStatus.Ready)
            Status = new MergeRequestStatus.Seen(DateTimeOffset.UtcNow, byUserId);
    }

    public void MarkStaged(Guid? byUserId = null)
    {
        if (Status is MergeRequestStatus.Pending or MergeRequestStatus.Ready or MergeRequestStatus.Seen)
            Status = new MergeRequestStatus.Staged(DateTimeOffset.UtcNow, byUserId);
    }

    public void UpdatePlatformStatus(PlatformMrStatus status) => PlatformStatus = status;

    public void UpdateApproval(ApprovalStatus approval) => Approval = approval;

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
