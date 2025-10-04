using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MappedFolderServer.Data;

public class RemoteOpenData(string id, string? secret = null)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("secret")]
    public string? Secret { get; set; } = secret;

    [JsonPropertyName("opensSlugId")]
    public Guid? OpensSlugId { get; set; } = null;

    [JsonPropertyName("path")]
    public string? Path { get; set; } = null;
    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; } = DateTime.UtcNow.AddMinutes(10);

    /// <summary>
    /// User who created the remote open entry. Null if temporary created by a client.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public Guid? CreatedBy { get; set; } = null;

    public bool CanBeAccessedBy(User loggedInUser)
    {
        return loggedInUser.IsAdmin || loggedInUser.Id == CreatedBy;
    }
}