using System.Numerics;
using Cryptology.Core.SecretSharing;

namespace Cryptology.Core.Tests;

public class MathematicsShamirTests
{
    private static ShamirShareSet KnownVector() => new(2089, 3, 5,
        [new(1, 1494), new(2, 1942), new(3, 489), new(4, 1313), new(5, 236)]);

    [Fact]
    public void EveryThresholdSubsetReconstructsIndependentPythonVector()
    {
        var set = KnownVector();
        for (var a = 0; a < 3; a++)
            for (var b = a + 1; b < 4; b++)
                for (var c = b + 1; c < 5; c++)
                    Assert.Equal(new BigInteger(1234), ShamirSecretSharing.Recover(set with { Shares = [set.Shares[a], set.Shares[b], set.Shares[c]] }));
        Assert.Equal(new BigInteger(1234), ShamirSecretSharing.Recover(set));
    }

    [Theory]
    [InlineData(0, 2, 5)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 1, 1)]
    [InlineData(1234, 3, 5)]
    [InlineData(2088, 5, 5)]
    public void GeneratedSharesSurviveJsonAndThresholdRecovery(int secret, int threshold, int count)
    {
        var set = ShamirSecretSharing.Split(secret, threshold, count, 2089);
        var json = ShamirSecretSharing.Export(set);
        var imported = ShamirSecretSharing.Import(json);
        Assert.Contains("\"prime\": \"2089\"", json);
        Assert.Equal(set.Prime, imported.Prime);
        Assert.Equal(threshold, imported.Threshold);
        Assert.Equal(count, imported.TotalShares);
        Assert.Equal(new BigInteger(secret), ShamirSecretSharing.Recover(imported with { Shares = imported.Shares.Take(threshold).ToArray() }));
        Assert.Equal(new BigInteger(secret), ShamirSecretSharing.Recover(imported));
    }

    [Theory]
    [InlineData(-1, 3, 5, 2089)]
    [InlineData(2089, 3, 5, 2089)]
    [InlineData(1, 0, 5, 2089)]
    [InlineData(1, 6, 5, 2089)]
    [InlineData(1, 2, 0, 2089)]
    [InlineData(1, 2, 5, 5)]
    [InlineData(1, 2, 5, 100)]
    public void SplitValidatesSecretThresholdAndField(int secret, int threshold, int count, int prime)
        => Assert.ThrowsAny<ArgumentException>(() => ShamirSecretSharing.Split(secret, threshold, count, prime));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ThresholdOneWorksInSmallestPrimeField(int secret)
    {
        var set = ShamirSecretSharing.Split(secret, 1, 1, 2);
        Assert.Equal(new BigInteger(secret), set.Shares[0].Y);
        Assert.Equal(new BigInteger(secret), ShamirSecretSharing.Recover(set));
        Assert.Equal(new BigInteger(secret), ShamirSecretSharing.Recover(ShamirSecretSharing.Import(ShamirSecretSharing.Export(set))));
    }

    [Fact]
    public void RecoveryRejectsInsufficientDuplicateOutOfFieldAndCompositeInputs()
    {
        var set = KnownVector();
        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Recover(set with { Shares = set.Shares.Take(2).ToArray() }));
        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Recover(set with { Shares = [set.Shares[0], set.Shares[0], set.Shares[2]] }));
        foreach (var bad in new Share[] { new(0, 1), new(-1, 1), new(2089, 1), new(1, -1), new(1, 2089) })
            Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Recover(set with { Shares = [bad, set.Shares[1], set.Shares[2]] }));
        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Recover(set with { Prime = 2100 }));
    }

    [Fact]
    public void ExtraShareDetectsInconsistencyButExactlyThresholdDoesNotAuthenticate()
    {
        var set = KnownVector();
        var damaged = new Share(1, 1495);
        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Recover(set with { Shares = [damaged, set.Shares[1], set.Shares[2], set.Shares[3]] }));
        var unauthenticated = ShamirSecretSharing.Recover(set with { Shares = [damaged, set.Shares[1], set.Shares[2]] });
        Assert.NotEqual(new BigInteger(1234), unauthenticated);
    }

    [Fact]
    public void StrictImportRejectsMalformedOrAmbiguousDocuments()
    {
        var valid = ShamirSecretSharing.Export(KnownVector());
        Assert.Throws<FormatException>(() => ShamirSecretSharing.Import("not json"));
        Assert.Throws<FormatException>(() => ShamirSecretSharing.Import(valid.Replace("\"version\": 1", "\"version\": 2")));
        Assert.Throws<FormatException>(() => ShamirSecretSharing.Import(valid.Replace("\"prime\": \"2089\"", "\"prime\": \"02089\"")));
        Assert.Throws<FormatException>(() => ShamirSecretSharing.Import(valid.Replace("\"prime\": \"2089\"", "\"prime\": 2089")));
        Assert.Throws<FormatException>(() => ShamirSecretSharing.Import(valid.Replace("\"version\": 1", "\"version\": 1, \"version\": 1")));
        Assert.Throws<FormatException>(() => ShamirSecretSharing.Import(valid.Replace("\"version\": 1", "\"unexpected\": 1")));
        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Import(valid.Replace("\"threshold\": 3", "\"threshold\": 6")));
        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Import(valid.Replace("\"y\": \"1494\"", "\"y\": \"2089\"")));
    }

    [Fact]
    public void SharingOperationsObserveCancellation()
    {
        var token = new CancellationToken(canceled: true);
        Assert.Throws<OperationCanceledException>(() => ShamirSecretSharing.Split(1234, 3, 5, 2089, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => ShamirSecretSharing.Recover(KnownVector(), cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => ShamirSecretSharing.Export(KnownVector(), cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => ShamirSecretSharing.Import("{}", cancellationToken: token));
    }
}
