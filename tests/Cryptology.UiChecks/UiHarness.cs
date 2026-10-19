using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;
using Cryptology.Desktop.ViewModels;
using Cryptology.Desktop.Views;

namespace Cryptology.UiChecks;

internal sealed class MemoryFiles : IFileService
{
    public Queue<string?> TextImports { get; } = new();
    public Queue<string?> FilePicks { get; } = new();
    public List<(string Name, string Text)> Saves { get; } = [];
    public string? Clipboard { get; set; }
    public Task<string?> OpenTextAsync(CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); return Task.FromResult(TextImports.Count == 0 ? null : TextImports.Dequeue()); }
    public Task<bool> SaveTextAsync(string text, string suggestedName, CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Saves.Add((suggestedName, text)); return Task.FromResult(true); }
    public Task<string?> PickFileAsync() => Task.FromResult(FilePicks.Count == 0 ? null : FilePicks.Dequeue());
    public Task CopyAsync(string text) { Clipboard = text; return Task.CompletedTask; }
    public Task<string?> PasteAsync() => Task.FromResult(Clipboard);
}

internal sealed class UiHarness : IDisposable
{
    public Strings Strings { get; } = new();
    public MemoryFiles Files { get; } = new();
    public RsaSession Rsa { get; } = new();
    public ShellViewModel Shell { get; }
    public MainWindow Window { get; }

    public UiHarness()
    {
        Shell = new ShellViewModel(Strings, Files, Rsa);
        Window = new MainWindow(Shell);
        Window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public T Tool<T>() where T : ToolViewModel => Shell.Tools.OfType<T>().Single();

    public T Select<T>() where T : ToolViewModel
    {
        T tool = Tool<T>();
        Find<ListBox>("ModuleNavigation").SelectedItem = tool;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(tool, Shell.SelectedTool);
        return tool;
    }

    public T Find<T>(string name) where T : Control => Window.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    public async Task ActionAsync(string action)
    {
        if (action is "import-private" or "export-private")
        {
            foreach (var expander in Window.GetVisualDescendants().OfType<Expander>()) expander.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
        }
        var button = Find<Button>(action.Replace('-', '_') + "Button");
        Assert.NotNull(button.Command);
        Assert.True(button.Command.CanExecute(button.CommandParameter));
        button.Command.Execute(button.CommandParameter);
        if (Shell.SelectedTool.RunCommand.ExecutionTask is { } task) await task;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Shell.SelectedTool.IsBusy);
    }

    public void CaptureIfRequested(string filename)
    {
        string? directory = Environment.GetEnvironmentVariable("PADO_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(Path.GetFullPath(directory));
        Dispatcher.UIThread.RunJobs();
        using var frame = Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(Path.GetFullPath(directory), filename + ".png"));
    }

    public void Dispose()
    {
        Window.Close();
        Shell.Dispose();
        Rsa.Dispose();
    }
}
