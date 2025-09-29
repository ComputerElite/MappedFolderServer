using System.Security.Claims;
using MappedFolderServer.Data;

namespace MappedFolderServer.Auth;

public interface ICurrentUserService
{
    User? GetCurrentUser();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDatabaseContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDatabaseContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public User? GetCurrentUser()
    {
        var sub = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (sub == null) return null;
        return _db.Users.SingleOrDefault(u => u.OidcId == sub);
    }
}