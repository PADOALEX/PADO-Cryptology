# Shamir secret sharing over a prime field

`Cryptology.Core.SecretSharing.ShamirSecretSharing` implements educational polynomial splitting and Lagrange recovery inspired by [Adi Shamir, “How to Share a Secret” (1979)](https://people.cs.pitt.edu/~litman/courses/cs2001/lectures/lecture-02-p612-shamir.pdf). Parameters satisfy 1≤threshold≤totalShares<prime and 0≤secret<prime. The modulus is checked with Miller–Rabin during split, recovery and import; this is probable-prime validation, not a certificate.

`Split(secret,k,n,p,rounds,token)` chooses f(0)=secret and coefficients using the platform CSPRNG. For k>1, intermediate coefficients are uniform in [0,p), and the highest coefficient is uniform in [1,p) to retain degree k−1. It evaluates f(1)…f(n) with Horner's rule. For k=1 the polynomial is constant and every share reveals the secret; p=2,n=1 works. There is no secrecy against a holder of a threshold-one share.

The nonzero highest-coefficient requirement conditions the textbook uniform-coefficient distribution. Therefore this demonstration does **not** claim the exact information-theoretic privacy statement of standard Shamir with all nonconstant coefficients sampled independently from the full field. It is not a production secret-management product.

`Recover(ShamirShareSet,rounds,token)` requires at least the declared k shares and no more than declared n. It rejects nonprime moduli, invalid metadata, duplicate x, x≤0, x≥p, y<0 and y≥p. Any k distinct points determine f; the secret is the Lagrange interpolation value f(0)=Σ y_i Π_(j≠i)(−x_j)/(x_i−x_j) mod p. Additional supplied points must agree with this polynomial, or recovery throws.

This is not authenticated sharing. Exactly k valid-looking shares always interpolate to some polynomial; an arbitrary in-range modification can yield a plausible wrong secret without being detectable. Extra points can show inconsistency but cannot identify a malicious owner or prove authenticity. A real scheme needs authenticated metadata/shares and an appropriate threat model. No check here replaces that.

The Python control polynomial f(x)=1234+166x+94x² mod 2089 gives points (1,1494),(2,1942),(3,489),(4,1313),(5,236). All ten three-point subsets recover 1234.

`Export(set,rounds,token)` and `Import(json,rounds,token)` use strict version1 JSON with decimal strings for arbitrary-sized integers:

```json
{
  "version": 1,
  "prime": "2089",
  "threshold": 3,
  "totalShares": 5,
  "shares": [
    { "x": "1", "y": "1494" },
    { "x": "2", "y": "1942" },
    { "x": "3", "y": "489" }
  ]
}
```

Import rejects missing, unknown or duplicate properties, unsupported versions, noncanonical decimal strings, malformed point objects, insufficient shares and out-of-field values. Metadata persists when exporting a subset: `TotalShares` describes the original distribution; `Shares.Count` describes the included subset. Metadata has no cryptographic authenticity. Import checks structure; polynomial consistency of additional points is checked when recovering.

Long loops observe cancellation. Arithmetic is variable-time; immutable arbitrary-size integers cannot be guaranteed erased from managed memory. The code clears coefficient references after splitting but makes no secure-erasure claim.
