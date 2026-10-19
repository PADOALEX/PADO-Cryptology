using System.Numerics;

namespace Cryptology.Core.NumberTheory;

public sealed record EuclidStep(BigInteger Dividend, BigInteger Divisor, BigInteger Quotient, BigInteger Remainder);
public sealed record ExtendedGcdResult(BigInteger Gcd, BigInteger X, BigInteger Y, IReadOnlyList<EuclidStep> Steps, bool TraceTruncated);
public sealed record ModPowStep(BigInteger Exponent, BigInteger Base, BigInteger Accumulator, bool Multiplied);
public sealed record ModPowResult(BigInteger Value, IReadOnlyList<ModPowStep> Steps, bool TraceTruncated);

/// <summary>Variable-time educational integer arithmetic; traces can contain secret intermediate values.</summary>
public static class IntegerMath
{
    public const int MaximumTraceSteps = 1024;

    public static BigInteger Normalize(BigInteger value, BigInteger modulus)
    {
        if (modulus <= 0) throw new ArgumentOutOfRangeException(nameof(modulus), "Modulus must be positive.");
        var result = value % modulus;
        return result.Sign < 0 ? result + modulus : result;
    }

    public static BigInteger Gcd(BigInteger a, BigInteger b, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        a = BigInteger.Abs(a);
        b = BigInteger.Abs(b);
        while (!b.IsZero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (a, b) = (b, a % b);
        }
        return a;
    }

    public static ExtendedGcdResult ExtendedGcd(BigInteger a, BigInteger b, bool includeSteps = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var oldR = BigInteger.Abs(a);
        var r = BigInteger.Abs(b);
        BigInteger oldX = 1, x = 0, oldY = 0, y = 1;
        var steps = includeSteps ? new List<EuclidStep>() : null;
        var truncated = false;
        while (!r.IsZero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quotient = BigInteger.DivRem(oldR, r, out var remainder);
            if (steps is not null)
            {
                if (steps.Count < MaximumTraceSteps) steps.Add(new(oldR, r, quotient, remainder));
                else truncated = true;
            }
            (oldR, r) = (r, remainder);
            (oldX, x) = (x, oldX - quotient * x);
            (oldY, y) = (y, oldY - quotient * y);
        }
        return new(oldR, a.Sign < 0 ? -oldX : oldX, b.Sign < 0 ? -oldY : oldY,
            steps is null ? Array.Empty<EuclidStep>() : steps.AsReadOnly(), truncated);
    }

    public static BigInteger ModInverse(BigInteger value, BigInteger modulus, CancellationToken cancellationToken = default)
    {
        if (modulus <= 1) throw new ArgumentOutOfRangeException(nameof(modulus), "Inverse modulus must exceed one.");
        var result = ExtendedGcd(value, modulus, cancellationToken: cancellationToken);
        if (result.Gcd != 1) throw new ArgumentException("A modular inverse does not exist for these inputs.", nameof(value));
        return Normalize(result.X, modulus);
    }

    public static ModPowResult ModPow(BigInteger value, BigInteger exponent, BigInteger modulus, bool includeSteps = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (exponent.Sign < 0) throw new ArgumentOutOfRangeException(nameof(exponent), "Exponent must be nonnegative.");
        if (modulus <= 0) throw new ArgumentOutOfRangeException(nameof(modulus), "Modulus must be positive.");
        var current = Normalize(value, modulus);
        var accumulator = BigInteger.One % modulus;
        var remaining = exponent;
        var steps = includeSteps ? new List<ModPowStep>() : null;
        var truncated = false;
        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var multiplied = !remaining.IsEven;
            if (multiplied) accumulator = accumulator * current % modulus;
            if (steps is not null)
            {
                if (steps.Count < MaximumTraceSteps) steps.Add(new(remaining, current, accumulator, multiplied));
                else truncated = true;
            }
            remaining >>= 1;
            if (remaining > 0) current = current * current % modulus;
        }
        return new(accumulator, steps is null ? Array.Empty<ModPowStep>() : steps.AsReadOnly(), truncated);
    }
}
