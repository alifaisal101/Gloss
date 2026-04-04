using BuildingBlocks.Infrastructure.Api.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Api.Documentation;

/// <summary>
/// <para>Fluent extensions for documenting endpoints with rich Scalar metadata.</para>
/// <para>
/// Usage:
///   group.MapPost("/", handler)
///       .WithDoc("Create subscription", "Creates a new subscription for the user.")
///       .Produces201&lt;Guid&gt;("Subscription created")
///       .ProducesError(400, "Subscription.EmptyPlan", "Plan is required");
/// </para>
/// <para>
///   group.MapGet("/user/{userId}", handler)
///       .WithDoc("List subscriptions", "Returns paginated subscriptions for a user.")
///       .ProducesPaged&lt;Subscription&gt;();
/// </para>
/// </summary>
public static class EndpointExtensions
{
    /// <summary>Sets operation summary + description for Scalar sidebar.</summary>
    public static RouteHandlerBuilder WithDoc(
        this RouteHandlerBuilder builder,
        string summary,
        string? description = null)
    {
        builder.WithSummary(summary);
        if (description is not null) builder.WithDescription(description);
        return builder;
    }

    /// <summary>Documents a 201 Created response with the data type.</summary>
    public static RouteHandlerBuilder Produces201<T>(
        this RouteHandlerBuilder builder,
        string description = "Resource created")
    {
        builder.Produces<ApiResponse<T>>(201).WithDescription(description);
        return builder;
    }

    /// <summary>Documents a 200 OK response with paged data.</summary>
    public static RouteHandlerBuilder ProducesPaged<T>(
        this RouteHandlerBuilder builder,
        string description = "Paginated results")
    {
        builder.Produces<ApiResponse<IEnumerable<T>>>(200)
            .WithDescription(description);
        return builder;
    }

    /// <summary>Documents a specific error response with example code and message.</summary>
    public static RouteHandlerBuilder ProducesError(
        this RouteHandlerBuilder builder,
        int statusCode,
        string errorCode,
        string errorMessage)
    {
        builder.Produces<ApiResponse>(statusCode).WithDescription($"{errorCode}: {errorMessage}");
        return builder;
    }

    /// <summary>Marks the endpoint as deprecated in docs.</summary>
    public static RouteHandlerBuilder Deprecated(this RouteHandlerBuilder builder)
    {
        builder.WithMetadata(new ObsoleteAttribute());
        return builder;
    }
}
