namespace MappedFolderServer.Data;

public class ApiError(string error)
{
    public string Error { get; set; } = error;
}