using System.Security.Cryptography;
using System.Text;

namespace Cryptology.Security;

/// <summary>
/// Standard .NET RSA demo: OAEP-SHA256 for short UTF-8 messages and PSS-SHA256 signatures.
/// Dispose the session when its key is no longer needed. No private key is exported automatically.
/// </summary>
public sealed class ModernRsaService : IDisposable
{
    public const int DefaultKeySize = 3072;
    public const int MinimumKeySize = 2048;
    public const int MaximumKeySize = 8192;
    public const int MaximumPemLength = 65_536;
    private const int Sha256Length = 32;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly RSA _rsa;
    private readonly object _sync = new();
    private bool _disposed;

    private ModernRsaService(RSA rsa, bool hasPrivateKey)
    {
        if (rsa.KeySize is < MinimumKeySize or > MaximumKeySize)
            throw new ArgumentException($"RSA keys must be between {MinimumKeySize} and {MaximumKeySize} bits.");
        _rsa = rsa;
        HasPrivateKey = hasPrivateKey;
        KeySize = rsa.KeySize;
    }

    public int KeySize { get; }
    public bool HasPrivateKey { get; }
    public int MaxPlaintextBytes => (KeySize + 7) / 8 - 2 * Sha256Length - 2;

    public static ModernRsaService Generate(int keySize = DefaultKeySize)
    {
        if (keySize is < MinimumKeySize or > MaximumKeySize || keySize % 256 != 0)
            throw new ArgumentOutOfRangeException(nameof(keySize), "Use 2048–8192 bits in multiples of 256.");
        RSA rsa = RSA.Create(keySize);
        try { return new ModernRsaService(rsa, true); }
        catch { rsa.Dispose(); throw; }
    }

    public static ModernRsaService ImportPublicPem(string pem)
    {
        ValidatePem(pem, "PUBLIC KEY", "RSA PUBLIC KEY");
        return Import(rsa => rsa.ImportFromPem(pem), false);
    }

    public static ModernRsaService ImportPrivatePem(string pem)
    {
        ValidatePem(pem, "PRIVATE KEY", "RSA PRIVATE KEY");
        return Import(rsa => rsa.ImportFromPem(pem), true);
    }

    public static ModernRsaService ImportEncryptedPrivatePem(string pem, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        ValidatePem(pem, "ENCRYPTED PRIVATE KEY");
        return Import(rsa => rsa.ImportFromEncryptedPem(pem, password), true);
    }

    public static async Task<ModernRsaService> ImportPublicPemFileAsync(string path, CancellationToken cancellationToken = default)
    {
        string pem = await ReadPemFileAsync(path, cancellationToken).ConfigureAwait(false);
        return await ImportFileKeyAsync(() => ImportPublicPem(pem), cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ModernRsaService> ImportEncryptedPrivatePemFileAsync(string path, string password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(password);
        string pem = await ReadPemFileAsync(path, cancellationToken).ConfigureAwait(false);
        return await ImportFileKeyAsync(() => ImportEncryptedPrivatePem(pem, password), cancellationToken).ConfigureAwait(false);
    }

    public string ExportPublicPem()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            return _rsa.ExportSubjectPublicKeyInfoPem();
        }
    }

    /// <summary>Caller must obtain explicit user consent and protect the returned immutable string.</summary>
    public string ExportEncryptedPrivatePem(string password, bool allowPrivateKeyExport)
    {
        if (!allowPrivateKeyExport)
            throw new InvalidOperationException("Explicit confirmation is required to export a private key.");
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length < 12)
            throw new ArgumentException("Use a private-key export password of at least 12 characters.", nameof(password));
        lock (_sync)
        {
            ThrowIfDisposed();
            RequirePrivateKey();
            return _rsa.ExportEncryptedPkcs8PrivateKeyPem(password,
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 600_000));
        }
    }

    public string EncryptUtf8ToBase64(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        int byteCount = StrictUtf8.GetByteCount(plaintext);
        if (byteCount > MaxPlaintextBytes)
            throw new ArgumentException($"RSA-OAEP SHA-256 accepts at most {MaxPlaintextBytes} UTF-8 bytes with this key. Use hybrid encryption for larger messages.", nameof(plaintext));
        byte[] bytes = StrictUtf8.GetBytes(plaintext);
        try
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return Convert.ToBase64String(_rsa.Encrypt(bytes, RSAEncryptionPadding.OaepSHA256));
            }
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public string DecryptUtf8FromBase64(string ciphertextBase64)
    {
        ArgumentNullException.ThrowIfNull(ciphertextBase64);
        byte[] ciphertext = DecodeBoundedBase64(ciphertextBase64, (KeySize + 7) / 8);
        byte[]? plaintext = null;
        try
        {
            if (ciphertext.Length != (KeySize + 7) / 8)
                throw new CryptographicException("The ciphertext does not have the RSA key's block length.");
            lock (_sync)
            {
                ThrowIfDisposed();
                RequirePrivateKey();
                plaintext = _rsa.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA256);
            }
            return StrictUtf8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public string SignUtf8ToBase64(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] bytes = StrictUtf8.GetBytes(text);
        try { return SignBytesToBase64(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public bool VerifyUtf8FromBase64(string text, string signatureBase64)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] bytes = StrictUtf8.GetBytes(text);
        try { return VerifyBytesFromBase64(bytes, signatureBase64); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public string SignBytesToBase64(ReadOnlySpan<byte> data)
    {
        byte[] hash = SHA256.HashData(data);
        try { return SignHashToBase64(hash); }
        finally { CryptographicOperations.ZeroMemory(hash); }
    }

    public bool VerifyBytesFromBase64(ReadOnlySpan<byte> data, string signatureBase64)
    {
        byte[] hash = SHA256.HashData(data);
        try { return VerifyHashFromBase64(hash, signatureBase64); }
        finally { CryptographicOperations.ZeroMemory(hash); }
    }

    public async Task<string> SignFileAsync(string path, CancellationToken cancellationToken = default)
    {
        byte[] hash = await HashFileAsync(path, cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SignHashToBase64(hash);
        }
        finally { CryptographicOperations.ZeroMemory(hash); }
    }

    public async Task<bool> VerifyFileAsync(string path, string signatureBase64, CancellationToken cancellationToken = default)
    {
        byte[] hash = await HashFileAsync(path, cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return VerifyHashFromBase64(hash, signatureBase64);
        }
        finally { CryptographicOperations.ZeroMemory(hash); }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _rsa.Dispose();
            _disposed = true;
        }
    }

    private string SignHashToBase64(byte[] hash)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            RequirePrivateKey();
            return Convert.ToBase64String(_rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        }
    }

    private bool VerifyHashFromBase64(byte[] hash, string signatureBase64)
    {
        ArgumentNullException.ThrowIfNull(signatureBase64);
        byte[] signature;
        try { signature = DecodeBoundedBase64(signatureBase64, (KeySize + 7) / 8); }
        catch (FormatException) { return false; }
        try
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                if (signature.Length != (KeySize + 7) / 8) return false;
                try { return _rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss); }
                catch (CryptographicException) { return false; }
            }
        }
        finally { CryptographicOperations.ZeroMemory(signature); }
    }

    private static async Task<byte[]> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static ModernRsaService Import(Action<RSA> import, bool hasPrivateKey)
    {
        RSA rsa = RSA.Create();
        try
        {
            import(rsa);
            return new ModernRsaService(rsa, hasPrivateKey);
        }
        catch (CryptographicException exception)
        {
            rsa.Dispose();
            throw new CryptographicException("The RSA key could not be imported. Check its format and password.", exception);
        }
        catch { rsa.Dispose(); throw; }
    }

    private static async Task<ModernRsaService> ImportFileKeyAsync(Func<ModernRsaService> factory, CancellationToken cancellationToken)
    {
        // A password KDF is synchronous in the platform API. Always keep it away from the UI thread.
        ModernRsaService key = await Task.Run(factory, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
        {
            key.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }
        return key;
    }

    private static void ValidatePem(string pem, params string[] allowedLabels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pem);
        if (pem.Length > MaximumPemLength)
            throw new ArgumentException($"PEM input is limited to {MaximumPemLength} characters.", nameof(pem));
        ReadOnlySpan<char> content = pem.AsSpan().Trim();
        if (!PemEncoding.TryFind(content, out PemFields fields) ||
            !allowedLabels.Contains(content[fields.Label].ToString(), StringComparer.Ordinal) ||
            fields.Location.Start.Value != 0 || fields.Location.End.Value != content.Length)
            throw new ArgumentException("Provide exactly one PEM block of the requested key type.", nameof(pem));
    }

    private static byte[] DecodeBoundedBase64(string text, int expectedBytes)
    {
        // Permit normal wrapped Base64 without accepting unbounded pasted files.
        if (text.Length > 4 * ((expectedBytes + 2) / 3) + 1024)
            throw new FormatException("The Base64 value exceeds an RSA block's expected length.");
        byte[] decoded = Convert.FromBase64String(text);
        if (decoded.Length > expectedBytes)
        {
            CryptographicOperations.ZeroMemory(decoded);
            throw new FormatException("The Base64 value exceeds an RSA block's expected length.");
        }
        return decoded;
    }

    private static async Task<string> ReadPemFileAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] bytes = new byte[MaximumPemLength + 1];
        try
        {
            int count = await stream.ReadAtLeastAsync(bytes, bytes.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            if (count > MaximumPemLength)
                throw new ArgumentException($"PEM files are limited to {MaximumPemLength} bytes.", nameof(path));
            return StrictUtf8.GetString(bytes.AsSpan(0, count)).TrimStart('\uFEFF');
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private void RequirePrivateKey()
    {
        if (!HasPrivateKey) throw new InvalidOperationException("A private key is required for this operation.");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
