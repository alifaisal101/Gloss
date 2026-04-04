using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Abstractions;
using Gloss.Application.Reviews;
using Gloss.Domain.Configs;

namespace Gloss.Infrastructure.Reviews;

internal sealed class AnthropicReviewProvider(
    HttpClient httpClient,
    IConfigRepository configRepository,
    ISecretEncryptor encryptor) : IReviewProvider
{
    private const string ApiVersion = "2023-06-01";
    private const string DefaultModel = "claude-sonnet-4-6";
    private const int MaxTokens = 16000;
    private const int ThinkingBudget = 10000;

    public async Task<IReadOnlyList<ReviewComment>> ReviewAsync(string diff, CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return [];

        var apiKey = encryptor.Decrypt(config.LlmApiKey).Value;
        var model = string.IsNullOrWhiteSpace(config.LlmModel) ? DefaultModel : config.LlmModel;

        var requestBody = BuildRequest(model, diff, config.LlmReasoningEnabled);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("v1/messages", UriKind.Relative));
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);
        if (config.LlmReasoningEnabled)
            request.Headers.Add("anthropic-beta", "interleaved-thinking-2025-05-14");
        request.Content = JsonContent.Create(requestBody, options: JsonOptions);

        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (result is null) return [];

        var text = result.Content.FirstOrDefault(c => string.Equals(c.Type, "text", StringComparison.Ordinal))?.Text;
        if (string.IsNullOrWhiteSpace(text)) return [];

        return ParseComments(text);
    }

    private static object BuildRequest(string model, string diff, bool reasoningEnabled)
    {
        var systemPrompt =
            """
            You are a senior software engineer performing a code review.
            Analyze the provided git diff and identify meaningful issues: bugs, null dereferences, missing error handling, security problems, logic errors, or significant code quality issues.
            Do NOT comment on style preferences, formatting, or trivial naming.
            Respond with a JSON array only — no markdown, no prose, just the raw JSON array.
            Each element must have:
              "filePath": the file path as shown in the diff (e.g. "src/Foo.cs")
              "line": the new-file line number closest to the issue (integer)
              "body": a concise, actionable comment addressed to the author
              "reasoning": one sentence explaining why this is an issue (optional, can be null)
            If there are no meaningful issues, respond with an empty array: []
            """;

        if (reasoningEnabled)
        {
            return new
            {
                model,
                max_tokens = MaxTokens,
                thinking = new { type = "enabled", budget_tokens = ThinkingBudget },
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = $"Review this diff:\n\n{diff}" }
                }
            };
        }

        return new
        {
            model,
            max_tokens = MaxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = $"Review this diff:\n\n{diff}" }
            }
        };
    }

    private static List<ReviewComment> ParseComments(string text)
    {
        try
        {
            var trimmed = text.Trim();
            var start = trimmed.IndexOf('[', StringComparison.Ordinal);
            var end = trimmed.LastIndexOf(']');
            if (start < 0 || end < 0) return [];

            var json = trimmed[start..(end + 1)];
            var items = JsonSerializer.Deserialize<CommentDto[]>(json, JsonOptions);
            if (items is null) return [];

            return items
                .Where(c => !string.IsNullOrWhiteSpace(c.FilePath) && !string.IsNullOrWhiteSpace(c.Body))
                .Select(c => new ReviewComment(c.FilePath!, c.Line, c.Body!, c.Reasoning))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed record AnthropicResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<ContentBlock> Content);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);

    private sealed record CommentDto(
        [property: JsonPropertyName("filePath")] string? FilePath,
        [property: JsonPropertyName("line")] int Line,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("reasoning")] string? Reasoning);
}
