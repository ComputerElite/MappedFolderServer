using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using MappedFolderServer.Scraping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/slugs")]
[Authorize("user")]
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
        if (!m.CanBeEditedBy(loggedInUser)) return Forbid();
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
        if (!m.CanBeEditedBy(loggedInUser)) return Forbid();
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
        if (!m.CanBeEditedBy(loggedInUser)) return Forbid();
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
        if (!m.CanBeEditedBy(loggedInUser)) return Forbid();
        m.PasswordHash = null;
        _db.Slugs.Update(m);
        _db.SaveChanges();
        return Ok();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download([FromRoute] Guid id)
    {
        if (!Config.Instance.DownloadFeatureEnabled)
        {
            return BadRequest("The download feature has been disabled by the server admin");
        }
        User? loggedInUser = _currentUser.GetCurrentUser();
        if (loggedInUser == null) return Unauthorized();
        SlugEntry? m = _db.Slugs.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        if (!m.CanBeEditedBy(loggedInUser)) return Forbid();

        bool includeRemote = true;
        
        IEnumerable<SlugEntry> allowedEntries = _db.Slugs.Where(x => x.IsPublic || x.CreatedBy == loggedInUser.Id || loggedInUser.IsAdmin);

        Scraper s = new Scraper(allowedEntries);
        return File(s.ScrapeSlug(m), "application/zip", $"{m.Slug}.zip");
    }

    private bool IsOutOfBounds(string filePath, string sourceFolderPath)
    {
        string fullPath = Path.GetFullPath(filePath, sourceFolderPath);
        if (!fullPath.StartsWith(sourceFolderPath)) return true;
        return false;
    }

    // --- Helper: Extract asset paths from HTML (both local + absolute) ---
    private static IEnumerable<string> ExtractAssetPaths(string html)
    {
        var assetPaths = new HashSet<string>();
        string pattern = @"(?:src|href)\s*=\s*[""']([^""'#?]+)[""']";
        foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase))
        {
            assetPaths.Add(match.Groups[1].Value);
        }

        return assetPaths;
    }

    // --- Helper: Fix HTML links ---
    private static async Task<string> FixHtmlLinksAsync(
        string html,
        string htmlFilePath,
        string baseFolder,
        bool includeRemote,
        Dictionary<string, byte[]> remoteAssets)
    {
        
        Console.WriteLine("Fixing " + htmlFilePath);
        string htmlDir = Path.GetDirectoryName(htmlFilePath)!;
        string pattern = @"((?:src|href)\s*=\s*[""'])([^""'#?]+)([""'])";

        return await Task.Run(() => Regex.Replace(html, pattern, match =>
        {
            string prefix = match.Groups[1].Value;
            string path = match.Groups[2].Value;
            string suffix = match.Groups[3].Value;

            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (includeRemote && remoteAssets.ContainsKey(path))
                {
                    string safeName = "remote/" + SanitizeFileName(path);
                    return $"{prefix}{safeName}{suffix}";
                }
                else
                {
                    // Leave original link as-is
                    return match.Value;
                }
            }
            else
            {
                string absPath = Path.GetFullPath(Path.Combine(htmlDir, path));
                if (!System.IO.File.Exists(absPath))
                    return match.Value;

                string relToBase = Path.GetRelativePath(baseFolder, absPath).Replace("\\", "/");
                return $"{prefix}{relToBase}{suffix}";
            }
        }, RegexOptions.IgnoreCase));
    }

    // --- Helper: Sanitize URLs into safe file names ---
    private static string SanitizeFileName(string url)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            url = url.Replace(c, '_');

        // Replace protocol and slashes
        return url.Replace("://", "_").Replace("/", "_");
    }
}