using Avalonia.Headless.XUnit;
using Cryptology.Desktop.ViewModels;
using Cryptology.Security;

namespace Cryptology.UiChecks;

public sealed class SecurityWorkflowTests
{
    [AvaloniaFact]
    public async Task ModernRsaGeneratesEncryptsExportsPublicKeyAndRejectsWrongKey()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<RsaViewModel>();
        await ui.ActionAsync("generate");
        Assert.Equal(3072, ui.Rsa.Required.KeySize);
        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", vm.PublicKey);
        vm.Text = "Українське повідомлення 🧪";
        string original = vm.Text;
        await ui.ActionAsync("encrypt");
        string ciphertext = vm.Output;
        vm.Text = ciphertext;
        await ui.ActionAsync("decrypt");
        Assert.Equal(original, vm.Output);
        await ui.ActionAsync("export-public");
        Assert.Equal(vm.PublicKey, ui.Files.Saves.Single().Text);
        vm.Text = new string('a', 319);
        await ui.ActionAsync("encrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
        await ui.Rsa.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        vm.Text = ciphertext;
        await ui.ActionAsync("decrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task PrivateExportRequiresConsentAndClearsPasswordAfterEachAttempt()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<RsaViewModel>();
        await ui.Rsa.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        vm.Password = "test-only long passphrase";
        await ui.ActionAsync("export-private");
        Assert.Empty(ui.Files.Saves);
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
        Assert.Equal("", vm.Password);
        Assert.False(vm.PrivateConsent);
        vm.Password = "test-only long passphrase"; vm.PrivateConsent = true;
        await ui.ActionAsync("export-private");
        Assert.StartsWith("-----BEGIN ENCRYPTED PRIVATE KEY-----", ui.Files.Saves.Single().Text);
        Assert.Equal("", vm.Password);
        Assert.False(vm.PrivateConsent);
    }

    [AvaloniaFact]
    public async Task SignatureFileCanBeSignedSavedLoadedVerifiedAndRejectedAfterTampering()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<SignatureViewModel>();
        await ui.Rsa.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, [0, 255, 10, 13, 0, 128]);
            ui.Files.FilePicks.Enqueue(path);
            await ui.ActionAsync("choose-file");
            Assert.Equal(path, vm.FilePath);
            await ui.ActionAsync("sign");
            Assert.NotEmpty(vm.Signature);
            await ui.ActionAsync("export-signature");
            string saved = ui.Files.Saves.Single().Text;
            vm.Signature = "";
            ui.Files.TextImports.Enqueue(saved);
            await ui.ActionAsync("import-signature");
            await ui.ActionAsync("verify");
            Assert.Equal(ui.Strings["Valid"], vm.Output);
            await File.AppendAllTextAsync(path, "changed");
            await ui.ActionAsync("verify");
            Assert.Equal(ui.Strings["Invalid"], vm.Output);
            await ui.Rsa.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
            await ui.ActionAsync("verify");
            Assert.Equal(ui.Strings["Invalid"], vm.Output);
            vm.Signature = "malformed!";
            await ui.ActionAsync("verify");
            Assert.Equal(ui.Strings["Invalid"], vm.Output);
            vm.FilePath = path + ".missing";
            await ui.ActionAsync("sign");
            Assert.StartsWith(ui.Strings["Error"], vm.Status);
        }
        finally { File.Delete(path); }
    }

    [AvaloniaFact]
    public async Task SignaturesShareCurrentKeyAndRsaPublicKeyDisplayStaysCurrent()
    {
        using var ui = new UiHarness();
        var rsa = ui.Select<RsaViewModel>();
        await ui.ActionAsync("generate");
        string oldPublic = rsa.PublicKey;
        var signatures = ui.Select<SignatureViewModel>();
        await ui.ActionAsync("generate");
        Assert.NotEqual(oldPublic, rsa.PublicKey);
        Assert.Equal(ui.Rsa.Required.ExportPublicPem(), rsa.PublicKey);
        signatures.Text = "same in-memory session";
        await ui.ActionAsync("sign");
        Assert.True(ui.Rsa.Required.VerifyUtf8FromBase64(signatures.Text, signatures.Signature));
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Valid"], signatures.Output);
    }

    [AvaloniaFact]
    public async Task EducationalSignaturesAndPublicOnlyModernVerificationWork()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<SignatureViewModel>();
        vm.ModernMode = false;
        await ui.ActionAsync("textbook-key");
        vm.Text = "abc";
        await ui.ActionAsync("sign");
        Assert.Equal("1480", vm.Signature);
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Valid"], vm.Output);
        vm.Text += "!";
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Invalid"], vm.Output);
        vm.ModernMode = true;
        using var key = ModernRsaService.Generate(2048);
        vm.Signature = key.SignUtf8ToBase64(vm.Text);
        await ui.Rsa.ChangeAsync(() => ModernRsaService.ImportPublicPem(key.ExportPublicPem()), CancellationToken.None);
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Valid"], vm.Output);
        await ui.ActionAsync("sign");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task EditingVerifiedInputsOrReplacingTheKeyClearsTheOldVerificationResult()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<SignatureViewModel>();
        await ui.Rsa.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        vm.Text = "verified text";
        await ui.ActionAsync("sign");
        string signature = vm.Signature;
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Valid"], vm.Output);
        vm.Text = "changed text";
        Assert.Equal("", vm.Output);
        vm.Text = "verified text";
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Valid"], vm.Output);
        vm.Signature = "malformed!";
        Assert.Equal("", vm.Output);
        vm.Signature = signature;
        await ui.ActionAsync("verify");
        Assert.Equal(ui.Strings["Valid"], vm.Output);
        await ui.Rsa.ChangeAsync(() => ModernRsaService.Generate(2048), CancellationToken.None);
        Assert.Equal("", vm.Output);
    }
}
