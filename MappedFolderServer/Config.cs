using System.Text.Json;
using MappedFolderServer.Util;

namespace MappedFolderServer;

public class Config
{
    public static Config? Instance;

    public string DbConnectionString { get; set; } =
        "Data Source=Database.db;Cache=Shared";
    public string ServedDirectory { get; set; } = "/serve/";
    public string FrontendUrl { get; set; } = "http://192.168.178.24/";

    public void PopulateFromEnvironment()
    {
        EnvBinder.PopulateFromEnvironment(this);
        Instance = this;
    }
}