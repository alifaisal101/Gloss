using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.Projection;

public sealed class ReviewerProjection : AggregateRoot<Guid>
{
    public string Content { get; private set; } = null!;
    public int Version { get; private set; }
    public long LastProcessedGlobalPosition { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ReviewerProjection() : base(Guid.NewGuid()) { }

    public static ReviewerProjection Seed(string content, long lastProcessedGlobalPosition)
    {
        var p = new ReviewerProjection();
        p.Content = content;
        p.Version = 1;
        p.LastProcessedGlobalPosition = lastProcessedGlobalPosition;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        return p;
    }

    public ReviewerProjection NextVersion(string content, long lastProcessedGlobalPosition)
    {
        var p = new ReviewerProjection();
        p.Content = content;
        p.Version = Version + 1;
        p.LastProcessedGlobalPosition = lastProcessedGlobalPosition;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        return p;
    }
}
