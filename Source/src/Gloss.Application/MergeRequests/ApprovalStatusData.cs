namespace Gloss.Application.MergeRequests;

public sealed record ApprovalStatusData(bool IsApproved, string? ApprovedByUsername, DateTimeOffset? ApprovedAt);
