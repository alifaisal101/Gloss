using System.Net.Http.Json;
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
            results.Add(new(mr.Iid, mr.Title, mr.Description, mr.SourceBranch, mr.TargetBranch, mr.Author.Username, diff));
        }

        return results;
    }

    private async Task<string> GetDiffAsync(string baseUrl, string encodedPath, int iid, string token, CancellationToken cancellationToken)
    {
        var diffs = await SendAsync<GitLabDiffDto[]>(
            $"{baseUrl}/api/v4/projects/{encodedPath}/merge_requests/{iid}/diffs",
            token, cancellationToken).ConfigureAwait(false);

        if (diffs is null || diffs.Length == 0) return string.Empty;

        return string.Join("\n", diffs.Select(d => d.Diff));
    }

    private async Task<T?> SendAsync<T>(string url, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("PRIVATE-TOKEN", token);
        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private sealed record GitLabMrDto(
        int Iid,
        string Title,
        string? Description,
        [property: JsonPropertyName("source_branch")] string SourceBranch,
        [property: JsonPropertyName("target_branch")] string TargetBranch,
        GitLabAuthorDto Author);

    private sealed record GitLabAuthorDto(string Username);

    private sealed record GitLabDiffDto(string Diff);
}
