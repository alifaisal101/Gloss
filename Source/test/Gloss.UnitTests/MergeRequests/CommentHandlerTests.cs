using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using Gloss.Application.MergeRequests.AddDraftComment;
using Gloss.Application.MergeRequests.DeleteDraftComment;
using Gloss.Application.MergeRequests.EditDraftComment;
using Gloss.Domain.MergeRequests;
using Moq;

namespace Gloss.UnitTests.MergeRequests;

public sealed class CommentHandlerTests
{
    private readonly Mock<IMergeRequestRepository> _mrRepo = new();
    private readonly Mock<IDraftCommentRepository> _commentRepo = new();
    private readonly Mock<IDomainContext> _domainContext = new();
    private readonly Mock<IEventStore> _eventStore = new();

    [Fact]
    public async Task AddComment_CallsGetByIdOnMrRepository()
    {
        var mrId = Guid.NewGuid();
        _mrRepo.Setup(r => r.GetByIdAsync(mrId, It.IsAny<CancellationToken>())).ReturnsAsync((MergeRequest?)null);
        var handler = new AddDraftCommentHandler(_mrRepo.Object, _domainContext.Object, _eventStore.Object);

        await handler.HandleAsync(mrId, "src/Foo.cs", 1, "body", null, CancellationToken.None);

        _mrRepo.Verify(r => r.GetByIdAsync(mrId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddComment_WhenMrNotFound_ReturnsNotFound()
    {
        _mrRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MergeRequest?)null);
        var handler = new AddDraftCommentHandler(_mrRepo.Object, _domainContext.Object, _eventStore.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), "src/Foo.cs", 1, "body", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.NotFound);
    }

    [Fact]
    public async Task EditComment_CallsGetByIdOnCommentRepository()
    {
        var commentId = Guid.NewGuid();
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>())).ReturnsAsync((DraftComment?)null);
        var handler = new EditDraftCommentHandler(_commentRepo.Object, _domainContext.Object, _eventStore.Object);

        await handler.HandleAsync(Guid.NewGuid(), commentId, "src/Foo.cs", 1, "body", null, CancellationToken.None);

        _commentRepo.Verify(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditComment_WhenCommentNotFound_ReturnsCommentNotFound()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DraftComment?)null);
        var handler = new EditDraftCommentHandler(_commentRepo.Object, _domainContext.Object, _eventStore.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "src/Foo.cs", 1, "body", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentNotFound);
    }

    [Fact]
    public async Task EditComment_WhenMrIdDoesNotMatchComment_ReturnsCommentNotFound()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null).Value;
        _commentRepo.Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(comment);
        var handler = new EditDraftCommentHandler(_commentRepo.Object, _domainContext.Object, _eventStore.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), comment.Id, "src/Foo.cs", 1, "updated", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentNotFound);
    }

    [Fact]
    public async Task DeleteComment_CallsGetByIdOnCommentRepository()
    {
        var commentId = Guid.NewGuid();
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>())).ReturnsAsync((DraftComment?)null);
        var handler = new DeleteDraftCommentHandler(_commentRepo.Object, _domainContext.Object, _eventStore.Object);

        await handler.HandleAsync(Guid.NewGuid(), commentId, CancellationToken.None);

        _commentRepo.Verify(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_WhenCommentNotFound_ReturnsCommentNotFound()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DraftComment?)null);
        var handler = new DeleteDraftCommentHandler(_commentRepo.Object, _domainContext.Object, _eventStore.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentNotFound);
    }

    [Fact]
    public async Task DeleteComment_WhenMrIdDoesNotMatchComment_ReturnsCommentNotFound()
    {
        var comment = DraftComment.Create(Guid.NewGuid(), "src/Foo.cs", 1, "body", null).Value;
        _commentRepo.Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(comment);
        var handler = new DeleteDraftCommentHandler(_commentRepo.Object, _domainContext.Object, _eventStore.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), comment.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MergeRequestErrors.CommentNotFound);
    }
}
