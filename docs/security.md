# Security boundaries

PADO Cryptology is an educational and analytical toolkit. It has not received an independent security audit and is not production-grade data protection software.

## Educational algorithms and standard API demos

Caesar, substitution, Vigenère, textbook RSA, raw mathematical RSA signatures and the finite-field Shamir visualizer teach algorithms. Their availability does not imply a safe complete storage, messaging or key-management system. Textbook RSA is deterministic and has no OAEP or PSS encoding; the original tiny-prime RSA and signatures remain only in the historical sources. See the [original security audit](audit/security.md) and [mathematics audit](audit/mathematics.md).

The independent `Cryptology.Security` library delegates RSA generation and private-key arithmetic to `System.Security.Cryptography.RSA`. It generates 3072-bit keys by default and rejects keys below 2048 bits. Resource limits cap keys at 8192 bits and PEM input at 64 KiB. OAEP uses SHA-256; PSS signatures use SHA-256 and `.NET`'s `RSASignaturePadding.Pss`. Padding is implemented by the platform cryptography provider. These are standard API demonstrations, not a claim about the security of the entire application. See [Microsoft's RSA API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsa?view=net-10.0) and [PKCS #1, RFC 8017](https://www.rfc-editor.org/rfc/rfc8017.html).

OAEP accepts `k - 2*32 - 2` plaintext bytes, where `k` is the RSA modulus length in bytes. This means 190 bytes for a 2048-bit key and 318 bytes for a 3072-bit key. The application checks strict UTF-8 byte length before invoking RSA and never splits large messages into raw RSA blocks. No hybrid file-encryption format is implemented. A future large-file design needs authenticated symmetric encryption, such as AES-GCM, with a fresh nonce and a securely wrapped random symmetric key. The limit follows [RFC 8017 section 7.1](https://www.rfc-editor.org/rfc/rfc8017.html#section-7.1).

## XOR, a one-time pad, and authenticated encryption

XOR is an operation. Applying it to data with any arbitrary key is not automatically a one-time pad. A true OTP requires an independent, uniform secret pad as long as the message, securely shared beforehand and used exactly once. This application demonstrates byte XOR with a CSPRNG-generated pad; its key generation inherits the platform RNG's assumptions rather than demonstrating an information-theoretic entropy source. Generated keys have exactly the number of UTF-8 message bytes; both shorter and longer keys are rejected.

Keep every pad secret and use it once. Reusing the same pad for two messages exposes the XOR of the two plaintexts. Losing the pad makes recovery impossible. Plaintext, ciphertext and key must be kept separate when saved or shared. Base64 and HEX are transport encodings, not encryption.

Neither XOR nor a true OTP authenticates a message. Flipping a ciphertext bit flips the corresponding plaintext bit; a wrong key or damaged ciphertext can still produce valid UTF-8 and therefore cannot always be detected. Strict decoding rejects invalid UTF-8 but is not an integrity check. Modern authenticated encryption provides both confidentiality and detection of unauthorized changes under its specified nonce and key rules; this demo's OTP does not. Use a reviewed authenticated encryption format and key-management system for real confidential data.

## Key lifetime and export

Modern private keys are held in an `IDisposable` RSA session and are not automatically displayed, exported or logged. Public key export uses SubjectPublicKeyInfo PEM. Private export requires an explicit `allowPrivateKeyExport` consent flag and a passphrase of at least 12 characters; the UI must show a warning and obtain the user's affirmative choice before setting that flag. Export produces password-encrypted PKCS #8 PEM using AES-256-CBC, PBKDF2 with SHA-256 and 600,000 iterations through the [.NET encrypted PKCS #8 API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.asymmetricalgorithm.exportencryptedpkcs8privatekeypem?view=net-10.0). Choose a long, unique passphrase and protect both it and the exported file. The format's password protection does not turn this app into a key vault. Educational keys deliberately expose their factors and exponents for mathematical inspection.

Imports reject malformed, multiple, mixed-type and oversized PEM blocks. Public-only sessions cannot sign, decrypt, or export a private key. Import also supports existing unencrypted private PEM programmatically for interoperability; the application should prefer encrypted private key files. No unencrypted private-key export method is exposed by this library.

Temporary plaintext, decoded pad, key-file and hash byte buffers are cleared with `CryptographicOperations.ZeroMemory` where practical. Caller-owned generated key arrays must be cleared by their caller. .NET strings, clipboard contents, UI bindings, operating-system buffers and provider-managed key material cannot all be reliably erased by managed application code. Passwords, PEM and text returned as strings have this limitation. The application provides no claim of complete memory erasure.

## Files, signatures and cancellation

File signatures hash the exact bytes using SHA-256 with an asynchronous file stream, then call RSA `SignHash` or `VerifyHash` with PSS. Files are not read wholly into memory. Cancellation is observed while hashing and immediately before the RSA operation; the provider's final RSA call itself is not interruptible. Encrypted key-file imports move the synchronous provider password derivation to a worker thread and discard/dispose the result if cancellation arrives during that call. `FileShare.Read` prevents concurrent writers on platforms that enforce that sharing rule, but does not replace a hostile-filesystem threat model. Verification describes the bytes read during that operation.

Detached signatures are Base64 RSA blocks. Verification returns false for malformed, oversized, wrong-length, changed or wrong-key signatures. File access failures and cancellation remain distinct errors. A valid signature establishes possession of the corresponding private key over the exact bytes; it does not establish who owns a public key. Users must authenticate public-key provenance separately.

Tests cover Unicode and byte boundaries, original ASCII XOR vectors, invalid encoding, deliberate XOR malleability, OAEP interoperation and limits, wrong keys and ciphertext damage, encrypted PEM round trips and consent, PSS interoperation, altered files and signatures, malformed signatures, empty files, streaming files and cancellation. No real secrets or private keys are checked in.
