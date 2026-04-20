using System.Text.Json;
using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using Gloss.Application.Projection;
using Gloss.Application.Projection.UpdateProjection;
using Gloss.Domain.Projection;
using Moq;

namespace Gloss.UnitTests.Projection;

public sealed class UpdateProjectionHandlerTests
{
    private readonly Mock<IReviewerProjectionRepository> _repo = new();
    private readonly Mock<IEventStore> _eventStore = new();
    private readonly Mock<IProjectionEngine> _engine = new();
    private readonly Mock<IDomainContext> _domainContext = new();

    private UpdateProjectionHandler CreateSut() =>
        new(_repo.Object, _eventStore.Object, _engine.Object, _domainContext.Object);

    [Fact]
    public async Task Handle_WhenNoEvents_DoesNotCallProjectionEngine()
    {
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ReviewerProjection?)null);
        _eventStore.Setup(s => s.QueryAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await CreateSut().HandleAsync(CancellationToken.None);

        _engine.Verify(
            e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoCurrentProjection_QueriesEventsFromPositionZero()
    {
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ReviewerProjection?)null);
        _eventStore.Setup(s => s.QueryAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([MakeEvent(1)]);
        _engine.Setup(e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>())).ReturnsAsync("projection");

        await CreateSut().HandleAsync(CancellationToken.None);

        _eventStore.Verify(
            s => s.QueryAsync(It.Is<EventQuery>(q => q.FromGlobalPosition == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurrentProjectionExists_QueriesFromNextGlobalPosition()
    {
        var current = ReviewerProjection.Seed("existing", lastProcessedGlobalPosition: 42);
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);
        _eventStore.Setup(s => s.QueryAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([MakeEvent(43)]);
        _engine.Setup(e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>())).ReturnsAsync("updated");

        await CreateSut().HandleAsync(CancellationToken.None);

        _eventStore.Verify(
            s => s.QueryAsync(It.Is<EventQuery>(q => q.FromGlobalPosition == 43), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CallsProjectionEngineWithCurrentContentAndNewEvents()
    {
        var current = ReviewerProjection.Seed("my existing projection", lastProcessedGlobalPosition: 5);
        var newEvent = MakeEvent(6);
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);
        _eventStore.Setup(s => s.QueryAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([newEvent]);
        _engine.Setup(e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>())).ReturnsAsync("updated");

        await CreateSut().HandleAsync(CancellationToken.None);

        _engine.Verify(
            e => e.BuildUpdatedProjectionAsync(
                "my existing projection",
                It.Is<IReadOnlyList<StoredEvent>>(events => events.Count == 1 && events[0].GlobalPosition == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CommitsAfterUpdatingProjection()
    {
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ReviewerProjection?)null);
        _eventStore.Setup(s => s.QueryAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([MakeEvent(1)]);
        _engine.Setup(e => e.BuildUpdatedProjectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<StoredEvent>>(), It.IsAny<CancellationToken>())).ReturnsAsync("new projection");

        await CreateSut().HandleAsync(CancellationToken.None);

        _domainContext.Verify(c => c.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static StoredEvent MakeEvent(long globalPosition) => new()
    {
        Id = Guid.NewGuid(),
        StreamId = "mr-test",
        EventType = "CommentAccepted",
        Position = 0,
        GlobalPosition = globalPosition,
        Payload = JsonDocument.Parse("{}"),
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
