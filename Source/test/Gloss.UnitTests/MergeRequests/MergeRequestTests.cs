using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class MergeRequestTests
{
    [Fact]
    public void Create_StartsInPendingState()
    {
        var mr = BuildMr();

        mr.Status.Should().BeOfType<MergeRequestStatus.Pending>();
    }

    [Fact]
    public void Create_SetsAllProperties()
    {
        var repoId = Guid.NewGuid();

        var mr = MergeRequest.Create(repoId, 42, "Fix bug", "A description",
            "fix/bug", "main", "alice", "diff content", "base", "head", "start");

        mr.RepositoryId.Should().Be(repoId);
        mr.ProviderIid.Should().Be(42);
        mr.Title.Should().Be("Fix bug");
        mr.Description.Should().Be("A description");
        mr.SourceBranch.Should().Be("fix/bug");
        mr.TargetBranch.Should().Be("main");
        mr.AuthorUsername.Should().Be("alice");
        mr.Diff.Should().Be("diff content");
        mr.BaseSha.Should().Be("base");
        mr.HeadSha.Should().Be("head");
        mr.StartSha.Should().Be("start");
    }

    [Fact]
    public void Create_WithNullOptionalFields_SetsThemToNull()
    {
        var mr = MergeRequest.Create(Guid.NewGuid(), 1, "Title", null,
            "src", "main", "alice", "diff", null, null, null);

        mr.Description.Should().BeNull();
        mr.BaseSha.Should().BeNull();
        mr.HeadSha.Should().BeNull();
        mr.StartSha.Should().BeNull();
    }

    [Fact]
    public void Create_AssignsUniqueId()
    {
        var mr1 = BuildMr();
        var mr2 = BuildMr();

        mr1.Id.Should().NotBe(mr2.Id);
    }

    [Fact]
    public void BeginReview_TransitionsToReviewingState()
    {
        var mr = BuildMr();

        mr.BeginReview();

        mr.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void CompleteReview_TransitionsToReadyState()
    {
        var mr = BuildMr();
        mr.BeginReview();

        mr.CompleteReview();

        mr.Status.Should().BeOfType<MergeRequestStatus.Ready>();
    }

    [Fact]
    public void Publish_TransitionsToPublishedState()
    {
        var mr = BuildMr();
        mr.BeginReview();
        mr.CompleteReview();

        mr.Publish();

        mr.Status.Should().BeOfType<MergeRequestStatus.Published>();
    }

    [Fact]
    public void ResetToPending_FromReviewing_ReturnsToPendingState()
    {
        var mr = BuildMr();
        mr.BeginReview();

        mr.ResetToPending();

        mr.Status.Should().BeOfType<MergeRequestStatus.Pending>();
    }

    [Fact]
    public void ResetToPending_FromReady_ReturnsToPendingState()
    {
        var mr = BuildMr();
        mr.BeginReview();
        mr.CompleteReview();

        mr.ResetToPending();

        mr.Status.Should().BeOfType<MergeRequestStatus.Pending>();
    }

    [Fact]
    public void FullReviewCycle_TransitionsCorrectly()
    {
        var mr = BuildMr();

        mr.Status.Should().BeOfType<MergeRequestStatus.Pending>();
        mr.BeginReview();
        mr.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
        mr.CompleteReview();
        mr.Status.Should().BeOfType<MergeRequestStatus.Ready>();
        mr.Publish();
        mr.Status.Should().BeOfType<MergeRequestStatus.Published>();
    }

    [Fact]
    public void Update_ReplacesAllMutableFields()
    {
        var mr = BuildMr();

        mr.Update("New title", "New desc", "feature/new", "develop",
            "bob", "new diff", "base2", "head2", "start2");

        mr.Title.Should().Be("New title");
        mr.Description.Should().Be("New desc");
        mr.SourceBranch.Should().Be("feature/new");
        mr.TargetBranch.Should().Be("develop");
        mr.AuthorUsername.Should().Be("bob");
        mr.Diff.Should().Be("new diff");
        mr.BaseSha.Should().Be("base2");
        mr.HeadSha.Should().Be("head2");
        mr.StartSha.Should().Be("start2");
    }

    [Fact]
    public void Update_DoesNotChangeRepositoryIdOrState()
    {
        var repoId = Guid.NewGuid();
        var mr = MergeRequest.Create(repoId, 1, "Title", null, "src", "main", "alice", "diff", null, "head1", null);
        mr.BeginReview();

        mr.Update("New title", null, "src", "main", "alice", "diff", null, null, null);

        mr.RepositoryId.Should().Be(repoId);
        mr.Status.Should().BeOfType<MergeRequestStatus.Reviewing>();
    }

    [Fact]
    public void Update_CanClearOptionalFields()
    {
        var mr = MergeRequest.Create(Guid.NewGuid(), 1, "Title", "desc", "src", "main", "alice", "diff", "b", "h", "s");

        mr.Update("Title", null, "src", "main", "alice", "diff", null, null, null);

        mr.Description.Should().BeNull();
        mr.BaseSha.Should().BeNull();
        mr.HeadSha.Should().BeNull();
        mr.StartSha.Should().BeNull();
    }

    [Fact]
    public void HasShas_WhenAllPresent_IsTrue()
    {
        var mr = MergeRequest.Create(Guid.NewGuid(), 1, "T", null, "s", "m", "a", "d", "base", "head", "start");

        (mr.BaseSha is not null && mr.HeadSha is not null && mr.StartSha is not null).Should().BeTrue();
    }

    [Fact]
    public void HasShas_WhenAnyMissing_IsFalse()
    {
        var mr = MergeRequest.Create(Guid.NewGuid(), 1, "T", null, "s", "m", "a", "d", null, "head", "start");

        (mr.BaseSha is not null && mr.HeadSha is not null && mr.StartSha is not null).Should().BeFalse();
    }

    private static MergeRequest BuildMr() =>
        MergeRequest.Create(Guid.NewGuid(), 1, "Fix bug", null,
            "fix/bug", "main", "alice", "diff --git a/Foo.cs", "base", "head", "start");
}
