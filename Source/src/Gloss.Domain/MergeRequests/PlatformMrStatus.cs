using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Models;

#pragma warning disable CA1034

namespace Gloss.Domain.MergeRequests;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Open), "Open")]
[JsonDerivedType(typeof(Closed), "Closed")]
[JsonDerivedType(typeof(Merged), "Merged")]
public abstract class PlatformMrStatus : ValueObject
{
    public sealed class Open : PlatformMrStatus
    {
        [JsonConstructor]
        public Open() { }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return true;
        }
    }

    [method: JsonConstructor]
    public sealed class Closed(DateTimeOffset occurredAt, string byUsername) : PlatformMrStatus
    {
        public DateTimeOffset OccurredAt { get; } = occurredAt;
        public string ByUsername { get; } = byUsername;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return OccurredAt;
            yield return ByUsername;
        }
    }

    [method: JsonConstructor]
    public sealed class Merged(DateTimeOffset occurredAt, string byUsername) : PlatformMrStatus
    {
        public DateTimeOffset OccurredAt { get; } = occurredAt;
        public string ByUsername { get; } = byUsername;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return OccurredAt;
            yield return ByUsername;
        }
    }
}
