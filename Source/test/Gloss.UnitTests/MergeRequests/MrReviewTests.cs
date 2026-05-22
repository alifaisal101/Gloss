using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class MrReviewTests
{
    [Fact]
    public void Create_StartsInPendingState()
    {
        var review = MrReview.Create(Guid.NewGuid(), Guid.Empty);
        review.Status.Should().BeOfType<MergeRequestStatus.Pending>();
    }

    [Fact]
    public void Create_SetsIds()
    {
        var mrId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var review = MrReview.Create(mrId, userId);
        review.MergeRequestId.Should().Be(mrId);
        review.UserId.Should().Be(userId);
    }

    [Fact]
    public void BeginReview_TransitionsToReviewing()
    {
        var review = BuildReview();
        review.BeginReview("head", "diff");
        review.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void CompleteReview_TransitionsToReady()
    {
        var review = BuildReview();
        review.BeginReview("head", "diff");
        review.CompleteReview();
        review.Status.Should().BeOfType<MergeRequestStatus.Ready>();
    }

    [Fact]
    public void MarkSeen_WhenReady_TransitionsToSeen()
    {
        var review = BuildReview();
        review.BeginReview("head", "diff");
        review.CompleteReview();
        review.MarkSeen();
        review.Status.Should().BeOfType<MergeRequestStatus.Seen>();
    }

    [Fact]
    public void MarkSeen_WhenPending_DoesNotTransition()
    {
        var review = BuildReview();
        review.MarkSeen();
        review.Status.Should().BeOfType<MergeRequestStatus.Pending>();
    }

    [Fact]
    public void MarkStaged_WhenReady_TransitionsToStaged()
    {
        var review = BuildReview();
        review.BeginReview("head", "diff");
        review.CompleteReview();
        review.MarkStaged();
        review.Status.Should().BeOfType<MergeRequestStatus.Staged>();
    }

    [Fact]
    public void MarkStaged_WhenPublished_DoesNotRegress()
    {
        var review = BuildReview();
        review.BeginReview("head", "diff");
        review.CompleteReview();
        review.Publish();
        review.MarkStaged();
        review.Status.Should().BeOfType<MergeRequestStatus.Published>();
    }

    [Fact]
    public void ResetToPending_ReturnsToPending()
    {
        var review = BuildReview();
        review.BeginReview("head", "diff");
        review.ResetToPending();
        review.Status.Should().BeOfType<MergeRequestStatus.Pending>();
    }

    [Fact]
    public void FullCycle_TransitionsCorrectly()
    {
        var review = BuildReview();
        review.Status.Should().BeOfType<MergeRequestStatus.Pending>();
        review.BeginReview("head", "diff");
        review.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
        review.CompleteReview();
        review.Status.Should().BeOfType<MergeRequestStatus.Ready>();
        review.Publish();
        review.Status.Should().BeOfType<MergeRequestStatus.Published>();
    }

    private static MrReview BuildReview() => MrReview.Create(Guid.NewGuid(), Guid.Empty);
}
