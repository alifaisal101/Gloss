namespace Gloss.Infrastructure.Reviews.Anthropic;

internal interface IClaudeApiClient
{
    Task<ClaudeResponse> SendAsync(
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> messages,
        IReadOnlyList<ClaudeToolDefinition> tools,
        CancellationToken cancellationToken);
}
