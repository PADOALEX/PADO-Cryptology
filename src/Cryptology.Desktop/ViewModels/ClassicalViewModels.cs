using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Cryptology.Core.ClassicalCiphers;
using Cryptology.Core.Cryptanalysis;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;

namespace Cryptology.Desktop.ViewModels;

public sealed partial class FrequencyViewModel(Strings l, IFileService files) : ToolViewModel("Frequency", l, files)
{
    [ObservableProperty] private bool caseSensitive;
    [ObservableProperty] private bool spaces;
    [ObservableProperty] private bool punctuation;
    [ObservableProperty] private bool other;
    [ObservableProperty] private bool alphabetical;
    [ObservableProperty] private IReadOnlyList<FrequencyRow> rows = [];
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        var options = new FrequencyOptions { CaseSensitive = CaseSensitive, IncludeSpaces = Spaces,
            IncludePunctuation = Punctuation, IncludeNonAlphabet = Other, IncludeZeroCounts = true,
            Sort = Alphabetical ? FrequencySort.SymbolAscending : FrequencySort.CountDescending };
        var text = Text; var alphabet = Alphabet;
        var analysis = await Task.Run(() => FrequencyAnalyzer.Analyze(text, alphabet, options, ct), ct);
        Rows = analysis.Rows;
        Output = $"{L["Count"]}: {analysis.TotalSymbols}\n" + string.Join('\n', Rows.Select(r => $"{r.Symbol}\t{r.Count}\t{r.Percentage:F3}%\t{r.ReferencePercentage:F3}%"));
        if (action == "csv")
        {
            var csv = "Symbol,Count,Percentage,ReferencePercentage\n" + string.Join('\n', Rows.Select(r =>
                $"\"{r.Symbol.Replace("\"", "\"\"")}\",{r.Count},{r.Percentage.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{r.ReferencePercentage.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}"));
            if (await Files.SaveTextAsync(csv, "frequency.csv", ct)) Status = L["Saved"];
        }
    }
    protected override void Clear() { base.Clear(); Rows = []; }
}

public sealed partial class MappingRow(string source) : ObservableObject
{
    public string Source { get; } = source;
    [ObservableProperty] private string target = "";
    [ObservableProperty] private bool duplicate;
}

public sealed partial class ClassicalViewModel : ToolViewModel
{
    [ObservableProperty] private string shift = "3";
    [ObservableProperty] private bool useSubstitution;
    [ObservableProperty] private bool hasDuplicates;
    public ObservableCollection<MappingRow> Mappings { get; } = [];
    public ClassicalViewModel(Strings l, IFileService files) : base("Classical", l, files) => RebuildMapping();
    private void RebuildMapping()
    {
        foreach (var row in Mappings) row.PropertyChanged -= RowChanged;
        Mappings.Clear();
        foreach (var symbol in Alphabet.Symbols) { var row = new MappingRow(symbol.ToString()) { Target = symbol.ToString() }; row.PropertyChanged += RowChanged; Mappings.Add(row); }
        CheckDuplicates();
    }
    protected override void AlphabetChanged() => RebuildMapping();
    private void RowChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(MappingRow.Target)) CheckDuplicates(); }
    private void CheckDuplicates()
    {
        var duplicates = Mappings.Where(r => r.Target.Length > 0).GroupBy(r => r.Target.ToUpperInvariant()).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        foreach (var row in Mappings) row.Duplicate = duplicates.Contains(row.Target.ToUpperInvariant());
        HasDuplicates = duplicates.Count > 0;
    }
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        if (action == "generate") { Apply(SubstitutionKey.Generate(Alphabet).Symbols); return; }
        if (action == "import-key") { var json = await Files.OpenTextAsync(ct); if (json is not null) Apply(SubstitutionKey.FromJson(json, Alphabet).Symbols); return; }
        if (action == "export-key") { await Files.SaveTextAsync(CreateKey().ToJson(), "substitution.json", ct); return; }
        var text = Text; var alphabet = Alphabet;
        if (UseSubstitution)
        {
            var key = CreateKey();
            Output = await Task.Run(() => action == "decrypt" ? SubstitutionCipher.Decrypt(text, key, ct) : SubstitutionCipher.Encrypt(text, key, ct), ct);
        }
        else
        {
            if (!long.TryParse(Shift, out var shift)) throw new ArgumentException(L["NumberError"]);
            Output = await Task.Run(() => action == "decrypt" ? CaesarCipher.Decrypt(text, shift, alphabet, ct) : CaesarCipher.Encrypt(text, shift, alphabet, ct), ct);
        }
    }
    private SubstitutionKey CreateKey() => SubstitutionKey.Create(string.Concat(Mappings.Select(r => r.Target)), Alphabet);
    private void Apply(string symbols) { for (var i = 0; i < Mappings.Count; i++) Mappings[i].Target = symbols[i].ToString(); }
}

public sealed partial class VigenereViewModel(Strings l, IFileService files) : ToolViewModel("Vigenere", l, files)
{
    [ObservableProperty] private string key = "LEMON";
    protected override void AlphabetChanged()
    {
        if (Key is "LEMON" or "ЛИМОН") Key = UkrainianAlphabet ? "ЛИМОН" : "LEMON";
    }
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        var text = Text; var key = Key; var alphabet = Alphabet;
        Output = await Task.Run(() => action == "decrypt" ? VigenereCipher.Decrypt(text, key, alphabet, ct) : VigenereCipher.Encrypt(text, key, alphabet, ct), ct);
    }
}

public sealed partial class AnalysisViewModel : ToolViewModel
{
    public ObservableCollection<MappingRow> Mappings { get; } = [];
    [ObservableProperty] private IReadOnlyList<PreviewSegment> previewSegments = [];
    [ObservableProperty] private string nGrams = "";
    [ObservableProperty] private string statistics = "";
    [ObservableProperty] private bool hasDuplicates;
    private bool applying;
    private CancellationTokenSource? previewCancellation;
    public Task PreviewTask { get; private set; } = Task.CompletedTask;
    public AnalysisViewModel(Strings l, IFileService files) : base("Analysis", l, files) { Rebuild(); PropertyChanged += Changed; }
    private void Changed(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(Text)) SchedulePreview(); }
    private void Rebuild()
    {
        foreach (var row in Mappings) row.PropertyChanged -= MappingChanged;
        Mappings.Clear();
        foreach (var c in Alphabet.Symbols) { var row = new MappingRow(c.ToString()); row.PropertyChanged += MappingChanged; Mappings.Add(row); }
        SchedulePreview();
    }
    protected override void AlphabetChanged() => Rebuild();
    private void MappingChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(MappingRow.Target) && !applying) SchedulePreview(); }
    private Dictionary<string, string> Map() => Mappings.Where(r => r.Target.Length > 0).ToDictionary(r => r.Source, r => r.Target);
    private void SchedulePreview()
    {
        previewCancellation?.Cancel(); previewCancellation?.Dispose();
        previewCancellation = new CancellationTokenSource();
        PreviewTask = UpdatePreviewAsync(previewCancellation.Token);
    }
    private async Task UpdatePreviewAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(120, ct);
            var map = Map(); var text = Text; var alphabet = Alphabet;
            var preview = await Task.Run(() => CryptanalysisService.Preview(text, alphabet, map, ct), ct);
            ct.ThrowIfCancellationRequested();
            Output = preview.Text; PreviewSegments = preview.Segments;
            HasDuplicates = preview.DuplicateTargets.Count > 0;
            foreach (var row in Mappings) row.Duplicate = preview.DuplicateTargets.Contains(row.Target.ToUpperInvariant());
            var s = preview.Statistics;
            Statistics = $"{L["Count"]}: {s.TotalSymbols} · {s.MappedSymbols}/{s.TotalSymbols} · {s.CharacterCoverage:P1} · {s.MappedUniqueSymbols}/{s.UniqueSymbols}";
            Status = "";
        }
        catch (OperationCanceledException) { }
        catch (ArgumentException e) { Status = L["Error"] + ": " + e.Message; }
    }
    protected override async Task ExecuteAsync(string action, CancellationToken ct)
    {
        if (action == "reset-map") { Apply(new Dictionary<string, string>()); return; }
        if (action == "import-map") { var json = await Files.OpenTextAsync(ct); if (json is not null) Apply(CryptanalysisService.MappingFromJson(json, Alphabet)); return; }
        if (action == "export-map") { await Files.SaveTextAsync(CryptanalysisService.MappingToJson(Map(), Alphabet), "analysis-mapping.json", ct); return; }
        var text = Text; var alphabet = Alphabet;
        if (action == "baseline") { Apply(await Task.Run(() => CryptanalysisService.BaselineFrequencyGuess(text, alphabet, ct), ct)); return; }
        if (action == "caesar-rank")
        {
            var candidates = await Task.Run(() => CryptanalysisService.RankCaesar(text, alphabet, ct), ct);
            NGrams = string.Join("\n\n", candidates.Take(5).Select(c => $"shift={c.Shift} · χ²={c.Score:F3}\n{c.Plaintext[..Math.Min(300, c.Plaintext.Length)]}"));
            return;
        }
        var grams = await Task.Run(() => (Bi: NGramAnalyzer.Analyze(text, alphabet, 2, cancellationToken: ct), Tri: NGramAnalyzer.Analyze(text, alphabet, 3, cancellationToken: ct)), ct);
        NGrams = string.Join("\n", grams.Bi.Take(15).Select(g => $"{g.NGram}: {g.Count}")) + "\n\n" + string.Join("\n", grams.Tri.Take(15).Select(g => $"{g.NGram}: {g.Count}"));
        SchedulePreview();
    }
    private void Apply(IReadOnlyDictionary<string, string> map)
    {
        applying = true;
        try { foreach (var row in Mappings) row.Target = map.GetValueOrDefault(row.Source, ""); }
        finally { applying = false; }
        SchedulePreview();
    }
    protected override void Clear() { base.Clear(); NGrams = Statistics = ""; Apply(new Dictionary<string, string>()); }
    public override void Dispose() { previewCancellation?.Cancel(); previewCancellation?.Dispose(); PropertyChanged -= Changed; base.Dispose(); }
}
