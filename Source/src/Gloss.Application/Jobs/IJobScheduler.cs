namespace Gloss.Application.Jobs;

public interface IJobScheduler
{
    void SchedulePollAll(string cron);
}
