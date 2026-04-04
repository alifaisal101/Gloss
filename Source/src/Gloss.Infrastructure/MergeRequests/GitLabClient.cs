using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Abstractions;
using Gloss.Application.MergeRequests;
using Gloss.Domain.Configs;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class GitLabClient(
    HttpClient httpClient,
    IConfigRepository configRepository,
    ISecretEncryptor encryptor) : IGitClient
{
    public async Task<IReadOnlyList<MergeRequestData>> GetOpenMergeRequestsAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return [];

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var mrs = await SendAsync<GitLabMrDto[]>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests?state=opened",
            token, cancellationToken).ConfigureAwait(false);

        if (mrs is null) return [];

        var results = new List<MergeRequestData>(mrs.Length);
        foreach (var mr in mrs)
        {
            var diff = await GetDiffAsync(baseUrl, encoded, mr.Iid, token, cancellationToken).ConfigureAwait(false);
            results.Add(new(mr.Iid, mr.Title, mr.Description, mr.SourceBranch, mr.TargetBranch, mr.Author.Username, diff, mr.DiffRefs.BaseSha, mr.DiffRefs.HeadSha, mr.DiffRefs.StartSha));
        }

        return results;
    }

    private async Task<string> GetDiffAsync(string baseUrl, string encodedPath, int iid, string token, CancellationToken cancellationToken)
    {
        var diffs = await SendAsync<GitLabDiffDto[]>(
            $"{baseUrl}/api/v4/projects/{encodedPath}/merge_requests/{iid}/diffs",
            token, cancellationToken).ConfigureAwait(false);

        if (diffs is null || diffs.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var d in diffs)
        {
            sb.Append("diff --git a/").Append(d.OldPath).Append(" b/").AppendLine(d.NewPath);
            if (d.NewFile)
                sb.AppendLine("new file mode 100644");
            else if (d.DeletedFile)
                sb.AppendLine("deleted file mode 100644");
            sb.Append("--- ").AppendLine(d.NewFile ? "/dev/null" : $"a/{d.OldPath}");
            sb.Append("+++ ").AppendLine(d.DeletedFile ? "/dev/null" : $"b/{d.NewPath}");
            sb.AppendLine(d.Diff.TrimEnd());
        }
        return sb.ToString();
    }

    private async Task<T?> SendAsync<T>(string url, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("PRIVATE-TOKEN", token);
        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishCommentAsync(
        string projectPath,
        int mrIid,
        string baseSha,
        string headSha,
        string startSha,
        string filePath,
        int line,
        string body,
        CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return;

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var payload = new
        {
            body,
            position = new
            {
                position_type = "text",
                base_sha = baseSha,
                head_sha = headSha,
                start_sha = startSha,
                new_path = filePath,
                new_line = line,
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}/discussions");
        request.Headers.Add("PRIVATE-TOKEN", token);
        request.Content = JsonContent.Create(payload);
        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private sealed record GitLabMrDto(
        int Iid,
        string Title,
        string? Description,
        [property: JsonPropertyName("source_branch")] string SourceBranch,
        [property: JsonPropertyName("target_branch")] string TargetBranch,
        GitLabAuthorDto Author,
        [property: JsonPropertyName("diff_refs")] GitLabDiffRefsDto DiffRefs);

    private sealed record GitLabAuthorDto(string Username);

    private sealed record GitLabDiffRefsDto(
        [property: JsonPropertyName("base_sha")] string BaseSha,
        [property: JsonPropertyName("head_sha")] string HeadSha,
        [property: JsonPropertyName("start_sha")] string StartSha);

    private sealed record GitLabDiffDto(
        [property: JsonPropertyName("old_path")] string OldPath,
        [property: JsonPropertyName("new_path")] string NewPath,
        string Diff,
        [property: JsonPropertyName("new_file")] bool NewFile,
        [property: JsonPropertyName("deleted_file")] bool DeletedFile);
}
