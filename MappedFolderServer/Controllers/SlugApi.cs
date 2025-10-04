using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/slugs")]
[Authorize(AuthenticationSchemes = "oidc")]
public class SlugApi : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly AppDatabaseContext _db;

    public SlugApi(ICurrentUserService currentUser, AppDatabaseContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    [HttpGet("all")]
    public IActionResult GetAll()
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        if (loggedInUser.IsAdmin)
        {
            return Ok(_db.Slugs.ToList());
        }
        return Ok(_db.Slugs.Where(x => x.CreatedBy == loggedInUser.Id).ToList());
    }

    [HttpPost]
    public IActionResult Create([FromBody] SlugEntry slugEntry)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        slugEntry.CreatedBy = slugEntry.EditedBy = loggedInUser.Id;
        slugEntry.FolderPath = Path.GetFullPath(slugEntry.FolderPath);
        if (!loggedInUser.CanAccessFolder(slugEntry.FolderPath, _db))
        {
            return BadRequest(
                new ApiError("You're not allowed to access this directory. Please ask the admin for permission"));
        }
        if (!Directory.Exists(slugEntry.FolderPath))
        {
            return BadRequest(new ApiError("Requested directory doesn't exist"));
        }
        
        if (_db.Slugs.Any(x => x.Slug == slugEntry.Slug))
        {
            return BadRequest(new ApiError("Slug already exists, please choose a different slug"));
        }
        
        _db.Slugs.Add(slugEntry);
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost("{id:guid}")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] SlugEntry slugEntry)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        SlugEntry? m = _db.Slugs.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        if (!m.CanBeAccessedBy(loggedInUser)) return Forbid();
        if (_db.Slugs.Any(x => x.Slug == slugEntry.Slug && x.Id != id))
        {
            return BadRequest(new ApiError("Slug already exists, please choose a different slug"));
        }
        slugEntry.FolderPath = Path.GetFullPath(slugEntry.FolderPath);
        if (!loggedInUser.CanAccessFolder(slugEntry.FolderPath, _db))
        {
            return BadRequest(
                new ApiError("You're not allowed to access this directory. Please ask the admin for permission"));
        }
        m.EditedBy = loggedInUser.Id;
        m.Slug = slugEntry.Slug;
        m.FolderPath = slugEntry.FolderPath;
        m.IsPublic = slugEntry.IsPublic;
        if (!Directory.Exists(slugEntry.FolderPath))
        {
            return BadRequest();
        }
        _db.Slugs.Update(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpDelete("{id:guid}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        SlugEntry? m = _db.Slugs.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        if (!m.CanBeAccessedBy(loggedInUser)) return Forbid();
        _db.Slugs.Remove(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpPost("{id:guid}/password")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] LoginRequest request)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        SlugEntry? m = _db.Slugs.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        if (!m.CanBeAccessedBy(loggedInUser)) return Forbid();
        m.PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt();
        m.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, m.PasswordSalt);
        _db.Slugs.Update(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpDelete("{id:guid}/password")]
    public IActionResult Update([FromRoute] Guid id)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        SlugEntry? m = _db.Slugs.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        if (!m.CanBeAccessedBy(loggedInUser)) return Forbid();
        m.PasswordHash = null;
        _db.Slugs.Update(m);
        _db.SaveChanges();
        return Ok();
    }
}