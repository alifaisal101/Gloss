using Gloss.Domain.Repositories;

namespace Gloss.UnitTests.Repositories;

public sealed class RepositoryTests
{
    [Fact]
    public void Create_SetsProjectPathAndProvider()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.ProjectPath.Should().Be("group/project-a");
        repo.Provider.Should().Be("gitlab");
    }

    [Fact]
    public void Create_AutoReviewEnabled_DefaultsToTrue()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.AutoReviewEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_PollCron_DefaultsToNull()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.PollCron.Should().BeNull();
    }

    [Fact]
    public void Create_LocalClonePath_DefaultsToNull()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.LocalClonePath.Should().BeNull();
    }

    [Fact]
    public void Create_AssignsUniqueId()
    {
        var r1 = Repository.Create("group/project-a", "gitlab");
        var r2 = Repository.Create("group/project-b", "gitlab");

        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void SetCloned_SetsLocalClonePath()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.SetCloned("/repos/group/project-a");

        repo.LocalClonePath.Should().Be("/repos/group/project-a");
    }

    [Fact]
    public void SetCloned_CanBeCalledMultipleTimes_UpdatesPath()
    {
        var repo = Repository.Create("group/project-a", "gitlab");
        repo.SetCloned("/repos/first");

        repo.SetCloned("/repos/second");

        repo.LocalClonePath.Should().Be("/repos/second");
    }

    [Fact]
    public void SetPollCron_UpdatesCron()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.SetPollCron("0 */2 * * * ?");

        repo.PollCron.Should().Be("0 */2 * * * ?");
    }

    [Fact]
    public void SetAutoReviewEnabled_Disable_SetsToFalse()
    {
        var repo = Repository.Create("group/project-a", "gitlab");

        repo.SetAutoReviewEnabled(false);

        repo.AutoReviewEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetAutoReviewEnabled_Enable_SetsToTrue()
    {
        var repo = Repository.Create("group/project-a", "gitlab");
        repo.SetAutoReviewEnabled(false);

        repo.SetAutoReviewEnabled(true);

        repo.AutoReviewEnabled.Should().BeTrue();
    }
}
