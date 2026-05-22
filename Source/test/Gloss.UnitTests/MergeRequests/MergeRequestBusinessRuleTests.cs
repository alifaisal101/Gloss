using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class MergeRequestBusinessRuleTests
{
    [Fact]
    public void BeginReview_WhenPendingWithValidDiffAndHeadSha_Succeeds()
    {
        var review = BuildReview();
        VoidResult result = review.BeginReview("abc123", "small diff");
        result.IsSuccess.Should().BeTrue();
        review.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void BeginReview_WhenAlreadyReviewing_ReturnsAlreadyReviewingError()
    {
        var review = BuildReview();
        review.BeginReview("abc", "diff");
        VoidResult result = review.BeginReview("abc", "diff");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.AlreadyReviewing);
    }

    [Fact]
    public void BeginReview_WhenAlreadyReviewing_DoesNotChangeState()
    {
        var review = BuildReview();
        review.BeginReview("abc", "diff");
        review.BeginReview("abc", "diff");
        review.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void BeginReview_WhenDiffExceedsLimit_ReturnsDiffTooLargeError()
    {
        var review = BuildReview();
        VoidResult result = review.BeginReview("abc", new string('+', 50_001));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.DiffTooLarge);
    }

    [Fact]
    public void BeginReview_WhenDiffIsExactlyAtLimit_Succeeds()
    {
        var review = BuildReview();
        VoidResult result = review.BeginReview("abc", new string('+', 50_000));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void BeginReview_WhenHeadShaIsMissing_ReturnsMissingShasError()
    {
        var review = BuildReview();
        VoidResult result = review.BeginReview(null, "small diff");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.MissingShas);
    }

    [Fact]
    public void BeginReview_WhenDiffTooLargeAndHeadShaMissing_ReturnsMissingShasFirst()
    {
        var review = BuildReview();
        VoidResult result = review.BeginReview(null, new string('+', 50_001));
        result.Error.Should().Be(MergeRequestErrors.MissingShas);
    }

    [Fact]
    public void Publish_WhenReady_Succeeds()
    {
        var review = BuildReview();
        review.BeginReview("abc", "diff");
        review.CompleteReview();
        VoidResult result = review.Publish();
        result.IsSuccess.Should().BeTrue();
        review.Status.Should().BeOfType<MergeRequestStatus.Published>();
    }

    [Fact]
    public void Publish_WhenPending_ReturnsNotReadyError()
    {
        var review = BuildReview();
        VoidResult result = review.Publish();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotReady);
    }

    [Fact]
    public void Publish_WhenReviewing_ReturnsNotReadyError()
    {
        var review = BuildReview();
        review.BeginReview("abc", "diff");
        VoidResult result = review.Publish();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotReady);
    }

    private static MrReview BuildReview() => MrReview.Create(Guid.NewGuid(), Guid.Empty);
}
