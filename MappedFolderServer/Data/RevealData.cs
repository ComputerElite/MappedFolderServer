using System.ComponentModel.DataAnnotations.Schema;

namespace MappedFolderServer.Data;

public class RevealData
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public Guid ForSlug { get; set; }
    public string SlugName { get; set; }
    public Guid ForUser { get; set; }
    public string? RemoteUrl { get; set; }
    public DateTime Created { get; set; }
}