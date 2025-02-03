using System.Security.Cryptography;
using System.Text;

namespace ImmortalVault_Server.Utils;

public static class HashUtils
{
    public static string ComputeSHA256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}