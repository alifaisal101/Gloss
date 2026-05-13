using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class MergeRequestBusinessRuleTests
{
    [Fact]
    public void BeginReview_WhenPendingWithValidDiffAndHeadSha_Succeeds()
    {
        var mr = BuildMr(headSha: "abc123", diff: "small diff");

        VoidResult result = mr.BeginReview();

        result.IsSuccess.Should().BeTrue();
        mr.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void BeginReview_WhenAlreadyReviewing_ReturnsAlreadyReviewingError()
    {
        var mr = BuildMr(headSha: "abc123", diff: "small diff");
        mr.BeginReview();

        VoidResult result = mr.BeginReview();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.AlreadyReviewing);
    }

    [Fact]
    public void BeginReview_WhenAlreadyReviewing_DoesNotChangeState()
    {
        var mr = BuildMr(headSha: "abc123", diff: "small diff");
        mr.BeginReview();

        mr.BeginReview();

        mr.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void BeginReview_WhenDiffExceedsLimit_ReturnsDiffTooLargeError()
    {
        var mr = BuildMr(headSha: "abc123", diff: new string('+', 50_001));

        VoidResult result = mr.BeginReview();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.DiffTooLarge);
    }

    [Fact]
    public void BeginReview_WhenDiffIsExactlyAtLimit_Succeeds()
    {
        var mr = BuildMr(headSha: "abc123", diff: new string('+', 50_000));

        VoidResult result = mr.BeginReview();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void BeginReview_WhenHeadShaIsMissing_ReturnsMissingShasError()
    {
        var mr = BuildMr(headSha: null, diff: "small diff");

        VoidResult result = mr.BeginReview();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.MissingShas);
    }

    [Fact]
    public void BeginReview_WhenDiffTooLargeAndHeadShaMissing_ReturnsMissingShasFirst()
    {
        var mr = BuildMr(headSha: null, diff: new string('+', 50_001));

        VoidResult result = mr.BeginReview();

        result.Error.Should().Be(MergeRequestErrors.MissingShas);
    }

    // ── Publish ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Publish_WhenReady_Succeeds()
    {
        var mr = BuildMr(headSha: "abc", diff: "diff");
        mr.BeginReview();
        mr.CompleteReview();

        VoidResult result = mr.Publish();

        result.IsSuccess.Should().BeTrue();
        mr.Status.Should().BeOfType<MergeRequestStatus.Published>();
    }

    [Fact]
    public void Publish_WhenPending_ReturnsNotReadyError()
    {
        var mr = BuildMr(headSha: "abc", diff: "diff");

        VoidResult result = mr.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotReady);
    }

    [Fact]
    public void Publish_WhenReviewing_ReturnsNotReadyError()
    {
        var mr = BuildMr(headSha: "abc", diff: "diff");
        mr.BeginReview();

        VoidResult result = mr.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotReady);
    }

    private static MergeRequest BuildMr(string? headSha, string diff) =>
        MergeRequest.Create(Guid.NewGuid(), 1, "Fix bug", null,
            "fix/bug", "main", "alice", diff, "base", headSha, "start");
}
