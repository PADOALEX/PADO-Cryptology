using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cryptology.Core.Models;

namespace Cryptology.Core.ClassicalCiphers;

/// <summary>Immutable complete permutation of one selected alphabet.</summary>
public sealed class SubstitutionKey
{
    public Alphabet Alphabet { get; }
    public string Symbols { get; }
    internal int[] Forward { get; }
    internal int[] Reverse { get; }

    private SubstitutionKey(string symbols, Alphabet alphabet, int[] forward)
    {
        Alphabet = alphabet;
        Symbols = symbols;
        Forward = forward;
        Reverse = new int[forward.Length];
        for (int i = 0; i < forward.Length; i++) Reverse[forward[i]] = i;
    }

    public static SubstitutionKey Create(string symbols, Alphabet alphabet)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(alphabet);
        Rune[] runes = symbols.EnumerateRunes().ToArray();
        if (runes.Length != alphabet.Length)
            throw new ArgumentException($"The key must have exactly {alphabet.Length} letters.", nameof(symbols));
        var permutation = new int[alphabet.Length];
        var present = new HashSet<int>();
        var canonical = new StringBuilder();
        for (int i = 0; i < runes.Length; i++)
        {
            if (!alphabet.TryGetIndex(runes[i], out int index))
                throw new ArgumentException($"Key symbol '{runes[i]}' is outside the {alphabet.Name} alphabet.", nameof(symbols));
            if (!present.Add(index)) throw new ArgumentException($"Duplicate key letter '{alphabet.At(index)}'. A substitution must be bijective.", nameof(symbols));
            permutation[i] = index;
            canonical.Append(alphabet.At(index).ToString());
        }
        return new SubstitutionKey(canonical.ToString(), alphabet, permutation);
    }

    public static SubstitutionKey Generate(Alphabet alphabet)
    {
        ArgumentNullException.ThrowIfNull(alphabet);
        var symbols = alphabet.Symbols.EnumerateRunes().ToArray();
        for (int i = symbols.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (symbols[i], symbols[j]) = (symbols[j], symbols[i]);
        }
        return Create(string.Concat(symbols.Select(r => r.ToString())), alphabet);
    }

    public string ToJson() => JsonSerializer.Serialize(new KeyDocument(1, Alphabet.Id, Symbols), new JsonSerializerOptions { WriteIndented = true });
    public static SubstitutionKey FromJson(string json, Alphabet? expectedAlphabet = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        var document = JsonSerializer.Deserialize<KeyDocument>(json) ?? throw new FormatException("Empty substitution-key document.");
        if (document.Version != 1) throw new FormatException("Unsupported substitution-key document version.");
        var alphabet = Models.Alphabet.FromId(document.Alphabet);
        if (expectedAlphabet is not null && expectedAlphabet.Id != alphabet.Id)
            throw new ArgumentException("The imported key uses a different alphabet.", nameof(json));
        return Create(document.Symbols, alphabet);
    }
    private sealed record KeyDocument(int Version, string Alphabet, string Symbols);
}
