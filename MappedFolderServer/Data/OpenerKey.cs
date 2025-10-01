using System.ComponentModel.DataAnnotations.Schema;

namespace MappedFolderServer.Data;

public class OpenerKey
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; }
    public Guid CreatedByUserId { get; init; }
    public Guid ModifiedByUserId { get; init; }
    public string Key { get; set; }
    public Guid OpensSlugId { get; set; }
}