namespace Gloss.Application.Configs.SaveConfig;

public sealed record SaveConfigCommand(
    string GitProvider,
    Uri GitBaseUrl,
    string GitToken,
    IReadOnlyList<string> GitProjects,
    string LlmProvider,
    string LlmApiKey,
    string LlmModel,
    bool LlmReasoningEnabled,
    string DefaultPollCron);
