namespace MappedFolderServer.Util;

public class SlugChecker
{
    public static readonly HashSet<string> ReservedSlugs = new HashSet<string>
    {
        "password", "slugs", "forbidden", "assets", "api"
    };
    
    public static bool IsSlugValid(string slug)
    {
        if (slug.EndsWith(".html")) return false;
        if(ReservedSlugs.Contains(slug)) return false;
        return true;
    }
}