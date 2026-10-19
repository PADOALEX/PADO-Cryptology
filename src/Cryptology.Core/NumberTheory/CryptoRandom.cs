using System.Numerics;
using System.Security.Cryptography;

namespace Cryptology.Core.NumberTheory;

public static class CryptoRandom
{
    /// <summary>Uniform rejection sampling in [0, exclusiveMaximum), avoiding modulo bias.</summary>
    public static BigInteger Below(BigInteger exclusiveMaximum, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (exclusiveMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        if (exclusiveMaximum == 1) return BigInteger.Zero;
        var highest = exclusiveMaximum - 1;
        var bytes = new byte[highest.GetByteCount(isUnsigned: true)];
        var highBits = (int)((highest.GetBitLength() - 1) % 8 + 1);
        var mask = (byte)((1 << highBits) - 1);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RandomNumberGenerator.Fill(bytes);
            bytes[^1] &= mask;
            var candidate = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
            if (candidate < exclusiveMaximum) return candidate;
        }
    }

    public static BigInteger InRange(BigInteger minimumInclusive, BigInteger maximumInclusive, CancellationToken cancellationToken = default)
    {
        if (maximumInclusive < minimumInclusive) throw new ArgumentException("Maximum must be at least the minimum.", nameof(maximumInclusive));
        return minimumInclusive + Below(maximumInclusive - minimumInclusive + 1, cancellationToken);
    }

    public static BigInteger Coprime(BigInteger modulus, CancellationToken cancellationToken = default)
    {
        if (modulus < 2) throw new ArgumentOutOfRangeException(nameof(modulus), "Modulus must be at least two.");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = InRange(1, modulus - 1, cancellationToken);
            if (IntegerMath.Gcd(candidate, modulus, cancellationToken) == 1) return candidate;
        }
    }
}
