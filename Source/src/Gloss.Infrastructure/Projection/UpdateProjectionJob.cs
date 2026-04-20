using Gloss.Application.Projection.UpdateProjection;

namespace Gloss.Infrastructure.Projection;

public sealed class UpdateProjectionJob(UpdateProjectionHandler handler)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
    }
}
