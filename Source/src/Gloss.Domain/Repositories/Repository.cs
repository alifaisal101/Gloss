using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.Repositories;

public sealed class Repository : AggregateRoot<Guid>
{
    public string ProjectPath { get; private set; } = null!;
    public string Provider { get; private set; } = null!;
    public string? PollCron { get; private set; }

    private Repository() : base(Guid.NewGuid()) { }

    public static Repository Create(string projectPath, string provider)
    {
        var repo = new Repository();
        repo.ProjectPath = projectPath;
        repo.Provider = provider;
        return repo;
    }

    public void SetPollCron(string pollCron) => PollCron = pollCron;
}
