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

    [HttpGet("signout")]
    public async Task<IActionResult> Signout()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/");
    }

    [HttpGet("startlogin")]
    public IActionResult CheckLoginType()
    {
        if (Config.Instance.UseOAuth) return Redirect("/api/v1/sso/start");
        return Redirect("/password");
    }

    [HttpGet("start")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public async Task<IActionResult> StartLogin()
    {
        
        return Redirect("/slugs");
    }
}