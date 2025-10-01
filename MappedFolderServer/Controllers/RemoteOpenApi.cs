using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MappedFolderServer.Auth;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MappedFolderServer.Controllers;

[Route("api/v1/remote")]
public class RemoteOpenApi : Controller
{
    private static Dictionary<string, string?> openDict = new();
    
    private ICurrentUserService _currentUser;

    public RemoteOpenApi(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpPost("send/{remoteId}")]
    [Authorize(AuthenticationSchemes = "oidc")]
    public IActionResult Send([FromRoute]string remoteId, [FromBody] RemoteWebsocketData data)
    {
        User? user = _currentUser.GetCurrentUser();
        if (user == null) return Forbid();
        if (!openDict.ContainsKey(remoteId))
        {
            return NotFound();
        }

        openDict[remoteId] = data.ToOpen;
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
    
    private static async Task HandleRemote(WebSocket webSocket)
    {
        string id;
        while(openDict.ContainsKey(id = Random.Shared.Next(10000).ToString().PadLeft(4, '0'))) {}
        // Generate Id and send to the client
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new RemoteWebsocketData(id))), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
        DateTime closeAt = DateTime.UtcNow.AddMinutes(5);
        openDict.Add(id, null);
        while (openDict.ContainsKey(id))
        {
            if (DateTime.UtcNow > closeAt)
            {
                break;
            }
            if (openDict[id] == null)
            {
                if (webSocket.State != WebSocketState.Open) break;
                await Task.Delay(50);
                continue;
            }

            if (webSocket.State != WebSocketState.Open) break;
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new RemoteWebsocketData(id, openDict[id]))), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
            break;
        }
        openDict.Remove(id);
        if (webSocket.State != WebSocketState.Open) return;
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

}