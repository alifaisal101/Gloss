using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace BuildingBlocks.Infrastructure.Api.Documentation;

public static class ScalarConfiguration
{
    public static IServiceCollection AddBuildingBlocksDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                ConfigureInfo(document, configuration);
                ConfigureServer(document, configuration);
                ConfigureTagGroups(document);
                return Task.CompletedTask;
            });

            options.AddOperationTransformer(OperationTransformers.AddStandardResponses);
            options.AddOperationTransformer(OperationTransformers.AddCommonHeaders);
            options.AddSchemaTransformer(SchemaTransformers.FlattenValueObjects);
        });

        return services;
    }

    public static IEndpointRouteBuilder MapBuildingBlocksDocumentation(this IEndpointRouteBuilder app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Gloss API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        return app;
    }

    private static void ConfigureInfo(OpenApiDocument document, IConfiguration configuration)
    {
        document.Info = new OpenApiInfo
        {
            Title = configuration["Observability:ServiceName"] ?? "Gloss",
            Version = "1.0.0",
            Description = BuildDescription(configuration),
        };
    }

    private static void ConfigureServer(OpenApiDocument document, IConfiguration configuration)
    {
        var serverUrl = configuration["Documentation:ServerUrl"];
        if (!string.IsNullOrEmpty(serverUrl))
            document.Servers = [new OpenApiServer { Url = serverUrl, Description = "API Server" }];
    }

    private static void ConfigureTagGroups(OpenApiDocument document)
    {
        document.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
        document.Extensions["x-tagGroups"] = new OpenApiJsonExtension(new JsonArray
        {
            new JsonObject { ["name"] = "Review",          ["tags"] = new JsonArray { "MergeRequests", "Comments" } },
            new JsonObject { ["name"] = "Configuration",   ["tags"] = new JsonArray { "Config", "Repositories", "Constitution" } },
            new JsonObject { ["name"] = "Infrastructure",  ["tags"] = new JsonArray { "Health" } },
        });
    }

    private static string BuildDescription(IConfiguration configuration)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine("Self-hosted code review assistant. Gloss watches your Git projects for merge requests, ");
        sb.AppendLine("reviews them using an LLM, and lets you inspect, edit, and publish comments from this UI.");
        sb.AppendLine();

        sb.AppendLine("## Response Envelope");
        sb.AppendLine();
        sb.AppendLine("Every endpoint returns the same JSON shape:");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("{ \"status\": 200, \"data\": { ... }, \"traceId\": \"...\", \"requestId\": \"...\" }");
        sb.AppendLine("{ \"status\": 400, \"error\": { \"code\": \"Config.InvalidGitProvider\", \"message\": \"...\" }, \"traceId\": \"...\", \"requestId\": \"...\" }");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Error Codes → HTTP Status");
        sb.AppendLine();
        sb.AppendLine("| Code pattern | HTTP |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| `*.NotFound` | 404 |");
        sb.AppendLine("| `*.Conflict` | 409 |");
        sb.AppendLine("| `*.Validation.*` | 422 |");
        sb.AppendLine("| *(anything else)* | 400 |");
        sb.AppendLine();

        AppendRateLimiting(sb, configuration);

        sb.AppendLine("## Infrastructure Endpoints");
        sb.AppendLine();
        sb.AppendLine("| Endpoint | Description |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| `GET /health` | Liveness probe |");
        sb.AppendLine("| `GET /ready` | Readiness probe — Postgres + Hangfire |");
        sb.AppendLine("| `GET /health/detail` | Full diagnostic JSON |");
        sb.AppendLine("| `GET /hangfire` | Background jobs dashboard |");
        sb.AppendLine("| `GET /scalar/v1` | This documentation |");
        sb.AppendLine("| `GET /openapi/v1.json` | OpenAPI 3.1 spec |");

        return sb.ToString();
    }

    private static void AppendRateLimiting(StringBuilder sb, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSec = configuration.GetValue("RateLimiting:WindowSeconds", 60);

        sb.AppendLine("## Rate Limiting");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Fixed-window: **{permitLimit} requests per {windowSec}s** per client IP. Returns `429` when exceeded.");
        sb.AppendLine();
    }
}
