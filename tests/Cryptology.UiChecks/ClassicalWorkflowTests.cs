using System.Text;
using Avalonia.Headless.XUnit;
using Cryptology.Desktop.ViewModels;

namespace Cryptology.UiChecks;

public sealed class ClassicalWorkflowTests
{
    [AvaloniaFact]
    public async Task FrequencyWorkflowHonorsUnicodeOptionsAndExportsCsv()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<FrequencyViewModel>();
        vm.UkrainianAlphabet = true;
        vm.Text = "Ґґ ЄІЇ! 🧪";
        await ui.ActionAsync("analyze");
        Assert.Equal(2, vm.Rows.Single(r => r.Symbol == "Ґ").Count);
        Assert.Equal(5, vm.Rows.Sum(r => r.Count));
        ui.CaptureIfRequested("frequency-analysis");
        vm.CaseSensitive = vm.Spaces = vm.Punctuation = vm.Other = true;
        await ui.ActionAsync("csv");
        Assert.Equal(9, vm.Rows.Sum(r => r.Count));
        Assert.Equal(1, vm.Rows.Single(r => r.Symbol == "🧪").Count);
        var saved = ui.Files.Saves.Single();
        Assert.Equal("frequency.csv", saved.Name);
        Assert.StartsWith("Symbol,Count,Percentage,ReferencePercentage\n", saved.Text);
        Assert.Contains("\"🧪\",1,", saved.Text);
        Assert.Equal(ui.Strings["Saved"], vm.Status);
    }

    [AvaloniaFact]
    public async Task SubstitutionKeyCanBeGeneratedExportedImportedAndEditedWithDuplicateFeedback()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<ClassicalViewModel>();
        vm.UseSubstitution = true;
        vm.UkrainianAlphabet = true;
        vm.Text = "Ґанок, Єнот, Іван та Їжак!";
        string original = vm.Text;
        await ui.ActionAsync("generate");
        Assert.Equal(33, vm.Mappings.Select(r => r.Target).Distinct().Count());
        await ui.ActionAsync("export-key");
        string exported = ui.Files.Saves.Single().Text;
        await ui.ActionAsync("encrypt");
        string ciphertext = vm.Output;
        await ui.ActionAsync("generate");
        ui.Files.TextImports.Enqueue(exported);
        await ui.ActionAsync("import-key");
        vm.Text = ciphertext;
        await ui.ActionAsync("decrypt");
        Assert.Equal(original, vm.Output);
        vm.Mappings[1].Target = vm.Mappings[0].Target;
        Assert.True(vm.HasDuplicates);
        Assert.True(vm.Mappings[0].Duplicate);
        Assert.True(vm.Mappings[1].Duplicate);
        await ui.ActionAsync("encrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task VigenereKnownVectorAndUkrainianRoundTripAreAccessibleFromUi()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<VigenereViewModel>();
        vm.Key = "LEMON";
        vm.Text = "ATTACKATDAWN";
        await ui.ActionAsync("encrypt");
        Assert.Equal("LXFOPVEFRNHR", vm.Output);
        vm.UkrainianAlphabet = true;
        vm.Key = "КЛЮЧ";
        vm.Text = "Ґанок і Їжак, Єнот! 🧪";
        string original = vm.Text;
        await ui.ActionAsync("encrypt");
        vm.Text = vm.Output;
        await ui.ActionAsync("decrypt");
        Assert.Equal(original, vm.Output);
        vm.Key = "КЛЮЧ!";
        await ui.ActionAsync("encrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task ManualCryptanalysisUpdatesPreviewAndPersistsItsMapping()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<AnalysisViewModel>();
        vm.Text = "ABAB ABA!";
        vm.Mappings.Single(r => r.Source == "A").Target = "X";
        vm.Mappings.Single(r => r.Source == "B").Target = "Y";
        await vm.PreviewTask;
        Assert.Equal("XYXY XYX!", vm.Output);
        Assert.Contains(vm.PreviewSegments, s => s.IsMapped);
        await ui.ActionAsync("analyze");
        await vm.PreviewTask;
        Assert.Contains("AB:", vm.NGrams);
        Assert.Contains("ABA:", vm.NGrams);
        Assert.Contains("7/7", vm.Statistics);
        await ui.ActionAsync("export-map");
        string mapping = ui.Files.Saves.Single().Text;
        await ui.ActionAsync("reset-map");
        await vm.PreviewTask;
        Assert.Equal("ABAB ABA!", vm.Output);
        ui.Files.TextImports.Enqueue(mapping);
        await ui.ActionAsync("import-map");
        await vm.PreviewTask;
        Assert.Equal("XYXY XYX!", vm.Output);
        vm.Mappings.Single(r => r.Source == "B").Target = "X";
        await vm.PreviewTask;
        Assert.True(vm.HasDuplicates);
        await ui.ActionAsync("baseline");
        await vm.PreviewTask;
        Assert.False(vm.HasDuplicates);
        Assert.NotEmpty(vm.Output);
        await ui.ActionAsync("caesar-rank");
        Assert.Contains("shift=", vm.NGrams);
    }

    [AvaloniaFact]
    public async Task OtpKeyAndCiphertextFilesRoundTripUnicodeAndRejectBadInputs()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<OtpViewModel>();
        vm.Text = "ҐЄІЇ 🧪\nbytes ≠ characters";
        string original = vm.Text;
        await ui.ActionAsync("generate");
        Assert.Equal(Encoding.UTF8.GetByteCount(original), Convert.FromBase64String(vm.Key).Length);
        await ui.ActionAsync("export-key");
        string exportedKey = ui.Files.Saves.Single().Text;
        await ui.ActionAsync("encrypt");
        string ciphertext = vm.Output;
        vm.Key = "";
        ui.Files.TextImports.Enqueue(exportedKey);
        await ui.ActionAsync("import-key");
        vm.Text = ciphertext;
        await ui.ActionAsync("decrypt");
        Assert.Equal(original, vm.Output);
        vm.Key = "AA==";
        await ui.ActionAsync("decrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
        vm.Text = "bad base64!";
        await ui.ActionAsync("decrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }
}
