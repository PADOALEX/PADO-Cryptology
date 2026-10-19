using System.Text;
using Cryptology.Core.Models;

namespace Cryptology.Core.Cryptanalysis;

public sealed record NGramRow(string NGram, int Count, double Percentage)
{
    public string Symbol => NGram;
}

public static class NGramAnalyzer
{
    /// <summary>Overlapping windows. Default breaks at every nonalphabet symbol, matching legacy words.</summary>
    public static IReadOnlyList<NGramRow> Analyze(string text, Alphabet alphabet, int n, bool crossWordBoundaries = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(alphabet);
        if (n < 1 || n > 32) throw new ArgumentOutOfRangeException(nameof(n), "Choose an n-gram length from 1 to 32.");
        var window = new Queue<string>(n);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        int total = 0;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune rune in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!alphabet.TryGetIndex(rune, out int index))
            {
                if (!crossWordBoundaries) window.Clear();
                continue;
            }
            window.Enqueue(alphabet.At(index).ToString());
            if (window.Count > n) window.Dequeue();
            if (window.Count != n) continue;
            string gram = string.Concat(window);
            counts[gram] = counts.GetValueOrDefault(gram) + 1;
            total++;
        }
        return counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new NGramRow(p.Key, p.Value, 100.0 * p.Value / total)).ToArray();
    }
}
