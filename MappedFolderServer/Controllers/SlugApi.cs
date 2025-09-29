using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/slugs")]
[Authorize(Policy = "Admin")]
public class SlugApi : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDatabaseContext _db;
    
    public SlugApi(IWebHostEnvironment env, AppDatabaseContext db)
    {
        _env = env;
        _db = db;
    }

    [HttpGet("all")]
    public IActionResult GetAll()
    {
        return Ok(_db.Mappings.ToList());
    }

    [HttpPost]
    public IActionResult Create([FromBody] SlugEntry slugEntry)
    {
        if (!Directory.Exists(slugEntry.FolderPath))
        {
            return BadRequest();
        }
        
        if (_db.Mappings.Any(x => x.Slug == slugEntry.Slug))
        {
            return BadRequest();
        }
        
        _db.Mappings.Add(slugEntry);
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost("{id:guid}")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] SlugEntry slugEntry)
    {
        SlugEntry? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        m.Slug = slugEntry.Slug;
        m.FolderPath = slugEntry.FolderPath;
        m.IsPublic = slugEntry.IsPublic;
        if (!Directory.Exists(slugEntry.FolderPath))
        {
            return BadRequest();
        }
        _db.Mappings.Update(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpDelete("{id:guid}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        SlugEntry? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        _db.Mappings.Remove(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpPost("{id:guid}/password")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] LoginRequest request)
    {
        SlugEntry? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        m.PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt();
        m.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, m.PasswordSalt);
        _db.Mappings.Update(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpDelete("{id:guid}/password")]
    public IActionResult Update([FromRoute] Guid id)
    {
        SlugEntry? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        m.PasswordHash = null;
        _db.Mappings.Update(m);
        _db.SaveChanges();
        return Ok();
    }
}