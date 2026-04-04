using Gloss.Application.Jobs.ListJobs;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.Jobs;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/jobs", async (
            [FromServices] ListJobsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).WithTags("Jobs");

        return app;
    }
}
