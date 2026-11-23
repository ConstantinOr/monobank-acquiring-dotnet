using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace MonoBank.Acquiring.Tests;

public class CryptographyTests
{
    [Theory]
    [InlineData(256)]
    [InlineData(384)]
    [InlineData(521)]
    public void Signatures_interoperate_with_native_DER_for_supported_curves(int keySize)
    {
        var curve = keySize switch
        {
            256 => ECCurve.NamedCurves.nistP256,
            384 => ECCurve.NamedCurves.nistP384,
            521 => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentOutOfRangeException(nameof(keySize)),
        };
        using var key = ECDsa.Create(curve);
        const string order = """{"amount":4200,"ccy":980}""";
        const string requestId = "req-1";
        var payload = Encoding.UTF8.GetBytes(order + requestId);
        var signature = Convert.FromBase64String(MonoPaySigner.Sign(order, requestId, key.ExportPkcs8PrivateKeyPem()));

        Assert.True(key.VerifyData(payload, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        Assert.Equal(0x30, signature[0]);
        if (keySize == 521)
        {
            Assert.Equal(0x81, signature[1]);
            Assert.Equal(signature.Length - 3, signature[2]);
        }
        else
        {
            Assert.Equal(signature.Length - 2, signature[1]);
        }

        var pubKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key.ExportSubjectPublicKeyInfoPem()));
        var nativeSignature = key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        Assert.True(WebhookVerifier.Verify(payload, Convert.ToBase64String(nativeSignature), pubKey));
        payload[0] ^= 1;
        Assert.False(WebhookVerifier.Verify(payload, Convert.ToBase64String(nativeSignature), pubKey));
    }

    [Theory]
    [InlineData("300602847FFFFFFF")]
    [InlineData("3006028480000000")]
    [InlineData("30060284FFFFFFFF")]
    [InlineData("30847FFFFFFF")]
    [InlineData("300402840000")]
    [InlineData("3006020301")]
    [InlineData("300402030102")]
    public void Verify_rejects_or_fails_invalid_ASN1_signatures(string signatureHex)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pubKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key.ExportSubjectPublicKeyInfoPem()));
        var signature = Convert.ToBase64String(Convert.FromHexString(signatureHex));

        try
        {
            Assert.False(WebhookVerifier.Verify(new byte[] { 1 }, signature, pubKey));
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            // netstandard2.0 DER parser / PEM import may throw on malformed input.
        }
    }
}
