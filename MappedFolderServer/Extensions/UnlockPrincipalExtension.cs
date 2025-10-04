using System.Security.Claims;
using MappedFolderServer.Data;


public static class UnlockPrincipalExtension
{
    /// <summary>
    /// Checks whether a user has unlocked this slug previously via password entry
    /// </summary>
    /// <param name="user"></param>
    /// <param name="entry"></param>
    /// <returns></returns>
    public static bool HasAccessToSlug(this ClaimsPrincipal user, SlugEntry entry)
    {
        return user.Claims.Any(c => c.Type == "UnlockedSlug" && c.Value == entry.Id.ToString());
    }
    /// <summary>
    /// Checks wheter the creator has authorized this session to see private slugs
    /// </summary>
    /// <param name="user"></param>
    /// <param name="entry"></param>
    /// <returns></returns>
    public static bool AlwaysHasAccessToSlug(this ClaimsPrincipal user, SlugEntry entry)
    {
        return user.Claims.Any(c => c.Type == "RemoteUnlockedSlug" && c.Value == entry.Id.ToString());
    }
}