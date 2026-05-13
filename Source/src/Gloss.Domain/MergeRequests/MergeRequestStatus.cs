using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Models;

#pragma warning disable CA1034

namespace Gloss.Domain.MergeRequests;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Pending), "Pending")]
[JsonDerivedType(typeof(Reviewing), "Reviewing")]
[JsonDerivedType(typeof(Ready), "Ready")]
[JsonDerivedType(typeof(Seen), "Seen")]
[JsonDerivedType(typeof(Staged), "Staged")]
[JsonDerivedType(typeof(Published), "Published")]
public abstract class MergeRequestStatus : ValueObject
{
    [method: JsonConstructor]
    public sealed class Pending(DateTimeOffset detectedAt) : MergeRequestStatus
    {
        public DateTimeOffset DetectedAt { get; } = detectedAt;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return DetectedAt;
        }
    }

    [method: JsonConstructor]
    public sealed class Reviewing(DateTimeOffset startedAt) : MergeRequestStatus
    {
        public DateTimeOffset StartedAt { get; } = startedAt;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return StartedAt;
        }
    }

    [method: JsonConstructor]
    public sealed class Ready(DateTimeOffset completedAt) : MergeRequestStatus
    {
        public DateTimeOffset CompletedAt { get; } = completedAt;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return CompletedAt;
        }
    }

    [method: JsonConstructor]
    public sealed class Seen(DateTimeOffset seenAt, Guid? byUserId) : MergeRequestStatus
    {
        public DateTimeOffset SeenAt { get; } = seenAt;
        public Guid? ByUserId { get; } = byUserId;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return SeenAt;
            yield return ByUserId.GetValueOrDefault();
        }
    }

    [method: JsonConstructor]
    public sealed class Staged(DateTimeOffset stagedAt, Guid? byUserId) : MergeRequestStatus
    {
        public DateTimeOffset StagedAt { get; } = stagedAt;
        public Guid? ByUserId { get; } = byUserId;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return StagedAt;
            yield return ByUserId.GetValueOrDefault();
        }
    }

    [method: JsonConstructor]
    public sealed class Published(DateTimeOffset publishedAt, Guid? byUserId) : MergeRequestStatus
    {
        public DateTimeOffset PublishedAt { get; } = publishedAt;
        public Guid? ByUserId { get; } = byUserId;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return PublishedAt;
            yield return ByUserId.GetValueOrDefault();
        }
    }
}
