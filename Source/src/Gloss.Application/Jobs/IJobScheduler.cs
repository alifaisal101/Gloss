namespace Gloss.Application.Jobs;

public interface IJobScheduler
{
    void SchedulePollAll(string cron);
    string EnqueueReview(Guid mergeRequestId);
    void CancelReview(string jobId);
    void EnqueueProjectionUpdate();
}
