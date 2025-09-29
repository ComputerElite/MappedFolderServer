using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MappedFolderServer.Controllers;
[Authorize(AuthenticationSchemes = "oidc")]
[Route("/api/v1/me")]
public class MeApi : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDatabaseContext _db;
    
    public MeApi(IWebHostEnvironment env, AppDatabaseContext db)
    {
        _env = env;
        _db = db;
    }
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(HttpContext.GetUserClaims());
    }

    [HttpGet("db")]
    public IActionResult GetDb()
    {
        return Ok(_db.Users.FirstOrDefault(x => x.OidcId == HttpContext.GetUserClaims().UserId));
    }
}