using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Abstractions;
using Gloss.Domain.Configs;
using Microsoft.Extensions.Configuration;

namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed class AnthropicApiClient(
    HttpClient httpClient,
    IConfigRepository configRepository,
    ISecretEncryptor encryptor,
    IConfiguration configuration) : IClaudeApiClient
{
    public async Task<ClaudeResponse> SendAsync(
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> messages,
        IReadOnlyList<ClaudeToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null)
            return new ClaudeResponse("end_turn", []);

        var apiKey = encryptor.Decrypt(config.LlmApiKey).Value;
        var model = string.IsNullOrWhiteSpace(config.LlmModel)
            ? configuration["Anthropic:DefaultModel"]!
            : config.LlmModel;
        var apiVersion = configuration["Anthropic:ApiVersion"]!;

        var requestBody = BuildRequest(model, systemPrompt, messages, tools, config);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("v1/messages", UriKind.Relative));
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", apiVersion);
        if (config.LlmReasoningEnabled)
            request.Headers.Add("anthropic-beta", "interleaved-thinking-2025-05-14");
        request.Content = JsonContent.Create(requestBody, options: SerializerOptions);

        var httpResponse = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        var result = await httpResponse.Content
            .ReadFromJsonAsync<AnthropicResponse>(SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return result is null
            ? new ClaudeResponse("end_turn", [])
            : MapResponse(result);
    }

    private static object BuildRequest(
        string model,
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> messages,
        IReadOnlyList<ClaudeToolDefinition> tools,
        Config config)
    {
        var apiMessages = messages.Select(m => new
        {
            role = m.Role,
            content = m.Content.Select(SerializeContent).ToArray()
        }).ToArray();

        var apiTools = tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = JsonSerializer.Deserialize<JsonElement>(t.InputSchemaJson, SerializerOptions)
        }).ToArray();

        if (config.LlmReasoningEnabled)
        {
            return new
            {
                model,
                max_tokens = config.LlmMaxTokens,
                thinking = new { type = "enabled", budget_tokens = config.LlmThinkingBudget },
                system = systemPrompt,
                tools = apiTools,
                messages = apiMessages
            };
        }

        return new
        {
            model,
            max_tokens = config.LlmMaxTokens,
            system = systemPrompt,
            tools = apiTools,
            messages = apiMessages
        };
    }

    private static object SerializeContent(IClaudeContent content) => content switch
    {
        ClaudeTextContent t => (object)new { type = "text", text = t.Text },
        ClaudeToolUseContent tu => new
        {
            type = "tool_use",
            id = tu.Id,
            name = tu.Name,
            input = JsonSerializer.Deserialize<JsonElement>(tu.InputJson, SerializerOptions)
        },
        ClaudeToolResultContent tr => new { type = "tool_result", tool_use_id = tr.ToolUseId, content = tr.Result },
        _ => throw new InvalidOperationException($"Unknown content type: {content.GetType().Name}")
    };

    private static ClaudeResponse MapResponse(AnthropicResponse response)
    {
        var content = response.Content
            .Select(MapContentBlock)
            .OfType<IClaudeContent>()
            .ToList();
        return new ClaudeResponse(response.StopReason ?? "end_turn", content);
    }

    private static IClaudeContent? MapContentBlock(ContentBlock block) => block.Type switch
    {
        "text" when block.Text is not null => new ClaudeTextContent(block.Text),
        "tool_use" when block.Name is not null => new ClaudeToolUseContent(
            block.Id ?? string.Empty,
            block.Name,
            block.Input.HasValue ? JsonSerializer.Serialize(block.Input.Value, SerializerOptions) : "{}"),
        _ => null
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed record AnthropicResponse(
        [property: JsonPropertyName("stop_reason")] string? StopReason,
        [property: JsonPropertyName("content")] IReadOnlyList<ContentBlock> Content);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("input")] JsonElement? Input);
}
