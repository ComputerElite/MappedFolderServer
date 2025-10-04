using System.Web;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using MappedFolderServer.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace MappedFolderServer.Controllers;

[Route("{slug:regex(^(?!slugs|password|forbidden|assets|api|slugs|reveal).*$)}/{*subpath}")]
public class SlugController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly AppDatabaseContext _db;

    public SlugController(ICurrentUserService currentUser, AppDatabaseContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    [HttpGet]
    public IActionResult Get(string slug, string subpath, [FromQuery(Name = "password")]string? password)
    {
        Console.WriteLine(slug);
        if (!SlugChecker.IsSlugValid(slug)) return NotFound();
        // Look up UUID in the database
        var entry = _db.Slugs.FirstOrDefault(t => t.Slug == slug);
        if (entry == null) return NotFound();

        User? loggedInUser = _currentUser.GetCurrentUser();

        switch (entry.AccessControl(User, loggedInUser, password, HttpContext))
        {
            case SlugEntry.SlugEntryAccessResult.AccessDenied:
                return Redirect($"/password?slug={slug}&redirect_after={HttpUtility.UrlEncode($"/{slug}/{subpath}")}");
            case SlugEntry.SlugEntryAccessResult.EmulateNotExisting:
                return NotFound();
            case SlugEntry.SlugEntryAccessResult.AccessGranted:
                break;
            default:
                return Forbid();
        }

        // Resolve file path
        var basePath = entry.FolderPath; // e.g. "/data/assets/german"
        var fullPath = Path.GetFullPath(Path.Combine(basePath, subpath ?? "index.html"));
        if (subpath == null && !Request.Path.ToUriComponent().EndsWith("/"))
            return RedirectPermanent($"/{slug}/");
        if (!fullPath.StartsWith(entry.FolderPath))
            return Forbid();

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(fullPath, contentType);
    }
}