using System.Numerics;
using System.Security.Cryptography;
using Cryptology.Core.NumberTheory;

namespace Cryptology.Core.Asymmetric;

public sealed class EducationalRsaKey
{
    internal EducationalRsaKey(BigInteger p, BigInteger q, BigInteger n, BigInteger phi, BigInteger e, BigInteger d)
    {
        P = p; Q = q; N = n; Phi = phi; E = e; D = d;
    }

    public BigInteger P { get; }
    public BigInteger Q { get; }
    public BigInteger N { get; }
    public BigInteger Phi { get; }
    public BigInteger E { get; }
    public BigInteger D { get; }
}

/// <summary>Unpadded, variable-time textbook RSA for education. Never use for real encryption or signatures.</summary>
public static class EducationalRsa
{
    public static EducationalRsaKey CreateKey(BigInteger p, BigInteger q, BigInteger e, int rounds = 32, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (p == q) throw new ArgumentException("RSA primes must be distinct.", nameof(q));
        if (!PrimalityTest.MillerRabin(p, rounds, cancellationToken)) throw new ArgumentException("P must be prime.", nameof(p));
        if (!PrimalityTest.MillerRabin(q, rounds, cancellationToken)) throw new ArgumentException("Q must be prime.", nameof(q));
        var phi = (p - 1) * (q - 1);
        if (e <= 1 || e >= phi) throw new ArgumentOutOfRangeException(nameof(e), "Public exponent must satisfy 1 < e < phi.");
        if (IntegerMath.Gcd(e, phi, cancellationToken) != 1) throw new ArgumentException("Public exponent must be coprime to phi.", nameof(e));
        return new(p, q, p * q, phi, e, IntegerMath.ModInverse(e, phi, cancellationToken));
    }

    public static BigInteger Encrypt(BigInteger message, EducationalRsaKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateRepresentative(message, key.N, nameof(message));
        return IntegerMath.ModPow(message, key.E, key.N, cancellationToken: cancellationToken).Value;
    }

    public static BigInteger Decrypt(BigInteger ciphertext, EducationalRsaKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateRepresentative(ciphertext, key.N, nameof(ciphertext));
        return IntegerMath.ModPow(ciphertext, key.D, key.N, cancellationToken: cancellationToken).Value;
    }

    /// <summary>Educational signature of SHA256(data) mod n. This is not RSA-PSS or a secure signature scheme.</summary>
    public static BigInteger SignData(ReadOnlySpan<byte> data, EducationalRsaKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var representative = HashRepresentative(data, key.N, cancellationToken);
        return IntegerMath.ModPow(representative, key.D, key.N, cancellationToken: cancellationToken).Value;
    }

    public static bool VerifyData(ReadOnlySpan<byte> data, BigInteger signature, EducationalRsaKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        if (signature < 0 || signature >= key.N) return false;
        var representative = HashRepresentative(data, key.N, cancellationToken);
        return IntegerMath.ModPow(signature, key.E, key.N, cancellationToken: cancellationToken).Value == representative;
    }

    private static BigInteger HashRepresentative(ReadOnlySpan<byte> data, BigInteger n, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hash = SHA256.HashData(data);
        cancellationToken.ThrowIfCancellationRequested();
        return new BigInteger(hash, isUnsigned: true, isBigEndian: true) % n;
    }

    private static void ValidateRepresentative(BigInteger value, BigInteger n, string parameter)
    {
        if (value < 0 || value >= n) throw new ArgumentOutOfRangeException(parameter, "Representative must satisfy 0 <= value < n.");
    }
}
