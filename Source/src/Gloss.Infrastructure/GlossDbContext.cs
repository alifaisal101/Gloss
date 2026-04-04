using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.Configs;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure;

public sealed class GlossDbContext(DbContextOptions<GlossDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Config> Configs => Set<Config>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GlossDbContext).Assembly);
    }
}
