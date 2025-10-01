using System.Security.Cryptography;

namespace MappedFolderServer.Auth;

public class RandomUtils
{
    private static readonly char[] _chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
    public static string GenerateToken()
    {
        return RandomNumberGenerator.GetString(_chars, 300);
    }
}