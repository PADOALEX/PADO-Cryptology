using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Cryptology.Desktop.ViewModels;

namespace Cryptology.UiChecks;

public sealed class WindowTests
{
    [AvaloniaFact]
    public void EveryModuleRendersAndSurvivesRepeatedNavigation()
    {
        using var ui = new UiHarness();
        Assert.Equal(10, ui.Shell.Tools.Count);
        var navigation = ui.Find<ListBox>("ModuleNavigation");
        var host = ui.Find<ContentControl>("ToolHost");
        for (int pass = 0; pass < 4; pass++)
        {
            foreach (var tool in ui.Shell.Tools)
            {
                navigation.SelectedItem = tool;
                Dispatcher.UIThread.RunJobs();
                Assert.Same(tool, ui.Shell.SelectedTool);
                Assert.StartsWith(tool.Id, Assert.IsAssignableFrom<Control>(host.Content).Name!);
                Assert.NotNull(ui.Find<TextBox>("OutputText"));
                Assert.NotNull(ui.Find<TextBlock>("StatusMessage"));
                if (pass == 0) ui.CaptureIfRequested(tool.Id.ToLowerInvariant());
            }
        }
    }

    [AvaloniaFact]
    public async Task ActualTextInputAndBoundEncryptButtonProduceVisibleCiphertext()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<ClassicalViewModel>();
        var input = ui.Find<TextBox>("InputText");
        Assert.True(input.Focus());
        ui.Window.KeyTextInput("Attack at dawn!");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Attack at dawn!", vm.Text);
        ui.Find<TextBox>("ShiftField").Text = "3";
        await ui.ActionAsync("encrypt");
        Assert.Equal("Dwwdfn dw gdzq!", vm.Output);
        Assert.Equal(vm.Output, ui.Find<TextBox>("OutputText").Text);
        ui.CaptureIfRequested("caesar-typed");
    }

    [AvaloniaFact]
    public void LanguageAndThemeControlsUpdateWithoutChangingCipherAlphabet()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<ClassicalViewModel>();
        ui.Find<CheckBox>("UkrainianAlphabetToggle").IsChecked = true;
        Assert.True(vm.UkrainianAlphabet);
        Assert.Equal(33, vm.Mappings.Count);
        string englishTitle = vm.Title;
        ui.Find<CheckBox>("UiLanguageToggle").IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(ui.Strings.Ukrainian);
        Assert.NotEqual(englishTitle, vm.Title);
        Assert.True(vm.UkrainianAlphabet);
        Assert.Equal(33, vm.Mappings.Count);
        Assert.Contains(vm.Mappings, row => row.Source == "Ґ");
        Assert.Equal("Зашифрувати", ui.Find<Button>("encryptButton").Content);
        ui.Find<CheckBox>("ThemeToggle").IsChecked = false;
        Assert.False(ui.Shell.DarkTheme);
        Assert.Equal(ThemeVariant.Light, ui.Window.RequestedThemeVariant);
        ui.CaptureIfRequested("ukrainian-light");
        ui.Find<CheckBox>("ThemeToggle").IsChecked = true;
        Assert.Equal(ThemeVariant.Dark, ui.Window.RequestedThemeVariant);
        ui.Find<CheckBox>("UiLanguageToggle").IsChecked = false;
        Assert.Equal(englishTitle, vm.Title);
        Assert.True(vm.UkrainianAlphabet);
    }

    [AvaloniaFact]
    public async Task ImportedTextCanBeEncryptedSavedReloadedCopiedAndCleared()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<VigenereViewModel>();
        const string text = "Imported text, Unicode 🧪\nsecond line";
        ui.Files.TextImports.Enqueue(text);
        await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(text, ui.Find<TextBox>("InputText").Text);
        await ui.ActionAsync("encrypt");
        string ciphertext = vm.Output;
        await vm.ExportCommand.ExecuteAsync(null);
        Assert.Equal(ciphertext, ui.Files.Saves.Single().Text);
        ui.Files.TextImports.Enqueue(ui.Files.Saves.Single().Text);
        await vm.ImportCommand.ExecuteAsync(null);
        await ui.ActionAsync("decrypt");
        Assert.Equal(text, vm.Output);
        await vm.CopyCommand.ExecuteAsync(null);
        Assert.Equal(text, ui.Files.Clipboard);
        vm.Text = "";
        await vm.PasteCommand.ExecuteAsync(null);
        Assert.Equal(text, vm.Text);
        vm.ClearCommand.Execute(null);
        Assert.Equal("", vm.Text);
        Assert.Equal("", vm.Output);
        Assert.Equal("", ui.Find<TextBox>("OutputText").Text);
    }

    [AvaloniaFact]
    public async Task LargeTextAndRepeatedNavigationPreserveViewModelResults()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<ClassicalViewModel>();
        string text = string.Concat(Enumerable.Repeat("Abc 🧪 ҐЄІЇ\n", 10_000));
        vm.Text = text;
        await ui.ActionAsync("encrypt");
        string encrypted = vm.Output;
        foreach (var tool in ui.Shell.Tools) ui.Shell.SelectedTool = tool;
        ui.Select<ClassicalViewModel>();
        Assert.Equal(text, vm.Text);
        Assert.Equal(encrypted, vm.Output);
        vm.Text = encrypted;
        await ui.ActionAsync("decrypt");
        Assert.Equal(text, vm.Output);
    }
}
