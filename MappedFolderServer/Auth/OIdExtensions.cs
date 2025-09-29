using System.Security.Claims;

namespace MappedFolderServer.Auth;
public class UserClaims
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }
    public string Username { get; set; }
    public string Nickname { get; set; }
    public string[] Groups { get; set; }
}

public static class HttpContextExtensions
{
    public static UserClaims GetUserClaims(this HttpContext context)
    {
        var claims = context.User.Claims;

        var userClaims = new UserClaims
        {
            UserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
            Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            EmailVerified = claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true",
            Username = claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value,
            Nickname = claims.FirstOrDefault(c => c.Type == "nickname")?.Value,
            Groups = claims.Where(c => c.Type == "groups").Select(c => c.Value).ToArray(),
        };
        foreach (var claim in claims)
        {
            Console.WriteLine(claim.Type + ": " + claim.Value);
        }

        return userClaims;
    }
}