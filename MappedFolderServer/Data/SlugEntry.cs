using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using MappedFolderServer.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace MappedFolderServer.Data;

[Index(nameof(Slug))]
//[Index(nameof(UserId))]
public class SlugEntry(string folderPath)
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid EditedBy { get; set; }

    public string FolderPath { get; set; } = folderPath;
    public string Slug { get; set; } = Guid.NewGuid().ToString();

    [NotMapped]
    public string displayName
    {
        get
        {
            if (Regex.IsMatch(Slug, @"^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$"))
            {
                return Path.GetFileName(FolderPath);
            }
            return Slug;
        }
    }

    public bool IsPublic { get; set; } = false;
    public bool PasswordSet => PasswordHash != null;
    [JsonIgnore]
    public string? PasswordHash { get; set; }
    [JsonIgnore]
    public string PasswordSalt { get; set; } = Guid.NewGuid().ToString();


    public bool CanBeEditedBy(User loggedInUser)
    {
        return loggedInUser.IsAdmin || loggedInUser.Id == CreatedBy;
    }

    public enum SlugEntryAccessResult
    {
        AccessGranted,
        AccessDenied,
        EmulateNotExisting
    }

    public SlugEntryAccessResult AccessControl(ClaimsPrincipal user, User? loggedInUser, string? providedPassword = null, HttpContext? httpContext = null)
    {
        if (IsPublic) return SlugEntryAccessResult.AccessGranted;
        if (loggedInUser != null && CanBeEditedBy(loggedInUser)) return SlugEntryAccessResult.AccessGranted;
        // access check only if not public. If user is admin skip the check
        var privateUnlocked = user.AlwaysHasAccessToSlug(this);
        if (!PasswordSet && !privateUnlocked)
        {
            // Slug is private and wasn't unlocked privately
            return SlugEntryAccessResult.EmulateNotExisting;
        }
        var unlocked = user.HasAccessToSlug(this) || privateUnlocked;
        if (unlocked) return SlugEntryAccessResult.AccessGranted;
        // check for password in query string
        ClaimsPrincipal? principal = null;
        if (providedPassword != null)
        {
            principal = SlugAuthController.ConfirmPassword(this, providedPassword);
        }

        if (principal == null)
        {
            // redirect to password prompt
            return SlugEntryAccessResult.AccessDenied;
        }
        if(httpContext != null)
        {
            // Sign in the user
            httpContext.SignInAsync("AppCookie", principal);
        }

        return SlugEntryAccessResult.AccessGranted;
    }
}