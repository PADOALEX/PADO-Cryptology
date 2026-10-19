using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Cryptology.Core.ClassicalCiphers;
using Cryptology.Core.Models;

namespace Cryptology.Core.Cryptanalysis;

public sealed record CaesarCandidate(int Shift, double Score, string Plaintext);
public sealed record PreviewSegment(string Text, bool IsMapped, bool IsAlphabet);
public sealed record PreviewStatistics(int TotalSymbols, int UniqueSymbols, int MappedSymbols, int MappedUniqueSymbols)
{
    public double CharacterCoverage => TotalSymbols == 0 ? 0 : (double)MappedSymbols / TotalSymbols;
    public double UniqueCoverage => UniqueSymbols == 0 ? 0 : (double)MappedUniqueSymbols / UniqueSymbols;
}
public sealed record SubstitutionPreview(string Text, IReadOnlyList<PreviewSegment> Segments,
    PreviewStatistics Statistics, IReadOnlyList<string> DuplicateTargets);

public static class CryptanalysisService
{
    /// <summary>The original frequency-order guess, with ordinal tie-breaking for reproducibility.</summary>
    public static IReadOnlyDictionary<string, string> BaselineFrequencyGuess(string text, Alphabet alphabet, CancellationToken cancellationToken = default)
    {
        var frequencies = FrequencyAnalyzer.Analyze(text, alphabet, cancellationToken: cancellationToken);
        string[] reference = alphabet.FrequencyOrder.EnumerateRunes().Select(r => r.ToString()).ToArray();
        var map = frequencies.Rows.Select((row, index) => (row.Symbol, Replacement: reference[index]))
            .ToDictionary(pair => pair.Symbol, pair => pair.Replacement, StringComparer.Ordinal);
        return new ReadOnlyDictionary<string, string>(map);
    }

    /// <summary>Ranks all Caesar keys by chi-square; lower is better. Not a general substitution solver.</summary>
    public static IReadOnlyList<CaesarCandidate> RankCaesar(string text, Alphabet alphabet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(alphabet);
        var counts = new int[alphabet.Length];
        int total = 0;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune rune in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (alphabet.TryGetIndex(rune, out int index)) { counts[index]++; total++; }
        }
        if (total == 0) throw new ArgumentException("The text contains no letters from the selected alphabet.", nameof(text));
        double[] expected = ReferenceFrequencies.For(alphabet).Select(p => p * total).ToArray();
        var candidates = new List<CaesarCandidate>(alphabet.Length);
        for (int shift = 0; shift < alphabet.Length; shift++)
        {
            double score = 0;
            for (int i = 0; i < alphabet.Length; i++)
            {
                double difference = counts[(i + shift) % alphabet.Length] - expected[i];
                score += difference * difference / expected[i];
            }
            candidates.Add(new CaesarCandidate(shift, score, CaesarCipher.Decrypt(text, shift, alphabet, cancellationToken)));
        }
        return candidates.OrderBy(c => c.Score).ThenBy(c => c.Shift).ToArray();
    }

    public static SubstitutionPreview Preview(string text, Alphabet alphabet, IReadOnlyDictionary<string, string> mapping, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var normalized = NormalizeMapping(mapping, alphabet);
        string[] duplicates = normalized.GroupBy(p => p.Value, StringComparer.Ordinal).Where(g => g.Count() > 1)
            .Select(g => g.Key).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        var segments = new List<PreviewSegment>();
        var output = new StringBuilder(text.Length);
        var chunk = new StringBuilder();
        bool? lastMapped = null, lastAlphabet = null;
        var unique = new HashSet<string>(StringComparer.Ordinal);
        var mappedUnique = new HashSet<string>(StringComparer.Ordinal);
        int total = 0, mapped = 0;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune rune in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool inAlphabet = alphabet.TryGetIndex(rune, out int index);
            string symbol = inAlphabet ? alphabet.At(index).ToString() : rune.ToString();
            bool isMapped = inAlphabet && normalized.ContainsKey(symbol);
            string result = rune.ToString();
            if (inAlphabet)
            {
                total++;
                unique.Add(symbol);
                if (isMapped)
                {
                    mapped++;
                    mappedUnique.Add(symbol);
                    Rune replacement = normalized[symbol].EnumerateRunes().First();
                    result = (Rune.IsLower(rune) ? Rune.ToLowerInvariant(replacement) : replacement).ToString();
                }
            }
            if (lastMapped.HasValue && (lastMapped != isMapped || lastAlphabet != inAlphabet))
            {
                segments.Add(new PreviewSegment(chunk.ToString(), lastMapped.Value, lastAlphabet!.Value));
                chunk.Clear();
            }
            chunk.Append(result);
            output.Append(result);
            lastMapped = isMapped;
            lastAlphabet = inAlphabet;
        }
        if (chunk.Length > 0) segments.Add(new PreviewSegment(chunk.ToString(), lastMapped!.Value, lastAlphabet!.Value));
        return new SubstitutionPreview(output.ToString(), segments.ToArray(), new PreviewStatistics(total, unique.Count, mapped, mappedUnique.Count), duplicates);
    }

    public static string MappingToJson(IReadOnlyDictionary<string, string> mapping, Alphabet alphabet)
    {
        var normalized = NormalizeMapping(mapping, alphabet);
        return JsonSerializer.Serialize(new MappingDocument(1, alphabet.Id, normalized), new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Validates the whole document before returning it. Duplicate targets remain visible draft diagnostics.</summary>
    public static IReadOnlyDictionary<string, string> MappingFromJson(string json, Alphabet alphabet)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(alphabet);
        var document = JsonSerializer.Deserialize<MappingDocument>(json) ?? throw new FormatException("Empty mapping document.");
        if (document.Version != 1) throw new FormatException("Unsupported mapping document version.");
        if (document.Alphabet != alphabet.Id) throw new ArgumentException("The mapping uses a different alphabet.", nameof(json));
        return new ReadOnlyDictionary<string, string>(NormalizeMapping(document.Mapping, alphabet));
    }

    private static Dictionary<string, string> NormalizeMapping(IReadOnlyDictionary<string, string> mapping, Alphabet alphabet)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(alphabet);
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in mapping)
        {
            string source = alphabet.NormalizeSymbol(pair.Key);
            if (string.IsNullOrEmpty(pair.Value)) continue; // Empty editor cells mean unmapped.
            string target = alphabet.NormalizeSymbol(pair.Value);
            if (!normalized.TryAdd(source, target)) throw new ArgumentException($"Duplicate source letter '{source}' after case normalization.", nameof(mapping));
        }
        return normalized;
    }
    private sealed record MappingDocument(int Version, string Alphabet, Dictionary<string, string> Mapping);
}
