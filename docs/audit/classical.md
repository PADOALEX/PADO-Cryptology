# Classical ciphers and cryptanalysis source audit

Audit performed before implementing the C# algorithms. Reviewed complete `config.py`, `tab1.py`, `tab2.py`, `tab3.py`, `tab4.py`, and shared `utils.py` from the supplied archive. The original code remains in `legacy/python`.

| Source | Original behavior | Assessment and migration |
|---|---|---|
| `config.py` | EN alphabet has 26 letters; UA has 33 including Ґ, Є, І, Ї. Reference strings give frequency **order**, not measured probabilities. UI language chooses the alphabet. | Preserve alphabet order and reference ranking in `Models.Alphabet`; choose UI language separately. Do not describe ranking strings as a statistical corpus. |
| `tab1.py` | Counts case-sensitive matched characters; sorts count descending then Unicode symbol; exports TXT/semicolon CSV. | Correct EN counting; UA regex `а-я` also accepts Russian-only Ы, Э, Ъ. CSV importer consumes a 1024-character sample without rewinding, and its side-effect generator can index malformed rows. Use exact alphabet membership, explicit inclusion options and structured export. |
| `tab2.py` | Caesar uses modular alphabet rotation; substitution uses complete permutation; both preserve case and punctuation. | Caesar math is sound. Key validation checks only length/duplicates: unique foreign symbols can be accepted, breaking the selected alphabet. Import says success for duplicate keys. Replace with immutable validated permutations and atomic JSON import. Generate permutations with the CSPRNG. |
| `tab3.py` | Uppercases entire input; counts chosen alphabet; overlapping bigrams/trigrams inside words; manual partial mapping, preview highlighting, duplicate warning, frequency-order initial guess, progress by distinct present letters. | Preserve workflow and baseline ranking. Existing preview loses original case, entries accept foreign characters, duplicate targets still apply, language changes rebuild/discard mappings, and statistics are not token coverage. C# preview preserves case and exposes both distinct-letter and token coverage plus duplicate diagnostics. Import validates the whole document before applying it. Add chi-square ranking of all Caesar keys as a separate transparent, limited attack. |
| `tab4.py` | Repeating-key Vigenère uses selected alphabet and advances key only on alphabet letters. Case/punctuation are preserved. | Formula is sound. Invalid key symbols are silently discarded, so `K!EY` becomes `KEY`. C# rejects empty and mixed/foreign keys with an explicit error. |
| `utils.py` | Repeated UTF-8 read/write and clipboard wrappers. | File dialogs/clipboard belong in Desktop; pure algorithms have no GUI dependency. |

## Shared concerns

Alphabet selection, key validation, case handling, file parsing, frequency counting, and table state are repeated across the Python frames. C# puts alphabet membership/case in one immutable model, pure transformations in `ClassicalCiphers`, and frequencies, n-grams, ranking and preview models in `Cryptanalysis`. No GUI event handlers are reused in the core.

The original tkinter widgets are replaced by small in-memory adapters to execute the original Python methods without opening the GUI. `classical-control-vectors.json` records those real results before migration. Correct cipher outputs are regression controls; Russian-only UA letters, uppercase preview, and silently filtered Vigenère keys are labeled defects and deliberately are **not** C# expected behavior.

## Verification plan

- Known EN and UA Caesar/Vigenère/substitution controls; round trips of mixed case, ҐЄІЇ, punctuation, emoji, empty text and large input.
- Caesar negative/very large shifts including `long.MinValue`; strict empty/foreign/duplicate substitution and Vigenère keys; permutation generation and JSON round trips.
- Frequency case folding, exact UA alphabet membership, optional whitespace/punctuation/other symbols, deterministic sorting, Unicode scalar counting, empty input and percentages.
- Word-boundary versus cross-boundary n-grams, overlapping windows, short/empty input, stable ties.
- Baseline guess ordering, deterministic complete Caesar rankings, explicit absence of evidence for no selected letters, manual preview case/segments/token statistics, duplicate mappings and atomic mapping import.

Classical ciphers and frequency attacks are educational/historical. Neither the baseline nor chi-square ranking guarantees plaintext recovery; neither uses machine learning. All deterministic test fixtures are public examples.

## Executed results

Before implementing the C# transformations, ran Python 3.13.7 against the supplied original methods using `generate_classical_vectors.py`. Reproduce from the repository root with `py -3 docs/audit/generate_classical_vectors.py` on Windows or `python3 docs/audit/generate_classical_vectors.py` elsewhere; the script depends only on Python's standard library and retained legacy sources, not CustomTkinter. After migration, `dotnet test tests/Cryptology.Core.Tests/Cryptology.Core.Tests.csproj -c Release --filter FullyQualifiedName~Classical` passed **46/46** tests on .NET SDK 10.0.102, with no compiler warnings. Full-solution and Desktop verification are recorded separately.
