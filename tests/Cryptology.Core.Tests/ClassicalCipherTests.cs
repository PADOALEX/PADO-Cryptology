using System.Text;
using Cryptology.Core.ClassicalCiphers;
using Cryptology.Core.Models;

namespace Cryptology.Core.Tests;

public sealed class ClassicalCipherTests
{
    [Fact]
    public void AlphabetsPreserveOriginalOrderAndExactMembership()
    {
        Assert.Equal(26, Alphabet.English.Length);
        Assert.Equal(33, Alphabet.Ukrainian.Length);
        Assert.Equal("АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ", Alphabet.Ukrainian.Symbols);
        foreach (Rune rune in "ҐґЄєІіЇї".EnumerateRunes()) Assert.True(Alphabet.Ukrainian.Contains(rune));
        foreach (Rune rune in "ЫЭЪЁыэъёſ".EnumerateRunes()) Assert.False(Alphabet.Ukrainian.Contains(rune));
        Assert.False(Alphabet.English.Contains(new Rune('ſ')));
        Assert.Equal(Alphabet.Ukrainian, Alphabet.FromId("ua"));
        Assert.Throws<ArgumentException>(() => Alphabet.FromId("ru"));
    }

    [Theory]
    [InlineData("Abc XyZ! 123 😀", 3, "en", "Def AbC! 123 😀")]
    [InlineData("ҐЄІЇ, ґєії! Яя", 5, "uk", "ЗЇМН, зїмн! Ґґ")]
    [InlineData("Abc", -1, "en", "Zab")]
    [InlineData("ABC", 26000000003L, "en", "DEF")]
    public void CaesarMatchesExecutedLegacyControls(string text, long shift, string id, string expected)
    {
        var alphabet = Alphabet.FromId(id);
        Assert.Equal(expected, CaesarCipher.Encrypt(text, shift, alphabet));
        Assert.Equal(text, CaesarCipher.Decrypt(expected, shift, alphabet));
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [InlineData(-10000000000)]
    [InlineData(0)]
    public void CaesarHandlesEveryLongShiftWithoutOverflow(long shift)
    {
        foreach (var alphabet in new[] { Alphabet.English, Alphabet.Ukrainian })
        {
            string text = alphabet.Symbols + alphabet.Symbols.ToLowerInvariant() + "! 123 😀\n";
            Assert.Equal(text, CaesarCipher.Decrypt(CaesarCipher.Encrypt(text, shift, alphabet), shift, alphabet));
            Assert.Equal(CaesarCipher.Encrypt(text, shift % alphabet.Length, alphabet), CaesarCipher.Encrypt(text, shift, alphabet));
        }
    }

    [Theory]
    [InlineData("Attack at dawn!", "LEMON", "en", "Lxfopv ef rnhr!")]
    [InlineData("Привіт, ҐЄІЇ!", "КЛЮЧ", "uk", "Авжщхґ, ВБХЧ!")]
    [InlineData("A! A 😀 A", "BC", "en", "B! C 😀 B")]
    public void VigenereMatchesLegacyControlsAndSkipsPunctuation(string text, string key, string id, string expected)
    {
        var alphabet = Alphabet.FromId(id);
        Assert.Equal(expected, VigenereCipher.Encrypt(text, key, alphabet));
        Assert.Equal(text, VigenereCipher.Decrypt(expected, key, alphabet));
        Assert.Equal(expected, VigenereCipher.Encrypt(text, key.ToLowerInvariant(), alphabet));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("L!EMON")]
    [InlineData("LEMON1")]
    [InlineData("КЛЮЧ")]
    [InlineData("😀")]
    [InlineData("ſ")]
    public void VigenereRejectsInvalidKeysInsteadOfFiltering(string key) =>
        Assert.Throws<ArgumentException>(() => VigenereCipher.Encrypt("abc", key, Alphabet.English));

    [Fact]
    public void SubstitutionMatchesExecutedLegacyControl()
    {
        var key = SubstitutionKey.Create("ZYXWVUTSRQPONMLKJIHGFEDCBA", Alphabet.English);
        Assert.Equal("Zyx CbA!", SubstitutionCipher.Encrypt("Abc XyZ!", key));
        Assert.Equal("Abc XyZ!", SubstitutionCipher.Decrypt("Zyx CbA!", key));
        Assert.Equal(key.Symbols, SubstitutionKey.Create(key.Symbols.ToLowerInvariant(), key.Alphabet).Symbols);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXY")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYA")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYa")]
    [InlineData("BCDEFGHIJKLMNOPQRSTUVWXYZ0")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYЖ")]
    public void SubstitutionRejectsMissingDuplicateAndForeignLetters(string value) =>
        Assert.Throws<ArgumentException>(() => SubstitutionKey.Create(value, Alphabet.English));

    [Fact]
    public void GeneratedSubstitutionIsPermutationAndJsonIsAlphabetBound()
    {
        foreach (var alphabet in new[] { Alphabet.English, Alphabet.Ukrainian })
        {
            var key = SubstitutionKey.Generate(alphabet);
            Assert.Equal(alphabet.Length, key.Symbols.Length);
            Assert.Equal(alphabet.Symbols.OrderBy(c => c), key.Symbols.OrderBy(c => c));
            var imported = SubstitutionKey.FromJson(key.ToJson(), alphabet);
            Assert.Equal(key.Symbols, imported.Symbols);
            Assert.Equal(alphabet.Id, imported.Alphabet.Id);
            string text = alphabet.Symbols + alphabet.Symbols.ToLowerInvariant() + " 19😀!";
            Assert.Equal(text, SubstitutionCipher.Decrypt(SubstitutionCipher.Encrypt(text, key), key));
        }
        var english = SubstitutionKey.Generate(Alphabet.English);
        Assert.Throws<ArgumentException>(() => SubstitutionKey.FromJson(english.ToJson(), Alphabet.Ukrainian));
        Assert.Throws<FormatException>(() => SubstitutionKey.FromJson("{\"Version\":2,\"Alphabet\":\"en\",\"Symbols\":\"x\"}"));
        Assert.Throws<System.Text.Json.JsonException>(() => SubstitutionKey.FromJson("bad JSON"));
    }

    [Fact]
    public void DeterministicRoundTripsCoverEveryLetterAndShift()
    {
        foreach (var alphabet in new[] { Alphabet.English, Alphabet.Ukrainian })
        {
            string text = alphabet.Symbols + "\n" + alphabet.Symbols.ToLowerInvariant() + " Test / Тест 123 😀 e\u0301";
            foreach (int shift in Enumerable.Range(-alphabet.Length, alphabet.Length * 2 + 1))
                Assert.Equal(text, CaesarCipher.Decrypt(CaesarCipher.Encrypt(text, shift, alphabet), shift, alphabet));
            foreach (string key in new[] { alphabet.Letters[0], alphabet.Symbols, alphabet.Symbols.ToLowerInvariant() })
                Assert.Equal(text, VigenereCipher.Decrypt(VigenereCipher.Encrypt(text, key, alphabet), key, alphabet));
            var substitution = SubstitutionKey.Create(string.Concat(alphabet.Letters.Reverse()), alphabet);
            Assert.Equal(text, SubstitutionCipher.Decrypt(SubstitutionCipher.Encrypt(text, substitution), substitution));
        }
    }

    [Fact]
    public void EmptyTextIsAllowedAndLargeMixedInputRoundTrips()
    {
        var alphabet = Alphabet.Ukrainian;
        var key = SubstitutionKey.Create(string.Concat(alphabet.Letters.Reverse()), alphabet);
        Assert.Empty(CaesarCipher.Encrypt("", 5, alphabet));
        Assert.Empty(VigenereCipher.Encrypt("", "КЛЮЧ", alphabet));
        Assert.Empty(SubstitutionCipher.Encrypt("", key));
        string text = string.Concat(Enumerable.Repeat("Привіт ҐЄІЇ! 😀\n", 10000));
        Assert.Equal(text, VigenereCipher.Decrypt(VigenereCipher.Encrypt(text, "КЛЮЧ", alphabet), "КЛЮЧ", alphabet));
        Assert.Equal(text, SubstitutionCipher.Decrypt(SubstitutionCipher.Encrypt(text, key), key));
    }

    [Fact]
    public void TransformationsObserveCancellation()
    {
        var token = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => CaesarCipher.Encrypt("abc", 1, Alphabet.English, token));
        Assert.Throws<OperationCanceledException>(() => VigenereCipher.Encrypt("abc", "key", Alphabet.English, token));
        Assert.Throws<OperationCanceledException>(() => SubstitutionCipher.Encrypt("abc", SubstitutionKey.Create(Alphabet.English.Symbols, Alphabet.English), token));
    }
}
