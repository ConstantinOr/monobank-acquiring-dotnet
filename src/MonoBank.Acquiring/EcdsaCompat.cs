using System.Security.Cryptography;
using System.Text;

namespace MonoBank.Acquiring;

internal static class EcdsaCompat
{
    public static void ImportPem(ECDsa ecdsa, string pem)
    {
#if NET5_0_OR_GREATER
        ecdsa.ImportFromPem(pem);
#else
        if (!TryDecodePem(pem, out var label, out var der))
            throw new ArgumentException("PEM payload is missing or invalid.");

        if (label.Equals("PUBLIC KEY", StringComparison.OrdinalIgnoreCase))
        {
            ecdsa.ImportParameters(ParseSubjectPublicKeyInfo(der));
            return;
        }

        if (label.Equals("PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            ecdsa.ImportParameters(ParsePkcs8PrivateKey(der));
            return;
        }

        if (label.Equals("EC PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            ecdsa.ImportParameters(ParseSec1PrivateKey(der, curveOid: null));
            return;
        }

        throw new ArgumentException($"Unsupported PEM label '{label}'.");
#endif
    }

    public static byte[] SignDer(ECDsa ecdsa, byte[] data)
    {
#if NET5_0_OR_GREATER
        return ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
#else
        var ieee = ecdsa.SignData(data, HashAlgorithmName.SHA256);
        return IeeeP1363ToDer(ieee);
#endif
    }

    public static bool VerifyDer(ECDsa ecdsa, byte[] data, byte[] derSignature)
    {
#if NET5_0_OR_GREATER
        return ecdsa.VerifyData(data, derSignature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
#else
        var fieldSize = FieldSizeBytes(ecdsa);
        var ieee = DerToIeeeP1363(derSignature, fieldSize);
        return ecdsa.VerifyData(data, ieee, HashAlgorithmName.SHA256);
#endif
    }

#if !NET5_0_OR_GREATER
    private static int FieldSizeBytes(ECDsa ecdsa)
    {
        var size = (ecdsa.KeySize + 7) / 8;
        return size <= 0 ? 32 : size;
    }

    private static bool TryDecodePem(string pem, out string label, out byte[] der)
    {
        label = "";
        der = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(pem))
            return false;

        var begin = pem.IndexOf("-----BEGIN ", StringComparison.Ordinal);
        if (begin < 0)
            return false;
        begin += "-----BEGIN ".Length;
        var labelEnd = pem.IndexOf("-----", begin, StringComparison.Ordinal);
        if (labelEnd < 0)
            return false;
        label = pem.Substring(begin, labelEnd - begin).Trim();

        var endMarker = "-----END " + label + "-----";
        var end = pem.IndexOf(endMarker, labelEnd, StringComparison.Ordinal);
        if (end < 0)
            return false;

        var base64 = pem.Substring(labelEnd + 5, end - (labelEnd + 5));
        var sb = new StringBuilder(base64.Length);
        foreach (var ch in base64)
        {
            if (!char.IsWhiteSpace(ch))
                sb.Append(ch);
        }

        try
        {
            der = Convert.FromBase64String(sb.ToString());
            return der.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ECParameters ParseSubjectPublicKeyInfo(byte[] der)
    {
        var reader = new AsnReader(der);
        reader.ReadSequence(out var spki);
        spki.ReadSequence(out var alg);
        var algOid = alg.ReadObjectIdentifier();
        if (algOid != "1.2.840.10045.2.1")
            throw new CryptographicException("Not an EC public key.");
        var curveOid = alg.ReadObjectIdentifier();
        alg.ThrowIfNotEmpty();

        var bits = spki.ReadBitString(out var unused);
        spki.ThrowIfNotEmpty();
        if (unused != 0 || bits.Length == 0 || bits[0] != 0x04)
            throw new CryptographicException("EC public key must be uncompressed.");

        var coord = (bits.Length - 1) / 2;
        var x = new byte[coord];
        var y = new byte[coord];
        Buffer.BlockCopy(bits, 1, x, 0, coord);
        Buffer.BlockCopy(bits, 1 + coord, y, 0, coord);

        return new ECParameters
        {
            Curve = CurveFromOid(curveOid),
            Q = new ECPoint { X = x, Y = y },
        };
    }

    private static ECParameters ParsePkcs8PrivateKey(byte[] der)
    {
        var reader = new AsnReader(der);
        reader.ReadSequence(out var pkcs8);
        var version = pkcs8.ReadIntegerBytes();
        if (version.Length != 1 || version[0] != 0)
            throw new CryptographicException("Unsupported PKCS#8 version.");

        pkcs8.ReadSequence(out var alg);
        var algOid = alg.ReadObjectIdentifier();
        if (algOid != "1.2.840.10045.2.1")
            throw new CryptographicException("Not an EC private key.");
        var curveOid = alg.ReadObjectIdentifier();
        alg.ThrowIfNotEmpty();

        var ecPrivate = pkcs8.ReadOctetString();
        // ignore optional attributes
        return ParseSec1PrivateKey(ecPrivate, curveOid);
    }

    private static ECParameters ParseSec1PrivateKey(byte[] der, string? curveOid)
    {
        var reader = new AsnReader(der);
        reader.ReadSequence(out var seq);
        var version = seq.ReadIntegerBytes();
        if (version.Length != 1 || version[0] != 1)
            throw new CryptographicException("Unsupported ECPrivateKey version.");

        var d = seq.ReadOctetString();
        string? oid = curveOid;
        byte[]? x = null;
        byte[]? y = null;

        while (!seq.IsEmpty)
        {
            var tag = seq.PeekTag();
            if (tag == 0xA0)
            {
                seq.ReadContext(0, out var curveParams);
                oid = curveParams.ReadObjectIdentifier();
                curveParams.ThrowIfNotEmpty();
            }
            else if (tag == 0xA1)
            {
                seq.ReadContext(1, out var pub);
                var bits = pub.ReadBitString(out var unused);
                pub.ThrowIfNotEmpty();
                if (unused != 0 || bits.Length == 0 || bits[0] != 0x04)
                    throw new CryptographicException("EC public key must be uncompressed.");
                var coord = (bits.Length - 1) / 2;
                x = new byte[coord];
                y = new byte[coord];
                Buffer.BlockCopy(bits, 1, x, 0, coord);
                Buffer.BlockCopy(bits, 1 + coord, y, 0, coord);
            }
            else
            {
                seq.Skip();
            }
        }

        if (oid is null)
            throw new CryptographicException("EC curve OID is missing.");

        var parameters = new ECParameters
        {
            Curve = CurveFromOid(oid),
            D = d,
        };
        if (x is not null && y is not null)
            parameters.Q = new ECPoint { X = x, Y = y };
        return parameters;
    }

    private static ECCurve CurveFromOid(string oid) =>
        oid switch
        {
            "1.2.840.10045.3.1.7" => ECCurve.NamedCurves.nistP256,
            "1.3.132.0.34" => ECCurve.NamedCurves.nistP384,
            "1.3.132.0.35" => ECCurve.NamedCurves.nistP521,
            _ => throw new CryptographicException($"Unsupported EC curve OID '{oid}'."),
        };

    private static byte[] DerToIeeeP1363(byte[] der, int fieldSize)
    {
        var reader = new AsnReader(der);
        reader.ReadSequence(out var seq);
        var r = seq.ReadIntegerBytes();
        var s = seq.ReadIntegerBytes();
        seq.ThrowIfNotEmpty();
        reader.ThrowIfNotEmpty();

        var ieee = new byte[checked(fieldSize * 2)];
        WriteUnsigned(r, ieee, 0, fieldSize);
        WriteUnsigned(s, ieee, fieldSize, fieldSize);
        return ieee;
    }

    private static byte[] IeeeP1363ToDer(byte[] ieee)
    {
        if (ieee.Length == 0 || ieee.Length % 2 != 0)
            throw new CryptographicException("Invalid IEEE P1363 signature length.");

        var field = ieee.Length / 2;
        var r = EncodeInteger(ieee, 0, field);
        var s = EncodeInteger(ieee, field, field);
        var contentLength = r.Length + s.Length;
        var der = new byte[2 + contentLength];
        der[0] = 0x30;
        der[1] = (byte)contentLength;
        Buffer.BlockCopy(r, 0, der, 2, r.Length);
        Buffer.BlockCopy(s, 0, der, 2 + r.Length, s.Length);
        return der;
    }

    private static void WriteUnsigned(byte[] integer, byte[] dest, int destOffset, int size)
    {
        var src = integer;
        var offset = 0;
        while (offset < src.Length - 1 && src[offset] == 0)
            offset++;

        var len = src.Length - offset;
        if (len > size)
            throw new CryptographicException("Integer is larger than the EC field size.");

        var pad = size - len;
        for (var i = 0; i < pad; i++)
            dest[destOffset + i] = 0;
        Buffer.BlockCopy(src, offset, dest, destOffset + pad, len);
    }

    private static byte[] EncodeInteger(byte[] ieee, int offset, int length)
    {
        var start = offset;
        var end = offset + length;
        while (start < end - 1 && ieee[start] == 0)
            start++;

        var needsPad = (ieee[start] & 0x80) != 0;
        var intLen = end - start + (needsPad ? 1 : 0);
        var result = new byte[2 + intLen];
        result[0] = 0x02;
        result[1] = (byte)intLen;
        var dest = 2;
        if (needsPad)
            result[dest++] = 0;
        Buffer.BlockCopy(ieee, start, result, dest, end - start);
        return result;
    }

    private sealed class AsnReader
    {
        private readonly byte[] _data;
        private int _offset;
        private readonly int _end;

        public AsnReader(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        private AsnReader(byte[] data, int offset, int end)
        {
            _data = data;
            _offset = offset;
            _end = end;
        }

        public bool IsEmpty => _offset >= _end;

        public byte PeekTag()
        {
            EnsureAvailable(1);
            return _data[_offset];
        }

        public void ReadSequence(out AsnReader content)
        {
            ReadTlv(0x30, out var start, out var length);
            content = new AsnReader(_data, start, start + length);
        }

        public void ReadContext(int number, out AsnReader content)
        {
            var expected = (byte)(0xA0 | number);
            ReadTlv(expected, out var start, out var length);
            content = new AsnReader(_data, start, start + length);
        }

        public byte[] ReadOctetString()
        {
            ReadTlv(0x04, out var start, out var length);
            return Slice(start, length);
        }

        public byte[] ReadIntegerBytes()
        {
            ReadTlv(0x02, out var start, out var length);
            return Slice(start, length);
        }

        public byte[] ReadBitString(out int unusedBits)
        {
            ReadTlv(0x03, out var start, out var length);
            if (length < 1)
                throw new CryptographicException("Invalid BIT STRING.");
            unusedBits = _data[start];
            return Slice(start + 1, length - 1);
        }

        public string ReadObjectIdentifier()
        {
            ReadTlv(0x06, out var start, out var length);
            if (length < 1)
                throw new CryptographicException("Invalid OID.");

            var first = _data[start];
            var sb = new StringBuilder();
            sb.Append(first / 40);
            sb.Append('.');
            sb.Append(first % 40);

            long value = 0;
            for (var i = 1; i < length; i++)
            {
                var b = _data[start + i];
                value = (value << 7) | (uint)(b & 0x7F);
                if ((b & 0x80) == 0)
                {
                    sb.Append('.');
                    sb.Append(value);
                    value = 0;
                }
            }

            return sb.ToString();
        }

        public void Skip()
        {
            ReadTlv(PeekTag(), out _, out _);
        }

        public void ThrowIfNotEmpty()
        {
            if (!IsEmpty)
                throw new CryptographicException("Unexpected trailing ASN.1 data.");
        }

        private void ReadTlv(byte expectedTag, out int contentStart, out int contentLength)
        {
            EnsureAvailable(2);
            var tag = _data[_offset++];
            if (tag != expectedTag)
                throw new CryptographicException($"Expected ASN.1 tag 0x{expectedTag:X2}, got 0x{tag:X2}.");

            var lengthByte = _data[_offset++];
            if ((lengthByte & 0x80) == 0)
            {
                contentLength = lengthByte;
            }
            else
            {
                var count = lengthByte & 0x7F;
                if (count == 0 || count > 4)
                    throw new CryptographicException("Invalid ASN.1 length.");
                EnsureAvailable(count);
                contentLength = 0;
                for (var i = 0; i < count; i++)
                    contentLength = (contentLength << 8) | _data[_offset++];
            }

            EnsureAvailable(contentLength);
            contentStart = _offset;
            _offset += contentLength;
        }

        private byte[] Slice(int start, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(_data, start, result, 0, length);
            return result;
        }

        private void EnsureAvailable(int count)
        {
            if (_offset + count > _end)
                throw new CryptographicException("Truncated ASN.1 data.");
        }
    }
#endif
}
