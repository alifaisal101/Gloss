using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace BuildingBlocks.Infrastructure.Api.Documentation;

internal static class OperationTransformers
{
    public static Task AddStandardResponses(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var errorSchema = BuildErrorSchema();

        var errorResponses = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["400"] = "Bad Request — invalid input or business rule violation",
            ["401"] = "Unauthorized — missing or invalid Bearer token",
            ["404"] = "Not Found — resource does not exist",
            ["422"] = "Validation Error — one or more fields failed validation",
            ["429"] = "Too Many Requests — rate limit exceeded",
            ["500"] = "Internal Server Error — unexpected failure",
        };

        operation.Responses ??= [];

        foreach (var (statusCode, desc) in errorResponses)
        {
            if (!operation.Responses.ContainsKey(statusCode))
            {
                operation.Responses[statusCode] = new OpenApiResponse
                {
                    Description = desc,
                    Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal)
                    {
                        ["application/json"] = new() { Schema = errorSchema },
                    },
                };
            }
        }
        return Task.CompletedTask;
    }

    private static OpenApiSchema BuildErrorSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
        {
            ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = JsonValue.Create(400) },
            ["error"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                {
                    ["code"]    = new OpenApiSchema { Type = JsonSchemaType.String, Example = JsonValue.Create("Entity.NotFound") },
                    ["message"] = new OpenApiSchema { Type = JsonSchemaType.String, Example = JsonValue.Create("The requested resource was not found.") },
                    ["errors"]  = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array | JsonSchemaType.Null,
                        Items = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                            {
                                ["field"]   = new OpenApiSchema { Type = JsonSchemaType.String },
                                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            },
                        },
                    },
                },
            },
            ["traceId"]   = new OpenApiSchema { Type = JsonSchemaType.String, Example = JsonValue.Create("00-a1b2c3d4e5f67890-a1b2c3d4e5f67890-01") },
            ["requestId"] = new OpenApiSchema { Type = JsonSchemaType.String, Example = JsonValue.Create("r-7f3a2b1c") },
        },
    };

    public static Task AddCommonHeaders(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Response language. `en` (default) or `ar` (Arabic). Affects error messages.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = [JsonValue.Create("en"), JsonValue.Create("ar")],
                Default = JsonValue.Create("en"),
            },
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Region",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Region code for feature flag targeting (e.g., `eu`, `us`, `mena`).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Distributed trace ID. Auto-generated if not provided. Returned as `X-Correlation-Id`.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        });

        return Task.CompletedTask;
    }

    public static Task AddFeatureFlagDocs(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var featureMetadata = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<FeatureFlagMetadata>()
            .FirstOrDefault();

        if (featureMetadata is null) return Task.CompletedTask;

        operation.Description = (operation.Description ?? "") +
            $"\n\n> **Feature Flag: `{featureMetadata.FeatureName}`**\n>\n" +
            $"> This endpoint is gated behind the `{featureMetadata.FeatureName}` feature flag. " +
            "When disabled, returns `404 Feature.NotAvailable`. " +
            "Targeting uses JWT `sub` (user), `role` claims (groups), and `X-Region` header.";

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
        operation.Extensions["x-feature-flag"] = new OpenApiJsonExtension(JsonValue.Create(featureMetadata.FeatureName));

        return Task.CompletedTask;
    }
}