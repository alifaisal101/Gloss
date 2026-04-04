using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace BuildingBlocks.Infrastructure.Api.Documentation;

internal sealed class OpenApiJsonExtension(JsonNode? value) : IOpenApiExtension
{
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var str)) writer.WriteValue(str);
        else writer.WriteRaw(value?.ToJsonString() ?? "null");
    }
}