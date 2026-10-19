# Security migration audit

The complete original `tab5.py`, `tab8.py` and `tab9.py` were read before changing their security behavior. The number theory and textbook RSA details are also covered in [mathematics.md](mathematics.md). Original code remains in `legacy/python`; it is historical source, not an application dependency.

| Original module | Algorithm and correct behavior | Defects and risks | Migration and tests |
| --- | --- | --- | --- |
| `tab5.py` | `ord(plaintext) XOR ord(key)` is reversible on Python code points when the same key is reused for reversal. The UI supports load, save, copy, swap and clear. | `random.choice` over a 26/33 letter alphabet is predictable and does not produce uniform random bytes. The key is measured in Unicode characters, a longer key is silently truncated by `zip`, and arbitrary XOR code points can be control characters or invalid interchange text. Both buttons use the same implementation. Empty input is rejected. No authenticity or reuse protection exists. | Replace the representation deliberately: strict UTF-8 bytes, RNG key with exactly the byte count, byte XOR, Base64 ciphertext and Base64/HEX keys. Reject both short and long keys. Preserve text round trips and clear temporary byte arrays. Test original ASCII XOR, all byte values, Ukrainian and supplementary Unicode, malformed encodings, exact lengths, empty text, invalid UTF-8, and tampering behavior. |
| `tab8.py` | Modular exponentiation, Extended Euclid and RSA equations are useful educational demonstrations. | Predictable tiny primes; private exponent displayed automatically; manually equal primes accepted; code points can exceed the modulus; deterministic per-character encryption leaks repeats; no OAEP; no key persistence standard. | Keep textbook math in Core with validated constraints and visible educational labels. Add independent standard-library RSA-OAEP SHA-256 demo in Security, >=2048 bits with 3072 default, strict short-message limit, public PEM and consent-gated encrypted private PEM. Test boundaries, wrong keys, damaged ciphertext and key import. |
| `tab9.py` | SHA-256 of the exact file bytes is reduced modulo `n`, signed and checked with modular exponentiation. Empty files can be signed. | The tiny modulus collapses the hash space; raw RSA has no PSS padding; unbounded whole-file reads; redundant math; broad exception handling; no signature range check (signature plus `n` also verifies). | Preserve this only as the textbook Core demonstration. Standard demo uses streaming async/cancellable SHA-256 and RSA-PSS `SignHash` / `VerifyHash`, detached Base64 signatures, public-key verification, malformed signature rejection and tamper/wrong-key/empty-file tests. |

## Original control vectors

Before implementation, `scripts/original_security_vectors.py` extracts the actual original method ASTs and executes them against inert widget objects. It needs only Python's standard library, does not import CustomTkinter, and fixes the random seed for original primality checks. The companion `security-original-vectors.json` records actual results, including defects; defect outputs are historical evidence, not desired C# behavior.

```powershell
python scripts/original_security_vectors.py
```

Key examples: `ABC` XOR `XYZ` gives code points `[25,27,25]`; `XYZABC` is accepted for the same three-character text; textbook RSA with `p=61,q=53` encrypts `A` to `2790`; repeated `AA` repeats that block. A supplementary Unicode character outside `n` loses information. Original signature verification accepts an out-of-range signature obtained by adding `n`.

The byte-level ASCII XOR vector carries forward unchanged. The original Unicode XOR representation, overlong-key acceptance, code-point RSA reduction and out-of-range signature acceptance are intentionally corrected. Modern OAEP and PSS outputs are randomized and are tested through round trips, tamper rejection and interoperability with .NET rather than fixed ciphertext strings.
