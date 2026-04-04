using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.EfCore;

public static class MigrationExtensions
{
    public static async Task MigrateAsync<TDbContext>(this IHost host) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }
}
