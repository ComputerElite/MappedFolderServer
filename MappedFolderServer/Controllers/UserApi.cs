using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MappedFolderServer.Controllers;
[Authorize("user")]
[Route("/api/v1/users")]
public class UserApi : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly AppDatabaseContext _db;
    
    public UserApi(ICurrentUserService currentUser, AppDatabaseContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    [HttpGet("all")]
    public IActionResult GetAll()
    {
        
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        if (!loggedInUser.IsAdmin) return Forbid();
        return Ok(_db.Users.ToList());
    }
    
    [HttpGet("me")]
    public IActionResult Get()
    {
        return Ok(HttpContext.GetUserClaims());
    }

    [HttpGet("me/db")]
    public IActionResult GetDb()
    {
        return Ok(_db.Users.FirstOrDefault(x => x.OidcId == HttpContext.GetUserClaims().UserId));
    }
}