using System.Security.Cryptography;
using System.Text;
using Cryptology.Security;
using Xunit;

namespace Cryptology.Security.Tests;

public sealed class RsaFixture : IDisposable
{
    public ModernRsaService Key { get; } = ModernRsaService.Generate(2048);
    public ModernRsaService OtherKey { get; } = ModernRsaService.Generate(2048);
    public void Dispose() { Key.Dispose(); OtherKey.Dispose(); }
}

public sealed class ModernRsaTests(RsaFixture fixture) : IClassFixture<RsaFixture>
{
    [Fact]
    public void DefaultKeyGenerationUses3072Bits()
    {
        using var key = ModernRsaService.Generate();
        Assert.Equal(3072, key.KeySize);
        Assert.Equal(318, key.MaxPlaintextBytes);
        Assert.True(key.HasPrivateKey);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(2047)]
    [InlineData(2050)]
    [InlineData(16384)]
    public void InvalidKeySizesAreRejected(int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ModernRsaService.Generate(size));

    [Theory]
    [InlineData("")]
    [InlineData("Short message")]
    [InlineData("Ґ Є І Ї — Україна 🧪")]
    public void OaepTextRoundTripsAndPublicKeyEncrypts(string text)
    {
        using var publicKey = ModernRsaService.ImportPublicPem(fixture.Key.ExportPublicPem());
        Assert.False(publicKey.HasPrivateKey);
        string ciphertext = publicKey.EncryptUtf8ToBase64(text);
        Assert.Equal(text, fixture.Key.DecryptUtf8FromBase64(ciphertext));
    }

    [Fact]
    public void OaepByteBoundaryIsCheckedBeforeEncryption()
    {
        Assert.Equal(190, fixture.Key.MaxPlaintextBytes);
        string exact = new('a', fixture.Key.MaxPlaintextBytes);
        Assert.Equal(exact, fixture.Key.DecryptUtf8FromBase64(fixture.Key.EncryptUtf8ToBase64(exact)));
        Assert.Throws<ArgumentException>(() => fixture.Key.EncryptUtf8ToBase64(exact + "a"));
        Assert.Throws<ArgumentException>(() => fixture.Key.EncryptUtf8ToBase64(new string('Ї', 96)));
    }

    [Fact]
    public void OaepIsRandomizedAndInteroperableWithDotnet()
    {
        string first = fixture.Key.EncryptUtf8ToBase64("example");
        string second = fixture.Key.EncryptUtf8ToBase64("example");
        Assert.NotEqual(first, second);
        using RSA publicKey = RSA.Create();
        publicKey.ImportFromPem(fixture.Key.ExportPublicPem());
        byte[] fromDotnet = publicKey.Encrypt("example"u8.ToArray(), RSAEncryptionPadding.OaepSHA256);
        Assert.Equal("example", fixture.Key.DecryptUtf8FromBase64(Convert.ToBase64String(fromDotnet)));
    }

    [Fact]
    public void OaepRejectsWrongKeyAndDamagedCiphertext()
    {
        string ciphertext = fixture.Key.EncryptUtf8ToBase64("secret");
        // Platform RSA providers may throw a derived exception (for example RSAOpenSsl).
        Assert.ThrowsAny<CryptographicException>(() => fixture.OtherKey.DecryptUtf8FromBase64(ciphertext));
        byte[] damaged = Convert.FromBase64String(ciphertext);
        damaged[damaged.Length / 2] ^= 0x80;
        Assert.ThrowsAny<CryptographicException>(() => fixture.Key.DecryptUtf8FromBase64(Convert.ToBase64String(damaged)));
        Assert.Throws<CryptographicException>(() => fixture.Key.DecryptUtf8FromBase64("AA=="));
        Assert.Throws<FormatException>(() => fixture.Key.DecryptUtf8FromBase64("malformed!"));
        Assert.Throws<FormatException>(() => fixture.Key.DecryptUtf8FromBase64(new string('A', 10_000)));
    }

    [Fact]
    public void PssTextSignatureUsesSha256AndRejectsTampering()
    {
        const string text = "Підписаний документ 🧪";
        string signature = fixture.Key.SignUtf8ToBase64(text);
        using var publicKey = ModernRsaService.ImportPublicPem(fixture.Key.ExportPublicPem());
        Assert.True(publicKey.VerifyUtf8FromBase64(text, signature));
        Assert.False(publicKey.VerifyUtf8FromBase64(text + "!", signature));
        Assert.False(fixture.OtherKey.VerifyUtf8FromBase64(text, signature));
        byte[] damaged = Convert.FromBase64String(signature);
        damaged[1] ^= 1;
        Assert.False(publicKey.VerifyUtf8FromBase64(text, Convert.ToBase64String(damaged)));
        using RSA reference = RSA.Create();
        reference.ImportFromPem(fixture.Key.ExportPublicPem());
        Assert.True(reference.VerifyData(Encoding.UTF8.GetBytes(text), Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64")]
    [InlineData("AA==")]
    [InlineData("=A==")]
    public void MalformedSignaturesReturnFalse(string signature) =>
        Assert.False(fixture.Key.VerifyUtf8FromBase64("content", signature));

    [Fact]
    public void OversizedSignatureReturnsFalse() =>
        Assert.False(fixture.Key.VerifyUtf8FromBase64("content", new string('A', 100_000)));

    [Fact]
    public void PssIsRandomized()
    {
        string first = fixture.Key.SignUtf8ToBase64("same text");
        string second = fixture.Key.SignUtf8ToBase64("same text");
        Assert.NotEqual(first, second);
        Assert.True(fixture.Key.VerifyUtf8FromBase64("same text", first));
        Assert.True(fixture.Key.VerifyUtf8FromBase64("same text", second));
    }

    [Fact]
    public async Task FileSignaturesCoverExactBytesIncludingEmptyFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            string emptySignature = await fixture.Key.SignFileAsync(path);
            Assert.True(await fixture.Key.VerifyFileAsync(path, emptySignature));
            Assert.True(fixture.Key.VerifyBytesFromBase64([], emptySignature));
            await File.WriteAllBytesAsync(path, [0, 1, 2, 255, 0, 13, 10]);
            Assert.False(await fixture.Key.VerifyFileAsync(path, emptySignature));
            string signature = await fixture.Key.SignFileAsync(path);
            using var publicKey = ModernRsaService.ImportPublicPem(fixture.Key.ExportPublicPem());
            Assert.True(await publicKey.VerifyFileAsync(path, signature));
            Assert.False(await fixture.OtherKey.VerifyFileAsync(path, signature));
            Assert.False(await publicKey.VerifyFileAsync(path, "bad!"));
            await File.WriteAllBytesAsync(path, [0, 1, 2, 255, 0, 13]);
            Assert.False(await publicKey.VerifyFileAsync(path, signature));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task FileSignatureStreamsLargeFilesAndMatchesByteVerification()
    {
        string path = Path.GetTempFileName();
        byte[] data = new byte[5 * 1024 * 1024 + 17];
        for (int i = 0; i < data.Length; i++) data[i] = (byte)(i % 251);
        try
        {
            await File.WriteAllBytesAsync(path, data);
            string signature = await fixture.Key.SignFileAsync(path);
            Assert.True(fixture.Key.VerifyBytesFromBase64(data, signature));
            Assert.True(await fixture.Key.VerifyFileAsync(path, signature));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task FileOperationsHonorCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Key.SignFileAsync("unused", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Key.VerifyFileAsync("unused", "AA==", cancellation.Token));
    }

    [Fact]
    public void PrivateOperationsRequirePrivateKey()
    {
        using var publicKey = ModernRsaService.ImportPublicPem(fixture.Key.ExportPublicPem());
        Assert.Throws<InvalidOperationException>(() => publicKey.SignUtf8ToBase64("text"));
        Assert.Throws<InvalidOperationException>(() => publicKey.ExportEncryptedPrivatePem("a long password", true));
        string cipher = fixture.Key.EncryptUtf8ToBase64("text");
        Assert.Throws<InvalidOperationException>(() => publicKey.DecryptUtf8FromBase64(cipher));
    }

    [Fact]
    public void PrivateExportRequiresConsentAndStrongEnoughPassword()
    {
        Assert.Throws<InvalidOperationException>(() => fixture.Key.ExportEncryptedPrivatePem("a long password", false));
        Assert.Throws<ArgumentException>(() => fixture.Key.ExportEncryptedPrivatePem("short", true));
    }

    [Fact]
    public async Task EncryptedPrivatePemRoundTripsAndWrongPasswordFails()
    {
        const string password = "test-only passphrase 012345";
        string pem = fixture.Key.ExportEncryptedPrivatePem(password, true);
        Assert.StartsWith("-----BEGIN ENCRYPTED PRIVATE KEY-----", pem);
        using var restored = ModernRsaService.ImportEncryptedPrivatePem(pem, password);
        Assert.True(restored.HasPrivateKey);
        Assert.Equal(fixture.Key.ExportPublicPem(), restored.ExportPublicPem());
        string ciphertext = fixture.Key.EncryptUtf8ToBase64("restored key");
        Assert.Equal("restored key", restored.DecryptUtf8FromBase64(ciphertext));
        Assert.ThrowsAny<CryptographicException>(() => ModernRsaService.ImportEncryptedPrivatePem(pem, "wrong password"));
        string path = Path.GetTempFileName();
        string publicPath = Path.GetTempFileName();
        string documentPath = Path.GetTempFileName();
        string signaturePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, pem);
            using var fromFile = await ModernRsaService.ImportEncryptedPrivatePemFileAsync(path, password);
            Assert.Equal(restored.ExportPublicPem(), fromFile.ExportPublicPem());
            // Full detached-file workflow: exported keys -> imported signer/verifier -> saved signature.
            await File.WriteAllTextAsync(publicPath, fromFile.ExportPublicPem());
            await File.WriteAllBytesAsync(documentPath, [0, 13, 10, 255, 128, 42]);
            await File.WriteAllTextAsync(signaturePath, await fromFile.SignFileAsync(documentPath));
            using var verifier = await ModernRsaService.ImportPublicPemFileAsync(publicPath);
            string detachedSignature = await File.ReadAllTextAsync(signaturePath);
            Assert.True(await verifier.VerifyFileAsync(documentPath, detachedSignature));
            await File.AppendAllTextAsync(documentPath, "tampered");
            Assert.False(await verifier.VerifyFileAsync(documentPath, detachedSignature));
        }
        finally
        {
            File.Delete(path);
            File.Delete(publicPath);
            File.Delete(documentPath);
            File.Delete(signaturePath);
        }
    }

    [Fact]
    public void StandardPrivatePemFormatsCanBeImported()
    {
        using RSA key = RSA.Create(2048);
        foreach (string pem in new[] { key.ExportPkcs8PrivateKeyPem(), key.ExportRSAPrivateKeyPem() })
        {
            using var restored = ModernRsaService.ImportPrivatePem(pem);
            Assert.Equal(key.ExportSubjectPublicKeyInfoPem(), restored.ExportPublicPem());
            Assert.True(restored.VerifyUtf8FromBase64("test", restored.SignUtf8ToBase64("test")));
            Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPublicPem(pem));
        }
    }

    [Fact]
    public void WeakImportedKeysAreRejected()
    {
        using RSA weak = RSA.Create(1024);
        Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPublicPem(weak.ExportSubjectPublicKeyInfoPem()));
        Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPrivatePem(weak.ExportPkcs8PrivateKeyPem()));
    }

    [Fact]
    public void InvalidAmbiguousAndOversizedPemAreRejected()
    {
        Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPublicPem("no key"));
        Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPublicPem(new string('A', ModernRsaService.MaximumPemLength + 1)));
        Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPublicPem(fixture.Key.ExportPublicPem() + "\n" + fixture.OtherKey.ExportPublicPem()));
        Assert.Throws<ArgumentException>(() => ModernRsaService.ImportPrivatePem(fixture.Key.ExportPublicPem()));
        Assert.ThrowsAny<CryptographicException>(() => ModernRsaService.ImportPublicPem("-----BEGIN PUBLIC KEY-----\nAAAA\n-----END PUBLIC KEY-----"));
    }

    [Fact]
    public async Task PublicKeyFileImportIsBounded()
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, fixture.Key.ExportPublicPem());
            using var restored = await ModernRsaService.ImportPublicPemFileAsync(path);
            Assert.Equal(fixture.Key.ExportPublicPem(), restored.ExportPublicPem());
            await File.WriteAllBytesAsync(path, new byte[ModernRsaService.MaximumPemLength + 1]);
            await Assert.ThrowsAsync<ArgumentException>(() => ModernRsaService.ImportPublicPemFileAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task KeyFileImportsHonorCancellationBeforeOpeningFile()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ModernRsaService.ImportPublicPemFileAsync("unused", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ModernRsaService.ImportEncryptedPrivatePemFileAsync("unused", "test password", cancellation.Token));
    }

    [Fact]
    public void DisposedKeyCannotBeUsed()
    {
        var key = ModernRsaService.Generate(2048);
        key.Dispose();
        key.Dispose();
        Assert.Throws<ObjectDisposedException>(() => key.ExportPublicPem());
        Assert.Throws<ObjectDisposedException>(() => key.EncryptUtf8ToBase64("text"));
        Assert.Throws<ObjectDisposedException>(() => key.SignUtf8ToBase64("text"));
    }
}
