namespace Gloss.Application.MergeRequests;

public sealed record IgnoredMergeRequestReadModel(
    Guid Id,
    Guid RepositoryId,
    int ProviderIid,
    string Title,
    string ProjectPath,
    DateTimeOffset IgnoredAt);
