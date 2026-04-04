namespace Gloss.Application.Jobs;

public sealed record ScheduledJob(string RepositoryPath, string Cron);
