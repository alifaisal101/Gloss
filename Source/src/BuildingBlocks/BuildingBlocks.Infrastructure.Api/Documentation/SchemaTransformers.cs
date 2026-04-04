using System.Text.Json.Nodes;
using BuildingBlocks.Domain.Models.Pagination;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BuildingBlocks.Infrastructure.Api.Documentation;

internal static class SchemaTransformers
{
    /// <summary>
    /// <para>Transforms value object types to their underlying primitives in OpenAPI docs:</para>
    /// <para>
    ///   Take → { type: "integer", minimum: 1, maximum: 1000, default: 20, description: "..." }
    ///   Skip → { type: "integer", minimum: 0, default: 0, description: "..." }
    /// </para>
    /// <para>This means consumers see clean integer params, not complex objects.</para>
    /// </summary>
    public static Task FlattenValueObjects(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(Take))
        {
            schema.Type = JsonSchemaType.Integer;
            schema.Format = "int32";
            schema.Minimum = "1";
            schema.Maximum = "1000";
            schema.Default = JsonValue.Create(20);
            schema.Description = "Number of items to return. Clamped to [1, 1000].";
            schema.Properties?.Clear();
        }
        else if (context.JsonTypeInfo.Type == typeof(Skip))
        {
            schema.Type = JsonSchemaType.Integer;
            schema.Format = "int32";
            schema.Minimum = "0";
            schema.Default = JsonValue.Create(0);
            schema.Description = "Number of items to skip. Minimum 0.";
            schema.Properties?.Clear();
        }

        return Task.CompletedTask;
    }
}