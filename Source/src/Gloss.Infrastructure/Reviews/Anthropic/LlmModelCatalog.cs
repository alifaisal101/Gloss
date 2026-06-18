using System.Globalization;
using Gloss.Application.Llm;
using Microsoft.Extensions.Configuration;

namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed class LlmModelCatalog(IConfiguration configuration) : ILlmModelCatalog
{
    public bool UsesAdaptiveThinking(string model) =>
        string.Equals(Find(model)?["AdaptiveThinking"], "true", StringComparison.OrdinalIgnoreCase);

    public int? MaxOutputTokens(string model)
    {
        var entry = Find(model);
        if (entry is not null && int.TryParse(entry["MaxOutputTokens"], CultureInfo.InvariantCulture, out var limit))
            return limit;
        return null;
    }

    private IConfigurationSection? Find(string model) =>
        configuration.GetSection("Anthropic:Models")
            .GetChildren()
            .FirstOrDefault(s => string.Equals(s["Id"], model, StringComparison.Ordinal));
}
