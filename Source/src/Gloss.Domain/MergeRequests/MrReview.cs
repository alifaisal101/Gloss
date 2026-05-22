using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests.BusinessRules;

namespace Gloss.Domain.MergeRequests;

public sealed class MrReview : AggregateRoot<Guid>
{
    public Guid MergeRequestId { get; private set; }
    public Guid UserId { get; private set; }
    public MergeRequestStatus Status { get; private set; } = null!;
    public string? ReviewJobId { get; private set; }

    private MrReview() : base(Guid.NewGuid()) { }

    public static MrReview Create(Guid mergeRequestId, Guid userId)
    {
        var review = new MrReview();
        review.MergeRequestId = mergeRequestId;
        review.UserId = userId;
        review.Status = new MergeRequestStatus.Pending(DateTimeOffset.UtcNow);
        return review;
    }

    public void SetReviewJobId(string jobId) => ReviewJobId = jobId;

    public VoidResult BeginReview(string? headSha, string diff)
    {
        var shaRule = CheckRule(new MergeRequestHasHeadSha(headSha));
        if (shaRule.IsFailure) return shaRule.Error;

        var reviewingRule = CheckRule(new MergeRequestNotAlreadyReviewing(Status));
        if (reviewingRule.IsFailure) return reviewingRule.Error;

        var diffRule = CheckRule(new MergeRequestDiffNotTooLarge(diff));
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
}
