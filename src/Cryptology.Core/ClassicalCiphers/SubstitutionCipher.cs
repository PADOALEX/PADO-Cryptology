using System.Text;

namespace Cryptology.Core.ClassicalCiphers;

/// <summary>Historical monoalphabetic substitution; not suitable for confidential data.</summary>
public static class SubstitutionCipher
{
    public static string Encrypt(string text, SubstitutionKey key, CancellationToken cancellationToken = default) => Transform(text, key, false, cancellationToken);
    public static string Decrypt(string text, SubstitutionKey key, CancellationToken cancellationToken = default) => Transform(text, key, true, cancellationToken);
    private static string Transform(string text, SubstitutionKey key, bool decrypt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(key);
        var output = new StringBuilder(text.Length);
        int[] permutation = decrypt ? key.Reverse : key.Forward;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Rune symbol in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Append(key.Alphabet.TryGetIndex(symbol, out int index)
                ? key.Alphabet.MatchCase(permutation[index], symbol).ToString()
                : symbol.ToString());
        }
        return output.ToString();
    }
}
