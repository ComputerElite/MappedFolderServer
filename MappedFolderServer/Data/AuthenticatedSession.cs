using MappedFolderServer.Auth;

namespace MappedFolderServer.Data;

public class AuthenticatedSession
{
    public AuthenticatedSession()
    {
        SessionToken = RandomUtils.GenerateToken();
    }
    public Guid Id { get; set; }
    public DateTime Expires { get; set; }
    public List<SlugEntry> ClaimedSlugs { get; set; } = [];
    public string SessionToken { get; set; }
}