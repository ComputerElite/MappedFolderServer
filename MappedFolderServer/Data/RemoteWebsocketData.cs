using System.Text.Json.Serialization;

namespace MappedFolderServer.Data;

public class RemoteWebsocketData(string id, string? toOpen = null)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;
    [JsonPropertyName("toOpen")]
    public string? ToOpen { get; set; } = toOpen;
}