using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Data;

[Index(nameof(ForUserId))]
public class FolderClaim(string folderPath, Guid forUserId)
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FolderPath { get; set; } = folderPath;
    public Guid ForUserId { get; set; } = forUserId;
}