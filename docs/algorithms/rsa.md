# Educational integer RSA

`Cryptology.Core.Asymmetric.EducationalRsa` demonstrates textbook RSA. It is unpadded, deterministic, variable-time and unsuitable for real confidential messages or signatures. The separate platform cryptography service supplies OAEP and PSS.

`CreateKey(p,q,e,rounds,token)` validates distinct probable-prime factors, computes n=pq and phi=(p−1)(q−1), requires 1<e<phi and gcd(e,phi)=1, and computes d=e^−1 mod phi. The immutable key exposes P,Q,N,Phi,E,D for the learning interface. Passing the same prime twice or supplying an exponent outside this domain is an error; the service never silently chooses a different exponent.

`Encrypt(m,key,token)` computes c=m^e mod n and `Decrypt(c,key,token)` computes m=c^d mod n. Both require 0≤representative<n. This integer API intentionally avoids the original per-Unicode-character ambiguity: callers must choose their own explicit encoding or use OAEP for bytes. The representative bounds follow the raw RSA primitives in [RFC8017 §5.1](https://www.rfc-editor.org/rfc/rfc8017.html#section-5.1); raw primitives alone are not a complete encryption scheme.

Known vector: p=61,q=53,e=17 gives n=3233,phi=3120,d=2753; m=65 encrypts to 2790 and decrypts to 65. It also works for representatives sharing a factor with n, such as 53 or 61.

`SignData(data,key,token)` computes h=unsigned_big_endian(SHA256(data)) mod n and s=h^d mod n. `VerifyData(data,s,key,token)` rejects s<0 or s≥n and compares s^e mod n with the same h. For UTF-8 `abc` and the above key, h=256 and s=1480. A valid representative of SHA-256 reduced modulo a tiny n offers no meaningful SHA-256 collision security. These methods preserve an educational demonstration only; [RFC8017 §8.1](https://www.rfc-editor.org/rfc/rfc8017.html#section-8.1) describes the PSS encoding used by the modern signature service.

All large-integer loops observe cancellation. SHA-256 of a supplied span is a synchronous platform call; file streaming and asynchronous reading belong to the application or modern service. No private-key constant-time behavior or secure erasure of `BigInteger` memory is claimed.
