using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Data;

[Index(nameof(Slug))]
//[Index(nameof(UserId))]
public class SlugEntry(string folderPath)
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public Guid? CreatedBy { get; set; }
    public string FolderPath { get; set; } = folderPath;
    public string Slug { get; set; } = Guid.NewGuid().ToString();
    public bool IsPublic { get; set; } = false;
    public bool PasswordSet => PasswordHash != null;
    [JsonIgnore]
    public string? PasswordHash { get; set; }
    [JsonIgnore]
    public string PasswordSalt { get; set; } = Guid.NewGuid().ToString();

    public bool CanBeAccessedBy(User loggedInUser)
    {
        return loggedInUser.IsAdmin || loggedInUser.Id == CreatedBy;
    }
}