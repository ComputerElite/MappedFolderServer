using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Data;

public class AppDatabaseContext : DbContext
{
    public DbSet<SlugEntry>  Mappings { get; set; }
    public DbSet<AuthenticatedSession> Sessions { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        new Config().PopulateFromEnvironment();
        optionsBuilder.UseSqlite(Config.Instance?.DbConnectionString ?? new Config().DbConnectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
}