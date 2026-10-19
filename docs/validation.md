# Validation record

Date: **19 October 2026**. Environment: Windows x64, .NET SDK **10.0.102**, .NET runtime **10.0.2**, Avalonia **11.3.22**. Results below were executed locally; no remote CI result is inferred.

## Build and automated checks

| Check | Actual result |
|---|---|
| `dotnet restore PADO.Cryptology.sln` | Passed; all seven projects restored. |
| `dotnet build PADO.Cryptology.sln -c Release --no-restore` | Passed, **0 warnings / 0 errors**. |
| `dotnet test PADO.Cryptology.sln -c Release` | **209 passed / 0 failed / 0 skipped**: Core126, Security49, UiChecks34. |
| `python scripts/cli_smoke.py` | **31/31** separate-process integration checks passed. |
| `dotnet run --project src/Cryptology.Cli -c Release -- demo` | All ten module demonstrations passed. |
| Original Python control generators | All three regenerated successfully using Python3 without GUI dependencies. |
| Legacy source integrity | All **13/13** retained files match the ZIP's SHA-256 hashes; see [manifest](audit/original-sha256.csv). |
| Windows self-contained publish | `dotnet publish src/Cryptology.Desktop -c Release -r win-x64 --self-contained true` passed. |

Core checks include both alphabets, exhaustive shift/property loops, strict keys, Unicode/large text, frequency options, n-grams, partial mappings, signed/zero arithmetic, traces, independent `BigInteger` references, known Jacobi/composite cases, textbook RSA and Shamir threshold subsets. Production random generators remain CSPRNGs.

Security checks cover exact byte XOR, strict UTF-8, key-length/format errors, OAEP size boundaries, wrong keys/damaged ciphertext, PSS changed/empty/binary files, malformed signatures, public-only verification, private-key export consent/passwords, encrypted PEM file exchange and cancellation.

The 34 Desktop checks cover all ten modules, four complete navigation cycles, actual Avalonia text input and bound encryption commands, live language relabeling, independent alphabets, light/dark switching, large-text round trips, manual preview/duplicate feedback, CSV/key/mapping/share/signature exchange, cancellation, stale signature-result invalidation, and deterministic shared-key lifecycle races. Eight checks exercise real UTF-8 file reading, UTF-8 BOMs, invalid UTF-8/UTF-16 rejection, the32 MiB limit and stream disposal on cancellation.

CLI integration uses real child processes and temporary freshly generated keys outside the source tree. It checks known EN/UA vectors, CSV, arithmetic, independently computed byte XOR, Shamir JSON, encrypted PEM file signing, wrong passwords, malformed signatures, changed and empty files, exit codes and overwrite refusal. The harness removes its temporary fixtures.

## Native Windows checks

The actual published `Cryptology.Desktop.exe` launched as a native Windows window without a startup error. Its sidebar exposes all ten modules. Native UI inspection checked module navigation and controls, and switching UI language to Ukrainian and the theme to Light updated the existing interface without changing the selected English cipher alphabet/key.

A real OS file dialog imported the UTF-8 fixture `Attack at dawn! ҐЄІЇ 🧪`. The Vigenère Encrypt button with `LEMON` produced `Lxfopv ef rnhr! ҐЄІЇ 🧪`. The native Copy result and Paste input buttons transferred that ciphertext through the Windows clipboard. Decrypt restored the original string. Export result used the native Save dialog; the saved file was compared with the input and was **byte-for-byte identical**. Native screenshots were inspected during this check; they are not redistributed as the headless captures below.

These native checks are representative OS integration checks. Cryptographic error permutations and repeated navigation are additionally exercised in the automated suites; they are not described as exhaustive manual testing.

## Screenshot provenance

The PNGs in [screenshots](screenshots) are **actual rendered Avalonia windows from the headless Skia backend**, captured by `Window.CaptureRenderedFrame()` during successful tests. They are not concept art, generated mockups or claimed native OS captures. Useful examples: [Caesar with typed input and result](screenshots/caesar-typed.png), [Ukrainian Light theme](screenshots/ukrainian-light.png), [frequency analysis](screenshots/frequency-analysis.png). Ten initial module views are also retained.

Reproduce captures from the repository root in PowerShell:

```powershell
$env:PADO_SCREENSHOTS = Join-Path (Get-Location) 'docs/screenshots'
dotnet test tests/Cryptology.UiChecks/Cryptology.UiChecks.csproj -c Release
Remove-Item Env:PADO_SCREENSHOTS
```

## Review and remaining boundaries

Independent source reviews covered the original modules, number-theory/RSA/Shamir correctness, classical VM integration, crypto-provider usage, cancellation and shared-key disposal, stale verification results, and real text-file decoding. Review findings were fixed and regression tests added where appropriate. This is engineering review, not an independent cryptographic audit.

This local validation record was written before the first repository publication. The [Windows/Ubuntu GitHub Actions workflow](../.github/workflows/ci.yml) now runs on pushes and pull requests; current remote results are available on the [Actions page](https://github.com/PADOALEX/PADO-Cryptology/actions/workflows/ci.yml). Local results above are separate from those remote runs. Native Linux/macOS desktop behavior remains unverified.

Editor imports are limited to32 MiB; Desktop numeric fields are limited to4096 characters to avoid synchronous huge-number parsing. Core retains arbitrary-precision arithmetic. The frequency chart displays up to40 letter rows; table/CSV data remain complete. Highlighted preview displays up to12,000 characters; the full transformed text is in Result. RSA provider calls and random filling cannot be interrupted mid-call; cancellable workflows observe cancellation around them and suppress/dispose canceled results. Errors include localized context and platform/library diagnostic details that may be English.

No hybrid file-encryption format, authenticated share transport, secure key vault, memory-erasure guarantee for managed strings, or production security certification is claimed. The project intentionally retains educational schemes with visible warnings. See [security boundaries](security.md).
