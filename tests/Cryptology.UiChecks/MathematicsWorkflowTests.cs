using System.Numerics;
using System.Text.Json.Nodes;
using Avalonia.Headless.XUnit;
using Cryptology.Desktop.ViewModels;

namespace Cryptology.UiChecks;

public sealed class MathematicsWorkflowTests
{
    [AvaloniaFact]
    public async Task ArithmeticButtonsShowEuclidPowerAndRandomRangeResults()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<MathViewModel>();
        vm.A = "240"; vm.B = "46";
        await ui.ActionAsync("euclid");
        Assert.Contains("gcd(240, 46) = 2", vm.Output);
        Assert.Contains("240 × (-9) + 46 × (47) = 2", vm.Output);
        vm.A = "4"; vm.B = "497"; vm.Exponent = "13";
        await ui.ActionAsync("power");
        Assert.StartsWith("4^13 mod 497 = 445", vm.Output);
        vm.A = "17"; vm.B = "17";
        await ui.ActionAsync("random"); Assert.Equal("17", vm.Output);
        await ui.ActionAsync("prime"); Assert.Equal("17", vm.Output);
        vm.B = "2";
        await ui.ActionAsync("coprime"); Assert.Equal("1", vm.Output);
        vm.A = "not an integer";
        await ui.ActionAsync("euclid");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task TheoryButtonsDistinguishJacobiProbablePrimeAndComposite()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<TheoryViewModel>();
        vm.A = "5"; vm.N = "11";
        await ui.ActionAsync("jacobi");
        Assert.Equal("(5/11) = 1", vm.Output);
        vm.N = "561";
        await ui.ActionAsync("miller-rabin");
        Assert.StartsWith(ui.Strings["Composite"], vm.Output);
        vm.N = "13";
        await ui.ActionAsync("solovay");
        Assert.StartsWith(ui.Strings["ProbablyPrime"], vm.Output);
        vm.Bits = "16";
        await ui.ActionAsync("generate");
        Assert.InRange(BigInteger.Parse(vm.N), new BigInteger(32768), new BigInteger(65535));
        vm.Rounds = "0";
        await ui.ActionAsync("miller-rabin");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task LongPrimeGenerationCanBeCancelledThroughCommand()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<TheoryViewModel>();
        vm.Bits = "4096"; vm.Rounds = "256";
        vm.RunCommand.Execute("generate");
        Assert.True(vm.IsBusy);
        Assert.False(vm.RunCommand.CanExecute("generate"));
        vm.CancelCommand.Execute(null);
        Assert.NotNull(vm.RunCommand.ExecutionTask);
        await vm.RunCommand.ExecutionTask;
        Assert.False(vm.IsBusy);
        Assert.Equal(ui.Strings["Cancelled"], vm.Status);
    }

    [AvaloniaFact]
    public async Task ShamirFilesPreserveMetadataAndRecoverFromThresholdSubset()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<ShamirViewModel>();
        vm.Secret = "1234"; vm.Threshold = "3"; vm.ShareCount = "5"; vm.Prime = "2089";
        await ui.ActionAsync("split");
        Assert.NotEmpty(vm.ShareJson);
        await ui.ActionAsync("export-shares");
        string exported = ui.Files.Saves.Single().Text;
        vm.ShareJson = "";
        ui.Files.TextImports.Enqueue(exported);
        await ui.ActionAsync("import-shares");
        var document = JsonNode.Parse(vm.ShareJson)!.AsObject();
        var shares = document["shares"]!.AsArray();
        shares.RemoveAt(3); shares.RemoveAt(1);
        vm.ShareJson = document.ToJsonString();
        await ui.ActionAsync("recover");
        Assert.StartsWith("s = 1234", vm.Output);
        Assert.Contains("Lᵢ(0)=", vm.Output);
        shares.RemoveAt(0);
        vm.ShareJson = document.ToJsonString();
        await ui.ActionAsync("recover");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
        vm.ShareJson = "invalid JSON";
        await ui.ActionAsync("recover");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }

    [AvaloniaFact]
    public async Task TextbookRsaValidatesKeysAndRefusesStaleEditedParameters()
    {
        using var ui = new UiHarness();
        var vm = ui.Select<RsaViewModel>();
        vm.ModernMode = false;
        await ui.ActionAsync("textbook-key");
        Assert.Contains("n = p × q = 3233", vm.Output);
        vm.Text = "65";
        await ui.ActionAsync("encrypt"); Assert.Equal("2790", vm.Output);
        vm.Text = vm.Output;
        await ui.ActionAsync("decrypt"); Assert.Equal("65", vm.Output);
        vm.P = "67";
        await ui.ActionAsync("encrypt");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
        vm.P = vm.Q;
        await ui.ActionAsync("textbook-key");
        Assert.StartsWith(ui.Strings["Error"], vm.Status);
    }
}
