using System.Numerics;
using System.Security.Cryptography;

namespace Cryptology.Core.NumberTheory;

public static class PrimeGenerator
{
    public static BigInteger Generate(int bits, int rounds = 32, CancellationToken cancellationToken = default)
    {
        if (bits < 2) throw new ArgumentOutOfRangeException(nameof(bits), "At least two bits are required.");
        PrimalityTest.ValidateRounds(rounds);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = new byte[checked((bits - 1) / 8 + 1)];
        var highBit = (bits - 1) % 8;
        var mask = (byte)((1 << (highBit + 1)) - 1);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RandomNumberGenerator.Fill(bytes);
            bytes[^1] = (byte)((bytes[^1] & mask) | (1 << highBit));
            bytes[0] |= 1;
            var candidate = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
            if (PrimalityTest.MillerRabin(candidate, rounds, cancellationToken)) return candidate;
        }
    }

    /// <summary>Searches all candidates from a random start with wraparound; throws if no probable prime exists.</summary>
    public static BigInteger GenerateInRange(BigInteger minimumInclusive, BigInteger maximumInclusive, int rounds = 32, CancellationToken cancellationToken = default)
    {
        PrimalityTest.ValidateRounds(rounds);
        cancellationToken.ThrowIfCancellationRequested();
        if (minimumInclusive > maximumInclusive) throw new ArgumentException("Maximum must be at least minimum.", nameof(maximumInclusive));
        minimumInclusive = BigInteger.Max(2, minimumInclusive);
        if (maximumInclusive < minimumInclusive) throw new ArgumentException("The interval contains no primes.");
        var start = CryptoRandom.InRange(minimumInclusive, maximumInclusive, cancellationToken);
        var candidate = start;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (PrimalityTest.MillerRabin(candidate, rounds, cancellationToken)) return candidate;
            candidate = candidate == maximumInclusive ? minimumInclusive : candidate + 1;
        } while (candidate != start);
        throw new ArgumentException("The interval contains no primes.");
    }
}
