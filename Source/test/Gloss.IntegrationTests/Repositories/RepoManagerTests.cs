using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models.Secrets;
using Gloss.Domain.Configs;
using Gloss.Domain.Repositories;
using Gloss.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using Git = LibGit2Sharp;

namespace Gloss.IntegrationTests.Repositories;

public sealed class RepoManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"gloss-repomgr-{Guid.NewGuid():N}");

    // The reviewer explores the cloned working tree with its tools, so it must reflect the MR head.
    // EnsureReadyAsync took headSha but never checked it out, leaving the tree on the default branch —
    // so anything the MR added looked "missing" and produced false "undefined / won't compile" reviews.
    [Fact]
    public async Task EnsureReady_ChecksOutTheMergeRequestHeadSha_NotTheDefaultBranchTip()
    {
        var (headSha, defaultTipSha) = CreateSourceRepoWithTwoCommits();
        var repoManager = CreateRepoManager();
        var repository = Repository.Create("proj", "gitlab");

        var localPath = await repoManager.EnsureReadyAsync(repository, headSha, CancellationToken.None);

        using var clone = new Git.Repository(localPath);
        clone.Head.Tip.Sha.Should().Be(headSha);
        clone.Head.Tip.Sha.Should().NotBe(defaultTipSha);
    }

    private (string HeadSha, string DefaultTipSha) CreateSourceRepoWithTwoCommits()
    {
        var sourcePath = Path.Combine(_root, "remotes", "proj.git");
        Directory.CreateDirectory(sourcePath);
        Git.Repository.Init(sourcePath);
        using var repo = new Git.Repository(sourcePath);
        var who = new Git.Signature("Test", "test@test.local", DateTimeOffset.UtcNow);

        File.WriteAllText(Path.Combine(sourcePath, "base.txt"), "base");
        Git.Commands.Stage(repo, "base.txt");
        var headCommit = repo.Commit("base", who, who);

        File.WriteAllText(Path.Combine(sourcePath, "later.txt"), "later");
        Git.Commands.Stage(repo, "later.txt");
        var laterCommit = repo.Commit("later", who, who);

        // Review the earlier commit: head (under review) != default branch tip.
        return (headCommit.Sha, laterCommit.Sha);
    }

    private RepoManager CreateRepoManager()
    {
        var config = Config.Create(
            GitProvider.GitLab,
            new Uri(Path.Combine(_root, "remotes")),
            EncryptedSecret.FromCipherText("git-cipher"),
            [],
            LlmProvider.Anthropic,
            EncryptedSecret.FromCipherText("llm-cipher"),
            "claude-sonnet-4-6",
            true, 16000, 10000, "0 */2 * * * ?");

        var configRepository = new Mock<IConfigRepository>();
        configRepository.Setup(r => r.FindAsync(It.IsAny<CancellationToken>())).ReturnsAsync(config);

        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Decrypt(It.IsAny<EncryptedSecret>())).Returns(Secret.Create("token").Value);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RepoBasePath"] = Path.Combine(_root, "clones"),
            })
            .Build();

        return new RepoManager(configRepository.Object, encryptor.Object, configuration);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_root)) return;
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_root, recursive: true);
    }
}
