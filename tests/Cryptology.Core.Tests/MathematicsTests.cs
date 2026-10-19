using System.Numerics;
using Cryptology.Core.NumberTheory;

namespace Cryptology.Core.Tests;

public class MathematicsTests
{
    [Theory]
    [InlineData(240, 46, 2)]
    [InlineData(-240, 46, 2)]
    [InlineData(240, -46, 2)]
    [InlineData(-240, -46, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(0, -7, 7)]
    [InlineData(1, 0, 1)]
    public void ExtendedGcdSatisfiesBezoutForSignedInputs(int a, int b, int gcd)
    {
        var result = IntegerMath.ExtendedGcd(a, b, includeSteps: true);
        Assert.Equal(new BigInteger(gcd), result.Gcd);
        Assert.Equal(result.Gcd, a * result.X + b * result.Y);
        Assert.Equal(result.Gcd, IntegerMath.Gcd(a, b));
        Assert.All(result.Steps, step => Assert.Equal(step.Dividend, step.Quotient * step.Divisor + step.Remainder));
    }

    [Fact]
    public void BigIntegerArithmeticExceedsMachineIntegerRange()
    {
        var a = (BigInteger.One << 512) - 1;
        var b = (BigInteger.One << 256) - 1;
        var result = IntegerMath.ExtendedGcd(a, b);
        Assert.Equal(b, result.Gcd);
        Assert.Equal(result.Gcd, a * result.X + b * result.Y);
    }

    [Theory]
    [InlineData(3, 11, 4)]
    [InlineData(-3, 11, 7)]
    [InlineData(17, 3120, 2753)]
    public void ModularInverseMatchesPythonControl(int value, int modulus, int expected)
        => Assert.Equal(new BigInteger(expected), IntegerMath.ModInverse(value, modulus));

    [Fact]
    public void ModularInverseRejectsInvalidDomain()
    {
        Assert.Throws<ArgumentException>(() => IntegerMath.ModInverse(2, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ModInverse(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ModInverse(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ModInverse(1, -7));
    }

    [Theory]
    [InlineData(4, 13, 497, 445)]
    [InlineData(-2, 5, 13, 7)]
    [InlineData(7, 0, 1, 0)]
    [InlineData(0, 0, 7, 1)]
    [InlineData(-10, 0, 3, 1)]
    public void ModPowMatchesIndependentControls(int value, int exponent, int modulus, int expected)
    {
        var result = IntegerMath.ModPow(value, exponent, modulus, includeSteps: true);
        Assert.Equal(new BigInteger(expected), result.Value);
        Assert.Equal(BigInteger.ModPow(IntegerMath.Normalize(value, modulus), exponent, modulus), result.Value);
    }

    [Fact]
    public void ModPowBoundsTraceWithoutStoppingCalculation()
    {
        var exponent = (BigInteger.One << 1100) + 9;
        var result = IntegerMath.ModPow(17, exponent, 97, includeSteps: true);
        Assert.Equal(IntegerMath.MaximumTraceSteps, result.Steps.Count);
        Assert.True(result.TraceTruncated);
        Assert.Equal(BigInteger.ModPow(17, exponent, 97), result.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ModPow(1, -1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ModPow(1, 1, 0));
    }

    [Theory]
    [InlineData(1001, 9907, -1)]
    [InlineData(-1, 7, -1)]
    [InlineData(5, 21, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(6, 9, 0)]
    public void JacobiMatchesOriginalControls(int a, int n, int expected)
        => Assert.Equal(expected, JacobiSymbol.Calculate(a, n));

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    [InlineData(2)]
    public void JacobiRejectsInvalidDenominator(int n)
        => Assert.Throws<ArgumentOutOfRangeException>(() => JacobiSymbol.Calculate(1, n));

    [Theory]
    [InlineData(-11, false)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(47, true)]
    [InlineData(101, true)]
    [InlineData(104729, true)]
    [InlineData(2147483647, true)]
    [InlineData(4, false)]
    [InlineData(9, false)]
    [InlineData(341, false)]
    [InlineData(561, false)]
    [InlineData(1105, false)]
    [InlineData(1729, false)]
    [InlineData(2047, false)]
    [InlineData(1373653, false)]
    [InlineData(3215031751, false)]
    public void ProbablePrimeTestsRecognizeKnownInputs(long n, bool expected)
    {
        Assert.Equal(expected, PrimalityTest.MillerRabin(n, rounds: 64));
        Assert.Equal(expected, PrimalityTest.SolovayStrassen(n, rounds: 64));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PrimalityRejectsVacuousRoundCounts(int rounds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PrimalityTest.MillerRabin(2, rounds));
        Assert.Throws<ArgumentOutOfRangeException>(() => PrimalityTest.SolovayStrassen(9, rounds));
    }

    [Fact]
    public void CryptographicRandomSamplingPreservesBoundsAndCoprimality()
    {
        var big = BigInteger.One << 160;
        Assert.Equal(BigInteger.Zero, CryptoRandom.Below(1));
        Assert.Equal(new BigInteger(-7), CryptoRandom.InRange(-7, -7));
        for (var i = 0; i < 64; i++)
        {
            Assert.InRange(CryptoRandom.Below(257), BigInteger.Zero, new BigInteger(256));
            Assert.InRange(CryptoRandom.InRange(-big, big), -big, big);
            var coprime = CryptoRandom.Coprime(210);
            Assert.InRange(coprime, BigInteger.One, new BigInteger(209));
            Assert.Equal(BigInteger.One, IntegerMath.Gcd(coprime, 210));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => CryptoRandom.Below(0));
        Assert.Throws<ArgumentException>(() => CryptoRandom.InRange(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CryptoRandom.Coprime(1));
    }

    [Fact]
    public void PrimeGenerationHasRequestedSizeAndHandlesEmptyIntervals()
    {
        var prime = PrimeGenerator.Generate(96);
        Assert.Equal(96, prime.GetBitLength());
        Assert.True(PrimalityTest.MillerRabin(prime, 64));
        Assert.Equal(new BigInteger(2), PrimeGenerator.GenerateInRange(-10, 2));
        Assert.Equal(new BigInteger(101), PrimeGenerator.GenerateInRange(101, 101));
        Assert.Throws<ArgumentException>(() => PrimeGenerator.GenerateInRange(14, 16));
        Assert.Throws<ArgumentException>(() => PrimeGenerator.GenerateInRange(-4, 1));
        Assert.Throws<ArgumentException>(() => PrimeGenerator.GenerateInRange(20, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PrimeGenerator.Generate(1));
    }

    [Fact]
    public void LongOperationsObserveCancellationBeforeWork()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var token = source.Token;
        Assert.Throws<OperationCanceledException>(() => IntegerMath.Gcd(240, 46, token));
        Assert.Throws<OperationCanceledException>(() => IntegerMath.ExtendedGcd(240, 46, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => IntegerMath.ModPow(4, 13, 497, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => JacobiSymbol.Calculate(1001, 9907, token));
        Assert.Throws<OperationCanceledException>(() => PrimalityTest.MillerRabin(101, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => PrimalityTest.SolovayStrassen(101, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => CryptoRandom.Below(1, token));
        Assert.Throws<OperationCanceledException>(() => PrimeGenerator.Generate(256, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => PrimeGenerator.GenerateInRange(2, 1000, cancellationToken: token));
    }
}
