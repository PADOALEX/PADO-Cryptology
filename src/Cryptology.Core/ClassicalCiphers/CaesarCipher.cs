using System.Text;
using Cryptology.Core.Models;

namespace Cryptology.Core.ClassicalCiphers;

/// <summary>Historical, educational cipher; preserves case and all nonalphabet symbols.</summary>
public static class CaesarCipher
{
    public static string Encrypt(string text, long shift, Alphabet alphabet, CancellationToken cancellationToken = default) => Transform(text, shift, alphabet, false, cancellationToken);
    public static string Decrypt(string text, long shift, Alphabet alphabet, CancellationToken cancellationToken = default) => Transform(text, shift, alphabet, true, cancellationToken);

    private static string Transform(string text, long shift, Alphabet alphabet, bool decrypt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(alphabet);
        int normalized = (int)((shift % alphabet.Length + alphabet.Length) % alphabet.Length);
        if (decrypt) normalized = (alphabet.Length - normalized) % alphabet.Length;
        var output = new StringBuilder(text.Length);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune symbol in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Append(alphabet.TryGetIndex(symbol, out int index)
                ? alphabet.MatchCase((index + normalized) % alphabet.Length, symbol).ToString()
                : symbol.ToString());
        }
        return output.ToString();
    }
}
