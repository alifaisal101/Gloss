using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Results;

namespace Gloss.Domain.Configs;

public sealed class GitProvider : ValueObject
{
    public static readonly GitProvider GitLab = new("gitlab");
    public static readonly GitProvider GitHub = new("github");

    public string Value { get; }

    private GitProvider(string value) => Value = value;

    public static Result<GitProvider> Create(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "gitlab" => Result.Success(GitLab),
            "github" => Result.Success(GitHub),
            _ => Result.Failure<GitProvider>(ConfigErrors.InvalidGitProvider),
        };

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
