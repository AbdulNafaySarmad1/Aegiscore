using System.Runtime.CompilerServices;
using AegisCore.Infrastructure.Security.FieldEncryption;

namespace AegisCore.Tests.Infrastructure;

/// <summary>Installs the fixed column-encryption key before any test runs, whichever test runs first.</summary>
internal static class TestKeys
{
    public static readonly string MasterKeyBase64 = Convert.ToBase64String(Enumerable.Range(1, 32).Select(v => (byte)v).ToArray());

    [ModuleInitializer]
    internal static void Initialize() => FieldCipher.Configure(MasterKeyBase64);
}
