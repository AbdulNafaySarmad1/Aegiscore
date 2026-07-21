using System.Text;
using Konscious.Security.Cryptography;
using AegisCore.Application.Common.Interfaces;

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

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        return Convert.ToBase64String(argon2.GetBytes(HashLength));
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        var hashToCompare = Convert.ToBase64String(argon2.GetBytes(HashLength));
        return hashToCompare == hash;
    }
}
