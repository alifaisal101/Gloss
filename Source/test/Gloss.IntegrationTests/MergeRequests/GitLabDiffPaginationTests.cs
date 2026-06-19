using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models.Secrets;
using Gloss.Domain.Configs;
using Gloss.Infrastructure.MergeRequests;
using Moq;

namespace Gloss.IntegrationTests.MergeRequests;

public sealed class GitLabDiffPaginationTests
{
    private const int TotalFiles = 25;

    // GitLab's /merge_requests/:iid/diffs is paged (default 20/page). GetDiffAsync fetched one page,
    // so the MR diff — and the review context, and the "All changes" UI view — silently capped at 20.
    [Fact]
    public async Task GetOpenMergeRequests_FetchesEveryDiffPage_NotJustTheFirstTwenty()
    {
        var client = CreateClient();

        var mrs = await client.GetOpenMergeRequestsAsync("group/proj", CancellationToken.None);

        var fileCount = Regex.Matches(mrs.Single().Diff, "diff --git").Count;
        fileCount.Should().Be(TotalFiles);
    }

    private static GitLabClient CreateClient()
    {
        var httpClient = new HttpClient(new PaginatingGitLabHandler());

        var config = Config.Create(
            GitProvider.GitLab,
            new Uri("https://gitlab.test"),
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

        return new GitLabClient(httpClient, configRepository.Object, encryptor.Object);
    }

    private sealed class PaginatingGitLabHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var json = uri.AbsolutePath.EndsWith("/diffs", StringComparison.Ordinal)
                ? DiffsPage(QueryInt(uri.Query, "page", 1), QueryInt(uri.Query, "per_page", 20))
                : uri.AbsolutePath.EndsWith("/merge_requests", StringComparison.Ordinal)
                    ? MergeRequestList
                    : "[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }

        private const string MergeRequestList =
            """[{"iid":123,"title":"t","description":null,"source_branch":"feat","target_branch":"main","author":{"username":"a"},"diff_refs":{"base_sha":"b","head_sha":"h","start_sha":"s"},"state":"opened"}]""";

        private static string DiffsPage(int page, int perPage)
        {
            var start = (page - 1) * perPage;
            var count = Math.Max(0, Math.Min(perPage, TotalFiles - start));
            var items = Enumerable.Range(start, count).Select(i =>
                $$"""{"old_path":"file{{i}}.cs","new_path":"file{{i}}.cs","diff":"@@ -1 +1 @@\n-a\n+b","new_file":false,"deleted_file":false}""");
            return "[" + string.Join(",", items) + "]";
        }

        private static int QueryInt(string query, string name, int fallback)
        {
            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0] == name && int.TryParse(kv[1], out var value)) return value;
            }
            return fallback;
        }
    }
}
