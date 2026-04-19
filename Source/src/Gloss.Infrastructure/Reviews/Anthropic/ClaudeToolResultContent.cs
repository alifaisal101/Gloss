namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed record ClaudeToolResultContent(string ToolUseId, string Result) : IClaudeContent;
