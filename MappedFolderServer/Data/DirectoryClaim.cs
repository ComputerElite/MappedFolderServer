namespace MappedFolderServer.Data;

public class FolderClaim(string folder, User u)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FolderPath { get; set; } = folder;
    public User User { get; set; } = u;
}