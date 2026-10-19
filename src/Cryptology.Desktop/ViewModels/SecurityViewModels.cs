using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Cryptology.Core.Asymmetric;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;
using Cryptology.Security;

namespace Cryptology.Desktop.ViewModels;

public sealed partial class OtpViewModel(Strings l, IFileService files) : ToolViewModel("Otp", l, files)
{
    [ObservableProperty] private string key = "";
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        if (action == "generate")
        {
            var plaintextForKey = Text;
            var generated = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var bytes = ByteOneTimePad.GenerateKeyForText(plaintextForKey);
                try { ct.ThrowIfCancellationRequested(); return ByteOneTimePad.EncodeKey(bytes); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }, ct);
            ct.ThrowIfCancellationRequested();
            Key = generated;
            return;
        }
        if (action == "import-key") { var s = await Files.OpenTextAsync(ct); if (s is not null) Key = s.Trim(); return; }
        if (action == "export-key") { await Files.SaveTextAsync(Key, "otp-secret-key.txt", ct); return; }
        var text = Text; var key = Key;
        var result = await Task.Run(() => action == "decrypt" ? ByteOneTimePad.DecryptFromBase64(text, key) : ByteOneTimePad.EncryptToBase64(text, key), ct);
        ct.ThrowIfCancellationRequested();
        Output = result;
    }
    protected override void Clear() { base.Clear(); Key = ""; }
}

public sealed partial class RsaViewModel : ToolViewModel
{
    private readonly RsaSession rsa;
    public RsaViewModel(Strings l, IFileService files, RsaSession rsa) : base("Rsa", l, files)
    { this.rsa = rsa; rsa.KeyChanged += KeyChanged; }
    private void KeyChanged(object? sender, EventArgs e) => PublicKey = rsa.Required.ExportPublicPem();
    [ObservableProperty] private bool modernMode = true;
    [ObservableProperty] private string p = "61";
    [ObservableProperty] private string q = "53";
    [ObservableProperty] private string e = "17";
    [ObservableProperty] private string publicKey = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool privateConsent;
    private EducationalRsaKey? educational;
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        if (action == "textbook-key")
        {
            var p = Integer(P); var q = Integer(Q); var e = Integer(E);
            educational = await Task.Run(() => EducationalRsa.CreateKey(p, q, e, 32, ct), ct);
            Output = $"n = p × q = {educational.N}\nφ(n) = (p−1)(q−1) = {educational.Phi}\ne = {educational.E}\nd = e⁻¹ mod φ(n) = {educational.D}";
            return;
        }
        if (action == "generate") { await rsa.ChangeAsync(() => ModernRsaService.Generate(), ct); PublicKey = rsa.Required.ExportPublicPem(); Output = $"RSA-{rsa.Required.KeySize} · OAEP-SHA256\nUTF-8 ≤ {rsa.Required.MaxPlaintextBytes} bytes"; return; }
        if (action == "import-public" || action == "import-private")
        {
            var path = await Files.PickFileAsync(); if (path is null) return;
            var password = Password;
            try
            {
                if (action == "import-public") await rsa.ChangeAsync(token => ModernRsaService.ImportPublicPemFileAsync(path, token), ct);
                else await rsa.ChangeAsync(token => ModernRsaService.ImportEncryptedPrivatePemFileAsync(path, password, token), ct);
            }
            finally { Password = ""; }
            return;
        }
        if (action == "export-public") { await Files.SaveTextAsync(rsa.Required.ExportPublicPem(), "rsa-public.pem", ct); return; }
        if (action == "export-private")
        {
            var password = Password; var consent = PrivateConsent;
            try
            {
                var encrypted = await rsa.UseAsync(key => Task.Run(() => key.ExportEncryptedPrivatePem(password, consent), ct), ct);
                await Files.SaveTextAsync(encrypted, "rsa-private.encrypted.pem", ct);
            }
            finally { Password = ""; PrivateConsent = false; }
            return;
        }
        var text = Text;
        if (ModernMode)
            Output = await rsa.UseAsync(key => Task.Run(() => action == "decrypt" ? key.DecryptUtf8FromBase64(text) : key.EncryptUtf8ToBase64(text), ct), ct);
        else
        {
            var key = educational ?? throw new InvalidOperationException(L["NoEducationalKey"]);
            // Changing the fields never silently reuses a stale educational key.
            if (Integer(P) != key.P || Integer(Q) != key.Q || Integer(E) != key.E) throw new InvalidOperationException(L["NoEducationalKey"]);
            var value = Integer(text);
            Output = (await Task.Run(() => action == "decrypt" ? EducationalRsa.Decrypt(value, key, ct) : EducationalRsa.Encrypt(value, key, ct), ct)).ToString();
        }
    }
    protected override void Clear() { base.Clear(); Password = ""; PrivateConsent = false; }
    public override void Dispose() { rsa.KeyChanged -= KeyChanged; base.Dispose(); }
}

public sealed partial class SignatureViewModel : ToolViewModel
{
    private readonly RsaSession rsa;
    public SignatureViewModel(Strings l, IFileService files, RsaSession rsa) : base("Signature", l, files)
    {
        this.rsa = rsa;
        rsa.KeyChanged += KeyChanged;
        PropertyChanged += InputsChanged;
    }
    private void KeyChanged(object? sender, EventArgs e) => Output = "";
    private void InputsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Text) or nameof(Signature) or nameof(FilePath) or nameof(ModernMode) or nameof(P) or nameof(Q) or nameof(E)) Output = "";
    }
    [ObservableProperty] private bool modernMode = true;
    [ObservableProperty] private string p = "61";
    [ObservableProperty] private string q = "53";
    [ObservableProperty] private string e = "17";
    [ObservableProperty] private string filePath = "";
    [ObservableProperty] private string signature = "";
    private EducationalRsaKey? educational;
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        if (action == "choose-file") { var path = await Files.PickFileAsync(); if (path is not null) FilePath = path; return; }
        if (action == "text-mode") { FilePath = ""; return; }
        if (action == "import-signature") { var s = await Files.OpenTextAsync(ct); if (s is not null) Signature = s.Trim(); return; }
        if (action == "export-signature") { await Files.SaveTextAsync(Signature, "signature.txt", ct); return; }
        if (action == "generate") { await rsa.ChangeAsync(() => ModernRsaService.Generate(), ct); Output = "RSA-3072 · RSA-PSS · SHA-256"; return; }
        if (action == "textbook-key")
        {
            var p = Integer(P); var q = Integer(Q); var e = Integer(E);
            educational = await Task.Run(() => EducationalRsa.CreateKey(p, q, e, 32, ct), ct);
            Output = $"n={educational.N}; φ={educational.Phi}; e={educational.E}; d={educational.D}\nSHA-256 mod n · textbook RSA";
            return;
        }
        var text = Text; var file = FilePath; var signature = Signature;
        if (ModernMode)
        {
            if (action == "sign") Signature = await rsa.UseAsync(key => file.Length == 0 ? Task.Run(() => key.SignUtf8ToBase64(text), ct) : key.SignFileAsync(file, ct), ct);
            else Output = L[await rsa.UseAsync(key => file.Length == 0 ? Task.Run(() => key.VerifyUtf8FromBase64(text, signature), ct) : key.VerifyFileAsync(file, signature, ct), ct) ? "Valid" : "Invalid"];
        }
        else
        {
            var key = educational ?? throw new InvalidOperationException(L["NoEducationalKey"]);
            if (Integer(P) != key.P || Integer(Q) != key.Q || Integer(E) != key.E) throw new InvalidOperationException(L["NoEducationalKey"]);
            byte[] data;
            if (file.Length == 0) data = Encoding.UTF8.GetBytes(text);
            else
            {
                // Raw educational signing consumes the file bytes; cap it, while modern mode streams arbitrary files.
                if (new FileInfo(file).Length > 32 * 1024 * 1024) throw new ArgumentException(L["TooLarge"]);
                data = await File.ReadAllBytesAsync(file, ct);
            }
            try
            {
                if (action == "sign") Signature = (await Task.Run(() => EducationalRsa.SignData(data, key, ct), ct)).ToString();
                else { var value = Integer(signature); Output = L[await Task.Run(() => EducationalRsa.VerifyData(data, value, key, ct), ct) ? "Valid" : "Invalid"]; }
            }
            finally { CryptographicOperations.ZeroMemory(data); }
        }
    }
    protected override void Clear() { base.Clear(); Signature = FilePath = ""; }
    public override void Dispose() { rsa.KeyChanged -= KeyChanged; PropertyChanged -= InputsChanged; base.Dispose(); }
}
