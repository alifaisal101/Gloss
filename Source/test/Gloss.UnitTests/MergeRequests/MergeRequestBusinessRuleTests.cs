using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class MergeRequestBusinessRuleTests
{
    [Fact]
    public void MarkReviewing_WhenPendingWithValidDiffAndHeadSha_Succeeds()
    {
        var mr = BuildMr(headSha: "abc123", diff: "small diff");

        VoidResult result = mr.MarkReviewing();

        result.IsSuccess.Should().BeTrue();
        mr.State.Should().Be(MergeRequestState.Reviewing);
    }

    [Fact]
    public void MarkReviewing_WhenAlreadyReviewing_ReturnsAlreadyReviewingError()
    {
        var mr = BuildMr(headSha: "abc123", diff: "small diff");
        mr.MarkReviewing();

        VoidResult result = mr.MarkReviewing();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.AlreadyReviewing);
    }

    [Fact]
    public void MarkReviewing_WhenAlreadyReviewing_DoesNotChangeState()
    {
        var mr = BuildMr(headSha: "abc123", diff: "small diff");
        mr.MarkReviewing();

        mr.MarkReviewing();

        mr.State.Should().Be(MergeRequestState.Reviewing);
    }

    [Fact]
    public void MarkReviewing_WhenDiffExceedsLimit_ReturnsDiffTooLargeError()
    {
        var mr = BuildMr(headSha: "abc123", diff: new string('+', 50_001));

        VoidResult result = mr.MarkReviewing();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.DiffTooLarge);
    }

    [Fact]
    public void MarkReviewing_WhenDiffIsExactlyAtLimit_Succeeds()
    {
        var mr = BuildMr(headSha: "abc123", diff: new string('+', 50_000));

        VoidResult result = mr.MarkReviewing();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MarkReviewing_WhenHeadShaIsMissing_ReturnsMissingShasError()
    {
        var mr = BuildMr(headSha: null, diff: "small diff");

        VoidResult result = mr.MarkReviewing();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.MissingShas);
    }

    [Fact]
    public void MarkReviewing_WhenDiffTooLargeAndHeadShaMissing_ReturnsMissingShasFirst()
    {
        var mr = BuildMr(headSha: null, diff: new string('+', 50_001));

        VoidResult result = mr.MarkReviewing();

        result.Error.Should().Be(MergeRequestErrors.MissingShas);
    }

    // ── MarkPublished ──────────────────────────────────────────────────────────

    [Fact]
    public void MarkPublished_WhenReady_Succeeds()
    {
        var mr = BuildMr(headSha: "abc", diff: "diff");
        mr.MarkReviewing();
        mr.MarkReady();

        VoidResult result = mr.MarkPublished();

        result.IsSuccess.Should().BeTrue();
        mr.State.Should().Be(MergeRequestState.Published);
    }

    [Fact]
    public void MarkPublished_WhenPending_ReturnsNotReadyError()
    {
        var mr = BuildMr(headSha: "abc", diff: "diff");

        VoidResult result = mr.MarkPublished();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotReady);
    }

    [Fact]
    public void MarkPublished_WhenReviewing_ReturnsNotReadyError()
    {
        var mr = BuildMr(headSha: "abc", diff: "diff");
        mr.MarkReviewing();

        VoidResult result = mr.MarkPublished();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotReady);
    }

    private static MergeRequest BuildMr(string? headSha, string diff) =>
        MergeRequest.Create(Guid.NewGuid(), 1, "Fix bug", null,
            "fix/bug", "main", "alice", diff, "base", headSha, "start");
}
