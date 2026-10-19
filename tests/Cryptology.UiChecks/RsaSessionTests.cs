using Avalonia.Headless.XUnit;
using Cryptology.Desktop.Services;
using Cryptology.Security;

namespace Cryptology.UiChecks;

public sealed class RsaSessionTests
{
    [AvaloniaFact]
    public async Task SharedSessionSerializesReplacementAndDisposesPreviousKey()
    {
        using var session = new RsaSession();
        await session.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        ModernRsaService old = session.Required;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<string> usingKey = session.UseAsync(async key =>
        {
            started.SetResult();
            await release.Task;
            return key.ExportPublicPem();
        }, CancellationToken.None);
        await started.Task;
        Task replacing = session.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        Assert.False(replacing.IsCompleted);
        release.SetResult();
        string previousPublic = await usingKey;
        await replacing;
        Assert.NotEqual(previousPublic, session.Required.ExportPublicPem());
        Assert.Throws<ObjectDisposedException>(() => old.ExportPublicPem());
    }

    [AvaloniaFact]
    public async Task CancellationWaitingForSessionDoesNotRunKeyFactory()
    {
        using var session = new RsaSession();
        await session.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> usingKey = session.UseAsync(async _ => { started.SetResult(); await release.Task; return 1; }, CancellationToken.None);
        await started.Task;
        bool factoryRan = false;
        using var cancellation = new CancellationTokenSource();
        Task replacement = session.ChangeAsync(() => { factoryRan = true; return ModernRsaService.Generate(2048); }, cancellation.Token);
        cancellation.Cancel();
        try { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => replacement); }
        finally { release.SetResult(); await usingKey; }
        Assert.False(factoryRan);
        Assert.NotEmpty(session.Required.ExportPublicPem());
    }

    [AvaloniaFact]
    public async Task DisposingDuringGenerationCannotResurrectAPrivateKey()
    {
        var session = new RsaSession();
        ModernRsaService generated = ModernRsaService.Generate(2048);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Task replacement = session.ChangeAsync(() =>
        {
            started.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test release was not signalled.");
            return generated;
        }, CancellationToken.None);
        try
        {
            await started.Task;
            session.Dispose();
            release.Set();
            await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => replacement);
            Assert.Throws<ObjectDisposedException>(() => generated.ExportPublicPem());
        }
        finally { release.Set(); session.Dispose(); generated.Dispose(); }
    }

    [AvaloniaFact]
    public async Task CancellationDuringKeyFactoryDisposesLateResultAndRetainsPreviousKey()
    {
        using var session = new RsaSession();
        await session.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        ModernRsaService previous = session.Required;
        ModernRsaService pending = ModernRsaService.Generate(2048);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        Task replacement = session.ChangeAsync(async _ => { started.SetResult(); await release.Task; return pending; }, cancellation.Token);
        try
        {
            await started.Task;
            cancellation.Cancel();
            release.SetResult();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => replacement);
            Assert.Same(previous, session.Required);
            Assert.NotEmpty(previous.ExportPublicPem());
            Assert.Throws<ObjectDisposedException>(() => pending.ExportPublicPem());
        }
        finally { release.TrySetResult(); pending.Dispose(); }
    }

    [AvaloniaFact]
    public async Task CancellationDuringProviderOperationDoesNotPublishCompletedResult()
    {
        using var session = new RsaSession();
        await session.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        Task<string> use = session.UseAsync(async key => { started.SetResult(); await release.Task; return key.ExportPublicPem(); }, cancellation.Token);
        await started.Task;
        cancellation.Cancel();
        release.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => use);
        Assert.NotEmpty(session.Required.ExportPublicPem());
    }
}
