using System.Security.Cryptography;
using System.Text;

namespace MonoBank.Acquiring;

public static class MonoPaySigner
{
    public static string Sign(string orderJson, string requestId, string privateKeyPem)
    {
        Guard.NotNullOrWhiteSpace(orderJson);
        Guard.NotNull(requestId);
        Guard.NotNullOrWhiteSpace(privateKeyPem);

        var payload = Encoding.UTF8.GetBytes(orderJson + requestId);
        using var ecdsa = ECDsa.Create();
        EcdsaCompat.ImportPem(ecdsa, privateKeyPem);
        return Convert.ToBase64String(EcdsaCompat.SignDer(ecdsa, payload));
    }
}
