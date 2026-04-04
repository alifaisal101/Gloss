using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.EfCore;

public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options);