using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MappedFolderServer.Controllers;

[Route("api/v1/folders")]
[Authorize(AuthenticationSchemes = "oidc")]
public class FolderClaimApi : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly AppDatabaseContext _db;

    public FolderClaimApi(ICurrentUserService currentUser, AppDatabaseContext db)
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
            return Ok(_db.FolderClaims.ToList());
        }
        return Ok(_db.FolderClaims.Where(x => x.ForUserId == loggedInUser.Id).ToList());
    }

    [HttpPost]
    public IActionResult Create([FromBody] FolderClaim folderClaimEntry)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        if (!loggedInUser.IsAdmin) return Forbid();
        folderClaimEntry.FolderPath = Path.GetFullPath(folderClaimEntry.FolderPath);
        if (!Directory.Exists(folderClaimEntry.FolderPath))
        {
            return BadRequest(new ApiError("Requested directory doesn't exist"));
        }
        
        _db.FolderClaims.Add(folderClaimEntry);
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost("{id:guid}")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] FolderClaim folderClaimEntry)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        if (!loggedInUser.IsAdmin) return Forbid();
        FolderClaim? m = _db.FolderClaims.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        folderClaimEntry.FolderPath = Path.GetFullPath(folderClaimEntry.FolderPath);
        if (!Directory.Exists(folderClaimEntry.FolderPath))
        {
            return BadRequest();
        }
        m.ForUserId = folderClaimEntry.ForUserId;
        m.FolderPath = folderClaimEntry.FolderPath;
        _db.FolderClaims.Update(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpDelete("{id:guid}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        if (!loggedInUser.IsAdmin) return Forbid();
        FolderClaim? m = _db.FolderClaims.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        _db.FolderClaims.Remove(m);
        _db.SaveChanges();
        return Ok();
    }
}