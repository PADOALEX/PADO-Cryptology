# Python audit and migration map

All thirteen project-owned Python files were read in full before implementing their C# replacements. The review covered algorithms, mathematics, correct behavior, invalid domains, cryptographic risks, repeated code and test requirements. IDE metadata, virtual environments and third-party packages were excluded. Clean original files remain in [`legacy/python`](../../legacy/python); they are not dependencies of the C# runtime.

## Python → C# correspondence

| Original source | Preserved workflow / mathematics | C# destination | Audit and verification |
|---|---|---|---|
| `cryptology.py` | Ten module navigation, dark/light theme, Ukrainian/English UI, reset/copy/load/save actions; no mathematical algorithm in the shell. | Desktop `App`, `ShellViewModel`, `ToolViewModel`, `MainWindow`, `Resources.Strings` and `FileService`. | Shell details below; headless/native results in [validation](../validation.md). |
| `config.py` | EN26 and UA33 alphabets including Ґ,Є,І,Ї; localization dictionaries and original frequency ranking. | Core `Models.Alphabet`, `Cryptanalysis.ReferenceFrequencies`; Desktop `Resources.Strings`. | [Classical audit](classical.md), configuration details below, alphabet/locale tests. |
| `utils.py` | UTF-8 load/save and clipboard workflows; no cipher mathematics. | Desktop `IFileService` / `FileService`, with test doubles for view-model workflows. | Shared-services details below; [classical audit](classical.md). |
| `tab1.py` | Symbol counts, relative frequencies, sorting, text import and result export. | Core `FrequencyAnalyzer`; Desktop `FrequencyViewModel` and `FrequencyChart`. | [Classical audit](classical.md), frequency/Unicode/options tests and CSV workflow checks. |
| `tab2.py` | Caesar modular rotation and complete substitution permutation; case/punctuation retention. | Core `CaesarCipher`, `SubstitutionKey`, `SubstitutionCipher`; Desktop `ClassicalViewModel`. | [Classical audit](classical.md), known vectors, signed shifts, strict key validation and round trips. |
| `tab3.py` | Frequencies, overlapping bigrams/trigrams, manual partial substitution, live preview, duplicate warnings and baseline rank guess. | Core `NGramAnalyzer`, `CryptanalysisService`; Desktop `AnalysisViewModel`. | [Classical audit](classical.md), preview/token statistics/mapping tests; additional transparent Caesar scoring. |
| `tab4.py` | Repeating-key Vigenère modulo the chosen alphabet; advance key only on matched letters. | Core `VigenereCipher`; Desktop `VigenereViewModel`. | [Classical audit](classical.md), EN/UA controls and strict key/round-trip tests. |
| `tab5.py` | XOR reversibility and text-file learning scenario; interchange representation deliberately corrected. | Security `ByteOneTimePad`; Desktop `OtpViewModel`. | [Security audit](security.md), original ASCII XOR controls, UTF-8 byte/bounds/tamper tests. |
| `tab6.py` | Euclidean/Bézout recurrence, square-and-multiply, random integer/prime/coprime workflows. | Core `IntegerMath`, `CryptoRandom`, `PrimeGenerator`, `PrimalityTest`; Desktop `MathViewModel`. | [Mathematics audit](mathematics.md), signed/zero/modulus/inverse/trace/cancellation tests. |
| `tab7.py` | Jacobi reciprocity and Solovay–Strassen; explicit probable-prime learning workflow. | Core `JacobiSymbol`, `PrimalityTest`; Desktop `TheoryViewModel`. | [Mathematics audit](mathematics.md), Jacobi controls, composite/prime/round-domain tests and MR comparison. |
| `tab8.py` | p,q,n,phi,e,d and educational integer RSA; standard API mode added separately. | Core `EducationalRsa`; Security `ModernRsaService`; Desktop `RsaViewModel`. | [Mathematics audit](mathematics.md), [security audit](security.md), known RSA vector/domain checks and provider OAEP tests. |
| `tab9.py` | Mathematical SHA-256 representative signing, file selection and verification; PSS added separately. | Core `EducationalRsa.SignData/VerifyData`; Security `ModernRsaService`; Desktop `SignatureViewModel`. | [Mathematics audit](mathematics.md), [security audit](security.md), representative controls, changed files/signatures/keys, empty/streamed files. |
| `tab10.py` | Prime-field polynomial evaluation and Lagrange recovery. | Core `ShamirSecretSharing`, `Share`, `ShamirShareSet`; Desktop `ShamirViewModel`. | [Mathematics audit](mathematics.md), every threshold subset, k=1, JSON metadata, invalid/duplicate/insufficient points, cancellation and tamper limits. |

## Shell: `cryptology.py`

The main window eagerly creates ten CustomTkinter frames, selects the active frame, switches theme/language and coordinates clipboard/file/reset actions. These user scenarios are worth retaining; there is no mathematical computation to preserve in this module.

The original code reaches private CustomTkinter internals such as `_segmented_button._buttons_dict`; those accesses are fragile across toolkit releases. Heavy event-handler calls run on the UI thread. Broad clipboard exception handlers can silently hide failures, and reset code depends on dozens of concrete widget names and misses some state. Repeated per-tab wiring couples the shell to each module's internals.

The replacement uses a small DI composition root, a shell holding ten module view models, native Avalonia controls, common observable command/status/cancellation behavior and local OS-facing services. Algorithm state belongs to the view model, while calculations remain in Core/Security. Theme and locale are shell settings; selecting a UI language does not change a cipher's alphabet. The tests cover module switching and view-model workflows; actual native launch, clipboard and file-dialog checks are recorded separately rather than inferred from headless success.

## Configuration: `config.py`

The original alphabet definitions are correct: English has 26 letters and Ukrainian has 33, including Ґ, Є, І, Ї. The two translation dictionaries provide the original bilingual vocabulary. `FREQ_*` entries are ordered strings, not measured frequency percentages or an empirical corpus.

Alphabet order and baseline ranking are preserved centrally. Exact alphabet membership replaces broad Cyrillic ranges in downstream tab logic. Ukrainian chart/scoring weights are transparently derived from the retained rank; English weights are illustrative. Locale and algorithm alphabet are now independent. Regression checks cover both alphabets, mixed scripts, case, supplementary Unicode, expected reference ordering and locale switching. No statistical or machine-learning claim is made for the original ranking.

## Shared services: `utils.py`

UTF-8 reading/writing and clipboard operations work for the original happy path and remain useful workflows. The implementation reads whole files synchronously, takes concrete application/widgets as dependencies and exposes broad errors; its wrappers overlap per-tab file/clipboard handlers. There is no mathematical algorithm to migrate from this module.

Desktop now uses `IFileService`, Avalonia `StorageProvider` dialogs and clipboard APIs. Text reading/writing is asynchronous with strict UTF-8 decoding, cancellation and a 32 MiB editor import cap. Saving truncates an existing selected output to prevent stale trailing bytes. Modern file signatures have a separate streaming path. Headless tests use service doubles; OS integration remains a native verification responsibility.

## Controls before correction

Controls execute actual original Python methods through AST extraction and inert widget adapters, avoiding GUI dependencies. Fixed seeds make the original PRNG behavior reproducible. Files recording defects are historical evidence; incorrect old outputs are not accepted as new expected behavior.

| Area | Generator / controls |
|---|---|
| Classical ciphers and analysis | [`generate_classical_vectors.py`](generate_classical_vectors.py), [`classical-control-vectors.json`](classical-control-vectors.json) |
| Number theory and mathematical RSA/Shamir | [`tools/reference_vectors.py`](../../tools/reference_vectors.py), [`tests/vectors/mathematics.json`](../../tests/vectors/mathematics.json) |
| Original byte-XOR/security behavior | [`scripts/original_security_vectors.py`](../../scripts/original_security_vectors.py), [`security-original-vectors.json`](security-original-vectors.json) |

Reproduce from the repository root with an installed Python 3:

```sh
python docs/audit/generate_classical_vectors.py
python tools/reference_vectors.py --original legacy/python
python scripts/original_security_vectors.py
```

No CustomTkinter installation is needed. The math JSON records SHA-256 hashes of its five audited source files; the other audit pages describe their controls and deliberate corrections. C# checks compare correct controls, assert algebraic invariants and exercise corrected invalid-input behavior. Actual executed results are in [validation.md](../validation.md).

## Intentional mathematical and security corrections

Negative GCD normalization, exponent-zero modulo-one behavior, invalid primality rounds, equal RSA factors and representative bounds are corrected. Vigenère no longer silently discards key characters; substitution keys and partial mappings validate the chosen alphabet. Unicode character XOR becomes explicit UTF-8 byte XOR. Shamir preserves threshold/prime metadata, supports 1≤k≤n, validates coordinates and documents the limits of detecting in-range tampering. All repeated GCD, inverse, primality and modular-power logic is consolidated.

Educational arithmetic remains visibly separate from platform OAEP/PSS. No original tiny-prime textbook RSA or unauthenticated sharing/XOR workflow is presented as a production security protocol. See the [security boundaries](../security.md) and detailed module audits for rationale and tests.
