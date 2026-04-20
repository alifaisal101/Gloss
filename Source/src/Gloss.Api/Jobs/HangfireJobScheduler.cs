using Gloss.Application.Jobs;
using Gloss.Infrastructure.Jobs;
using Hangfire;

namespace Gloss.Api.Jobs;

internal sealed class HangfireJobScheduler() : IJobScheduler
{
    private const string PollAllJobId = "poll-all-repos";

    public void SchedulePollAll(string cron)
    {
        RecurringJob.AddOrUpdate<RepositoryPollJob>(PollAllJobId, job => job.ExecuteAsync(CancellationToken.None), cron);
    }

    public void EnqueueReview(Guid mergeRequestId)
    {
        BackgroundJob.Enqueue<ReviewMergeRequestJob>(job => job.ExecuteAsync(mergeRequestId, CancellationToken.None));
    }

    public void EnqueueProjectionUpdate()
        => throw new NotSupportedException();
}
