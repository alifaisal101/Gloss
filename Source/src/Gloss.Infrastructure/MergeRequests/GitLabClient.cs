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
            var (baseSha, headSha, startSha) = mr.DiffRefs is not null
                ? (mr.DiffRefs.BaseSha, mr.DiffRefs.HeadSha, mr.DiffRefs.StartSha)
                : await GetVersionShasAsync(baseUrl, encoded, mr.Iid, token, cancellationToken).ConfigureAwait(false);
            results.Add(new(mr.Iid, mr.Title, mr.Description, mr.SourceBranch, mr.TargetBranch, mr.Author?.Username ?? string.Empty, diff, baseSha, headSha, startSha));
        }

        return results;
    }

    public async Task<MrShasData?> GetMrShasAsync(string projectPath, int mrIid, CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return null;

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var mr = await SendAsync<GitLabMrDto>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}",
            token, cancellationToken).ConfigureAwait(false);

        if (mr?.DiffRefs?.BaseSha is not null && mr.DiffRefs.HeadSha is not null && mr.DiffRefs.StartSha is not null)
            return new(mr.DiffRefs.BaseSha, mr.DiffRefs.HeadSha, mr.DiffRefs.StartSha);

        var versions = await SendAsync<GitLabMrVersionDto[]>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}/versions",
            token, cancellationToken).ConfigureAwait(false);

        var latest = versions?.FirstOrDefault();
        if (latest?.BaseCommitSha is not null && latest.HeadCommitSha is not null && latest.StartCommitSha is not null)
            return new(latest.BaseCommitSha, latest.HeadCommitSha, latest.StartCommitSha);

        return null;
    }

    private async Task<(string? BaseSha, string? HeadSha, string? StartSha)> GetVersionShasAsync(
        string baseUrl, string encodedPath, int iid, string token, CancellationToken cancellationToken)
    {
        var versions = await SendAsync<GitLabMrVersionDto[]>(
            $"{baseUrl}/api/v4/projects/{encodedPath}/merge_requests/{iid}/versions",
            token, cancellationToken).ConfigureAwait(false);
        var latest = versions?.FirstOrDefault();
        return (latest?.BaseCommitSha, latest?.HeadCommitSha, latest?.StartCommitSha);
    }

    public async Task<IReadOnlyList<MrCommitData>> GetCommitsAsync(string projectPath, int mrIid, CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return [];

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var commits = await SendAsync<GitLabCommitDto[]>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}/commits",
            token, cancellationToken).ConfigureAwait(false);

        if (commits is null || commits.Length == 0) return [];

        var results = new List<MrCommitData>(commits.Length);
        foreach (var commit in commits)
        {
            var diffs = await SendAsync<GitLabDiffDto[]>(
                $"{baseUrl}/api/v4/projects/{encoded}/repository/commits/{commit.Id}/diff",
                token, cancellationToken).ConfigureAwait(false);
            results.Add(new(commit.Id, commit.Title, commit.AuthorName, BuildDiff(diffs ?? [])));
        }

        return results;
    }

    private async Task<string> GetDiffAsync(string baseUrl, string encodedPath, int iid, string token, CancellationToken cancellationToken)
    {
        var diffs = await SendAsync<GitLabDiffDto[]>(
            $"{baseUrl}/api/v4/projects/{encodedPath}/merge_requests/{iid}/diffs",
            token, cancellationToken).ConfigureAwait(false);
        return BuildDiff(diffs ?? []);
    }

    private static string BuildDiff(GitLabDiffDto[] diffs)
    {
        if (diffs.Length == 0) return string.Empty;
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

    public async Task<PlatformMrStatusData> GetMergeRequestStatusAsync(string projectPath, int mrIid, CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return new("Open", null, null);

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var mr = await SendAsync<GitLabMrDto>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}",
            token, cancellationToken).ConfigureAwait(false);

        if (mr is null) return new("Open", null, null);

        return mr.State switch
        {
            "closed" => new("Closed", mr.ClosedAt, mr.ClosedBy?.Username),
            "merged" => new("Merged", mr.MergedAt, mr.MergedBy?.Username),
            _ => new("Open", null, null)
        };
    }

    public async Task<bool> IsMergeRequestApprovedAsync(string projectPath, int mrIid, CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return false;

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var approvals = await SendAsync<GitLabApprovalDto>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}/approvals",
            token, cancellationToken).ConfigureAwait(false);

        return approvals?.Approved ?? false;
    }

    public async Task<IReadOnlyList<PlatformCommentData>> GetMrDiscussionsAsync(string projectPath, int mrIid, CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return [];

        var token = encryptor.Decrypt(config.GitToken).Value;
        var encoded = Uri.EscapeDataString(projectPath);
        var baseUrl = config.GitBaseUrl.AbsoluteUri.TrimEnd('/');

        var notes = await SendAsync<GitLabNoteDto[]>(
            $"{baseUrl}/api/v4/projects/{encoded}/merge_requests/{mrIid}/notes?sort=asc",
            token, cancellationToken).ConfigureAwait(false);

        if (notes is null) return [];

        return notes
            .Where(n => !n.System)
            .Select(n => new PlatformCommentData(
                n.Author.Username,
                n.Body,
                n.Position?.NewPath,
                n.Position?.NewLine,
                n.CreatedAt))
            .ToList();
    }

    public async Task PublishCommentAsync(
        string projectPath,
        int mrIid,
        string? baseSha,
        string? headSha,
        string? startSha,
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

        object payload = baseSha is not null && headSha is not null && startSha is not null
            ? new
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
            }
            : new { body };

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
        GitLabAuthorDto? Author,
        [property: JsonPropertyName("diff_refs")] GitLabDiffRefsDto? DiffRefs,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("closed_at")] DateTimeOffset? ClosedAt,
        [property: JsonPropertyName("merged_at")] DateTimeOffset? MergedAt,
        [property: JsonPropertyName("closed_by")] GitLabUserDto? ClosedBy,
        [property: JsonPropertyName("merged_by")] GitLabUserDto? MergedBy);

    private sealed record GitLabAuthorDto(string Username);

    private sealed record GitLabUserDto(string Username);

    private sealed record GitLabApprovalDto([property: JsonPropertyName("approved")] bool Approved);

    private sealed record GitLabNoteDto(
        int Id,
        string Body,
        [property: JsonPropertyName("author")] GitLabAuthorDto Author,
        [property: JsonPropertyName("system")] bool System,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("position")] GitLabNotePositionDto? Position);

    private sealed record GitLabNotePositionDto(
        [property: JsonPropertyName("new_path")] string? NewPath,
        [property: JsonPropertyName("new_line")] int? NewLine);

    private sealed record GitLabDiffRefsDto(
        [property: JsonPropertyName("base_sha")] string BaseSha,
        [property: JsonPropertyName("head_sha")] string HeadSha,
        [property: JsonPropertyName("start_sha")] string StartSha);

    private sealed record GitLabMrVersionDto(
        [property: JsonPropertyName("base_commit_sha")] string? BaseCommitSha,
        [property: JsonPropertyName("head_commit_sha")] string? HeadCommitSha,
        [property: JsonPropertyName("start_commit_sha")] string? StartCommitSha);

    private sealed record GitLabCommitDto(
        string Id,
        string Title,
        [property: JsonPropertyName("author_name")] string AuthorName);

    private sealed record GitLabDiffDto(
        [property: JsonPropertyName("old_path")] string OldPath,
        [property: JsonPropertyName("new_path")] string NewPath,
        string Diff,
        [property: JsonPropertyName("new_file")] bool NewFile,
        [property: JsonPropertyName("deleted_file")] bool DeletedFile);
}
