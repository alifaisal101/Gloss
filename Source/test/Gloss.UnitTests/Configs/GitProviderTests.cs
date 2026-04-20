using Gloss.Domain.Configs;

namespace Gloss.UnitTests.Configs;

public sealed class GitProviderTests
{
    [Theory]
    [InlineData("gitlab")]
    [InlineData("GITLAB")]
    [InlineData("GitLab")]
    public void Create_GitLab_Succeeds(string input)
    {
        var result = GitProvider.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(GitProvider.GitLab);
    }

    [Theory]
    [InlineData("github")]
    [InlineData("GITHUB")]
    [InlineData("GitHub")]
    public void Create_GitHub_Succeeds(string input)
    {
        var result = GitProvider.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(GitProvider.GitHub);
    }

    [Theory]
    [InlineData("bitbucket")]
    [InlineData("azure")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_UnknownProvider_Fails(string input)
    {
        var result = GitProvider.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConfigErrors.InvalidGitProvider);
    }

    [Fact]
    public void Create_Null_Fails()
    {
        var result = GitProvider.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConfigErrors.InvalidGitProvider);
    }

    [Fact]
    public void GitLab_Value_IsLowercase()
    {
        GitProvider.GitLab.Value.Should().Be("gitlab");
    }

    [Fact]
    public void GitHub_Value_IsLowercase()
    {
        GitProvider.GitHub.Value.Should().Be("github");
    }

    [Fact]
    public void GitLab_Equals_GitLab()
    {
        GitProvider.GitLab.Should().Be(GitProvider.GitLab);
    }

    [Fact]
    public void GitLab_DoesNotEqual_GitHub()
    {
        GitProvider.GitLab.Should().NotBe(GitProvider.GitHub);
    }

    [Fact]
    public void Create_GitLab_ReturnsSingletonInstance()
    {
        var r1 = GitProvider.Create("gitlab");
        var r2 = GitProvider.Create("GITLAB");

        r1.Value.Should().Be(r2.Value);
    }
}
