using System.Security.Cryptography;
using System.Text;

namespace MonoBank.Acquiring;

public static class WebhookVerifier
{
    public static bool Verify(ReadOnlySpan<byte> body, string xSignBase64, string pubKeyBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xSignBase64);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubKeyBase64);

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
            ecdsa.ImportFromPem(Encoding.UTF8.GetString(pemBytes));
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new ArgumentException("Public key is not a PEM ECDSA key.", ex);
        }

        return ecdsa.VerifyData(body, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }
}
