using System.Text;
using Cryptology.Core.Models;

namespace Cryptology.Core.Cryptanalysis;

public enum FrequencySort { CountDescending, CountAscending, SymbolAscending, SymbolDescending }
public sealed record FrequencyOptions
{
    public bool CaseSensitive { get; init; }
    /// <summary>Includes every Unicode whitespace scalar, including line breaks and tabs.</summary>
    public bool IncludeSpaces { get; init; }
    public bool IncludePunctuation { get; init; }
    /// <summary>Includes digits, other alphabets, emoji and symbols; whitespace/punctuation have separate switches.</summary>
    public bool IncludeNonAlphabet { get; init; }
    public bool IncludeZeroCounts { get; init; }
    public FrequencySort Sort { get; init; } = FrequencySort.CountDescending;
}
public sealed record FrequencyRow(string Symbol, int Count, double Percentage, double ReferencePercentage, int ReferenceRank);
public sealed record FrequencyAnalysis(int TotalSymbols, IReadOnlyList<FrequencyRow> Rows);

public static class FrequencyAnalyzer
{
    public static FrequencyAnalysis Analyze(string text, Alphabet alphabet, FrequencyOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(alphabet);
        options ??= new FrequencyOptions();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        int total = 0;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune rune in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isLetter = alphabet.TryGetIndex(rune, out int index);
            bool include = isLetter || (Rune.IsWhiteSpace(rune) ? options.IncludeSpaces
                : Rune.IsPunctuation(rune) ? options.IncludePunctuation : options.IncludeNonAlphabet);
            if (!include) continue;
            string symbol = isLetter && !options.CaseSensitive ? alphabet.At(index).ToString() : rune.ToString();
            counts[symbol] = counts.GetValueOrDefault(symbol) + 1;
            total++;
        }
        if (options.IncludeZeroCounts)
        {
            foreach (Rune rune in alphabet.Symbols.EnumerateRunes())
            {
                counts.TryAdd(rune.ToString(), 0);
                if (options.CaseSensitive) counts.TryAdd(Rune.ToLowerInvariant(rune).ToString(), 0);
            }
        }
        double[] reference = ReferenceFrequencies.For(alphabet);
        var ranks = alphabet.FrequencyOrder.EnumerateRunes().Select((r, i) => (r, i)).ToDictionary(pair => pair.r, pair => pair.i + 1);
        var rows = counts.Select(pair =>
        {
            Rune rune = pair.Key.EnumerateRunes().First();
            bool inAlphabet = alphabet.TryGetIndex(rune, out int index);
            return new FrequencyRow(pair.Key, pair.Value, total == 0 ? 0 : 100.0 * pair.Value / total,
                inAlphabet ? reference[index] * 100 : 0, inAlphabet ? ranks[alphabet.At(index)] : 0);
        });
        IOrderedEnumerable<FrequencyRow> sorted = options.Sort switch
        {
            FrequencySort.CountDescending => rows.OrderByDescending(r => r.Count).ThenBy(r => r.Symbol, StringComparer.Ordinal),
            FrequencySort.CountAscending => rows.OrderBy(r => r.Count).ThenBy(r => r.Symbol, StringComparer.Ordinal),
            FrequencySort.SymbolAscending => rows.OrderBy(r => r.Symbol, StringComparer.Ordinal),
            FrequencySort.SymbolDescending => rows.OrderByDescending(r => r.Symbol, StringComparer.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(options), "Unknown frequency sort order.")
        };
        return new FrequencyAnalysis(total, sorted.ToArray());
    }
}

/// <summary>Publicly inspectable heuristic profile, not a trained language model.</summary>
public static class ReferenceFrequencies
{
    // Conventional illustrative English letter percentages; normalized below.
    private static readonly double[] EnglishWeights = [8.167, 1.492, 2.782, 4.253, 12.702, 2.228, 2.015, 6.094,
        6.966, 0.153, 0.772, 4.025, 2.406, 6.749, 7.507, 1.929, 0.095, 5.987, 6.327, 9.056,
        2.758, 0.978, 2.360, 0.150, 1.974, 0.074];

    /// <summary>Returns a fresh probability array in alphabet order.</summary>
    public static double[] For(Alphabet alphabet)
    {
        ArgumentNullException.ThrowIfNull(alphabet);
        double[] weights;
        if (alphabet.Id == "en") weights = (double[])EnglishWeights.Clone();
        else
        {
            // config.py supplied only a ranking. Rank-derived weights are explicitly heuristic.
            var rank = alphabet.FrequencyOrder.EnumerateRunes().Select((r, i) => (r, i))
                .ToDictionary(pair => pair.r, pair => pair.i + 1);
            weights = alphabet.Symbols.EnumerateRunes().Select(r => 1.0 / Math.Pow(rank[r], 0.8)).ToArray();
        }
        double total = weights.Sum();
        return weights.Select(w => w / total).ToArray();
    }
}
