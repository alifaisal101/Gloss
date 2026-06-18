namespace Gloss.Application.Llm;

public interface ILlmModelCatalog
{
    bool UsesAdaptiveThinking(string model);
    int? MaxOutputTokens(string model);
}
