using System.Numerics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Cryptology.Core.NumberTheory;
using Cryptology.Core.SecretSharing;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;

namespace Cryptology.Desktop.ViewModels;

public sealed partial class MathViewModel(Strings l, IFileService files) : ToolViewModel("Math", l, files)
{
    [ObservableProperty] private string a = "240";
    [ObservableProperty] private string b = "46";
    [ObservableProperty] private string exponent = "13";
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        var a = action == "coprime" ? BigInteger.Zero : Integer(A); var b = Integer(B);
        var exponent = action == "power" ? Integer(Exponent) : BigInteger.Zero;
        Output = await Task.Run(() =>
        {
            switch (action)
            {
                case "random": return CryptoRandom.InRange(a, b, ct).ToString();
                case "prime": return PrimeGenerator.GenerateInRange(a, b, 32, ct).ToString();
                case "coprime": return CryptoRandom.Coprime(b, ct).ToString();
                case "power":
                    var power = IntegerMath.ModPow(a, exponent, b, true, ct);
                    return $"{a}^{exponent} mod {b} = {power.Value}\n\n" + string.Join('\n', power.Steps.Select(s => $"e={s.Exponent}, b={s.Base}, r={s.Accumulator}, ×={s.Multiplied}"))
                        + (power.TraceTruncated ? "\n… (1024 trace steps)" : "");
                default:
                    var result = IntegerMath.ExtendedGcd(a, b, true, ct);
                    var inverse = b > 1 && result.Gcd == 1 ? IntegerMath.ModInverse(a, b, ct).ToString() : "∄";
                    return $"gcd({a}, {b}) = {result.Gcd}\nx = {result.X}; y = {result.Y}\n{a} × ({result.X}) + {b} × ({result.Y}) = {result.Gcd}\na⁻¹ mod b = {inverse}\n\n"
                        + string.Join('\n', result.Steps.Select(s => $"{s.Dividend} = {s.Quotient} × {s.Divisor} + {s.Remainder}"))
                        + (result.TraceTruncated ? "\n… (1024 trace steps)" : "");
            }
        }, ct);
    }
}

public sealed partial class TheoryViewModel(Strings l, IFileService files) : ToolViewModel("Theory", l, files)
{
    [ObservableProperty] private string a = "1001";
    [ObservableProperty] private string n = "9907";
    [ObservableProperty] private string rounds = "32";
    [ObservableProperty] private string bits = "128";
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        var rounds = action == "jacobi" ? 32 : SmallInteger(Rounds, 1, 256);
        if (action == "generate")
        {
            var bits = SmallInteger(Bits, 2, 4096);
            N = (await Task.Run(() => PrimeGenerator.Generate(bits, rounds, ct), ct)).ToString();
            Output = N + $"\n{L["ProbablyPrime"]} · Miller–Rabin · {rounds} · P(error) ≤ 4^(-{rounds})";
            return;
        }
        var n = Integer(N);
        if (action == "jacobi")
        {
            var a = Integer(A);
            Output = $"({a}/{n}) = " + await Task.Run(() => JacobiSymbol.Calculate(a, n, ct), ct);
            return;
        }
        var solovay = action == "solovay";
        var probably = await Task.Run(() => solovay ? PrimalityTest.SolovayStrassen(n, rounds, ct) : PrimalityTest.MillerRabin(n, rounds, ct), ct);
        Output = L[probably ? "ProbablyPrime" : "Composite"] + $"\n{(solovay ? "Solovay–Strassen" : "Miller–Rabin")} · {rounds}\n"
            + (probably ? $"P(error) ≤ {(solovay ? 2 : 4)}^(-{rounds})" : "");
    }
}

public sealed partial class ShamirViewModel(Strings l, IFileService files) : ToolViewModel("Shamir", l, files)
{
    [ObservableProperty] private string secret = "1234";
    [ObservableProperty] private string threshold = "3";
    [ObservableProperty] private string shareCount = "5";
    [ObservableProperty] private string prime = "2089";
    [ObservableProperty] private string shareJson = "";
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        if (action == "import-shares") { var s = await Files.OpenTextAsync(ct); if (s is not null) { await Task.Run(() => ShamirSecretSharing.Import(s, 32, ct), ct); ShareJson = s; } return; }
        if (action == "export-shares") { var json = ShareJson; var normalized = await Task.Run(() => ShamirSecretSharing.Export(ShamirSecretSharing.Import(json, 32, ct), 32, ct), ct); await Files.SaveTextAsync(normalized, "shamir-shares.json", ct); return; }
        if (action == "split")
        {
            var secret = Integer(Secret); var p = Integer(Prime); var k = SmallInteger(Threshold, 1, 10000); var n = SmallInteger(ShareCount, 1, 10000);
            var set = await Task.Run(() => ShamirSecretSharing.Split(secret, k, n, p, 32, ct), ct);
            ShareJson = await Task.Run(() => ShamirSecretSharing.Export(set, 32, ct), ct);
            Output = $"f(x) = s + a₁x + … + aₖ₋₁xᵏ⁻¹ mod p\np = {p}; k = {k}; n = {n}\n\n" + string.Join('\n', set.Shares.Select(s => $"f({s.X}) = {s.Y}"));
        }
        else
        {
            var json = ShareJson;
            Output = await Task.Run(() =>
            {
                var set = ShamirSecretSharing.Import(json, 32, ct);
                var recovered = ShamirSecretSharing.Recover(set, 32, ct);
                var steps = new StringBuilder($"s = {recovered}\np = {set.Prime}\ns = Σ yᵢ × ∏(-xⱼ)/(xᵢ-xⱼ) mod p\n\n");
                var basis = set.Shares.Take(set.Threshold).ToArray();
                foreach (var point in basis)
                {
                    BigInteger weight = 1;
                    foreach (var other in basis)
                        if (point.X != other.X) weight = IntegerMath.Normalize(weight * -other.X * IntegerMath.ModInverse(point.X - other.X, set.Prime, ct), set.Prime);
                    steps.AppendLine($"x={point.X}: Lᵢ(0)={weight}; yᵢLᵢ(0) mod p={IntegerMath.Normalize(point.Y * weight, set.Prime)}");
                }
                return steps.ToString();
            }, ct);
        }
    }
    protected override void Clear() { base.Clear(); ShareJson = ""; }
}
