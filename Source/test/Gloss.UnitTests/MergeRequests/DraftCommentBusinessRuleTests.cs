using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.UnitTests.MergeRequests;

public sealed class DraftCommentBusinessRuleTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        Result<DraftComment> result = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 10, "Null check missing", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Body.Should().Be("Null check missing");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyBody_ReturnsCommentBodyRequiredError(string body)
    {
        Result<DraftComment> result = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, body, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentBodyRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyFilePath_ReturnsCommentFilePathRequiredError(string filePath)
    {
        Result<DraftComment> result = DraftComment.Create(Guid.NewGuid(), filePath, 1, "body", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentFilePathRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidLineNumber_ReturnsCommentLineInvalidError(int line)
    {
        Result<DraftComment> result = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", line, "body", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentLineInvalid);
    }

    [Fact]
    public void Create_WithLineNumberOne_Succeeds()
    {
        Result<DraftComment> result = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_BodyValidatedBeforeFilePath()
    {
        Result<DraftComment> result = DraftComment.Create(Guid.NewGuid(), "", 1, "", null);

        result.Error.Should().Be(MergeRequestErrors.CommentBodyRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyBody_ReturnsCommentBodyRequiredError(string body)
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "original", null).Value;

        VoidResult result = comment.Update("src/Foo.cs", 1, body, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentBodyRequired);
    }

    [Fact]
    public void Update_WithEmptyBody_DoesNotChangeState()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "original", null).Value;

        comment.Update("src/Foo.cs", 1, "", null);

        comment.Body.Should().Be("original");
        comment.State.Should().Be(DraftCommentState.Generated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyFilePath_ReturnsCommentFilePathRequiredError(string filePath)
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null).Value;

        VoidResult result = comment.Update(filePath, 1, "body", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentFilePathRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WithInvalidLineNumber_ReturnsCommentLineInvalidError(int line)
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null).Value;

        VoidResult result = comment.Update("src/Foo.cs", line, "body", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentLineInvalid);
    }

    [Fact]
    public void Update_WithValidData_Succeeds()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "original", null).Value;

        VoidResult result = comment.Update("src/Bar.cs", 5, "updated", "reasoning");

        result.IsSuccess.Should().BeTrue();
        comment.State.Should().Be(DraftCommentState.Edited);
    }
}
