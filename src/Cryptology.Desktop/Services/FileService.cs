using System.Text;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace Cryptology.Desktop.Services;

public interface IFileService
{
    Task<string?> OpenTextAsync(CancellationToken ct = default);
    Task<bool> SaveTextAsync(string text, string suggestedName, CancellationToken ct = default);
    Task<string?> PickFileAsync();
    Task CopyAsync(string text);
    Task<string?> PasteAsync();
}

public sealed class FileService : IFileService
{
    public Window? Owner { get; set; }
    private Window Window => Owner ?? throw new InvalidOperationException("No desktop window is available.");
    public async Task<string?> OpenTextAsync(CancellationToken ct = default)
    {
        var path = await PickFileAsync();
        if (path is null) return null;
        return await ReadUtf8FileAsync(path, ct);
    }
    public static async Task<string> ReadUtf8FileAsync(string path, CancellationToken ct = default)
    {
        const int limit = 32 * 1024 * 1024;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > limit) throw new ArgumentException("Text imports are limited to 32 MiB.");
        var initialLength = (int)stream.Length;
        var bytes = new byte[initialLength + 1];
        var count = await stream.ReadAtLeastAsync(bytes, bytes.Length, false, ct);
        if (count > initialLength) throw new IOException("The file changed while reading. Please import it again.");
        var offset = count >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        return new UTF8Encoding(false, true).GetString(bytes, offset, count - offset);
    }
    public async Task<bool> SaveTextAsync(string text, string suggestedName, CancellationToken ct = default)
    {
        var file = await Window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "PADO Cryptology", SuggestedFileName = suggestedName,
            ShowOverwritePrompt = true,
        });
        if (file is null) return false;
        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(text.AsMemory(), ct);
        return true;
    }
    public async Task<string?> PickFileAsync()
    {
        var files = await Window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        { Title = "PADO Cryptology", AllowMultiple = false });
        return files.Count == 0 ? null : files[0].TryGetLocalPath()
            ?? throw new IOException("A local file is required.");
    }
    public Task CopyAsync(string text) => Window.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
    public Task<string?> PasteAsync() => Window.Clipboard?.TryGetTextAsync() ?? Task.FromResult<string?>(null);
}
