using System.ComponentModel.DataAnnotations.Schema;

namespace MappedFolderServer.Data;

public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; }

    public string OidcId { get; init; } = null!;
    public string Name { get; set; } = null!;
    public bool IsAdmin { get; set; } = false;

    /// <summary>
    /// Checks whether a user can access a permission
    /// </summary>
    /// <param name="fullPath"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    public bool CanAccessFolder(string fullPath, AppDatabaseContext db)
    {
        if (IsAdmin) return true;
        fullPath = Path.GetFullPath(fullPath); // just make sure that '../' cannot exist in the path for example. A fully resolved path must be used in this function
        if (db.FolderClaims.Any(x => x.ForUserId == Id && fullPath.StartsWith(x.FolderPath)))
            return true;
        return false;
    }
}