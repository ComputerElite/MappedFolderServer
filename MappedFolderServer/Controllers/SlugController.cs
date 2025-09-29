using System.Web;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using MappedFolderServer.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace MappedFolderServer.Controllers;

[Route("{slug:regex(^(?!slugs|password|forbidden|assets|api|slugs).*$)}/{*subpath}")]
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
    public IActionResult Get(string slug, string subpath)
    {
        Console.WriteLine(slug);
        if (!SlugChecker.IsSlugValid(slug)) return NotFound();
        // Look up UUID in the database
        var entry = _db.Mappings.FirstOrDefault(t => t.Slug == slug);
        if (entry == null) return NotFound();

        User? loggedInUser = _currentUser.GetCurrentUser();
        
        if (!entry.IsPublic && (loggedInUser == null || !entry.CanBeAccessedBy(loggedInUser))) // access check only if not public. If user is admin skip the check
        {
            if (!entry.PasswordSet)
            {
                // private
                return NotFound();
            }
            var unlocked = User.Claims.Any(c => c.Type == "UnlockedSlug" && c.Value == entry.Id.ToString());
            
            if (loggedInUser != null && entry.CreatedBy != null) unlocked = loggedInUser.Id == entry.CreatedBy;
            if (!unlocked)
            {
                // redirect to password prompt
                return Redirect($"/password?slug={slug}&redirect_after={HttpUtility.UrlEncode($"/{slug}/{subpath}")}");
            }
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