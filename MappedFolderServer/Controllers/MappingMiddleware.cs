using System.Web;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace MappedFolderServer.Controllers;

[Route("slugs/{slug}/{*subpath}")]
public class TenantController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDatabaseContext _db;

    public TenantController(IWebHostEnvironment env, AppDatabaseContext db)
    {
        _env = env;
        _db = db;
    }

    [HttpGet]
    public IActionResult Get(string slug, string subpath)
    {
        // Look up UUID in the database
        Console.WriteLine(slug);
        var entry = _db.Mappings.FirstOrDefault(t => t.Slug == slug);
        if (entry == null) return NotFound();
        
        if (!entry.IsPublic && User.Claims.All(x => x.Type != "AdminUserId"))
        {
            if (!entry.PasswordSet)
            {
                // private
                return NotFound();
            }
            var unlocked = User.Claims.Any(c => c.Type == "UnlockedSlug" && c.Value == entry.Id.ToString());
            if (!unlocked)
            {
                // redirect to password prompt
                return Redirect($"/enter-password?slug={slug}&redirect_after={HttpUtility.UrlEncode($"/slugs/{slug}/{subpath}")}");
            }
        }

        // Resolve file path
        var basePath = entry.FolderPath; // e.g. "/data/assets/german"
        var fullPath = Path.GetFullPath(Path.Combine(basePath, subpath ?? "index.html"));
        if (subpath == null && !Request.Path.ToUriComponent().EndsWith("/"))
            return RedirectPermanent($"/slugs/{slug}/");
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