using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Data;

public class AppDatabaseContext : DbContext
{
    public DbSet<SlugEntry>  Mappings { get; set; }
    public DbSet<AuthenticatedSession> Sessions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FolderClaim> FolderClaims { get; set; }
    public DbSet<RemoteWebsocketData>  RemoteWebsocketData { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Config.GetFromEnvironment();
        optionsBuilder.UseSqlite(Config.Instance?.DbConnectionString ?? new Config().DbConnectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
}