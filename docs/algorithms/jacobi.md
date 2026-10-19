# Jacobi symbol and probable-prime tests

`JacobiSymbol.Calculate(a,n,token)` requires a positive odd n. It returns −1, 0 or 1 and defines (a/1)=1. Numerators can be negative. The implementation normalizes a mod n, removes factors of two (flipping sign for n≡3 or5 mod8), applies quadratic reciprocity when swapping two values ≡3 mod4, and repeats Euclidean reduction. A final denominator other than one yields zero. These rules agree with [NIST DLMF §27.9](https://dlmf.nist.gov/27.9).

Examples: (1001/9907)=−1, (−1/7)=−1, (6/9)=0. Jacobi +1 for composite n does not establish a quadratic residue; (5/21)=1 is a useful demonstration.

`PrimalityTest.MillerRabin(n,rounds,token)` and `SolovayStrassen(n,rounds,token)` require rounds≥1, including for tiny inputs. Values below2 return false. Small-prime trial division handles trivial cases; each remaining round draws an independent cryptographic random base uniformly in [2,n−2].

Miller–Rabin writes n−1=2^s d with d odd, computes a^d mod n, and accepts a round only for 1, −1 or a subsequent square equal to −1. Solovay–Strassen compares a^((n−1)/2) mod n with the Jacobi residue and rejects zero Jacobi. For a fixed odd composite, independent rounds bound false acceptance by 4^−rounds and 2^−rounds respectively. The result is “probable prime”, never a proof. See [Handbook of Applied Cryptography, chapter4, algorithms4.18/4.24 and facts4.19/4.25](https://cacr.uwaterloo.ca/hac/about/chap4.pdf).

Default rounds are 32; the argument remains explicit in the UI. All long loops check cancellation and use the shared cancellable modular-power implementation. Tests cover primes, signed/zero edge inputs, invalid rounds, Carmichael numbers 561/1105/1729 and pseudoprimes 341/2047/1373653/3215031751. Randomized composite tests use 64 rounds; controls for original methods use the fixed Python seed 20261019.
