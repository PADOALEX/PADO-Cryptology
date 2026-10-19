using System.Security.Cryptography;
using System.Text;
using Cryptology.Security;
using Xunit;

namespace Cryptology.Security.Tests;

public sealed class ByteOneTimePadTests
{
    [Fact]
    public void OriginalAsciiControlVectorIsPreservedAtByteLevel()
    {
        // Captured from unmodified tab5.py before migration; see docs/audit/security-original-vectors.json.
        Assert.Equal(new byte[] { 25, 27, 25 }, ByteOneTimePad.Transform("ABC"u8, "XYZ"u8));
        Assert.Equal("GRsZ", ByteOneTimePad.EncryptToBase64("ABC", "WFla"));
        Assert.Equal("ABC", ByteOneTimePad.DecryptFromBase64("GRsZ", "WFla"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Hello, world!\nTabs\tand null\0")]
    [InlineData("Ґанок, Єнот, Іван та Їжак 🧪🔐")]
    [InlineData("English + Українська + 日本語 + e\u0301")]
    public void StrictUtf8TextRoundTripsWithBothKeyFormats(string text)
    {
        byte[] key = ByteOneTimePad.GenerateKeyForText(text);
        try
        {
            Assert.Equal(Encoding.UTF8.GetByteCount(text), key.Length);
            foreach (KeyEncoding format in Enum.GetValues<KeyEncoding>())
            {
                string encoded = ByteOneTimePad.EncodeKey(key, format);
                Assert.Equal(key, ByteOneTimePad.DecodeKey(encoded, format));
                string cipher = ByteOneTimePad.EncryptToBase64(text, encoded, format);
                Assert.Equal(key.Length, Convert.FromBase64String(cipher).Length);
                Assert.Equal(text, ByteOneTimePad.DecryptFromBase64(cipher, encoded, format));
            }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact]
    public void AllByteValuesXorTwiceToOriginal()
    {
        byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        byte[] key = Enumerable.Range(0, 256).Select(i => (byte)(255 - i)).ToArray();
        byte[] cipher = ByteOneTimePad.Transform(data, key);
        Assert.All(cipher, value => Assert.Equal((byte)255, value));
        Assert.Equal(data, ByteOneTimePad.Transform(cipher, key));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void BothShortAndLongKeysAreRejected(int keyLength)
    {
        string key = Convert.ToBase64String(new byte[keyLength]);
        // Ї is one Unicode scalar but two UTF-8 bytes.
        Assert.Throws<ArgumentException>(() => ByteOneTimePad.EncryptToBase64("Ї", key));
        Assert.Throws<ArgumentException>(() => ByteOneTimePad.DecryptFromBase64("AAA=", key));
    }

    [Fact]
    public void InvalidUtf8AfterDecryptionIsRejected()
    {
        Assert.Throws<DecoderFallbackException>(() => ByteOneTimePad.DecryptFromBase64("/w==", "AA=="));
    }

    [Fact]
    public void InvalidUtf16InputIsRejectedInsteadOfSilentlyReplaced()
    {
        string invalid = new('\uD800', 1);
        Assert.Throws<EncoderFallbackException>(() => ByteOneTimePad.GenerateKeyForText(invalid));
        Assert.Throws<EncoderFallbackException>(() => ByteOneTimePad.EncryptToBase64(invalid, "AA=="));
    }

    [Theory]
    [InlineData("!")]
    [InlineData("AA")]
    [InlineData("=AAA")]
    public void MalformedBase64IsRejected(string value)
    {
        Assert.Throws<FormatException>(() => ByteOneTimePad.EncryptToBase64("a", value));
        Assert.Throws<FormatException>(() => ByteOneTimePad.DecryptFromBase64(value, "AA=="));
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("0")]
    [InlineData("GG")]
    public void MalformedHexKeysAreRejected(string value) =>
        Assert.Throws<FormatException>(() => ByteOneTimePad.DecodeKey(value, KeyEncoding.Hex));

    [Fact]
    public void XorDoesNotAuthenticateData()
    {
        string cipher = ByteOneTimePad.EncryptToBase64("ABC", "AAAA");
        byte[] changed = Convert.FromBase64String(cipher);
        changed[0] ^= 1;
        Assert.Equal("@BC", ByteOneTimePad.DecryptFromBase64(Convert.ToBase64String(changed), "AAAA"));
    }

    [Fact]
    public void NegativeKeySizeIsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => ByteOneTimePad.GenerateKey(-1));
}
