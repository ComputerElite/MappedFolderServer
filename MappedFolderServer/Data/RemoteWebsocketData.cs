using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MappedFolderServer.Data;

public class RemoteWebsocketData(string id, string? secret = null)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;
    [JsonPropertyName("secret")]
    public string? Secret { get; set; } = secret;

    [JsonPropertyName("opensSlugId")]
    public Guid? OpensSlugId { get; set; } = null;
    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; } = DateTime.UtcNow.AddMinutes(10);

    public Guid? CreatedByUserId { get; set; } = null;
}