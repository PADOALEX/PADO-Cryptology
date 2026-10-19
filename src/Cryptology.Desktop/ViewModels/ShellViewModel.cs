using CommunityToolkit.Mvvm.ComponentModel;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;
using Cryptology.Security;

namespace Cryptology.Desktop.ViewModels;

public sealed partial class ShellViewModel : ObservableObject, IDisposable
{
    public Strings L { get; }
    public IReadOnlyList<ToolViewModel> Tools { get; }
    [ObservableProperty] private ToolViewModel selectedTool;
    [ObservableProperty] private bool darkTheme = true;
    public ShellViewModel(Strings l, IFileService files, RsaSession rsa)
    {
        L = l;
        Tools = [new FrequencyViewModel(l, files), new ClassicalViewModel(l, files), new AnalysisViewModel(l, files),
            new VigenereViewModel(l, files), new OtpViewModel(l, files), new MathViewModel(l, files),
            new TheoryViewModel(l, files), new RsaViewModel(l, files, rsa), new SignatureViewModel(l, files, rsa), new ShamirViewModel(l, files)];
        selectedTool = Tools[0];
    }
    public void Dispose() { foreach (var tool in Tools) tool.Dispose(); GC.SuppressFinalize(this); }
}
