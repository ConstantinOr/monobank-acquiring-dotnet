using System.Security.Cryptography;
using System.Text;

namespace MonoBank.Acquiring;

public static class MonoPaySigner
{
    public static string Sign(string orderJson, string requestId, string privateKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderJson);
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);

        var payload = Encoding.UTF8.GetBytes(orderJson + requestId);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);
        var der = ecdsa.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return Convert.ToBase64String(der);
    }
}
