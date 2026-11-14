using System.Security.Cryptography;
using System.Text;

namespace MonoBank.Acquiring;

public static class WebhookVerifier
{
    public static bool Verify(ReadOnlySpan<byte> body, string xSignBase64, string pubKeyBase64)
    {
        Guard.NotNullOrWhiteSpace(xSignBase64);
        Guard.NotNullOrWhiteSpace(pubKeyBase64);

        byte[] pemBytes;
        byte[] signature;
        try
        {
            pemBytes = Convert.FromBase64String(pubKeyBase64);
            signature = Convert.FromBase64String(xSignBase64);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("X-Sign or public key is not valid base64.", ex);
        }

        using var ecdsa = ECDsa.Create();
        try
        {
            EcdsaCompat.ImportPem(ecdsa, Encoding.UTF8.GetString(pemBytes));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is CryptographicException)
        {
            throw new ArgumentException("Public key is not a PEM ECDSA key.", ex);
        }

        return EcdsaCompat.VerifyDer(ecdsa, body.ToArray(), signature);
    }
}
