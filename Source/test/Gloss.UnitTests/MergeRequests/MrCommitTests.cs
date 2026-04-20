using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class MrCommitTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var mrId = Guid.NewGuid();

        var commit = MrCommit.Create(mrId, "abc1234", "Fix null ref", "Alice", "diff --git a/Foo.cs");

        commit.MergeRequestId.Should().Be(mrId);
        commit.Sha.Should().Be("abc1234");
        commit.Title.Should().Be("Fix null ref");
        commit.AuthorName.Should().Be("Alice");
        commit.Diff.Should().Be("diff --git a/Foo.cs");
    }

    [Fact]
    public void Create_AssignsUniqueId()
    {
        var mrId = Guid.NewGuid();

        var c1 = MrCommit.Create(mrId, "sha1", "Title", "Author", "diff");
        var c2 = MrCommit.Create(mrId, "sha2", "Title", "Author", "diff");

        c1.Id.Should().NotBe(c2.Id);
    }

    [Fact]
    public void Create_MultipleCommitsForSameMr_HaveSameMergeRequestId()
    {
        var mrId = Guid.NewGuid();

        var c1 = MrCommit.Create(mrId, "sha1", "First", "Alice", "diff1");
        var c2 = MrCommit.Create(mrId, "sha2", "Second", "Bob", "diff2");

        c1.MergeRequestId.Should().Be(mrId);
        c2.MergeRequestId.Should().Be(mrId);
    }
}
