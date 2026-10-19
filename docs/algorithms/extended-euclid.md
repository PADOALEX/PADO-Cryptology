# Extended Euclidean algorithm and modular arithmetic

`Cryptology.Core.NumberTheory.IntegerMath` uses arbitrary-precision `BigInteger`. GCD is nonnegative, including signed inputs; gcd(0,0)=0. `ExtendedGcd(a,b)` returns g,x,y satisfying ax+by=g. It runs the Euclidean recurrence iteratively on |a| and |b| and restores coefficient signs. `ExtendedGcd(240,46)` returns (2,−9,47).

`ModInverse(a,m)` requires m>1 and gcd(a,m)=1. It normalizes the Bézout coefficient into [0,m). Otherwise it throws `ArgumentException` (or `ArgumentOutOfRangeException` for invalid modulus). Example: inverse(17,3120)=2753; inverse(−3,11)=7. `Normalize(a,m)` requires m>0 and returns the canonical nonnegative residue.

`ModPow(a,e,m)` implements binary square-and-multiply, requires e≥0 and m>0, and returns `ModPowResult.Value`. The accumulator starts at 1 mod m, so exponent zero with m=1 correctly yields zero. Example: 4^13 mod497=445. Negative bases are normalized first.

Pass `includeSteps:true` to receive `EuclidStep(Dividend,Divisor,Quotient,Remainder)` or `ModPowStep(Exponent,Base,Accumulator,Multiplied)`. Power traces show exponent/base before the iteration and accumulator after the optional multiply. Traces contain at most 1024 records; `TraceTruncated` reports omitted records while the calculation still completes. With tracing disabled the step collection is empty. Avoid traces when values are confidential.

All iterative operations accept `CancellationToken`; cancellation throws `OperationCanceledException`. Division and individual large-integer multiplication are synchronous and cannot be interrupted halfway through a runtime operation. These educational routines have no constant-time guarantee.

`CryptoRandom.Below(max)` uses `RandomNumberGenerator.Fill`, masks unused high bits and rejects samples ≥max. `InRange(min,max)` includes both bounds and supports negative endpoints; reversed bounds are rejected. `Coprime(n)` samples [1,n−1] until gcd=1, for n≥2. It never uses modulo reduction of random bytes to choose a bounded sample.

`PrimeGenerator.Generate(bits,rounds,token)` samples odd candidates with exactly the requested bit length (bits≥2). `GenerateInRange(min,max,rounds,token)` begins at a random integer and scans the complete interval with wraparound; if no candidate passes it throws. This interval search is **not** uniform over the primes in that interval. Both generators return probable primes tested by Miller–Rabin, not primality certificates. An interval can be expensive to exhaust; the desktop runs this work away from the UI thread and supports cancellation.

Reference controls are in `tests/vectors/mathematics.json`; regenerate with `python tools/reference_vectors.py --original legacy/python` before C# implementation changes.
