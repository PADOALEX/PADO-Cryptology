using System.Numerics;

namespace Cryptology.Core.NumberTheory;

public static class JacobiSymbol
{
    public static int Calculate(BigInteger a, BigInteger oddPositiveN, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (oddPositiveN <= 0 || oddPositiveN.IsEven)
            throw new ArgumentOutOfRangeException(nameof(oddPositiveN), "Jacobi denominator must be positive and odd.");
        var n = oddPositiveN;
        a = IntegerMath.Normalize(a, n);
        var sign = 1;
        while (!a.IsZero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (a.IsEven)
            {
                cancellationToken.ThrowIfCancellationRequested();
                a >>= 1;
                if (n % 8 == 3 || n % 8 == 5) sign = -sign;
            }
            (a, n) = (n, a);
            if (a % 4 == 3 && n % 4 == 3) sign = -sign;
            a %= n;
        }
        return n == 1 ? sign : 0;
    }
}

public static class PrimalityTest
{
    private static readonly int[] SmallPrimes = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47];

    public static bool MillerRabin(BigInteger n, int rounds = 32, CancellationToken cancellationToken = default)
    {
        ValidateRounds(rounds);
        cancellationToken.ThrowIfCancellationRequested();
        var quick = TrialDivision(n);
        if (quick.HasValue) return quick.Value;
        var d = n - 1;
        var s = 0;
        while (d.IsEven)
        {
            cancellationToken.ThrowIfCancellationRequested();
            d >>= 1;
            s++;
        }
        for (var i = 0; i < rounds; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var a = CryptoRandom.InRange(2, n - 2, cancellationToken);
            var x = IntegerMath.ModPow(a, d, n, cancellationToken: cancellationToken).Value;
            if (x == 1 || x == n - 1) continue;
            var passed = false;
            for (var r = 1; r < s; r++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                x = x * x % n;
                if (x == n - 1) { passed = true; break; }
                if (x == 1) return false;
            }
            if (!passed) return false;
        }
        return true;
    }

    public static bool SolovayStrassen(BigInteger n, int rounds = 32, CancellationToken cancellationToken = default)
    {
        ValidateRounds(rounds);
        cancellationToken.ThrowIfCancellationRequested();
        var quick = TrialDivision(n);
        if (quick.HasValue) return quick.Value;
        for (var i = 0; i < rounds; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var a = CryptoRandom.InRange(2, n - 2, cancellationToken);
            var jacobi = JacobiSymbol.Calculate(a, n, cancellationToken);
            if (jacobi == 0 || IntegerMath.ModPow(a, (n - 1) / 2, n, cancellationToken: cancellationToken).Value != IntegerMath.Normalize(jacobi, n))
                return false;
        }
        return true;
    }

    internal static void ValidateRounds(int rounds)
    {
        if (rounds < 1) throw new ArgumentOutOfRangeException(nameof(rounds), "At least one primality-test round is required.");
    }

    private static bool? TrialDivision(BigInteger n)
    {
        if (n < 2) return false;
        foreach (var p in SmallPrimes)
        {
            if (n == p) return true;
            if (n % p == 0) return false;
        }
        return null;
    }
}
