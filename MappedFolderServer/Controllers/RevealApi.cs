using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/reveal")]
public class RevealApi : Controller
{
    private ICurrentUserService _currentUser;
    private AppDatabaseContext _db;

    public RevealApi(AppDatabaseContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("remotes")]
    [Authorize("user")]
    public IActionResult All([FromQuery] bool userOnly = false)
    {
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        if (loggedInUser.IsAdmin && !userOnly)
        {
            return Ok(_db.Reveal.OrderByDescending(x => x.Created).ToList());
        }
        return Ok(_db.Reveal.Where(x => x.ForUser == loggedInUser.Id).OrderByDescending(x => x.Created).ToList());
    }
    
    [HttpPost("remote/{slug}")]
    public async Task<IActionResult> PostRemote([FromRoute]string slug, [FromBody]RevealData data)
    {
        SlugEntry? s = _db.Slugs.FirstOrDefault(s => s.Slug == slug);
        if (s == null) return NotFound();
        // Check if poster has access to slug
        
        User? loggedInUser = _currentUser.GetCurrentUser();
        switch (s.AccessControl(User, loggedInUser))
        {
            case SlugEntry.SlugEntryAccessResult.AccessGranted:
                break;
            case SlugEntry.SlugEntryAccessResult.EmulateNotExisting:
                return NotFound();
            default:
                return Forbid();
        }

        RevealData revealData = new RevealData
        {
            Created = DateTime.UtcNow,
            SlugName = slug,
            ForSlug = s.Id,
            ForUser = s.CreatedBy,
            RemoteUrl = data.RemoteUrl
        };
        _db.Add(revealData);
        // Remote all old entries from db
        // Perhaps this should be moved somewhere else?
        DateTime deleteOlderThan = DateTime.UtcNow.AddDays(-1);
        await _db.Reveal.Where(x => x.Created < deleteOlderThan).ExecuteDeleteAsync();
        await _db.SaveChangesAsync();
        
        return Ok();
    }
}