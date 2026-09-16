using System.Security.Cryptography;
using AegisCore.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests;

public sealed class JwtKeyMaterialTests
{
    [Fact]
    public void Generates_Once_Into_The_Configured_Paths_And_Never_Overwrites()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aegis-jwt-" + Guid.NewGuid().ToString("N"));
        var privatePath = Path.Combine(directory, "jwt-signing.key");
        var publicPath = Path.Combine(directory, "jwt-signing.pub");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [JwtKeyMaterial.PrivateKeyPathKey] = privatePath,
            [JwtKeyMaterial.PublicKeyPathKey] = publicPath,
        }).Build();
        try
        {
            Assert.Null(JwtKeyMaterial.PrivateKeyPem(configuration));
            Assert.True(JwtKeyMaterial.EnsureGenerated(configuration));
            var privatePem = JwtKeyMaterial.PrivateKeyPem(configuration)!;
            var publicPem = JwtKeyMaterial.PublicKeyPem(configuration)!;
            Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", privatePem, StringComparison.Ordinal);
            Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicPem, StringComparison.Ordinal);

            // The pair must actually verify against each other.
            using var signer = RSA.Create(); signer.ImportFromPem(privatePem);
            using var verifier = RSA.Create(); verifier.ImportFromPem(publicPem);
            var payload = "token"u8.ToArray();
            var signature = signer.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.True(verifier.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

            // A second start finds the files and leaves them alone.
            Assert.False(JwtKeyMaterial.EnsureGenerated(configuration));
            Assert.Equal(privatePem, JwtKeyMaterial.PrivateKeyPem(configuration));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void Inline_Pem_Takes_Precedence_Over_Paths()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [JwtKeyMaterial.PublicKeyKey] = "-----BEGIN PUBLIC KEY-----\ninline\n-----END PUBLIC KEY-----",
            [JwtKeyMaterial.PublicKeyPathKey] = Path.Combine(Path.GetTempPath(), "does-not-exist.pub"),
        }).Build();
        Assert.Contains("inline", JwtKeyMaterial.PublicKeyPem(configuration), StringComparison.Ordinal);
        Assert.False(JwtKeyMaterial.EnsureGenerated(configuration));
    }
}
