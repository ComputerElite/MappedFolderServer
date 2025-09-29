using System.Security.Claims;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MappedFolderServer.Controllers;
[Route("/api/v1/sso")]
public class SsoApi : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDatabaseContext _db;
    
    public SsoApi(IWebHostEnvironment env, AppDatabaseContext db)
    {
        _env = env;
        _db = db;
    }
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(HttpContext.GetUserClaims());
    }

    [HttpGet("start")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public async Task<IActionResult> StartLogin()
    {
        return Redirect("/slugs");
    }
}