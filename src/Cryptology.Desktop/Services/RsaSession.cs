using Cryptology.Security;

namespace Cryptology.Desktop.Services;

/// <summary>One in-memory modern key shared by encryption and signature tools.</summary>
public sealed class RsaSession : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object lifetime = new();
    private bool disposed;
    private ModernRsaService? current;
    public ModernRsaService? Current { get { lock (lifetime) return current; } }
    public event EventHandler? KeyChanged;
    public ModernRsaService Required => Current ?? throw new InvalidOperationException("Generate or import a modern RSA key first.");
    public async Task<T> UseAsync<T>(Func<ModernRsaService, Task<T>> action, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            lock (lifetime) ObjectDisposedException.ThrowIf(disposed, this);
            var result = await action(Required);
            ct.ThrowIfCancellationRequested();
            return result;
        }
        finally { gate.Release(); }
    }
    public Task ChangeAsync(Func<ModernRsaService> factory, CancellationToken ct) =>
        ChangeAsync(token => Task.Run(factory, token), ct);
    public async Task ChangeAsync(Func<CancellationToken, Task<ModernRsaService>> factory, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            lock (lifetime) ObjectDisposedException.ThrowIf(disposed, this);
            var key = await factory(ct);
            ModernRsaService? previous;
            lock (lifetime)
            {
                if (disposed || ct.IsCancellationRequested)
                {
                    key.Dispose();
                    ct.ThrowIfCancellationRequested();
                    throw new ObjectDisposedException(nameof(RsaSession));
                }
                previous = current; current = key;
            }
            previous?.Dispose();
            KeyChanged?.Invoke(this, EventArgs.Empty);
        }
        finally { gate.Release(); }
    }
    public void Dispose()
    {
        ModernRsaService? key;
        lock (lifetime) { if (disposed) return; disposed = true; key = current; current = null; }
        key?.Dispose();
    }
}
