using System.Security.Claims;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MappedFolderServer.Controllers;

[Route("/api/v1/authorize")]
public class SlugAuthController : Controller
{
    private readonly AppDatabaseContext _db;

    public SlugAuthController(AppDatabaseContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> NormalLogin([FromBody] LoginRequest login)
    {
        if (Config.Instance.UseOAuth)
            return Problem(
                "Username+password authentication has been disabled by your admin. Please use OAuth instead.");
        if (login.Password != Config.Instance.AdminPassword || login.Username != Config.Instance.AdminUser)
        {
            return Unauthorized();
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.Username)
        };

        var identity = new ClaimsIdentity(claims, "oidc");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AppCookie", principal);

        return Ok(); // back to requested slug
    }

    [HttpPost("{slug}")]
    public async Task<IActionResult> Submit(string slug, [FromBody]LoginRequest login)
    {
        
        var entry = _db.Slugs.FirstOrDefault(p => p.Slug == slug);
        if (entry == null) return NotFound();

        ClaimsPrincipal? principal = ConfirmPassword(entry, login.Password);
        if (principal == null) return Unauthorized();

        await HttpContext.SignInAsync("AppCookie", principal);

        return Ok(); // back to requested slug
    }

    public static ClaimsPrincipal GetClaim(SlugEntry slug, string claimName = "UnlockedSlug")
    {                  
        // Add a claim proving the user has unlocked this slug
        var claims = new List<Claim>
        {
            new(claimName, slug.Id.ToString())
        };
                                                      
        var identity = new ClaimsIdentity(claims, "AppCookie");
        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal? ConfirmPassword(SlugEntry slug, string password)
    {
        if (!BCrypt.Net.BCrypt.Verify(password, slug.PasswordHash))
        {
            return null;
        }

        return GetClaim(slug);
    }
}