using Gloss.Application.Jobs;
using Gloss.Infrastructure.Projection;
using Gloss.Infrastructure.Repositories;
using Gloss.Infrastructure.Reviews;
using Hangfire;

namespace Gloss.Api.Jobs;

internal sealed class HangfireJobScheduler() : IJobScheduler
{
    private const string PollAllJobId = "poll-all-repos";

    public void SchedulePollAll(string cron)
    {
        RecurringJob.AddOrUpdate<RepositoryPollJob>(PollAllJobId, job => job.ExecuteAsync(CancellationToken.None), cron);
    }

    public string EnqueueReview(Guid mergeRequestId) =>
        BackgroundJob.Enqueue<ReviewMergeRequestJob>(job => job.ExecuteAsync(mergeRequestId, CancellationToken.None));

    public void CancelReview(string jobId) => BackgroundJob.Delete(jobId);

    public void EnqueueProjectionUpdate()
        => BackgroundJob.Enqueue<UpdateProjectionJob>(job => job.ExecuteAsync(CancellationToken.None));
}
