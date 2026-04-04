namespace Gloss.Application.Reviews;

public interface IReviewProvider
{
    Task<IReadOnlyList<ReviewComment>> ReviewAsync(string diff, CancellationToken cancellationToken);
}
