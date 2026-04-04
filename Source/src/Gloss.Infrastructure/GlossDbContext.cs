using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.Configs;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure;

public sealed class GlossDbContext(DbContextOptions<GlossDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Config> Configs => Set<Config>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<MergeRequest> MergeRequests => Set<MergeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GlossDbContext).Assembly);
    }
}
