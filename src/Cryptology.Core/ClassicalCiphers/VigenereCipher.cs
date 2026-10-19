using System.Text;
using Cryptology.Core.Models;

namespace Cryptology.Core.ClassicalCiphers;

/// <summary>Historical repeating-key Vigenère; only alphabet letters consume the key.</summary>
public static class VigenereCipher
{
    public static string Encrypt(string text, string key, Alphabet alphabet, CancellationToken cancellationToken = default) => Transform(text, key, alphabet, false, cancellationToken);
    public static string Decrypt(string text, string key, Alphabet alphabet, CancellationToken cancellationToken = default) => Transform(text, key, alphabet, true, cancellationToken);

    private static string Transform(string text, string key, Alphabet alphabet, bool decrypt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(alphabet);
        Rune[] keyRunes = key.EnumerateRunes().ToArray();
        if (keyRunes.Length == 0) throw new ArgumentException("The keyword must contain at least one alphabet letter.", nameof(key));
        var shifts = new int[keyRunes.Length];
        for (int i = 0; i < keyRunes.Length; i++)
        {
            if (!alphabet.TryGetIndex(keyRunes[i], out shifts[i]))
                throw new ArgumentException($"The keyword contains '{keyRunes[i]}', which is outside the {alphabet.Name} alphabet. Remove it explicitly.", nameof(key));
        }
        var output = new StringBuilder(text.Length);
        int keyPosition = 0;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune symbol in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (alphabet.TryGetIndex(symbol, out int index))
            {
                int shift = shifts[keyPosition];
                int next = (index + (decrypt ? -shift : shift) + alphabet.Length) % alphabet.Length;
                output.Append(alphabet.MatchCase(next, symbol).ToString());
                keyPosition = (keyPosition + 1) % shifts.Length;
            }
            else output.Append(symbol.ToString());
        }
        return output.ToString();
    }
}
