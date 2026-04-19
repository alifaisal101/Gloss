namespace Gloss.Application.Reviews;

public interface IReviewProvider
{
    Task<IReadOnlyList<ReviewComment>> ReviewAsync(ReviewContext context, CancellationToken cancellationToken);
}
