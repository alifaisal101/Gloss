using Gloss.Application.Reviews.ReviewMergeRequest;

namespace Gloss.Infrastructure.Jobs;

public sealed class ReviewMergeRequestJob(ReviewMergeRequestHandler handler)
{
    public async Task ExecuteAsync(Guid mergeRequestId, CancellationToken cancellationToken) =>
        await handler.HandleAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
}
