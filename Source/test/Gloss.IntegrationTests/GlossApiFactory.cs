using Gloss.Application.Jobs;
using Gloss.Application.MergeRequests;
using Gloss.Application.Repositories;
using Gloss.Application.Reviews;
using Gloss.Domain.Repositories;
using Gloss.Infrastructure;
using Gloss.Infrastructure.Reviews;
using Gloss.Infrastructure.Reviews.Anthropic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Gloss.IntegrationTests;

public class GlossApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static PostgreSqlContainer? _sharedContainer;
    private static int _refCount;

    private readonly string _databaseName = $"gloss_{Guid.NewGuid():N}";

    public string ConnectionString => new NpgsqlConnectionStringBuilder(_sharedContainer!.GetConnectionString())
    {
        Database = _databaseName
    }.ToString();

    public Mock<IGitClient> GitClient { get; } = new();
    public Mock<IReviewProvider> ReviewProvider { get; } = new();
    public Mock<IJobScheduler> JobScheduler { get; } = new();
    public Mock<IRepoManager> RepoManager { get; } = new();
    internal Mock<IClaudeApiClient> ClaudeApiClient { get; } = new();
    internal Mock<IReviewFileSystem> ReviewFileSystem { get; } = new();

    protected virtual bool UseRealReviewProvider => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GlossDb"] = ConnectionString,
                ["Hangfire:Enabled"] = "false",
            }));

        builder.ConfigureServices(services =>
        {
            foreach (var d in services
                .Where(d => d.ImplementationType?.FullName?.StartsWith("Hangfire.", StringComparison.Ordinal) == true
                         || d.ImplementationFactory?.Method.DeclaringType?.FullName?.StartsWith("Hangfire.", StringComparison.Ordinal) == true)
                .ToList())
            {
                services.Remove(d);
            }
            services.AddSingleton(GitClient.Object);
            services.AddSingleton(JobScheduler.Object);
            services.AddSingleton(RepoManager.Object);
            if (UseRealReviewProvider)
            {
                services.AddSingleton(ClaudeApiClient.Object);
                services.AddSingleton(ReviewFileSystem.Object);
            }
            else
            {
                services.AddSingleton(ReviewProvider.Object);
            }
        });
    }

    public async Task InitializeAsync()
    {
        await Lock.WaitAsync();
        try
        {
            if (_sharedContainer is null)
            {
                _sharedContainer = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
                await _sharedContainer.StartAsync();
            }
            Interlocked.Increment(ref _refCount);
        }
        finally
        {
            Lock.Release();
        }

        await using var adminConn = new NpgsqlConnection(_sharedContainer.GetConnectionString());
        await adminConn.OpenAsync();
        await using var createCmd = adminConn.CreateCommand();
        createCmd.CommandText = $"""CREATE DATABASE "{_databaseName}" """;
        await createCmd.ExecuteNonQueryAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GlossDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        GitClient.Reset();
        ReviewProvider.Reset();
        JobScheduler.Reset();
        RepoManager.Reset();
        ClaudeApiClient.Reset();
        ReviewFileSystem.Reset();
        GitClient.Setup(x => x.GetCommitsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        GitClient.Setup(x => x.GetMrShasAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MrShasData("base-sha", "head-sha", "start-sha"));
        RepoManager
            .Setup(r => r.EnsureReadyAsync(It.IsAny<Repository>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/repos/test");

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DO $$
            DECLARE r RECORD;
            BEGIN
              FOR r IN SELECT tablename FROM pg_tables WHERE schemaname = 'public'
              LOOP
                EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' CASCADE';
              END LOOP;
            END $$;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await using var adminConn = new NpgsqlConnection(_sharedContainer!.GetConnectionString());
        await adminConn.OpenAsync();
        await using var dropCmd = adminConn.CreateCommand();
        dropCmd.CommandText = $"""DROP DATABASE IF EXISTS "{_databaseName}" WITH (FORCE)""";
        await dropCmd.ExecuteNonQueryAsync();

        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            await Lock.WaitAsync();
            try
            {
                if (_refCount == 0)
                {
                    await _sharedContainer.DisposeAsync();
                    _sharedContainer = null;
                }
            }
            finally
            {
                Lock.Release();
            }
        }
    }
}
