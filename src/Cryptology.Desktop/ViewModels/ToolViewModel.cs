using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cryptology.Core.Models;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;

namespace Cryptology.Desktop.ViewModels;

public abstract partial class ToolViewModel : ObservableObject, IDisposable
{
    protected readonly IFileService Files;
    private CancellationTokenSource? operation;
    public Strings L { get; }
    public string Id { get; }
    public string Title => L[Id];
    public string Description => L[Id + "Help"];
    [ObservableProperty] private string text = "";
    [ObservableProperty] private string output = "";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool ukrainianAlphabet;
    public Alphabet Alphabet => UkrainianAlphabet ? Alphabet.Ukrainian : Alphabet.English;
    public IAsyncRelayCommand<string> RunCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand ImportCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IAsyncRelayCommand CopyCommand { get; }
    public IAsyncRelayCommand PasteCommand { get; }
    public IRelayCommand ClearCommand { get; }
    public IRelayCommand SwapCommand { get; }

    protected ToolViewModel(string id, Strings l, IFileService files)
    {
        Id = id; L = l; Files = files;
        RunCommand = new AsyncRelayCommand<string>(RunActionAsync, _ => !IsBusy);
        CancelCommand = new RelayCommand(() => operation?.Cancel());
        ImportCommand = new AsyncRelayCommand(() => GuardAsync(async () => { var s = await Files.OpenTextAsync(); if (s is not null) Text = s; }));
        ExportCommand = new AsyncRelayCommand(() => GuardAsync(async () => { if (await Files.SaveTextAsync(Output, Id + ".txt")) Status = L["Saved"]; }));
        CopyCommand = new AsyncRelayCommand(() => GuardAsync(async () => { await Files.CopyAsync(Output); Status = L["Copied"]; }));
        PasteCommand = new AsyncRelayCommand(() => GuardAsync(async () => { var s = await Files.PasteAsync(); if (s is not null) Text = s; }));
        ClearCommand = new RelayCommand(Clear, () => !IsBusy);
        SwapCommand = new RelayCommand(() => Text = Output, () => !IsBusy);
        L.PropertyChanged += Localized;
    }
    private void Localized(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    { OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(Description)); }
    partial void OnUkrainianAlphabetChanged(bool value) => AlphabetChanged();
    protected virtual void AlphabetChanged() { }
    partial void OnIsBusyChanged(bool value)
    {
        RunCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
        SwapCommand.NotifyCanExecuteChanged();
    }
    public async Task RunActionAsync(string? action)
    {
        if (IsBusy) return;
        using var cts = new CancellationTokenSource();
        operation = cts;
        IsBusy = true; Status = "";
        try { await ExecuteAsync(action ?? "run", cts.Token); }
        catch (OperationCanceledException) { Status = L["Cancelled"]; }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or System.Security.Cryptography.CryptographicException or FormatException or System.Text.Json.JsonException or OverflowException or UnauthorizedAccessException)
        { Status = L["Error"] + ": " + e.Message; }
        finally { operation = null; IsBusy = false; }
    }
    protected async Task GuardAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception e) when (e is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        { Status = L["Error"] + ": " + e.Message; }
    }
    protected abstract Task ExecuteAsync(string action, CancellationToken ct);
    protected virtual void Clear() { Text = Output = Status = ""; }
    protected System.Numerics.BigInteger Integer(string value)
    {
        if (value.Length > 4096) throw new ArgumentException(L["IntegerLimit"]);
        return System.Numerics.BigInteger.TryParse(value, out var n) ? n : throw new ArgumentException(L["NumberError"]);
    }
    protected int SmallInteger(string value, int min, int max)
    {
        if (!int.TryParse(value, out var n) || n < min || n > max) throw new ArgumentException($"{L["NumberError"]} [{min}, {max}]");
        return n;
    }
    public virtual void Dispose() { operation?.Cancel(); L.PropertyChanged -= Localized; GC.SuppressFinalize(this); }
}
