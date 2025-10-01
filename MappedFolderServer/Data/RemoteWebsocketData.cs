using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MappedFolderServer.Data;

public class RemoteWebsocketData(string id, string? openSecret = null)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;
    [JsonPropertyName("openSecret")]
    public string? OpenSecret { get; set; } = openSecret;

    [JsonPropertyName("opensSlugId")]
    public Guid? OpensSlugId { get; set; } = null;
    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; } = DateTime.UtcNow.AddMinutes(10);
}