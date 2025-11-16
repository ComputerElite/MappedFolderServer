using System.Security.Cryptography;
using System.Text;

namespace MappedFolderServer.Util;

public class TokenEncryptor
{
    private static readonly byte[] key = Encoding.UTF8.GetBytes(Config.Instance.EncryptionKey);

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // unique IV per encryption

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
            sw.Write(plainText);

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        // Extract the IV (first 16 bytes for AES)
        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipher, iv, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}