using System.ComponentModel.DataAnnotations.Schema;

namespace MappedFolderServer.Data;

public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; }

    public string OidcId { get; init; } = null!;
    public string Name { get; set; } = null!;
    public bool IsAdmin { get; set; } = false;
}