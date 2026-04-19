namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed record ClaudeToolUseContent(string Id, string Name, string InputJson) : IClaudeContent;
