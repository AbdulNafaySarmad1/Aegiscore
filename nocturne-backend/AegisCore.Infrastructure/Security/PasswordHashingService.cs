using System.Text;
using Konscious.Security.Cryptography;
using AegisCore.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Service for hashing and verifying passwords using Argon2id with OWASP 2024 parameters.
/// </summary>
public class PasswordHashingService : IPasswordHashingService
{
    private const int DegreeOfParallelism = 4;
    private const int Iterations = 3;
    private const int MemorySize = 65536; // 64 MB
    private const int HashLength = 16;
    private const int SaltLength = 16;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = HashPassword(password, salt);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = HashPassword(password, salt);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize,
            Salt = salt
        };

        return argon2.GetBytes(HashLength);
    }
}
