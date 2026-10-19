using System.Collections.ObjectModel;
using System.Text;

namespace Cryptology.Core.Models;

/// <summary>Exact English/Ukrainian letters. UI language is independent of this choice.</summary>
public sealed class Alphabet
{
    public static Alphabet English { get; } = new("en", "English", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ETAOINSHRDLCUMWFGYPBVKJXQZ");
    public static Alphabet Ukrainian { get; } = new("uk", "Українська", "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ", "ОАНИВТЕРСЛІДПУМКЯЬЗБГЧХЙШЦЮЄЇЖФЩҐ");

    private readonly Rune[] letters;
    private readonly Dictionary<Rune, int> indices = new();
    public string Id { get; }
    public string Name { get; }
    public string Symbols { get; }
    public string FrequencyOrder { get; }
    public int Length => letters.Length;
    public IReadOnlyList<string> Letters { get; }

    private Alphabet(string id, string name, string symbols, string frequencyOrder)
    {
        Id = id;
        Name = name;
        Symbols = symbols;
        FrequencyOrder = frequencyOrder;
        letters = symbols.EnumerateRunes().ToArray();
        Letters = new ReadOnlyCollection<string>(letters.Select(r => r.ToString()).ToArray());
        for (int i = 0; i < letters.Length; i++)
        {
            indices.Add(letters[i], i);
            indices.Add(Rune.ToLowerInvariant(letters[i]), i);
        }
    }

    public bool TryGetIndex(Rune symbol, out int index) => indices.TryGetValue(symbol, out index);
    public bool Contains(Rune symbol) => indices.ContainsKey(symbol);
    public Rune At(int index) => letters[index];
    public Rune MatchCase(int index, Rune original) => Rune.IsLower(original) ? Rune.ToLowerInvariant(letters[index]) : letters[index];
    public string NormalizeSymbol(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var runes = symbol.EnumerateRunes().ToArray();
        if (runes.Length != 1 || !TryGetIndex(runes[0], out int index))
            throw new ArgumentException($"Each symbol must be one letter from the {Name} alphabet.", nameof(symbol));
        return letters[index].ToString();
    }
    public static Alphabet FromId(string id) => id?.ToLowerInvariant() switch
    {
        "en" or "eng" or "english" => English,
        "uk" or "ua" or "ukr" or "ukrainian" => Ukrainian,
        _ => throw new ArgumentException("Choose the English (en) or Ukrainian (uk) alphabet.", nameof(id))
    };
}
