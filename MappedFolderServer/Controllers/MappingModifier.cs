using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/slugs")]
[Authorize(Policy = "Admin")]
public class MappingModifier : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDatabaseContext _db;
    
    public MappingModifier(IWebHostEnvironment env, AppDatabaseContext db)
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
    public IActionResult Create([FromBody] Mapping mapping)
    {
        if (!Directory.Exists(mapping.FolderPath))
        {
            return BadRequest();
        }
        
        if (_db.Mappings.Any(x => x.Slug == mapping.Slug))
        {
            return BadRequest();
        }
        
        _db.Mappings.Add(mapping);
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost("{id:guid}")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] Mapping mapping)
    {
        Mapping? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        m.Slug = mapping.Slug;
        m.FolderPath = mapping.FolderPath;
        m.IsPublic = mapping.IsPublic;
        if (!Directory.Exists(mapping.FolderPath))
        {
            return BadRequest();
        }
        _db.Mappings.Update(m);
        _db.SaveChanges();
        return Ok();
    }
    
    [HttpPost("{id:guid}/password")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] LoginRequest request)
    {
        Mapping? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
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
        Mapping? m = _db.Mappings.FirstOrDefault(x => x.Id == id);
        if (m == null) return NotFound();
        m.PasswordHash = null;
        _db.Mappings.Update(m);
        _db.SaveChanges();
        return Ok();
    }
}