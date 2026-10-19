using Cryptology.Core.ClassicalCiphers;
using Cryptology.Core.Cryptanalysis;
using Cryptology.Core.Models;

namespace Cryptology.Core.Tests;

public sealed class ClassicalCryptanalysisTests
{
    [Fact]
    public void FrequencyCanMatchLegacyCaseSensitiveControlOrFoldCase()
    {
        var sensitive = FrequencyAnalyzer.Analyze("Aaa B!", Alphabet.English, new() { CaseSensitive = true });
        Assert.Equal(4, sensitive.TotalSymbols);
        Assert.Equal(new[] { "a", "A", "B" }, sensitive.Rows.Select(r => r.Symbol));
        Assert.Equal(new[] { 2, 1, 1 }, sensitive.Rows.Select(r => r.Count));
        Assert.Equal(50, sensitive.Rows[0].Percentage);
        var folded = FrequencyAnalyzer.Analyze("Aaa B!", Alphabet.English);
        Assert.Equal(new[] { "A", "B" }, folded.Rows.Select(r => r.Symbol));
        Assert.Equal(75, folded.Rows[0].Percentage);
        Assert.Equal(100, folded.Rows.Sum(r => r.Percentage), 10);
    }

    [Fact]
    public void UkrainianIncludesAllSpecialLettersAndExcludesRussianOnlyLetters()
    {
        var result = FrequencyAnalyzer.Analyze("ҐґЄєІіЇї ЫЭЪЁ", Alphabet.Ukrainian);
        Assert.Equal(8, result.TotalSymbols);
        Assert.Equal(4, result.Rows.Count);
        Assert.All(result.Rows, r => Assert.Equal(2, r.Count));
    }

    [Theory]
    [InlineData(false, false, false, 1)]
    [InlineData(true, false, false, 3)]
    [InlineData(false, true, false, 2)]
    [InlineData(false, false, true, 3)]
    [InlineData(true, true, true, 6)]
    public void FrequencyInclusionOptionsCountUnicodeScalars(bool spaces, bool punctuation, bool other, int expected)
    {
        var result = FrequencyAnalyzer.Analyze("A😀😀! \n", Alphabet.English, new()
        {
            IncludeSpaces = spaces, IncludePunctuation = punctuation, IncludeNonAlphabet = other
        });
        Assert.Equal(expected, result.TotalSymbols);
        if (other) Assert.Equal(2, result.Rows.Single(r => r.Symbol == "😀").Count);
        Assert.Equal(100, result.Rows.Sum(r => r.Percentage), 10);
    }

    [Fact]
    public void FrequencySortingZeroCountsAndReferenceRankAreExplicit()
    {
        var result = FrequencyAnalyzer.Analyze("BB A", Alphabet.English, new() { Sort = FrequencySort.SymbolAscending });
        Assert.Equal(new[] { "A", "B" }, result.Rows.Select(r => r.Symbol));
        result = FrequencyAnalyzer.Analyze("BB A", Alphabet.English, new() { Sort = FrequencySort.CountAscending });
        Assert.Equal(new[] { 1, 2 }, result.Rows.Select(r => r.Count));
        result = FrequencyAnalyzer.Analyze("", Alphabet.English, new() { IncludeZeroCounts = true });
        Assert.Equal(26, result.Rows.Count);
        Assert.Equal(0, result.TotalSymbols);
        Assert.All(result.Rows, row => Assert.Equal(0, row.Percentage));
        Assert.Equal(1, result.Rows.Single(r => r.Symbol == "E").ReferenceRank);
        Assert.Equal(100, result.Rows.Sum(r => r.ReferencePercentage), 8);
        var ukrainian = FrequencyAnalyzer.Analyze("", Alphabet.Ukrainian, new() { IncludeZeroCounts = true });
        Assert.Equal(1, ukrainian.Rows.Single(r => r.Symbol == "О").ReferenceRank);
        Assert.Equal(33, ukrainian.Rows.Single(r => r.Symbol == "Ґ").ReferenceRank);
        Assert.Equal(100, ukrainian.Rows.Sum(r => r.ReferencePercentage), 8);
    }

    [Fact]
    public void NGramsMatchExecutedPythonControlAndRespectWordBoundaries()
    {
        var bigrams = NGramAnalyzer.Analyze("ABABA AB", Alphabet.English, 2);
        Assert.Equal(new[] { "AB", "BA" }, bigrams.Select(r => r.NGram));
        Assert.Equal(new[] { 3, 2 }, bigrams.Select(r => r.Count));
        var trigrams = NGramAnalyzer.Analyze("ABABA AB", Alphabet.English, 3);
        Assert.Equal(new[] { "ABA", "BAB" }, trigrams.Select(r => r.NGram));
        Assert.Equal(new[] { 2, 1 }, trigrams.Select(r => r.Count));
        Assert.Empty(NGramAnalyzer.Analyze("A B", Alphabet.English, 2));
        Assert.Equal("AB", NGramAnalyzer.Analyze("A 😀 B", Alphabet.English, 2, true).Single().NGram);
        Assert.Equal(3, NGramAnalyzer.Analyze("AAAA", Alphabet.English, 2).Single().Count);
        Assert.Empty(NGramAnalyzer.Analyze("", Alphabet.English, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => NGramAnalyzer.Analyze("AB", Alphabet.English, 0));
    }

    [Fact]
    public void UkrainianNGramsUseSpecialAlphabetLettersAndStableTieSorting()
    {
        var result = NGramAnalyzer.Analyze("ґєії ґє", Alphabet.Ukrainian, 2);
        Assert.Equal("ҐЄ", result[0].NGram);
        Assert.Equal(2, result[0].Count);
        Assert.Equal(100, result.Sum(r => r.Percentage), 8);
    }

    [Fact]
    public void BaselineGuessPreservesOriginalFrequencyRanking()
    {
        var map = CryptanalysisService.BaselineFrequencyGuess("ZZZ XX Y", Alphabet.English);
        Assert.Equal("E", map["Z"]);
        Assert.Equal("T", map["X"]);
        Assert.Equal("A", map["Y"]);
        Assert.Equal(3, map.Values.Distinct().Count());
        Assert.Empty(CryptanalysisService.BaselineFrequencyGuess("123!", Alphabet.English));
    }

    [Fact]
    public void ChiSquareRecoversKnownShiftFromIndependentEnglishText()
    {
        var alphabet = Alphabet.English;
        const string text = "Frequency analysis compares letter counts with a reference language profile. " +
            "A longer message usually gives a more useful result because short samples are affected by chance. " +
            "The classical Caesar cipher shifts every letter by the same amount, so testing every possible shift " +
            "is inexpensive. This demonstration preserves spaces and punctuation, allowing a person to read " +
            "the strongest candidates and decide whether the suggested plaintext makes sense. The method " +
            "offers evidence for an initial guess and never guarantees that a message has been recovered correctly.";
        string encrypted = CaesarCipher.Encrypt(text, 17, alphabet);
        var candidates = CryptanalysisService.RankCaesar(encrypted, alphabet);
        Assert.Equal(26, candidates.Count);
        Assert.Equal(17, candidates[0].Shift);
        Assert.Equal(text, candidates[0].Plaintext);
        Assert.True(candidates.Zip(candidates.Skip(1)).All(pair => pair.First.Score <= pair.Second.Score));
        Assert.Throws<ArgumentException>(() => CryptanalysisService.RankCaesar("123😀", alphabet));
    }

    [Fact]
    public void PartialPreviewPreservesCaseShowsDuplicatesAndCountsRealCoverage()
    {
        var result = CryptanalysisService.Preview("AaB, 😀 c", Alphabet.English, new Dictionary<string, string> { ["a"] = "e", ["B"] = "E" });
        Assert.Equal("EeE, 😀 c", result.Text);
        Assert.Equal(new[] { "E" }, result.DuplicateTargets);
        Assert.Equal(4, result.Statistics.TotalSymbols);
        Assert.Equal(3, result.Statistics.UniqueSymbols);
        Assert.Equal(3, result.Statistics.MappedSymbols);
        Assert.Equal(2, result.Statistics.MappedUniqueSymbols);
        Assert.Equal(0.75, result.Statistics.CharacterCoverage);
        Assert.Equal(2.0 / 3, result.Statistics.UniqueCoverage);
        Assert.Equal(result.Text, string.Concat(result.Segments.Select(s => s.Text)));
        Assert.Equal("EeE", result.Segments.Single(s => s.IsMapped).Text);
        Assert.Equal("c", result.Segments.Single(s => s.IsAlphabet && !s.IsMapped).Text);
    }

    [Fact]
    public void EmptyPreviewAndEmptyEditorCellsAreValid()
    {
        var map = new Dictionary<string, string> { ["A"] = "" };
        var result = CryptanalysisService.Preview("", Alphabet.English, map);
        Assert.Empty(result.Text);
        Assert.Empty(result.Segments);
        Assert.Equal(0, result.Statistics.CharacterCoverage);
        Assert.Equal("Ab!", CryptanalysisService.Preview("Ab!", Alphabet.English, map).Text);
    }

    [Fact]
    public void MappingJsonRoundTripsAndRejectsWholeInvalidDocument()
    {
        var alphabet = Alphabet.Ukrainian;
        var map = new Dictionary<string, string> { ["ґ"] = "і", ["Є"] = "Ї" };
        string json = CryptanalysisService.MappingToJson(map, alphabet);
        var imported = CryptanalysisService.MappingFromJson(json, alphabet);
        Assert.Equal("І", imported["Ґ"]);
        Assert.Equal("Ї", imported["Є"]);
        Assert.Equal("Ії!", CryptanalysisService.Preview("Ґє!", alphabet, imported).Text);
        Assert.Throws<ArgumentException>(() => CryptanalysisService.MappingFromJson(json, Alphabet.English));
        Assert.Throws<ArgumentException>(() => CryptanalysisService.Preview("x", Alphabet.English, new Dictionary<string, string> { ["A"] = "Ґ" }));
        Assert.Throws<ArgumentException>(() => CryptanalysisService.Preview("x", Alphabet.English, new Dictionary<string, string> { ["A"] = "B", ["a"] = "C" }));
        Assert.Throws<System.Text.Json.JsonException>(() => CryptanalysisService.MappingFromJson("bad JSON", alphabet));
    }

    [Fact]
    public void LargeAnalysisAndCancellationWork()
    {
        string text = string.Concat(Enumerable.Repeat("Ґєії, abc😀\n", 10000));
        Assert.Equal(40000, FrequencyAnalyzer.Analyze(text, Alphabet.Ukrainian).TotalSymbols);
        Assert.Equal(30000, NGramAnalyzer.Analyze(text, Alphabet.Ukrainian, 2).Sum(r => r.Count));
        var token = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => FrequencyAnalyzer.Analyze(text, Alphabet.English, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => NGramAnalyzer.Analyze(text, Alphabet.English, 2, cancellationToken: token));
        Assert.Throws<OperationCanceledException>(() => CryptanalysisService.Preview(text, Alphabet.English, new Dictionary<string, string>(), token));
        Assert.Throws<OperationCanceledException>(() => CryptanalysisService.RankCaesar(text, Alphabet.English, token));
    }
}
