using System.Numerics;
using System.Text;
using Cryptology.Core.Asymmetric;

namespace Cryptology.Core.Tests;

public class MathematicsRsaTests
{
    [Fact]
    public void TextbookRsaMatchesPythonKnownVector()
    {
        var key = EducationalRsa.CreateKey(61, 53, 17);
        Assert.Equal(new BigInteger(3233), key.N);
        Assert.Equal(new BigInteger(3120), key.Phi);
        Assert.Equal(new BigInteger(2753), key.D);
        Assert.Equal(new BigInteger(2790), EducationalRsa.Encrypt(65, key));
        Assert.Equal(new BigInteger(65), EducationalRsa.Decrypt(2790, key));
        foreach (var message in new BigInteger[] { 0, 1, 53, 61, 3232 })
            Assert.Equal(message, EducationalRsa.Decrypt(EducationalRsa.Encrypt(message, key), key));
    }

    [Theory]
    [InlineData(61, 61, 17)]
    [InlineData(4, 53, 17)]
    [InlineData(61, 0, 17)]
    [InlineData(-61, 53, 17)]
    [InlineData(61, 53, 1)]
    [InlineData(61, 53, 3120)]
    [InlineData(61, 53, 65537)]
    [InlineData(61, 53, 6)]
    public void KeyValidationRejectsInvalidPrimesAndExponent(int p, int q, int e)
        => Assert.ThrowsAny<ArgumentException>(() => EducationalRsa.CreateKey(p, q, e));

    [Fact]
    public void RepresentativesCannotSilentlyWrapModuloN()
    {
        var key = EducationalRsa.CreateKey(61, 53, 17);
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationalRsa.Encrypt(-1, key));
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationalRsa.Encrypt(key.N, key));
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationalRsa.Decrypt(-1, key));
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationalRsa.Decrypt(key.N, key));
    }

    [Fact]
    public void EducationalHashSignatureMatchesPythonAndChecksStrictRange()
    {
        var key = EducationalRsa.CreateKey(61, 53, 17);
        var bytes = Encoding.UTF8.GetBytes("abc");
        var signature = EducationalRsa.SignData(bytes, key);
        Assert.Equal(new BigInteger(1480), signature);
        Assert.True(EducationalRsa.VerifyData(bytes, signature, key));
        Assert.False(EducationalRsa.VerifyData(Encoding.UTF8.GetBytes("abd"), signature, key));
        Assert.False(EducationalRsa.VerifyData(bytes, signature + key.N, key));
        Assert.False(EducationalRsa.VerifyData(bytes, -1, key));
    }

    [Fact]
    public void RsaOperationsObserveCancellation()
    {
        var key = EducationalRsa.CreateKey(61, 53, 17);
        var token = new CancellationToken(canceled: true);
        Assert.Throws<OperationCanceledException>(() => EducationalRsa.CreateKey(61, 53, 17, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => EducationalRsa.Encrypt(65, key, token));
        Assert.Throws<OperationCanceledException>(() => EducationalRsa.Decrypt(2790, key, token));
        Assert.Throws<OperationCanceledException>(() => EducationalRsa.SignData([], key, token));
        Assert.Throws<OperationCanceledException>(() => EducationalRsa.VerifyData([], 0, key, token));
    }
}
