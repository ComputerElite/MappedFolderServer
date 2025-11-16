using System.ComponentModel.DataAnnotations.Schema;

namespace MappedFolderServer.Data;

public class GitRepo(string url)
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Url { get; set; } = url;
    public string Branch { get; set; } = "main";
    public string CurrentCommitHash { set; get; }
    public DateTime LastPulled { get; set; }
    public string? Username { get; set; }
    public string? EncryptedPassword { get; set; }
}