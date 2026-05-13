#pragma warning disable CA1034

using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.MergeRequests;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NotApproved), "NotApproved")]
[JsonDerivedType(typeof(Approved), "Approved")]
public abstract class ApprovalStatus : ValueObject
{
    public sealed class NotApproved : ApprovalStatus
    {
        [JsonConstructor]
        public NotApproved() { }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return true;
        }
    }

    [method: JsonConstructor]
    public sealed class Approved(string? byUsername, DateTimeOffset? approvedAt) : ApprovalStatus
    {
        public string? ByUsername { get; } = byUsername;
        public DateTimeOffset? ApprovedAt { get; } = approvedAt;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ByUsername ?? string.Empty;
            yield return ApprovedAt ?? DateTimeOffset.MinValue;
        }
    }
}
