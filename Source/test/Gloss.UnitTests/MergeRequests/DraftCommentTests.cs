using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class DraftCommentTests
{
    [Fact]
    public void Create_DefaultState_IsGenerated()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 10, "Null check missing", null).Value;

        comment.State.Should().Be(DraftCommentState.Generated);
    }

    [Fact]
    public void Create_WithUserAddedState_SetsUserAddedState()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 10, "My comment", null, DraftCommentState.UserAdded).Value;

        comment.State.Should().Be(DraftCommentState.UserAdded);
    }

    [Fact]
    public void Create_SetsAllProperties()
    {
        var mrId = Guid.NewGuid();

        var comment = DraftComment.Create(mrId, "src/Foo.cs", 42, "Null check missing", "Can throw here").Value;

        comment.MrReviewId.Should().Be(mrId);
        comment.FilePath.Should().Be("src/Foo.cs");
        comment.Line.Should().Be(42);
        comment.Body.Should().Be("Null check missing");
        comment.Reasoning.Should().Be("Can throw here");
    }

    [Fact]
    public void Create_WithNullReasoning_SetsReasoningToNull()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null).Value;

        comment.Reasoning.Should().BeNull();
    }

    [Fact]
    public void Create_AssignsUniqueId()
    {
        var mrId = Guid.NewGuid();

        var c1 = DraftComment.Create(mrId, "src/Foo.cs", 1, "body", null).Value;
        var c2 = DraftComment.Create(mrId, "src/Foo.cs", 1, "body", null).Value;

        c1.Id.Should().NotBe(c2.Id);
    }

    [Fact]
    public void Update_SetsEditedState()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 10, "Original", null).Value;

        comment.Update("src/Foo.cs", 10, "Updated", null);

        comment.State.Should().Be(DraftCommentState.Edited);
    }

    [Fact]
    public void Update_ReplacesAllFields()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Old.cs", 5, "Old body", "Old reasoning").Value;

        comment.Update("src/New.cs", 99, "New body", "New reasoning");

        comment.FilePath.Should().Be("src/New.cs");
        comment.Line.Should().Be(99);
        comment.Body.Should().Be("New body");
        comment.Reasoning.Should().Be("New reasoning");
    }

    [Fact]
    public void Update_CanClearReasoning()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", "reasoning").Value;

        comment.Update("src/Foo.cs", 1, "body", null);

        comment.Reasoning.Should().BeNull();
    }

    [Fact]
    public void Update_DoesNotChangeMrReviewId()
    {
        var mrId = Guid.NewGuid();
        var comment = DraftComment.Create(mrId, "src/Foo.cs", 1, "body", null).Value;

        comment.Update("src/Bar.cs", 2, "new body", null);

        comment.MrReviewId.Should().Be(mrId);
    }

    [Fact]
    public void Update_WhenAlreadyEdited_RemainsEdited()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null).Value;
        comment.Update("src/Foo.cs", 1, "edit 1", null);

        comment.Update("src/Foo.cs", 1, "edit 2", null);

        comment.State.Should().Be(DraftCommentState.Edited);
    }

    [Fact]
    public void Update_WhenUserAdded_BecomesEdited()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null, DraftCommentState.UserAdded).Value;

        comment.Update("src/Foo.cs", 1, "updated", null);

        comment.State.Should().Be(DraftCommentState.Edited);
    }
}
