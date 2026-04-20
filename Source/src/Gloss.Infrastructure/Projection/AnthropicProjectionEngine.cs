using System.Text;
using BuildingBlocks.Application.EventSourcing;
using Gloss.Application.Projection;
using Gloss.Infrastructure.Reviews.Anthropic;

namespace Gloss.Infrastructure.Projection;

internal sealed class AnthropicProjectionEngine(IClaudeApiClient claudeApiClient) : IProjectionEngine
{
    public async Task<string> BuildUpdatedProjectionAsync(
        string currentProjection,
        IReadOnlyList<StoredEvent> newEvents,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(currentProjection);
        var userMessage = BuildUserMessage(newEvents);
        var messages = new List<ClaudeMessage>
        {
            new("user", [new ClaudeTextContent(userMessage)])
        };

        var response = await claudeApiClient.SendAsync(systemPrompt, messages, [], cancellationToken)
            .ConfigureAwait(false);

        var text = response.Content.OfType<ClaudeTextContent>().FirstOrDefault()?.Text;
        return string.IsNullOrWhiteSpace(text) ? currentProjection : text;
    }

    private static string BuildSystemPrompt(string currentProjection) =>
        $"""
        You are maintaining a persistent projection of a software engineer's code review preferences and style.

        The projection captures two dimensions:
        - Behavioural: what the reviewer flags, ignores, adds, or removes
        - Stylistic: tone, phrasing, length, vocabulary, level of directness

        Rules:
        - Write in second person ("You flag...", "You tend to...", "When you see X, you always...")
        - Be specific and precise — this projection is injected into every future review
        - When an event carries a reason, treat it as a scoping constraint and produce a specific bounded rule, not a generalisation
        - Preserve accurate rules from the current projection that are not contradicted by new events
        - Return only the updated projection text, nothing else

        <current_projection>
        {(string.IsNullOrEmpty(currentProjection) ? "(empty — this is the first projection)" : currentProjection)}
        </current_projection>
        """;

    private static string BuildUserMessage(IReadOnlyList<StoredEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("New events since the last projection update:");
        sb.AppendLine();
        foreach (var e in events)
        {
            sb.Append('[');
            sb.Append(e.OccurredAt.ToString("u"));
            sb.Append("] ");
            sb.AppendLine(e.EventType);
            sb.AppendLine(e.Payload.RootElement.GetRawText());
            sb.AppendLine();
        }
        sb.AppendLine("Produce the updated projection.");
        return sb.ToString();
    }
}
