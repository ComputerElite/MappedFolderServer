using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
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

    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string[] bannedFolders = ["node_modules/", ".git/"];

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

        string sourceFolderPath = m.FolderPath;
        if (!Directory.Exists(sourceFolderPath))
            return NotFound("Folder not found.");

        using var memoryStream = new MemoryStream();
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);

        var filesToInclude =
            new HashSet<string>(Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories));
        var remoteAssets = new Dictionary<string, byte[]>(); // url -> file bytes

        // Parse HTML files and find assets
        foreach (var htmlFile in Directory.GetFiles(sourceFolderPath, "*.html", SearchOption.AllDirectories))
        {
            if (bannedFolders.Any(x => htmlFile.Contains(x))) continue;
            if (IsOutOfBounds( htmlFile, sourceFolderPath)) continue;
            Console.WriteLine($"Preprocessing {htmlFile}");
            string htmlContent = System.IO.File.ReadAllText(htmlFile);
            string htmlDir = Path.GetDirectoryName(htmlFile)!;
            var assetPaths = ExtractAssetPaths(htmlContent);

            foreach (var relPath in assetPaths)
            {
                if (relPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle remote files
                    if (includeRemote && !remoteAssets.ContainsKey(relPath))
                    {
                        try
                        {
                            Console.WriteLine($"Downloading {relPath}");
                            var data = await httpClient.GetByteArrayAsync(relPath);
                            remoteAssets[relPath] = data;
                        }
                        catch
                        {
                            // Skip failed downloads silently
                        }
                    }
                }
                else
                {
                    string absPath = Path.GetFullPath(Path.Combine(htmlDir, relPath));
                    if (System.IO.File.Exists(absPath))
                        filesToInclude.Add(absPath);
                }
            }
        }

        // Add all local files
        foreach (var filePath in filesToInclude)
        {
            if (bannedFolders.Any(x => filePath.Contains(x))) continue;
            if (IsOutOfBounds(filePath, sourceFolderPath)) continue;
            string entryName = Path.GetRelativePath(sourceFolderPath, filePath).Replace("\\", "/");
            Console.WriteLine("Zipping " + entryName);
            if (filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                string htmlContent = System.IO.File.ReadAllText(filePath);
                string fixedHtml = await FixHtmlLinksAsync(htmlContent, filePath, sourceFolderPath, includeRemote,
                    remoteAssets);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                using var entryStream = new StreamWriter(entry.Open(), Encoding.UTF8);
                entryStream.Write(fixedHtml);
            }
            else
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var fileStream = System.IO.File.OpenRead(filePath);
                await fileStream.CopyToAsync(entryStream);
            }
        }

        // Add downloaded remote files
        if (includeRemote)
        {
            foreach (var kvp in remoteAssets)
            {
                string url = kvp.Key;
                byte[] content = kvp.Value;

                // Generate a safe local filename
                string fileName = "remote/" + SanitizeFileName(url);
                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(content);
            }
        }
        archive.Dispose();
        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "application/zip", $"{m.Slug}.zip");
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