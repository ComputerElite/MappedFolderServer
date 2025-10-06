using System.Text.Json;
using MappedFolderServer.Util;

namespace MappedFolderServer;

public class Config
{
    public static Config? Instance;

    public string DbConnectionString
    {
        get => "Data Source=" + DbFilePath + ";Cache=Shared";
    }

    public string DbFilePath { get; set; } =
        "Database.db";

    public bool DownloadFeatureEnabled { get; set; } = false;
    public string FrontendUrl { get; set; } = "http://192.168.178.24/";

    public bool UseOAuth { get; set; } = false;
    public string OAuthAuthority { get; set; } = "";
    public string OAuthClientId { get; set; } = "";
    public string OAuthClientSecret { get; set; } = "";
    
    public string OAuthAdminRole { get; set; } = "mfs-admin";
    
    
    // FOR TESTING ONLY
    // Disabled when UseOAuth is true
    public string AdminUser { get; set; } = "admin";
    public string AdminPassword { get; set; } = "admin";

    public static void GetFromEnvironment()
    {
        Instance = new Config();
        EnvBinder.PopulateFromEnvironment(Instance);
    }
}