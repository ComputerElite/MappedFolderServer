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
        if (login.Password != "admin" || login.Username != "admin")
        {
            return Unauthorized();
        }
        var claims = new List<Claim>
        {
            new("AdminUserId", login.Username)
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal);

        return Ok(); // back to requested slug
    }

    [HttpPost("{slug}")]
    public async Task<IActionResult> Submit(string slug, [FromBody]LoginRequest login)
    {
        
        var entry = _db.Mappings.FirstOrDefault(p => p.Slug == slug);
        if (entry == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(login.Password + entry.PasswordSalt, entry.PasswordHash))
        {
            return Unauthorized();
        }

        // Add a claim proving the user has unlocked this slug
        var claims = new List<Claim>
        {
            new("UnlockedSlug", entry.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal);

        return Ok(); // back to requested slug
    }
}