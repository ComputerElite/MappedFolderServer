using System.Net.WebSockets;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/remotes")]
public class RemoteOpenApi : Controller
{
    private ICurrentUserService _currentUser;
    private AppDatabaseContext _db;

    public RemoteOpenApi(ICurrentUserService currentUser, AppDatabaseContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    [HttpDelete("delete/{id}")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public async Task<IActionResult> Delete(string id)
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Unauthorized();
        RemoteOpenData? data = _db.RemoteOpenData.FirstOrDefault(x => x.Id == id);
        if (data == null) return NotFound();
        if (!data.CanBeAccessedBy(user)) return Forbid();
        _db.Remove(data);
        await _db.SaveChangesAsync();
        return Ok();    
    }
    
    [HttpPost("regenerate/{id}")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public async Task<IActionResult> Regenerate(string id)
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Unauthorized();
        RemoteOpenData? data = _db.RemoteOpenData.FirstOrDefault(x => x.Id == id);
        if (data == null) return NotFound();
        if (!data.CanBeAccessedBy(user)) return Forbid();
        string secret = RandomUtils.GenerateToken();
        // Store the hash in the database
        data.Secret = BCrypt.Net.BCrypt.HashPassword(secret);
        _db.Update(data);
        await _db.SaveChangesAsync();
        data.Secret = secret;
        // But send the secret back to the client
        return Ok(data);    
    }
    

    [HttpPost("new")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public async Task<IActionResult> Create([FromBody] RemoteOpenData data)
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Unauthorized();
        if (data.OpensSlugId != null)
        {
            SlugEntry? slug = _db.Slugs.FirstOrDefault(x => x.Id == data.OpensSlugId);
            if (slug == null) return NotFound();
            if(!slug.CanBeAccessedBy(user)) return Forbid();
        }
        RemoteOpenData d = new RemoteOpenData(Guid.NewGuid().ToString())
        {
            CreatedBy = user.Id,
            Expires = DateTime.MaxValue,
            Name = data.Name,
            Path = data.Path,
            OpensSlugId = data.OpensSlugId,
            Secret = RandomUtils.GenerateToken()
        };
        string secret = RandomUtils.GenerateToken();
        // Store the hash in the database
        d.Secret = BCrypt.Net.BCrypt.HashPassword(secret);
        _db.RemoteOpenData.Add(d);
        await _db.SaveChangesAsync();
        d.Secret = secret;
        // But send the secret back to the client
        return Ok(d);    
    }

    [HttpGet("all")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public async Task<IActionResult> All()
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Unauthorized();
        if (user.IsAdmin)
        {
            return Ok(_db.RemoteOpenData.ToList());
        }
        return Ok(_db.RemoteOpenData.Where(x => x.CreatedBy == user.Id).ToList());
    }

    [HttpGet("open/{id}")]
    public async Task<IActionResult> Open([FromQuery(Name = "secret")] string? secret, [FromRoute]string id)
    {
        RemoteOpenData? wsData = _db.RemoteOpenData.FirstOrDefault(x => x.Id == id);
        if (wsData == null) return Unauthorized();
        if (!BCrypt.Net.BCrypt.Verify(secret, wsData.Secret)) return Unauthorized();
        if (wsData.Expires < DateTime.UtcNow)
        {
            _db.Remove(wsData);
            await _db.SaveChangesAsync();
            return Unauthorized();
        }

        if (wsData.OpensSlugId == null)
        {
            return NotFound(
                "This RemoteOpenEntry isn't associated with any slug. Please update it from within the slug page");
        }
        SlugEntry? slug = _db.Slugs.FirstOrDefault(x => x.Id == wsData.OpensSlugId);
        if (slug == null) return NotFound();

        await HttpContext.SignInAsync("AppCookie", SlugAuthController.GetClaim(slug, "RemoteUnlockedSlug"));
        if(wsData.CreatedBy == null)
        {
            // Single use for remote. If it contains a user however we keep it for multi use
            _db.RemoteOpenData.Remove(wsData);
            await _db.SaveChangesAsync();
        }
        return Redirect($"/{slug.Slug}/{wsData.Path ?? ""}");
    }

    [HttpPost("edit/{remoteId}")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public IActionResult Send([FromRoute]string remoteId, [FromBody] RemoteOpenData data)
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Forbid();
        RemoteOpenData? remoteData = _db.RemoteOpenData.FirstOrDefault(x => x.Id == remoteId);
        if (remoteData == null)
        {
            return NotFound();
        }

        if (remoteData.CreatedBy != null && !remoteData.CanBeAccessedBy(user))
        {
            return Forbid();
        }

        if (data.OpensSlugId == null)
        {
            // revokes access to slugs. Aka parks a RemoteOpenEntry
            remoteData.OpensSlugId = null;
            _db.RemoteOpenData.Update(remoteData);
            _db.SaveChanges();
            return Ok();
        }

        SlugEntry? slug = _db.Slugs.FirstOrDefault(x => x.Id == data.OpensSlugId);
        if (slug == null)
        {
            return Forbid();
        }

        if (!slug.CanBeAccessedBy(user))
        {
            return Forbid();
        }

        remoteData.OpensSlugId = data.OpensSlugId;
        if (data.Name != null) remoteData.Name = data.Name;
        if (data.Path != null) remoteData.Path = data.Path;
        if (remoteData.CreatedBy == null)
        {
            // Only set a secret and update expiration if it's a temporary open request
            remoteData.Secret = Guid.NewGuid().ToString();
            remoteData.Expires = DateTime.UtcNow.AddMinutes(1);
        }
        _db.RemoteOpenData.Update(remoteData);
        _db.SaveChanges();
        return Ok();
    }
    
    
    [Route("ws")]
    public async Task RemoteWebsocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleRemote(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        
    }

    private string GenerateId()
    {
        return Random.Shared.Next(10000).ToString().PadLeft(4, '0');
    }
    
    
    private async Task HandleRemote(WebSocket webSocket)
    {
        await _db.RemoteOpenData.Where(x => x.Expires > DateTime.UtcNow).ExecuteDeleteAsync();
        string id = GenerateId();
        while (_db.RemoteOpenData.Any(x => x.Id == id))
        {
            id = GenerateId();
        }
        // Generate Id and send to the client
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new RemoteOpenData(id))), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
        RemoteOpenData data = new RemoteOpenData(id)
        {
            Expires = DateTime.UtcNow.AddMinutes(10)
        };
        _db.RemoteOpenData.Add(data);
        await _db.SaveChangesAsync();
        while (_db.RemoteOpenData.Any(x => x.Id == id))
        {
            await _db.Entry(data).ReloadAsync();
            if (DateTime.UtcNow > data.Expires)
            {
                _db.Remove(data);
                break;
            }

            if (data.OpensSlugId == null)
            {
                if (webSocket.State != WebSocketState.Open) break;
                await Task.Delay(50);
                continue;
            }

            if (webSocket.State != WebSocketState.Open) break;
            Console.WriteLine("Sending");
            if (data.Secret == null) break;
            string unhashedSecret = data.Secret;
            data.Secret = BCrypt.Net.BCrypt.HashPassword(data.Secret);
            await _db.SaveChangesAsync();
            data.Secret = unhashedSecret;
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
            break;
        }

        Console.WriteLine("Closing");
        if (webSocket.State != WebSocketState.Open) return;
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

}