using MappedFolderServer.Auth;

namespace MappedFolderServer.Data;

public class AuthenticatedSession
{
    public AuthenticatedSession()
    {
        SessionToken = RandomUtils.GenerateSessionToken();
    }
    public Guid Id { get; set; }
    public DateTime Expires { get; set; }
    public List<Mapping> ClaimedSlugs { get; set; } = [];
    public string SessionToken { get; set; }
}