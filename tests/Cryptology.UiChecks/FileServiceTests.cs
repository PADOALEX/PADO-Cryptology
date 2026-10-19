using System.Text;
using Cryptology.Desktop.Services;

namespace Cryptology.UiChecks;

/// <summary>Real filesystem checks of the same bounded decoder used by native text imports.</summary>
public sealed class FileServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Utf8WithOptionalBomPreservesUkrainianSupplementaryAndInternalBom(bool includeBom)
    {
        const string text = "ҐЄІЇ, Привіт! English 😀\nA\uFEFFB";
        var payload = new UTF8Encoding(false, true).GetBytes(text);
        var bytes = includeBom ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(payload).ToArray() : payload;
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, bytes);
            Assert.Equal(text, await FileService.ReadUtf8FileAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Utf16BomIsRejectedInsteadOfSilentlySelectingAnotherEncoding(bool bigEndian)
    {
        var encoding = new UnicodeEncoding(bigEndian, byteOrderMark: true, throwOnInvalidBytes: true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes("ABC ҐЄІЇ")).ToArray();
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, bytes);
            await Assert.ThrowsAsync<DecoderFallbackException>(() => FileService.ReadUtf8FileAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task InvalidUtf8RejectsMalformedOverlongSurrogateAndTruncatedSequences()
    {
        byte[][] payloads = [[0x80], [0xC0, 0xAF], [0xED, 0xA0, 0x80], [0xF0, 0x9F, 0x98], [0xFF]];
        var path = Path.GetTempFileName();
        try
        {
            foreach (var bytes in payloads)
            {
                await File.WriteAllBytesAsync(path, bytes);
                await Assert.ThrowsAsync<DecoderFallbackException>(() => FileService.ReadUtf8FileAsync(path));
            }
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task EmptyFileAndBomOnlyFileDecodeAsEmpty()
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.Equal("", await FileService.ReadUtf8FileAsync(path));
            await File.WriteAllBytesAsync(path, [0xEF, 0xBB, 0xBF]);
            Assert.Equal("", await FileService.ReadUtf8FileAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task OversizedLengthIsRejectedBeforeReadingOrDecoding()
    {
        var path = Path.GetTempFileName();
        try
        {
            // Extending a fixture avoids allocating a matching managed payload.
            await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
                stream.SetLength(32L * 1024 * 1024 + 1);
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => FileService.ReadUtf8FileAsync(path));
            Assert.Contains("32 MiB", exception.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task CancelledImportDoesNotReturnPartiallyReadTextAndReleasesFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "Cancelled import: ҐЄІЇ", new UTF8Encoding(false, true));
            var token = new CancellationToken(canceled: true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileService.ReadUtf8FileAsync(path, token));
            // Exclusive reopen also verifies await-using closed the stream on cancellation.
            await using var reopened = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(reopened.Length > 0);
        }
        finally { File.Delete(path); }
    }
}
