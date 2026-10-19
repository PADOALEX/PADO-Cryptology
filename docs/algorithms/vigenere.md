# Vigenère and classical cryptanalysis

Vigenère is a historical educational cipher. It should not protect confidential information. Caesar and monoalphabetic substitution have the same limitation.

For an alphabet of size *m*, assign each letter an index from 0 through *m − 1*. A keyword repeats, giving indices *kᵢ*. Encryption is `cᵢ = (pᵢ + kᵢ) mod m`; decryption is `pᵢ = (cᵢ − kᵢ + m) mod m`. Caesar is the special case of a constant shift. Substitution instead applies an arbitrary bijection of the alphabet.

The core supports `ABCDEFGHIJKLMNOPQRSTUVWXYZ` (26 letters) and `АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ` (33 letters). These exact orders come from the original project. Input case is preserved. Whitespace, punctuation, digits, emoji and foreign letters pass through and **do not consume** a Vigenère key position. `Attack at dawn!` with `LEMON` becomes `Lxfopv ef rnhr!`; `Привіт, ҐЄІЇ!` with `КЛЮЧ` becomes `Авжщхґ, ВБХЧ!`.

Keywords may be lowercase or uppercase, must be nonempty and must consist entirely of selected alphabet letters. Mixed alphabets, spaces and punctuation are rejected explicitly. In particular, `L!EMON` is an error; it does not silently become `LEMON`. Substitution keys must contain every selected alphabet letter exactly once, ignoring case. JSON documents include an alphabet identifier and schema version, and are validated before being returned to the UI.

Frequency analysis counts Unicode scalars, so an emoji counts once. Options separately include whitespace (all Unicode whitespace), punctuation, and other symbols, and optionally keep uppercase/lowercase distinct. The denominator is the number of included symbols. Frequency sorting is deterministic; count ties use ordinal symbol order. Zero-count rows are optional. Reference ranks preserve the original `FREQ_ENG`/`FREQ_UKR` strings. Their order does not constitute a measured corpus.

The displayed English reference percentages are a conventional illustrative frequency profile, normalized to 100%. Ukrainian percentages are explicitly **heuristic**: rank *r* from the original config has weight `r^−0.8`, then all 33 weights are normalized. They should not be cited as empirical Ukrainian statistics. Reference values are for each folded alphabet letter even when case-sensitive rows are shown.

N-grams are overlapping windows of normalized selected letters. By default a nonalphabet symbol ends a word, matching the original Python behavior. An explicit cross-boundary option removes separators before counting. `ABABA AB` therefore has `AB:3`, `BA:2`, `ABA:2`, `BAB:1` by default. Counts are ordered descending, then ordinal.

`BaselineFrequencyGuess` preserves the Python strategy of assigning the most frequent ciphertext letters to the reference ranking. Short text and ties often make this guess inaccurate. The improved **Caesar chi-square ranking** tries all *m* possible Caesar keys and scores each plaintext histogram using `Σ (observed − expected)² / expected`; lower scores rank first. This attack is limited to Caesar and does not claim to solve arbitrary substitution or Vigenère. Profiles, language, text length and subject matter affect reliability. Neither mode uses machine learning.

Manual substitution is a draft ciphertext-to-plaintext mapping. Empty cells mean unmapped. Invalid or multiple-letter entries are rejected, while duplicate target letters are allowed in draft preview and returned as explicit diagnostics. The original ciphertext remains unchanged. Preview segments identify mapped letters, unmapped alphabet letters and other characters for UI highlighting. Statistics distinguish coverage of distinct letters from coverage of actual alphabet-letter occurrences; a complete-looking draft may still be wrong.

All text transformations and analysis loops accept cancellation tokens. The unit tests include executed Python controls, exhaustive letter/shift round trips, Ukrainian special letters, strict key errors, long-shift overflow boundaries, JSON validation, Unicode counting, overlapping n-grams, deterministic ranking, preview coverage, cancellation and large mixed-text input.
