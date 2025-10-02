using System.Net.WebSockets;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Controllers;

[Route("api/v1/remote")]
public class RemoteOpenApi : Controller
{
    private ICurrentUserService _currentUser;
    private AppDatabaseContext _db;

    public RemoteOpenApi(ICurrentUserService currentUser, AppDatabaseContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    [Authorize("oidc")]
    public async Task<IActionResult> All()
    {
        User? user = _currentUser.GetCurrentUser();
        return Ok();
    }

    [HttpGet("open/{id}")]
    public async Task<IActionResult> Open([FromQuery(Name = "secret")] string? secret, [FromRoute]string id)
    {
        RemoteWebsocketData? wsData = _db.RemoteWebsocketData.FirstOrDefault(x => x.Id == id);
        if (wsData == null) return Unauthorized();
        if (!BCrypt.Net.BCrypt.Verify(secret, wsData.Secret)) return Unauthorized();
        if (wsData.Expires < DateTime.UtcNow)
        {
            _db.Remove(wsData);
            await _db.SaveChangesAsync();
            return Unauthorized();
        }
        SlugEntry? slug = _db.Mappings.FirstOrDefault(x => x.Id == wsData.OpensSlugId);
        if (slug == null) return NotFound();

        await HttpContext.SignInAsync("AppCookie", SlugAuthController.GetClaim(slug, "RemoteUnlockedSlug"));
        if(wsData.CreatedByUserId == null)
        {
            // Single use for remote. If it contains a user however we keep it for multi use
            _db.RemoteWebsocketData.Remove(wsData);
            await _db.SaveChangesAsync();
        }
        return Redirect($"/{slug.Slug}");
    }

    [HttpPost("send/{remoteId}")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public IActionResult Send([FromRoute]string remoteId, [FromBody] RemoteWebsocketData data)
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Forbid();
        RemoteWebsocketData? remoteData = _db.RemoteWebsocketData.FirstOrDefault(x => x.Id == remoteId);
        if (remoteData == null)
        {
            return NotFound();
        }

        SlugEntry? slug = _db.Mappings.FirstOrDefault(x => x.Id == data.OpensSlugId);
        if (slug == null)
        {
            return Forbid();
        }

        if (!slug.CanBeAccessedBy(user))
        {
            return Forbid();
        }

        remoteData.OpensSlugId = data.OpensSlugId;
        remoteData.Secret = Guid.NewGuid().ToString();
        remoteData.Expires = DateTime.UtcNow.AddMinutes(1);
        _db.RemoteWebsocketData.Update(remoteData);
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

    private string GetId()
    {
        return Random.Shared.Next(10000).ToString().PadLeft(4, '0');
    }
    
    
    private async Task HandleRemote(WebSocket webSocket)
    {
        await _db.RemoteWebsocketData.Where(x => x.Expires > DateTime.UtcNow).ExecuteDeleteAsync();
        string id = GetId();
        while (_db.RemoteWebsocketData.Any(x => x.Id == id))
        {
            id = GetId();
        }
        // Generate Id and send to the client
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new RemoteWebsocketData(id))), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
        RemoteWebsocketData data = new RemoteWebsocketData(id)
        {
            Expires = DateTime.UtcNow.AddMinutes(10)
        };
        _db.RemoteWebsocketData.Add(data);
        await _db.SaveChangesAsync();
        while (_db.RemoteWebsocketData.Any(x => x.Id == id))
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