using System.Security.Cryptography;
using System.Text;

namespace Cryptology.Security;

public enum KeyEncoding { Base64, Hex }

/// <summary>
/// Educational byte XOR. A secret uniform pad used once is essential; XOR does not authenticate data.
/// </summary>
public static class ByteOneTimePad
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static int GetUtf8ByteCount(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return StrictUtf8.GetByteCount(text);
    }

    /// <summary>The caller owns and should clear the returned key.</summary>
    public static byte[] GenerateKeyForText(string text) => GenerateKey(GetUtf8ByteCount(text));

    /// <summary>The caller owns and should clear the returned key.</summary>
    public static byte[] GenerateKey(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        return RandomNumberGenerator.GetBytes(byteCount);
    }

    public static string EncodeKey(ReadOnlySpan<byte> key, KeyEncoding encoding = KeyEncoding.Base64) => encoding switch
    {
        KeyEncoding.Base64 => Convert.ToBase64String(key),
        KeyEncoding.Hex => Convert.ToHexString(key),
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };

    /// <summary>The caller owns and should clear the returned key.</summary>
    public static byte[] DecodeKey(string encodedKey, KeyEncoding encoding = KeyEncoding.Base64)
    {
        ArgumentNullException.ThrowIfNull(encodedKey);
        return encoding switch
        {
            KeyEncoding.Base64 => Convert.FromBase64String(encodedKey),
            KeyEncoding.Hex => Convert.FromHexString(encodedKey),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    /// <summary>Applies XOR only when data and key have exactly the same byte length.</summary>
    public static byte[] Transform(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        if (data.Length != key.Length)
            throw new ArgumentException($"The key must contain exactly {data.Length} bytes.", nameof(key));
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++) result[i] = (byte)(data[i] ^ key[i]);
        return result;
    }

    public static string EncryptToBase64(string plaintext, string encodedKey, KeyEncoding encoding = KeyEncoding.Base64)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        byte[] plaintextBytes = StrictUtf8.GetBytes(plaintext);
        byte[]? key = null;
        byte[]? ciphertext = null;
        try
        {
            key = DecodeKey(encodedKey, encoding);
            ciphertext = Transform(plaintextBytes, key);
            return Convert.ToBase64String(ciphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            if (key is not null) CryptographicOperations.ZeroMemory(key);
            if (ciphertext is not null) CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    public static string DecryptFromBase64(string ciphertextBase64, string encodedKey, KeyEncoding encoding = KeyEncoding.Base64)
    {
        ArgumentNullException.ThrowIfNull(ciphertextBase64);
        byte[] ciphertext = Convert.FromBase64String(ciphertextBase64);
        byte[]? key = null;
        byte[]? plaintext = null;
        try
        {
            key = DecodeKey(encodedKey, encoding);
            plaintext = Transform(ciphertext, key);
            return StrictUtf8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            if (key is not null) CryptographicOperations.ZeroMemory(key);
            if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
