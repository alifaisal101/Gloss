using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Models.Secrets;

namespace Gloss.Domain.Configs;

public sealed class Config : AggregateRoot<Guid>
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    public GitProvider GitProvider { get; private set; } = null!;
    public Uri GitBaseUrl { get; private set; } = null!;
    public EncryptedSecret GitToken { get; private set; } = null!;
    public IReadOnlyList<string> GitProjects { get; private set; } = [];
    public LlmProvider LlmProvider { get; private set; } = null!;
    public EncryptedSecret LlmApiKey { get; private set; } = null!;
    public string LlmModel { get; private set; } = null!;
    public bool LlmReasoningEnabled { get; private set; }
    public int LlmMaxTokens { get; private set; }
    public int LlmThinkingBudget { get; private set; }
    public string DefaultPollCron { get; private set; } = null!;
    public bool IsPolling { get; private set; }

    public void SetPolling(bool polling) => IsPolling = polling;

    private Config() : base(SingletonId) { }

    public static Config Create(
        GitProvider gitProvider,
        Uri gitBaseUrl,
        EncryptedSecret gitToken,
        IReadOnlyList<string> gitProjects,
        LlmProvider llmProvider,
        EncryptedSecret llmApiKey,
        string llmModel,
        bool llmReasoningEnabled,
        int llmMaxTokens,
        int llmThinkingBudget,
        string defaultPollCron)
    {
        var config = new Config();
        config.Apply(gitProvider, gitBaseUrl, gitToken, gitProjects, llmProvider, llmApiKey, llmModel, llmReasoningEnabled, llmMaxTokens, llmThinkingBudget, defaultPollCron);
        return config;
    }

    public void Update(
        GitProvider gitProvider,
        Uri gitBaseUrl,
        EncryptedSecret gitToken,
        IReadOnlyList<string> gitProjects,
        LlmProvider llmProvider,
        EncryptedSecret llmApiKey,
        string llmModel,
        bool llmReasoningEnabled,
        int llmMaxTokens,
        int llmThinkingBudget,
        string defaultPollCron) =>
        Apply(gitProvider, gitBaseUrl, gitToken, gitProjects, llmProvider, llmApiKey, llmModel, llmReasoningEnabled, llmMaxTokens, llmThinkingBudget, defaultPollCron);

    private void Apply(
        GitProvider gitProvider,
        Uri gitBaseUrl,
        EncryptedSecret gitToken,
        IReadOnlyList<string> gitProjects,
        LlmProvider llmProvider,
        EncryptedSecret llmApiKey,
        string llmModel,
        bool llmReasoningEnabled,
        int llmMaxTokens,
        int llmThinkingBudget,
        string defaultPollCron)
    {
        GitProvider = gitProvider;
        GitBaseUrl = gitBaseUrl;
        GitToken = gitToken;
        GitProjects = gitProjects;
        LlmProvider = llmProvider;
        LlmApiKey = llmApiKey;
        LlmModel = llmModel;
        LlmReasoningEnabled = llmReasoningEnabled;
        LlmMaxTokens = llmMaxTokens;
        LlmThinkingBudget = llmThinkingBudget;
        DefaultPollCron = defaultPollCron;
    }
}
